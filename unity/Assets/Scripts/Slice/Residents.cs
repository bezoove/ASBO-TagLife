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
            ag.speed = Random.Range(1.0f, 1.35f); ag.angularSpeed = 300f; ag.acceleration = 3f; ag.stoppingDistance = 0.25f; ag.autoBraking = true;
            ag.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; ag.avoidancePriority = Random.Range(30, 70);
            if (Nav.Snap(transform.position, out var q, 2f)) transform.position = q;
            ag.enabled = true;
            if (ag.isOnNavMesh) ag.Warp(transform.position);
            Count++;
        }
        void OnDestroy() { all.Remove(this); }

        void Update()
        {
            if (p == null || ag == null || home == null || home.root == null) return;
            if (p.state != Person.State.Walking) { if (ag.enabled) ag.enabled = false; enabled = false; return; }
            if (!ag.isOnNavMesh) return;
            float v = ag.velocity.magnitude;
            if (v > 0.08f) p.Animate(v);
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

        public System.Collections.IEnumerator TalkFor(float t)
        {
            if (p == null) yield break;
            p.SetTalk(true); yield return new WaitForSeconds(t); if (p != null) p.SetTalk(false);
        }

        void Next()
        {
            float r = Random.value;
            Vector3 dest; bool ok;
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
