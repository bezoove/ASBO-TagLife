using UnityEditor;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>Photos dropped into Assets/Resources/PhotoProps are set up for use as props (readable for colour sampling, sensible size, no sRGB surprises).</summary>
    public class ASBOPhotoProps : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/PhotoProps/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default; ti.isReadable = true; ti.maxTextureSize = 512; ti.mipmapEnabled = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None;
        }

        [MenuItem("ASBO/Props/Make photo props folder")]
        public static void MakeFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/PhotoProps")) AssetDatabase.CreateFolder("Assets/Resources", "PhotoProps");
            EditorUtility.RevealInFinder("Assets/Resources/PhotoProps");
            Debug.Log("[ASBO] Drop photos in Assets/Resources/PhotoProps named name_WxHxD in cm, e.g. kettle_22x24x16.jpg");
        }
    }
}
