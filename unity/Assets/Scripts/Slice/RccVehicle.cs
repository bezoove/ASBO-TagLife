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
            var b = CarBody.Hatch; // the demo car's short wheelbase suits a hatchback
            float L = (zF - zR) / (b.wheelZ1 - b.wheelZ0);
            float wAtWheel = 0.85f - 0.1f;
            var spec = new CarBody.Spec { L = L, u = b.u, belt = b.belt, top = b.top, W = b.W.Select(x => x * track / wAtWheel).ToArray(), wheelR = r, wheelZ0 = b.wheelZ0, wheelZ1 = b.wheelZ1, tumble = b.tumble };
            foreach (var mr in GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = false;
            // RCC's motion-blur wheel discs get switched back on by the controller: remove them outright
            foreach (var t in GetComponentsInChildren<Transform>(true).Where(t => t != transform && t.name.ToLowerInvariant().Contains("blur")).ToList()) t.gameObject.SetActive(false);
            foreach (var w in wcs) { var f = w.GetType().GetField("wheelBlurRenderers", F); if (f != null && f.FieldType.IsArray) f.SetValue(w, System.Array.CreateInstance(f.FieldType.GetElementType(), 0)); }
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.enabled = false;
            var vis = new GameObject("ASBO_Body").transform; vis.SetParent(transform, false);
            float zc = (zF + zR) / 2f - ((b.wheelZ0 + b.wheelZ1) / 2f - 0.5f) * L;
            vis.localPosition = new Vector3(0f, groundY, zc); vis.localRotation = Quaternion.identity;
            var ours = CarBody.Build(vis, spec, paint, true, 4242);
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
