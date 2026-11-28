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
        public static void BuildControllers() { foreach (var ik in new[] { true, false }) { Build("M", "Male", ik); Build("F", "Female", ik); } Build("M", "Male", true, true); Build("F", "Female", true, true); BuildDrunk(); BuildHurt(); AssetDatabase.SaveAssets(); }

        const string MC = "Assets/MoCapCentral/MC_Sample/Animations/";
        /// <summary>A clip from any imported Humanoid file (.fbx or .anim); null with a warning if missing.</summary>
        static AnimationClip AnyClip(string path)
        {
            if (path.EndsWith(".anim")) { var a = AssetDatabase.LoadAssetAtPath<AnimationClip>(path); if (a == null) Debug.LogWarning("[ASBO] missing " + path); return a; }
            var c = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(x => !x.name.StartsWith("__preview__"));
            if (c == null) Debug.LogWarning("[ASBO] missing " + path);
            return c;
        }

        /// <summary>1D blend on "Variant" (0, 0.5, 1): each person keeps one of these for good (set once at spawn).</summary>
        static Motion Variants(AnimatorController ac, string name, params AnimationClip[] clips)
        {
            var ok = clips.Where(c => c != null).ToArray();
            if (ok.Length == 0) return null; if (ok.Length == 1) return ok[0];
            var bt = new BlendTree { name = name, blendType = BlendTreeType.Simple1D, blendParameter = "Variant", useAutomaticThresholds = false, hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(bt, ac);
            for (int i = 0; i < ok.Length; i++) bt.AddChild(ok[i], i / (float)(ok.Length - 1));
            return bt;
        }

        /// <summary>1D blend on a whole-number parameter (0, 1, 2, ...): set to one of those, it plays just that motion.</summary>
        static Motion Pick(AnimatorController ac, string name, string param, (Motion m, float ts, float off)[] kids)
        {
            var ok = kids.Where(k => k.m != null).ToArray();
            if (ok.Length == 0) return null; if (ok.Length == 1 && ok[0].ts == 1f && ok[0].off == 0f) return ok[0].m;
            var bt = new BlendTree { name = name, blendType = BlendTreeType.Simple1D, blendParameter = param, useAutomaticThresholds = false, hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(bt, ac);
            for (int i = 0; i < ok.Length; i++) bt.AddChild(ok[i].m, i);
            var ch = bt.children; for (int i = 0; i < ch.Length; i++) { ch[i].timeScale = ok[i].ts; ch[i].cycleOffset = ok[i].off; } bt.children = ch;
            return bt;
        }

        /// <summary>
        /// Swap the straight-ahead child of the 8-way blend (between yMin and yMax m/s) for the Mixamo versions, one per
        /// "Gait": each played at the pace that slot moves at (no foot slide) and offset in its cycle so its left foot
        /// comes forward when the clip it replaced did.
        /// </summary>
        static void GaitSlot(AnimatorController ac, BlendTree tree, float yMin, float yMax, params (string file, string clip, float speed)[] takes)
        {
            var kids = tree.children;
            for (int i = 0; i < kids.Length; i++)
            {
                if (Mathf.Abs(kids[i].position.x) > 0.1f || kids[i].position.y < yMin || kids[i].position.y > yMax || !(kids[i].motion is AnimationClip was)) continue;
                float slot = kids[i].position.y, wasPhase = ASBOMixamo.Phase(was);
                var list = new List<(Motion, float, float)>();
                foreach (var (file, clip, known) in takes)
                {
                    var c = ASBOMixamo.ClipFrom(file, clip); if (c == null) continue;
                    float v = c.averageSpeed.magnitude; if (v < 0.4f) v = known; if (v < 0.4f) { Debug.LogWarning("[ASBO] mixamo " + clip + ": can't tell its pace"); continue; }
                    float ts = slot / v; if (ts < 0.7f || ts > 1.6f) { Debug.LogWarning($"[ASBO] mixamo {clip}: {v:F2} m/s is too far from {slot:F2}"); continue; }
                    float off = Mathf.Repeat(ASBOMixamo.Phase(c) - wasPhase, 1f);
                    list.Add((c, ts, off));
                    Debug.Log($"[ASBO] mixamo {clip} at {slot:F2} m/s: own pace {v:F2}, timeScale {ts:F2}, cycle offset {off:F2} (replaces {was.name})");
                }
                if (list.Count == 0) continue;
                kids[i].motion = Pick(ac, "Gait " + slot.ToString("F1"), "Gait", list.ToArray()); kids[i].timeScale = 1f;
                if (list.Count == 1) { kids[i].timeScale = list[0].Item2; kids[i].cycleOffset = list[0].Item3; }
            }
            tree.children = kids;
        }

        const string KO = "Assets/MoCapCentral/MC_Sample/Animations/Fight/MCU_am_Ready_Fight_01_Knockdown_A.FBX";
        /// <summary>
        /// Cut the moves out of MoCap Central's 25 s fight take. Ranges found by measuring the body through the take
        /// (ASBOFightCuts: hands against head and chest), not by eye: jab, cross, left hook, right to the body, overhand,
        /// front kick, fists-up stance, high guard, and buckling from a body shot.
        /// </summary>
        static void SplitFightClips()
        {
            var mi = AssetImporter.GetAtPath(KO) as ModelImporter; if (mi == null) { Debug.LogWarning("[ASBO] no " + KO); return; }
            var defs = mi.defaultClipAnimations; if (defs.Length == 0) return;
            var baseC = defs[0];
            ModelImporterClipAnimation Make(string name, float f0, float f1, bool loop) => new ModelImporterClipAnimation
            {
                name = name, takeName = baseC.takeName, firstFrame = f0, lastFrame = f1, loopTime = loop, loopPose = loop,
                lockRootRotation = true, keepOriginalOrientation = false, lockRootHeightY = true, keepOriginalPositionY = true, lockRootPositionXZ = true, keepOriginalPositionXZ = false,
                maskType = baseC.maskType
            };
            var list = new[] { baseC, Make("ASBO_Punch", 155, 180, false), Make("ASBO_Kick", 102, 139, false), Make("ASBO_Stance", 30, 90, true),
                Make("ASBO_Cross", 182, 202, false), Make("ASBO_Hook", 213, 242, false), Make("ASBO_Body", 322, 340, false), Make("ASBO_Over", 550, 583, false),
                Make("ASBO_Guard", 585, 600, true), Make("ASBO_Buckle", 519, 546, false) };
            var cur = mi.clipAnimations;
            if (cur.Length == list.Length && cur.Select(c => c.name + c.firstFrame + c.lastFrame).SequenceEqual(list.Select(c => c.name + c.firstFrame + c.lastFrame))) return;
            mi.clipAnimations = list; mi.SaveAndReimport();
            Debug.Log("[ASBO] fight clips cut from " + System.IO.Path.GetFileName(KO));
        }
        static AnimationClip KOClip(string name) => AssetDatabase.LoadAllAssetsAtPath(KO).OfType<AnimationClip>().FirstOrDefault(c => c.name == name);

        /// <summary>Upper body only (spine, head, arms, fingers): the fists-up stance and guard ride on top of walking.</summary>
        static AvatarMask UpperBodyMask()
        {
            string path = Out + "UpperBody.mask";
            var m = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
            if (m != null) return m;
            m = new AvatarMask();
            for (var part = AvatarMaskBodyPart.Root; part < AvatarMaskBodyPart.LastBodyPart; part++)
                m.SetHumanoidBodyPartActive(part, part == AvatarMaskBodyPart.Body || part == AvatarMaskBodyPart.Head || part == AvatarMaskBodyPart.LeftArm || part == AvatarMaskBodyPart.RightArm || part == AvatarMaskBodyPart.LeftFingers || part == AvatarMaskBodyPart.RightFingers);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>
        /// Fighting: any-state triggers for each move and for taking a hit (head or body), back to Move when done; plus a
        /// "Fists" layer over the upper body for the fists-up stance and the guard (Fighter sets its weight).
        /// Durations here are the clip lengths at these speeds; Fighter's move table matches them.
        /// </summary>
        static void AddFight(AnimatorController ac, AnimatorStateMachine sm, AnimatorState move)
        {
            SplitFightClips();
            foreach (var n in new[] { "Punch", "Cross", "Hook", "Body", "Hay", "Kick", "Shove", "Struck", "StruckBody" }) if (!ac.parameters.Any(q => q.name == n)) ac.AddParameter(n, AnimatorControllerParameterType.Trigger);
            foreach (var n in new[] { "Guard" }) if (!ac.parameters.Any(q => q.name == n)) ac.AddParameter(n, AnimatorControllerParameterType.Bool);
            foreach (var n in new[] { "AVar", "RVar" }) if (!ac.parameters.Any(q => q.name == n)) ac.AddParameter(n, AnimatorControllerParameterType.Float);
            // the first version of each move (MoCap Central / Kevin Iglesias): clip, play speed, when it lands and when you can go again (s at that speed), how close the bodies are
            var old = new (string trig, AnimationClip c, float speed, float imp, float dur, float contact)[] {
                ("Punch", KOClip("ASBO_Punch"), 2.1f, 0.29f, 0.40f, 0.85f), ("Cross", KOClip("ASBO_Cross"), 1.6f, 0.21f, 0.42f, 0.88f), ("Hook", KOClip("ASBO_Hook"), 1.9f, 0.27f, 0.51f, 0.80f),
                ("Body", KOClip("ASBO_Body"), 1.55f, 0.14f, 0.39f, 0.75f), ("Hay", (AnimationClip)null, 1.7f, 0.36f, 0.62f, 0.85f), ("Kick", KOClip("ASBO_Kick"), 1.8f, 0.50f, 0.69f, 1.05f),
                ("Shove", AnyClip(KevBase + "Male/Combat/Shield/HumanM@AttackShield01.fbx"), 1.5f, 0.26f, 0.60f, 0.80f),
                ("Struck", AnyClip(KevBase + "Male/Combat/HumanM@CombatDamage01.fbx"), 1.4f, 0f, 0f, 0f), ("StruckBody", KOClip("ASBO_Buckle"), 1.5f, 0f, 0f, 0f) };
            var vars = new List<ASBO.Slice.FightSet.Var>();
            int i = 0;
            foreach (var (trig, c, speed, imp, dur, contact) in old)
            {
                bool react = trig.StartsWith("Struck");
                // every version of this move: the old one plus the Mixamo ones (FightSet.Takes), picked at random each time ("AVar" / "RVar")
                var kids = new List<(Motion m, float ts)>();
                if (c != null) { vars.Add(new ASBO.Slice.FightSet.Var { trig = trig, idx = 0, imp = imp, dur = dur, contact = contact, w = 1f, name = c.name }); kids.Add((c, speed)); }
                for (int k = 0; k < ASBO.Slice.FightSet.Takes.Length; k++)
                {
                    var t = ASBO.Slice.FightSet.Takes[k]; if (t.trig != trig) continue;
                    var mc = ASBOMixamo.ClipFrom(t.file, ASBOMixamo.FightClipName(k)); if (mc == null) continue;
                    float tImp = Mathf.Max(0f, t.hit - t.a) / t.speed;
                    vars.Add(new ASBO.Slice.FightSet.Var { trig = trig, idx = kids.Count, imp = tImp, dur = tImp + ASBO.Slice.FightSet.Recovery(trig), contact = t.contact, w = t.w, big = t.big, name = t.file });
                    kids.Add((mc, t.speed));
                }
                if (kids.Count == 0) { Debug.LogWarning("[ASBO] no clip for " + trig); continue; }
                var bt = new BlendTree { name = "F" + trig + " versions", blendType = BlendTreeType.Simple1D, blendParameter = react ? "RVar" : "AVar", useAutomaticThresholds = false, hideFlags = HideFlags.HideInHierarchy };
                AssetDatabase.AddObjectToAsset(bt, ac);
                for (int j = 0; j < kids.Count; j++) bt.AddChild(kids[j].m, j);
                var ch = bt.children; for (int j = 0; j < ch.Length; j++) ch[j].timeScale = kids[j].ts; bt.children = ch;
                var st = sm.AddState("F" + trig, new Vector3(650, 60 * i++)); st.motion = bt; st.iKOnFeet = true; st.speed = 1f;
                var tin = sm.AddAnyStateTransition(st); tin.AddCondition(AnimatorConditionMode.If, 0, trig); tin.hasExitTime = false; tin.duration = react ? 0.05f : 0.07f; tin.canTransitionToSelf = react;
                var tout = st.AddTransition(move); tout.hasExitTime = true; tout.exitTime = 0.86f; tout.duration = 0.16f;
                Debug.Log($"[ASBO] fight {trig}: {kids.Count} versions ({string.Join(", ", kids.Select(x => x.m.name + " " + (((AnimationClip)x.m).length / x.ts).ToString("F2") + "s"))})");
            }
            Directory.CreateDirectory(Out);
            File.WriteAllText(Out + "FightVariants.json", JsonUtility.ToJson(new ASBO.Slice.FightSet.Table { v = vars.ToArray() }, true));
            AssetDatabase.ImportAsset(Out + "FightVariants.json");
            // the Fists layer: stance by default, guard while "Guard" is held
            var stance = KOClip("ASBO_Stance"); var guard = KOClip("ASBO_Guard");
            var mxStance = ASBOMixamo.ClipNamed("MX_Stance"); // the ready pose held round Mixamo's body block: fists properly up
            if (mxStance != null) { stance = mxStance; guard = mxStance; }
            if (stance == null || guard == null) return;
            if (ac.layers.Any(l => l.name == "Fists")) return;
            var lsm = new AnimatorStateMachine { name = "Fists", hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(lsm, ac);
            var sSt = lsm.AddState("Stance", new Vector3(300, 0)); sSt.motion = guard; // fists right up by the face (the high guard), not the low ready stance
            var gSt = lsm.AddState("Guard", new Vector3(300, 80)); gSt.motion = guard;
            lsm.defaultState = sSt;
            var a = sSt.AddTransition(gSt); a.AddCondition(AnimatorConditionMode.If, 0, "Guard"); a.hasExitTime = false; a.duration = 0.08f;
            var b = gSt.AddTransition(sSt); b.AddCondition(AnimatorConditionMode.IfNot, 0, "Guard"); b.hasExitTime = false; b.duration = 0.15f;
            ac.AddLayer(new AnimatorControllerLayer { name = "Fists", stateMachine = lsm, avatarMask = UpperBodyMask(), defaultWeight = 0f, blendingMode = AnimatorLayerBlendingMode.Override });
        }

        /// <summary>Hurt: getting up off the floor, then bent over holding the belly (idle and limping walk). MoCap Central injured set.</summary>
        /// <summary>
        /// The "Fidget" layer: small movements added on top of the body (upper body only) for people sat or stood about,
        /// one state per movement in <see cref="ASBO.Slice.Fidget.Clips"/>, each going back to "Still" when it's done.
        /// The Fidget component picks them at random and sets the layer's weight.
        /// </summary>
        static void AddFidgets(AnimatorController ac)
        {
            const string maskPath = "Assets/Resources/Anim/FidgetUpper.mask";
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
            if (mask == null)
            {
                mask = new AvatarMask();
                for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                {
                    var bp = (AvatarMaskBodyPart)i;
                    mask.SetHumanoidBodyPartActive(bp, bp == AvatarMaskBodyPart.Body || bp == AvatarMaskBodyPart.Head || bp == AvatarMaskBodyPart.LeftArm || bp == AvatarMaskBodyPart.RightArm || bp == AvatarMaskBodyPart.LeftFingers || bp == AvatarMaskBodyPart.RightFingers);
                }
                AssetDatabase.CreateAsset(mask, maskPath);
            }
            ac.AddLayer("Fidget");
            var ls = ac.layers; var L = ls[ls.Length - 1];
            L.blendingMode = AnimatorLayerBlendingMode.Additive; L.avatarMask = mask; L.defaultWeight = 0f; ac.layers = ls;
            var fsm = ac.layers[ls.Length - 1].stateMachine;
            var still = fsm.AddState("Still", new Vector3(300, 0)); fsm.defaultState = still;
            int made = 0;
            for (int i = 0; i < ASBO.Slice.Fidget.Clips.Length; i++)
            {
                string path = ASBO.Slice.Fidget.Clips[i].path;
                var c = path.StartsWith("mixamo:") ? ASBOMixamo.ClipNamed(path.Substring(7)) : AnyClip(path);
                var st = fsm.AddState("F" + i, new Vector3(560, 50 * i)); st.motion = c; if (c != null) made++;
                var back = st.AddTransition(still); back.hasExitTime = true; back.exitTime = 0.92f; back.duration = 0.35f;
            }
            Debug.Log($"[ASBO] fidgets: {made} of {ASBO.Slice.Fidget.Clips.Length} small movements on the Fidget layer of {ac.name}");
        }

        static void BuildHurt()
        {
            string ctrlPath = Out + "LocoHurt.controller";
            if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);
            var idle = AnyClip(MC + "Injured/MCU_am_InjuredBelly_Idle_01.FBX"); var walk = AnyClip(MC + "Injured/MCU_am_InjuredBelly_Loco_Walk_Fwd.FBX"); var up = AnyClip(MC + "Injured/MCU_am_InjuredBelly_DropToFloor_GetUp.FBX");
            if (idle == null || walk == null || up == null) return;
            var ac = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            foreach (var n in new[] { "MoveX", "MoveZ", "Variant" }) ac.AddParameter(n, AnimatorControllerParameterType.Float);
            foreach (var n in new[] { "Talk", "Kneel", "Wave", "Grounded" }) ac.AddParameter(n, AnimatorControllerParameterType.Bool);
            foreach (var n in new[] { "Jump", "TurnL", "TurnR" }) ac.AddParameter(n, AnimatorControllerParameterType.Trigger);
            var sm = ac.layers[0].stateMachine;
            var move = ac.CreateBlendTreeInController("Move", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.FreeformDirectional2D; tree.blendParameter = "MoveX"; tree.blendParameterY = "MoveZ";
            tree.AddChild(idle, Vector2.zero);
            float sp = Mathf.Max(0.4f, walk.averageSpeed.magnitude); tree.AddChild(walk, new Vector2(0f, sp));
            sm.defaultState = move;
            var gu = sm.AddState("GetUp", new Vector3(300, -80)); gu.motion = up;
            var t = gu.AddTransition(move); t.hasExitTime = true; t.exitTime = 0.7f; t.duration = 0.5f;
            var talk = sm.AddState("Talk", new Vector3(300, 140)); talk.motion = Clip("Male/Social/Conversation/HumanM@Talk01");
            var ti = move.AddTransition(talk); ti.AddCondition(AnimatorConditionMode.If, 0, "Talk"); ti.hasExitTime = false; ti.duration = 0.3f;
            var to = talk.AddTransition(move); to.AddCondition(AnimatorConditionMode.IfNot, 0, "Talk"); to.hasExitTime = false; to.duration = 0.3f;
            AddFight(ac, sm, move);
            foreach (var cs in sm.states) cs.state.iKOnFeet = cs.state != gu;
            var ls = ac.layers; ls[0].iKPass = true; ac.layers = ls;
            EditorUtility.SetDirty(ac);
            Debug.Log($"[ASBO] LocoHurt: limp {sp:F2} m/s, get-up {up.length:F2}s");
        }

        /// <summary>Test controller: one state per fight clip (int "Clip"), to film them and pick out the punches.</summary>
        [MenuItem("ASBO/Test/7 Build Fight Test Controller")]
        public static void BuildFightTest()
        {
            string ctrlPath = Out + "FightTest.controller";
            if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ac.AddParameter("Clip", AnimatorControllerParameterType.Int);
            var sm = ac.layers[0].stateMachine;
            string[] files = { MC + "Fight/MCU_am_Ready_Fight_01_Knockdown_A.FBX", MC + "Fight/MCU_af_Ready_Fight_01_Knockdown_B.FBX", MC + "Fight/MCU_am_Ready_Fight_02_Kickdown_A.FBX", MC + "Fight/MCU_af_Ready_Fight_02_Kickdown_B.FBX",
                KevBase + "Male/Combat/HumanM@CombatIdle01.fbx", KevBase + "Male/Combat/HumanM@CombatDamage01.fbx", KevBase + "Male/Combat/1H/HumanM@Attack1H01_R.fbx", "Assets/DoubleL/FBX Unity/Enemy Attack/Enemy_Attack_1.fbx",
                MC + "Injured/MCU_am_InjuredBelly_DropToFloor.FBX", MC + "Injured/MCU_am_InjuredBelly_DropToFloor_Loop.FBX", MC + "Injured/MCU_am_InjuredBelly_DropToFloor_GetUp.FBX", MC + "Injured/MCU_am_InjuredBelly_Loco_Walk_Fwd.FBX", MC + "Injured/MCU_am_InjuredBelly_Idle_01.FBX" };
            var empty = sm.AddState("Empty"); sm.defaultState = empty;
            for (int i = 0; i < files.Length; i++)
            {
                var c = AnyClip(files[i]); if (c == null) continue;
                var st = sm.AddState("C" + i, new Vector3(300, 60 * i)); st.motion = c;
                var t = sm.AddAnyStateTransition(st); t.AddCondition(AnimatorConditionMode.Equals, i, "Clip"); t.duration = 0f; t.canTransitionToSelf = false;
                Debug.Log($"[ASBO] fight clip {i}: {c.name} {c.length:F2}s loop {c.isLooping}");
            }
            EditorUtility.SetDirty(ac); AssetDatabase.SaveAssets();
        }

        /// <summary>The estate drunk: MoCap Central's drunk idle and stagger (imported Asset Store pack), same parameters as the others.</summary>
        static void BuildDrunk()
        {
            string ctrlPath = Out + "LocoM_Drunk.controller";
            if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);
            var idle = AnyClip(MC + "Drunk/MCU_am_StandDrunk_Idle_01.FBX"); var walk = AnyClip(MC + "Drunk/MCU_am_Drunk_Loco_Walk_01.FBX");
            if (idle == null || walk == null) return;
            var ac = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            foreach (var n in new[] { "MoveX", "MoveZ", "Variant" }) ac.AddParameter(n, AnimatorControllerParameterType.Float);
            foreach (var n in new[] { "Talk", "Kneel", "Wave", "Grounded" }) ac.AddParameter(n, AnimatorControllerParameterType.Bool);
            foreach (var n in new[] { "Jump", "TurnL", "TurnR" }) ac.AddParameter(n, AnimatorControllerParameterType.Trigger);
            var sm = ac.layers[0].stateMachine;
            var move = ac.CreateBlendTreeInController("Move", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.FreeformDirectional2D; tree.blendParameter = "MoveX"; tree.blendParameterY = "MoveZ";
            tree.AddChild(idle, Vector2.zero);
            float sp = Mathf.Max(0.5f, walk.averageSpeed.magnitude);
            tree.AddChild(walk, new Vector2(0f, sp));
            sm.defaultState = move;
            var talkClip = Clip("Male/Social/Conversation/HumanM@Talk01");
            var talk = sm.AddState("Talk", new Vector3(300, 140)); talk.motion = Variants(ac, "Talks", talkClip, AnyClip(MC + "Emotions/MCU_am_Stand_Emotion_ThisGuy_01.FBX"), AnyClip(MC + "Idle/MCU_am_Stand_Conv_Talk_05_Generic.FBX"));
            var ti = move.AddTransition(talk); ti.AddCondition(AnimatorConditionMode.If, 0, "Talk"); ti.hasExitTime = false; ti.duration = 0.3f;
            var to = talk.AddTransition(move); to.AddCondition(AnimatorConditionMode.IfNot, 0, "Talk"); to.hasExitTime = false; to.duration = 0.3f;
            AddFight(ac, sm, move);
            foreach (var cs in sm.states) cs.state.iKOnFeet = true;
            var ls = ac.layers; ls[0].iKPass = true; ac.layers = ls;
            EditorUtility.SetDirty(ac);
            Debug.Log($"[ASBO] LocoM_Drunk: stagger {sp:F2} m/s, len {walk.length:F2}s, loop {walk.isLooping}");
        }

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
            foreach (var n in new[] { "MoveX", "MoveZ", "Variant", "Gait", "IdleVar", "SitVar" }) ac.AddParameter(n, AnimatorControllerParameterType.Float);
            foreach (var n in new[] { "Talk", "Kneel", "Wave", "Grounded", "Seated", "Crouch", "Sit", "Stick", "Fall" }) ac.AddParameter(n, AnimatorControllerParameterType.Bool);
            foreach (var n in new[] { "Jump", "TurnL", "TurnR", "Climb", "HardLand", "RunStop", "Stamp", "Carjack", "PulledOut", "Roll", "Stumble", "WallClimb", "Take", "Barged", "Throw" }) ac.AddParameter(n, AnimatorControllerParameterType.Trigger);
            var sm = ac.layers[0].stateMachine;

            var move = ac.CreateBlendTreeInController("Move", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.FreeformDirectional2D; tree.blendParameter = "MoveX"; tree.blendParameterY = "MoveZ";
            var idle = Clip(p + "Idles/" + h + "Idle01");
            // standing about: the pack idle, looking round, scratching an arm (MoCap Central)
            // plus the Mixamo idles; each person keeps one ("IdleVar", set once)
            tree.AddChild(ik ? Pick(ac, "Idles", "IdleVar", new[] { idle, AnyClip(MC + "Idle/MCU_am_Stand_Idle_03_LookAround.FBX"), AnyClip(MC + "Idle/MCU_am_Stand_Idle_06_ScratchArm.FBX") }
                .Concat(ASBOMixamo.IdleFiles.Select((f, q) => ASBOMixamo.ClipFrom(f, "MX_Idle" + q))).Select(c => ((Motion)c, 1f, 0f)).ToArray()) : idle, Vector2.zero);
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
            // quicker running than the clips' own pace: play them faster and move faster to match (feet stay planted)
            {
                var kids0 = tree.children; const float k = 1.3f;
                for (int q = 0; q < kids0.Length; q++) if (kids0[q].position.magnitude > 2.6f) { kids0[q].position *= k; kids0[q].timeScale *= k; }
                tree.children = kids0; sp.run *= k; sp.sprint *= k;
            }
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
                // the Mixamo walks (your X Bot downloads) take over walking forward; each person has their own way of walking
                // ("Gait", set once), lined up in step with the pack's sideways and diagonal cycles. Running stays on the pack's
                // own motion-capture run: the Mixamo runs retargeted badly onto our bodies (a foot twisted out, arms into the
                // sides, chest pushed out), so they're left out.
                GaitSlot(ac, tree, 0.5f, 2.6f, ("Walking", "MX_Walk", 1.71f), ("Walking (1)", "MX_Walk1", 1.75f));
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
            if (ik) talk.motion = Variants(ac, "Talks", (AnimationClip)talk.motion, AnyClip("Assets/DoubleL/Demo/Anim/Dialogue_1.anim"), AnyClip(MC + "Idle/MCU_am_Stand_Conv_Talk_05_Generic.FBX"));
            var ti = move.AddTransition(talk); ti.AddCondition(AnimatorConditionMode.If, 0, "Talk"); ti.hasExitTime = false; ti.duration = 0.3f;
            var to = talk.AddTransition(move); to.AddCondition(AnimatorConditionMode.IfNot, 0, "Talk"); to.hasExitTime = false; to.duration = 0.3f;
            if (ik) AddFight(ac, sm, move);
            // paramedic kneel and a wave: CMU mocap clips (retargeted through Humanoid like the pack's)
            foreach (var (nm, y) in new[] { ("Kneel", 220f), ("Wave", 300f) })
            {
                var c = ASBOImport.ClipNamed(nm); if (c == null) { Debug.LogWarning("[ASBO] no " + nm + " clip"); continue; }
                var st = St(nm, c, new Vector3(300, y));
                var a = move.AddTransition(st); a.AddCondition(AnimatorConditionMode.If, 0, nm); a.hasExitTime = false; a.duration = 0.35f;
                var b = st.AddTransition(move); b.AddCondition(AnimatorConditionMode.IfNot, 0, nm); b.hasExitTime = false; b.duration = 0.35f;
            }
            // crouched (and sneaking along): Mixamo's crouch idle and crouch walk on the forward speed
            var cIdle = ASBOMixamo.ClipFrom("Crouching Idle", "MX_CrouchIdle"); var cWalk = ASBOMixamo.ClipFrom("Crouch Walk", "MX_CrouchWalk");
            if (cIdle != null && cWalk != null)
            {
                var crouch = ac.CreateBlendTreeInController("Crouch", out BlendTree ct, 0);
                ct.blendType = BlendTreeType.Simple1D; ct.blendParameter = "MoveZ"; ct.useAutomaticThresholds = false;
                // each clip at the speed it was captured at, so the feet stay planted (the crouch walk was being dragged along at
                // more than twice its pace, which looked like shuffling along on your backside); quicker than that, the hunched sneak
                float cw = Mathf.Clamp(cWalk.averageSpeed.magnitude, 0.4f, 0.8f);
                ct.AddChild(cIdle, 0f); ct.AddChild(cWalk, cw);
                var sneak = ASBOMixamo.ClipFrom("Sneaking Forward", "MX_Sneak");
                if (sneak != null)
                {
                    float sv = Mathf.Max(1f, sneak.averageSpeed.magnitude); const float top = 1.6f;
                    ct.AddChild(sneak, top); var kids = ct.children; kids[2].timeScale = top / sv; ct.children = kids;
                    Debug.Log($"[ASBO] crouch: walk {cw:F2} m/s, sneak captured {sv:F2} m/s played at {top / sv:F2}x for {top} m/s");
                }
                var down = ASBOMixamo.ClipFrom("Standing To Crouched", "MX_CrouchDown"); var upc = ASBOMixamo.ClipFrom("Crouched To Standing", "MX_CrouchUp");
                if (down != null && upc != null)
                {
                    // getting down and getting up: the body folds down into the crouch and straightens out of it (cut short if you set off)
                    var dn = St("CrouchDown", down, new Vector3(520, 300)); var un = St("CrouchUp", upc, new Vector3(520, 360));
                    var a = move.AddTransition(dn); a.AddCondition(AnimatorConditionMode.If, 0, "Crouch"); a.hasExitTime = false; a.duration = 0.12f;
                    var a2 = dn.AddTransition(crouch); a2.hasExitTime = true; a2.exitTime = 0.7f; a2.duration = 0.15f;
                    var a3 = dn.AddTransition(un); a3.AddCondition(AnimatorConditionMode.IfNot, 0, "Crouch"); a3.hasExitTime = false; a3.duration = 0.15f;
                    var b = crouch.AddTransition(un); b.AddCondition(AnimatorConditionMode.IfNot, 0, "Crouch"); b.hasExitTime = false; b.duration = 0.12f;
                    var b2 = un.AddTransition(move); b2.hasExitTime = true; b2.exitTime = 0.7f; b2.duration = 0.15f;
                    var b3 = un.AddTransition(crouch); b3.AddCondition(AnimatorConditionMode.If, 0, "Crouch"); b3.hasExitTime = false; b3.duration = 0.15f;
                }
                else
                {
                    var a = move.AddTransition(crouch); a.AddCondition(AnimatorConditionMode.If, 0, "Crouch"); a.hasExitTime = false; a.duration = 0.25f;
                    var b = crouch.AddTransition(move); b.AddCondition(AnimatorConditionMode.IfNot, 0, "Crouch"); b.hasExitTime = false; b.duration = 0.25f;
                }
            }
            else Debug.LogWarning("[ASBO] no crouch clips (ASBO/Mixamo/2 Cut Clips)");
            // climbing up onto something
            var climb = ASBOMixamo.ClipFrom("Braced Hang To Crouch", "MX_ClimbUp");
            if (climb != null)
            {
                var cs = St("Climb", climb, new Vector3(300, 460)); cs.speed = 1.0f;
                var a = sm.AddAnyStateTransition(cs); a.AddCondition(AnimatorConditionMode.If, 0, "Climb"); a.hasExitTime = false; a.duration = 0.1f; a.canTransitionToSelf = false;
                var b = cs.AddTransition(move); b.hasExitTime = true; b.exitTime = 0.92f; b.duration = 0.3f;
            }
            // sat on a bench
            var sit = ASBOMixamo.ClipFrom("Jostled Passenger", "MX_Sit");
            if (sit != null)
            {
                // three ways of sitting (SitVar 0, 1, 2): settled, shifting about, and leaning forward with the hands out
                // (on a bar or a machine's buttons); each person picks one and starts it at their own point in it
                var ss = sm.AddState("Sit", new Vector3(300, 540));
                ss.motion = Pick(ac, "Sits", "SitVar", new (Motion, float, float)[] { (sit, 1f, 0f), (ASBOMixamo.ClipFrom("Jostled Passenger", "MX_SitB"), 1f, 0f), (ASBOMixamo.ClipFrom("Honking Horn", "MX_Drive"), 1f, 0f) });
                var a = move.AddTransition(ss); a.AddCondition(AnimatorConditionMode.If, 0, "Sit"); a.hasExitTime = false; a.duration = 0.3f;
                var b = ss.AddTransition(move); b.AddCondition(AnimatorConditionMode.IfNot, 0, "Sit"); b.hasExitTime = false; b.duration = 0.3f;
            }
            // sat at the wheel of a car (Mixamo's driver, between toots of the horn)
            var driveClip = ASBOMixamo.ClipFrom("Honking Horn", "MX_Drive");
            if (driveClip != null)
            {
                var ds = St("Drive", driveClip, new Vector3(300, 380));
                var a = sm.AddAnyStateTransition(ds); a.AddCondition(AnimatorConditionMode.If, 0, "Seated"); a.hasExitTime = false; a.duration = 0.2f; a.canTransitionToSelf = false;
                var b = ds.AddTransition(move); b.AddCondition(AnimatorConditionMode.IfNot, 0, "Seated"); b.hasExitTime = false; b.duration = 0.25f;
            }
            else Debug.LogWarning("[ASBO] no MX_Drive clip (ASBO/Mixamo/2 Cut Clips)");
            // a big drop: knees buckle on landing, a hand to the floor, up again
            var hard = ASBOMixamo.ClipFrom("Hard Landing", "MX_HardLand");
            if (hard != null)
            {
                var hs = St("HardLand", hard, new Vector3(900, 40));
                var a = sm.AddAnyStateTransition(hs); a.AddCondition(AnimatorConditionMode.If, 0, "HardLand"); a.hasExitTime = false; a.duration = 0.06f; a.canTransitionToSelf = false;
                var b = hs.AddTransition(move); b.hasExitTime = true; b.exitTime = 0.88f; b.duration = 0.2f;
            }
            // stamping on someone on the floor; dragging a driver out of their seat; being dragged out
            foreach (var (trig, file, cut, y, exit) in new[] { ("Stamp", "Stomp", "MX_Stomp", 620f, 0.9f), ("Carjack", "Pull Pilot From Seat", "MX_Carjack", 680f, 0.92f), ("PulledOut", "Pulled From Seat", "MX_PulledOut", 740f, 0.95f) })
            {
                var c = ASBOMixamo.ClipFrom(file, cut); if (c == null) { Debug.LogWarning("[ASBO] no " + cut); continue; }
                var st = St(trig, c, new Vector3(900, y)); if (trig == "Throw") st.speed = 1.7f;
                var a = sm.AddAnyStateTransition(st); a.AddCondition(AnimatorConditionMode.If, 0, trig); a.hasExitTime = false; a.duration = 0.12f; a.canTransitionToSelf = false;
                var b = st.AddTransition(move); b.hasExitTime = true; b.exitTime = exit; b.duration = 0.25f;
            }
            // a long drop: flailing until the ground arrives (the landing's own state takes over from here)
            var fallc = ASBOMixamo.ClipFrom("Falling Idle", "MX_FallIdle");
            if (fallc != null)
            {
                var fs = St("Fall", fallc, new Vector3(900, -40));
                var a = sm.AddAnyStateTransition(fs); a.AddCondition(AnimatorConditionMode.If, 0, "Fall"); a.hasExitTime = false; a.duration = 0.3f; a.canTransitionToSelf = false;
                var b = fs.AddTransition(move); b.AddCondition(AnimatorConditionMode.IfNot, 0, "Fall"); b.hasExitTime = false; b.duration = 0.12f;
            }
            // landing a drop on the run: a roll and up again; a trip; the run-up wall climb; reaching for something; being barged
            foreach (var (trig, file, cut, y, exit, inDur) in new[] {
                ("Roll", "Falling To Roll", "MX_Roll", 800f, 0.9f, 0.06f), ("Stumble", "Jogging Stumble", "MX_Stumble", 860f, 0.86f, 0.1f),
                ("WallClimb", "Sprint To Wall Climb", "MX_WallClimb", 920f, 0.95f, 0.1f), ("Take", "Taking Item", "MX_Take", 980f, 0.9f, 0.2f),
                ("Barged", "Hit To Body", "MX_Barged", 1040f, 0.85f, 0.08f), ("Throw", "Baseball Pitching", "MX_Throw", 1100f, 0.82f, 0.08f) })
            {
                var c = ASBOMixamo.ClipFrom(file, cut); if (c == null) { Debug.LogWarning("[ASBO] no " + cut); continue; }
                var st = St(trig, c, new Vector3(900, y));
                var a = sm.AddAnyStateTransition(st); a.AddCondition(AnimatorConditionMode.If, 0, trig); a.hasExitTime = false; a.duration = inDur; a.canTransitionToSelf = false;
                var b = st.AddTransition(move); b.hasExitTime = true; b.exitTime = exit; b.duration = 0.22f;
            }
            // pulling up from a run: the last strides braking, not a skid to a standstill (let go if you set off again)
            var rstop = ASBOMixamo.ClipFrom("Run To Stop", "MX_RunToStop");
            if (rstop != null)
            {
                var rs = St("RunStop", rstop, new Vector3(300, -180));
                var a = move.AddTransition(rs); a.AddCondition(AnimatorConditionMode.If, 0, "RunStop"); a.hasExitTime = false; a.duration = 0.12f;
                var b = rs.AddTransition(move); b.hasExitTime = true; b.exitTime = 0.9f; b.duration = 0.2f;
                var c = rs.AddTransition(move); c.AddCondition(AnimatorConditionMode.If, 0, "Stick"); c.hasExitTime = false; c.duration = 0.2f;
            }
            // Foot IK on every state: feet go back to where the original mocap put them (fixes wide stance after retargeting onto UMA)
            foreach (var cs in sm.states) cs.state.iKOnFeet = ik;
            if (ik) { var ls = ac.layers; ls[0].iKPass = true; ac.layers = ls; } // ground-aware feet (FootGround.OnAnimatorIK)
            if (!ik) { EditorUtility.SetDirty(ac); return; }
            AddFidgets(ac);
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
            ASBOEstate.Build(); // Hawthorn Way goes back in whenever the test scene is rebuilt
        }
    }
}
#endif
