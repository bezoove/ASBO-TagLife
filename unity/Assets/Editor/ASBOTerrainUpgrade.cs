#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Swaps the generated terrain layers for the photographic ones from Terrain Textures Pack Free (imported Asset
    /// Store pack): mown lawn, weedy clover grass for the rougher patches, and stony dirt for the worn verges.
    /// The 8K sources are capped to 2048 (1024 on Android) so they stay light on phones.
    /// </summary>
    public static class ASBOTerrainUpgrade
    {
        const string Pack = "Assets/TerrainTexturesPackFree/TerrainTextures/";
        const string Gen = "Assets/Art/Generated/Terrain/";

        [MenuItem("ASBO/Art/Upgrade Terrain Layers")]
        public static void Run()
        {
            if (!Directory.Exists(Pack)) { Debug.LogWarning("[ASBO] Terrain Textures Pack Free not imported"); return; }
            Set("grass", "GrassUV01", 6f, new Color(0.95f, 1f, 0.92f, 1f));
            Set("meadow", "GrassUV02", 7f, new Color(0.92f, 0.98f, 0.88f, 1f));
            Set("earth", "GroundStones01", 3.5f, new Color(1f, 0.95f, 0.88f, 1f));
            // the billboard grass tufts follow the new lawn colour
            foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            {
                var td = t.terrainData; var dp = td.detailPrototypes;
                for (int i = 0; i < dp.Length; i++) { dp[i].healthyColor = new Color(0.36f, 0.45f, 0.2f); dp[i].dryColor = new Color(0.48f, 0.47f, 0.26f); }
                td.detailPrototypes = dp; EditorUtility.SetDirty(td);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[ASBO] terrain layers upgraded");
        }

        static void Set(string layerName, string tex, float tile, Color tint)
        {
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(Gen + layerName + ".terrainlayer");
            if (layer == null) { Debug.LogWarning("[ASBO] no terrain layer " + layerName); return; }
            layer.diffuseTexture = Tex(Pack + tex + ".png", false);
            var n = Tex(Pack + tex + "_N.png", true);
            if (n != null) { layer.normalMapTexture = n; layer.normalScale = 0.8f; }
            layer.tileSize = new Vector2(tile, tile);
            layer.diffuseRemapMin = Vector4.zero; layer.diffuseRemapMax = tint;
            layer.smoothness = 0.04f; layer.metallic = 0f;
            var so = new SerializedObject(layer); var sp = so.FindProperty("m_SmoothnessSource"); if (sp != null) { sp.intValue = 0; so.ApplyModifiedPropertiesWithoutUndo(); }
            EditorUtility.SetDirty(layer);
        }

        static Texture2D Tex(string path, bool normal)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (ti == null) return null;
            bool dirty = false;
            if (normal && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
            if (ti.maxTextureSize != 2048) { ti.maxTextureSize = 2048; dirty = true; }
            if (!ti.mipmapEnabled) { ti.mipmapEnabled = true; dirty = true; }
            var android = ti.GetPlatformTextureSettings("Android");
            if (!android.overridden || android.maxTextureSize != 1024)
            {
                android.overridden = true; android.maxTextureSize = 1024; android.format = TextureImporterFormat.ASTC_6x6;
                ti.SetPlatformTextureSettings(android); dirty = true;
            }
            if (dirty) ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
#endif
