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
        public static string F(string n) => $"{Dir}/X Bot@{n}.fbx";

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
            var sb = new System.Text.StringBuilder();
            foreach (var path in Directory.GetFiles(Dir, "*.fbx").Select(p => p.Replace('\\', '/')).OrderBy(p => p))
            {
                var c = Raw(path); if (c == null) { Debug.LogWarning("[ASBO] mixamo: no clip in " + path); continue; }
                sb.AppendLine(Summary(Path.GetFileNameWithoutExtension(path).Replace("X Bot@", ""), c));
            }
            File.WriteAllText("Library/mixamo_summary.txt", sb.ToString());
            Debug.Log("[ASBO] mixamo: summary written to Library/mixamo_summary.txt");
        }

        /// <summary>
        /// One line per take: length, how far the root travels (and its speed), how well the end matches the start
        /// (a loop), and when the arms and legs are fastest (where a punch or kick lands), plus how low the hips go.
        /// </summary>
        public static string Summary(string name, AnimationClip c)
        {
            var (speed, dz) = RootSpeed(c);
            var all = Muscles(c, "Leg", "Foot", "Arm", "Forearm", "Spine", "Chest", "Neck", "Head").Select(m => Curve(c, m)).Where(k => k != null).ToArray();
            float loopDiff = 0; foreach (var k in all) loopDiff += Mathf.Abs(k.Evaluate(0) - k.Evaluate(c.length)); loopDiff /= Mathf.Max(1, all.Length);
            (float t, float v) Peak(params string[] parts)
            {
                var ks = Muscles(c, parts).Select(m => Curve(c, m)).Where(k => k != null).ToArray(); float bt = 0, bv = 0, dt = 1f / 30f;
                for (float t = 0; t + dt <= c.length; t += dt) { float v = 0; foreach (var k in ks) v += Mathf.Abs(k.Evaluate(t + dt) - k.Evaluate(t)); v /= dt; if (v > bv) { bv = v; bt = t; } }
                return (bt, bv);
            }
            var arm = Peak("Right Arm", "Right Forearm", "Left Arm", "Left Forearm"); var leg = Peak("Right Upper Leg", "Right Lower Leg", "Left Upper Leg", "Left Lower Leg");
            var y = Curve(c, "RootT.y"); float yMin = 9f, y0 = y != null ? y.Evaluate(0) : 0f, y1 = y != null ? y.Evaluate(c.length) : 0f;
            if (y != null) for (float t = 0; t <= c.length; t += 0.05f) yMin = Mathf.Min(yMin, y.Evaluate(t));
            return $"{name}|len {c.length:F2}|root {speed:F2}m/s {dz:F2}m|loop {loopDiff:F3}|arm {arm.t:F2}s {arm.v:F0}|leg {leg.t:F2}s {leg.v:F0}|hipY {y0:F2}->{y1:F2} min {yMin:F2}";
        }

        static AnimationClip Raw(string path) { var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray(); return all.FirstOrDefault(c => c.name == "FULL") ?? all.FirstOrDefault(c => !c.name.StartsWith("MX")) ?? all.FirstOrDefault(); }

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

        /// <summary>
        /// Play every take on the X Bot skeleton and measure where the fists and feet go relative to the hips: when each
        /// reaches furthest forward (a punch or kick landing) and how far, so the moves can be cut round the hit and the
        /// game knows when it lands. Writes Library/mixamo_strikes.txt.
        /// </summary>
        [MenuItem("ASBO/Mixamo/3 Measure Strikes")]
        public static void MeasureStrikes()
        {
            var sb = new System.Text.StringBuilder();
            AnimationMode.StartAnimationMode();
            try
            {
                foreach (var path in Directory.GetFiles(Dir, "*.fbx").Select(p => p.Replace('\\', '/')).OrderBy(p => p))
                {
                    var c = Raw(path); var model = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (c == null || model == null) continue;
                    var go = Object.Instantiate(model); go.hideFlags = HideFlags.HideAndDontSave;
                    var a = go.GetComponent<Animator>(); if (a == null || !a.isHuman) { Object.DestroyImmediate(go); continue; }
                    var hips = a.GetBoneTransform(HumanBodyBones.Hips); var chest = a.GetBoneTransform(HumanBodyBones.UpperChest) ?? a.GetBoneTransform(HumanBodyBones.Chest);
                    var limbs = new[] { HumanBodyBones.RightHand, HumanBodyBones.LeftHand, HumanBodyBones.RightFoot, HumanBodyBones.LeftFoot, HumanBodyBones.Head }.Select(a.GetBoneTransform).ToArray();
                    // how far each fist is from the chest, each foot from the hips, the head from the hips (straightened limbs = a strike)
                    float Ext(int i) => Vector3.Distance(limbs[i].position, i < 2 ? chest.position : hips.position);
                    AnimationMode.SampleAnimationClip(go, c, 0f);
                    var base0 = Enumerable.Range(0, 5).Select(Ext).ToArray();
                    var best = new float[5]; var bestT = new float[5]; var bestH = new float[5];
                    float moveA = -1f, moveB = 0f; var prev = limbs.Select(l => l.position).ToArray();
                    for (float t = 0f; t <= c.length; t += 1f / 30f)
                    {
                        AnimationMode.SampleAnimationClip(go, c, t);
                        float sp = 0f;
                        for (int i = 0; i < 5; i++)
                        {
                            float r = Ext(i) - base0[i];
                            if (r > best[i]) { best[i] = r; bestT[i] = t; bestH[i] = limbs[i].position.y - go.transform.position.y; }
                            sp = Mathf.Max(sp, (limbs[i].position - prev[i]).magnitude * 30f); prev[i] = limbs[i].position;
                        }
                        if (t > 0f && sp > 1.2f) { if (moveA < 0f) moveA = t; moveB = t; }
                    }
                    sb.AppendLine($"{Path.GetFileNameWithoutExtension(path).Replace("X Bot@", "")}|len {c.length:F2}|moves {moveA:F2}-{moveB:F2}|R {best[0]:F2}@{bestT[0]:F2} h{bestH[0]:F2}|L {best[1]:F2}@{bestT[1]:F2} h{bestH[1]:F2}|RF {best[2]:F2}@{bestT[2]:F2} h{bestH[2]:F2}|LF {best[3]:F2}@{bestT[3]:F2} h{bestH[3]:F2}|head {best[4]:F2}@{bestT[4]:F2}");
                    Object.DestroyImmediate(go);
                }
            }
            finally { AnimationMode.StopAnimationMode(); }
            File.WriteAllText("Library/mixamo_strikes.txt", sb.ToString());
            Debug.Log("[ASBO] mixamo: strikes measured -> Library/mixamo_strikes.txt");
        }

        static readonly string[] Timed = {
            "Lead Jab", "Cross Punch", "Punching", "Hook", "Hook (1)", "Hook Punch", "Right Hook", "Uppercut", "Headbutt", "Headbutt (1)", "Illegal Elbow Punch", "Illegal Elbow Punch (1)",
            "Illegal Knee", "Knee Jab", "Knee Jabs To Uppercut", "Kicking", "Flying Kick", "Stomp", "Standing Melee Attack Backhand", "Fist Fight A",
            "Head Hit", "Big Hit To Head", "Receive Uppercut To The Face", "Receive Uppercut To The Face (1)", "Receive Uppercut To The Face (2)", "Hit To Body", "Rib Hit", "Big Stomach Hit",
            "Stunned", "Dodging", "Dodging Back", "Body Block", "Running (1)", "Walking (1)", "Swagger Walk", "Idle (1)" };

        static readonly string[] Traced = { "Head Hit", "Big Hit To Head", "Receive Uppercut To The Face", "Receive Uppercut To The Face (1)", "Hit To Body", "Rib Hit", "Big Stomach Hit", "Stunned", "Dodging Back" };

        /// <summary>
        /// For each fight take: every fist, foot, knee and the head tracked along the facing direction (metres in front of
        /// where the character stands). Per limb: rest, furthest forward and when (the moment a strike lands), when it
        /// starts and finishes moving (half way out and back). For hits taken: when the head is knocked furthest back.
        /// Writes Library/mixamo_timing.txt.
        /// </summary>
        [MenuItem("ASBO/Mixamo/4 Time Fight Moves")]
        public static void TimeMoves()
        {
            var sb = new System.Text.StringBuilder();
            AnimationMode.StartAnimationMode();
            try
            {
                foreach (var name in Timed)
                {
                    var path = F(name); var c = Raw(path); var model = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (c == null || model == null) { sb.AppendLine(name + "|missing"); continue; }
                    var go = Object.Instantiate(model); go.hideFlags = HideFlags.HideAndDontSave;
                    var a = go.GetComponent<Animator>(); if (a == null || !a.isHuman) { Object.DestroyImmediate(go); continue; }
                    var bones = new[] { HumanBodyBones.RightHand, HumanBodyBones.LeftHand, HumanBodyBones.RightFoot, HumanBodyBones.LeftFoot, HumanBodyBones.RightLowerLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.Head, HumanBodyBones.Hips };
                    string[] tag = { "RH", "LH", "RF", "LF", "RK", "LK", "Hd", "Hp" };
                    var tr = bones.Select(a.GetBoneTransform).ToArray();
                    int n = Mathf.FloorToInt(c.length * 30f) + 1; var z = new float[8, n]; var y = new float[8, n];
                    for (int f = 0; f < n; f++)
                    {
                        AnimationMode.SampleAnimationClip(go, c, Mathf.Min(c.length, f / 30f));
                        for (int i = 0; i < 8; i++) { var q = tr[i].position - go.transform.position; z[i, f] = q.z; y[i, f] = q.y; }
                    }
                    sb.Append($"{name}|len {c.length:F2}");
                    for (int i = 0; i < 8; i++)
                    {
                        float rest = z[i, 0], mx = -9f, mn = 9f; int fm = 0, fn = 0;
                        for (int f = 0; f < n; f++) { if (z[i, f] > mx) { mx = z[i, f]; fm = f; } if (z[i, f] < mn) { mn = z[i, f]; fn = f; } }
                        float half = rest + (mx - rest) * 0.5f; int on = fm, off = fm;
                        while (on > 0 && z[i, on] > half) on--; while (off < n - 1 && z[i, off] > half) off++;
                        sb.Append($"|{tag[i]} {rest:F2}>{mx:F2}@{fm / 30f:F2} y{y[i, fm]:F2} [{on / 30f:F2}-{off / 30f:F2}] min {mn:F2}@{fn / 30f:F2}");
                    }
                    sb.AppendLine();
                    if (Traced.Contains(name))
                    {
                        // head forward/height and hips forward/height every 0.1 s (when a hit lands: the head snaps)
                        sb.Append("  trace hd z/y hp z/y:");
                        for (int f = 0; f < n; f += 3) sb.Append($" {f / 30f:F1}:{z[6, f]:F2}/{y[6, f]:F2}/{z[7, f]:F2}/{y[7, f]:F2}");
                        sb.AppendLine();
                    }
                    Object.DestroyImmediate(go);
                }
            }
            finally { AnimationMode.StopAnimationMode(); }
            File.WriteAllText("Library/mixamo_timing.txt", sb.ToString());
            Debug.Log("[ASBO] mixamo: fight moves timed -> Library/mixamo_timing.txt");
        }

        [MenuItem("ASBO/Mixamo/2 Cut Clips")]
        public static void CutAll()
        {
            Cut("Walking", "MX_Walk", -1f, -1f, true);
            Cut("Walking (1)", "MX_Walk1", -1f, -1f, true);
            Cut("Running", "MX_Run", -1f, -1f, true);
            Cut("Running (1)", "MX_Run1", -1f, -1f, true);
            for (int i = 0; i < IdleFiles.Length; i++) Cut(IdleFiles[i], "MX_Idle" + i, -1f, -1f, true);
            Cut("Body Block", "MX_Stance", 2.1f, 3.35f, true);
            Cut("Evading A Threat", "MX_Evade", -1f, -1f, false);
            Cut("Falling Down", "MX_FallDown", -1f, -1f, false);
            Cut("Zombie Stand Up", "MX_StandUp", -1f, -1f, false);
            Cut("Walk Backwards", "MX_WalkBack", -1f, -1f, true);
            Cut("Honking Horn", "MX_Drive", 3.0f, 4.3f, true); // sat at the wheel, hands on it (between the toots)
            Cut("Crouching Idle", "MX_CrouchIdle", -1f, -1f, true);
            Cut("Crouch Walk", "MX_CrouchWalk", -1f, -1f, true);
            Cut("Climbing To Top", "MX_ClimbUp", -1f, -1f, false);
            Cut("Jostled Passenger", "MX_Sit", 0.5f, 4.5f, true); // sat on a bench
            Cut("Jostled Passenger", "MX_SitB", 1.2f, 5.2f, true); // the same seat, a different stretch of it (people sat side by side don't match)
            Cut("Sneaking Forward", "MX_Sneak", -1f, -1f, true);    // the quicker hunched sneak (the crouch walk is the slow one)
            Cut("Standing To Crouched", "MX_CrouchDown", -1f, -1f, false);
            Cut("Crouched To Standing", "MX_CrouchUp", -1f, -1f, false);
            Cut("Hard Landing", "MX_HardLand", 0.5f, 2.2f, false);  // from touchdown: knees buckle, hand to the floor, up again
            Cut("Run To Stop", "MX_RunToStop", -1f, -1f, false);
            Cut("Look Over Shoulder", "MX_LookBack", -1f, -1f, false);
            Cut("Pulled From Seat", "MX_PulledOut", -1f, -1f, false); // dragged out of the driver's seat
            Cut("Stomp", "MX_Stomp", 2.3f, 3.73f, false);           // stamping on someone who's down (the boot lands about 0.8 s in)
            Cut("Pull Pilot From Seat", "MX_Carjack", 5.4f, 9.43f, false); // reaching in and dragging the driver out
            Drop("Uppercut", "MX_Uppercut"); // the whole take again under another name: Unity refuses the copy (now cut as a Hay version)
            // cuts left in a file from an older list of takes (a take moved to another file keeps its old copy here,
            // and Unity drops the newer cut if it's the same stretch of the same take): out they go
            foreach (var file in ASBO.Slice.FightSet.Takes.Select(t => t.file).Distinct())
            {
                var mi = (ModelImporter)AssetImporter.GetAtPath(F(file)); if (mi == null) continue;
                var stale = mi.clipAnimations.Where(x => x.name.StartsWith("MXF_") && !Enumerable.Range(0, ASBO.Slice.FightSet.Takes.Length).Any(k => ASBO.Slice.FightSet.Takes[k].file == file && FightClipName(k) == x.name)).Select(x => x.name).ToList();
                if (stale.Count == 0) continue;
                mi.clipAnimations = mi.clipAnimations.Where(x => !stale.Contains(x.name)).ToArray(); mi.SaveAndReimport();
                Debug.Log($"[ASBO] mixamo: dropped old cuts from {file}: {string.Join(", ", stale)}");
            }
            // every version of every fight move and hit reaction (FightSet.Takes): cut round the blow
            for (int k = 0; k < ASBO.Slice.FightSet.Takes.Length; k++) { var t = ASBO.Slice.FightSet.Takes[k]; if (File.Exists(F(t.file))) Cut(t.file, FightClipName(k), t.a, t.b, false); }
            cache = null;
            foreach (var (file, n) in new[] { ("Walking", "MX_Walk"), ("Walking (1)", "MX_Walk1"), ("Running", "MX_Run"), ("Running (1)", "MX_Run1"), ("Body Block", "MX_Stance") })
            {
                var c = ClipFrom(file, n);
                Debug.Log($"[ASBO] mixamo cut {n}: {(c != null ? $"{c.length:F2}s loop {c.isLooping}, travels {c.averageSpeed.magnitude:F2} m/s, drifts {Drift(c):F2} m in the pose" : "missing")}");
            }
        }

        /// <summary>The standing-about idles (each person gets one of these or the pack's own).</summary>
        public static readonly string[] IdleFiles = { "Idle", "Idle (1)", "Idle (2)", "Idle (3)" };
        public static string FightClipName(int k) => "MXF_" + ASBO.Slice.FightSet.Takes[k].trig + "_" + k;

        /// <summary>How far the hips wander from where the character stands over the clip (should be ~0: played in place).</summary>
        public static float Drift(AnimationClip c)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(F("Walking")); if (model == null || c == null) return -1f;
            var go = Object.Instantiate(model); go.hideFlags = HideFlags.HideAndDontSave; float d = 0f;
            AnimationMode.StartAnimationMode();
            try
            {
                // relative to the character's own spot (sampling moves the object itself by the root motion, which the game ignores)
                var hips = go.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips);
                AnimationMode.SampleAnimationClip(go, c, 0f); var p0 = go.transform.InverseTransformPoint(hips.position);
                for (float t = 0.1f; t <= c.length; t += 0.1f) { AnimationMode.SampleAnimationClip(go, c, t); var dd = go.transform.InverseTransformPoint(hips.position) - p0; dd.y = 0f; d = Mathf.Max(d, dd.magnitude); }
            }
            finally { AnimationMode.StopAnimationMode(); Object.DestroyImmediate(go); }
            return d;
        }

        /// <summary>
        /// Where in its cycle a walk or run has the left foot furthest forward (0..1): used to line the Mixamo cycles up
        /// with the pack's sideways and diagonal ones so blending between them never crosses the feet.
        /// </summary>
        public static float Phase(AnimationClip c)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(F("Walking")); if (model == null || c == null) return 0f;
            var go = Object.Instantiate(model); go.hideFlags = HideFlags.HideAndDontSave; float best = -9f, bt = 0f;
            AnimationMode.StartAnimationMode();
            try
            {
                var a = go.GetComponent<Animator>(); var hips = a.GetBoneTransform(HumanBodyBones.Hips); var lf = a.GetBoneTransform(HumanBodyBones.LeftFoot);
                for (int i = 0; i < 60; i++)
                {
                    float t = i / 60f; AnimationMode.SampleAnimationClip(go, c, t * c.length);
                    float z = go.transform.InverseTransformPoint(lf.position).z - go.transform.InverseTransformPoint(hips.position).z;
                    if (z > best) { best = z; bt = t; }
                }
            }
            finally { AnimationMode.StopAnimationMode(); Object.DestroyImmediate(go); }
            return bt;
        }

        /// <summary>
        /// Cut the takes into the named clips (ranges in seconds; loops get Unity's loop-pose match). Played in place:
        /// the take's travel goes to root motion, which the game ignores (it moves people itself), so the body stays
        /// over its feet. The whole take stays in the file too (as "FULL") for the measuring tools.
        /// </summary>
        public static void Cut(string file, string clip, float a, float b, bool loop, bool bakeXZ = false)
        {
            var path = F(file); var mi = (ModelImporter)AssetImporter.GetAtPath(path); if (mi == null) { Debug.LogWarning("[ASBO] mixamo: no " + path); return; }
            var src = mi.defaultClipAnimations[0];
            float fps = 30f; var raw = Raw(path); if (raw != null && raw.frameRate > 1f) fps = raw.frameRate;
            var list = mi.clipAnimations.Where(x => x.name != clip).ToList();
            if (!list.Any(x => x.name == "FULL")) { var full = mi.defaultClipAnimations[0]; full.name = "FULL"; list.Insert(0, full); }
            list.Add(new ModelImporterClipAnimation
            {
                name = clip, takeName = src.takeName, firstFrame = a < 0 ? src.firstFrame : a * fps, lastFrame = b < 0 ? src.lastFrame : Mathf.Min(src.lastFrame, b * fps),
                loopTime = loop, loopPose = loop, lockRootRotation = true, keepOriginalOrientation = true,
                lockRootHeightY = true, keepOriginalPositionY = true, lockRootPositionXZ = bakeXZ, keepOriginalPositionXZ = bakeXZ
            });
            mi.clipAnimations = list.ToArray(); mi.SaveAndReimport();
        }

        /// <summary>Take a cut back out of a file.</summary>
        public static void Drop(string file, string clip)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(F(file)); if (mi == null || !mi.clipAnimations.Any(x => x.name == clip)) return;
            mi.clipAnimations = mi.clipAnimations.Where(x => x.name != clip).ToArray(); mi.SaveAndReimport();
        }

        static Dictionary<string, AnimationClip> cache;
        /// <summary>A clip cut from one take.</summary>
        public static AnimationClip ClipFrom(string file, string name) => AssetDatabase.LoadAllAssetsAtPath(F(file)).OfType<AnimationClip>().FirstOrDefault(x => x.name == name);

        public static AnimationClip ClipNamed(string name)
        {
            if (cache == null)
            {
                cache = new Dictionary<string, AnimationClip>();
                foreach (var path in Directory.GetFiles(Dir, "*.fbx"))
                    foreach (var c in AssetDatabase.LoadAllAssetsAtPath(path.Replace('\\', '/')).OfType<AnimationClip>())
                        if (!c.name.StartsWith("__preview__") && !cache.ContainsKey(c.name)) cache[c.name] = c;
            }
            return cache.TryGetValue(name, out var r) && r != null ? r : null;
        }
    }
}
#endif
