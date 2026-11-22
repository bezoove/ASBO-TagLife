#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>
    /// A look at what a Mixamo take actually does: the X Bot posed at a few moments of a file's take, photographed from
    /// the side and the front, into one strip (Shots/clip_NAME.png). For choosing poses and cut points without guessing.
    /// </summary>
    public static class ASBOClipShot
    {
        /// <summary>The pose of a take in numbers (hip and knee bend, arms, root height) at a few moments: is it sitting?</summary>
        public static void Pose(string file, string times)
        {
            file = file.Replace('_', ' ');
            var path = ASBOMixamo.F(file);
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).OrderByDescending(c => c.length).FirstOrDefault();
            if (clip == null) { Debug.LogWarning("[ASBO] pose: nothing at " + path); return; }
            var want = new[] { "RootT.y", "Left Upper Leg Front-Back", "Left Lower Leg Stretch", "Spine Front-Back", "Left Arm Down-Up", "Left Arm Front-Back", "Left Forearm Stretch", "Right Arm Front-Back" };
            var binds = AnimationUtility.GetCurveBindings(clip);
            var sb = new System.Text.StringBuilder($"[ASBO] pose {file} ({clip.length:F2}s):");
            foreach (var ts in times.Split(','))
            {
                float t = float.Parse(ts, System.Globalization.CultureInfo.InvariantCulture); sb.Append($"\n  t={t:F1}:");
                foreach (var w in want) { var bd = binds.FirstOrDefault(x => x.propertyName == w); var cv = bd.propertyName == w ? AnimationUtility.GetEditorCurve(clip, bd) : null; sb.Append($" {w.Replace("Left ", "L").Replace("Right ", "R")}={(cv != null ? cv.Evaluate(t).ToString("F2") : "-")}"); }
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>file: the take's name ("Honking Horn"); times: comma-separated seconds ("0,0.5,1").</summary>
        public static void Shot(string file, string times)
        {
            file = file.Replace('_', ' '); // the job runner splits on spaces
            var path = ASBOMixamo.F(file);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).OrderByDescending(c => c.length).FirstOrDefault();
            if (model == null || clip == null) { Debug.LogWarning("[ASBO] clip shot: nothing at " + path); return; }
            var ts = times.Split(',').Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            const int W = 360, H = 420;
            var strip = new Texture2D(W * ts.Length, H * 2, TextureFormat.RGB24, false);
            // its own little world, so the town isn't in the picture
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var go = Object.Instantiate(model); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            var cg = new GameObject("ClipShotCam"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg, scene); var cam = cg.AddComponent<Camera>(); cam.scene = scene;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.6f, 0.66f); cam.fieldOfView = 34f; cam.nearClipPlane = 0.05f;
            var lg = new GameObject("ClipShotLight"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lg, scene); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.2f; lg.transform.rotation = Quaternion.Euler(40, 30, 0);
            var rt = new RenderTexture(W, H, 24);
            AnimationMode.StartAnimationMode();
            try
            {
                for (int i = 0; i < ts.Length; i++)
                {
                    AnimationMode.BeginSampling(); AnimationMode.SampleAnimationClip(go, clip, Mathf.Min(ts[i], clip.length)); AnimationMode.EndSampling();
                    var hips = go.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.EndsWith("Hips"));
                    Vector3 c = hips != null ? hips.position : go.transform.position + Vector3.up;
                    for (int v = 0; v < 2; v++)
                    {
                        Vector3 dir = v == 0 ? Vector3.right : Vector3.forward;
                        cam.transform.position = c + dir * 3.4f + Vector3.up * 0.2f; cam.transform.LookAt(c + Vector3.up * 0.05f);
                        cam.targetTexture = rt; cam.Render();
                        RenderTexture.active = rt; strip.ReadPixels(new Rect(0, 0, W, H), i * W, (1 - v) * H); RenderTexture.active = null;
                    }
                }
            }
            finally { AnimationMode.StopAnimationMode(); cam.targetTexture = null; Object.DestroyImmediate(rt); UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
            strip.Apply();
            string dir2 = Path.GetFullPath(Application.dataPath + "/../Shots"); Directory.CreateDirectory(dir2);
            File.WriteAllBytes(dir2 + "/clip_" + file.Replace(' ', '_') + ".png", strip.EncodeToPNG());
            Debug.Log($"[ASBO] clip shot {file}: {clip.name} {clip.length:F2}s at {times}");
        }
    }
}
#endif
