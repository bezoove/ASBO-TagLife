#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>
    /// The Mixamo moves (Assets/Mixamo/XBot, downloaded with your Adobe ID; never committed to git): imported as
    /// Humanoid so they play on our UMA people, analysed by measuring the body through each take, and cut into the
    /// pieces the game uses: a walk cycle, a run cycle (from the run before it stops), a fists-up fighting stance (the
    /// ready pose held before and after the body block), an uppercut for the hold-to-punch, and more.
    /// </summary>
    public static class ASBOMixamo
    {
        public const string Dir = "Assets/Mixamo/XBot";
        static string F(string n) => $"{Dir}/X Bot@{n}.fbx";

        // which clips loop, and what each file is cut into (name, from s, to s, loop); null range = whole take
        static readonly Dictionary<string, (string name, float a, float b, bool loop)[]> Cuts = new Dictionary<string, (string, float, float, bool)[]>
        {
            ["Walking"] = new[] { ("MX_Walk", -1f, -1f, true) },
            ["Standing"] = new[] { ("MX_Stand", -1f, -1f, true) },
            ["Crouching Idle"] = new[] { ("MX_CrouchIdle", -1f, -1f, true) },
            ["Sneaking Forward"] = new[] { ("MX_Sneak", -1f, -1f, true) },
            ["Crouched Sneaking Right"] = new[] { ("MX_SneakRight", -1f, -1f, true) },
            ["Walk Backwards"] = new[] { ("MX_WalkBack", -1f, -1f, true) },
            ["Uppercut"] = new[] { ("MX_Uppercut", -1f, -1f, false) },
            ["Dying"] = new[] { ("MX_Dying", -1f, -1f, false) },
            ["Falling Down"] = new[] { ("MX_FallDown", -1f, -1f, false) },
            ["Zombie Stand Up"] = new[] { ("MX_StandUp", -1f, -1f, false) },
            ["Evading A Threat"] = new[] { ("MX_Evade", -1f, -1f, false) },
            ["Pulled From Seat"] = new[] { ("MX_PulledOut", -1f, -1f, false) },
            ["Jumping Down"] = new[] { ("MX_JumpDown", -1f, -1f, false) },
            ["Climbing Down"] = new[] { ("MX_ClimbDown", -1f, -1f, false) },
            ["Freehang Climb"] = new[] { ("MX_Climb", -1f, -1f, false) },
            ["Idle To Sprint"] = new[] { ("MX_IdleToSprint", -1f, -1f, false) },
            ["Run To Stop"] = new[] { ("MX_RunToStop", -1f, -1f, false) },
            ["Body Block"] = new[] { ("MX_Block", -1f, -1f, false) },
            ["Fight Idle To Action Idle"] = new[] { ("MX_FightIdle", -1f, -1f, false) },
        };

        [MenuItem("ASBO/Mixamo/1 Import and Analyse")]
        public static void ImportAndAnalyse()
        {
            int n = 0;
            foreach (var path in Directory.GetFiles(Dir, "*.fbx").Select(p => p.Replace('\\', '/')))
            {
                var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                bool dirty = mi.animationType != ModelImporterAnimationType.Human || mi.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel || mi.materialImportMode != ModelImporterMaterialImportMode.None;
                mi.animationType = ModelImporterAnimationType.Human; mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.importCameras = false; mi.importLights = false;
                mi.resampleCurves = true; mi.animationCompression = ModelImporterAnimationCompression.Optimal;
                if (dirty) { mi.SaveAndReimport(); n++; }
            }
            Debug.Log($"[ASBO] mixamo: {n} files set to Humanoid");
            foreach (var path in Directory.GetFiles(Dir, "*.fbx").Select(p => p.Replace('\\', '/')))
            {
                var c = Raw(path); if (c == null) { Debug.LogWarning("[ASBO] mixamo: no clip in " + path); continue; }
                var (speed, dz) = RootSpeed(c);
                Debug.Log($"[ASBO] mixamo {Path.GetFileNameWithoutExtension(path)}: {c.length:F2}s {c.frameRate:F0}fps root {speed:F2} m/s (moves {dz:F2} m)");
            }
            Loops("Run To Stop", 0.45f, 0.95f);
            Loops("Idle To Sprint", 0.45f, 0.95f);
            Loops("Walking", 0.8f, 1.4f);
            Stance("Body Block"); Stance("Fight Idle To Action Idle");
        }

        static AnimationClip Raw(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        static AnimationCurve Curve(AnimationClip c, string prop)
        {
            var b = AnimationUtility.GetCurveBindings(c).FirstOrDefault(x => x.propertyName == prop);
            return b.propertyName == prop ? AnimationUtility.GetEditorCurve(c, b) : null;
        }

        static (float speed, float dist) RootSpeed(AnimationClip c)
        {
            var x = Curve(c, "RootT.x"); var z = Curve(c, "RootT.z");
            if (x == null || z == null) return (0, 0);
            float d = new Vector2(x.Evaluate(c.length) - x.Evaluate(0), z.Evaluate(c.length) - z.Evaluate(0)).magnitude;
            return (d / Mathf.Max(0.01f, c.length), d);
        }

        static string[] Muscles(AnimationClip c, params string[] parts) =>
            AnimationUtility.GetCurveBindings(c).Select(b => b.propertyName).Where(p => parts.Any(q => p.Contains(q)) && !p.StartsWith("Root")).Distinct().ToArray();

        /// <summary>Find the best loop in a take: two moments one stride apart where the legs are in the same place.</summary>
        static void Loops(string file, float tMin, float tMax)
        {
            var c = Raw(F(file)); if (c == null) return;
            var names = Muscles(c, "Leg", "Foot", "Toes", "Arm", "Forearm", "Spine", "Chest");
            var curves = names.Select(m => Curve(c, m)).Where(k => k != null).ToArray();
            var z = Curve(c, "RootT.z"); var x = Curve(c, "RootT.x");
            float dt = 1f / 30f; var best = new List<(float cost, float a, float b, float v)>();
            for (float a = 0; a < c.length - tMin; a += dt)
                for (float T = tMin; T <= tMax && a + T <= c.length; T += dt)
                {
                    float cost = 0; foreach (var k in curves) cost += Mathf.Abs(k.Evaluate(a) - k.Evaluate(a + T));
                    float v = z != null ? new Vector2(x.Evaluate(a + T) - x.Evaluate(a), z.Evaluate(a + T) - z.Evaluate(a)).magnitude / T : 0;
                    best.Add((cost / curves.Length, a, a + T, v));
                }
            foreach (var e in best.OrderBy(e => e.cost - e.v * 0.02f).Take(6))
                Debug.Log($"[ASBO] mixamo loop {file}: {e.a:F2}-{e.b:F2}s (frames {e.a * 30:F0}-{e.b * 30:F0}) pose diff {e.cost:F4} speed {e.v:F2} m/s");
        }

        /// <summary>Where the fighter holds the fists-up ready pose: the stillest stretch with the arms raised.</summary>
        static void Stance(string file)
        {
            var c = Raw(F(file)); if (c == null) return;
            var names = Muscles(c, "Arm", "Forearm", "Spine", "Chest", "Neck", "Head", "Hand");
            var curves = names.Select(m => Curve(c, m)).Where(k => k != null).ToArray();
            var lift = Curve(c, "Left Arm Down-Up"); var bend = Curve(c, "Left Forearm Stretch");
            float dt = 1f / 30f; var sb = new System.Text.StringBuilder();
            for (float t = 0; t <= c.length; t += 0.1f)
            {
                float vel = 0; foreach (var k in curves) vel += Mathf.Abs(k.Evaluate(Mathf.Min(c.length, t + dt)) - k.Evaluate(t));
                sb.Append($"{t:F1}:{vel / curves.Length / dt:F2}/{(lift != null ? lift.Evaluate(t) : 0):F2}/{(bend != null ? bend.Evaluate(t) : 0):F2} ");
            }
            Debug.Log($"[ASBO] mixamo stance {file} ({c.length:F2}s) t:motion/armUp/elbow {sb}");
        }

        /// <summary>When a punch lands: the moment the punching arm is fastest and then stops (logged per 50 ms).</summary>
        static void Strike(string file)
        {
            var c = Raw(F(file)); if (c == null) return;
            var names = Muscles(c, "Right Arm", "Right Forearm", "Right Shoulder", "Left Arm", "Left Forearm");
            var curves = names.Select(m => Curve(c, m)).Where(k => k != null).ToArray();
            var sb = new System.Text.StringBuilder(); float dt = 0.05f;
            for (float t = 0; t + dt <= c.length; t += dt)
            {
                float vel = 0; foreach (var k in curves) vel += Mathf.Abs(k.Evaluate(t + dt) - k.Evaluate(t));
                sb.Append($"{t:F2}:{vel / dt:F1} ");
            }
            Debug.Log($"[ASBO] mixamo strike {file} ({c.length:F2}s) arm speed {sb}");
        }

        [MenuItem("ASBO/Mixamo/2 Cut Clips")]
        public static void CutAll()
        {
            Cut("Walking", "MX_Walk", 0f, 1.0f, true);
            Cut("Body Block", "MX_Stance", 2.1f, 3.35f, true);
            Cut("Uppercut", "MX_Uppercut", -1f, -1f, false);
            Cut("Evading A Threat", "MX_Evade", -1f, -1f, false, false);
            Cut("Falling Down", "MX_FallDown", -1f, -1f, false, false);
            Cut("Zombie Stand Up", "MX_StandUp", -1f, -1f, false);
            Cut("Walk Backwards", "MX_WalkBack", -1f, -1f, true);
            Strike("Uppercut");
            foreach (var n in new[] { "MX_Walk", "MX_Stance", "MX_Uppercut" }) { var c = ClipNamed(n); Debug.Log($"[ASBO] mixamo cut {n}: {(c != null ? c.length.ToString("F2") + "s loop " + c.isLooping : "missing")}"); }
        }

        /// <summary>Cut the takes into the named clips (ranges in seconds; loops get Unity's loop-pose match).</summary>
        public static void Cut(string file, string clip, float a, float b, bool loop, bool bakeXZ = true)
        {
            var path = F(file); var mi = (ModelImporter)AssetImporter.GetAtPath(path);
            var src = mi.defaultClipAnimations[0];
            float fps = 30f; var raw = Raw(path); if (raw != null) fps = raw.frameRate;
            var list = mi.clipAnimations.Where(x => x.name != clip).ToList();
            list.Add(new ModelImporterClipAnimation
            {
                name = clip, takeName = src.takeName, firstFrame = a < 0 ? src.firstFrame : a * fps, lastFrame = b < 0 ? src.lastFrame : b * fps,
                loopTime = loop, loopPose = loop, lockRootRotation = true, keepOriginalOrientation = true,
                lockRootHeightY = true, keepOriginalPositionY = true, lockRootPositionXZ = bakeXZ, keepOriginalPositionXZ = true
            });
            mi.clipAnimations = list.ToArray(); mi.SaveAndReimport();
        }

        public static AnimationClip ClipNamed(string name)
        {
            foreach (var path in Directory.GetFiles(Dir, "*.fbx"))
            {
                var c = AssetDatabase.LoadAllAssetsAtPath(path.Replace('\\', '/')).OfType<AnimationClip>().FirstOrDefault(x => x.name == name);
                if (c != null) return c;
            }
            return null;
        }
    }
}
#endif
