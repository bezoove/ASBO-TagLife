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
        public static void BuildControllers() { foreach (var ik in new[] { true, false }) { Build("M", "Male", ik); Build("F", "Female", ik); } Build("M", "Male", true, true); Build("F", "Female", true, true); AssetDatabase.SaveAssets(); }

        const string SABase = "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Animations/";
        /// <summary>Unity's Starter Assets mocap walk/run (imported Asset Store pack), for an A/B against the current set.</summary>
        static AnimationClip SAClip(string file)
        {
            var path = SABase + file + ".anim.fbx";
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) { Debug.LogWarning("[ASBO] missing " + path); return null; }
            if (mi.animationType != ModelImporterAnimationType.Human) Debug.LogWarning("[ASBO] " + file + " is " + mi.animationType + ", not Humanoid");
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(x => !x.name.StartsWith("__preview__"));
        }

        static void Build(string s, string folder, bool ik, bool sa = false)
        {
            string p = folder + "/", h = "Human" + s + "@";
            string ctrlPath = Out + "Loco" + s + (sa ? "_SA" : ik ? "" : "_Stock") + ".controller";
            Directory.CreateDirectory(Out);
            if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            foreach (var n in new[] { "MoveX", "MoveZ" }) ac.AddParameter(n, AnimatorControllerParameterType.Float);
            foreach (var n in new[] { "Talk", "Kneel", "Wave", "Grounded" }) ac.AddParameter(n, AnimatorControllerParameterType.Bool);
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
            if (sa)
            {
                // forward walk and run from Starter Assets, played at the speed our walk/run move at so the feet don't slide
                var kids = tree.children;
                // Starter Assets clips are in place; their own controller plays the walk at 2.0 m/s and the run at sprint, 5.335 m/s
                foreach (var (kev, file, authored) in new[] { ("Walk01_Forward", "Locomotion--Walk_N", 2.0f), ("Sprint01_Forward", "Locomotion--Run_N", 5.335f) })
                {
                    var c = SAClip(file); if (c == null) continue;
                    for (int i = 0; i < kids.Length; i++)
                    {
                        if (kids[i].motion == null || !kids[i].motion.name.Contains(kev) || Mathf.Abs(kids[i].position.x) > 0.1f) continue;
                        float kevSpeed = kids[i].position.y, saSpeed = c.averageSpeed.z > 0.2f ? c.averageSpeed.z : authored;
                        kids[i].motion = c;
                        kids[i].timeScale = kevSpeed / saSpeed;
                        Debug.Log($"[ASBO] SA {file}: clip speed {saSpeed:F2} m/s, ours {kevSpeed:F2} -> timeScale {kids[i].timeScale:F2}, len {c.length:F2}s, loop {c.isLooping}");
                    }
                }
                tree.children = kids;
            }
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
            // paramedic kneel and a wave: CMU mocap clips (retargeted through Humanoid like the pack's)
            foreach (var (nm, y) in new[] { ("Kneel", 220f), ("Wave", 300f) })
            {
                var c = ASBOImport.ClipNamed(nm); if (c == null) { Debug.LogWarning("[ASBO] no " + nm + " clip"); continue; }
                var st = St(nm, c, new Vector3(300, y));
                var a = move.AddTransition(st); a.AddCondition(AnimatorConditionMode.If, 0, nm); a.hasExitTime = false; a.duration = 0.35f;
                var b = st.AddTransition(move); b.AddCondition(AnimatorConditionMode.IfNot, 0, nm); b.hasExitTime = false; b.duration = 0.35f;
            }
            // Foot IK on every state: feet go back to where the original mocap put them (fixes wide stance after retargeting onto UMA)
            foreach (var cs in sm.states) cs.state.iKOnFeet = ik;
            if (ik) { var ls = ac.layers; ls[0].iKPass = true; ac.layers = ls; } // ground-aware feet (FootGround.OnAnimatorIK)
            if (!ik) { EditorUtility.SetDirty(ac); return; }
            EditorUtility.SetDirty(ac);
            if (sa) return;
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

        [MenuItem("ASBO/Test/Log Leg Curves")]
        public static void LogLegCurves()
        {
            foreach (var rel in new[] { "Male/Movement/Run/RootMotion/HumanM@Run01_Forward [RM]", "Male/Movement/Walk/RootMotion/HumanM@Walk01_Forward [RM]", "Male/Idles/HumanM@Idle01" })
            {
                var c = Clip(rel); if (c == null) continue;
                var sb = new System.Text.StringBuilder("[ASBO] curves " + c.name + ": ");
                foreach (var b in AnimationUtility.GetCurveBindings(c))
                {
                    if (!(b.propertyName.Contains("Leg") || b.propertyName.Contains("Foot") || b.propertyName.Contains("Spread"))) continue;
                    var cv = AnimationUtility.GetEditorCurve(c, b); float mn = 9, mx = -9, sum = 0; int n = 40;
                    for (int i = 0; i < n; i++) { float v = cv.Evaluate(c.length * i / n); mn = Mathf.Min(mn, v); mx = Mathf.Max(mx, v); sum += v; }
                    sb.Append($"{b.propertyName}=[{mn:F2},{sum / n:F2},{mx:F2}] ");
                }
                Debug.Log(sb.ToString());
            }
        }

        /// <summary>RCC ships Built-in pipeline materials (pink in URP): convert them to URP Lit / Particles Unlit, keeping maps and colours.</summary>
        [MenuItem("ASBO/Test/5 Convert RCC Materials to URP")]
        public static void ConvertRcc()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit"); var pu = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            int n = 0;
            foreach (var path in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Realistic Car Controller Pro" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(path); if (m == null || m.shader == null) continue;
                string sn = m.shader.name;
                if (path.Contains("/Font/") || path.Contains("/Lensflares/") || path.Contains("Skybox"))
                {   // not surface materials: put back anything an earlier run changed
                    var orig = path.Contains("/Font/") ? (path.EndsWith(".asset") ? null : "GUI/Text Shader") : path.Contains("/Lensflares/") ? "FX/Flare" : "Skybox/Procedural";
                    if (orig != null && sn != orig && Shader.Find(orig) != null) { m.shader = Shader.Find(orig); EditorUtility.SetDirty(m); }
                    continue;
                }
                if (sn.StartsWith("Universal Render Pipeline") || sn.StartsWith("Shader Graphs")) continue;
                Texture T(string p) => m.HasProperty(p) ? m.GetTexture(p) : null;
                Color C(string p, Color d) => m.HasProperty(p) ? m.GetColor(p) : d;
                float F(string p, float d) => m.HasProperty(p) ? m.GetFloat(p) : d;
                var main = T("_MainTex"); var col = C("_Color", C("_TintColor", Color.white)); var bump = T("_BumpMap");
                var mg = T("_MetallicGlossMap"); float met = F("_Metallic", 0f), gl = F("_Glossiness", F("_Smoothness", 0.5f));
                var em = T("_EmissionMap"); var emc = C("_EmissionColor", Color.black); float mode = F("_Mode", 0f);
                bool particle = sn.ToLowerInvariant().Contains("particle") || sn.ToLowerInvariant().Contains("additive") || sn.ToLowerInvariant().Contains("alpha blended") || path.Contains("/Particle") || path.Contains("Skidmark") || path.Contains("WheelBlur");
                m.shader = particle ? pu : lit;
                if (main != null) m.SetTexture("_BaseMap", main);
                m.SetColor("_BaseColor", col);
                if (!particle)
                {
                    if (bump != null) { m.SetTexture("_BumpMap", bump); m.EnableKeyword("_NORMALMAP"); }
                    if (mg != null) { m.SetTexture("_MetallicGlossMap", mg); m.EnableKeyword("_METALLICSPECGLOSSMAP"); }
                    m.SetFloat("_Metallic", met); m.SetFloat("_Smoothness", gl);
                    if (em != null || emc.maxColorComponent > 0.01f) { if (em != null) m.SetTexture("_EmissionMap", em); m.SetColor("_EmissionColor", emc); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
                }
                bool transparent = particle || mode >= 2f;
                if (transparent)
                {
                    m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", sn.ToLowerInvariant().Contains("additive") ? 2f : 0f);
                    m.SetOverrideTag("RenderType", "Transparent");
                    m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    m.SetFloat("_DstBlend", (float)(sn.ToLowerInvariant().Contains("additive") ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
                    m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
                }
                else if (mode == 1f) { m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", F("_Cutoff", 0.5f)); m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450; }
                EditorUtility.SetDirty(m); n++;
                Debug.Log("[ASBO] RCC material " + path + ": " + sn + " -> " + m.shader.name);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[ASBO] converted " + n + " RCC materials to URP");
        }

        [MenuItem("ASBO/Test/6 Use Test Scene For Builds")]
        public static void UseTestSceneForBuilds()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/AbbotsWend_Test.unity", true), new EditorBuildSettingsScene("Assets/Scenes/AbbotsWend.unity", false) };
            Debug.Log("[ASBO] build scenes: AbbotsWend_Test (UMA/mocap/RCC) first");
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
