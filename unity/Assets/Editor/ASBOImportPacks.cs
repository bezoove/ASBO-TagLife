#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Imports downloaded Asset Store packages from the local cache without dialogs, one at a time, with anything that
    /// would touch the project's own settings (ProjectSettings/, Packages/) filtered out first.
    /// Driven from the job runner: "call ASBO.EditorTools.ASBOImportPacks.Import Rotary".
    /// </summary>
    public static class ASBOImportPacks
    {
        static IEnumerable<string> Cached()
        {
            var roots = new[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity") };
            foreach (var root in roots)
            {
                if (!Directory.Exists(root)) continue;
                foreach (var f in Directory.GetFiles(root, "*.unitypackage", SearchOption.AllDirectories)) yield return f;
            }
        }

        [MenuItem("ASBO/Packs/List Downloaded")]
        public static void List()
        {
            foreach (var f in Cached())
            {
                var paths = Paths(f);
                int risky = paths.Values.Count(p => Excluded(p));
                int scripts = paths.Values.Count(p => p.EndsWith(".cs"));
                var tops = paths.Values.Select(p => p.Split('/').Length > 1 ? p.Split('/')[0] + "/" + p.Split('/')[1] : p).Distinct().Take(4);
                Debug.Log($"[ASBO] pack '{Path.GetFileNameWithoutExtension(f)}' {new FileInfo(f).Length / 1048576f:F1} MB, {paths.Count} files, {scripts} scripts, {risky} settings files, roots: {string.Join(", ", tops)}");
            }
        }

        static bool Excluded(string p) => p.StartsWith("ProjectSettings/") || p.StartsWith("Packages/") || p == "ProjectSettings" || p == "Packages";

        /// <summary>Import every cached package whose file name contains the keyword (case-insensitive).</summary>
        public static void Import(string keyword)
        {
            var hits = Cached().Where(f => Path.GetFileNameWithoutExtension(f).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            if (hits.Count == 0) { Debug.LogWarning("[ASBO] no cached pack matching '" + keyword + "'"); return; }
            foreach (var f in hits)
            {
                var paths = Paths(f);
                var drop = new HashSet<string>(paths.Where(kv => Excluded(kv.Value)).Select(kv => kv.Key));
                string src = f;
                if (drop.Count > 0)
                {
                    src = Path.GetFullPath("Temp/asbo_filtered_" + Path.GetFileName(f));
                    Filter(f, src, drop);
                    Debug.Log($"[ASBO] {Path.GetFileName(f)}: left out {drop.Count} settings entries");
                }
                Debug.Log("[ASBO] importing " + Path.GetFileName(f));
                AssetDatabase.ImportPackage(src, false);
            }
        }

        // ---- .unitypackage = gzip(tar) of <guid>/{asset, asset.meta, pathname, preview.png}
        static Dictionary<string, string> Paths(string file)
        {
            var map = new Dictionary<string, string>();
            Walk(file, (name, data) =>
            {
                if (name.EndsWith("/pathname") && data != null)
                    map[name.Substring(0, name.IndexOf('/'))] = Encoding.UTF8.GetString(data).Split('\n')[0].Trim();
            }, true);
            return map;
        }

        static void Walk(string file, Action<string, byte[]> onEntry, bool wantPathnameData)
        {
            using var fs = File.OpenRead(file);
            using var gz = new GZipStream(fs, CompressionMode.Decompress);
            var hdr = new byte[512];
            while (ReadFull(gz, hdr, 512))
            {
                if (hdr.All(b => b == 0)) break;
                string name = Str(hdr, 0, 100); string prefix = Str(hdr, 345, 155);
                if (!string.IsNullOrEmpty(prefix)) name = prefix + "/" + name;
                name = name.TrimStart('.', '/');
                long size = Convert.ToInt64(Str(hdr, 124, 12).Trim().Length == 0 ? "0" : Str(hdr, 124, 12).Trim(), 8);
                long padded = (size + 511) / 512 * 512;
                byte[] data = null;
                if (wantPathnameData && name.EndsWith("/pathname") && size < 65536) { data = new byte[padded]; ReadFull(gz, data, (int)padded); Array.Resize(ref data, (int)size); }
                else Skip(gz, padded);
                onEntry(name, data);
            }
        }

        static void Filter(string src, string dst, HashSet<string> dropGuids)
        {
            using var fs = File.OpenRead(src);
            using var gz = new GZipStream(fs, CompressionMode.Decompress);
            using var os = File.Create(dst);
            using var ogz = new GZipStream(os, System.IO.Compression.CompressionLevel.Fastest);
            var hdr = new byte[512]; var buf = new byte[1 << 16];
            while (ReadFull(gz, hdr, 512))
            {
                if (hdr.All(b => b == 0)) break;
                string name = Str(hdr, 0, 100); string prefix = Str(hdr, 345, 155);
                if (!string.IsNullOrEmpty(prefix)) name = prefix + "/" + name;
                string guid = name.TrimStart('.', '/').Split('/')[0];
                long size = Convert.ToInt64(Str(hdr, 124, 12).Trim().Length == 0 ? "0" : Str(hdr, 124, 12).Trim(), 8);
                long padded = (size + 511) / 512 * 512;
                bool keep = !dropGuids.Contains(guid);
                if (keep) ogz.Write(hdr, 0, 512);
                long left = padded;
                while (left > 0)
                {
                    int n = (int)Math.Min(buf.Length, left);
                    if (!ReadFull(gz, buf, n)) break;
                    if (keep) ogz.Write(buf, 0, n);
                    left -= n;
                }
            }
            var zero = new byte[1024]; ogz.Write(zero, 0, zero.Length);
        }

        static string Str(byte[] b, int off, int len) { int end = off; while (end < off + len && b[end] != 0) end++; return Encoding.ASCII.GetString(b, off, end - off); }
        static bool ReadFull(Stream s, byte[] b, int n) { int got = 0; while (got < n) { int r = s.Read(b, got, n - got); if (r <= 0) return false; got += r; } return true; }
        static void Skip(Stream s, long n) { var tmp = new byte[1 << 16]; while (n > 0) { int r = s.Read(tmp, 0, (int)Math.Min(tmp.Length, n)); if (r <= 0) break; n -= r; } }
    }
}
#endif
