using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// People cost a lot to draw (each is about 60,000 triangles), so far-off ones aren't drawn and only those close by cast
    /// shadows. Phones draw people out to 55 m and shadow them within 16 m (less when the phone is struggling); the editor
    /// and computers go much further. Everything they do carries on; it's only the drawing that stops.
    /// </summary>
    public class BodyLOD : MonoBehaviour
    {
        readonly List<SkinnedMeshRenderer> rs = new List<SkinnedMeshRenderer>();
        readonly Dictionary<SkinnedMeshRenderer, ShadowCastingMode> own = new Dictionary<SkinnedMeshRenderer, ShadowCastingMode>();
        float next; bool hidden, shadowed = true;

        static float Hide => Application.isMobilePlatform ? 55f - 8f * (Perf.I != null ? Perf.I.Level : 0) : 140f;
        static float Shade => Application.isMobilePlatform ? 16f - 3f * (Perf.I != null ? Perf.I.Level : 0) : 45f;

        void Gather()
        {
            rs.Clear(); GetComponentsInChildren(true, rs);
            foreach (var r in rs) if (!own.ContainsKey(r)) own[r] = r.shadowCastingMode;
        }

        void Update()
        {
            if (Time.time < next) return; next = Time.time + 0.35f + Random.value * 0.25f;
            var cam = Camera.main; if (cam == null) return;
            if (rs.Count == 0 || rs.Exists(r => r == null)) { Gather(); hidden = false; shadowed = true; }
            float d = Vector3.Distance(cam.transform.position, transform.position);
            bool hide = d > Hide, shade = d < Shade;
            if (hide != hidden) { hidden = hide; foreach (var r in rs) if (r != null) r.forceRenderingOff = hide; }
            if (shade != shadowed)
            {
                shadowed = shade;
                foreach (var r in rs) if (r != null && own.TryGetValue(r, out var m) && m != ShadowCastingMode.Off) r.shadowCastingMode = shade ? m : ShadowCastingMode.Off;
            }
        }

        void OnDisable() { foreach (var r in rs) if (r != null) r.forceRenderingOff = false; hidden = false; }
    }
}
