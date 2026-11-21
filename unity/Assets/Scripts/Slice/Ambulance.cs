using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>UK-style battenburg ambulance: drives to a casualty, crew treat them, load them on a trolley and take them to hospital.</summary>
    public class Ambulance : MonoBehaviour
    {
        public static string Status = "";
        static readonly HashSet<Person> dispatched = new HashSet<Person>();
        public static void ResetAll() { dispatched.Clear(); Status = ""; }

        Light blueA, blueB; Material lightOn, lightOff; Renderer[] barA, barB;

        public static void Dispatch(Person victim)
        {
            if (!dispatched.Add(victim)) return;
            var go = new GameObject("Ambulance");
            go.transform.SetParent(World.Root, false);
            var a = go.AddComponent<Ambulance>();
            a.BuildVehicle();
            a.StartCoroutine(a.Run(victim));
        }

        Transform[] wheels; float wheelSpin, wheelR = 0.33f; Vector3 lastPos;

        /// <summary>
        /// The old game's panel van, scaled up, in ambulance yellow with the green and yellow Battenburg chequer down the
        /// sides and across the back, a blue light bar, and AMBULANCE mirrored on the bonnet.
        /// </summary>
        void BuildVehicle()
        {
            const float S = 1.14f;
            var spec = CarBody.Get("van_a");
            var vis = new GameObject("Body").transform; vis.SetParent(transform, false); vis.localScale = Vector3.one * S;
            wheels = CarBody.Build(vis, spec, new Color(0.97f, 0.84f, 0.06f), true, 777, false);
            wheelR = spec.m.rad * S;
            var m = spec.m; float hw = m.hw * S, Le = m.L * 0.5f * S, roof = (m.gh.roof + m.gh.crown) * S;
            var yellow = Gfx.Mat(new Color(0.98f, 0.86f, 0.05f), null, 0.6f);
            var green = Gfx.Mat(new Color(0.03f, 0.5f, 0.24f), null, 0.6f);
            var white = Gfx.Mat(new Color(0.93f, 0.93f, 0.92f), null, 0.55f);
            // Battenburg: two rows of squares from the back to the front wheel arch, both sides
            float sq = 0.26f, zA = -Le + 0.18f, zB = (m.wxF - m.archR) * S - 0.05f; int n = Mathf.Max(4, Mathf.FloorToInt((zB - zA) / sq));
            for (int i = 0; i < n; i++)
                foreach (int sd in new[] { -1, 1 })
                    for (int r = 0; r < 2; r++)
                        Gfx.Box(transform, new Vector3(sd * (hw - 0.012f), 0.66f + r * sq, zA + (i + 0.5f) * sq), new Vector3(0.012f, sq, sq), ((i + r) % 2 == 0) ? green : yellow, 0, "Batten", false);
            for (int i = 0; i < 6; i++)
                for (int r = 0; r < 2; r++)
                    Gfx.Box(transform, new Vector3(-hw * 0.82f + (i + 0.5f) * (hw * 1.64f / 6f), 0.7f + r * 0.22f, -Le - 0.006f), new Vector3(hw * 1.64f / 6f, 0.22f, 0.012f), ((i + r) % 2 == 0) ? green : yellow, 0, "BattenRear", false);
            // light bar
            lightOn = Gfx.Emissive(new Color(0.1f, 0.3f, 1f), 4f); lightOff = Gfx.Mat(new Color(0.05f, 0.1f, 0.3f));
            float barZ = m.gh.fr * S - 0.25f;
            Gfx.Box(transform, new Vector3(0, roof + 0.03f, barZ), new Vector3(1.3f, 0.05f, 0.26f), white, 0, "BarBase", false);
            var a = Gfx.Box(transform, new Vector3(-0.36f, roof + 0.11f, barZ), new Vector3(0.62f, 0.11f, 0.22f), lightOff, 0, "BarA", false);
            var b = Gfx.Box(transform, new Vector3(0.36f, roof + 0.11f, barZ), new Vector3(0.62f, 0.11f, 0.22f), lightOff, 0, "BarB", false);
            var a2 = Gfx.Box(transform, new Vector3(-0.45f, roof - 0.05f, -Le + 0.05f), new Vector3(0.22f, 0.12f, 0.08f), lightOff, 0, "BarA2", false);
            var b2 = Gfx.Box(transform, new Vector3(0.45f, roof - 0.05f, -Le + 0.05f), new Vector3(0.22f, 0.12f, 0.08f), lightOff, 0, "BarB2", false);
            barA = new[] { a.GetComponent<Renderer>(), a2.GetComponent<Renderer>() }; barB = new[] { b.GetComponent<Renderer>(), b2.GetComponent<Renderer>() };
            blueA = MakeLight(new Vector3(-0.5f, roof + 0.4f, barZ)); blueB = MakeLight(new Vector3(0.5f, roof + 0.4f, barZ));
            // mirrored AMBULANCE on the bonnet front so drivers read it in their mirrors
            try
            {
                var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var tgo = new GameObject("Lettering"); tgo.transform.SetParent(transform, false);
                tgo.transform.localPosition = new Vector3(0, 0.98f, Le + 0.015f); tgo.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                tgo.transform.localScale = new Vector3(-0.07f, 0.07f, 0.07f);
                var tm = tgo.AddComponent<TextMesh>(); tm.text = "AMBULANCE"; tm.font = f; tm.fontSize = 64; tm.characterSize = 0.6f;
                tm.anchor = TextAnchor.MiddleCenter; tm.color = new Color(0.03f, 0.4f, 0.2f);
                tgo.GetComponent<MeshRenderer>().sharedMaterial = f.material;
            }
            catch { }
            foreach (var c in GetComponentsInChildren<Collider>()) Destroy(c);
            var au = gameObject.AddComponent<AudioSource>();
            au.clip = SirenClip(); au.loop = true; au.spatialBlend = 1f; au.minDistance = 10f; au.maxDistance = 160f; au.rolloffMode = AudioRolloffMode.Linear; au.volume = 0.5f;
            au.Play();
            lastPos = transform.position;
        }

        Light MakeLight(Vector3 p)
        {
            var g = new GameObject("Blue"); g.transform.SetParent(transform, false); g.transform.localPosition = p;
            var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(0.2f, 0.4f, 1f); l.range = 9f; l.intensity = 0f; l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForceVertex;
            return l;
        }

        static AudioClip siren;
        static AudioClip SirenClip()
        {
            if (siren != null) return siren;
            int sr = 22050, n = sr * 2; var d = new float[n]; float ph = 0;
            for (int i = 0; i < n; i++)
            {
                float f = ((i / (sr * 0.55f)) % 2f) < 1f ? 660f : 880f;
                ph += 2f * Mathf.PI * f / sr; d[i] = Mathf.Sign(Mathf.Sin(ph)) * 0.25f + Mathf.Sin(ph) * 0.25f;
            }
            siren = AudioClip.Create("siren", n, 1, sr, false); siren.SetData(d, 0); return siren;
        }

        void Update()
        {
            // wheels roll with however far we moved
            float moved = Vector3.Dot(transform.position - lastPos, transform.forward); lastPos = transform.position;
            wheelSpin += moved / Mathf.Max(0.2f, wheelR) * Mathf.Rad2Deg;
            if (wheels != null) foreach (var w in wheels) w.localRotation = Quaternion.Euler(wheelSpin, 0f, 90f);
            bool a = ((int)(Time.time * 5f)) % 2 == 0;
            blueA.intensity = a ? 40f : 0f; blueB.intensity = a ? 0f : 40f;
            foreach (var r in barA) r.sharedMaterial = a ? lightOn : lightOff;
            foreach (var r in barB) r.sharedMaterial = a ? lightOff : lightOn;
        }

        IEnumerator Run(Person victim)
        {
            Status = "Ambulance dispatched";
            yield return new WaitForSeconds(3f);
            Vector3 vp = victim.BodyPos; vp.y = 0;
            float laneZ = World.LaneZ; float parkX = Mathf.Clamp(vp.x - 7f, -150f, 150f);
            transform.position = new Vector3(parkX - 120f, 0, laneZ); transform.rotation = Quaternion.Euler(0, 90f, 0);
            float v = 0;
            while (parkX - transform.position.x > 0.3f)
            {
                float dist = parkX - transform.position.x;
                float target = Mathf.Clamp(dist * 0.8f, 0f, 17f);
                v = Mathf.MoveTowards(v, target, 6f * Time.deltaTime);
                transform.position += Vector3.right * (Mathf.Max(v, dist > 2f ? 1f : 0f) * Time.deltaTime);
                yield return null;
            }
            Status = "Ambulance on scene";
            // crew
            var crew = new List<Person>();
            for (int i = 0; i < 2; i++)
            {
                var p = World.MakePerson(transform.position + new Vector3(-1.4f + (i == 0 ? -1.2f : 1.2f), 0, -3.2f), new Color(0.95f, 0.85f, 0.1f), new Color(0.05f, 0.25f, 0.15f), "Paramedic");
                p.isParamedic = true; crew.Add(p);
            }
            Vector3 side = new Vector3(0, 0, 0.9f);
            yield return WalkAll(crew, new[] { vp + new Vector3(-0.9f, 0, 0.6f), vp + new Vector3(0.9f, 0, 0.6f) }, 1.9f);
            Status = victim.dead ? "Paramedics confirm death" : "Paramedics treating casualty";
            if (GameAudio.I != null) GameAudio.I.PlayVoice("medic", vp, 0.3f, 1f, 0);
            foreach (var c in crew) { if (Busy(c)) continue; c.SetKneel(true); c.transform.rotation = Quaternion.LookRotation((vp - c.transform.position).WithY(0)); }
            yield return new WaitForSeconds(2.5f);
            if (GameAudio.I != null) { if (victim.dead) GameAudio.I.PlayVoice("medicdead", vp, 0f, 1f, 0); else GameAudio.I.PlayVoice("medic", vp, 0f, 1f, 1); }
            yield return new WaitForSeconds(2.5f);
            foreach (var c in crew) c.SetKneel(false);
            yield return new WaitForSeconds(0.8f);
            // trolley + blanket
            var trolley = new GameObject("Trolley").transform; trolley.SetParent(World.Root, false);
            trolley.position = vp + new Vector3(0, 0, -0.4f);
            Gfx.Box(trolley, new Vector3(0, 0.72f, 0), new Vector3(0.7f, 0.08f, 1.9f), Gfx.Mat(new Color(0.75f, 0.75f, 0.78f), null, 0.6f, 0.5f), 0, "Bed", false);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                    Gfx.Box(trolley, new Vector3(sx * 0.3f, 0.36f, sz * 0.8f), new Vector3(0.05f, 0.7f, 0.05f), Gfx.Mat(new Color(0.4f, 0.4f, 0.42f), null, 0.5f, 0.6f), 0, "Leg", false);
            Gfx.Box(trolley, new Vector3(0, 0.86f, 0), new Vector3(0.62f, 0.2f, 1.7f), victim.dead ? Gfx.Mat(Color.white) : Gfx.Mat(new Color(0.15f, 0.45f, 0.75f)), 0, "Blanket", false);
            trolley.rotation = Quaternion.Euler(0, 90f, 0);
            victim.TakeAway();
            if (GameAudio.I != null) GameAudio.I.PlayVoice("medic", vp, 0.3f, 1f, 2);
            Status = "Casualty loaded onto trolley";
            // push trolley to ambulance rear
            Vector3 rear = transform.position + new Vector3(-0.0f, 0, -4.2f);
            var tw = StartCoroutine(MoveTrolley(trolley, rear, crew));
            yield return tw;
            trolley.gameObject.SetActive(false);
            foreach (var c in crew) if (c != null && !Busy(c)) c.gameObject.SetActive(false); // anyone still scrapping gets left behind
            Status = "En route to Wend Valley Hospital";
            float v2 = 0;
            while (transform.position.x < parkX + 260f)
            {
                v2 = Mathf.MoveTowards(v2, 22f, 5f * Time.deltaTime);
                transform.position += Vector3.right * (v2 * Time.deltaTime);
                yield return null;
            }
            Destroy(trolley.gameObject); foreach (var c in crew) if (c != null && !c.gameObject.activeSelf) Destroy(c.gameObject);
            Destroy(gameObject);
        }

        IEnumerator MoveTrolley(Transform trolley, Vector3 rear, List<Person> crew)
        {
            float guard = 0;
            while (guard < 20f)
            {
                guard += Time.deltaTime;
                Vector3 d = rear - trolley.position; d.y = 0;
                if (d.magnitude < 0.4f) break;
                Vector3 step = d.normalized * 1.4f * Time.deltaTime;
                trolley.position += step;
                trolley.rotation = Quaternion.Slerp(trolley.rotation, Quaternion.LookRotation(d.normalized), 4f * Time.deltaTime);
                for (int i = 0; i < crew.Count; i++)
                {
                    if (Busy(crew[i])) continue;
                    Vector3 side = trolley.right * (i == 0 ? -0.9f : 0.9f);
                    crew[i].Step(trolley.position + side, 1.5f);
                }
                yield return null;
            }
        }

        static bool Busy(Person c) { if (c == null || c.state != Person.State.Walking) return true; var f = c.GetComponent<Fighter>(); return f != null && f.Fighting; }

        IEnumerator WalkAll(List<Person> crew, Vector3[] targets, float spd)
        {
            float guard = 0;
            while (guard < 25f)
            {
                guard += Time.deltaTime; bool all = true;
                for (int i = 0; i < crew.Count; i++)
                {
                    if (Busy(crew[i])) continue; // fighting or floored: leave them to it
                    if ((targets[i] - crew[i].transform.position).WithY(0).magnitude > 0.35f) { crew[i].Step(targets[i], spd); all = false; }
                }
                if (all) break;
                yield return null;
            }
        }
    }
}
