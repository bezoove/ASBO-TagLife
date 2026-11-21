#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ASBO.Slice;
using UnityEditor;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Picks the sounds the game uses out of the imported packs, sets phone-friendly import settings on them
    /// (mono Vorbis for 3D one-shots, streamed stereo for ambience) and writes Assets/Resources/SfxBank.asset.
    /// Only the clips referenced here end up in a build.
    /// </summary>
    public static class ASBOAudioBank
    {
        enum Kind { Shot, Loop, Ambience }
        const string L441 = "Assets/44.1 General Library (Free Sample Pack)/";
        const string SB = "Assets/Libraries/Soundbits_freeSFX_2025/Sounds/";
        const string CF = "Assets/Classic Footstep SFX/";
        const string DK = "Assets/Deadly Kombat Free version/";

        static readonly (string key, Kind kind, string[] paths)[] Map =
        {
            ("step0", Kind.Shot, new[] { L441 + "Volume I/Footsteps_Concrete_Single_03.wav", L441 + "Volume I/Footsteps_Stone_Single_15.wav", CF + "Floor/Floor_step?.wav" }),
            ("step1", Kind.Shot, new[] { CF + "Forest ground/Forest_ground_step*.wav", L441 + "Volume I/Footsteps_Grass_Single_04.wav", L441 + "Volume I/Footsteps_Leaves_Dry_Single_03.wav" }),
            ("step2", Kind.Shot, new[] { CF + "Rocky_ground/Rocky_ground_step*.wav", CF + "Ground/Ground_Step*.wav" }),
            ("step3", Kind.Shot, new[] { CF + "Floor/Floor_step1?.wav", CF + "Floor/Floor_step2?.wav" }),
            ("land0", Kind.Shot, new[] { CF + "Rocky_ground/Rocky_ground_jump*.wav" }),
            ("land1", Kind.Shot, new[] { CF + "Forest ground/Forest_ground_jump*.wav" }),
            ("engine", Kind.Loop, new[] { L441 + "Volume I/Car_Grand-Scenic_Engine_Idle_Exterior_01_LOOP.wav" }),
            ("crashMetal", Kind.Shot, new[] { SB + "cs-d_metal_crash_designed_011.wav", SB + "cs-d_mixed_crash_designed_013.wav" }),
            ("crashGlass", Kind.Shot, new[] { SB + "cs-d_glass_crash_designed_013.wav", SB + "cs-d_glass_crash_designed_037.wav", L441 + "Volume II/Smashing_Glass_Bottle_01.wav" }),
            ("bone", Kind.Shot, new[] { SB + "jmg_bone-break_snap_027.wav" }),
            ("painM", Kind.Shot, new[] { SB + "ss2-h_male_grunt-shout_048.wav", SB + "ss2-h_male_grunt-shout_057.wav", SB + "ss2-h_male_grunt-shout_079.wav", SB + "ss2-h_male_scream_032.wav" }),
            ("painF", Kind.Shot, new[] { SB + "ss2-h_female_shout-of-pain_014.wav", SB + "ss2-h_female_shout-of-pain_028.wav", SB + "ss2-h_female_shout-of-pain_154.wav", SB + "ss2-h_female_scream_070.wav" }),
            ("dog", Kind.Shot, new[] { SB + "ca_large-dog_barking_05.wav", SB + "ca_group-of-dogs_barking_01.wav" }),
            ("crow", Kind.Shot, new[] { SB + "ca_bird_crow_02.wav", SB + "ca_bird_magpie_01.wav" }),
            ("ambTown", Kind.Ambience, new[] { L441 + "Volume I/Ambience_BRT_Morning_C01_Town_LOOP.wav" }),
            ("moped", Kind.Loop, new[] { SB + "sctr_driving-loop_02_m_48khz.wav" }),
            // fighting (Deadly Kombat sound pack)
            ("whooshPunch", Kind.Shot, new[] { DK + "punch_short_whoosh*.wav", DK + "punch_long_whoosh*.wav" }),
            ("whooshKick", Kind.Shot, new[] { DK + "kick_short_whoosh*.wav", DK + "kick_long_whoosh*.wav" }),
            ("hitFace", Kind.Shot, new[] { DK + "face_hit_small*.wav", DK + "face_hit_Large*.wav" }),
            ("hitBody", Kind.Shot, new[] { DK + "body_hit_small*.wav", DK + "body_hit_large*.wav" }),
            ("hitKO", Kind.Shot, new[] { DK + "face_hit_finisher*.wav", DK + "body_hit_finisher*.wav" }),
        };

        [MenuItem("ASBO/Audio/Build Sound Bank")]
        public static void Build()
        {
            var bank = ScriptableObject.CreateInstance<SfxBank>();
            int total = 0;
            foreach (var (key, kind, paths) in Map)
            {
                var clips = new List<AudioClip>();
                foreach (var p in paths)
                    foreach (var f in Resolve(p))
                    {
                        Configure(f, kind);
                        var c = AssetDatabase.LoadAssetAtPath<AudioClip>(f);
                        if (c != null && !clips.Contains(c)) clips.Add(c);
                    }
                if (clips.Count == 0) { Debug.LogWarning("[ASBO] sound bank: nothing for '" + key + "'"); continue; }
                bank.entries.Add(new SfxBank.Entry { key = key, clips = clips.ToArray() });
                total += clips.Count;
            }
            Directory.CreateDirectory("Assets/Resources");
            const string dst = "Assets/Resources/SfxBank.asset";
            if (AssetDatabase.LoadAssetAtPath<SfxBank>(dst) != null) AssetDatabase.DeleteAsset(dst);
            AssetDatabase.CreateAsset(bank, dst); AssetDatabase.SaveAssets();
            Debug.Log($"[ASBO] sound bank: {bank.entries.Count} keys, {total} clips -> {dst}");
        }

        static IEnumerable<string> Resolve(string p)
        {
            if (!p.Contains("*") && !p.Contains("?")) { if (File.Exists(p)) yield return p; yield break; }
            string dir = Path.GetDirectoryName(p).Replace('\\', '/'), pat = Path.GetFileName(p);
            if (!Directory.Exists(dir)) yield break;
            foreach (var f in Directory.GetFiles(dir, pat).Where(f => !f.EndsWith(".meta")).OrderBy(f => f)) yield return f.Replace('\\', '/');
        }

        static void Configure(string path, Kind kind)
        {
            var ai = AssetImporter.GetAtPath(path) as AudioImporter; if (ai == null) return;
            var s = ai.defaultSampleSettings;
            bool amb = kind == Kind.Ambience;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = amb ? 0.35f : 0.5f;
            s.loadType = amb ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate; s.sampleRateOverride = amb ? 32000u : 44100u;
            s.preloadAudioData = !amb;
            bool changed = ai.forceToMono != !amb || ai.defaultSampleSettings.loadType != s.loadType || ai.defaultSampleSettings.compressionFormat != s.compressionFormat
                || ai.defaultSampleSettings.sampleRateOverride != s.sampleRateOverride || Mathf.Abs(ai.defaultSampleSettings.quality - s.quality) > 0.01f;
            if (!changed) return;
            ai.forceToMono = !amb; ai.loadInBackground = amb;
            ai.defaultSampleSettings = s;
            ai.SaveAndReimport();
        }
    }
}
#endif
