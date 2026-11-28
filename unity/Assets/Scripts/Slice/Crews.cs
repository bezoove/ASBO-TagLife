using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Where you stand with each of the three crews, and it sticks. Batter their lads, rob on their ends, and it drops; buy off
    /// their dealer, run their jobs, and it climbs. Hated (-30) and they jump you on sight anywhere; hunted (-60) and they send
    /// a squad out after you. Rate you enough (+20) and the dealer offers you in: join, and your own crew back you in a fight,
    /// leave you be on their ends, and the other two take it personally. Grudges fade slowly. Kept between sessions.
    /// </summary>
    public static class Crews
    {
        public static readonly float[] Standing = new float[3];
        public static int Affiliation = -1;
        static readonly float[] changedAt = { -99f, -99f, -99f };
        static bool loaded; static float saveAt;

        public static string Name(int g) => TownPlan.Turfs[g].gang;
        public static bool Hates(int g) => g >= 0 && g < 3 && g != Affiliation && Standing[g] <= -30f;
        public static bool Hunting(int g) => g >= 0 && g < 3 && g != Affiliation && Standing[g] <= -60f;
        public static bool CanJoin(int g) => Affiliation < 0 && g >= 0 && Standing[g] >= 20f;
        public static float ChangedAt(int g) => changedAt[g];
        public static string Word(int g)
        {
            if (g == Affiliation) return "your lot";
            float v = Standing[g]; return v <= -60f ? "want you dead" : v <= -30f ? "hate you" : v <= -8f ? "wary of you" : v < 8f ? "don't know you" : v < 20f ? "rate you" : "rate you highly";
        }

        public static void Load()
        {
            if (loaded) return; loaded = true;
            for (int g = 0; g < 3; g++) Standing[g] = PlayerPrefs.GetFloat("asbo.crew" + g, 0f);
            Affiliation = PlayerPrefs.GetInt("asbo.crewAff", -1);
        }
        static void Save() { for (int g = 0; g < 3; g++) PlayerPrefs.SetFloat("asbo.crew" + g, Standing[g]); PlayerPrefs.SetInt("asbo.crewAff", Affiliation); saveAt = 0f; }

        /// <summary>Shift your standing with crew g; crossing a line gets said out loud.</summary>
        public static void Bump(int g, float d)
        {
            if (g < 0 || g > 2 || Mathf.Abs(d) < 0.01f) return; Load();
            float was = Standing[g], floor = g == Affiliation ? 10f : -100f;
            Standing[g] = Mathf.Clamp(was + d, floor, 100f); changedAt[g] = Time.time; saveAt = Time.time + 3f;
            float v = Standing[g];
            if (g != Affiliation)
            {
                if (was > -30f && v <= -30f) World.Toast(Cap(Name(g)) + " have had enough of you. Stay off their ends.", 2);
                else if (was > -60f && v <= -60f) World.Toast(Cap(Name(g)) + " want you gone. They'll come looking.", 2);
                else if (was < 20f && v >= 20f && Affiliation < 0) World.Toast(Cap(Name(g)) + " are starting to rate you. Their dealer might have a word.", 0);
            }
        }
        static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        public static void Join(int g)
        {
            if (!CanJoin(g)) return;
            Affiliation = g; Standing[g] = Mathf.Max(Standing[g], 50f); changedAt[g] = Time.time;
            for (int o = 0; o < 3; o++) if (o != g) Bump(o, -25f);
            Rel.Rep += 10f; Save();
            World.Toast("You're in with " + Name(g) + ". The other two won't like it.", 2);
        }

        // ------------------------------------------------------------------ what you do to them
        internal static void OnHit(Fighter att, Fighter vic, float dmg)
        {
            if (att == null || vic == null) return;
            var a = att.Body; var v = vic.Body; if (a == null || v == null) return;
            if (a.isPlayer)
            {
                int g = Gangs.GangOf(v); if (g < 0) return;
                bool down = v.state != Person.State.Walking || v.dead;
                Bump(g, -(dmg * 0.25f + (down ? 10f : 0f)));
                for (int o = 0; o < 3; o++) if (o != g && o != Affiliation) Bump(o, dmg * 0.04f);     // the enemy of their enemy
                if (down && g != Affiliation) Rel.Rep += 1.5f;
            }
            else if (v.isPlayer && Affiliation >= 0) CrewsRun.BackUp(att);
        }
        internal static void OnCrime(string what, Vector3 at)
        {
            int g = TownPlan.TurfAt(at); if (g < 0 || g == Affiliation) return;
            Bump(g, -3f);                                          // robbing on somebody's ends is disrespect
        }

        internal static void Tick(float dt)
        {
            // grudges and favours fade towards where they'd sit anyway
            for (int g = 0; g < 3; g++)
            {
                float target = g == Affiliation ? 40f : 0f, v = Standing[g];
                if (Mathf.Abs(v - target) > 0.01f) Standing[g] = Mathf.MoveTowards(v, target, dt * (0.6f / 60f));
            }
            if (saveAt > 0f && Time.time > saveAt) Save();
        }
    }

    /// <summary>The crews' grudges in the world: jumped on sight, squads sent out, your own lot backing you up, the standings on screen.</summary>
    public class CrewsRun : MonoBehaviour
    {
        static CrewsRun I;
        readonly List<(Person p, int gang)> foot = new List<(Person, int)>();
        readonly List<Person> squad = new List<Person>(); int squadGang = -1; float squadUntil, nextSquad, tick; readonly float[] jumpCool = new float[3];
        GUIStyle st;

        public static void Ensure(Transform parent)
        {
            if (I != null) return; Crews.Load();
            var g = new GameObject("Crews"); g.transform.SetParent(parent, false); I = g.AddComponent<CrewsRun>();
            Fighter.OnHit -= Crews.OnHit; Fighter.OnHit += Crews.OnHit;
            Offences.OnCrime -= Crews.OnCrime; Offences.OnCrime += Crews.OnCrime;
            I.nextSquad = Time.time + 120f;
        }
        void OnDestroy() { Fighter.OnHit -= Crews.OnHit; Offences.OnCrime -= Crews.OnCrime; if (I == this) I = null; }

        void Update()
        {
            Crews.Tick(Time.deltaTime);
            if ((tick -= Time.deltaTime) > 0f) return; tick = 0.5f;
            var pc = Gangs.Player; if (pc == null) return; Vector3 pp = pc.transform.position;
            var pf = Fighter.Session.PlayerFighter;
            Gangs.Foot(foot);
            // hated: whoever of theirs clocks you comes over with a couple of mates
            if (!pc.InCar && pf != null)
                for (int g = 0; g < 3; g++)
                {
                    if (!Crews.Hates(g) || Time.time < jumpCool[g]) continue;
                    Person seer = null;
                    foreach (var (p, pg) in foot) if (pg == g && (p.transform.position - pp).sqrMagnitude < 15f * 15f && NoiseBus.CanSee(p, pc.transform)) { seer = p; break; }
                    if (seer == null) continue;
                    jumpCool[g] = Time.time + 35f; int sent = 0;
                    foreach (var (p, pg) in foot)
                    {
                        if (pg != g || sent >= 3 || (p.transform.position - seer.transform.position).sqrMagnitude > 18f * 18f) continue;
                        var f = p.GetComponent<Fighter>(); if (f == null || f.Fighting) continue; f.SquareUp(pf, true); sent++;
                    }
                    World.Toast(new[] { "\"That's him!\"", "\"Oi! You've got some nerve.\"", "\"Get him!\"" }[Random.Range(0, 3)], 2);
                }
            // hunted: a squad comes out after you, from somewhere you can't see
            if (squad.Count > 0)
            {
                squad.RemoveAll(p => p == null || p.dead);
                bool far = true; foreach (var p in squad) if ((p.transform.position - pp).sqrMagnitude < 70f * 70f) far = false;
                if (squad.Count == 0 || Time.time > squadUntil && far) { foreach (var p in squad) Gangs.Drop(p); squad.Clear(); }
                else if (pf != null) foreach (var p in squad) { var f = p.GetComponent<Fighter>(); if (f != null && !f.Fighting && !f.Down) f.SquareUp(pf, true); }
            }
            else if (Time.time > nextSquad)
            {
                int g = -1; for (int o = 0; o < 3; o++) if (Crews.Hunting(o) && (g < 0 || Crews.Standing[o] < Crews.Standing[g])) g = o;
                nextSquad = Time.time + Random.Range(150f, 240f);
                if (g >= 0 && TrySpawnSquad(g, pp)) World.Toast(char.ToUpper(Crews.Name(g)[0]) + Crews.Name(g).Substring(1) + " are out looking for you.", 2);
            }
        }

        /// <summary>Test: lay into a Kestrel lad (fake hits), check the standing drops; make T-Bone's lot hunt you, check a squad comes.</summary>
        public static void Test() { if (I != null) I.StartCoroutine(I.RunTest()); }
        System.Collections.IEnumerator RunTest()
        {
            var pf = Fighter.Session.PlayerFighter; Gangs.Foot(foot); Fighter vic = null;
            foreach (var (p, g) in foot) if (g == 1) { vic = p.GetComponent<Fighter>(); if (vic != null) break; }
            float s0 = Crews.Standing[1];
            if (pf != null && vic != null) for (int i = 0; i < 4; i++) Crews.OnHit(pf, vic, 20f);
            Debug.Log($"[ASBO] crewtest: hit a Kestrel lad x4: standing {s0:0} -> {Crews.Standing[1]:0} ({Crews.Word(1)}), hates {Crews.Hates(1)}; pf {(pf != null)} vic {(vic != null)}");
            Crews.Bump(2, -80f - Crews.Standing[2]); nextSquad = Time.time; yield return new WaitForSeconds(1.5f);
            Debug.Log($"[ASBO] crewtest: T-Bone's lot {Crews.Standing[2]:0} ({Crews.Word(2)}), hunting {Crews.Hunting(2)}, squad {squad.Count} of gang {squadGang}");
            Crews.Bump(1, -Crews.Standing[1]); Crews.Bump(2, -Crews.Standing[2]);   // put it back
        }

        bool TrySpawnSquad(int g, Vector3 pp)
        {
            // somewhere people walk, out of sight, 30 to 60 m off
            Vector3 at = Vector3.zero; bool found = false; var cam = Camera.main;
            for (int i = 0; i < 30 && !found; i++)
            {
                var o = Person.All[Random.Range(0, Person.All.Count)]; if (o == null || o.isPlayer || o.dead) continue;
                Vector3 c = o.transform.position; float d = (c - pp).magnitude; if (d < 30f || d > 60f) continue;
                if (cam != null) { var vp = cam.WorldToViewportPoint(c); if (vp.z > 0f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f) continue; }
                at = c + new Vector3(Random.Range(-1.5f, 1.5f), 0.15f, Random.Range(-1.5f, 1.5f)); found = true;
            }
            if (!found) return false;
            squadGang = g; squadUntil = Time.time + 100f;
            for (int i = 0; i < 3; i++) { var p = Gangs.Squad(at + new Vector3(i * 0.9f, 0f, (i % 2) * 0.9f), g); if (p != null) squad.Add(p); }
            return squad.Count > 0;
        }

        /// <summary>Somebody's laying into you: any of your crew close by pile in on him.</summary>
        internal static void BackUp(Fighter attacker)
        {
            if (I == null || attacker == null || Crews.Affiliation < 0 || Gangs.GangOf(attacker.Body) == Crews.Affiliation) return;
            Gangs.Foot(I.foot); int sent = 0; Vector3 at = attacker.transform.position;
            foreach (var (p, g) in I.foot)
            {
                if (g != Crews.Affiliation || sent >= 2 || (p.transform.position - at).sqrMagnitude > 20f * 20f) continue;
                var f = p.GetComponent<Fighter>(); if (f == null || f.Fighting || f.Down) continue; f.SquareUp(attacker, true); sent++;
            }
            if (sent > 0) World.Toast("Your lot pile in.", 0);
        }

        void OnGUI()
        {
            if (World.HideHud) return;
            bool any = false; for (int g = 0; g < 3; g++) if (Mathf.Abs(Crews.Standing[g]) > 6f || Time.time - Crews.ChangedAt(g) < 6f || g == Crews.Affiliation) any = true;
            if (!any) return;
            float sc = Mathf.Max(1f, Screen.height / 720f);
            st ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
            st.fontSize = Mathf.RoundToInt(14 * sc);
            float w = 260f * sc, h = 20f * sc, x = Screen.width - w - 16f, y = 16f + Screen.height * 0.06f + (Offences.Wanted >= 4f ? Mathf.Max(18, Screen.height / 28) * 1.6f : 0f);
            for (int g = 0; g < 3; g++)
            {
                if (!(Mathf.Abs(Crews.Standing[g]) > 6f || Time.time - Crews.ChangedAt(g) < 6f || g == Crews.Affiliation)) continue;
                var c = TownPlan.Turfs[g].colour; string s = TownPlan.Turfs[g].tag + "  " + Crews.Word(g);
                float bw = 70f * sc, bx = x + w - bw, by = y + h * 0.7f;
                st.normal.textColor = new Color(0, 0, 0, 0.8f); GUI.Label(new Rect(x + 1, y + 1, w - bw - 8f * sc, h), s, st);
                st.normal.textColor = Color.Lerp(c, Color.white, 0.25f); GUI.Label(new Rect(x, y, w - bw - 8f * sc, h), s, st);
                // the bar: grey track, then the standing either side of the middle
                var old = GUI.color; GUI.color = new Color(0, 0, 0, 0.55f); GUI.DrawTexture(new Rect(bx, by - 3f * sc, bw, 6f * sc), Texture2D.whiteTexture);
                float v = Crews.Standing[g] / 100f; GUI.color = v >= 0f ? new Color(0.45f, 0.9f, 0.4f) : new Color(0.95f, 0.3f, 0.25f);
                float mid = bx + bw * 0.5f; GUI.DrawTexture(new Rect(v >= 0f ? mid : mid + v * bw * 0.5f, by - 3f * sc, Mathf.Abs(v) * bw * 0.5f, 6f * sc), Texture2D.whiteTexture);
                GUI.color = old; y += h;
            }
        }
    }
}
