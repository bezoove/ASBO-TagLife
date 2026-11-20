#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>Free-pack test: UMA 3 characters (real clothes), Kevin Iglesias Human Basic Motions, RCC Lite car.</summary>
    public static class ASBOFreeTest
    {
        const string KevBase = "Assets/Kevin Iglesias/Human Animations/Animations/";
        const string Out = "Assets/Resources/Anim/";

        [MenuItem("ASBO/Test/1 Install UMA URP Support")]
        public static void InstallUmaUrp()
        {
            var t = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UMA.Editors.WelcomeToUMA") ?? a.GetType("UMA.WelcomeToUMA") ?? a.GetTypes().FirstOrDefault(x => x.Name == "WelcomeToUMA")).FirstOrDefault(x => x != null);
            if (t == null) { Debug.LogError("[ASBO] WelcomeToUMA not found"); return; }
            var srpEnum = t.GetNestedType("SrpSupport", BindingFlags.NonPublic | BindingFlags.Public);
            var urp = Enum.Parse(srpEnum, "Urp");
            var getPath = t.GetMethod("GetBundledArchiveAbsolutePath", BindingFlags.NonPublic | BindingFlags.Static);
            var begin = t.GetMethod("BeginSrpPackageImport", BindingFlags.NonPublic | BindingFlags.Static);
            string path = (string)getPath.Invoke(null, new[] { urp });
            Debug.Log("[ASBO] Installing UMA URP support from " + path);
            begin.Invoke(null, new object[] { urp, path });
        }

        static AnimationClip Clip(string rel)
        {
            var path = KevBase + rel + ".fbx";
            var c = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(x => !x.name.StartsWith("__preview__"));
            if (c == null) Debug.LogWarning("[ASBO] missing clip " + path);
            return c;
        }

        [System.Serializable] class Speeds { public float walk, run, sprint, turnSecs; }

        [MenuItem("ASBO/Test/2 Build Locomotion Controllers")]
        public static void BuildControllers() { Build("M", "Male"); Build("F", "Female"); AssetDatabase.SaveAssets(); }

        static void Build(string s, string folder)
        {
            string p = folder + "/", h = "Human" + s + "@";
            string ctrlPath = Out + "Loco" + s + ".controller";
            Directory.CreateDirectory(Out);
            if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            foreach (var n in new[] { "MoveX", "MoveZ" }) ac.AddParameter(n, AnimatorControllerParameterType.Float);
            foreach (var n in new[] { "Talk", "Kneel", "Grounded" }) ac.AddParameter(n, AnimatorControllerParameterType.Bool);
            foreach (var n in new[] { "Jump", "TurnL", "TurnR" }) ac.AddParameter(n, AnimatorControllerParameterType.Trigger);
            var sm = ac.layers[0].stateMachine;

            var move = ac.CreateBlendTreeInController("Move", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.FreeformDirectional2D; tree.blendParameter = "MoveX"; tree.blendParameterY = "MoveZ";
            var idle = Clip(p + "Idles/" + h + "Idle01");
            tree.AddChild(idle, Vector2.zero);
            var sp = new Speeds();
            void Dirs(string gait, string[] dirs)
            {
                foreach (var d in dirs)
                {
                    var c = Clip($"{p}Movement/{gait}/RootMotion/{h}{gait}01_{d} [RM]");
                    if (c == null) continue;
                    var v = c.averageSpeed; var pos = new Vector2(v.x, v.z);
                    if (pos.magnitude < 0.05f) continue;
                    tree.AddChild(c, pos);
                    if (d == "Forward") { if (gait == "Walk") sp.walk = pos.y; else if (gait == "Run") sp.run = pos.y; else sp.sprint = pos.y; }
                }
            }
            var eight = new[] { "Forward", "ForwardLeft", "ForwardRight", "Left", "Right", "Backward", "BackwardLeft", "BackwardRight" };
            Dirs("Walk", eight); Dirs("Run", eight); Dirs("Sprint", new[] { "Forward", "ForwardLeft", "ForwardRight", "Left", "Right" });
            sm.defaultState = move;

            AnimatorState St(string name, AnimationClip c, Vector3 pos) { var st = sm.AddState(name, pos); st.motion = c; return st; }
            var turnL = St("TurnL", Clip($"{p}Movement/Turn/{h}Turn01_Left"), new Vector3(300, -120));
            var turnR = St("TurnR", Clip($"{p}Movement/Turn/{h}Turn01_Right"), new Vector3(300, -60));
            sp.turnSecs = turnL.motion != null ? ((AnimationClip)turnL.motion).length : 0.8f;
            foreach (var (st, trig) in new[] { (turnL, "TurnL"), (turnR, "TurnR") })
            {
                var tin = move.AddTransition(st); tin.AddCondition(AnimatorConditionMode.If, 0, trig); tin.hasExitTime = false; tin.duration = 0.12f;
                var tout = st.AddTransition(move); tout.hasExitTime = true; tout.exitTime = 0.85f; tout.duration = 0.2f;
            }
            var jb = St("JumpBegin", Clip($"{p}Movement/Jump/{h}Jump01 - Begin"), new Vector3(300, 40));
            var fall = St("Fall", Clip($"{p}Movement/Jump/{h}Fall01"), new Vector3(500, 40));
            var land = St("Land", Clip($"{p}Movement/Jump/{h}Jump01 - Land"), new Vector3(700, 40));
            var tj = sm.AddAnyStateTransition(jb); tj.AddCondition(AnimatorConditionMode.If, 0, "Jump"); tj.hasExitTime = false; tj.duration = 0.08f; tj.canTransitionToSelf = false;
            var t1 = jb.AddTransition(fall); t1.hasExitTime = true; t1.exitTime = 0.9f; t1.duration = 0.1f;
            var t1b = jb.AddTransition(land); t1b.AddCondition(AnimatorConditionMode.If, 0, "Grounded"); t1b.hasExitTime = true; t1b.exitTime = 0.6f; t1b.duration = 0.08f;
            var t2 = fall.AddTransition(land); t2.AddCondition(AnimatorConditionMode.If, 0, "Grounded"); t2.hasExitTime = false; t2.duration = 0.08f;
            var t3 = land.AddTransition(move); t3.hasExitTime = true; t3.exitTime = 0.55f; t3.duration = 0.2f;
            var talk = St("Talk", Clip($"{p}Social/Conversation/{h}Talk01"), new Vector3(300, 140));
            var ti = move.AddTransition(talk); ti.AddCondition(AnimatorConditionMode.If, 0, "Talk"); ti.hasExitTime = false; ti.duration = 0.3f;
            var to = talk.AddTransition(move); to.AddCondition(AnimatorConditionMode.IfNot, 0, "Talk"); to.hasExitTime = false; to.duration = 0.3f;
            EditorUtility.SetDirty(ac);
            File.WriteAllText(Out + "LocoSpeeds" + s + ".json", JsonUtility.ToJson(sp));
            AssetDatabase.ImportAsset(Out + "LocoSpeeds" + s + ".json");
            Debug.Log($"[ASBO] Loco{s}: {tree.children.Length} clips, walk {sp.walk:F2} run {sp.run:F2} sprint {sp.sprint:F2} m/s, turn {sp.turnSecs:F2}s");
        }

        /// <summary>Re-attach marker components (after moving them into their own files) and strip missing scripts.</summary>
        [MenuItem("ASBO/Test/4 Fix Scene Markers")]
        public static void FixMarkers()
        {
            foreach (var path in new[] { "Assets/Scenes/AbbotsWend.unity", "Assets/Scenes/AbbotsWend_Test.unity" })
            {
                if (!File.Exists(path)) continue;
                var sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int removed = 0;
                foreach (var root in sc.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                foreach (var root in sc.GetRootGameObjects())
                {
                    if (root.name == "World" && root.GetComponent<ASBO.Slice.BakedWorld>() == null) root.AddComponent<ASBO.Slice.BakedWorld>();
                    if (root.name == "FreePackTest" && root.GetComponent<ASBO.Slice.FreePackTest>() == null) root.AddComponent<ASBO.Slice.FreePackTest>();
                    if (root.name == "RCC_Car" && root.GetComponent<ASBO.Slice.RccVehicle>() == null) root.AddComponent<ASBO.Slice.RccVehicle>();
                }
                foreach (var root in sc.GetRootGameObjects())
                    foreach (var cref in root.GetComponentsInChildren<ASBO.Slice.CottageRef>(true))
                    {
                        var lights = new List<Light>();
                        foreach (var l in cref.GetComponentsInChildren<Light>(true))
                            if (l.name == "FireLight") { lights.Add(l); if (l.GetComponent<ASBO.Slice.FireFlicker>() == null) l.gameObject.AddComponent<ASBO.Slice.FireFlicker>(); }
                        var lod = cref.GetComponent<ASBO.Slice.CotLOD>() ?? cref.gameObject.AddComponent<ASBO.Slice.CotLOD>();
                        var deco = cref.transform.Find("Interior"); var furn = cref.transform.Find("Furniture");
                        lod.Setup(deco ? deco.gameObject : null, furn ? furn.gameObject : null, lights);
                    }
                EditorSceneManager.SaveScene(sc);
                Debug.Log("[ASBO] fixed markers in " + path + " (removed " + removed + " missing scripts)");
            }
            EditorSceneManager.OpenScene("Assets/Scenes/AbbotsWend_Test.unity", OpenSceneMode.Single);
        }

        [MenuItem("ASBO/Test/3 Build Test Scene")]
        public static void BuildScene()
        {
            if (!File.Exists("Assets/Resources/Anim/LocoM.controller")) BuildControllers();
            const string src = "Assets/Scenes/AbbotsWend.unity", dst = "Assets/Scenes/AbbotsWend_Test.unity";

            AssetDatabase.CopyAsset(src, dst);
            var scene = EditorSceneManager.OpenScene(dst, OpenSceneMode.Single);
            var glib = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UMA/Core/Defaults/UMA_GLIB.prefab");
            if (glib != null) PrefabUtility.InstantiatePrefab(glib);
            else Debug.LogError("[ASBO] UMA_GLIB prefab not found");
            var marker = new GameObject("FreePackTest"); marker.AddComponent<ASBO.Slice.FreePackTest>();
            // RCC Lite demo car where the old car was
            var carPath = AssetDatabase.FindAssets("t:Prefab Model_Skyline", new[] { "Assets/Realistic Car Controller Pro/Prefabs" }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
            if (carPath != null)
            {
                var car = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(carPath));
                car.name = "RCC_Car"; car.transform.position = new Vector3(-14f, 0.35f, -26.3f); car.transform.rotation = Quaternion.Euler(0, 90f, 0);
                car.AddComponent<ASBO.Slice.RccVehicle>();
            }
            else Debug.LogError("[ASBO] RCC demo car prefab not found");
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ASBO] Test scene built: " + dst);
        }
    }
}
#endif
