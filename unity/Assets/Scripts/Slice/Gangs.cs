using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ASBO.Slice
{
    /// <summary>
    /// The three estates' gangs (TownPlan.Turfs): the Hawthorn lot (red), the Kestrel Crew (blue), T-Bone's lot (yellow).
    /// Each hangs about its own estate's spots in hoodies of its colour, a couple carrying a bat or a blade, one or two
    /// riding round on an electric scooter or an e-bike, and one dealing from a bench. Territorial: hang about on another
    /// crew's ends and they clock you, warn you off, then come for you mob-handed. The dealer sells three things:
    /// whack (calms you down, right down, to a dribbling stupor), fizz (the opposite: wired, fast, twitchy) and wow
    /// (you start seeing things that aren't there). All made up.
    /// </summary>
    public class Gangs : MonoBehaviour
    {
        public static Gangs I;
        static PlayerCtl pcc;
        public static PlayerCtl Player { get { if (pcc == null) pcc = Object.FindFirstObjectByType<PlayerCtl>(); return pcc; } }
        class Member { public Person p; public int gang; public Vector3 home; public bool dealer; public Ride ride; public float idleT; }
        readonly List<Member> members = new List<Member>();
        // where each crew hangs about (world x, z), and its dealer's bench
        static readonly Vector2[][] Spots =
        {
            new[] { new Vector2(-6f, -160f), new Vector2(-12f, -153f), new Vector2(-36f, -163f), new Vector2(20f, -158f), new Vector2(0f, -130f) },
            new[] { new Vector2(-205f, 45f), new Vector2(-225f, 92f), new Vector2(-240f, 55f), new Vector2(-207f, 146f), new Vector2(-250f, 66f) },
            new[] { new Vector2(223f, 60f), new Vector2(189f, 60f), new Vector2(202f, 92f), new Vector2(169f, 141f), new Vector2(150f, 52f) },
        };
        static readonly Vector3[] DealerAt = { new Vector3(-22f, 0f, -164f), new Vector3(-196f, 0f, 45f), new Vector3(267f, 0f, 95f) };
        // the riders' loops round each estate
        static readonly Vector2[][] Loops =
        {
            new[] { new Vector2(-30f, -155f), new Vector2(16f, -155f), new Vector2(16f, -147f), new Vector2(0f, -110f), new Vector2(0f, -147f), new Vector2(-30f, -147f) },
            new[] { new Vector2(-172f, 64.5f), new Vector2(-255f, 64.5f), new Vector2(-255f, 71.5f), new Vector2(-172f, 71.5f) },
            new[] { new Vector2(140f, 32f), new Vector2(275f, 32f), new Vector2(275f, 28f), new Vector2(140f, 28f) },
        };

        public static void Spawn(Transform parent)
        {
            if (I != null || !TownMats.Load()) return; // (the estate and town materials, for the weapons and rides)
            var g = new GameObject("Gangs"); g.transform.SetParent(parent, false); I = g.AddComponent<Gangs>();
            var r = new System.Random(1312);
            for (int gang = 0; gang < 3; gang++)
            {
                var col = TownPlan.Turfs[gang].colour;
                foreach (var s in Spots[gang])
                {
                    int n = 1 + r.Next(2);
                    for (int i = 0; i < n; i++)
                    {
                        var at = new Vector3(s.x + (float)(r.NextDouble() - 0.5) * 2.4f, 0.15f, s.y + (float)(r.NextDouble() - 0.5) * 2.4f);
                        var m = I.Make(at, gang, col); if (m == null) continue;
                        if (r.NextDouble() < 0.4) Weapon(m.p, r.Next(3));
                    }
                }
                var d = I.Make(DealerAt[gang] + Vector3.up * 0.15f, gang, col); if (d != null) d.dealer = true;
                // riders: one on a scooter, one on an e-bike
                for (int k = 0; k < 2; k++)
                {
                    var loop = Loops[gang]; var p0 = loop[k % loop.Length];
                    var rm = I.Make(new Vector3(p0.x, 0.15f, p0.y), gang, col); if (rm == null) continue;
                    rm.ride = Ride.Mount(rm.p, k == 0, loop, k * 2);
                }
            }
            Debug.Log($"[ASBO] gangs: {I.members.Count} out on the three estates");
        }

        /// <summary>Which crew a person runs with (-1: nobody's).</summary>
        public static int GangOf(Person p) { if (I == null || p == null) return -1; foreach (var m in I.members) if (m.p == p) return m.gang; return -1; }
        public static Vector3 DealerPos(int g) => DealerAt[g];
        public static bool IsDealer(Person p) { if (I == null || p == null) return false; foreach (var m in I.members) if (m.p == p) return m.dealer; return false; }
        /// <summary>Every crew member on foot (not riding, not dealing): for the crews' grudges and the jobs.</summary>
        public static void Foot(List<(Person p, int gang)> into) { into.Clear(); if (I == null) return; foreach (var m in I.members) if (m.p != null && m.ride == null && !m.dealer && !m.p.dead) into.Add((m.p, m.gang)); }
        /// <summary>A squad member sent after the player: one of the crew's own, in its colours, often tooled up.</summary>
        public static Person Squad(Vector3 at, int gang)
        {
            if (I == null) return null; var m = I.Make(at, gang, TownPlan.Turfs[gang].colour); if (m == null) return null;
            if (Random.value < 0.5f) Weapon(m.p, Random.Range(0, 3)); return m.p;
        }
        public static void Drop(Person p) { if (I == null || p == null) return; I.members.RemoveAll(m => m.p == p); Destroy(p.gameObject); }

        Member Make(Vector3 at, int gang, Color col)
        {
            var p = World.MakePerson(at, col * 0.55f, new Color(0.08f, 0.08f, 0.09f), "Gang" + gang);
            if (p == null) return null;
            p.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            var m = new Member { p = p, gang = gang, home = at, idleT = Random.Range(2f, 8f) };
            members.Add(m); return m;
        }

        /// <summary>A baseball bat, a lump of wood or a blade in the right hand (a prop on the hand bone).</summary>
        static void Weapon(Person p, int kind)
        {
            var anim = p.anim; if (anim == null) { p.StartCoroutine(Later(p, kind)); return; }
            var hand = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null; if (hand == null) { p.StartCoroutine(Later(p, kind)); return; }
            var k = new MeshKit(); var E = CouncilEstate.M;
            var wood = TownMats.bench != null ? TownMats.bench : E.fence;
            if (kind == 0) { k.Cyl(wood, new Vector3(0, -0.3f, 0), 0.022f, 0.34f, 8); k.Cyl(wood, new Vector3(0, -0.82f, 0), 0.045f, 0.52f, 10, true, 0.026f); } // bat
            else if (kind == 1) k.Box(E.fence, new Vector3(-0.025f, -0.75f, -0.025f), new Vector3(0.025f, 0.08f, 0.025f)); // a length of timber
            else { k.Box(E.black, new Vector3(-0.015f, -0.08f, -0.012f), new Vector3(0.015f, 0.04f, 0.012f)); k.Box(TownMats.steel, new Vector3(-0.012f, -0.27f, -0.003f), new Vector3(0.012f, -0.08f, 0.003f)); } // a blade
            var g = k.Build("Weapon", hand, false, ShadowCastingMode.Off);
            if (g != null) { g.transform.localPosition = new Vector3(0f, -0.04f, 0.03f); g.transform.localRotation = Quaternion.Euler(0, 0, 90f); }
        }
        static System.Collections.IEnumerator Later(Person p, int kind) { yield return new WaitForSeconds(2f); if (p != null && p.anim != null) Weapon(p, kind); }

        // ------------------------------------------------------------------ turf
        float heat, warnT, tick; int heatGang = -1; float hostileUntil;
        void Update()
        {
            Drugs.Tick();
            if ((tick -= Time.deltaTime) > 0f) return; tick = 0.5f;
            var pc = Gangs.Player; if (pc == null) return;
            Vector3 pp = pc.transform.position; int turf = TownPlan.TurfAt(pp);
            // anyone of theirs close by sees you: the heat builds while you hang about (slower if you're sneaking)
            int seen = 0; Member nearest = null; float nd = 1e9f;
            foreach (var m in members)
            {
                if (m.p == null || m.gang != turf || m.ride != null) continue;
                float d = (m.p.transform.position - pp).sqrMagnitude; if (d < 22f * 22f) seen++;
                if (d < nd) { nd = d; nearest = m; }
            }
            if (turf < 0 || seen == 0 || pc.InCar || Crews.Affiliation == turf) { heat = Mathf.Max(0f, heat - 0.5f * 0.6f); return; }
            if (heatGang != turf) { heatGang = turf; heat = 0f; }
            heat += 0.5f * (pc.Crouched ? 0.35f : 1f) * Mathf.Min(3, seen) * 0.5f * (Crews.Standing[turf] >= 25f ? 0.3f : Crews.Hates(turf) ? 4f : 1f);
            if (heat > 5f && Time.time > warnT)
            {
                warnT = Time.time + 25f;
                World.Toast(new[] { "Oi! You lost? This is " + TownPlan.Turfs[turf].estate + ".", "Who are you? Get off our ends.", "Wrong estate, mate. Walk away." }[Random.Range(0, 3)], 1);
                if (nearest != null) nearest.p.transform.rotation = Quaternion.LookRotation((pp - nearest.p.transform.position).WithY(0));
            }
            if (heat > 14f && Time.time > hostileUntil)
            {
                hostileUntil = Time.time + 40f; heat = 6f;
                var pf = Fighter.Session.PlayerFighter; int sent = 0;
                members.Sort((a, b) => (a.p == null ? 1e9f : (a.p.transform.position - pp).sqrMagnitude).CompareTo(b.p == null ? 1e9f : (b.p.transform.position - pp).sqrMagnitude));
                foreach (var m in members)
                {
                    if (m.p == null || m.gang != turf || m.ride != null || m.dealer || sent >= 3) continue;
                    var f = m.p.GetComponent<Fighter>(); if (f == null) continue;
                    f.SquareUp(pf, true); sent++;
                }
                World.Toast(TownPlan.Turfs[turf].gang + " are coming for you.", 2);
            }
        }

        // ------------------------------------------------------------------ dealing
        static int menuGang = -1; static float menuT;
        /// <summary>TALK by a dealer: open his little menu. True if there was a dealer to talk to.</summary>
        public static bool TryDeal()
        {
            if (I == null || Gangs.Player == null) return false;
            Vector3 pp = Gangs.Player.transform.position;
            foreach (var m in I.members)
                if (m.dealer && m.p != null && (m.p.transform.position - pp).sqrMagnitude < 2.8f * 2.8f)
                {
                    if (Crews.Hates(m.gang)) { World.Toast("\"You've got some front coming here. Get lost before I call the lads.\"", 1); return true; }
                    if (I.heatGang == m.gang && I.heat > 10f) { World.Toast("He looks straight through you. Not from round here, are you?", 1); return true; }
                    menuGang = m.gang; menuT = Time.time + 12f; return true;
                }
            return false;
        }
        void OnGUI()
        {
            if (menuGang < 0 || Time.time > menuT || Gangs.Player == null) { menuGang = -1; return; }
            float s = Mathf.Max(1f, Screen.height / 720f); float w = 300 * s, h = 52 * s, x = Screen.width * 0.5f - w * 0.5f, y = Screen.height * 0.36f;
            var st = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(18 * s), wordWrap = true };
            bool join = Crews.CanJoin(menuGang); int rows = join ? 6 : 5;
            GUI.Box(new Rect(x - 10, y - 46 * s, w + 20, h * rows + 60 * s + rows * 4), "");
            var lab = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(18 * s), alignment = TextAnchor.MiddleCenter }; lab.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y - 40 * s, w, 34 * s), "\"What you after?\"", lab);
            if (GUI.Button(new Rect(x, y, w, h), "Whack  £10  (sorts your head right out)", st)) Buy(Drugs.Kind.Whack, 10f);
            if (GUI.Button(new Rect(x, y + h + 4, w, h), "Fizz  £10  (gets you going)", st)) Buy(Drugs.Kind.Fizz, 10f);
            if (GUI.Button(new Rect(x, y + (h + 4) * 2, w, h), "Wow  £15  (you'll see things)", st)) Buy(Drugs.Kind.Wow, 15f);
            if (GUI.Button(new Rect(x, y + (h + 4) * 3, w, h), "Got any work?", st)) { int g = menuGang; menuGang = -1; Missions.OfferFrom(g); }
            if (join && GUI.Button(new Rect(x, y + (h + 4) * 4, w, h), "I want in. Proper.", st)) { int g = menuGang; menuGang = -1; Crews.Join(g); }
            if (GUI.Button(new Rect(x, y + (h + 4) * (join ? 5 : 4), w, h), "Nothing. Laters.", st)) menuGang = -1;
        }
        static void Buy(Drugs.Kind k, float price) { int g = menuGang; menuGang = -1; if (!Econ.Spend(price, k.ToString())) return; Drugs.Take(k); if (g >= 0) Crews.Bump(g, 1.5f); }
    }

    /// <summary>
    /// An electric scooter or an e-bike with a gang member on it, riding a loop round the estate (the rider stands on
    /// the scooter; sits on the bike). Simple and cheap: one mesh each, the rider carried along.
    /// </summary>
    public class Ride : MonoBehaviour
    {
        Person rider; Vector2[] loop; int next; bool scooter; float speed; Transform frontWheel, backWheel;
        public static Ride Mount(Person p, bool scooter, Vector2[] loop, int start)
        {
            var g = new GameObject(scooter ? "E-Scooter" : "E-Bike"); var r = g.AddComponent<Ride>();
            r.rider = p; r.loop = loop; r.next = (start + 1) % loop.Length; r.scooter = scooter; r.speed = scooter ? 5.5f : 7f;
            g.transform.position = new Vector3(loop[start].x, 0.05f, loop[start].y);
            r.BuildMesh();
            var cc = p.GetComponent<CharacterController>(); if (cc != null) cc.enabled = false;
            var nav = p.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (nav != null) nav.enabled = false;
            return r;
        }
        void BuildMesh()
        {
            var E = CouncilEstate.M; var k = new MeshKit(); var blk = E.black; var frame = TownMats.steel;
            if (scooter)
            {
                k.Box(blk, new Vector3(-0.08f, 0.1f, -0.42f), new Vector3(0.08f, 0.15f, 0.38f));                 // deck
                k.Box(frame, new Vector3(-0.02f, 0.12f, 0.36f), new Vector3(0.02f, 1.05f, 0.42f));                // stem
                k.Box(blk, new Vector3(-0.26f, 1.03f, 0.37f), new Vector3(0.26f, 1.07f, 0.41f));                  // bars
            }
            else
            {
                foreach (var (a, b) in new[] { (new Vector3(0, 0.33f, -0.48f), new Vector3(0, 0.75f, 0f)), (new Vector3(0, 0.75f, 0f), new Vector3(0, 0.95f, 0.46f)), (new Vector3(0, 0.33f, -0.48f), new Vector3(0, 0.36f, 0.05f)), (new Vector3(0, 0.36f, 0.05f), new Vector3(0, 0.95f, 0.46f)), (new Vector3(0, 0.95f, 0.46f), new Vector3(0, 0.33f, 0.52f)) })
                { var d = b - a; k.Push(Matrix4x4.TRS((a + b) * 0.5f, Quaternion.LookRotation(d.normalized), Vector3.one)); k.Box(frame, new Vector3(-0.02f, -0.02f, -d.magnitude * 0.5f), new Vector3(0.02f, 0.02f, d.magnitude * 0.5f)); k.Pop(); }
                k.Box(blk, new Vector3(-0.05f, 0.42f, -0.2f), new Vector3(0.05f, 0.62f, 0.1f));                    // battery
                k.Box(blk, new Vector3(-0.08f, 0.78f, -0.08f), new Vector3(0.08f, 0.83f, 0.14f));                  // saddle
                k.Box(blk, new Vector3(-0.3f, 0.98f, 0.44f), new Vector3(0.3f, 1.01f, 0.48f));                     // bars
            }
            k.Build("Frame", transform, false, ShadowCastingMode.On);
            float r = scooter ? 0.1f : 0.33f;
            frontWheel = Wheel(new Vector3(0, r, scooter ? 0.4f : 0.52f), r); backWheel = Wheel(new Vector3(0, r, scooter ? -0.4f : -0.48f), r);
        }
        Transform Wheel(Vector3 at, float r)
        {
            var k = new MeshKit(); k.CylX(CouncilEstate.M.black, Vector3.zero, r, 0.05f, 12);
            var g = k.Build("Wheel", transform, false, ShadowCastingMode.Off); g.transform.localPosition = at; return g.transform;
        }
        void Update()
        {
            if (frontWheel == null || rider == null || rider.state != Person.State.Walking) { if (rider != null && rider.state != Person.State.Walking) enabled = false; return; }
            var tgt = new Vector3(loop[next].x, 0.05f, loop[next].y); Vector3 to = (tgt - transform.position).WithY(0);
            if (to.magnitude < 1.2f) { next = (next + 1) % loop.Length; return; }
            // slow for the player standing in the way
            float sp = speed; var pc = Gangs.Player; if (pc != null && (pc.transform.position - transform.position).sqrMagnitude < 9f) sp = 1f;
            var want = Quaternion.LookRotation(to.normalized); transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 140f * Time.deltaTime);
            transform.position += transform.forward * sp * Time.deltaTime;
            if (Physics.Raycast(transform.position + Vector3.up * 1.5f, Vector3.down, out var h, 3f, ~((1 << 2) | (1 << Person.RagdollLayer) | (1 << Person.VehicleLayer)), QueryTriggerInteraction.Ignore)) transform.position = transform.position.WithY(h.point.y + 0.02f);
            float spin = sp * Time.deltaTime / (scooter ? 0.1f : 0.33f) * Mathf.Rad2Deg; frontWheel.Rotate(spin, 0, 0, Space.Self); backWheel.Rotate(spin, 0, 0, Space.Self);
            // the rider: stood on the deck, or sat on the saddle, carried along
            rider.transform.SetPositionAndRotation(transform.position + transform.up * (scooter ? 0.15f : 0.35f) - transform.forward * (scooter ? 0.05f : 0.1f), transform.rotation);
            rider.SetMove(Vector2.zero);
            if (!scooter && rider.anim != null) foreach (var pr in rider.anim.parameters) if (pr.name == "Seated") { rider.anim.SetBool("Seated", true); break; }
        }
    }

    /// <summary>
    /// What the dealers sell, and what it does to you. Whack: slow, heavy, swaying, the colour drained out, dribbling
    /// (a minute). Fizz: fast, twitchy, the colour turned up, the heart going (45 seconds). Wow: the colours crawl and
    /// people who aren't there stand about, gone when you get near (a minute).
    /// </summary>
    public static class Drugs
    {
        public enum Kind { None, Whack, Fizz, Wow }
        public static Kind On = Kind.None; static float until, start;
        static ColorAdjustments ca; static Vignette vg; static bool looked; static float baseSat, baseVig;
        static readonly List<GameObject> ghosts = new List<GameObject>(); static float ghostT;
        public static float SpeedScale => On == Kind.Whack ? 0.55f : On == Kind.Fizz ? 1.35f : 1f;

        public static void Take(Kind k)
        {
            On = k; start = Time.time; until = Time.time + (k == Kind.Fizz ? 45f : 60f);
            World.Toast(k == Kind.Whack ? "Ohhh. Everything's... very... slow." : k == Kind.Fizz ? "Your heart's going like the clappers." : "Hang on. Was that there before?", 1);
        }
        static void Look()
        {
            if (looked) return; looked = true;
            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
            {
                if (v.profile == null) continue;
                if (ca == null && v.profile.TryGet(out ColorAdjustments c)) { ca = c; baseSat = c.saturation.value; }
                if (vg == null && v.profile.TryGet(out Vignette g)) { vg = g; baseVig = g.intensity.value; }
            }
        }
        public static void Tick()
        {
            if (On == Kind.None) return;
            Look(); float t = Time.time - start, left = until - Time.time, fade = Mathf.Clamp01(left / 6f) * Mathf.Clamp01(t / 3f);
            var cam = Camera.main;
            switch (On)
            {
                case Kind.Whack:
                    if (ca != null) { ca.saturation.overrideState = true; ca.saturation.value = Mathf.Lerp(baseSat, -55f, fade); }
                    if (vg != null) { vg.intensity.overrideState = true; vg.intensity.value = Mathf.Lerp(baseVig, 0.5f, fade); }
                    if (cam != null) cam.transform.rotation *= Quaternion.Euler(Mathf.Sin(t * 0.7f) * 2.2f * fade, 0f, Mathf.Sin(t * 0.45f) * 4f * fade);
                    break;
                case Kind.Fizz:
                    if (ca != null) { ca.saturation.overrideState = true; ca.saturation.value = Mathf.Lerp(baseSat, 55f, fade); }
                    if (cam != null) cam.transform.position += Random.insideUnitSphere * 0.025f * fade;
                    break;
                case Kind.Wow:
                    if (ca != null) { ca.hueShift.overrideState = true; ca.hueShift.value = Mathf.Sin(t * 0.6f) * 70f * fade; ca.saturation.overrideState = true; ca.saturation.value = Mathf.Lerp(baseSat, 35f, fade); }
                    Ghosts(fade);
                    break;
            }
            if (left <= 0f)
            {
                if (ca != null) { ca.saturation.value = baseSat; ca.hueShift.value = 0f; } if (vg != null) vg.intensity.value = baseVig;
                foreach (var g in ghosts) if (g != null) Object.Destroy(g); ghosts.Clear();
                World.Toast(On == Kind.Whack ? "You come round. There's dribble on your chin." : On == Kind.Fizz ? "The fizz wears off. You feel like death." : "Things go back to how they were. Probably.", 0);
                On = Kind.None;
            }
        }
        /// <summary>People who aren't there: figures stood about at the edge of things, gone when you get close.</summary>
        static void Ghosts(float fade)
        {
            var pc = Gangs.Player; if (pc == null) return;
            for (int i = ghosts.Count - 1; i >= 0; i--) { var g = ghosts[i]; if (g == null || (g.transform.position - pc.transform.position).sqrMagnitude < 5f * 5f) { if (g != null) Object.Destroy(g); ghosts.RemoveAt(i); } }
            if (Time.time < ghostT || ghosts.Count >= 4 || fade < 0.5f) return; ghostT = Time.time + Random.Range(2f, 5f);
            Vector3 at = pc.transform.position + Quaternion.Euler(0, Random.Range(-70f, 70f), 0) * pc.transform.forward * Random.Range(9f, 18f);
            var k = new MeshKit(); var m = CouncilEstate.M.black;
            k.Cyl(m, Vector3.zero, 0.22f, 1.45f, 8, true, 0.18f); k.Sphere(m, new Vector3(0, 1.62f, 0), Vector3.one * 0.13f, 8, 5); // a dark figure
            var go = k.Build("Ghost", null, false, ShadowCastingMode.Off); go.transform.position = at; go.transform.rotation = Quaternion.LookRotation((pc.transform.position - at).WithY(0));
            ghosts.Add(go);
        }
    }
}
