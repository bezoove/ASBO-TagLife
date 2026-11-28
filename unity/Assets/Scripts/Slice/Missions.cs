using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Work. Jobs come two ways: ask a crew's dealer ("got any work?") or the burner phone buzzes with a text from somebody
    /// who's heard of you. Five kinds, any order, done however you like:
    ///  - a motor: nick any car and leave it at the lock-ups;
    ///  - a run: take a parcel to a door on somebody else's ends (not with the police on you);
    ///  - a burglary: go through a named house, at least two places;
    ///  - a lesson: knock somebody down who owes, or one of a rival crew who's been mouthing off;
    ///  - a fare: pick somebody up in a car and get them across town before the meter runs out.
    /// A beam marks where to go, the top of the screen says what's next and how long's left. Paid in cash and respect, and
    /// whoever gave you the job rates you more (their rivals less). Fail and they rate you less.
    /// </summary>
    public class Missions : MonoBehaviour
    {
        public static Missions I;
        public enum Kind { Motor, Run, Burgle, Lesson, Fare }
        class Job
        {
            public Kind kind; public int crew = -1; public string from, title, brief;
            public Vector3 a, b; public Home home; public Person target; public float limit, start, pay, rep; public int stage;
            public string Next;
        }
        Job offer, job; float offerUntil, nextText, beamT; int done; GameObject beam; Material beamMat; GUIStyle st, small, btn;
        static readonly string[] Texters = { "Unknown", "Dazza", "Shell", "Big Mick", "Unknown number", "Nathan (don't answer)" };

        public static void Ensure(Transform parent)
        {
            if (I != null) return;
            var g = new GameObject("Missions"); g.transform.SetParent(parent, false); I = g.AddComponent<Missions>();
            I.done = PlayerPrefs.GetInt("asbo.jobs", 0); I.nextText = Time.time + 90f;
        }

        /// <summary>A crew's dealer asked for work.</summary>
        public static void OfferFrom(int crew)
        {
            if (I == null) return;
            if (I.job != null) { World.Toast("\"Finish what you're doing first.\"", 1); return; }
            var kinds = new[] { Kind.Run, Kind.Burgle, Kind.Lesson, Kind.Motor };
            for (int t = 0; t < 6; t++) { var j = I.Make(kinds[Random.Range(0, kinds.Length)], crew); if (j != null) { I.Offer(j); return; } }
            World.Toast("\"Nothing going right now. Come back later.\"", 0);
        }

        void Offer(Job j) { offer = j; offerUntil = Time.time + 25f; }

        // ------------------------------------------------------------------ making jobs
        Job Make(Kind k, int crew)
        {
            var pc = Gangs.Player; if (pc == null) return null; Vector3 pp = pc.transform.position;
            var j = new Job { kind = k, crew = crew, from = crew >= 0 ? TownPlan.Turfs[crew].tag + " dealer" : Texters[Random.Range(0, Texters.Length)] };
            string boss = crew >= 0 ? Crews.Name(crew) : "them";
            switch (k)
            {
                case Kind.Motor:
                {
                    var lks = FindObjectsByType<Lockup>(FindObjectsSortMode.None); if (lks.Length == 0) return null;
                    var lk = lks[Random.Range(0, lks.Length)]; var h = lk.GetComponent<Home>();
                    float L = h != null ? (h.radius - 3f) * 2f : 10f; j.b = lk.transform.TransformPoint(new Vector3(L * 0.5f, 0f, -5f));
                    j.title = "A motor"; j.brief = "Need a motor. Any motor, I'm not fussy. Leave it in front of the " + lk.name + " and walk away.";
                    j.limit = 360f; j.pay = Random.Range(120, 200); j.rep = 4f; j.Next = "Nick a car";
                    break;
                }
                case Kind.Run:
                {
                    int other = -1; for (int t = 0; t < 8 && other < 0; t++) { int o = Random.Range(0, 3); if (o != crew) other = o; }
                    var door = DoorOn(other, pp, 40f, 400f); if (door == null) return null;
                    j.home = door; j.b = FrontOf(door); j.a = pp;
                    j.title = "A run"; j.brief = "Got a parcel wants dropping round " + Where(j.b) + ". Don't open it. Don't turn up with the police on you.";
                    j.limit = 300f; j.pay = Random.Range(60, 110); j.rep = 3f; j.Next = "Drop the parcel"; j.stage = 1;
                    break;
                }
                case Kind.Burgle:
                {
                    int turf = crew >= 0 ? (crew + 1 + Random.Range(0, 2)) % 3 : Random.Range(0, 3);
                    var h = DoorOn(turf, pp, 40f, 400f, true); if (h == null) return null;
                    j.home = h; j.b = FrontOf(h);
                    j.title = "A house"; j.brief = "Number " + (Mathf.Abs(h.name.GetHashCode()) % 90 + 2) + " round " + Where(j.b) + ". They're loaded and they're careless. Go through the place: two rooms' worth, at least.";
                    j.limit = 480f; j.pay = Random.Range(80, 140); j.rep = 5f; j.Next = "Get into the house";
                    break;
                }
                case Kind.Lesson:
                {
                    Person tgt = null; bool rival = crew >= 0 && Random.value < 0.5f;
                    if (rival)
                    {
                        var foot = new List<(Person p, int gang)>(); Gangs.Foot(foot); var pick = new List<Person>();
                        foreach (var (p, g) in foot) if (g != crew && g != Crews.Affiliation) { float d = (p.transform.position - pp).magnitude; if (d > 30f && d < 350f) pick.Add(p); }
                        if (pick.Count > 0) tgt = pick[Random.Range(0, pick.Count)];
                    }
                    if (tgt == null)
                    {
                        rival = false;
                        for (int t = 0; t < 40 && tgt == null; t++)
                        {
                            var o = Person.All[Random.Range(0, Person.All.Count)];
                            if (o == null || o.isPlayer || o.dead || o.important || o.isParamedic || o.state != Person.State.Walking || o.GetComponent<Bobby>() != null || Gangs.GangOf(o) >= 0 || o.GetComponent<Fighter>() == null) continue;
                            float d = (o.transform.position - pp).magnitude; if (d > 25f && d < 250f) tgt = o;
                        }
                    }
                    if (tgt == null) return null;
                    string nm = Persona.Name(tgt); j.target = tgt; string he = tgt.male ? "he" : "she", him = tgt.male ? "him" : "her";
                    j.title = "A lesson"; j.brief = rival ? nm + ", one of " + Crews.Name(Gangs.GangOf(tgt)) + ", has been mouthing off about " + boss + ". Put " + him + " on the floor."
                                                          : nm + " owes and " + he + "'s stopped answering. Go and remind " + him + ". Put " + him + " on the floor, nothing fancy.";
                    j.limit = 300f; j.pay = Random.Range(70, 130); j.rep = rival ? 6f : 3f; j.Next = "Find " + nm;
                    break;
                }
                case Kind.Fare:
                {
                    Vector3 from = Vector3.zero, to = Vector3.zero; bool ok = false;
                    for (int t = 0; t < 40 && !ok; t++)
                    {
                        var o = Person.All[Random.Range(0, Person.All.Count)]; var q = Person.All[Random.Range(0, Person.All.Count)];
                        if (o == null || q == null || o.isPlayer || q.isPlayer) continue;
                        from = o.transform.position; to = q.transform.position; float d0 = (from - pp).magnitude, d1 = (to - from).magnitude;
                        ok = d0 > 25f && d0 < 220f && d1 > 120f && d1 < 450f;
                    }
                    if (!ok) return null;
                    j.a = from; j.b = to; j.title = "A fare"; j.brief = "Need a lift, cash in hand. I'm by the beam. Don't hang about, I've a train to catch.";
                    j.limit = 60f + (from - pp).magnitude / 7f + (to - from).magnitude / 7f; j.pay = Mathf.Round(15f + (to - from).magnitude * 0.12f); j.rep = 1f; j.Next = "Get a car";
                    break;
                }
            }
            return j;
        }

        /// <summary>An estate house on turf g (any turf if g is -1) between near and far metres away.</summary>
        static Home DoorOn(int g, Vector3 pp, float near, float far, bool lived = false)
        {
            var list = new List<Home>();
            foreach (var h in Home.All)
            {
                if (h == null || h.shop || h.variant == null || !(h.variant.StartsWith("g") || h.variant.StartsWith("house")) || h.doors.Count == 0) continue;
                if (lived && (h.H == null || !h.H.Occupied)) continue;
                Vector3 c = FrontOf(h); float d = (c - pp).magnitude; if (d < near || d > far) continue;
                if (g >= 0 && TownPlan.TurfAt(c) != g) continue;
                list.Add(h);
            }
            if (list.Count == 0 && g >= 0) return DoorOn(-1, pp, near, far, lived);
            return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
        }
        static string Where(Vector3 p) { int t = TownPlan.TurfAt(p); return t >= 0 ? TownPlan.Turfs[t].estate : "the other side of town"; }
        static Vector3 FrontOf(Home h)
        {
            var d = h.doors.Find(x => x.tag == "front" && x.door != null) ?? h.doors.Find(x => x.door != null);
            if (d == null) return h.WCentre;
            Vector3 p = d.door.transform.position; Vector3 out1 = (p - h.WCentre).WithY(0f).normalized; return p + out1 * 1.2f;
        }

        // ------------------------------------------------------------------ running them
        void Update()
        {
            var pc = Gangs.Player; if (pc == null) return;
            if (offer != null && Time.time > offerUntil) offer = null;
            if (job == null)
            {
                if (offer == null && Time.time > nextText)
                {
                    nextText = Time.time + Random.Range(150f, 300f);
                    int crew = -1; if (Random.value < 0.5f) { int g = Random.Range(0, 3); if (Crews.Standing[g] > -10f || g == Crews.Affiliation) crew = g; }
                    var kinds = crew >= 0 ? new[] { Kind.Run, Kind.Burgle, Kind.Lesson, Kind.Motor } : new[] { Kind.Motor, Kind.Fare, Kind.Lesson, Kind.Burgle };
                    for (int t = 0; t < 5; t++) { var j = Make(kinds[Random.Range(0, kinds.Length)], crew); if (j != null) { if (crew >= 0) j.from = TownPlan.Turfs[crew].tag + " (text)"; Offer(j); if (GameAudio.I != null) GameAudio.I.Knock(pc.transform.position, 1f, false); break; } }
                }
                ShowBeam(false, Vector3.zero); return;
            }
            Vector3 pp = pc.transform.position; float left = job.limit - (Time.time - job.start);
            if (left <= 0f) { Fail("Out of time."); return; }
            switch (job.kind)
            {
                case Kind.Motor:
                    if (job.stage == 0) { if (pc.InCar) { job.stage = 1; job.Next = "Leave it at the lock-ups"; } }
                    else if (job.stage == 1)
                    {
                        if (!pc.InCar) { job.stage = 0; job.Next = "Nick a car"; }
                        else if ((pc.Vehicle.position - job.b).WithY(0f).magnitude < 7f && pc.VehicleSpeed < 2f) { job.stage = 2; job.Next = "Get out and walk away"; }
                    }
                    else if (!pc.InCar) Win("\"Nice. That'll do.\"");
                    break;
                case Kind.Run:
                    if ((pp - job.b).WithY(0f).magnitude < 2.6f && !pc.InCar)
                    {
                        if (Offences.Wanted >= 40f) { if (Time.frameCount % 240 == 0) World.Toast("Nobody's opening that door with you this hot. Lose the police first.", 1); job.Next = "Lose the heat, then knock"; }
                        else Win("A hand comes round the door and takes it. The door shuts.");
                    }
                    break;
                case Kind.Burgle:
                {
                    int got = 0; var h = job.home;
                    if (h == null) { Fail("The job's off."); return; }
                    if (h.done != null) for (int i = 0; i < h.spots.Count && i < h.done.Length; i++) if (h.done[i] && h.spots[i].kind == "rummage") got++;
                    if (got > 0 && job.stage == 0) { job.stage = 1; }
                    job.Next = got == 0 ? "Get into the house and go through it" : "Keep going: " + got + " of 2";
                    if (got >= 2) Win("That'll do. Get out before somebody comes home.");
                    break;
                }
                case Kind.Lesson:
                    if (job.target == null) { Fail("They've gone."); return; }
                    if (job.target.dead || job.target.state != Person.State.Walking) Win("Message delivered.");
                    break;
                case Kind.Fare:
                    if (job.stage == 0) { if (pc.InCar) { job.stage = 1; job.Next = "Pick up the fare"; } }
                    else if (job.stage == 1)
                    {
                        if (!pc.InCar) { job.stage = 0; job.Next = "Get a car"; }
                        else if ((pc.Vehicle.position - job.a).WithY(0f).magnitude < 8f && pc.VehicleSpeed < 2.5f) { job.stage = 2; job.Next = "Get them there"; World.Toast("\"Cheers, mate.\" They climb in the back.", 0); }
                    }
                    else if (job.stage == 2)
                    {
                        if (!pc.InCar) { Fail("You've left your fare sat in the car. They do one."); return; }
                        if ((pc.Vehicle.position - job.b).WithY(0f).magnitude < 9f && pc.VehicleSpeed < 2.5f) { float tip = left > job.limit * 0.35f ? Mathf.Round(job.pay * 0.3f) : 0f; job.pay += tip; Win(tip > 0f ? "\"Blimey, that was quick.\" Plus a tip." : "They pay up and get out."); }
                    }
                    break;
            }
            if (job != null) ShowBeam(true, Target(job));
        }

        Vector3 Target(Job j)
        {
            switch (j.kind)
            {
                case Kind.Motor: return j.stage == 0 ? Vector3.positiveInfinity : j.b;
                case Kind.Lesson: return j.target != null ? j.target.transform.position : Vector3.positiveInfinity;
                case Kind.Fare: return j.stage == 0 ? Vector3.positiveInfinity : j.stage == 1 ? j.a : j.b;
                default: return j.b;
            }
        }

        void Accept()
        {
            job = offer; offer = null; if (job == null) return; job.start = Time.time;
            World.Toast("Right. " + job.Next + ".", 0);
        }
        void Win(string line)
        {
            var j = job; job = null; done++; PlayerPrefs.SetInt("asbo.jobs", done);
            Econ.Give(j.pay); Rel.Rep += j.rep;
            if (j.crew >= 0) { Crews.Bump(j.crew, 8f); for (int o = 0; o < 3; o++) if (o != j.crew) Crews.Bump(o, -1.5f); }
            World.Toast(line + "  +" + Econ.Fmt(j.pay), 2);
            nextText = Mathf.Max(nextText, Time.time + 60f);
        }
        void Fail(string why)
        {
            var j = job; job = null; if (j != null && j.crew >= 0) Crews.Bump(j.crew, -4f);
            World.Toast("Job's gone. " + why, 1);
        }

        void ShowBeam(bool on, Vector3 at)
        {
            bool real = on && !float.IsInfinity(at.x);
            if (!real) { if (beam != null && beam.activeSelf) beam.SetActive(false); return; }
            if (beam == null)
            {
                beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder); beam.name = "Job beam"; Destroy(beam.GetComponent<Collider>());
                beam.transform.SetParent(transform, false); beam.transform.localScale = new Vector3(0.7f, 30f, 0.7f);
                var sh = Shader.Find("Universal Render Pipeline/Unlit"); beamMat = new Material(sh != null ? sh : Shader.Find("Unlit/Color"));
                var c = new Color(1f, 0.82f, 0.15f); if (beamMat.HasProperty("_BaseColor")) beamMat.SetColor("_BaseColor", c); else beamMat.color = c;
                var mr = beam.GetComponent<MeshRenderer>(); mr.sharedMaterial = beamMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            }
            if (!beam.activeSelf) beam.SetActive(true);
            // pulse thin and thick so it reads at a distance
            beamT += Time.deltaTime; float w = 0.55f + 0.2f * Mathf.Sin(beamT * 3f);
            beam.transform.position = at + Vector3.up * 30f; beam.transform.localScale = new Vector3(w, 30f, w);
        }

        // ------------------------------------------------------------------ on screen
        void OnGUI()
        {
            if (World.HideHud) return;
            float sc = Mathf.Max(1f, Screen.height / 720f);
            st ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            small ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true };
            btn ??= new GUIStyle(GUI.skin.button);
            st.fontSize = Mathf.RoundToInt(17 * sc); small.fontSize = Mathf.RoundToInt(16 * sc); btn.fontSize = Mathf.RoundToInt(18 * sc);
            if (job != null)
            {
                var pc = Gangs.Player; float left = Mathf.Max(0f, job.limit - (Time.time - job.start));
                string dist = ""; Vector3 t = Target(job);
                if (pc != null && !float.IsInfinity(t.x))
                {
                    Vector3 d = (t - pc.transform.position).WithY(0f); var cam = Camera.main;
                    string arrow = ""; if (cam != null) { float ang = Vector3.SignedAngle(cam.transform.forward.WithY(0f), d, Vector3.up); arrow = new[] { "ahead", "ahead right", "right", "behind right", "behind", "behind left", "left", "ahead left" }[((Mathf.RoundToInt(ang / 45f) % 8) + 8) % 8] + ", "; }
                    dist = "   " + arrow + Mathf.RoundToInt(d.magnitude) + "m";
                }
                string s = job.title.ToUpper() + "  ·  " + job.Next + dist + "   " + Mathf.FloorToInt(left / 60f) + ":" + Mathf.FloorToInt(left % 60f).ToString("00");
                var r = new Rect(Screen.width * 0.2f, 10f * sc, Screen.width * 0.6f, 30f * sc);
                st.normal.textColor = new Color(0, 0, 0, 0.85f); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, st);
                st.normal.textColor = left < 30f ? new Color(1f, 0.45f, 0.35f) : new Color(1f, 0.85f, 0.3f); GUI.Label(r, s, st);
            }
            if (offer != null)
            {
                float w = Mathf.Min(Screen.width - 40f, 460f * sc), h = 210f * sc, x = (Screen.width - w) * 0.5f, y = Screen.height * 0.18f;
                GUI.Box(new Rect(x - 8, y - 8, w + 16, h + 16), "");
                small.normal.textColor = new Color(0.7f, 0.9f, 1f); GUI.Label(new Rect(x, y, w, 24f * sc), "Text from " + offer.from + ":", small);
                small.normal.textColor = Color.white; GUI.Label(new Rect(x, y + 26f * sc, w, 110f * sc), "\"" + offer.brief + "\"  (" + Econ.Fmt(offer.pay) + ")", small);
                float bw = (w - 12f) * 0.5f, by = y + h - 56f * sc;
                if (GUI.Button(new Rect(x, by, bw, 52f * sc), "I'm on it", btn)) Accept();
                if (GUI.Button(new Rect(x + bw + 12f, by, bw, 52f * sc), "Not now", btn)) offer = null;
            }
        }

        /// <summary>Test: make one of each and say what came out (and where), then start the first and drop it.</summary>
        public static void Test()
        {
            if (I == null) { Debug.Log("[ASBO] jobtest: no missions"); return; }
            foreach (Kind k in System.Enum.GetValues(typeof(Kind)))
                for (int crew = -1; crew < 3; crew += 2)
                {
                    var j = I.Make(k, crew);
                    Debug.Log(j == null ? $"[ASBO] jobtest {k} crew {crew}: NONE" : $"[ASBO] jobtest {k} crew {crew}: {j.title} '{j.brief}' a {j.a} b {j.b} home {(j.home != null ? j.home.name : "-")} target {(j.target != null ? j.target.name : "-")} pay {j.pay} limit {j.limit:0}");
                }
            I.offer = I.Make(Kind.Lesson, 0); I.Accept(); Debug.Log($"[ASBO] jobtest accepted: {(I.job != null ? I.job.Next : "none")}");
        }
    }
}
