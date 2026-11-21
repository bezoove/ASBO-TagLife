using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ASBO.Slice
{
    /// <summary>Runtime NavMesh over the estate and road (built once after the world geometry exists).</summary>
    public static class Nav
    {
        public static bool Ready; public static int AgentType;
        static NavMeshDataInstance inst;

        public static void Build(Bounds b)
        {
            if (inst.valid) inst.Remove();
            var s = NavMesh.CreateSettings();
            s.agentRadius = 0.24f; s.agentHeight = 1.75f; s.agentClimb = 0.3f; s.agentSlope = 40f;
            s.overrideVoxelSize = true; s.voxelSize = 0.08f; s.overrideTileSize = true; s.tileSize = 160; s.minRegionArea = 0.5f;
            var src = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(b, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), src);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var data = NavMeshBuilder.BuildNavMeshData(s, src, b, Vector3.zero, Quaternion.identity);
            inst = NavMesh.AddNavMeshData(data);
            AgentType = s.agentTypeID; Ready = true;
            var tri = NavMesh.CalculateTriangulation();
            bool a = Snap(new Vector3(0, 0, Estate.LaneZ), out var qa, 1f), c = Snap(new Vector3(0, 0, -30f), out var qc, 1f);
            Debug.Log("[ASBO] NavMesh built from " + src.Count + " sources in " + sw.ElapsedMilliseconds + " ms, tris=" + tri.indices.Length / 3 + " lane=" + a + " road=" + c + " type=" + AgentType);
        }

        /// <summary>Use the NavMesh baked into the scene (default agent type, tuned for doorways in project settings).</summary>
        public static void UseBaked() { AgentType = 0; Ready = true; }

        public static bool Snap(Vector3 p, out Vector3 q, float r = 1.5f)
        {
            var f = new NavMeshQueryFilter { agentTypeID = AgentType, areaMask = NavMesh.AllAreas };
            if (NavMesh.SamplePosition(p, out var hit, r, f)) { q = hit.position; return true; }
            q = p; return false;
        }
    }

    /// <summary>
    /// An Abbots Wend resident: potters about the house (upstairs too), tends the front garden, strolls the lane,
    /// calls on neighbours and passes the time of day with anyone they meet.
    /// </summary>
    public class Resident : MonoBehaviour
    {
        public CotHouse home; public string arch;
        Person p; NavMeshAgent ag; float wait = 1f, chatCd; string doing = "";
        // trouble: who we're fighting (or running from), until when, and how often we swing
        Fighter fleeFrom, fighter; float angryUntil, baseSpeed;
        Transform watching; float watchUntil, cheerUntil;
        Transform talkTo; bool coming; float comeRepath;
        /// <summary>Not busy with anything that matters (fighting, running off, watching a scrap, talking to you).</summary>
        public bool Free => p != null && p.state == Person.State.Walking && enabled && watching == null && fleeFrom == null && talkTo == null && doing != "fight" && doing != "hold" && (fighter == null || !fighter.Fighting);
        static readonly List<Resident> all = new List<Resident>();
        public string Doing => doing;
        public static int Count;
        public static string Stats() { int on = 0, mv = 0; foreach (var r in all) { if (r.ag != null && r.ag.enabled && r.ag.isOnNavMesh) on++; if (r.ag != null && r.ag.enabled && r.ag.velocity.sqrMagnitude > 0.01f) mv++; } return "residents " + all.Count + " on-nav " + on + " moving " + mv; }

        public static Resident Spawn(CotHouse h, int idx, System.Random r)
        {
            var look = Look(h.pl.arch, idx, r);
            Vector3 at = h.root.TransformPoint(h.MainRoomLocal()) + new Vector3(((float)r.NextDouble() - 0.5f), 0, ((float)r.NextDouble() - 0.5f));
            Nav.Snap(at, out at, 2f);
            var person = World.MakePerson(at, look.shirt, look.trousers, h.pl.arch + "@" + h.pl.name, false, ModelFor(h.pl.arch, idx));
            person.agentDriven = true;
            var res = person.gameObject.AddComponent<Resident>();
            res.home = h; res.arch = h.pl.arch; res.wait = 1f + (float)r.NextDouble() * 4f;
            return res;
        }

        static string ModelFor(string arch, int idx)
        {
            switch (arch)
            {
                case "colonel": return idx == 0 ? "m_old_jacket_bald" : "f_old_skirt_bun";
                case "widow": return "f_old_jacket_bun";
                case "barrister": return idx == 0 ? "m_fit_jacket_short" : "f_slim_shirt_long";
                case "banker": return idx == 0 ? "m_heavy_shirt_bald" : "f_curvy_skirt_bob";
                case "horsey": return idx == 0 ? "f_slim_shirt_long" : "m_heavy_jacket_short";
                case "gentry": return idx == 0 ? "m_old_shirt_bald" : "f_old_jacket_bun";
                case "surgeon": return idx == 0 ? "m_young_shirt_short" : "f_young_skirt_bob";
                case "author": return idx == 0 ? "m_fit_tee_short" : "f_young_tee_long";
                case "gallery": return idx == 0 ? "f_slim_dress_bob" : "m_young_tee_short";
                case "antiques": return idx == 0 ? "m_heavy_jacket_short" : "f_curvy_jacket_bun";
                default: return null;
            }
        }

        struct LookS { public Color shirt, trousers; }
        static LookS Look(string arch, int idx, System.Random r)
        {
            Color tweed = new Color(0.42f, 0.33f, 0.22f), navy = new Color(0.12f, 0.15f, 0.28f), cord = new Color(0.45f, 0.38f, 0.25f), cream = new Color(0.85f, 0.8f, 0.68f), grey = new Color(0.35f, 0.36f, 0.38f);
            switch (arch)
            {
                case "colonel": return new LookS { shirt = idx == 0 ? tweed : new Color(0.55f, 0.6f, 0.45f), trousers = cord };
                case "widow": return new LookS { shirt = new Color(0.55f, 0.45f, 0.6f), trousers = grey };
                case "barrister": return new LookS { shirt = navy, trousers = navy };
                case "banker": return new LookS { shirt = idx == 0 ? grey : new Color(0.7f, 0.3f, 0.35f), trousers = grey };
                case "horsey": return new LookS { shirt = new Color(0.2f, 0.32f, 0.2f), trousers = cream };
                case "gentry": return new LookS { shirt = idx == 0 ? tweed : new Color(0.3f, 0.4f, 0.55f), trousers = cord };
                case "surgeon": return new LookS { shirt = new Color(0.6f, 0.7f, 0.8f), trousers = navy };
                case "author": return new LookS { shirt = new Color(0.6f, 0.25f, 0.2f), trousers = new Color(0.25f, 0.22f, 0.2f) };
                case "gallery": return new LookS { shirt = new Color(0.1f, 0.1f, 0.1f), trousers = new Color(0.1f, 0.1f, 0.1f) };
                default: return new LookS { shirt = Color.HSVToRGB((float)r.NextDouble(), 0.4f, 0.6f), trousers = grey };
            }
        }

        void Start()
        {
            p = GetComponent<Person>();
            all.Add(this);
            gameObject.SetActive(false); // create the agent while inactive so it binds to our agent type, not the default
            ag = gameObject.AddComponent<NavMeshAgent>();
            ag.agentTypeID = Nav.AgentType;
            ag.enabled = false;
            gameObject.SetActive(true); ag.radius = 0.24f; ag.height = 1.75f; ag.baseOffset = 0f;
            ag.speed = p != null && p.drunk ? Random.Range(0.58f, 0.68f) : Random.Range(1.0f, 1.35f); ag.angularSpeed = 300f; ag.acceleration = 3f; ag.stoppingDistance = 0.25f; ag.autoBraking = true;
            ag.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; ag.avoidancePriority = Random.Range(30, 70);
            baseSpeed = ag.speed;
            if (Nav.Snap(transform.position, out var q, 2f)) transform.position = q;
            ag.enabled = true;
            if (ag.isOnNavMesh) ag.Warp(transform.position);
            Count++;
        }
        void OnDestroy() { all.Remove(this); }

        void OnEnable() { if (ag != null && ag.enabled && ag.isOnNavMesh) { ag.isStopped = false; fleeFrom = null; watching = null; wait = 1f; } }

        void Update()
        {
            if (p == null || ag == null || home == null || home.root == null) return;
            if (p.state != Person.State.Walking) { if (ag.enabled) ag.enabled = false; enabled = false; return; }
            if (!ag.isOnNavMesh) return;
            fighter ??= GetComponent<Fighter>();
            if (fighter != null && fighter.Fighting) return; // the fighter drives us while we're scrapping
            float v = ag.velocity.magnitude;
            if (v > 0.08f) p.Animate(v);
            if (watching != null) { Watching(); return; }
            if (talkTo != null) { TalkStep(); return; }
            if (fleeFrom != null)
            {
                if (Time.time > angryUntil || (!ag.pathPending && ag.remainingDistance < 0.5f)) { fleeFrom = null; ag.speed = baseSpeed; doing = ""; wait = Random.Range(2f, 5f); }
                return;
            }
            chatCd -= Time.deltaTime;
            if (!ag.pathPending && ag.remainingDistance <= ag.stoppingDistance + 0.1f)
            {
                if (doing == "visit" || doing == "lane" || doing == "gate") Chat();
                wait -= Time.deltaTime;
                if (wait <= 0f) Next();
            }
        }

        void Chat()
        {
            if (chatCd > 0f || GameAudio.I == null) return;
            foreach (var o in all)
            {
                if (o == this || o.p == null || o.p.state != Person.State.Walking) continue;
                if ((o.transform.position - transform.position).sqrMagnitude < 9f)
                {
                    chatCd = 25f + Random.value * 20f; o.chatCd = Mathf.Max(o.chatCd, 6f);
                    Vector3 d = o.transform.position - transform.position; d.y = 0;
                    if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
                    GameAudio.I.PlayVoice("resident", transform.position + Vector3.up * 1.6f);
                    StartCoroutine(TalkFor(4f + Random.value * 3f)); o.StartCoroutine(o.TalkFor(4f + Random.value * 3f));
                    return;
                }
            }
        }

        /// <summary>A scrap has started with us in it: stop the routine (the Fighter moves us now).</summary>
        public void EnterFight()
        {
            if (ag == null) return;
            if (baseSpeed <= 0f) baseSpeed = ag.speed;
            watching = null; fleeFrom = null; talkTo = null; doing = "fight"; p.SetWave(false); p.SetTalk(false);
            if (ag.enabled && ag.isOnNavMesh) { ag.isStopped = true; ag.ResetPath(); }
        }

        /// <summary>Out of the scrap. Legging it: run off home (or just away), then carry on. Otherwise back to the day.</summary>
        public void ExitFight(Fighter runFrom)
        {
            if (ag == null || !ag.enabled || !ag.isOnNavMesh) return;
            if (baseSpeed <= 0f) baseSpeed = ag.speed;
            ag.isStopped = false; ag.updateRotation = true;
            if (runFrom != null)
            {
                fleeFrom = runFrom; angryUntil = Time.time + 15f; doing = "flee";
                Vector3 dest;
                if (!HomePoint(out dest))
                {
                    Vector3 away = (transform.position - runFrom.transform.position).WithY(0); if (away.sqrMagnitude < 0.01f) away = transform.forward;
                    if (!Nav.Snap(transform.position + away.normalized * 18f, out dest, 4f)) dest = transform.position;
                }
                ag.speed = p.hurt ? 1.2f : 3.4f; ag.SetDestination(dest);
                if (GameAudio.I != null) GameAudio.I.Pain(transform.position + Vector3.up * 1.6f, p.male, 0.1f);
            }
            else { doing = ""; ag.speed = baseSpeed; wait = Random.Range(1.5f, 4f); }
        }

        /// <summary>Somebody's scrapping nearby: stop, turn and watch for a while. The drunk and lairy cheer them on; others wince.</summary>
        public void Watch(Transform t, float secs, bool cheer)
        {
            if (ag == null || !ag.enabled || !ag.isOnNavMesh || p.state != Person.State.Walking || doing == "flee" || talkTo != null) return;
            if (baseSpeed <= 0f) baseSpeed = ag.speed;
            watching = t; watchUntil = Time.time + secs; doing = "watch";
            ag.isStopped = true; ag.ResetPath();
            if (cheer) { cheerUntil = Time.time + 1.8f; StartCoroutine(CheerFor(Random.Range(0.1f, 0.9f))); }
            else StartCoroutine(Wince(Random.Range(0.1f, 0.9f)));
        }
        System.Collections.IEnumerator CheerFor(float delay) { yield return new WaitForSeconds(delay); if (p == null || watching == null) yield break; p.SetWave(true); yield return new WaitForSeconds(1.8f); if (p != null) p.SetWave(false); }
        System.Collections.IEnumerator Wince(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (p == null || watching == null || ag == null || !ag.enabled) yield break;
            Vector3 away = (transform.position - watching.position).WithY(0); if (away.sqrMagnitude < 0.01f) yield break;
            for (float t = 0f; t < 0.35f; t += Time.deltaTime) { if (ag.enabled && ag.isOnNavMesh) ag.Move(away.normalized * 1.2f * Time.deltaTime); yield return null; }
        }
        void Watching()
        {
            if (watching == null || Time.time > watchUntil || !watching.gameObject.activeInHierarchy)
            {
                watching = null; doing = ""; p.SetWave(false); ag.isStopped = false; ag.speed = baseSpeed > 0f ? baseSpeed : ag.speed; wait = Random.Range(0.5f, 2f);
                return;
            }
            Vector3 d = (watching.position - transform.position).WithY(0);
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 240f * Time.deltaTime);
        }

        /// <summary>Walk over to the player to talk to them (they answer with TALK).</summary>
        public void ComeOver(Transform player)
        {
            if (ag == null || !ag.enabled || !ag.isOnNavMesh) return;
            if (baseSpeed <= 0f) baseSpeed = ag.speed;
            talkTo = player; coming = true; doing = "approach"; comeRepath = 0f;
            ag.isStopped = false; ag.speed = p.drunk ? baseSpeed : Mathf.Max(baseSpeed, 1.25f);
        }

        /// <summary>Stand and talk to the player (on), or carry on with the day (off).</summary>
        public void TalkWith(Transform player, bool on)
        {
            if (ag == null) return;
            if (baseSpeed <= 0f) baseSpeed = ag.speed;
            if (on) { talkTo = player; coming = false; doing = "talkp"; if (ag.enabled && ag.isOnNavMesh) { ag.isStopped = true; ag.ResetPath(); } }
            else if (talkTo != null) { talkTo = null; coming = false; doing = ""; p.SetTalk(false); if (ag.enabled && ag.isOnNavMesh) { ag.isStopped = false; ag.speed = baseSpeed; } wait = Random.Range(1f, 3f); }
        }

        void TalkStep()
        {
            Vector3 d = (talkTo.position - transform.position).WithY(0); float dist = d.magnitude;
            if (coming)
            {
                if (dist > 1.5f)
                {
                    ag.isStopped = false;
                    if (Time.time > comeRepath) { comeRepath = Time.time + 0.5f; if (Nav.Snap(talkTo.position - d.normalized * 1.2f, out var dest, 2f)) ag.SetDestination(dest); }
                    return;
                }
                ag.isStopped = true; ag.ResetPath(); // close enough: wait to be answered
            }
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 300f * Time.deltaTime);
        }

        /// <summary>Stand still at a spot for a while (scripted tests, cut-scenes), then carry on with the day.</summary>
        public void HoldAt(Vector3 pos, float yaw, float secs)
        {
            if (ag != null && ag.enabled && ag.isOnNavMesh) { ag.Warp(pos); ag.ResetPath(); } else transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f); wait = secs; doing = "hold";
        }

        public System.Collections.IEnumerator TalkFor(float t)
        {
            if (p == null) yield break;
            p.SetTalk(true); yield return new WaitForSeconds(t); if (p != null) p.SetTalk(false);
        }

        // ---- bedtime: most turn in late evening, night owls (and drunks) are out till the small hours; up again in the morning
        float BedHour { get { uint h = (uint)Mathf.Abs(GetInstanceID() * 2654435761); bool owl = p.drunk || h % 4 == 0; return owl ? 25f + (h % 60) / 60f : 22f + (h % 90) / 60f; } }
        float WakeHour { get { uint h = (uint)Mathf.Abs(GetInstanceID() * 40503); return 6.5f + (h % 150) / 60f; } }
        bool Bedtime { get { float hr = GameClock.Hour; float bed = BedHour, wake = WakeHour; return bed >= 24f ? (hr >= bed - 24f && hr < wake) : (hr >= bed || hr < wake); } }
        static readonly List<Resident> asleep = new List<Resident>();
        /// <summary>Indoors for the night: off the street until their wake-up time.</summary>
        void TurnIn()
        {
            doing = "bed"; asleep.Add(this); if (ag.enabled) ag.ResetPath();
            gameObject.SetActive(false);
        }
        /// <summary>Called by the clock: anyone whose morning it is comes back out of their front door.</summary>
        public static void WakeAny()
        {
            for (int i = asleep.Count - 1; i >= 0; i--)
            {
                var r = asleep[i]; if (r == null) { asleep.RemoveAt(i); continue; }
                if (r.Bedtime) continue;
                asleep.RemoveAt(i); r.gameObject.SetActive(true); r.doing = ""; r.wait = Random.Range(1f, 6f);
                if (r.HomePoint(out var door) && r.ag != null && r.ag.enabled) r.ag.Warp(door);
            }
        }

        void Next()
        {
            float r = Random.value;
            Vector3 dest; bool ok;
            if (doing == "bed" || (doing == "home" && Bedtime)) { TurnIn(); return; }
            if (Bedtime) { doing = "home"; ok = HomePoint(out dest); wait = 0.5f; if (ok) ag.SetDestination(dest); else TurnIn(); return; } // time to go in
            if (r < 0.42f) { doing = "home"; ok = HomePoint(out dest); wait = Random.Range(6f, 18f); }
            else if (r < 0.62f) { doing = "garden"; ok = GardenPoint(home, out dest); wait = Random.Range(8f, 20f); }
            else if (r < 0.8f) { doing = "lane"; ok = Nav.Snap(new Vector3(Random.Range(-38f, 38f), 0, Estate.LaneZ + Random.Range(-1.2f, 1.2f)), out dest); wait = Random.Range(3f, 9f); }
            else if (r < 0.95f && Estate.Houses.Count > 1)
            {
                doing = "visit";
                var h = Estate.Houses[Random.Range(0, Estate.Houses.Count)];
                if (h == home) h = Estate.Houses[(Estate.Houses.IndexOf(h) + 1) % Estate.Houses.Count];
                Vector3 door = h.root.TransformPoint(h.FrontDoorLocal);
                ok = Nav.Snap(door + h.root.TransformDirection(h.FrontOutLocal) * 0.6f, out dest, 2f);
                wait = Random.Range(10f, 24f);
            }
            else { doing = "gate"; ok = Nav.Snap(new Vector3(Random.Range(-2f, 2f), 0, -18.5f), out dest); wait = Random.Range(4f, 10f); }
            if (ok) ag.SetDestination(dest); else wait = 1f;
        }

        bool HomePoint(out Vector3 dest)
        {
            var rooms = home.P.rooms;
            for (int tries = 0; tries < 6; tries++)
            {
                var rm = rooms[Random.Range(0, rooms.Count)];
                if (rm.lv > 1 || rm.lv < 0) continue;
                var R = rm.Main; if (R == null || R.x1 - R.x0 < 1.2f || R.d1 - R.d0 < 1.2f) continue;
                var local = new Vector3(Random.Range(R.x0 + 0.5f, R.x1 - 0.5f), rm.y + 0.05f, Random.Range(R.d0 + 0.5f, R.d1 - 0.5f));
                if (Nav.Snap(home.root.TransformPoint(local), out dest, 0.8f)) return true;
            }
            dest = transform.position; return false;
        }

        static bool GardenPoint(CotHouse h, out Vector3 dest)
        {
            Vector3 door = h.root.TransformPoint(h.FrontDoorLocal);
            Vector3 outw = h.root.TransformDirection(h.FrontOutLocal), along = Vector3.Cross(Vector3.up, outw);
            Vector3 p = door + outw * Random.Range(2.5f, 6.5f) + along * Random.Range(-5f, 5f);
            return Nav.Snap(p, out dest, 1.5f);
        }
    }
}
