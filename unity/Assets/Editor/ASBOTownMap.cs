#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>
    /// A top-down map of the built town (Shots/townmap.png): rays straight down every metre, coloured by what they hit
    /// (ground, road, low walls and fences, buildings shaded by height, water), with a 50 m grid and its coordinates.
    /// For finding the empty plots to fill. Run: call ASBO.EditorTools.ASBOTownMap.Run
    /// </summary>
    public static class ASBOTownMap
    {
        public static void Run()
        {
            const int x0 = -330, x1 = 330, z0 = -230, z1 = 230, S = 2;   // 2 px per metre
            int W = (x1 - x0) * S, H = (z1 - z0) * S;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            var px = new Color32[W * H];
            for (int j = 0; j < H; j++)
                for (int i = 0; i < W; i++)
                {
                    float x = x0 + (i + 0.5f) / S, z = z0 + (j + 0.5f) / S;
                    Color32 c = new Color32(200, 214, 170, 255);
                    if (Physics.Raycast(new Vector3(x, 80f, z), Vector3.down, out var hit, 200f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        float y = hit.point.y; string n = hit.collider.name.ToLower() + " " + (hit.collider.transform.parent != null ? hit.collider.transform.parent.name.ToLower() : "");
                        if (y < -0.4f) c = new Color32(80, 130, 190, 255);
                        else if (n.Contains("road") || n.Contains("street") || n.Contains("junction") || n.Contains("carpark") || n.Contains("tarmac")) c = new Color32(110, 110, 116, 255);
                        else if (y < 0.25f) c = n.Contains("pave") || n.Contains("path") ? new Color32(176, 176, 170, 255) : new Color32(196, 212, 164, 255);
                        else if (y < 1.6f) c = new Color32(170, 140, 110, 255);
                        else { byte v = (byte)Mathf.Clamp(150 - y * 5f, 30, 150); c = new Color32(v, (byte)(v * 0.9f), (byte)(v * 0.85f), 255); }
                    }
                    if (((int)x) % 50 == 0 && Mathf.Abs(x - Mathf.Round(x)) < 0.3f || ((int)z) % 50 == 0 && Mathf.Abs(z - Mathf.Round(z)) < 0.3f) c = new Color32(255, 0, 0, 255);
                    px[j * W + i] = c;
                }
            tex.SetPixels32(px); tex.Apply();
            string dir = Path.GetFullPath(Application.dataPath + "/../Shots"); Directory.CreateDirectory(dir);
            File.WriteAllBytes(dir + "/townmap.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log($"[ASBO] town map: {W}x{H} px, x {x0}..{x1}, z {z0}..{z1} (z up the image is +z), grid lines every 50 m");
        }
    }
}
#endif
