using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// The road network as lanes for traffic, built from TownPlan: two lanes per stretch of road (driving on the left),
    /// joined at each junction by turning paths (straight on, left, right; a U-turn round Mill Lane's turning head).
    /// Road ends at the edge of the map are portals: a car driving off one comes back in at another, out of sight.
    /// </summary>
    public static class TrafficNet
    {
        public class Seg
        {
            public Vector3[] pts; public float[] cum; public float len;
            public void Make(List<Vector3> p)
            {
                pts = p.ToArray(); cum = new float[pts.Length]; for (int i = 1; i < pts.Length; i++) cum[i] = cum[i - 1] + (pts[i] - pts[i - 1]).magnitude; len = cum[pts.Length - 1];
            }
            public Vector3 At(float s, out Vector3 tan)
            {
                s = Mathf.Clamp(s, 0f, len); int i = 1; while (i < pts.Length - 1 && cum[i] < s) i++;
                float seg = cum[i] - cum[i - 1]; float t = seg > 1e-4f ? (s - cum[i - 1]) / seg : 0f;
                tan = (pts[i] - pts[i - 1]).normalized; return Vector3.Lerp(pts[i - 1], pts[i], t);
            }
        }
        public class Node { public Vector3 p; public TownPlan.Junction j; public bool zebra, portal, turnHead; public readonly List<Lane> ins = new List<Lane>(), outs = new List<Lane>(); public JunctionSignal sig; }
        public class Lane : Seg { public int road; public Vector3 dir; public Node from, to; public int group; public readonly List<Move> outs = new List<Move>(); public bool Signalled => to != null && (to.j != null || to.zebra); }
        public class Move : Seg { public Lane a, b; public int turn; public Node at; } // turn: 0 straight, 1 left, 2 right, 3 round

        public static readonly List<Node> Nodes = new List<Node>();
        public static readonly List<Lane> Lanes = new List<Lane>();
        static bool built;

        public static void Build()
        {
            if (built) return; built = true;
            Nodes.Clear(); Lanes.Clear();
            Node NodeAt(Vector3 p)
            {
                foreach (var n in Nodes) if ((n.p - p).WithY(0).sqrMagnitude < 1f) return n;
                var nn = new Node { p = p.WithY(0) }; nn.j = TownPlan.JunctionAt(p); Nodes.Add(nn); return nn;
            }
            var zebra = new Vector3(TownPlan.ZebraX, 0, World.RoadZ);
            for (int ri = 0; ri < TownPlan.Roads.Length; ri++)
            {
                var r = TownPlan.Roads[ri]; Vector3 d = (r.b - r.a); float len = d.magnitude; d /= len;
                var pts = new List<(float s, Node n)> { (0f, NodeAt(r.a)), (len, NodeAt(r.b)) };
                foreach (var j in TownPlan.Junctions) { float s = Vector3.Dot(j.p - r.a, d); if (s > 0.5f && s < len - 0.5f && (r.a + d * s - j.p).WithY(0).magnitude < 0.5f) pts.Add((s, NodeAt(j.p))); }
                if (r.name == "Long Road") { var zn = NodeAt(zebra); zn.zebra = true; pts.Add((zebra.x - r.a.x, zn)); }
                pts.Sort((x, y) => x.s.CompareTo(y.s));
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    MakeLane(ri, pts[i].n, pts[i + 1].n, d);
                    MakeLane(ri, pts[i + 1].n, pts[i].n, -d);
                }
            }
            foreach (var n in Nodes)
            {
                bool edge = Mathf.Abs(n.p.x) > 245f || n.p.z > 285f;
                if (n.j == null && !n.zebra && n.ins.Count == 1 && n.outs.Count == 1) { if (edge) n.portal = true; else n.turnHead = true; }
            }
            foreach (var n in Nodes) Connect(n);
        }

        static float Trim(Node n, bool atEnd)
        {
            if (n.j != null) return n.j.kind == TownPlan.JKind.Pelican ? (atEnd ? 4.2f : 3.2f) : (atEnd ? TownPlan.BoxHalf : TownPlan.Half + 0.4f);
            if (n.zebra) return atEnd ? 3.0f : 2.4f;
            if (Mathf.Abs(n.p.x) > 245f || n.p.z > 285f) return 0f; // map edge
            return 7f; // the turning head
        }

        static void MakeLane(int road, Node from, Node to, Vector3 d)
        {
            Vector3 left = new Vector3(-d.z, 0, d.x) * TownPlan.LaneOff;
            float t0 = Trim(from, false), t1 = Trim(to, true);
            Vector3 a = from.p + d * t0 + left, b = to.p - d * t1 + left;
            if (Vector3.Dot(b - a, d) < 1f) return;
            var l = new Lane { road = road, dir = d, from = from, to = to };
            l.Make(new List<Vector3> { a, b });
            var r = TownPlan.Roads[road];
            // which set of lights: on a crossroads east-west is 0; on a T the straight-through road is 0; a pelican is all 0
            if (to.j != null)
            {
                if (to.j.kind == TownPlan.JKind.Cross) l.group = Mathf.Abs(d.x) > 0.5f ? 0 : 1;
                else if (to.j.kind == TownPlan.JKind.Tee) l.group = ((to.p - r.a).WithY(0).magnitude > 1f && (to.p - r.b).WithY(0).magnitude > 1f) ? 0 : 1;
                else l.group = 0;
            }
            from.outs.Add(l); to.ins.Add(l); Lanes.Add(l);
        }

        static void Connect(Node n)
        {
            foreach (var a in n.ins)
                foreach (var b in n.outs)
                {
                    float dot = Vector3.Dot(a.dir, b.dir); bool reverse = dot < -0.9f;
                    if (reverse && !n.turnHead && !n.portal) continue;
                    if (n.portal) continue; // portals are handled by teleporting
                    var m = new Move { a = a, b = b, at = n };
                    Vector3 p0 = a.pts[a.pts.Length - 1], p2 = b.pts[0];
                    var pts = new List<Vector3>();
                    if (reverse)
                    {
                        m.turn = 3; // round the turning head
                        Vector3 c = n.p; float R = (p0 - c).WithY(0).magnitude;
                        float a0 = Mathf.Atan2(p0.z - c.z, p0.x - c.x), a1 = Mathf.Atan2(p2.z - c.z, p2.x - c.x);
                        float da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad; if (da > 0) da -= Mathf.PI * 2f; // round to the left (clockwise from above in UK? keep it consistent)
                        for (int i = 0; i <= 16; i++) { float an = a0 + da * i / 16f; pts.Add(c + new Vector3(Mathf.Cos(an), 0, Mathf.Sin(an)) * Mathf.Max(R, 5.5f)); }
                    }
                    else if (dot > 0.9f) { m.turn = 0; pts.Add(p0); pts.Add(p2); }
                    else
                    {
                        m.turn = Vector3.Cross(a.dir, b.dir).y < 0f ? 1 : 2;
                        // the corner of the two lane lines as the curve's control point
                        Vector3 ctl = Intersect(p0, a.dir, p2, b.dir);
                        for (int i = 0; i <= 10; i++) { float t = i / 10f; pts.Add((1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * ctl + t * t * p2); }
                    }
                    m.Make(pts);
                    a.outs.Add(m);
                }
        }

        static Vector3 Intersect(Vector3 p, Vector3 d, Vector3 q, Vector3 e)
        {
            float den = d.x * e.z - d.z * e.x; if (Mathf.Abs(den) < 1e-4f) return (p + q) * 0.5f;
            float t = ((q.x - p.x) * e.z - (q.z - p.z) * e.x) / den; return p + d * t;
        }

        /// <summary>Lanes cars can come in on from off the map (the far ends of the portal roads).</summary>
        public static List<Lane> Entries()
        {
            var l = new List<Lane>(); foreach (var n in Nodes) if (n.portal) l.AddRange(n.outs); return l;
        }
    }

    /// <summary>
    /// A car driving the town's roads: follows its lane, turns at junctions (picking a way at random), keeps its
    /// distance, stops at red (most of them: a few chancers go through on amber or just after it turns red), waits to
    /// turn right across oncoming traffic, stops for anyone on a zebra or in the road, and becomes a physics wreck if
    /// rammed hard. Drives on the left.
    /// </summary>
    public class TrafficCar : MonoBehaviour, IVehicle
    {
        public float cruise = 12f;
        public bool testIgnorePeople; // tests: drive straight into whoever's in the road
        float stoppedUntil;
        /// <summary>Just hit someone: emergency stop, and sit there a while.</summary>
        public void Struck() { stoppedUntil = Time.time + 9f; testIgnorePeople = false; }
        float speed; Rigidbody rb; Transform[] wheels; float wheelSpin; bool wrecked; float horn;
        AudioSource eng; float L; bool chancer; float think, gap = 999f, waitT;
        TrafficNet.Seg seg; float s; TrafficNet.Lane lane; TrafficNet.Move move, nextMove;
        public float Speed => wrecked ? (rb != null ? rb.linearVelocity.magnitude : 0f) : speed;
        public Vector3 Velocity => wrecked ? rb.linearVelocity : transform.forward * speed;
        public static readonly List<TrafficCar> All = new List<TrafficCar>();
        static AudioClip engClip; static bool realEngine; float pitchBase = 1f;
        const float Y = TownPlan.RoadTop;

        public static TrafficCar Spawn(Transform parent, TrafficNet.Lane l, float at, System.Random r)
        {
            var g = new GameObject("TrafficCar"); g.transform.SetParent(parent, false);
            var t = g.AddComponent<TrafficCar>();
            var spec = CarBody.Random(r);
            t.L = spec.L;
            t.wheels = CarBody.Build(g.transform, spec, spec.van ? CarBody.VanPaint(r) : CarBody.RandomPaint(r), true, r.Next());
            Driver.Seat(g.transform, spec.m, r.Next()); // someone at the wheel
            Dents.Add(g, g.transform, spec.van ? 2000f : 1200f);
            var ob = g.AddComponent<UnityEngine.AI.NavMeshObstacle>(); ob.shape = UnityEngine.AI.NavMeshObstacleShape.Box; ob.carving = false; // walkers steer round it
            ob.center = new Vector3(0f, 0.8f, 0f); ob.size = new Vector3(spec.m.W + 0.3f, 1.6f, spec.m.L + 0.4f);
            g.layer = Person.VehicleLayer; // bodies on the road pass through, not crushed
            var bc = g.AddComponent<BoxCollider>();
            float h = spec.van ? 2.0f : 1.4f;
            bc.center = new Vector3(0, 0.3f + h / 2f, 0); bc.size = new Vector3(1.7f, h, spec.L);
            t.rb = g.AddComponent<Rigidbody>(); t.rb.isKinematic = true; t.rb.mass = spec.van ? 2000f : 1200f; t.rb.interpolation = RigidbodyInterpolation.Interpolate;
            t.cruise = (10.5f + (float)r.NextDouble() * 3.5f) * (r.NextDouble() < 0.12 ? 1.18f : 1f); // a few in a hurry
            t.chancer = r.NextDouble() < 0.07;
            if (engClip == null) { engClip = SfxBank.Pick("engine"); realEngine = engClip != null; if (engClip == null) engClip = AudioSynth.Engine(); }
            t.pitchBase = 0.9f + (float)r.NextDouble() * 0.2f;
            t.eng = g.AddComponent<AudioSource>(); t.eng.clip = engClip; t.eng.loop = true; t.eng.spatialBlend = 1f; t.eng.minDistance = 4f; t.eng.maxDistance = 55f; t.eng.volume = 0.18f;
            t.eng.time = (float)r.NextDouble() * engClip.length; t.eng.Play();
            t.EnterLane(l, at); t.speed = t.cruise * 0.6f;
            var p = l.At(at, out var tan); g.transform.SetPositionAndRotation(p.WithY(Y), Quaternion.LookRotation(tan));
            t.think = (float)r.NextDouble() * 0.1f;
            All.Add(t);
            return t;
        }
        void OnDestroy() { All.Remove(this); }

        void EnterLane(TrafficNet.Lane l, float at)
        {
            lane = l; move = null; seg = l; s = at; waitT = 0f;
            // pick where we're going at the end of it: mostly straight on
            nextMove = null;
            if (l.outs.Count > 0)
            {
                float tot = 0f; foreach (var m in l.outs) tot += W(m);
                float pick = Random.value * tot;
                foreach (var m in l.outs) { pick -= W(m); if (pick <= 0f) { nextMove = m; break; } }
                if (nextMove == null) nextMove = l.outs[0];
            }
        }
        static float W(TrafficNet.Move m) => m.turn == 0 ? 0.55f : m.turn == 1 ? 0.25f : m.turn == 2 ? 0.2f : 1f;

        void Advance(float ds)
        {
            s += ds;
            int guard = 0;
            while (s > seg.len && guard++ < 4)
            {
                s -= seg.len;
                if (move != null) { var to = move.b; EnterLane(to, s); s = Mathf.Min(s, to.len); continue; }
                if (lane.to.portal) { Reappear(); return; }
                if (nextMove == null) { s = seg.len; speed = 0f; return; }
                move = nextMove; seg = move;
            }
        }

        /// <summary>Off the edge of the map: back in at another road end, somewhere out of sight.</summary>
        void Reappear()
        {
            var entries = TrafficNet.Entries(); var cam = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            for (int tries = 0; tries < 8 && entries.Count > 0; tries++)
            {
                var e = entries[Random.Range(0, entries.Count)];
                Vector3 p = e.pts[0];
                if ((p - cam).WithY(0).magnitude < 90f) continue;
                bool busy = false; foreach (var o in All) if (o != this && (o.transform.position - p).sqrMagnitude < 144f) busy = true;
                if (busy) continue;
                EnterLane(e, 0f); speed = cruise * 0.8f;
                transform.position = p.WithY(Y); rb.position = p.WithY(Y); return;
            }
            // nowhere free: turn round at the edge
            foreach (var l in lane.to.outs) { EnterLane(l, 0f); var q = l.pts[0]; transform.position = q.WithY(Y); rb.position = q.WithY(Y); return; }
        }

        void FixedUpdate()
        {
            if (wrecked || seg == null) return;
            float dt = Time.fixedDeltaTime;
            think -= dt; if (think <= 0f) { think = 0.1f; gap = LookAhead(); }
            float target = cruise * (move != null && move.turn != 0 ? 0.55f : 1f);
            if (testIgnorePeople) { gap = 999f; }
            if (Time.time < stoppedUntil) target = 0f;
            float stopDist = gap - 2.2f;
            if (stopDist < 30f) target = Mathf.Min(target, Mathf.Max(0f, Mathf.Sqrt(Mathf.Max(0f, stopDist) * 2f * 4.5f)));
            float acc = target > speed ? 2.3f : Time.time < stoppedUntil ? 9.5f : 7.5f;
            speed = Mathf.MoveTowards(speed, target, acc * dt);
            if (speed < 0.3f) waitT += dt; else waitT = 0f;
            if (stopDist < 3f && speed < 0.5f && waitT > 4f && BlockedByPlayer) { horn -= dt; if (horn < 0f) { horn = 5f + Random.value * 6f; if (GameAudio.I != null) GameAudio.I.Play3D(AudioSynth.Thud(), transform.position, 0.0f); } }
            Advance(speed * dt);
            var p = seg.At(s, out var tan);
            rb.MovePosition(p.WithY(Y));
            if (tan.sqrMagnitude > 0.01f) rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(tan), 1f - Mathf.Exp(-12f * dt)));
        }

        bool blockedByPlayer; bool BlockedByPlayer => blockedByPlayer;

        /// <summary>Distance to whatever we have to stop behind: a car, a person, the player's car, the ambulance, a red light, a zebra in use, oncoming traffic when turning right.</summary>
        float LookAhead()
        {
            float best = 999f; blockedByPlayer = false;
            Vector3 me = transform.position, fwd = transform.forward; float front = L * 0.5f;
            void Consider(Vector3 p, float halfLen, float lateral, bool player)
            {
                Vector3 v = (p - me).WithY(0); float ahead = Vector3.Dot(v, fwd); if (ahead <= 0f || ahead > 40f) return;
                float side = (v - fwd * ahead).magnitude; if (side > lateral) return;
                float d = ahead - front - halfLen; if (d < best) { best = Mathf.Max(0f, d); blockedByPlayer = player; }
            }
            foreach (var o in All) { if (o == this || !o.isActiveAndEnabled) continue; Consider(o.transform.position, o.L * 0.5f, 1.5f, false); }
            foreach (var amb in Ambulance.All) if (amb != null) Consider(amb.transform.position, amb.HalfLength, 1.9f, false);
            if (!testIgnorePeople) foreach (var pp in Person.All) { if (pp == null) continue; Consider(pp.BodyPos, 0.3f, 1.4f, pp.isPlayer); }
            // the player's car: a wide berth when someone's driving it, but one parked at the kerb is passed (it isn't in the lane)
            if (PlayerCar.I != null) Consider(PlayerCar.I.transform.position, 2.2f, PlayerCar.I.driven ? 1.8f : 1.3f, true);
            // the end of our lane: lights, the zebra, turning right across the traffic
            if (move == null && lane != null && lane.to != null)
            {
                float toLine = lane.len - s;
                if (toLine < 45f)
                {
                    var n = lane.to;
                    if (n.zebra && ZebraBusy()) best = Mathf.Min(best, toLine + 1.5f);
                    if (n.j != null)
                    {
                        if (n.sig == null) n.sig = JunctionSignal.At(n.p);
                        if (n.sig != null && n.sig.MustStop(lane.group, toLine, speed, chancer)) best = Mathf.Min(best, toLine + 2.0f);
                        else if (nextMove != null && nextMove.turn == 2 && !OncomingClear(n)) best = Mathf.Min(best, toLine + 2.0f);
                    }
                }
            }
            return best;
        }

        /// <summary>Turning right: is anything coming the other way (or already in the junction going straight)?</summary>
        bool OncomingClear(TrafficNet.Node n)
        {
            foreach (var o in All)
            {
                if (o == this || o.wrecked || o.seg == null) continue;
                if (o.lane != null && o.move == null && o.lane.to == n && Vector3.Dot(o.lane.dir, lane.dir) < -0.9f && o.lane.len - o.s < 35f && o.speed > 0.5f) return false;
                if (o.move != null && o.move.at == n && o.move.turn != 2 && Vector3.Dot(o.move.a.dir, lane.dir) < -0.9f) return false;
            }
            return true;
        }

        static bool ZebraBusy()
        {
            foreach (var p in Person.All) { if (p == null || p.state != Person.State.Walking) continue; var q = p.BodyPos; if (q.x > TownPlan.ZebraX - 1.8f && q.x < TownPlan.ZebraX + 1.8f && Mathf.Abs(q.z - World.RoadZ) < 5.2f) return true; }
            return false;
        }

        void Update()
        {
            wheelSpin += Speed * Time.deltaTime / 0.31f * Mathf.Rad2Deg;
            if (!wrecked && wheels != null) foreach (var w in wheels) w.localRotation = Quaternion.Euler(wheelSpin, 0, 90f);
            if (eng != null) { eng.pitch = realEngine ? pitchBase * (0.95f + Speed * 0.035f) : 0.55f + Speed * 0.05f; }
        }

        void OnCollisionEnter(Collision c)
        {
            if (wrecked) return;
            float rel = c.relativeVelocity.magnitude;
            if (rel < 4f) return;
            Wreck(c.relativeVelocity);
        }

        public void Wreck(Vector3 impulseDir)
        {
            wrecked = true;
            rb.isKinematic = false; rb.linearVelocity = transform.forward * speed; rb.angularDamping = 0.6f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (GameAudio.I != null) GameAudio.I.CarCrash(transform.position, impulseDir.magnitude);
            if (eng != null) eng.volume = 0.05f;
        }
    }

    public static class Traffic
    {
        public static void Spawn(Transform root)
        {
            TrafficNet.Build();
            var r = new System.Random(31);
            var g = new GameObject("Traffic").transform; g.SetParent(root, false);
            int want = Application.isMobilePlatform ? 16 : 26;
            float total = 0f; foreach (var l in TrafficNet.Lanes) total += l.len;
            var player = Object.FindFirstObjectByType<PlayerCtl>(); Vector3 pp = player != null ? player.transform.position : new Vector3(-17f, 0, -24f);
            int made = 0;
            for (int tries = 0; tries < want * 12 && made < want; tries++)
            {
                float pick = (float)r.NextDouble() * total; TrafficNet.Lane lane = TrafficNet.Lanes[0];
                foreach (var l in TrafficNet.Lanes) { pick -= l.len; if (pick <= 0f) { lane = l; break; } }
                if (lane.len < 12f) continue;
                float at = 4f + (float)r.NextDouble() * (lane.len - 8f);
                Vector3 p = lane.At(at, out _);
                if ((p - pp).WithY(0).magnitude < 25f) continue;
                bool close = false; foreach (var o in TrafficCar.All) if ((o.transform.position - p).sqrMagnitude < 15f * 15f) close = true;
                if (close) continue;
                TrafficCar.Spawn(g, lane, at, r); made++;
            }
        }
    }
}
