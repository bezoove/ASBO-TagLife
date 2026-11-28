using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// The inside of a tower block, built as you get near and taken down when you leave, so a dozen sixteen-storey blocks
    /// cost nothing until you're in one. Each floor: a dog-leg concrete stair up a spine wall, two lifts, a lino corridor
    /// across the middle with the rubbish chute, and four flats off it (two either side of the core at the front, two at the
    /// back) each built by the home builder for whoever lives there: their own plan, era, mess and things worth nicking.
    /// The ground floor is the lobby (post boxes, notices) with the bin store, the caretaker's office, the bike store and
    /// the drying room off the corridor. The lifts work (a floor picker) unless they're out of order, which they often are.
    /// Frame: x along the entrance front (0..W), the front at z = 0 facing -z, depth +z (0..D), y up from the ground.
    /// </summary>
    public class TowerInterior : MonoBehaviour
    {
        [System.Serializable] public struct WinRec { public char side; public float a0, a1, y0, y1; }

        public string block; public float W, D, GF, FH, coreX, lobbyZ = 1.3f, recessX0, recessX1, recessH = 2.6f, insetL, insetR;
        public int storeys, seed;
        public List<WinRec> wins = new List<WinRec>();
        public List<Collider> baked = new List<Collider>();
        public Home core; public bool liftBroken;
        public static readonly List<TowerInterior> All = new List<TowerInterior>();
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        // ================================================================== the plan (shared by baking and building)
        [System.NonSerialized] bool calc;
        float cx0, cx1, sx0, sx1, zH0, zH1, zL, zm, zc1, xL, xR, splitX; int n0, n1;
        const float Tread = 0.27f, FloorTop = 0.02f;
        public float Yf(int f) => f <= 0 ? 0f : GF + (f - 1) * FH;
        public float Hf(int f) => f <= 0 ? GF : FH;
        public float Top => Yf(storeys);
        public int LevelAt(float y) => y < GF ? 0 : Mathf.Clamp(1 + Mathf.FloorToInt((y - GF) / FH), 0, storeys - 1);
        float LiftX => cx0 + 1.55f;
        float StairEnd(int f) => zL - ((f == 0 ? n0 : n1) - 1) * Tread;   // where the flights from this floor meet the half landing

        void Calc()
        {
            calc = true;
            cx0 = coreX - 3.2f; cx1 = coreX + 3.2f; sx1 = cx1; sx0 = cx1 - 2.3f;
            xL = insetL; xR = W - insetR;
            n0 = Mathf.CeilToInt(GF * 0.5f / 0.18f); n1 = Mathf.CeilToInt(FH * 0.5f / 0.18f);
            zH0 = lobbyZ + 0.02f; zH1 = zH0 + 1.15f; zL = zH1 + (Mathf.Max(n0, n1) - 1) * Tread;
            zm = Mathf.Max(zL + 1.3f, D * 0.5f - 0.75f); zc1 = zm + 1.5f;
            // the party wall between the back flats: in a gap between the back windows near the middle if there is one
            splitX = (xL + xR) * 0.5f; float best = 99f;
            float y1 = Yf(1), y2 = Yf(2);
            var bw = new List<WinRec>(); foreach (var w in wins) if (w.side == 'B' && w.y0 >= y1 - 0.01f && w.y0 < y2 - 0.01f) bw.Add(w);
            bw.Sort((a, b) => a.a0.CompareTo(b.a0));
            for (int i = 0; i + 1 < bw.Count; i++)
            {
                float g = (bw[i].a1 + bw[i + 1].a0) * 0.5f; if (g < xL + 3.4f || g > xR - 3.4f) continue;
                float d = Mathf.Abs(g - (xL + xR) * 0.5f); if (d < best && d < 2.2f) { best = d; splitX = g; }
            }
        }

        // ================================================================== baking (editor, with the building)
        public static TowerInterior Bake(Transform building, Vector3 pos, float yaw, string block, float W, float D, float GF, float FH, int storeys,
            float coreX, float lobbyZ, float rx0, float rx1, float rh, float insetL, float insetR, int seed, List<WinRec> wins, List<Collider> baked)
        {
            var g = new GameObject("Inside"); g.transform.SetParent(building, false); g.transform.localPosition = pos; g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var ti = g.AddComponent<TowerInterior>();
            ti.block = block; ti.W = W; ti.D = D; ti.GF = GF; ti.FH = FH; ti.storeys = storeys; ti.coreX = coreX; ti.lobbyZ = lobbyZ; ti.recessX0 = rx0; ti.recessX1 = rx1; ti.recessH = rh;
            ti.insetL = insetL; ti.insetR = insetR; ti.seed = seed; ti.wins = wins; ti.baked = baked; ti.Calc();
            var rng = new System.Random(seed * 7 + 3);
            ti.liftBroken = rng.NextDouble() < 0.22;
            // the common parts: one home for the entrance door, the lifts, the post boxes and the chutes
            var h = g.AddComponent<Home>(); ti.core = h;
            var hh = Household.Make(50000 + seed, rng); hh.kind = Household.Kind.Couple; hh.wealth = 0.3f; hh.stuff = 0.6f; hh.family = block;
            h.H = hh; h.label = block; h.variant = "tower";
            var d = PropDoor.Make(g.transform, "door_steel", new Vector3(coreX, FloorTop, lobbyZ), 0f, "front");
            if (d != null) h.doors.Add(new Home.DoorRec { door = d, tag = "front", lv = 0, tier = rng.NextDouble() < 0.45 ? -1 : 0 });
            // the glazed screen either side of the entrance door, solid
            var gl = new GameObject("Glass"); gl.transform.SetParent(g.transform, false);
            void Gc(float a, float b, float y0, float y1) { if (b - a < 0.02f) return; var c = gl.AddComponent<BoxCollider>(); c.center = new Vector3((a + b) * 0.5f, (y0 + y1) * 0.5f, lobbyZ - 0.01f); c.size = new Vector3(b - a, y1 - y0, 0.1f); }
            Gc(rx0, coreX - 0.5f, 0f, rh); Gc(coreX + 0.5f, rx1, 0f, rh); Gc(coreX - 0.5f, coreX + 0.5f, 2.15f, rh);
            // spots: the lifts on every floor, the post boxes and the notices in the lobby, the chute on every floor
            for (int f = 0; f < storeys; f++)
            {
                float y = ti.Yf(f) + FloorTop;
                Vector3 at = f == 0 ? new Vector3(ti.LiftX, y, ti.zm - 1.7f - 0.35f) : new Vector3(ti.LiftX, y, ti.zm + 0.35f);
                h.spots.Add(new Home.SpotRec { kind = "lift", label = ti.liftBroken ? "Press the lift button" : "Call the lift", tag = f.ToString(), pos = at, yaw = f == 0 ? 0f : 180f, room = 0, lv = 0 });
                if (f > 0) h.spots.Add(new Home.SpotRec { kind = "chute", label = "Open the rubbish chute", tag = "chute", pos = new Vector3(ti.cx0 + 3.6f, y, ti.zm - 1.7f + 0.35f), yaw = 180f, room = 0, lv = 0 });
            }
            h.spots.Add(new Home.SpotRec { kind = "rummage", label = "Fish about in the post boxes", tag = "post", pos = new Vector3(ti.cx0 + 0.45f, FloorTop, lobbyZ + 1.2f), yaw = -90f, room = 0, lv = 0 });
            h.spots.Add(new Home.SpotRec { kind = "read", label = "Read the notice board", tag = "notice", pos = new Vector3(ti.cx0 + 0.45f, FloorTop, lobbyZ + 2.6f), yaw = -90f, room = 0, lv = 0 });
            h.rooms.Add(new Home.RoomRec { type = "hall", use = "lobby", lv = 0, centre = new Vector3((ti.cx0 + ti.sx0) * 0.5f, 0f, (lobbyZ + ti.zm) * 0.5f), u0 = ti.cx0, u1 = ti.sx0, d0 = lobbyZ, d1 = ti.zm });
            h.zoneRects.Add(Rect.MinMaxRect(ti.cx0, lobbyZ, ti.sx1, ti.zc1));
            h.centre = new Vector3(coreX, 1.5f, (lobbyZ + ti.zm) * 0.5f); h.radius = ti.Top + D; h.midZ = (lobbyZ + ti.zc1) * 0.5f; h.halfD = (ti.zc1 - lobbyZ) * 0.5f + 0.3f;
            return ti;
        }

        // ================================================================== streaming
        class Job { public float xa, xb, za, zb; public bool front; public char ext; public bool bareL, bareR; public int idx; public string kind; }
        class Floor { public int f; public GameObject go; public List<Job> jobs = new List<Job>(); public int next; public bool done; }
        readonly Dictionary<int, Floor> floors = new Dictionary<int, Floor>();
        readonly HashSet<int> want = new HashSet<int>();
        Transform player; float nextTick; bool active, inside; [System.NonSerialized] public bool pinned; int level; GameObject shellGo; Light hallLight;

        void Update()
        {
            if (player == null) { var pc = FindFirstObjectByType<PlayerCtl>(); if (pc == null) return; player = pc.transform; }
            if (!calc) Calc();
            if (Time.time >= nextTick) { nextTick = Time.time + 0.2f; Tick(); }
            if (active) Step();
            if (fadeDir != 0) { fade = Mathf.Clamp01(fade + fadeDir * Time.deltaTime * 2.5f); if (fade <= 0f && fadeDir < 0) fadeDir = 0; }
        }

        void Tick()
        {
            Vector3 lp = transform.InverseTransformPoint(player.position);
            float dx = Mathf.Max(0f, Mathf.Max(-lp.x, lp.x - W)), dz = Mathf.Max(0f, Mathf.Max(-lp.z, lp.z - D)); float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (!active && dist < 30f) Activate();
            else if (active && dist > 45f && !pinned) { Deactivate(); return; }
            if (!active || pinned) return;
            inside = lp.x > 0f && lp.x < W && lp.z > 0f && lp.z < D;
            level = inside ? LevelAt(lp.y + 0.3f) : 0;
            want.Clear(); want.Add(level); if (inside) { want.Add(level - 1); want.Add(level + 1); }
            want.RemoveWhere(f => f < 0 || f >= storeys);
            var drop = new List<int>(); foreach (var kv in floors) if (Mathf.Abs(kv.Key - level) > 2 || (!inside && kv.Key > 1)) drop.Add(kv.Key);
            foreach (int f in drop) { if (floors[f].go != null) Destroy(floors[f].go); floors.Remove(f); }
            if (drop.Count > 0 || Time.frameCount % 50 == 0) GameAudio.Zones.RemoveAll(z => z.t == null);
            // one plain light in the corridor of the floor you're on
            if (hallLight == null) { var g = new GameObject("TowerHallLight"); g.transform.SetParent(transform, false); hallLight = g.AddComponent<Light>(); hallLight.type = LightType.Point; hallLight.shadows = LightShadows.None; hallLight.range = 10f; hallLight.color = new Color(0.9f, 0.95f, 1f); }
            hallLight.enabled = inside;
            hallLight.transform.localPosition = new Vector3((xL + xR) * 0.5f, Yf(level) + Hf(level) - 0.6f, (zm + zc1) * 0.5f); hallLight.intensity = 1.3f;
        }

        void Step()
        {
            int best = -1, bd = 99;
            foreach (int f in want) { if (floors.TryGetValue(f, out var fl) && fl.done) continue; int d = Mathf.Abs(f - level); if (d < bd) { bd = d; best = f; } }
            if (best >= 0) BuildStep(best);
        }

        void BuildStep(int f)
        {
            if (!floors.TryGetValue(f, out var fl)) { fl = NewFloor(f); floors[f] = fl; }
            else if (fl.next < fl.jobs.Count) { var j = fl.jobs[fl.next++]; try { if (j.kind == "flat") BuildFlat(fl, j); else BuildStore(fl, j); } catch (System.Exception e) { Debug.LogException(e); } }
            if (fl.next >= fl.jobs.Count) fl.done = true;
        }
        /// <summary>Have a floor ready right now (behind the lift's fade).</summary>
        public void BuildNow(int f) { int guard = 0; while (guard++ < 12 && !(floors.TryGetValue(f, out var fl) && fl.done)) BuildStep(f); }

        void Activate()
        {
            active = true; if (!TownMats.Load()) return; CotMats.Init();
            foreach (var c in baked) if (c != null) c.enabled = false;
            shellGo = new GameObject("Shell"); shellGo.transform.SetParent(transform, false);
            float top = Top;
            void C(float x0, float x1, float y0, float y1, float z0, float z1) { var b = shellGo.AddComponent<BoxCollider>(); b.center = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f); b.size = new Vector3(x1 - x0, y1 - y0, z1 - z0); }
            C(insetL - 0.25f, insetL + 0.02f, -0.3f, top, 0f, D); C(W - insetR - 0.02f, W - insetR + 0.25f, -0.3f, top, 0f, D);
            C(0f, W, -0.3f, top, D - 0.02f, D + 0.25f);
            C(0f, recessX0, -0.3f, top, -0.25f, 0.02f); C(recessX1, W, -0.3f, top, -0.25f, 0.02f); C(recessX0, recessX1, recessH, top, -0.25f, 0.02f);
            C(recessX0 - 0.15f, recessX0, -0.3f, recessH, 0f, lobbyZ); C(recessX1, recessX1 + 0.15f, -0.3f, recessH, 0f, lobbyZ);
            C(0f, W, top, top + 0.4f, 0f, D);
            C(recessX0, recessX1, -0.3f, FloorTop, -1.5f, lobbyZ);      // the step in the entrance recess
        }
        void Deactivate()
        {
            active = false;
            foreach (var kv in floors) if (kv.Value.go != null) Destroy(kv.Value.go);
            floors.Clear();
            if (shellGo != null) Destroy(shellGo);
            foreach (var c in baked) if (c != null) c.enabled = true;
            if (hallLight != null) hallLight.enabled = false;
        }

        // ================================================================== a floor
        Floor NewFloor(int f)
        {
            var fl = new Floor { f = f, go = new GameObject("Floor" + f) };
            fl.go.transform.SetParent(transform, false); fl.go.transform.localPosition = new Vector3(0f, Yf(f), 0f);
            // what goes round the corridor
            string[] gk = { "bins", "caretaker", "bikes", "drying" };
            var spans = new (float xa, float xb, float za, float zb, bool front, char ext, bool bl, bool br)[]
            {
                (xL, cx0, 0f, zm, true, 'L', false, true), (cx1, xR, 0f, zm, true, 'R', false, false),
                (xL, splitX, zc1, D, false, 'R', true, false), (splitX, xR, zc1, D, false, 'L', false, true),
            };
            for (int i = 0; i < 4; i++)
            {
                var s = spans[i]; float wd = s.xb - s.xa;
                string kind = wd < 2.0f ? "duct" : wd < 3.6f ? "chute" : f == 0 ? gk[i] : "flat";
                fl.jobs.Add(new Job { xa = s.xa, xb = s.xb, za = s.za, zb = s.zb, front = s.front, ext = s.ext, bareL = s.bl, bareR = s.br, idx = i, kind = kind });
            }
            try { CoreFloor(fl); } catch (System.Exception e) { Debug.LogException(e); }
            return fl;
        }

        // ------------------------------------------------------------------ the core: stair, lifts, corridor (and the lobby)
        void CoreFloor(Floor fl)
        {
            int f = fl.f; float H = Hf(f), ft = FloorTop, ch = H - 0.25f;
            var k = new MeshKit(); var dk = new MeshKit(); var cols = new List<(Vector3 c, Vector3 s)>();
            Material wallM = TownMats.lobbyWall, floorM = f == 0 ? TownMats.lobbyFloor : TownMats.lino, conc = TownMats.panel, steel = TownMats.steel, ceilM = HomeBuilder.Flat(new Color(0.86f, 0.86f, 0.84f), 0.1f);
            var glow = CouncilEstate.M.glow; var black = CouncilEstate.M.black;
            void Bx(MeshKit m, Material mat, float x0, float x1, float y0, float y1, float z0, float z1, bool col = false)
            {
                if (x1 - x0 < 0.001f || y1 - y0 < 0.001f || z1 - z0 < 0.001f) return;
                if (m != null && mat != null) m.Box(mat, new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));
                if (col) cols.Add((new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, y1 - y0, z1 - z0)));
            }
            void Plate(float x0, float x1, float z0, float z1, bool ceiling = true)
            {
                if (x1 - x0 < 0.01f || z1 - z0 < 0.01f) return;
                k.Quad(floorM, new Vector3(x0, ft, z0), new Vector3(x1, ft, z0), new Vector3(x1, ft, z1), new Vector3(x0, ft, z1), Vector3.up);
                Bx(null, null, x0, x1, ft - 0.26f, ft, z0, z1, true);
                if (ceiling) k.Quad(ceilM, new Vector3(x0, ch, z0), new Vector3(x1, ch, z0), new Vector3(x1, ch, z1), new Vector3(x0, ch, z1), Vector3.down);
            }
            var rng = new System.Random(seed * 131 + f * 17);
            // floors
            Plate(xL, xR, zm, zc1);
            Plate(sx0, sx1, zL, zm);
            Plate(cx0 + 3.1f, sx0, zm - 1.7f, zm);
            if (f == 0) { Plate(cx0, sx0, lobbyZ, zm - 1.7f); Plate(sx0, sx1, zH0, zL, false); }
            // the stair: its west wall, the dead block in front, the spine between the flights
            Bx(k, wallM, sx0 - 0.12f, sx0, 0f, H, zH0, zL, true);
            Bx(k, wallM, sx0, sx1, 0f, H, f == 0 ? lobbyZ : 0.12f, zH0, true);
            if (f == 0)
            {
                // the lobby's front wall either side of the entrance recess, and over the glazed screen
                Bx(k, wallM, cx0, recessX0, 0f, H, 0.12f, lobbyZ, true);
                Bx(k, wallM, recessX1, sx1, 0f, H, 0.12f, lobbyZ, true);
                Bx(k, wallM, recessX0, recessX1, recessH, H, lobbyZ - 0.02f, lobbyZ + 0.1f, true);
            }
            float zHe = StairEnd(f);
            if (f < storeys - 1) Bx(k, wallM, sx0 + 1.1f, sx0 + 1.2f, 0f, H, zHe, zL, true);
            // lifts and the riser beside them
            if (f == 0)
            {
                Bx(k, wallM, cx0, cx0 + 3.1f, 0f, H, zm - 1.7f, zm, true);
                LiftDoors(dk, zm - 1.7f, -1f, H);
            }
            else
            {
                Bx(k, wallM, cx0, cx0 + 3.1f, 0f, H, zm - 1.7f, zm, true);
                Bx(k, wallM, cx0, sx0 - 0.12f, 0f, H, 0.12f, zm - 1.7f, true);
                LiftDoors(dk, zm, 1f, H);
                // the rubbish chute hopper on the riser's face in the nook
                Bx(dk, steel, cx0 + 3.3f, cx0 + 3.9f, ft + 0.75f, ft + 1.25f, zm - 1.7f, zm - 1.6f, true);
                Bx(dk, black, cx0 + 3.35f, cx0 + 3.85f, ft + 0.85f, ft + 1.15f, zm - 1.6f, zm - 1.59f);
            }
            // corridor ends (with a window if the outside has one there)
            EndWall(k, dk, cols, f, xL, 1f, H, wallM);
            EndWall(k, dk, cols, f, xR, -1f, H, wallM);
            // lights: strips along the corridor, a bulkhead in the stairwell
            int nl = Mathf.Max(2, Mathf.RoundToInt((xR - xL) / 4.5f));
            for (int i = 0; i < nl; i++) { float x = Mathf.Lerp(xL + 1.5f, xR - 1.5f, nl == 1 ? 0.5f : i / (float)(nl - 1)); Bx(dk, glow, x - 0.55f, x + 0.55f, ch - 0.05f, ch, (zm + zc1) * 0.5f - 0.07f, (zm + zc1) * 0.5f + 0.07f); }
            Bx(dk, glow, sx0 + 0.3f, sx0 + 0.6f, ft + H * 0.5f + 1.9f, ft + H * 0.5f + 2.1f, zH0 + 0.0f, zH0 + 0.06f);
            Bx(dk, glow, (cx0 + 3.1f + sx1) * 0.5f - 0.3f, (cx0 + 3.1f + sx1) * 0.5f + 0.3f, ch - 0.05f, ch, (zL + zm) * 0.5f - 0.1f, (zL + zm) * 0.5f + 0.1f);
            // the stairs to the floor above
            if (f < storeys - 1) Flights(k, dk, cols, f, conc, steel);
            else Bx(k, steel, sx1 - 1.1f, sx1, ft, ft + 1.05f, zL - 0.05f, zL, true);
            if (f == 0)
            {
                // boxed in under the upper flight
                Bx(k, wallM, sx0, sx0 + 1.1f, 0f, H * 0.5f - 0.2f, zH0, zL - 0.35f, true);
                Lobby(dk, rng);
            }
            // a fire extinguisher by the lifts, a radiator in the corridor, graffiti
            dk.Cyl(TownMats.redPaint, new Vector3(cx0 + 3.0f + 0.2f, ft + 0.15f, f == 0 ? zm - 1.7f - 0.15f : zm + 0.12f), 0.08f, 0.55f, 10);
            Bx(dk, HomeBuilder.Flat(new Color(0.92f, 0.92f, 0.9f), 0.5f), xL + 1.0f, xL + 2.0f, ft + 0.15f, ft + 0.7f, zc1 - 0.09f, zc1 - 0.02f);
            if (TownMats.graffiti != null)
            {
                int ng = rng.Next(4) + (f == 0 ? 2 : 0);
                for (int i = 0; i < ng; i++)
                {
                    int cell = rng.Next(16); float cu = (cell % 4) * 0.25f, cv = 0.75f - (cell / 4) * 0.25f; float w = 0.9f + (float)rng.NextDouble() * 1.2f, h = w * 0.45f, y = ft + 0.4f + (float)rng.NextDouble() * 0.7f;
                    int where = rng.Next(3);
                    if (where == 0) { float x = Mathf.Lerp(xL + 0.5f, xR - 0.5f - w, (float)rng.NextDouble()); float z = zc1 - 0.125f; Decal(dk, new Vector3(x, y, z), new Vector3(x + w, y, z), h, Vector3.back, cu, cv); }
                    else if (where == 1) { float z = Mathf.Lerp(zH0 + 0.3f, Mathf.Max(zH0 + 0.4f, zL - w - 0.2f), (float)rng.NextDouble()); float x = sx0 + 0.005f; Decal(dk, new Vector3(x, y + H * 0.4f, z + w), new Vector3(x, y + H * 0.4f, z), h, Vector3.right, cu, cv); }
                    else { float x = cx0 + 3.12f; float z = zm - 1.65f; Decal(dk, new Vector3(x, y, z), new Vector3(x, y, z + Mathf.Min(w, 1.5f)), h, Vector3.right, cu, cv); }
                }
            }
            // build
            k.Build("Core", fl.go.transform, false, ShadowCastingMode.Off);
            var dg = dk.Build("CoreDeco", fl.go.transform, false, ShadowCastingMode.Off); if (dg != null) dg.layer = Bld.DetailLayer;
            var cg = new GameObject("CoreSolids"); cg.transform.SetParent(fl.go.transform, false);
            foreach (var c in cols) { var b = cg.AddComponent<BoxCollider>(); b.center = c.c; b.size = new Vector3(Mathf.Max(0.02f, c.s.x), Mathf.Max(0.02f, c.s.y), Mathf.Max(0.02f, c.s.z)); }
            var zr = new List<CRect> { new CRect(xL, xR, zm, zc1), new CRect(sx0 - 0.1f, sx1, zH0, zm), new CRect(cx0, sx0, zm - 1.7f, zm) };
            if (f == 0) zr.Add(new CRect(cx0, sx0, lobbyZ, zm));
            GameAudio.RegisterZone(fl.go.transform, zr, H);
        }

        static void Decal(MeshKit k, Vector3 a, Vector3 b, float h, Vector3 n, float cu, float cv)
        {
            Vector3 up = Vector3.up * h, off = n * 0.008f;
            k.QuadUV(TownMats.graffiti, a + off, b + off, b + up + off, a + up + off, n, new Vector2(cu, cv), new Vector2(cu + 0.25f, cv), new Vector2(cu + 0.25f, cv + 0.25f), new Vector2(cu, cv + 0.25f));
        }

        void LiftDoors(MeshKit k, float z, float side, float H)
        {
            float ft = FloorTop, zf = side > 0 ? z : z - 0.02f, zb = side > 0 ? z + 0.02f : z;
            foreach (float lx in new[] { cx0 + 0.25f, cx0 + 1.75f })
            {
                k.Box(TownMats.steel, new Vector3(lx - 0.08f, ft, side > 0 ? z : z - 0.03f), new Vector3(lx + 1.18f, ft + 2.2f, side > 0 ? z + 0.03f : z));
                k.Box(TownMats.liftDoor, new Vector3(lx, ft, zf - side * 0.01f), new Vector3(lx + 1.1f, ft + 2.1f, zb + side * 0.01f));
                k.Box(CouncilEstate.M.black, new Vector3(lx + 0.545f, ft, side > 0 ? z + 0.03f : z - 0.035f), new Vector3(lx + 0.555f, ft + 2.1f, side > 0 ? z + 0.035f : z - 0.03f));
            }
            // call button, and an out-of-order sign taped on if they're broken
            k.Box(TownMats.hiVis, new Vector3(cx0 + 1.52f, ft + 1.05f, side > 0 ? z : z - 0.03f), new Vector3(cx0 + 1.62f, ft + 1.25f, side > 0 ? z + 0.03f : z));
            if (liftBroken) k.Box(HomeBuilder.Flat(new Color(0.95f, 0.95f, 0.9f), 0.1f), new Vector3(cx0 + 0.45f, ft + 1.3f, side > 0 ? z + 0.03f : z - 0.04f), new Vector3(cx0 + 0.95f, ft + 1.6f, side > 0 ? z + 0.04f : z - 0.03f));
        }

        /// <summary>A corridor end wall (x = at, facing dir), with the outside's window cut through if one lands there.</summary>
        void EndWall(MeshKit k, MeshKit dk, List<(Vector3 c, Vector3 s)> cols, int f, float at, float dir, float H, Material wallM)
        {
            float x0 = dir > 0 ? at : at - 0.12f, x1 = x0 + 0.12f, y0 = Yf(f);
            char side = dir > 0 ? 'L' : 'R';
            Rect hole = default; bool has = false;
            foreach (var w in wins)
            {
                if (w.side != side || w.y0 < y0 - 0.01f || w.y0 >= y0 + H - 0.01f) continue;
                if (w.a0 > zm + 0.08f && w.a1 < zc1 - 0.08f) { hole = Rect.MinMaxRect(w.a0, w.y0 - y0, w.a1, w.y1 - y0); has = true; break; }
            }
            void B(float za, float zb, float ya, float yb) { if (zb - za < 0.001f || yb - ya < 0.001f) return; k.Box(wallM, new Vector3(x0, ya, za), new Vector3(x1, yb, zb)); cols.Add((new Vector3((x0 + x1) * 0.5f, (ya + yb) * 0.5f, (za + zb) * 0.5f), new Vector3(0.12f, yb - ya, zb - za))); }
            if (!has) { B(zm, zc1, 0f, H); return; }
            B(zm, hole.xMin, 0f, H); B(hole.xMax, zc1, 0f, H); B(hole.xMin, hole.xMax, 0f, hole.yMin); B(hole.xMin, hole.xMax, hole.yMax, H);
            var fr = HomeBuilder.Flat(new Color(0.2f, 0.2f, 0.22f), 0.5f); float xm = (x0 + x1) * 0.5f;
            dk.Box(fr, new Vector3(xm - 0.03f, hole.yMin, hole.xMin), new Vector3(xm + 0.03f, hole.yMin + 0.05f, hole.xMax)); dk.Box(fr, new Vector3(xm - 0.03f, hole.yMax - 0.05f, hole.xMin), new Vector3(xm + 0.03f, hole.yMax, hole.xMax));
            dk.Box(fr, new Vector3(xm - 0.03f, hole.yMin, hole.xMin), new Vector3(xm + 0.03f, hole.yMax, hole.xMin + 0.05f)); dk.Box(fr, new Vector3(xm - 0.03f, hole.yMin, hole.xMax - 0.05f), new Vector3(xm + 0.03f, hole.yMax, hole.xMax));
            dk.Box(CotMats.glassClear, new Vector3(xm - 0.004f, hole.yMin + 0.05f, hole.xMin + 0.05f), new Vector3(xm + 0.004f, hole.yMax - 0.05f, hole.xMax - 0.05f));
            cols.Add((new Vector3(xm, (hole.yMin + hole.yMax) * 0.5f, (hole.xMin + hole.xMax) * 0.5f), new Vector3(0.08f, hole.yMax - hole.yMin, hole.xMax - hole.xMin)));
        }

        /// <summary>The dog-leg up to the next floor: a flight up strip A to the half landing at the front, a flight back up strip B to the landing above.</summary>
        void Flights(MeshKit k, MeshKit dk, List<(Vector3 c, Vector3 s)> cols, int f, Material conc, Material steel)
        {
            int n = f == 0 ? n0 : n1; float H = Hf(f), rise = H / (2 * n), ft = FloorTop, zHe = StairEnd(f);
            float a0 = sx1 - 1.1f, a1 = sx1, b0 = sx0, b1 = sx0 + 1.1f;
            var nose = HomeBuilder.Flat(new Color(0.75f, 0.62f, 0.15f), 0.4f);
            void T(float x0, float x1, float y0, float y1, float z0, float z1, bool nosingFront)
            {
                k.Box(conc, new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));
                cols.Add((new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, y1 - y0, z1 - z0)));
                float zn = nosingFront ? z0 : z1;
                dk.Box(nose, new Vector3(x0 + 0.02f, y1, nosingFront ? zn : zn - 0.05f), new Vector3(x1 - 0.02f, y1 + 0.006f, nosingFront ? zn + 0.05f : zn));
            }
            for (int i = 1; i < n; i++) T(a0, a1, Mathf.Max(0f, ft + (i - 1) * rise - 0.2f), ft + i * rise, zL - i * Tread, zL - (i - 1) * Tread, true);
            // the half landing
            float hy = ft + n * rise;
            k.Box(conc, new Vector3(sx0, hy - 0.22f, zH0), new Vector3(sx1, hy, zHe)); cols.Add((new Vector3((sx0 + sx1) * 0.5f, hy - 0.11f, (zH0 + zHe) * 0.5f), new Vector3(sx1 - sx0, 0.22f, zHe - zH0)));
            k.Quad(TownMats.lino, new Vector3(sx0, hy + 0.002f, zH0), new Vector3(sx1, hy + 0.002f, zH0), new Vector3(sx1, hy + 0.002f, zHe), new Vector3(sx0, hy + 0.002f, zHe), Vector3.up);
            for (int j = 1; j < n; j++) T(b0, b1, hy + (j - 1) * rise - 0.2f, hy + j * rise, zHe + (j - 1) * Tread, zHe + j * Tread, false);
            // handrails on the spine, either side
            for (int s = 0; s < 2; s++)
            {
                float x = s == 0 ? sx0 + 1.2f : sx0 + 1.06f, xw = 0.04f;
                for (int i = 0; i < n - 1; i++)
                {
                    float t0 = i / (float)(n - 1), t1 = (i + 1) / (float)(n - 1);
                    float za = s == 0 ? zL - t0 * (zL - zHe) : zHe + t0 * (zL - zHe), zb = s == 0 ? zL - t1 * (zL - zHe) : zHe + t1 * (zL - zHe);
                    float y = (s == 0 ? ft : hy) + (t0 + t1) * 0.5f * (H * 0.5f) + 0.9f;
                    dk.Box(steel, new Vector3(x, y - 0.025f, Mathf.Min(za, zb)), new Vector3(x + xw, y + 0.025f, Mathf.Max(za, zb)));
                }
            }
        }

        void Lobby(MeshKit dk, System.Random rng)
        {
            float ft = FloorTop, x = cx0 + 0.02f;
            // post boxes and the notice board on the west wall
            dk.Box(TownMats.steel, new Vector3(x, ft + 0.9f, lobbyZ + 0.4f), new Vector3(x + 0.3f, ft + 1.9f, lobbyZ + 2.0f));
            for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) dk.Box(CouncilEstate.M.black, new Vector3(x + 0.3f, ft + 1.0f + r * 0.23f, lobbyZ + 0.5f + c * 0.38f), new Vector3(x + 0.31f, ft + 1.05f + r * 0.23f, lobbyZ + 0.8f + c * 0.38f));
            dk.Box(TownMats.notice, new Vector3(x, ft + 1.2f, lobbyZ + 2.2f), new Vector3(x + 0.04f, ft + 2.0f, lobbyZ + 3.2f));
            // a CCTV dome, a doormat, leaflets on the floor
            dk.Sphere(CouncilEstate.M.black, new Vector3(cx0 + 0.4f, GF - 0.35f, lobbyZ + 0.4f), new Vector3(0.1f, 0.08f, 0.1f), 8, 4, true);
            dk.Box(HomeBuilder.Flat(new Color(0.15f, 0.14f, 0.13f), 0.1f), new Vector3(coreX - 0.6f, ft, lobbyZ + 0.05f), new Vector3(coreX + 0.6f, ft + 0.012f, lobbyZ + 1.0f));
            for (int i = 0; i < 4; i++) { float lx = coreX - 0.8f + (float)rng.NextDouble() * 1.6f, lz = lobbyZ + 1.1f + (float)rng.NextDouble() * 1.5f; dk.Box(HomeBuilder.Flat(Color.HSVToRGB((float)rng.NextDouble(), 0.4f, 0.9f), 0.2f), new Vector3(lx, ft, lz), new Vector3(lx + 0.15f, ft + 0.004f, lz + 0.21f)); }
            // strip lights
            dk.Box(CouncilEstate.M.glow, new Vector3((cx0 + sx0) * 0.5f - 0.6f, GF - 0.31f, (lobbyZ + zm) * 0.5f - 0.07f), new Vector3((cx0 + sx0) * 0.5f + 0.6f, GF - 0.26f, (lobbyZ + zm) * 0.5f + 0.07f));
            // the lobby's ceiling
            dk.Quad(HomeBuilder.Flat(new Color(0.86f, 0.86f, 0.84f), 0.1f), new Vector3(cx0, GF - 0.25f, lobbyZ), new Vector3(sx0, GF - 0.25f, lobbyZ), new Vector3(sx0, GF - 0.25f, zm), new Vector3(cx0, GF - 0.25f, zm), Vector3.down);
        }

        // ------------------------------------------------------------------ the flats
        void BuildFlat(Floor fl, Job j)
        {
            int f = fl.f; float wf = j.xb - j.xa, df = j.zb - j.za, y0 = Yf(f), H = Hf(f);
            var fr = new GameObject("Flat" + (j.idx + 1)).transform; fr.SetParent(fl.go.transform, false);
            if (j.front) { fr.localPosition = new Vector3(j.xa, 0f, j.zb); fr.localRotation = Quaternion.identity; }
            else { fr.localPosition = new Vector3(j.xb, 0f, j.za); fr.localRotation = Quaternion.Euler(0f, 180f, 0f); }
            var spec = new HomeBuilder.FlatSpec { outside = TownMats.lobbyWall, extSide = j.ext, bareL = j.bareL, bareR = j.bareR };
            foreach (var w in wins)
            {
                if (w.y0 < y0 - 0.01f || w.y0 >= y0 + H - 0.01f) continue;
                float ya = w.y0 - y0, yb = w.y1 - y0;
                if (j.front)
                {
                    if (w.side == 'F') { if (w.a1 > j.xa && w.a0 < j.xb) spec.winsB.Add(new Vector4(w.a0 - j.xa, w.a1 - j.xa, ya, yb)); }
                    else if (w.side == j.ext && w.a1 > j.za && w.a0 < j.zb) spec.winsS.Add(new Vector4(j.zb - w.a1, j.zb - w.a0, ya, yb));
                }
                else
                {
                    if (w.side == 'B') { if (w.a1 > j.xa && w.a0 < j.xb) spec.winsB.Add(new Vector4(j.xb - w.a1, j.xb - w.a0, ya, yb)); }
                    else if (((w.side == 'L' && j.ext == 'R') || (w.side == 'R' && j.ext == 'L')) && w.a1 > j.za && w.a0 < j.zb) spec.winsS.Add(new Vector4(w.a0 - j.za, w.a1 - j.za, ya, yb));
                }
            }
            int id = seed * 1000 + f * 8 + j.idx; var rng = new System.Random(id * 31 + 17);
            int no = (f - 1) * 4 + j.idx + 1;
            var hb = new HomeBuilder { flat = true, rng = rng, seed = id, name = block + " flat " + no, x0 = 0f, mirror = false };
            hb.W = wf; hb.D = df; hb.U0 = 0.12f; hb.U1 = wf - 0.12f; hb.D0 = 0.12f; hb.D1 = df - 0.12f;
            float c = H - 0.25f; hb.Y = new[] { FloorTop, FloorTop + H, FloorTop + 2 * H }; hb.C = new[] { c, c + H, c + 2 * H };
            hb.H = Household.Make(id, rng); hb.fs = spec;
            var home = hb.Build(fr);
            home.label = "Flat " + no; home.Attach();
            // a doormat outside, its number on the door
            var mk = new MeshKit(); float du = spec.doorU;
            var mat = hb.H.Occupied ? HomeBuilder.Flat(new Color(0.22f + (float)rng.NextDouble() * 0.3f, 0.16f, 0.1f), 0.1f) : null;
            if (mat != null) mk.Box(mat, new Vector3(du - 0.4f, FloorTop, 0.2f), new Vector3(du + 0.4f, FloorTop + 0.015f, 0.7f));
            mk.Box(CotMats.brass, new Vector3(du + 0.62f, FloorTop + 1.45f, 0.0f), new Vector3(du + 0.78f, FloorTop + 1.57f, 0.012f));
            var mg = mk.Build("Mat", fr, false, ShadowCastingMode.Off); if (mg != null) mg.layer = Bld.DetailLayer;
        }

        // ------------------------------------------------------------------ the ground floor's rooms, and chute cupboards in narrow gaps
        void BuildStore(Floor fl, Job j)
        {
            int f = fl.f; float H = Hf(f), ft = FloorTop, ch = H - 0.25f;
            var go = new GameObject("Store_" + j.kind); go.transform.SetParent(fl.go.transform, false);
            if (j.kind == "duct")
            {
                // too narrow for anything: a service duct, solid, painted where the corridor sees it
                var dkit = new MeshKit(); dkit.Box(TownMats.lobbyWall, new Vector3(j.xa, 0f, j.za), new Vector3(j.xb, H, j.zb)); dkit.Build("Duct", go.transform, false, ShadowCastingMode.Off);
                var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3((j.xa + j.xb) * 0.5f, H * 0.5f, (j.za + j.zb) * 0.5f); bc.size = new Vector3(j.xb - j.xa, H, j.zb - j.za);
                return;
            }
            var k = new MeshKit(); var fk = new MeshKit(); var cols = new List<(Vector3 c, Vector3 s)>();
            var wallM = TownMats.lobbyWall; var inner = HomeBuilder.Surf("concrete_floor", 2f, new Color(0.78f, 0.78f, 0.76f), 0.1f); var ceilM = HomeBuilder.Flat(new Color(0.8f, 0.8f, 0.78f), 0.1f);
            void Bx(MeshKit m, Material mat, float x0, float x1, float y0, float y1, float z0, float z1, bool col = false)
            {
                if (x1 - x0 < 0.001f || y1 - y0 < 0.001f || z1 - z0 < 0.001f) return;
                if (m != null && mat != null) m.Box(mat, new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));
                if (col) cols.Add((new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, y1 - y0, z1 - z0)));
            }
            float xa = j.xa + 0.12f, xb = j.xb - 0.12f, za = j.front ? 0.12f : j.za + 0.12f, zb = j.front ? j.zb - 0.12f : D - 0.12f;
            // floor, ceiling, the four walls (the corridor one with the door)
            k.Quad(inner, new Vector3(xa, ft, za), new Vector3(xb, ft, za), new Vector3(xb, ft, zb), new Vector3(xa, ft, zb), Vector3.up); Bx(null, null, j.xa, j.xb, ft - 0.26f, ft, j.za, j.zb, true);
            k.Quad(ceilM, new Vector3(xa, ch, za), new Vector3(xb, ch, za), new Vector3(xb, ch, zb), new Vector3(xa, ch, zb), Vector3.down);
            Bx(k, wallM, j.xa, xa, 0f, H, j.za, j.zb, true); Bx(k, wallM, xb, j.xb, 0f, H, j.za, j.zb, true);
            Bx(k, wallM, j.xa, j.xb, 0f, H, j.front ? 0f : zb, j.front ? za : j.zb, true);
            float dz0 = j.front ? zb : j.za, dz1 = j.front ? j.zb : za;
            float door = j.front ? (j.idx == 0 ? xb - 0.7f : xa + 0.7f) : (xa + xb) * 0.5f;
            door = Mathf.Clamp(door, xa + 0.55f, xb - 0.55f);
            Bx(k, wallM, j.xa, door - 0.48f, 0f, H, dz0, dz1, true); Bx(k, wallM, door + 0.48f, j.xb, 0f, H, dz0, dz1, true); Bx(k, wallM, door - 0.48f, door + 0.48f, 2.1f, H, dz0, dz1, true);
            var d = PropDoor.Make(go.transform, "door_steel", new Vector3(door, ft, (dz0 + dz1) * 0.5f), 0f, "store");
            Bx(fk, CouncilEstate.M.glow, (xa + xb) * 0.5f - 0.5f, (xa + xb) * 0.5f + 0.5f, ch - 0.05f, ch, (za + zb) * 0.5f - 0.06f, (za + zb) * 0.5f + 0.06f);
            // the home for its spots
            var home = go.AddComponent<Home>(); home.H = core != null ? core.H : Household.Make(seed, new System.Random(seed)); home.label = j.kind; home.variant = "store";
            var rng = new System.Random(seed * 59 + f * 7 + j.idx);
            void Spot(string kind, string label, string tag, float x, float z, bool hidden = false) => home.spots.Add(new Home.SpotRec { kind = kind, label = label, tag = tag, pos = new Vector3(x, ft, z), yaw = 0f, room = 0, lv = 0, hidden = hidden });
            float back = j.front ? za + 0.05f : zb - 0.05f, sgn = j.front ? 1f : -1f;   // the wall away from the door, and which way is into the room from it
            var grey = HomeBuilder.Flat(new Color(0.42f, 0.44f, 0.45f), 0.4f); var green = HomeBuilder.Flat(new Color(0.18f, 0.35f, 0.2f), 0.4f); var lid = HomeBuilder.Flat(new Color(0.08f, 0.08f, 0.08f), 0.4f);
            var bag = HomeBuilder.Flat(new Color(0.06f, 0.06f, 0.07f), 0.7f);
            switch (j.kind)
            {
                case "bins":
                case "chute":
                    {
                        int nb = Mathf.Max(1, Mathf.FloorToInt((xb - xa - 0.2f) / 1.4f)); if (j.kind == "chute") nb = 1;
                        for (int i = 0; i < nb; i++)
                        {
                            float cx = xa + 0.8f + i * 1.4f; float z0 = j.front ? back : back - 1.05f, z1 = z0 + 1.05f;
                            Bx(fk, i % 3 == 2 ? green : grey, cx - 0.62f, cx + 0.62f, ft + 0.12f, ft + 1.25f, z0, z1, true);
                            Bx(fk, lid, cx - 0.66f, cx + 0.66f, ft + 1.25f, ft + 1.33f, z0 - 0.02f, z1 + 0.02f);
                            for (int w = 0; w < 4; w++) fk.Cyl(lid, new Vector3(cx + (w % 2 == 0 ? -0.5f : 0.5f), ft, w < 2 ? z0 + 0.1f : z1 - 0.1f), 0.07f, 0.12f, 8);
                            Spot("rummage", "Go through the bins", "bins", cx, j.front ? z1 + 0.35f : z0 - 0.35f);
                        }
                        for (int i = 0; i < 3 + rng.Next(4); i++) { float bx = xa + 0.3f + (float)rng.NextDouble() * (xb - xa - 0.6f), bz = (za + zb) * 0.5f + ((float)rng.NextDouble() - 0.5f) * (zb - za) * 0.4f; fk.Sphere(bag, new Vector3(bx, ft + 0.2f, bz), new Vector3(0.22f, 0.22f, 0.2f), 8, 5); }
                        if (j.kind == "chute") { Bx(fk, TownMats.steel, xa + 0.4f, xa + 1.2f, ch - 0.6f, ch, j.front ? back : back - 0.8f, j.front ? back + 0.8f : back, false); }
                        break;
                    }
                case "caretaker":
                    {
                        var wood = HomeBuilder.Flat(new Color(0.55f, 0.42f, 0.3f), 0.3f);
                        float dzz = j.front ? back : back - 0.7f;
                        Bx(fk, wood, xa + 0.3f, xa + 1.7f, ft + 0.72f, ft + 0.76f, dzz, dzz + 0.7f, true); Bx(fk, wood, xa + 0.32f, xa + 0.36f, ft, ft + 0.72f, dzz + 0.05f, dzz + 0.65f); Bx(fk, wood, xa + 1.64f, xa + 1.68f, ft, ft + 0.72f, dzz + 0.05f, dzz + 0.65f);
                        Bx(fk, lid, xa + 0.7f, xa + 1.1f, ft + 0.76f, ft + 1.06f, dzz + 0.1f, dzz + 0.14f); // a little telly
                        Spot("rummage", "Go through the caretaker's desk", "drawers", xa + 1.0f, j.front ? dzz + 1.0f : dzz - 0.3f);
                        // key cabinet, shelving with paint tins, mop and bucket
                        Bx(fk, TownMats.steel, xb - 0.62f, xb - 0.02f, ft + 1.1f, ft + 1.7f, (za + zb) * 0.5f - 0.3f, (za + zb) * 0.5f + 0.3f, false);
                        Spot("rummage", "Open the key cabinet", "cabinet", xb - 0.5f, (za + zb) * 0.5f);
                        Bx(fk, TownMats.steel, xb - 0.45f, xb - 0.02f, ft, ft + 1.9f, za + 0.1f, za + 1.1f, true);
                        for (int i = 0; i < 6; i++) fk.Cyl(HomeBuilder.Flat(Color.HSVToRGB((float)rng.NextDouble(), 0.5f, 0.85f), 0.4f), new Vector3(xb - 0.25f, ft + 0.4f + (i / 3) * 0.6f, za + 0.3f + (i % 3) * 0.28f), 0.09f, 0.2f, 10);
                        Spot("rummage", "Go through the shelving", "shelf", xb - 0.7f, za + 0.6f);
                        fk.Cyl(HomeBuilder.Flat(new Color(0.85f, 0.75f, 0.1f), 0.4f), new Vector3(xa + 0.4f, ft, (za + zb) * 0.5f), 0.18f, 0.3f, 10);
                        break;
                    }
                case "bikes":
                    {
                        var frame = HomeBuilder.Flat(new Color(0.6f, 0.1f, 0.1f), 0.5f); var tyre = HomeBuilder.Flat(new Color(0.05f, 0.05f, 0.05f), 0.3f);
                        int nbk = Mathf.Max(2, Mathf.FloorToInt((xb - xa) / 0.7f) - 1);
                        for (int i = 0; i < nbk; i++)
                        {
                            float bx = xa + 0.5f + i * 0.7f; float bz = j.front ? back + 0.55f : back - 0.55f;
                            fk.Push(new Vector3(bx, ft, bz), 0f);
                            fk.CylX(tyre, new Vector3(0, 0.3f, 0.5f), 0.3f, 0.04f, 12); fk.CylX(tyre, new Vector3(0, 0.3f, -0.5f), 0.3f, 0.04f, 12);
                            fk.Box(i % 2 == 0 ? frame : grey, new Vector3(-0.02f, 0.28f, -0.5f), new Vector3(0.02f, 0.32f, 0.5f)); fk.Box(lid, new Vector3(-0.1f, 0.75f, -0.35f), new Vector3(0.1f, 0.79f, -0.25f));
                            fk.Pop();
                        }
                        Bx(null, null, xa + 0.2f, xa + 0.5f + nbk * 0.7f, ft, ft + 1.0f, j.front ? back : back - 1.1f, j.front ? back + 1.1f : back, true);
                        // a pram, a mattress against the wall, junk
                        Bx(fk, HomeBuilder.Flat(new Color(0.15f, 0.2f, 0.4f), 0.4f), xb - 1.1f, xb - 0.4f, ft + 0.3f, ft + 0.9f, (za + zb) * 0.5f - 0.4f, (za + zb) * 0.5f + 0.4f, true);
                        Bx(fk, HomeBuilder.Flat(new Color(0.85f, 0.8f, 0.7f), 0.1f), xb - 0.25f, xb - 0.05f, ft, ft + 1.9f, za + 0.3f, za + 1.7f, true);
                        Spot("rummage", "Poke about in the junk", "bag", xb - 0.9f, (za + zb) * 0.5f + (j.front ? 0.8f : -0.8f), true);
                        break;
                    }
                default: // drying room
                    {
                        float z0 = za + 0.6f, z1 = zb - 0.6f;
                        for (int l = 0; l < 3; l++)
                        {
                            float lx = xa + (l + 1) * (xb - xa) / 4f;
                            Bx(fk, TownMats.steel, lx - 0.01f, lx + 0.01f, ch - 0.45f, ch - 0.43f, z0, z1);
                            for (float z = z0 + 0.2f; z < z1 - 0.3f; z += 0.5f) if (rng.NextDouble() < 0.6) Bx(fk, HomeBuilder.Flat(Color.HSVToRGB((float)rng.NextDouble(), 0.35f, 0.85f), 0.1f), lx - 0.01f, lx + 0.01f, ch - 1.2f, ch - 0.44f, z, z + 0.4f);
                        }
                        Bx(fk, HomeBuilder.Flat(new Color(0.94f, 0.94f, 0.95f), 0.6f), xa + 0.05f, xa + 0.65f, ft, ft + 0.85f, za + 0.1f, za + 0.7f, true);
                        Spot("rummage", "Go through the washing", "basket", (xa + xb) * 0.5f, (za + zb) * 0.5f);
                        Spot("rummage", "Check the washing machine", "washer", xa + 0.8f, za + 0.4f);
                        break;
                    }
            }
            k.Build("Shell", go.transform, false, ShadowCastingMode.Off);
            var fg = fk.Build("Stuff", go.transform, false, ShadowCastingMode.Off); if (fg != null) fg.layer = Bld.DetailLayer;
            var cg = new GameObject("Solids"); cg.transform.SetParent(go.transform, false);
            foreach (var c in cols) { var b = cg.AddComponent<BoxCollider>(); b.center = c.c; b.size = new Vector3(Mathf.Max(0.02f, c.s.x), Mathf.Max(0.02f, c.s.y), Mathf.Max(0.02f, c.s.z)); }
            home.rooms.Add(new Home.RoomRec { type = "hall", use = j.kind, lv = 0, centre = new Vector3((xa + xb) * 0.5f, 0f, (za + zb) * 0.5f), u0 = xa, u1 = xb, d0 = za, d1 = zb });
            home.zoneRects.Add(Rect.MinMaxRect(xa, za, xb, zb)); home.centre = new Vector3((xa + xb) * 0.5f, 1.3f, (za + zb) * 0.5f); home.radius = 6f; home.midZ = (za + zb) * 0.5f; home.halfD = (zb - za) * 0.5f + 0.3f;
            if (d != null) home.doors.Add(new Home.DoorRec { door = d, tag = "store", lv = 0, tier = -1 });
            home.Attach();
        }

        // ================================================================== test: build floors 0..3 and flood-walk them from the street
        public static void Test(string key) { new GameObject("TowerTest").AddComponent<TestRun>().key = key; }
        class TestRun : MonoBehaviour
        {
            public string key;
            IEnumerator Start()
            {
                yield return new WaitForSeconds(0.5f);
                foreach (var ti in new List<TowerInterior>(All))
                {
                    if (ti == null || (key != "all" && ti.block.Replace(" ", "-") != key)) continue;
                    var t0 = Time.realtimeSinceStartup;
                    if (!ti.calc) ti.Calc(); ti.pinned = true; if (!ti.active) ti.Activate();
                    for (int f = 0; f <= 3 && f < ti.storeys; f++) ti.BuildNow(f);
                    float bt = Time.realtimeSinceStartup - t0;
                    yield return null;
                    var doors = new List<Collider>(); foreach (var d in ti.GetComponentsInChildren<PropDoor>()) foreach (var c in d.GetComponentsInChildren<Collider>()) if (c.enabled) { c.enabled = false; doors.Add(c); }
                    Physics.SyncTransforms();
                    var tr = ti.transform; const float cs = 0.1f;
                    var seen = new HashSet<long>(); var q = new Queue<Vector3>(); var reached = new List<Vector3>();
                    q.Enqueue(new Vector3(ti.coreX, 0.02f, ti.lobbyZ - 0.6f)); q.Enqueue(new Vector3(ti.coreX, 0.02f, ti.lobbyZ + 0.8f)); int guard = 0;
                    { Vector3 sp = tr.TransformPoint(new Vector3(ti.coreX, 0.5f, ti.lobbyZ + 0.8f)); bool hh = Physics.Raycast(sp, Vector3.down, out var h0, 2f, ~0, QueryTriggerInteraction.Ignore); Debug.Log($"[ASBO] towertest seed ray: {hh} {(hh ? h0.collider.name + " y=" + tr.InverseTransformPoint(h0.point).y.ToString("0.00") : "")} capsule={Physics.CheckCapsule(tr.TransformPoint(new Vector3(ti.coreX, 0.67f, ti.lobbyZ + 0.8f)), tr.TransformPoint(new Vector3(ti.coreX, 1.52f, ti.lobbyZ + 0.8f)), 0.28f, ~0, QueryTriggerInteraction.Ignore)}"); }
                    while (q.Count > 0 && guard++ < 600000)
                    {
                        var p = q.Dequeue(); long kk = ((long)Mathf.RoundToInt((p.x + 2f) / cs) * 4096 + Mathf.RoundToInt((p.z + 2f) / cs)) * 512 + Mathf.RoundToInt(p.y / 0.3f);
                        if (!seen.Add(kk)) continue; reached.Add(p);
                        foreach (var dv in new[] { new Vector2(cs, 0), new Vector2(-cs, 0), new Vector2(0, cs), new Vector2(0, -cs) })
                        {
                            float nx = p.x + dv.x, nz = p.z + dv.y; if (nx < -0.5f || nx > ti.W + 0.5f || nz < -1.2f || nz > ti.D + 0.5f) continue;
                            if (nz < 0f && (nx < ti.recessX0 || nx > ti.recessX1)) continue;
                            Vector3 wp = tr.TransformPoint(new Vector3(nx, p.y + 0.45f, nz));
                            if (!Physics.Raycast(wp, Vector3.down, out var hit, 0.95f, ~0, QueryTriggerInteraction.Ignore)) continue;
                            float gy = tr.InverseTransformPoint(hit.point).y; if (gy - p.y > 0.4f || p.y - gy > 0.7f) continue;
                            if (gy > ti.Yf(3) + 0.5f) continue;
                            Vector3 a = tr.TransformPoint(new Vector3(nx, gy + 0.65f, nz)), b = tr.TransformPoint(new Vector3(nx, gy + 1.5f, nz));
                            if (Physics.CheckCapsule(a, b, 0.28f, ~0, QueryTriggerInteraction.Ignore)) continue;
                            q.Enqueue(new Vector3(nx, gy, nz));
                        }
                    }
                    foreach (var c in doors) c.enabled = true;
                    ti.pinned = false;
                    // every room of every home in the floors built, and the landings
                    int total = 0, bad = 0, spots = 0, rum = 0; var miss = new List<string>(); float top = 0f; foreach (var p in reached) top = Mathf.Max(top, p.y);
                    int spotBad = 0; var spotMiss = new List<string>();
                    foreach (var h in ti.GetComponentsInChildren<Home>())
                    {
                        if (h == ti.core) continue; spots += h.spots.Count;
                        var near = new List<Vector3>(); // reached cells in this home's local frame
                        foreach (var p in reached) near.Add(h.transform.InverseTransformPoint(tr.TransformPoint(p)));
                        bool flat = h.variant != null && h.variant.StartsWith("flat");
                        foreach (var r in h.rooms)
                        {
                            if (r.type == "cupboard") continue;
                            total++; bool ok = false;
                            foreach (var lp in near)
                            {
                                if (Mathf.Abs(lp.y - r.centre.y) > 0.6f) continue;
                                if (flat) { float u = lp.x, d = -lp.z; if (u > r.u0 + 0.15f && u < r.u1 - 0.15f && d > r.d0 + 0.15f && d < r.d1 - 0.15f) { ok = true; break; } }
                                else if ((lp.x - r.centre.x) * (lp.x - r.centre.x) + (lp.z - r.centre.z) * (lp.z - r.centre.z) < 2.25f) { ok = true; break; }
                            }
                            if (!ok) { bad++; miss.Add(h.name + ":" + r.type + "@" + tr.InverseTransformPoint(h.transform.TransformPoint(r.centre)).ToString("0.0")); }
                        }
                        foreach (var sp in h.spots)
                        {
                            if (sp.kind != "rummage") continue; rum++; bool ok = false;
                            foreach (var lp in near) if (Mathf.Abs(lp.y - sp.pos.y) < 0.6f && (lp.x - sp.pos.x) * (lp.x - sp.pos.x) + (lp.z - sp.pos.z) * (lp.z - sp.pos.z) < 1.0f) { ok = true; break; }
                            if (!ok) { spotBad++; if (spotMiss.Count < 12) spotMiss.Add(h.name + ":" + sp.tag); }
                        }
                    }
                    miss.Add($"rummage spots out of reach: {spotBad}/{rum} [{string.Join(", ", spotMiss)}]");
                    for (int f = 1; f <= 3 && f < ti.storeys; f++)
                    {
                        total++; bool ok = false; float y = ti.Yf(f) + FloorTop; float lx = (ti.sx0 + ti.sx1) * 0.5f, lz = (ti.zL + ti.zm) * 0.5f;
                        foreach (var p in reached) if (Mathf.Abs(p.y - y) < 0.3f && Mathf.Abs(p.x - lx) < 1f && Mathf.Abs(p.z - lz) < 0.8f) { ok = true; break; }
                        if (!ok) { bad++; miss.Add("landing" + f); }
                    }
                    Debug.Log($"[ASBO] towertest {ti.block}: built 0..3 in {bt * 1000f:0}ms, reached {reached.Count} cells (top y {top:0.0}), {bad}/{total} rooms unreachable; {spots} spots ({rum} rummage), lift {(ti.liftBroken ? "broken" : "works")}\n  {string.Join("\n  ", miss)}");
                    yield return null;
                }
                Debug.Log("[ASBO] towertest: done"); Destroy(gameObject);
            }
        }

        /// <summary>Pictures of one floor: the lobby or corridor, the stair, and every room of every flat (to Shots/views/tw_*).</summary>
        public static void Shots(string key, int floor) { var r = new GameObject("TowerShots").AddComponent<ShotRun>(); r.key = key; r.floor = floor; }
        class ShotRun : MonoBehaviour
        {
            public string key; public int floor;
            IEnumerator Start()
            {
                TowerInterior ti = null; foreach (var t in All) if (t != null && t.block.Replace(" ", "-") == key) ti = t;
                if (ti == null) { Debug.Log("[ASBO] towershots: no " + key); Destroy(gameObject); yield break; }
                var pc = FindFirstObjectByType<PlayerCtl>(); var tr = ti.transform; if (!ti.calc) ti.Calc();
                int f = floor; float y = ti.Yf(f) + FloorTop;
                pc.Teleport(tr.TransformPoint(new Vector3((ti.xL + ti.xR) * 0.5f, y + 0.1f, (ti.zm + ti.zc1) * 0.5f)), tr.eulerAngles.y, 10f);
                yield return new WaitForSeconds(1.0f); ti.BuildNow(f); yield return new WaitForSeconds(0.8f);
                int n = 0;
                void Shot(Vector3 at, Vector3 look, string name) { var a = tr.TransformPoint(at); var b = tr.TransformPoint(look); DebugCmd.ShotAt(a.x, a.y, a.z, b.x, b.y, b.z, $"tw_{key}_{f}_{n++:D2}_{name}"); }
                Shot(new Vector3(ti.xL + 0.4f, y + 1.6f, (ti.zm + ti.zc1) * 0.5f), new Vector3(ti.xR, y + 1.0f, (ti.zm + ti.zc1) * 0.5f), "corridor");
                Shot(new Vector3(ti.sx0 + 0.6f, y + 1.7f, ti.zm - 0.2f), new Vector3(ti.sx0 + 0.6f, y + 0.6f, ti.zH0), "stair");
                if (f == 0) Shot(new Vector3(ti.coreX, y + 1.6f, ti.lobbyZ + 0.3f), new Vector3(ti.cx0 + 1.5f, y + 0.9f, ti.zm), "lobby");
                ti.floors.TryGetValue(f, out var fl);
                if (fl != null)
                    foreach (var h in fl.go.GetComponentsInChildren<Home>())
                    {
                        foreach (var r in h.rooms)
                        {
                            if (r.type == "cupboard") continue;
                            bool flat = h.variant != null && h.variant.StartsWith("flat");
                            Vector3 c0, c1;
                            if (flat) { float cu = (r.u0 + r.u1) * 0.5f; c0 = new Vector3(cu, r.centre.y + 1.65f, -(r.d0 + 0.25f)); c1 = new Vector3(cu, r.centre.y + 0.7f, -r.d1); }
                            else { c0 = new Vector3(r.u0 + 0.3f, r.centre.y + 1.6f, r.d0 + 0.3f); c1 = new Vector3(r.u1 - 0.2f, r.centre.y + 0.5f, r.d1 - 0.2f); }
                            Vector3 wc = h.transform.TransformPoint(r.centre);
                            pc.Teleport(wc + Vector3.up * 0.1f, 0f, 10f); yield return new WaitForSeconds(0.5f);
                            Vector3 a = tr.InverseTransformPoint(h.transform.TransformPoint(c0)), b = tr.InverseTransformPoint(h.transform.TransformPoint(c1));
                            Shot(a, b, h.name.Replace(" ", "") + "_" + r.type);
                            yield return null;
                        }
                    }
                Debug.Log($"[ASBO] towershots: {n} pictures"); Destroy(gameObject);
            }
        }

        // ================================================================== the lift
        bool liftOpen; int liftFrom; float fade; int fadeDir; static GUIStyle btn, head;
        public void CallLift(int spot)
        {
            var pw = PropWorld.I;
            if (liftBroken) { if (pw != null) pw.Say(Random.value < 0.5f ? "You jab the button. Nothing. OUT OF ORDER, says a soggy note. It's the stairs." : "The lift's stuck between floors again. Somebody's been sick in it anyway."); return; }
            int.TryParse(core.spots[spot].tag, out liftFrom); liftOpen = true;
        }
        void OnGUI()
        {
            if (fade > 0.001f) { var c = GUI.color; GUI.color = new Color(0, 0, 0, fade); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = c; }
            if (!liftOpen) return;
            float s = Mathf.Max(1f, Screen.height / 720f);
            if (btn == null) { btn = new GUIStyle(GUI.skin.button); head = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }; }
            btn.fontSize = Mathf.RoundToInt(26 * s); head.fontSize = Mathf.RoundToInt(24 * s); head.normal.textColor = Color.white;
            int cols = 4, rows = Mathf.CeilToInt(storeys / (float)cols);
            float bw = 120f * s, bh = 70f * s, pad = 10f * s, w = cols * (bw + pad) + pad, h = rows * (bh + pad) + pad + 60f * s + bh;
            var box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.color = new Color(0.1f, 0.1f, 0.12f, 0.92f); GUI.DrawTexture(box, Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(box.x, box.y + pad, w, 44f * s), block + " - which floor?", head);
            for (int i = 0; i < storeys; i++)
            {
                int r = i / cols, c = i % cols; var rr = new Rect(box.x + pad + c * (bw + pad), box.y + 60f * s + r * (bh + pad), bw, bh);
                GUI.enabled = i != liftFrom;
                if (GUI.Button(rr, i == 0 ? "G" : i.ToString(), btn)) { liftOpen = false; StartCoroutine(Ride(i)); }
                GUI.enabled = true;
            }
            if (GUI.Button(new Rect(box.x + pad, box.yMax - bh - pad, w - pad * 2, bh), "Never mind", btn)) liftOpen = false;
        }
        static readonly string[] RideLines =
        {
            "The lift stinks of piss and Lynx.", "Someone's scratched their name into the mirror. Twice.", "The lift judders, thinks about it, and carries on.",
            "A bloke gets in, looks at you, and gets straight back out.", "The light flickers the whole way. Lovely.", "Ding."
        };
        IEnumerator Ride(int f)
        {
            fadeDir = 1; while (fade < 1f) yield return null;
            BuildNow(f); if (f > 0) BuildNow(f - 1);
            var pc = player != null ? player.GetComponent<PlayerCtl>() : null;
            Vector3 lp = f == 0 ? new Vector3(LiftX, Yf(0) + 0.1f, zm - 1.7f - 0.7f) : new Vector3(LiftX, Yf(f) + 0.1f, zm + 0.7f);
            float yaw = transform.eulerAngles.y + (f == 0 ? 180f : 0f);
            if (pc != null) pc.Teleport(transform.TransformPoint(lp), yaw, 10f);
            NoiseBus.Emit(transform.TransformPoint(lp), 3f, player);
            yield return new WaitForSeconds(0.9f);
            if (PropWorld.I != null) PropWorld.I.Say(RideLines[Random.Range(0, RideLines.Length)] + (f == 0 ? " Ground floor." : " Floor " + f + "."));
            fadeDir = -1;
        }
    }
}
