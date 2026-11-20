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

        void BuildVehicle()
        {
            var white = Gfx.Mat(new Color(0.93f, 0.93f, 0.92f), null, 0.55f, 0.1f);
            var yellow = Gfx.Mat(new Color(0.98f, 0.85f, 0.05f), null, 0.5f);
            var green = Gfx.Mat(new Color(0.05f, 0.62f, 0.3f), null, 0.5f);
            var black = Gfx.Mat(new Color(0.05f, 0.05f, 0.05f), null, 0.3f);
            var glass = Gfx.Mat(new Color(0.08f, 0.1f, 0.14f), null, 0.9f);
            Gfx.Box(transform, new Vector3(0, 1.25f, -0.3f), new Vector3(2.1f, 1.9f, 4.4f), white, 0, "Box", false);
            Gfx.Box(transform, new Vector3(0, 0.95f, 2.2f), new Vector3(2.05f, 1.3f, 1.4f), white, 0, "Cab", false);
            Gfx.Box(transform, new Vector3(0, 1.4f, 2.3f), new Vector3(1.9f, 0.7f, 1.3f), glass, 0, "Windscreen", false);
            Gfx.Box(transform, new Vector3(0, 0.4f, 0f), new Vector3(2.0f, 0.4f, 5.4f), black, 0, "Chassis", false);
            // battenburg chequer on both sides, front chequer stripe too
            for (int i = 0; i < 10; i++)
                foreach (int s in new[] { -1, 1 })
                    for (int r = 0; r < 2; r++)
                    {
                        var m = ((i + r) % 2 == 0) ? yellow : green;
                        Gfx.Box(transform, new Vector3(s * 1.07f, 0.95f + r * 0.32f, -2.3f + i * 0.5f), new Vector3(0.04f, 0.32f, 0.5f), m, 0, "Batten", false);
                    }
            for (int i = 0; i < 4; i++)
                Gfx.Box(transform, new Vector3(-0.75f + i * 0.5f, 0.78f, 2.92f), new Vector3(0.5f, 0.3f, 0.04f), (i % 2 == 0) ? yellow : green, 0, "BattenFront", false);
            // wheels
            foreach (float z in new[] { 1.8f, -1.5f }) foreach (int s in new[] { -1, 1 })
                    Gfx.Prim(PrimitiveType.Cylinder, transform, new Vector3(s * 0.98f, 0.45f, z), new Vector3(0.9f, 0.14f, 0.9f), black, "Wheel", false).transform.localRotation = Quaternion.Euler(0, 0, 90f);
            // light bar
            lightOn = Gfx.Emissive(new Color(0.1f, 0.3f, 1f), 4f); lightOff = Gfx.Mat(new Color(0.05f, 0.1f, 0.3f));
            var a = Gfx.Box(transform, new Vector3(-0.45f, 2.3f, 1.9f), new Vector3(0.8f, 0.14f, 0.2f), lightOff, 0, "BarA", false);
            var b = Gfx.Box(transform, new Vector3(0.45f, 2.3f, 1.9f), new Vector3(0.8f, 0.14f, 0.2f), lightOff, 0, "BarB", false);
            var a2 = Gfx.Box(transform, new Vector3(-0.45f, 2.3f, -2.3f), new Vector3(0.8f, 0.14f, 0.2f), lightOff, 0, "BarA2", false);
            var b2 = Gfx.Box(transform, new Vector3(0.45f, 2.3f, -2.3f), new Vector3(0.8f, 0.14f, 0.2f), lightOff, 0, "BarB2", false);
            barA = new[] { a.GetComponent<Renderer>(), a2.GetComponent<Renderer>() }; barB = new[] { b.GetComponent<Renderer>(), b2.GetComponent<Renderer>() };
            blueA = MakeLight(new Vector3(-0.5f, 2.6f, 1.9f)); blueB = MakeLight(new Vector3(0.5f, 2.6f, 1.9f));
            // mirrored AMBULANCE lettering on the front so drivers see it correctly in their mirrors
            try
            {
                var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var tgo = new GameObject("Lettering"); tgo.transform.SetParent(transform, false);
                tgo.transform.localPosition = new Vector3(0, 1.45f, 2.93f); tgo.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                tgo.transform.localScale = new Vector3(-0.1f, 0.1f, 0.1f);
                var tm = tgo.AddComponent<TextMesh>(); tm.text = "AMBULANCE"; tm.font = f; tm.fontSize = 64; tm.characterSize = 0.6f;
                tm.anchor = TextAnchor.MiddleCenter; tm.color = new Color(0.05f, 0.5f, 0.25f);
                tgo.GetComponent<MeshRenderer>().sharedMaterial = f.material;
            }
            catch { }
            var au = gameObject.AddComponent<AudioSource>();
            au.clip = SirenClip(); au.loop = true; au.spatialBlend = 1f; au.minDistance = 10f; au.maxDistance = 160f; au.rolloffMode = AudioRolloffMode.Linear; au.volume = 0.5f;
            au.Play();
        }

        Light MakeLight(Vector3 p)
        {
            var g = new GameObject("Blue"); g.transform.SetParent(transform, false); g.transform.localPosition = p;
            var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(0.2f, 0.4f, 1f); l.range = 14f; l.intensity = 0f;
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
            foreach (var c in crew) { c.SetKneel(true); c.transform.rotation = Quaternion.LookRotation((vp - c.transform.position).WithY(0)); }
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
            foreach (var c in crew) c.gameObject.SetActive(false);
            Status = "En route to Wend Valley Hospital";
            float v2 = 0;
            while (transform.position.x < parkX + 260f)
            {
                v2 = Mathf.MoveTowards(v2, 22f, 5f * Time.deltaTime);
                transform.position += Vector3.right * (v2 * Time.deltaTime);
                yield return null;
            }
            Destroy(trolley.gameObject); foreach (var c in crew) if (c != null) Destroy(c.gameObject);
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
                    Vector3 side = trolley.right * (i == 0 ? -0.9f : 0.9f);
                    crew[i].Step(trolley.position + side, 1.5f);
                }
                yield return null;
            }
        }

        IEnumerator WalkAll(List<Person> crew, Vector3[] targets, float spd)
        {
            float guard = 0;
            while (guard < 25f)
            {
                guard += Time.deltaTime; bool all = true;
                for (int i = 0; i < crew.Count; i++)
                {
                    if ((targets[i] - crew[i].transform.position).WithY(0).magnitude > 0.35f) { crew[i].Step(targets[i], spd); all = false; }
                }
                if (all) break;
                yield return null;
            }
        }
    }
}
