using UnityEngine;
using UnityEngine.AI;

namespace ASBO.Slice
{
    /// <summary>
    /// The ankle tag. You're on licence: the tag keeps you inside the zone (the playable area). Near the edge your phone
    /// buzzes and tells you how close you are; step outside and it's a violation, with a countdown before the tag phones
    /// it in. Hanging about right at the edge gets phoned in too. Driving doesn't get you out of it.
    /// </summary>
    public class Tag : MonoBehaviour
    {
        public static Tag I;
        public static int Violations;
        public static Rect Zone; static bool haveZone;
        /// <summary>Switched off for now (you asked): no warnings, no violations, nothing on screen. Flip back on for the story.</summary>
        public static bool Enabled = false;
        public static float EdgeDist { get; private set; } = 999f;
        public static bool Outside { get; private set; }
        public static float Countdown { get; private set; }
        const float Warn = 15f, Report = 20f, Linger = 35f;

        PlayerCtl pc; float buzzT, outsideT, lingerT; bool reported; AudioSource beep;

        public static void Create(Transform parent) { if (I != null) return; var g = new GameObject("Tag"); g.transform.SetParent(parent, false); I = g.AddComponent<Tag>(); }

        void Start()
        {
            pc = FindFirstObjectByType<PlayerCtl>();
            // the zone: the estate (every house and its gardens, the lane, the main road out front) with room to roam,
            // or failing that where people can walk
            if (Estate.Houses.Count > 0)
            {
                Vector3 mn = Vector3.one * 1e9f, mx = -Vector3.one * 1e9f;
                foreach (var h in Estate.Houses) if (h != null && h.root != null) { mn = Vector3.Min(mn, h.root.position); mx = Vector3.Max(mx, h.root.position); }
                mn.z = Mathf.Min(mn.z, World.RoadZ); mx.z = Mathf.Max(mx.z, World.RoadZ);
                Zone = Rect.MinMaxRect(mn.x - 45f, mn.z - 40f, mx.x + 45f, mx.z + 40f);
            }
            else
            {
                var tri = NavMesh.CalculateTriangulation();
                if (tri.vertices != null && tri.vertices.Length > 0)
                {
                    Vector3 mn = tri.vertices[0], mx = tri.vertices[0];
                    foreach (var v in tri.vertices) { mn = Vector3.Min(mn, v); mx = Vector3.Max(mx, v); }
                    Zone = Rect.MinMaxRect(mn.x + 4f, mn.z + 4f, mx.x - 4f, mx.z - 4f);
                }
            }
            haveZone = Zone.width > 20f && Zone.height > 20f;
            Debug.Log($"[ASBO] tag zone {Zone.xMin:F0}..{Zone.xMax:F0} x {Zone.yMin:F0}..{Zone.yMax:F0}");
            beep = gameObject.AddComponent<AudioSource>(); beep.spatialBlend = 0f; beep.volume = 0.5f; beep.playOnAwake = false;
            beep.clip = MakeBeep();
        }

        static AudioClip MakeBeep()
        {
            int rate = 22050, n = rate / 4; var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)rate; bool on = t < 0.07f || (t > 0.12f && t < 0.19f); d[i] = on ? Mathf.Sin(t * 2f * Mathf.PI * 1760f) * 0.35f * (1f - Mathf.Repeat(t, 0.12f) / 0.12f * 0.3f) : 0f; }
            var c = AudioClip.Create("tagbeep", n, 1, rate, false); c.SetData(d, 0); return c;
        }

        void Update()
        {
            if (pc == null) { pc = FindFirstObjectByType<PlayerCtl>(); return; }
            if (!haveZone || !Enabled) { Outside = false; EdgeDist = 999f; Countdown = 0f; return; }
            Vector3 p = pc.transform.position;
            float dx = Mathf.Min(p.x - Zone.xMin, Zone.xMax - p.x), dz = Mathf.Min(p.z - Zone.yMin, Zone.yMax - p.z);
            EdgeDist = Mathf.Min(dx, dz);
            bool wasOut = Outside; Outside = EdgeDist < 0f;
            if (Outside && !wasOut) { World.Toast("TAG VIOLATION: you have left the zone. Turn back.", 1); Buzz(true); outsideT = 0f; }
            if (!Outside && wasOut) { Countdown = 0f; if (!reported) World.Toast("Back inside the zone.", 2); }
            if (Outside)
            {
                outsideT += Time.deltaTime; Countdown = Mathf.Max(0f, Report - outsideT);
                buzzT -= Time.deltaTime; if (buzzT <= 0f) { buzzT = 1.2f; Buzz(true); }
                if (outsideT >= Report && !reported) PhoneIn();
            }
            else if (EdgeDist < Warn)
            {
                buzzT -= Time.deltaTime; if (buzzT <= 0f) { buzzT = Mathf.Lerp(1.2f, 3.5f, EdgeDist / Warn); Buzz(false); }
                lingerT = EdgeDist < 4f ? lingerT + Time.deltaTime : Mathf.Max(0f, lingerT - Time.deltaTime);
                if (lingerT > Linger && !reported) PhoneIn();
            }
            else { lingerT = 0f; if (reported && EdgeDist > Warn + 10f) reported = false; }
        }

        void Buzz(bool hard)
        {
            if (beep != null && beep.clip != null) { beep.pitch = hard ? 1f : 0.85f; beep.Play(); }
#if UNITY_ANDROID || UNITY_IOS
            if (hard) Handheld.Vibrate();
#endif
        }

        void PhoneIn()
        {
            reported = true; Violations++;
            World.Toast("The tag has phoned it in. Somebody official has been told you are loitering at the edge.", 1);
        }

        /// <summary>The line for the screen: how close you are, or the violation countdown.</summary>
        public static string HudLine => !haveZone || !Enabled ? null : Outside ? "TAG ZONE: return to the area" + (Countdown > 0f ? "  ·  reported in " + Mathf.CeilToInt(Countdown) + "s" : "") : EdgeDist < Warn ? Mathf.CeilToInt(EdgeDist) + " m to the edge of the zone" : null;
    }
}
