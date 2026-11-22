using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Only the few lamps, fires and room lights nearest the camera are real lights; the rest switch off (their glowing
    /// shades and flames still show). Every real light costs on every object it touches, so this keeps the cost flat
    /// however many lit rooms and fires the town gets. Phones: the nearest 6 within 40 m; computers: 16 within 80 m.
    /// Lights on cars, the ambulance and other moving things are left alone.
    /// </summary>
    public class LightBudget : MonoBehaviour
    {
        public static LightBudget I;
        readonly List<Light> lights = new List<Light>();
        readonly List<(float d, Light l)> near = new List<(float, Light)>();
        float next, gather;

        public static void Create(Transform parent) { var g = new GameObject("LightBudget"); g.transform.SetParent(parent, false); I = g.AddComponent<LightBudget>(); }

        static int Max => Application.isMobilePlatform ? 6 - (Perf.I != null ? Perf.I.Level : 0) : 16;
        static float Reach => Application.isMobilePlatform ? 40f : 80f;

        void Gather()
        {
            lights.Clear();
            foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional) continue;
                if (l.GetComponentInParent<Rigidbody>() != null || l.GetComponentInParent<IVehicle>() != null || l.GetComponentInParent<Person>() != null) continue; // moving things keep their own
                lights.Add(l);
            }
        }

        void Update()
        {
            if (Time.time > gather) { gather = Time.time + 10f; Gather(); } // new lights (a house lit up, a fire) are picked up
            if (Time.time < next) return; next = Time.time + 0.3f;
            var cam = Camera.main; if (cam == null) return;
            Vector3 c = cam.transform.position; float reach2 = Reach * Reach;
            near.Clear();
            for (int i = lights.Count - 1; i >= 0; i--)
            {
                var l = lights[i]; if (l == null) { lights.RemoveAt(i); continue; }
                if (!l.gameObject.activeInHierarchy) continue;
                float d = (l.transform.position - c).sqrMagnitude - l.range * l.range * 0.25f;
                if (d < reach2) near.Add((d, l)); else if (l.enabled) l.enabled = false;
            }
            near.Sort((a, b) => a.d.CompareTo(b.d));
            int max = Max;
            for (int i = 0; i < near.Count; i++) { bool on = i < max; if (near[i].l.enabled != on) near[i].l.enabled = on; }
        }
    }
}
