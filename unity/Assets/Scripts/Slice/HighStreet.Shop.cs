using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// The inside of every shop on the parades: a shop floor fitted for what it sells (or bare and dusty if it is to let), a stockroom
    /// or kitchen behind, a back lobby with a toilet, and a stair up the east side to the flat over it (and a second flight to the top
    /// flat in the three-storey units). Worked in "lx, d" space: lx across the unit from its west party wall, d back from the front.
    /// </summary>
    public static partial class HighStreet
    {
        // recorded by the era builders for the interior to match (windows, back wall openings, heights)
        static List<Rect> lastHoles = new List<Rect>(), lastBack = new List<Rect>();
        static float lastG = 3.9f, lastFH = 3.0f, doorC;
        static int shopSeq;
        public static bool WithInteriors = true;

        /// <summary>What a shopfront atlas cell sells.</summary>
        static string KindOf(int cell)
        {
            if (EmptyCells.Contains(cell)) return "empty";
            switch (cell)
            {
                case 0: case 20: return "grocer";
                case 1: case 2: case 21: return "food";
                case 13: return "bakery";
                case 3: return "bookie";
                case 4: return "laundry";
                case 5: case 12: return "salon";
                case 7: return "charity";
                case 8: return "post";
                case 9: return "pharmacy";
                case 10: return "pound";
                case 11: return "vape";
                default: return "grocer";
            }
        }

        static void ShopInterior(Transform g, Unit u, float x0, float x1, float topH, string parade, int idx)
        {
            if (!WithInteriors) return;
            var b = new ShopB { parent = g, x0 = x0, w = x1 - x0, G = lastG, FH = lastFH, storeys = Mathf.Max(2, u.storeys), topH = topH, name = parade + "_shop" + (idx + 1), unit = u, holes = new List<Rect>(lastHoles), back = new List<Rect>(lastBack), doorX = doorC };
            b.kind = KindOf(u.shop);
            int seed = parade.GetHashCode() * 31 + idx * 977 + u.shop;
            b.rng = new System.Random(seed);
            b.Build();
        }

        /// <summary>A shop with its flat over in someone else's building (the precinct): frame has the front at z = 0 and the inside to -z, the unit 0..w across.
        /// holes: the upper front windows (x across the unit, y); back: the back wall's openings (the door at 0.8..1.7 and the windows).</summary>
        public static void UnitInterior(Transform frame, string name, float w, float G, float FH, int storeys, float topH, int cell, float doorX, List<Rect> holes, List<Rect> back, float depth = Depth)
        {
            var b = new ShopB { parent = frame, x0 = 0f, w = w, G = G, FH = FH, storeys = Mathf.Max(2, storeys), topH = topH, name = name, unit = default, holes = holes, back = back, doorX = doorX, dep = depth };
            b.kind = KindOf(cell); if (b.kind == "empty" && cell % 3 != 0) b.kind = new[] { "grocer", "food", "pound", "charity", "salon", "bookie", "laundry", "pharmacy", "vape", "bakery", "post" }[cell % 11];
            b.rng = new System.Random(name.GetHashCode() * 31 + cell);
            b.Build();
        }

        // ================================================================================================================
        sealed partial class ShopB
        {
            public Transform parent; public float x0, w, G, FH, topH, doorX; public int storeys; public string name, kind; public Unit unit; public List<Rect> holes, back;
            public System.Random rng; public Household H, S;
            const float PT = 0.1f, SW = 0.12f;
            public float dep = Depth; float DBack => dep - 0.14f;
            const float DS = 4.9f;               // back of the shop floor
            const float AF = 9.9f, AT = 5.15f;   // the lower flight: foot (d) and top (d), 19 treads of 0.25
            float[] Yf, Cf;
            public class Room { public int lv, idx; public string type, use; public float lx0, lx1, d0, d1; public Material floor; public Color wall; public bool noFurnish, wet; public List<Rect> items = new List<Rect>(); public float Cx => (lx0 + lx1) * 0.5f; public float Cd => (d0 + d1) * 0.5f; public bool Has(float x, float d) => x >= lx0 - 0.01f && x <= lx1 + 0.01f && d >= d0 - 0.01f && d <= d1 + 0.01f; }
            public class Wl { public int lv; public char ax; public float at, a, b, t = PT; public bool mesh = true; public Color col = new Color(0.85f, 0.84f, 0.8f); public List<(float c, float wd, bool leaf, string tag)> gaps = new List<(float, float, bool, string)>(); }
            readonly List<Room> rooms = new List<Room>(); readonly List<Wl> walls = new List<Wl>(); readonly List<Rect> voids = new List<Rect>(); readonly List<int> voidLv = new List<int>();
            readonly List<(Vector3 c, Vector3 s)> solids = new List<(Vector3, Vector3)>();
            MeshKit shell = new MeshKit(), fin = new MeshKit(), furn = new MeshKit(), glass = new MeshKit(), deco = new MeshKit();
            Transform root; Home home; Material plaster, ceil, trim, black, steel, whiteM;
            readonly List<Rect>[] clear = { new List<Rect>(), new List<Rect>(), new List<Rect>() };
            readonly List<Rect>[] hard = { new List<Rect>(), new List<Rect>(), new List<Rect>() };

            public Vector3 P(float lx, float y, float d) => new Vector3(x0 + lx, y, -d);
            float Fd(int lv) => lv == 0 ? 0.26f : 0.14f;

            void Bx(MeshKit k, Material m, float lx0, float lx1, float y0, float y1, float d0, float d1, bool col = false)
            {
                Vector3 a = P(lx0, y0, d0), c = P(lx1, y1, d1); Vector3 mn = Vector3.Min(a, c), mx = Vector3.Max(a, c);
                k.Box(m, mn, mx); if (col) solids.Add(((mn + mx) * 0.5f, mx - mn));
            }
            void Solid(float lx0, float lx1, float y0, float y1, float d0, float d1)
            {
                Vector3 a = P(lx0, y0, d0), c = P(lx1, y1, d1); Vector3 mn = Vector3.Min(a, c), mx = Vector3.Max(a, c); solids.Add(((mn + mx) * 0.5f, mx - mn));
            }

            Room AddRoom(int lv, string type, float lx0, float lx1, float d0, float d1)
            {
                var r = new Room { lv = lv, type = type, lx0 = lx0, lx1 = lx1, d0 = d0, d1 = d1, idx = rooms.Count }; rooms.Add(r); return r;
            }
            Wl AddWall(int lv, char ax, float at, float a, float b, float t = PT) { var wl = new Wl { lv = lv, ax = ax, at = at, a = a, b = b, t = t }; walls.Add(wl); return wl; }
            void Gap(Wl wl, float c, float wd, bool leaf = true, string tag = "") { wl.gaps.Add((c, wd, leaf, tag)); }

            // ------------------------------------------------------------------------------------------ build
            public void Build()
            {
                float L0 = SW, L1 = w - SW;
                Yf = new float[storeys]; Cf = new float[storeys];
                Yf[0] = 0.04f; for (int f = 1; f < storeys; f++) Yf[f] = G + (f - 1) * FH;
                for (int f = 0; f < storeys; f++) Cf[f] = f + 1 < storeys ? Yf[f + 1] - 0.35f : Yf[f] + FH - 0.3f;
                plaster = HomeBuilder.Surf("plaster_smooth", 2f, new Color(0.88f, 0.87f, 0.84f), 0.1f); ceil = HomeBuilder.Flat(new Color(0.93f, 0.92f, 0.89f), 0.1f);
                trim = HomeBuilder.Flat(new Color(0.94f, 0.94f, 0.92f), 0.4f); black = HomeBuilder.Flat(new Color(0.08f, 0.08f, 0.09f), 0.3f);
                steel = HomeBuilder.Flat(new Color(0.62f, 0.64f, 0.66f), 0.7f); whiteM = HomeBuilder.Flat(new Color(0.92f, 0.92f, 0.9f), 0.5f);
                bool empty = kind == "empty";
                S = Household.Make(1000 + shopSeq, rng); S.wealth = Mathf.Clamp01(S.wealth); if (empty) S.kind = Household.Kind.Vacant; else if (S.kind == Household.Kind.Vacant) S.kind = Household.Kind.Family;
                H = Household.Make(2000 + shopSeq, rng); if (empty) H.kind = Household.Kind.Vacant; else if (H.kind == Household.Kind.Vacant) H.kind = Household.Kind.Couple;
                shopSeq++;

                var go = new GameObject(name); go.transform.SetParent(parent, false); root = go.transform; home = go.AddComponent<Home>();
                home.H = S; home.label = kind == "empty" ? "Empty unit" : "Shop"; home.shop = true; home.shopKind = kind; home.variant = kind;

                LayoutGround(L0, L1);
                for (int f = 1; f < storeys; f++) LayoutUpper(f, L0, L1);
                // doors, walls
                foreach (var wl in walls) BuildWall(wl);
                BuildShellAndSlabs(L0, L1);
                InitClear();
                for (int i = 0; i < rooms.Count; i++) FitRoom(rooms[i]);
                Finish();
            }

            // ------------------------------------------------------------------------------------------ ground floor
            void LayoutGround(float L0, float L1)
            {
                float dcl = Mathf.Clamp(doorX - x0, 0.9f, w - 1.0f);
                var shop = AddRoom(0, "shop", L0, L1, Fd(0), DS); shop.use = kind;
                float xa = L1 - 1.0f;                                       // west edge of the stair strip
                bool kitchen = kind == "food" || kind == "bakery";
                var stock = AddRoom(0, kitchen ? "kitchen" : "stock", L0, xa, DS, 8.0f);
                var rear = AddRoom(0, "rear", L0, xa, 8.0f, DBack);
                var stair = AddRoom(0, "stairA", xa, L1, DS, DBack); stair.noFurnish = true; stair.use = "stair";
                // the front: a wall with the shopfront door in it (the facade supplies the look; this supplies the door and the gap)
                var wf = AddWall(0, 'x', Fd(0), L0, L1); wf.mesh = false; Gap(wf, dcl, 1.0f, true, "shopfront");
                // shop / back
                var wb = AddWall(0, 'x', DS, L0, L1); Gap(wb, Mathf.Min(1.4f, xa - 0.8f), 0.95f, true, "staff");
                // stock / rear
                var wr = AddWall(0, 'x', 8.0f, L0, xa); Gap(wr, Mathf.Clamp(xa * 0.5f + 0.3f, 1.2f, xa - 0.6f), 0.9f, true, "inner");
                // the stair strip's west wall, open at the foot
                var ws = AddWall(0, 'z', xa, DS, DBack); Gap(ws, 10.4f, 0.9f, false, "tall");
                // back door and toilet
                var wbk = AddWall(0, 'x', DBack, L0, xa); wbk.mesh = false; Gap(wbk, 1.25f, 0.9f, true, "back");
                // the toilet cubicle in the rear room's west corner, its door facing the lobby
                float cw = 1.15f;
                var wcR = AddRoom(0, "wc", L0, L0 + cw, 8.05f, 9.35f); wcR.wet = true; wcR.use = "wc"; wcRect = new Rect(L0, 8.0f, cw, 1.35f);
                var wcE = AddWall(0, 'z', L0 + cw, 8.0f, 9.35f); Gap(wcE, 8.7f, 0.7f, true, "wc");
                var wcS = AddWall(0, 'x', 9.35f, L0, L0 + cw);
                rear.use = "lobby";
                shop.floor = FloorMat(kind == "empty" ? "large_grey_tiles" : kind == "laundry" || kind == "food" || kind == "bakery" || kind == "pharmacy" ? "large_grey_tiles" : kind == "salon" || kind == "vape" ? "wood_slats" : "large_grey_tiles", kind == "charity" ? new Color(0.62f, 0.52f, 0.44f) : Color.white, 1.0f);
                stock.floor = FloorMat("large_grey_tiles", new Color(0.7f, 0.7f, 0.7f), 1.0f); rear.floor = stock.floor; stair.floor = stock.floor;
                var wallTint = new Color(0.9f, 0.89f, 0.85f);
                foreach (var r in new[] { shop, stock, rear, stair }) r.wall = wallTint;
                wb.col = wf.col = wallTint;
                voids.Add(new Rect(xa, AT, L1 - xa, 10.5f - AT)); voidLv.Add(1);   // the stairwell through the first floor
            }

            Material FloorMat(string name, Color tint, float tile) => HomeBuilder.Surf(name, tile, tint, 0.25f);

            // ------------------------------------------------------------------------------------------ upper floors
            int v1; Rect wcRect;
            void LayoutUpper(int f, float L0, float L1)
            {
                if (f == 1) v1 = rng.Next(3);
                float xa = L1 - 1.0f, xb = L1 - 2.05f; bool top = f == storeys - 1;
                float fd = Fd(f);
                var floorLiving = FloorMat(H.floorLiving, H.floorLiving.StartsWith("carpet") ? H.carpet : Color.white, 1.0f);
                var floorBed = FloorMat(H.floorBed, H.floorBed.StartsWith("carpet") ? Color.Lerp(H.carpet, H.bedding, 0.3f) : Color.white, 1.0f);
                var floorKit = FloorMat(H.floorKitchen, Color.white, 0.8f); var floorBath = FloorMat(H.floorBath, Color.white, 0.8f); var floorHall = FloorMat(H.floorHall, H.floorHall.StartsWith("carpet") ? H.carpet : Color.white, 1.0f);
                Room Mk(string type, float a, float b, float c, float d, Material fl, Color wc) { var r = AddRoom(f, type, a, b, c, d); r.floor = fl; r.wall = wc; return r; }
                float west = xb - 0.05f;     // the shaft's west wall
                // front half: the front room
                var hall = Mk("hall", L0, L1, 4.35f, 5.6f, floorHall, H.wallHall); hall.use = f == 1 ? "landing" : "landing2";
                string frontType = f == 1 ? (v1 == 2 ? "bedroom" : "living") : "bedroom";
                var front = Mk(frontType, L0, L1, fd, 4.3f, frontType == "bedroom" ? floorBed : floorLiving, frontType == "bedroom" ? H.wallBed : H.wallLiving);
                if (f == 1 && v1 == 0) front.use = "kitchenette";
                if (f == 1 && v1 == 1) front.use = "studio";
                var wfh = AddWall(f, 'x', 4.3f, L0, L1); Gap(wfh, Mathf.Clamp(L1 - 1.6f, 1.0f, w - 1.2f), 0.9f, true, "inner"); wfh.col = H.wallHall;
                // rear rooms west of the shaft
                string t1, t2; Room r1, r2;
                if (f == 1) { t1 = v1 == 0 ? "bedroom" : v1 == 1 ? "kitchen" : "living"; t2 = "bath"; }
                else { t1 = "bedroom"; t2 = "bath"; }
                float dm = f == 1 && v1 == 1 ? 8.2f : 8.3f;
                r1 = Mk(t1, L0, west, 5.6f, dm, t1 == "kitchen" ? floorKit : t1 == "bedroom" ? floorBed : floorLiving, t1 == "kitchen" ? H.wallKitchen : t1 == "bedroom" ? H.wallBed : H.wallLiving);
                r2 = Mk(t2, L0, west, dm, DBack, floorBath, new Color(0.9f, 0.9f, 0.9f)); r2.wet = true;
                if (f == 1 && v1 == 2) r1.use = "kitchenette";
                var wh = AddWall(f, 'x', 5.6f, L0, west); Gap(wh, Mathf.Clamp(1.4f, L0 + 0.7f, west - 0.7f), 0.85f, true, "inner"); wh.col = H.wallHall;
                var wm = AddWall(f, 'x', dm, L0, west); Gap(wm, Mathf.Clamp((L0 + west) * 0.5f, L0 + 0.7f, west - 0.6f), 0.8f, true, "inner"); wm.col = H.wallBed;
                // the shaft: wall on its west side with a way in at the top of the upper flight (top floor) or none (first floor: the shaft is the two flights)
                var wsh = AddWall(f, 'z', west, 5.6f, DBack); wsh.col = H.wallHall;
                if (f == storeys - 1 && storeys > 2) Gap(wsh, 10.35f, 0.8f, true, "inner");
                // landing / corridor rooms (no furniture)
                var shaft = Mk("shaft", west, L1, 5.6f, DBack, floorHall, H.wallHall); shaft.noFurnish = true; shaft.use = f == 1 ? "shaft1" : "shaft2";
                clear[f].Add(new Rect(L0, 4.3f, w, 1.3f));
                if (f == 1) { voids.Add(new Rect(xb, 5.6f, xa - xb - 0.05f, 4.0f)); voidLv.Add(2); }
            }

            // ------------------------------------------------------------------------------------------ walls
            void BuildWall(Wl wl)
            {
                float y0 = Yf[wl.lv], y1 = Cf[wl.lv]; float half = wl.t * 0.5f;
                var gs = new List<(float c, float wd, bool leaf, string tag)>(wl.gaps); gs.Sort((p, q) => p.c.CompareTo(q.c));
                var mat = HomeBuilder.Surf("plaster_smooth", 2f, wl.col, 0.1f);
                void Seg(float s0, float s1, float ya, float yb)
                {
                    if (s1 - s0 < 0.005f || yb - ya < 0.005f) return;
                    if (!wl.mesh) return;
                    if (wl.ax == 'x') Bx(shell, mat, s0, s1, ya, yb, wl.at - half, wl.at + half, true); else Bx(shell, mat, wl.at - half, wl.at + half, ya, yb, s0, s1, true);
                }
                float s = wl.a;
                foreach (var gp in gs)
                {
                    float g0 = gp.c - gp.wd * 0.5f, g1 = gp.c + gp.wd * 0.5f;
                    Seg(s, g0, y0, y1);
                    if (gp.tag != "tall") Seg(g0, g1, y0 + 2.12f, y1);
                    s = g1;
                    // architrave
                    float k = 0.05f, t = wl.t + 0.02f;
                    if (wl.mesh && gp.tag != "tall")
                    {
                        if (wl.ax == 'x') { Bx(fin, trim, g0 - k, g0, y0, y0 + 2.12f, wl.at - t / 2, wl.at + t / 2); Bx(fin, trim, g1, g1 + k, y0, y0 + 2.12f, wl.at - t / 2, wl.at + t / 2); Bx(fin, trim, g0 - k, g1 + k, y0 + 2.1f, y0 + 2.15f, wl.at - t / 2, wl.at + t / 2); }
                        else { Bx(fin, trim, wl.at - t / 2, wl.at + t / 2, y0, y0 + 2.12f, g0 - k, g0); Bx(fin, trim, wl.at - t / 2, wl.at + t / 2, y0, y0 + 2.12f, g1, g1 + k); Bx(fin, trim, wl.at - t / 2, wl.at + t / 2, y0 + 2.1f, y0 + 2.15f, g0 - k, g1 + k); }
                    }
                    if (gp.leaf) MakeDoor(wl, gp);
                }
                Seg(s, wl.b, y0, y1);
            }

            void MakeDoor(Wl wl, (float c, float wd, bool leaf, string tag) gp)
            {
                bool shopfront = gp.tag == "shopfront"; string id = shopfront ? "door_steel" : "door";
                Vector3 p = wl.ax == 'x' ? P(gp.c, Yf[wl.lv], wl.at) : P(wl.at, Yf[wl.lv], gp.c);
                float yaw = wl.ax == 'x' ? 0f : 90f;
                var d = PropDoor.Make(root, id, p, yaw, gp.tag); if (d == null) return;
                home.doors.Add(new Home.DoorRec { door = d, tag = gp.tag, lv = wl.lv, tier = shopfront ? 1 : gp.tag == "back" ? 1 : -1 });
            }

            // ------------------------------------------------------------------------------------------ shell, slabs
            static List<Rect> Minus(Rect r, List<Rect> cut)
            {
                var cur = new List<Rect> { r };
                foreach (var c in cut)
                {
                    var next = new List<Rect>();
                    foreach (var a in cur)
                    {
                        if (!a.Overlaps(c)) { next.Add(a); continue; }
                        float x0 = Mathf.Max(a.xMin, c.xMin), x1 = Mathf.Min(a.xMax, c.xMax), y0 = Mathf.Max(a.yMin, c.yMin), y1 = Mathf.Min(a.yMax, c.yMax);
                        if (c.xMin > a.xMin) next.Add(Rect.MinMaxRect(a.xMin, a.yMin, c.xMin, a.yMax));
                        if (c.xMax < a.xMax) next.Add(Rect.MinMaxRect(c.xMax, a.yMin, a.xMax, a.yMax));
                        if (c.yMin > a.yMin) next.Add(Rect.MinMaxRect(x0, a.yMin, x1, c.yMin));
                        if (c.yMax < a.yMax) next.Add(Rect.MinMaxRect(x0, c.yMax, x1, a.yMax));
                    }
                    cur = next;
                }
                return cur;
            }

            void BuildShellAndSlabs(float L0, float L1)
            {
                for (int lv = 0; lv < storeys; lv++)
                {
                    float fd = Fd(lv), y0 = Yf[lv], y1 = Cf[lv];
                    var vs = new List<Rect>(); for (int i = 0; i < voids.Count; i++) if (voidLv[i] == lv) vs.Add(voids[i]);
                    var foot = new Rect(L0, fd, L1 - L0, DBack - fd);
                    foreach (var p in Minus(foot, vs)) Solid(p.xMin, p.xMax, y0 - (lv == 0 ? 0.3f : 0.35f), y0, p.yMin, p.yMax);
                    // slab underside / ceiling and floors per room
                    foreach (var r in rooms)
                    {
                        if (r.lv != lv) continue;
                        var cuts = new List<Rect>(vs); if (r.type == "rear") cuts.Add(wcRect);
                        if (r.floor != null && r.type != "wc") foreach (var pc in Minus(new Rect(r.lx0, r.d0, r.lx1 - r.lx0, r.d1 - r.d0), cuts)) Quad(shell, r.floor, pc.xMin, pc.xMax, pc.yMin, pc.yMax, y0 + 0.002f, Vector3.up);
                        if (r.type != "wc") Quad(shell, ceil, r.lx0, r.lx1, r.d0, r.d1, y1, Vector3.down);
                    }
                    // outer skins: party walls, the back wall (with its door and windows), the front
                    var wm = HomeBuilder.Surf("plaster_smooth", 2f, new Color(0.9f, 0.89f, 0.85f), 0.1f);
                    shell.Quad(wm, P(L0, y0, fd), P(L0, y1, fd), P(L0, y1, DBack), P(L0, y0, DBack), Vector3.right);
                    shell.Quad(wm, P(L1, y0, DBack), P(L1, y1, DBack), P(L1, y1, fd), P(L1, y0, fd), Vector3.left);
                }
                Solid(0f, L0, -0.3f, topH, 0f, dep); Solid(L1, w, -0.3f, topH, 0f, dep);                   // the party walls
                // back wall: skin with holes, colliders
                {
                    var wmat = HomeBuilder.Surf("plaster_smooth", 2f, new Color(0.9f, 0.89f, 0.85f), 0.1f);
                    var abs = new List<Rect>(back);
                    WallHoles(shell, wmat, x0 + L0, x0 + L1, 0f, topH, -DBack, abs, true);
                    // collider: whole back wall above the doors/windows; the holes are filled (a back door leaf blocks its own doorway when shut; windows are out of reach)
                    Solid(0f, 0.8f, -0.3f, topH, DBack, dep); Solid(1.7f, w, -0.3f, Yf[1], DBack, dep); Solid(0.8f, 1.7f, 2.12f, Yf[1], DBack, dep); Solid(0f, w, Yf[1], topH, DBack, dep);
                }
                // front: ground skin with the shop door opening; upper skins with the window holes
                {
                    var wmat = HomeBuilder.Surf("plaster_smooth", 2f, new Color(0.9f, 0.89f, 0.85f), 0.1f);
                    float dcl = doorX - x0; dcl = Mathf.Clamp(dcl, 0.9f, w - 1.0f);
                    float gl = lastG - 0.2f;
                    WallHoles(shell, wmat, x0 + L0, x0 + L1, 0f, Cf[0] + 0.0f, -Fd(0), new List<Rect> { new Rect(x0 + dcl - 0.5f, 0f, 1.0f, 2.15f) }, false);
                    // above the ground ceiling, upper-floor front skin with its windows
                    var uh = new List<Rect>(holes);
                    WallHoles(shell, wmat, x0 + L0, x0 + L1, Yf[1] - 0.35f, topH, -Fd(1), uh, false);
                    // colliders: ground front in pieces round the door, upper front solid
                    Solid(0f, dcl - 0.5f, -0.3f, Yf[1], 0f, Fd(0)); Solid(dcl + 0.5f, w, -0.3f, Yf[1], 0f, Fd(0)); Solid(dcl - 0.5f, dcl + 0.5f, 2.15f, Yf[1], 0f, Fd(0));
                    Solid(0f, w, Yf[1], topH, 0f, Fd(1));
                    // the door reveals and a threshold
                    Bx(fin, trim, dcl - 0.55f, dcl - 0.5f, 0f, 2.17f, 0.02f, 0.3f); Bx(fin, trim, dcl + 0.5f, dcl + 0.55f, 0f, 2.17f, 0.02f, 0.3f); Bx(fin, trim, dcl - 0.55f, dcl + 0.55f, 2.15f, 2.2f, 0.02f, 0.3f);
                }
                // roof: stand-on collider
                Solid(0f, w, Cf[storeys - 1], topH, 0f, dep);
                // the stairs
                Stairs(L0, L1);
            }

            void Quad(MeshKit k, Material m, float lx0, float lx1, float d0, float d1, float y, Vector3 n)
            {
                if (lx1 - lx0 < 0.005f || d1 - d0 < 0.005f) return;
                k.Quad(m, P(lx0, y, d0), P(lx1, y, d0), P(lx1, y, d1), P(lx0, y, d1), n);
            }

            // ------------------------------------------------------------------------------------------ stairs
            void Stairs(float L0, float L1)
            {
                float xa = L1 - 1.0f, xb = L1 - 2.05f;
                var tread = HomeBuilder.Surf("wood_slats_dark", 1.2f, Color.white, 0.2f);
                var riser = HomeBuilder.Flat(new Color(0.9f, 0.9f, 0.88f), 0.3f);
                // ground to first: 20 rises, 19 treads from the foot (d 9.9) to the top (d 5.15)
                float rise = (Yf[1] - Yf[0]) / 20f;
                for (int i = 1; i <= 19; i++) Bx(shell, tread, xa + 0.02f, L1, Yf[0], Yf[0] + i * rise, AF - i * 0.25f, AF - (i - 1) * 0.25f, true);
                // hand rail along the open side up to the landing
                RailBox(xa + 0.0f, 5.15f, AF, Yf[0] + 0.0f, Yf[1] + 1.0f, true);
                if (storeys > 2)
                {
                    // first to second: from d 5.6 to 9.6, 16 treads, in the strip west of the first flight
                    float r2 = (Yf[2] - Yf[1]) / 17f;
                    for (int j = 1; j <= 16; j++) Bx(shell, tread, xb, xa - 0.05f, Yf[1], Yf[1] + j * r2, 5.6f + (j - 1) * 0.25f, 5.6f + j * 0.25f, true);
                    // rail between the first-floor stairwell and the second flight, and along the second-floor void
                    Bx(fin, steel, xa - 0.07f, xa - 0.05f, Yf[1], Yf[1] + 1.0f, 5.3f, 10.5f, true);
                    Bx(fin, steel, xa - 0.07f, xa - 0.05f, Yf[2], Yf[2] + 1.0f, 5.6f, 9.6f, true);
                    Bx(fin, steel, xb - 0.02f, xb, Yf[2], Yf[2] + 1.0f, 5.6f, 9.6f, false);
                }
                else
                {
                    Bx(fin, steel, xa - 0.07f, xa - 0.05f, Yf[1], Yf[1] + 1.0f, 5.3f, 10.5f, true);
                }
                // the south end of the first-floor stairwell (the slab carries on beyond d 10.5; a guard keeps you off the edge)
                Bx(fin, steel, xa, L1, Yf[1], Yf[1] + 1.0f, 10.5f, 10.52f, true);
            }
            void RailBox(float lx, float d0, float d1, float y0, float y1, bool col)
            {
                Bx(fin, steel, lx - 0.03f, lx, Yf[1], Yf[1] + 1.0f, d0, d1, false);
            }

            // ------------------------------------------------------------------------------------------ keeping clear
            void InitClear()
            {
                foreach (var wl in walls)
                    foreach (var gp in wl.gaps)
                    {
                        Rect q = wl.ax == 'x' ? new Rect(gp.c - gp.wd / 2 - 0.05f, wl.at - 0.95f, gp.wd + 0.1f, 1.9f) : new Rect(wl.at - 0.95f, gp.c - gp.wd / 2 - 0.05f, 1.9f, gp.wd + 0.1f);
                        clear[wl.lv].Add(q);
                    }
                float L1 = w - SW, xa = L1 - 1.0f;
            }

            // ------------------------------------------------------------------------------------------ finishing
            void Finish()
            {
                var sh = shell.Build("Shell", root, false, ShadowCastingMode.Off);
                var tr = fin.Build("Trim", root, false, ShadowCastingMode.Off);
                var fu = furn.Build("Furniture", root, false, ShadowCastingMode.Off); if (fu != null) SetLayer(fu, Bld.DetailLayer);
                var de = deco.Build("Deco", root, false, ShadowCastingMode.Off); if (de != null) SetLayer(de, Bld.DetailLayer);
                var cols = new GameObject("Solids"); cols.transform.SetParent(root, false);
                foreach (var s in solids) { var bc = cols.AddComponent<BoxCollider>(); bc.center = s.c; bc.size = new Vector3(Mathf.Max(0.02f, s.s.x), Mathf.Max(0.02f, s.s.y), Mathf.Max(0.02f, s.s.z)); }
                float xa = x0 + SW, xb = x0 + w - SW;
                home.zoneRects.Add(Rect.MinMaxRect(Mathf.Min(xa, xb), -DBack, Mathf.Max(xa, xb), -0.14f));
                home.centre = P(w * 0.5f, 2.0f, dep * 0.5f); home.radius = 7.5f; home.midZ = -dep * 0.5f; home.halfD = dep * 0.5f + 0.4f;
                home.Setup(new[] { sh, tr, de }, fu);
                // record rooms for the glow / light switches
                foreach (var r in rooms) home.rooms.Add(new Home.RoomRec { type = r.type, use = r.use, lv = r.lv, centre = P(r.Cx, Yf[r.lv], r.Cd), u0 = r.lx0, u1 = r.lx1, d0 = r.d0, d1 = r.d1 });
            }
            static void SetLayer(GameObject g, int l) { g.layer = l; foreach (Transform t in g.transform) SetLayer(t.gameObject, l); }
        }
    }
}
