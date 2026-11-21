using System.Linq;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>Lets the game's people react to the RCC car like any other vehicle.</summary>
    public class RccVehicle : MonoBehaviour, IVehicle
    {
        Rigidbody rb;
        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            // RCC Lite's exhaust smoke throws every frame under URP in this Unity version: switch it off for now
            foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true)) if (mb != null && mb.GetType().Name == "RCCP_Exhaust") { mb.enabled = false; mb.gameObject.SetActive(false); }
            // ...and so do its skidmarks (NullReference inside RCCP_Skidmarks.AddSkidMark): no skid marks until that's sorted
            foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null || mb.GetType().Name != "RCCP_WheelCollider") continue;
                var t = mb.GetType(); const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                var f = t.GetField("drawSkid", F); if (f != null && f.FieldType == typeof(bool)) { f.SetValue(mb, false); continue; }
                var pr = t.GetProperty("drawSkid", F); if (pr != null && pr.CanWrite) pr.SetValue(mb, false);
            }
        }
        // RCC switches itself to its own on-screen controller on phones (and in the editor when the target is Android),
        // which ignores our touch controls. Our buttons drive a virtual gamepad, so keep RCC on gamepad/keyboard input.
        static System.Reflection.FieldInfo mobF; static System.Reflection.PropertyInfo mobP; static object rccSettings; static bool mobLooked;
        static void KeepGamepadInput()
        {
            if (!mobLooked)
            {
                mobLooked = true;
                const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    var t = asm.GetType("RCCP_Settings"); if (t == null) continue;
                    rccSettings = t.GetProperty("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)?.GetValue(null);
                    mobF = t.GetField("mobileControllerEnabled", F);
                    if (mobF == null) mobP = t.GetProperty("mobileControllerEnabled", F);
                    var mc = t.GetField("mobileController", F); if (mc != null && rccSettings != null && mc.FieldType == typeof(bool)) mc.SetValue(rccSettings, false);
                    Debug.Log("[ASBO] RCC input: mobile controller " + (mobF != null ? "field" : mobP != null ? (mobP.CanWrite ? "property" : "read-only property") : "not found") + ", forcing gamepad/keyboard");
                    break;
                }
            }
            if (rccSettings == null) return;
            if (mobF != null) { if ((bool)mobF.GetValue(rccSettings)) mobF.SetValue(rccSettings, false); }
            else if (mobP != null && mobP.CanWrite && (bool)mobP.GetValue(rccSettings)) mobP.SetValue(rccSettings, false);
        }
        /// <summary>
        /// Set while the player is in the driving seat. We feed RCC our own inputs (keyboard, a real gamepad, or the
        /// touch buttons' virtual gamepad) instead of relying on its input manager, which ignores the touch pad.
        /// </summary>
        public bool driven;
        RCCP_Input rin; readonly RCCP_Inputs feed = new RCCP_Inputs(); bool overriding;
        void Update()
        {
            KeepGamepadInput();
            if (rin == null) rin = GetComponentInChildren<RCCP_Input>(true);
            if (rin == null) return;
            if (!driven)
            {
                if (overriding) { feed.Clear(); feed.handbrakeInput = 1f; rin.OverrideInputs(feed); rin.DisableOverrideInputs(); rin.overridePlayerInputs = false; overriding = false; }
                return;
            }
            float thr = 0f, brk = 0f, steer = 0f, hb = 0f;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) thr = 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) brk = 1f;
                steer += (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
                if (kb.spaceKey.isPressed) hb = 1f;
            }
            foreach (var pad in UnityEngine.InputSystem.Gamepad.all) // real pads and the on-screen one
            {
                thr = Mathf.Max(thr, pad.rightTrigger.ReadValue());
                brk = Mathf.Max(brk, pad.leftTrigger.ReadValue());
                float sx = pad.leftStick.ReadValue().x; if (Mathf.Abs(sx) > Mathf.Abs(steer)) steer = sx;
                if (pad.buttonSouth.isPressed) hb = 1f;
            }
            feed.throttleInput = thr; feed.brakeInput = brk; feed.steerInput = Mathf.Clamp(steer, -1f, 1f); feed.handbrakeInput = hb;
            rin.overridePlayerInputs = true; rin.OverrideInputs(feed); overriding = true;
        }

        public bool reskin = true;
        public Color paint = new Color(0.1f, 0.2f, 0.42f);

        void Start() { if (reskin) Reskin(); }

        /// <summary>
        /// Swap the pack's demo body (a real car) for one of our own procedural bodies, sized so its wheel arches
        /// sit over RCC's wheels. Our wheels ride on RCC's wheel models, so they spin, steer and compress as before.
        /// </summary>
        void Reskin()
        {
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var wcs = GetComponentsInChildren<MonoBehaviour>(true).Where(m => m != null && m.GetType().Name == "RCCP_WheelCollider").ToList();
            if (wcs.Count < 4) return;
            var models = wcs.Select(w => w.GetType().GetField("wheelModel", F)?.GetValue(w) as Transform).ToList();
            if (models.Any(m => m == null)) return;
            var uwc = wcs.Select(w => w.GetComponent<WheelCollider>()).FirstOrDefault(c => c != null);
            float r = uwc != null ? uwc.radius : 0.33f;
            var lp = models.Select(m => transform.InverseTransformPoint(m.position)).ToArray();
            float zF = lp.Max(p => p.z), zR = lp.Min(p => p.z), track = lp.Average(p => Mathf.Abs(p.x));
            float groundY = lp.Average(p => p.y) - r;
            var spec = CarBody.Get("hatch_a"); var cm = spec.m; // the estate's five-door hatch, scaled so its arches sit on RCC's wheels
            float scale = (zF - zR) / cm.wb;
            foreach (var mr in GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = false;
            // RCC's motion-blur wheel discs get switched back on by the controller: remove them outright
            foreach (var t in GetComponentsInChildren<Transform>(true).Where(t => t != transform && t.name.ToLowerInvariant().Contains("blur")).ToList()) t.gameObject.SetActive(false);
            foreach (var w in wcs) { var f = w.GetType().GetField("wheelBlurRenderers", F); if (f != null && f.FieldType.IsArray) f.SetValue(w, System.Array.CreateInstance(f.FieldType.GetElementType(), 0)); }
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.enabled = false;
            var vis = new GameObject("ASBO_Body").transform; vis.SetParent(transform, false);
            float zc = (zF + zR) / 2f - scale * (cm.wxF + cm.wxR) / 2f;
            vis.localPosition = new Vector3(0f, groundY, zc); vis.localRotation = Quaternion.identity; vis.localScale = Vector3.one * scale;
            var ours = CarBody.Build(vis, spec, paint, true, 4242, false);
            foreach (var w in ours)
            {
                Vector3 wl = transform.InverseTransformPoint(w.position);
                var model = models.OrderBy(m => (transform.InverseTransformPoint(m.position) - wl).sqrMagnitude).First();
                w.position = model.position;
                w.SetParent(model, true); // keeps our axle orientation; inherits RCC's spin, steer and suspension
            }
            Debug.Log($"[ASBO] RCC car reskinned: wheelbase {zF - zR:F2} m, track {track * 2f:F2} m, wheel r {r:F2}");
        }

        public float Speed => rb != null ? rb.linearVelocity.magnitude : 0f;
        public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
        public RCCP_CarController Car => GetComponent<RCCP_CarController>();
    }

}
