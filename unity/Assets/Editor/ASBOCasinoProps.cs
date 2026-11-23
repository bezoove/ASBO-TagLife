#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using ASBO.Slice;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Brings in the modelled props (tools/art/casino: casino_props.fbx, the atlas maps and the two tables) and builds
    /// Resources/CasinoProps/PropLib.asset: each prop's mesh and what it is for, plus the two shared materials (the
    /// opaque atlas with normal, metal/smoothness and glow maps; glass). Run ASBO/Art/Build Casino Props, then rebuild
    /// the town so the casino picks them up.
    /// </summary>
    public static class ASBOCasinoProps
    {
        const string Src = "Assets/Art/CasinoProps", Res = "Assets/Resources/CasinoProps";

        [MenuItem("ASBO/Art/Build Casino Props")]
        public static void Build()
        {
            Directory.CreateDirectory(Res);
            Tex($"{Src}/casino_albedo.png", true, 4096, 4096, TextureImporterFormat.ASTC_8x8, false);
            Tex($"{Src}/casino_normal.png", false, 2048, 2048, TextureImporterFormat.ASTC_6x6, true);
            Tex($"{Src}/casino_mask.png", false, 2048, 1024, TextureImporterFormat.ASTC_6x6, false, true);
            Tex($"{Src}/casino_emit.png", true, 2048, 1024, TextureImporterFormat.ASTC_8x8, false);
            var alb = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Src}/casino_albedo.png");
            var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Src}/casino_normal.png");
            var msk = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Src}/casino_mask.png");
            var emi = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Src}/casino_emit.png");
            if (alb == null) { Debug.LogWarning("[ASBO] casino props: atlas missing in " + Src); return; }

            var op = Mat("CP_Opaque", m =>
            {
                m.SetTexture("_BaseMap", alb); m.SetColor("_BaseColor", Color.white);
                m.SetTexture("_BumpMap", nrm); m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_MetallicGlossMap", msk); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_Smoothness", 1f); m.SetFloat("_Metallic", 1f); m.SetFloat("_SmoothnessTextureChannel", 0f);
                m.SetTexture("_EmissionMap", emi); m.SetColor("_EmissionColor", Color.white * 1.8f); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            });
            var gl = Mat("CP_Glass", m =>
            {
                m.SetTexture("_BaseMap", alb); m.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.3f));
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 1f); m.SetFloat("_SrcBlend", 1f); m.SetFloat("_DstBlend", 10f); m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.EnableKeyword("_ALPHAPREMULTIPLY_ON"); m.renderQueue = 3000; m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_Smoothness", 0.95f); m.SetFloat("_Metallic", 0f); m.SetShaderPassEnabled("ShadowCaster", false);
            });

            string fbx = $"{Src}/casino_props.fbx";
            var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
            if (mi == null) { Debug.LogWarning("[ASBO] casino props: no " + fbx); return; }
            mi.importCameras = false; mi.importLights = false; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None;
            mi.importBlendShapes = false; mi.importNormals = ModelImporterNormals.Import; mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.meshCompression = ModelImporterMeshCompression.Off; mi.isReadable = true; mi.globalScale = 1f; mi.useFileScale = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "CP_Opaque"), op);
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "CP_Glass"), gl);
            mi.SaveAndReimport();
            var meshes = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Mesh>().GroupBy(x => x.name).ToDictionary(g => g.Key, g => g.First());

            string libPath = $"{Res}/PropLib.asset";
            var lib = AssetDatabase.LoadAssetAtPath<PropLib>(libPath);
            if (lib == null) { lib = ScriptableObject.CreateInstance<PropLib>(); AssetDatabase.CreateAsset(lib, libPath); }
            lib.entries.Clear(); lib.opaque = op; lib.glass = gl;
            float F(string s) => string.IsNullOrEmpty(s) ? 0f : float.Parse(s, CultureInfo.InvariantCulture);
            int missing = 0;
            foreach (var line in File.ReadAllLines($"{Src}/casino_props.txt"))
            {
                var f = line.Split('\t'); if (f.Length < 20) continue;
                if (!meshes.TryGetValue(f[0], out var mesh)) { missing++; continue; }
                var e = new PropLib.Entry
                {
                    id = f[0], mesh = mesh, kind = f[1] switch { "pickup" => PropLib.Kind.Pickup, "sit" => PropLib.Kind.Sit, "use" => PropLib.Kind.Use, "door" => PropLib.Kind.Door, _ => PropLib.Kind.Static },
                    mass = F(f[2]), label = f[3], action = f[4], seat = F(f[5]), cash = F(f[6]), drink = f[7], food = f[8], game = string.IsNullOrEmpty(f[9]) ? -1 : (int)F(f[9]),
                    code = f[10], hinge = F(f[11]), size = new Vector3(F(f[13]), F(f[14]), F(f[15])), center = new Vector3(F(f[16]), F(f[17]), F(f[18])), tris = (int)F(f[19]),
                };
                lib.entries.Add(e);
            }
            File.Copy($"{Src}/casino_regions.txt", $"{Res}/casino_regions.txt", true);
            EditorUtility.SetDirty(lib); AssetDatabase.SaveAssets(); AssetDatabase.ImportAsset($"{Res}/casino_regions.txt");
            PropLib.Flush();
            // check the axes came across: the importer's bounds against the size and centre the forge measured
            foreach (var id in new[] { "till", "slot0", "bar_stool", "door" })
            {
                var e = lib.entries.FirstOrDefault(x => x.id == id); if (e == null) continue;
                Debug.Log($"[ASBO] casino props: {id} mesh centre {e.mesh.bounds.center:F3} size {e.mesh.bounds.size:F3} | forge centre {e.center:F3} size {e.size:F3}");
            }
            Debug.Log($"[ASBO] casino props: {lib.entries.Count} props in the library" + (missing > 0 ? $", {missing} without a mesh" : ""));
        }

        static void Tex(string path, bool srgb, int max, int android, TextureImporterFormat fmt, bool normal, bool alpha = false)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path); if (imp == null) return;
            imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default; imp.sRGBTexture = srgb && !normal;
            imp.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None; imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Clamp; imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = 4; imp.maxTextureSize = max;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = android, format = fmt, compressionQuality = 60 });
            imp.SaveAndReimport();
        }

        static Material Mat(string name, System.Action<Material> set)
        {
            string path = $"{Res}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Gfx.Lit) { name = name }; AssetDatabase.CreateAsset(m, path); }
            if (m.shader != Gfx.Lit) m.shader = Gfx.Lit;
            set(m); m.enableInstancing = false; EditorUtility.SetDirty(m);
            return m;
        }
    }
}
#endif
