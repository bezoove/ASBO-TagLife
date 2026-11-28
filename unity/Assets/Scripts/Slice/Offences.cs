using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// The law. Crimes only count if somebody who can see you sees them. Seen, you're Wanted (0 to 100, shown as stars) and
    /// it fades while you keep your head down. The police are a nuisance, not the real danger: a bobby who sees you while
    /// you're Wanted comes over, tells you to behave (Wanted drops a bit), and if you're properly Wanted nicks you:
    /// a fine out of your pocket and you wake up outside the station.
    /// </summary>
    public static class Offences
    {
        public static float Wanted;
        static float last;

        /// <summary>A crime has happened at 'at'. If somebody can see the player, it counts.</summary>
        /// <summary>Every crime, seen or not: the crews care what happens on their ends.</summary>
        public static System.Action<string, Vector3> OnCrime;
        public static void Report(string what, float heat, Vector3 at)
        {
            OnCrime?.Invoke(what, at);
            var pc = Object.FindFirstObjectByType<PlayerCtl>(); if (pc == null) return;
            Person by = null;
            foreach (var p in Person.All)
            {
                if (p == null || p.isPlayer || p.dead || p.state != Person.State.Walking) continue;
                Vector3 d = p.transform.position - at; d.y = 0f; if (d.sqrMagnitude > 900f) continue;
                if (!NoiseBus.CanSee(p, pc.transform)) continue;
                if (by == null || p.GetComponent<Bobby>() != null) by = p;
                if (p.GetComponent<Bobby>() != null) { heat *= 1.5f; break; }
            }
            if (by == null) return;
            Wanted = Mathf.Min(100f, Wanted + heat); last = Time.time;
            by.Noticed(pc.transform);
            World.Toast(what + (by.GetComponent<Bobby>() != null ? ". A policeman saw." : ". Somebody saw."), 1);
        }

        public static void Tick(float dt)
        {
            if (Wanted <= 0f || Time.time - last < 12f) return;
            Wanted = Mathf.Max(0f, Wanted - 1.2f * dt); // out of sight for a bit: it fades
        }
    }

    /// <summary>Spawns the beat bobbies, draws the stars, ticks the heat down.</summary>
    public class OffencesRun : MonoBehaviour
    {
        static OffencesRun I; GUIStyle st; bool spawned;
        public static void Ensure() { if (I == null) { var g = new GameObject("Offences"); I = g.AddComponent<OffencesRun>(); } }

        void Update()
        {
            Offences.Tick(Time.deltaTime);
            if (!spawned && Time.time > 12f && Person.All.Count > 6) { spawned = true; for (int i = 0; i < 2; i++) SpawnBobby(i); }
        }
        void SpawnBobby(int i)
        {
            Vector3 at = Services.PoliceDesk + new Vector3(6f + i * 3f, 0f, 6f);
            if (Services.PoliceBays.Count > i) at = Services.PoliceBays[i].p + new Vector3(2.5f, 0f, 0f);
            if (Physics.Raycast(at + Vector3.up * 3f, Vector3.down, out var h, 8f, 1 << 0, QueryTriggerInteraction.Ignore)) at = h.point;
            var p = World.MakePerson(at + Vector3.up * 0.1f, new Color(0.9f, 0.92f, 0.95f), new Color(0.07f, 0.07f, 0.1f), "PC");
            if (p != null) p.gameObject.AddComponent<Bobby>();
        }

        void OnGUI()
        {
            if (Offences.Wanted < 4f || World.HideHud) return;
            st ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = Mathf.Max(18, Screen.height / 28), fontStyle = FontStyle.Bold };
            int stars = Mathf.Clamp(Mathf.CeilToInt(Offences.Wanted / 20f), 1, 5);
            string s = new string('★', stars) + new string('☆', 5 - stars);
            var r = new Rect(Screen.width - 320f - 16f, 16f + Screen.height * 0.06f, 320f, st.fontSize * 1.5f);
            st.normal.textColor = new Color(0, 0, 0, 0.8f); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, st);
            st.normal.textColor = new Color(0.5f, 0.7f, 1f); GUI.Label(r, s, st);
        }
    }

    /// <summary>A beat bobby: walks about the estate, and when he sees you while you're Wanted he comes over.</summary>
    public class Bobby : MonoBehaviour
    {
        enum S { Patrol, Pause, Chase, Talk }
        S s; Person p; PlayerCtl pc; Vector3 dest; float t, lostT, cool, stuckT; Vector3 lastPos;
        static readonly string[] Warn = { "\"Hello, hello, hello. What's all this, then?\"", "\"Oi. Behave yourself.\"", "\"I've got my eye on you, son.\"", "\"Move along. Nothing to see here.\"" };

        void Start() { p = GetComponent<Person>(); if (p != null) p.agentDriven = true; Pick(); }

        void Pick()
        {
            s = S.Patrol; t = Time.time + Random.Range(18f, 30f); stuckT = 0f;
            // somewhere people walk: next to a random resident
            for (int i = 0; i < 12; i++)
            {
                var o = Person.All[Random.Range(0, Person.All.Count)]; if (o == null || o == p || o.isPlayer) continue;
                dest = o.transform.position; return;
            }
            dest = transform.position + new Vector3(Random.Range(-20f, 20f), 0f, Random.Range(-20f, 20f));
        }

        void Update()
        {
            if (p == null || p.dead || p.state != Person.State.Walking) return;
            if (pc == null) { pc = Object.FindFirstObjectByType<PlayerCtl>(); if (pc == null) return; }
            Vector3 to = pc.transform.position - transform.position; to.y = 0f; float d = to.magnitude;
            bool sees = d < 24f && NoiseBus.CanSee(p, pc.transform);
            if (s != S.Talk && s != S.Chase && Offences.Wanted >= 20f && sees) { s = S.Chase; lostT = 0f; World.Toast("\"Oi! You! Stop there!\"", 1); }
            switch (s)
            {
                case S.Patrol:
                    {
                        Vector3 dd = dest - transform.position; dd.y = 0f;
                        if (dd.magnitude < 2.5f || Time.time > t) { s = S.Pause; t = Time.time + Random.Range(3f, 7f); p.Animate(0f); break; }
                        p.Step(dest, 1.5f);
                        stuckT += Time.deltaTime; if (stuckT > 2f) { stuckT = 0f; if ((transform.position - lastPos).sqrMagnitude < 0.25f) Pick(); lastPos = transform.position; }
                        break;
                    }
                case S.Pause: p.Animate(0f); if (Time.time > t) Pick(); break;
                case S.Chase:
                    {
                        if (!sees) lostT += Time.deltaTime; else lostT = 0f;
                        if (lostT > 8f || Offences.Wanted < 8f) { Pick(); break; }
                        if (d > 1.5f) { p.Step(pc.transform.position, 3.8f); break; }
                        p.Animate(0f);
                        if (Time.time < cool) break;
                        cool = Time.time + 6f; s = S.Talk; t = Time.time + 1.6f;
                        if (Offences.Wanted >= 45f) Arrest(); else { World.Toast(Warn[Random.Range(0, Warn.Length)], 1); Offences.Wanted = Mathf.Max(0f, Offences.Wanted - 10f); }
                        break;
                    }
                case S.Talk: p.Animate(0f); if (Time.time > t) { if (Offences.Wanted >= 20f && sees) s = S.Chase; else Pick(); } break;
            }
        }

        void Arrest()
        {
            float fine = Mathf.Min(Econ.Cash, 20f + Offences.Wanted * 0.8f);
            Econ.Cash -= fine; Offences.Wanted = 0f;
            Vector3 out0 = Services.PoliceBays.Count > 0 ? Services.PoliceBays[0].p + new Vector3(2f, 0.1f, 0f) : Services.PoliceDesk + new Vector3(0f, 0.1f, 4f);
            pc.Teleport(out0, 90f, 8f);
            World.Toast($"\"You're nicked.\" A night in the cells and a {Econ.Fmt(fine)} fine. You wake up outside the station.", 1);
            Pick();
        }
    }
}
