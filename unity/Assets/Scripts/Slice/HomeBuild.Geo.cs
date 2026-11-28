using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    public partial class HomeBuilder
    {
        // ------------------------------------------------------------------ materials
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        public static void ResetMats() { mats.Clear(); }
        /// <summary>A scanned surface tinted, cached, at a given tile size in metres; falls back to a flat colour if the surface is missing.</summary>
        public static Material Surf(string name, float tile, Color tint, float smooth = 0.2f)
        {
            string key = name + "|" + tile + "|" + tint.ToString();
            if (mats.TryGetValue(key, out var m)) return m;
            m = (name != null ? Surfaces.Mat(name, tile, tint) : null) ?? Gfx.Mat(tint, null, smooth);
            MeshKit.Tile[m] = tile; mats[key] = m; return m;
        }
        public static Material Flat(Color c, float smooth = 0.3f)
        {
            string key = "flat|" + c.ToString() + smooth; if (mats.TryGetValue(key, out var m)) return m;
            m = Gfx.Mat(c, null, smooth); MeshKit.Tile[m] = 1f; mats[key] = m; return m;
        }
        public static Color Shade(Color c, float f) => new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), 1f);

        Material ceilM, glassM, skirtM;

        // ------------------------------------------------------------------ build
        public Home Build(Transform grp)
        {
            CotMats.Init();
            parent = grp; if (flat) { if (fs != null && fs.house) HouseLayout(); else FlatLayout(); } else Layout(); Assign();
            ceilM = Flat(new Color(0.92f, 0.91f, 0.88f), 0.1f); glassM = CotMats.glassClear; skirtM = Flat(Shade(H.trim, 0.97f), 0.4f);
            var go = new GameObject(name); root = go.transform; root.SetParent(grp, false);
            home = go.AddComponent<Home>(); home.H = H; home.name = name;
            if (flat) { FlatSlabs(); Walls(); FlatOuter(); }
            else { Slabs(); Walls(); Stairs(); Guards(); Liners(); Facings(); }
            Furnish();
            Finish();
            return home;
        }

        // ------------------------------------------------------------------ floors and ceilings
        static List<Rect> RectMinus(Rect r, Rect v)
        {
            var o = new List<Rect>();
            if (!r.Overlaps(v)) { o.Add(r); return o; }
            float xa = Mathf.Max(r.xMin, v.xMin), xb = Mathf.Min(r.xMax, v.xMax), ya = Mathf.Max(r.yMin, v.yMin), yb = Mathf.Min(r.yMax, v.yMax);
            if (v.yMin - r.yMin > 0.001f) o.Add(Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, ya));
            if (r.yMax - v.yMax > 0.001f) o.Add(Rect.MinMaxRect(r.xMin, yb, r.xMax, r.yMax));
            if (v.xMin - r.xMin > 0.001f) o.Add(Rect.MinMaxRect(r.xMin, ya, xa, yb));
            if (r.xMax - v.xMax > 0.001f) o.Add(Rect.MinMaxRect(xb, ya, r.xMax, yb));
            return o;
        }
        List<Rect> Cut(Rect r, int lv) { var l = new List<Rect> { r }; foreach (var v in voids) if (v.lv == lv) { var n = new List<Rect>(); foreach (var q in l) n.AddRange(RectMinus(q, v.r)); l = n; } return l; }

        void Slabs()
        {
            for (int lv = 0; lv < 3; lv++)
            {
                float top = Y[lv];
                foreach (var q in Cut(new Rect(U0, D0, U1 - U0, D1 - D0), lv))
                    Bx(shell, ceilM, q.xMin, q.xMax, lv == 0 ? 0f : top - 0.2f, top - 0.012f, q.yMin, q.yMax, true);
            }
            Bx(shell, ceilM, U0, U1, C[2], C[2] + 0.2f, D0, D1, false);
            // finishes
            foreach (var r in rooms)
            {
                var m = FloorMat(r);
                foreach (var q in Cut(r.Rect, r.lv))
                    Bx(fin, m, q.xMin, q.xMax, Y[r.lv] - 0.012f, Y[r.lv], q.yMin, q.yMax, false);
            }
            // the bits no room owns (stair blocks keep the hall's floor under them; slivers get plain boards)
            foreach (var b in blocksList()) { }
        }
        IEnumerable<int> blocksList() { yield break; }

        public Material FloorMat(Room r)
        {
            string n; Color tint = Color.white; float tile = 1.4f;
            switch (r.type)
            {
                case "lounge": case "dining": case "den": n = H.floorLiving; tint = n.StartsWith("carpet") ? H.carpet : n.StartsWith("wood") ? new Color(0.9f, 0.86f, 0.8f) : Color.white; break;
                case "bed": n = H.floorBed; tint = n.StartsWith("carpet") ? Color.Lerp(H.carpet, H.bedding, 0.25f) : Color.white; break;
                case "kitchen": case "utility": n = H.floorKitchen; tile = 1.0f; break;
                case "bath": case "wc": n = H.floorBath; tile = 1.0f; break;
                case "garage": n = "concrete_floor"; tint = new Color(0.8f, 0.8f, 0.78f); tile = 2f; break;
                default: n = H.floorHall; tint = n.StartsWith("carpet") ? H.carpet : Color.white; break;
            }
            if (r.type == "garage" && r.use == "converted") { n = H.floorLiving; tint = n.StartsWith("carpet") ? H.carpet : Color.white; tile = 1.4f; }
            return Surf(n, tile, tint, 0.15f);
        }

        // ------------------------------------------------------------------ wall colours
        Color WallColour(Room r)
        {
            if (r == null) return new Color(0.9f, 0.9f, 0.88f);
            switch (r.type)
            {
                case "lounge": case "dining": case "den": return H.wallLiving;
                case "bed": return H.wallBed;
                case "kitchen": case "utility": return H.wallKitchen;
                case "garage": return r.use == "converted" ? H.wallLiving : new Color(0.72f, 0.72f, 0.7f);
                default: return H.wallHall;
            }
        }
        /// <summary>Wall surface for one side of a wall piece between y0 and y1 (so a dado or bathroom tiling can split it).</summary>
        void Skin(Room r, int lv, char axis, float at, float a, float b, float side, float thickHalf, float y0, float y1, bool colliderless = true)
        {
            // side = -1 (towards smaller coordinate of the constant axis) or +1; the skin sits just outside the wall's half thickness
            float s0 = side < 0 ? at - thickHalf - 0.03f : at + thickHalf, s1 = side < 0 ? at - thickHalf : at + thickHalf + 0.03f;
            bool wet = r != null && (r.type == "bath" || r.type == "wc");
            var col = WallColour(r);
            if (wet)
            {
                float split = Mathf.Min(y1, y0 + 1.5f);
                Piece(Surf(H.wallBath, 0.6f, Color.white, 0.5f), axis, s0, s1, a, b, y0, split);
                if (y1 > split) Piece(Surf("plaster_smooth", 1.5f, Shade(col, 0.95f)), axis, s0, s1, a, b, split, y1);
            }
            else if (H.era <= 1 && r != null && r.type != "garage" && r.type != "kitchen" && y1 - y0 > 1.8f)
            {
                float split = y0 + 0.95f; var dado = Shade(col, 0.72f);
                Piece(Surf("plaster_smooth", 1.5f, dado), axis, s0, s1, a, b, y0, split);
                Piece(Flat(Shade(H.trim, 0.9f), 0.4f), axis, s0 - (side < 0 ? 0.008f : 0f), s1 + (side < 0 ? 0f : 0.008f), a, b, split - 0.025f, split + 0.025f);
                Piece(Surf("plaster_smooth", 1.5f, col), axis, s0, s1, a, b, split, y1);
            }
            else Piece(Surf("plaster_smooth", 1.5f, col), axis, s0, s1, a, b, y0, y1);
            // skirting
            if (y0 < Y[lv] + 0.01f && y1 - y0 > 1.0f && !wet)
                Piece(skirtM, axis, side < 0 ? s0 - 0.012f : s0, side < 0 ? s1 : s1 + 0.012f, a, b, y0, y0 + 0.1f);
        }
        void Piece(Material m, char axis, float s0, float s1, float a, float b, float y0, float y1)
        {
            if (b - a < 0.001f || y1 - y0 < 0.001f) return;
            if (axis == 'u') Bx(shell, m, a, b, y0, y1, s0, s1);
            else Bx(shell, m, s0, s1, y0, y1, a, b);
        }

        // ------------------------------------------------------------------ partitions
        void Walls()
        {
            foreach (var w in walls)
            {
                float yb = Y[w.lv], yt = C[w.lv], half = w.thick * 0.5f;
                var gs = new List<(float c, float w, bool leaf, string tag)>(w.gaps); gs.Sort((p, q) => p.c.CompareTo(q.c));
                float cur = w.from;
                foreach (var g in gs)
                {
                    float g0 = g.c - g.w * 0.5f, g1 = g.c + g.w * 0.5f;
                    WallPiece(w, cur, g0, yb, yt, half);
                    // lintel over the gap
                    if (g.tag != "tall") WallPiece(w, g0, g1, yb + 2.05f, yt, half);
                    cur = g1;
                    if (g.leaf) MakeDoor(w, g);
                }
                WallPiece(w, cur, w.to, yb, yt, half);
            }
        }

        void WallPiece(Wall w, float a, float b, float y0, float y1, float half)
        {
            if (b - a < 0.005f || y1 - y0 < 0.005f) return;
            float mid = (a + b) * 0.5f;
            Room ra, rb;
            if (w.axis == 'u') { ra = RoomAt(w.lv, mid, w.at - half - 0.2f); rb = RoomAt(w.lv, mid, w.at + half + 0.2f); }
            else { ra = RoomAt(w.lv, w.at - half - 0.2f, mid); rb = RoomAt(w.lv, w.at + half + 0.2f, mid); }
            // solid core (the collider), then a coloured skin each side
            if (w.axis == 'u') Bx(shell, ceilM, a, b, y0, y1, w.at - half, w.at + half, true);
            else Bx(shell, ceilM, w.at - half, w.at + half, y0, y1, a, b, true);
            if (ra != null) Skin(ra, w.lv, w.axis, w.at, a, b, -1f, half, y0, y1);
            else ExteriorSkin(w, a, b, y0, y1, half, -1f);
            if (rb != null) Skin(rb, w.lv, w.axis, w.at, a, b, +1f, half, y0, y1);
            else ExteriorSkin(w, a, b, y0, y1, half, +1f);
        }
        void ExteriorSkin(Wall w, float a, float b, float y0, float y1, float half, float side)
        {
            if (w.bare) return;
            float s0 = side < 0 ? w.at - half - 0.03f : w.at + half, s1 = side < 0 ? w.at - half : w.at + half + 0.03f;
            Piece(flat && fs != null && fs.outside != null ? fs.outside : CouncilEstate.M.white, w.axis, s0, s1, a, b, y0, y1);
        }

        void MakeDoor(Wall w, (float c, float w, bool leaf, string tag) g)
        {
            bool front = g.tag == "front"; string id = front ? "door_steel" : "door";
            Vector3 p = w.axis == 'u' ? P(g.c, Y[w.lv], w.at) : P(w.at, Y[w.lv], g.c);
            float yaw = w.axis == 'u' ? 0f : 90f;
            var d = PropDoor.Make(root, id, p - root.localPosition, yaw, g.tag);
            if (d == null) return;
            var dd = new Door { lv = w.lv, axis = w.axis, at = w.at, c = g.c, leaf = d, tag = g.tag };
            doors.Add(dd);
            // door furniture: a frame (architrave) round the opening, in the trim colour
            float t = w.thick + 0.02f, k = 0.05f, gw = g.w;
            if (w.axis == 'u') { Bx(fin, skirtM, g.c - gw / 2 - k, g.c - gw / 2, Y[w.lv], Y[w.lv] + 2.07f, w.at - t / 2, w.at + t / 2); Bx(fin, skirtM, g.c + gw / 2, g.c + gw / 2 + k, Y[w.lv], Y[w.lv] + 2.07f, w.at - t / 2, w.at + t / 2); Bx(fin, skirtM, g.c - gw / 2 - k, g.c + gw / 2 + k, Y[w.lv] + 2.05f, Y[w.lv] + 2.1f, w.at - t / 2, w.at + t / 2); }
            else { Bx(fin, skirtM, w.at - t / 2, w.at + t / 2, Y[w.lv], Y[w.lv] + 2.07f, g.c - gw / 2 - k, g.c - gw / 2); Bx(fin, skirtM, w.at - t / 2, w.at + t / 2, Y[w.lv], Y[w.lv] + 2.07f, g.c + gw / 2, g.c + gw / 2 + k); Bx(fin, skirtM, w.at - t / 2, w.at + t / 2, Y[w.lv] + 2.05f, Y[w.lv] + 2.1f, g.c - gw / 2 - k, g.c + gw / 2 + k); }
            home.doors.Add(new Home.DoorRec { door = d, tag = g.tag, lv = w.lv, tier = front ? H.doorTier : (g.tag == "back" ? (rng.NextDouble() < 0.4 ? 0 : 1) : -1) });
        }

        // ------------------------------------------------------------------ stairs
        void Stairs()
        {
            var tread = Surf(H.floorHall.StartsWith("carpet") ? "carpet_hex" : "wood_slats_dark", 1.2f, H.floorHall.StartsWith("carpet") ? H.carpet : Color.white, 0.2f);
            // ground to first: up the right-hand wall, from the front to the back (u 4.3..5.2, d 0.5..3.5)
            const int n = 13;
            float rise0 = (Y[1] - Y[0]) / n, step0 = 2.5f / n;
            for (int i = 1; i <= n; i++)
                Bx(shell, tread, 4.3f, U1, Y[0], Y[0] + i * rise0, 3.5f - i * step0, 3.5f - (i - 1) * step0, true);
            // first to second: up the left-hand wall from d 1.2 to 4.2 (u 0.2..1.1)
            float rise1 = (Y[2] - Y[1]) / n, step1 = 3.0f / n;
            for (int i = 1; i <= n; i++)
                Bx(shell, tread, U0, 1.1f, Y[1], Y[1] + i * rise1, 1.2f + (i - 1) * step1, 1.2f + i * step1, true);
            // handrails on the open sides
            Rail(4.3f, 3.5f, 1.0f, Y[0], Y[1], 'd');
            Rail(1.1f, 1.2f, 4.2f, Y[1], Y[2], 'd');
        }
        void Rail(float u, float d0, float d1, float y0, float y1, char axis)
        {
            var rm = Flat(Shade(H.trim, 0.9f), 0.4f); var wood = Flat(H.era >= 2 ? new Color(0.12f, 0.12f, 0.13f) : new Color(0.35f, 0.22f, 0.12f), 0.4f);
            int n = 9;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n, d = Mathf.Lerp(d0, d1, t), y = Mathf.Lerp(y0 + 0.2f, y1, t);
                Bx(fin, rm, u - 0.02f, u + 0.02f, y, y + 0.9f, d - 0.02f, d + 0.02f);
            }
            // sloping top rail as stepped boxes
            for (int i = 0; i < 12; i++)
            {
                float t0 = i / 12f, t1 = (i + 1) / 12f;
                float y = Mathf.Lerp(y0 + 0.2f, y1, (t0 + t1) / 2f) + 0.9f;
                Bx(fin, wood, u - 0.04f, u + 0.04f, y - 0.03f, y + 0.03f, Mathf.Lerp(d0, d1, t0), Mathf.Lerp(d0, d1, t1));
            }
            // a guard on the floor above where the stairwell is open: a solid collider to stop you stepping in the gap
        }

        // guards round the stairwell voids, and the boxed-in slivers
        void Guards()
        {
            var gm = Flat(Shade(H.trim, 0.95f), 0.4f);
            // first floor: the stairwell's back edge, beside the little cupboard, is guarded
            Bx(shell, gm, 4.3f, U1, Y[1], Y[1] + 1.0f, 3.5f, 3.56f, true);
            // second floor: the void in the front-left: guards along its right and front edges, the sliver in front boxed in
            Bx(shell, gm, 1.08f, 1.14f, Y[2], Y[2] + 1.0f, 1.2f, 4.2f, true);
            Bx(shell, ceilM, U0, 1.14f, Y[2], C[2], D0, 1.2f, true);
            var rb = RoomAt(2, 2.0f, 2.0f);
            if (rb != null) Skin(rb, 2, 'u', 1.2f, U0, 1.14f, +1f, 0f, Y[2], C[2]);
        }

        // ------------------------------------------------------------------ outer wall liners (inside faces of the front, back and party walls)
        struct Hole { public float u0, u1, y0, y1; public bool win; public int kind; }
        void Liners()
        {
            // party walls: the full height of the house, solid
            for (int s = 0; s < 2; s++)
            {
                float a = s == 0 ? 0f : U1, b = s == 0 ? U0 : W;
                bool endHere = s == 0 ? endU0 : endW; int psk = 0; if (endHere) { bool minX = (s == 0) != mirror; psk = minX ? 1 : 2; }
                Bx(shell, ceilM, a, b, 0f, 7.85f, 0f, D, true, psk);
                // skins by level and room
                foreach (var r in rooms)
                {
                    bool touches = s == 0 ? Mathf.Abs(r.u0 - U0) < 0.01f : Mathf.Abs(r.u1 - U1) < 0.01f; if (!touches) continue;
                    float side = s == 0 ? 1f : -1f; float at = s == 0 ? U0 : U1;
                    // a half-thickness of 0 and the skin outside on the room side
                    SkinPlane(r, 'd', at, r.d0, r.d1, side);
                }
            }
            // front (d 0..0.12) and back (d 8.48..8.6): solid cells around the holes; an inside skin on each room
            var front = new List<Hole>
            {
                new Hole { u0 = 0.3f, u1 = 2.7f, y0 = 0f, y1 = 2.1f, kind = 1 },                       // garage door (inside face: a plain panel)
                new Hole { u0 = 3.0f, u1 = 4.3f, y0 = 0f, y1 = 2.3f, kind = 2 },                       // porch
                new Hole { u0 = 4.5f, u1 = 5.15f, y0 = 0.95f, y1 = 2.1f, win = true },
            };
            var back = new List<Hole>
            {
                new Hole { u0 = 0.4f, u1 = 1.8f, y0 = 0.95f, y1 = 2.1f, win = true },
                new Hole { u0 = 2.2f, u1 = 3.1f, y0 = 0f, y1 = 2.05f, kind = 3 },                       // back door
                new Hole { u0 = 3.4f, u1 = 5.1f, y0 = 0.3f, y1 = 2.1f, win = true },
            };
            for (int f = 1; f <= 2; f++)
            {
                float y0 = f * CouncilEstate.FF;
                front.Add(new Hole { u0 = 0.25f, u1 = 5.15f, y0 = y0 + 0.67f, y1 = y0 + 2.1f, win = true }); front.Add(new Hole { u0 = 0.25f, u1 = 5.15f, y0 = y0 + 0.05f, y1 = y0 + 0.67f, kind = 4 });
                back.Add(new Hole { u0 = 0.25f, u1 = 5.15f, y0 = y0 + 0.67f, y1 = y0 + 2.1f, win = true }); back.Add(new Hole { u0 = 0.25f, u1 = 5.15f, y0 = y0 + 0.05f, y1 = y0 + 0.67f, kind = 4 });
            }
            OuterWall(front, true); OuterWall(back, false);
            // the porch: solid sides and roof so you can't walk through its walls, the porch door's frame
            Bx(shell, CouncilEstate.M.white, 2.99f, 3.01f, 0f, 2.3f, 0f, 1.0f, true);
            Bx(shell, CouncilEstate.M.white, 4.29f, 4.31f, 0f, 2.3f, 0f, 1.0f, true);
            solids.Add((P(3.65f, 2.45f, 0.5f), new Vector3(1.3f, 0.3f, 1.0f), 0f));
        }

        void SkinPlane(Room r, char axis, float at, float a, float b, float side)
        {
            float s0 = side > 0 ? at : at - 0.03f, s1 = side > 0 ? at + 0.03f : at;
            float y0 = Y[r.lv], y1 = C[r.lv];
            bool wet = r.type == "bath" || r.type == "wc"; var col = WallColour(r);
            if (wet) { Piece(Surf(H.wallBath, 0.6f, Color.white, 0.5f), axis, s0, s1, a, b, y0, y0 + 1.5f); Piece(Surf("plaster_smooth", 1.5f, Shade(col, 0.95f)), axis, s0, s1, a, b, y0 + 1.5f, y1); }
            else Piece(Surf("plaster_smooth", 1.5f, col), axis, s0, s1, a, b, y0, y1);
            if (!wet) Piece(skirtM, axis, side > 0 ? s0 : s0 - 0.012f, side > 0 ? s1 + 0.012f : s1, a, b, y0, y0 + 0.1f);
        }

        void OuterWall(List<Hole> holes, bool front)
        {
            var xs = new SortedSet<float> { U0, U1 }; var ys = new SortedSet<float> { 0f, 7.85f };
            foreach (var h in holes) { xs.Add(Mathf.Clamp(h.u0, U0, U1)); xs.Add(Mathf.Clamp(h.u1, U0, U1)); ys.Add(h.y0); ys.Add(h.y1); }
            var X = new List<float>(xs); var Yl = new List<float>(ys);
            float dA = front ? 0f : D1, dB = front ? D0 : D;
            for (int j = 0; j < Yl.Count - 1; j++)
            {
                float ya = Yl[j], yb = Yl[j + 1]; if (yb - ya < 1e-4f) continue;
                for (int i = 0; i < X.Count - 1; i++)
                {
                    float xa = X[i], xb = X[i + 1]; if (xb - xa < 1e-4f) continue;
                    float cx = (xa + xb) * 0.5f, cy = (ya + yb) * 0.5f; bool inHole = false;
                    foreach (var h in holes) if (cx > h.u0 && cx < h.u1 && cy > h.y0 && cy < h.y1) { inHole = true; break; }
                    if (inHole) continue;
                    Bx(shell, ceilM, xa, xb, ya, yb, dA, dB, true, front ? 32 : 16);
                    // inside skin
                    int lv = cy < Y[1] - 0.2f ? 0 : cy < Y[2] - 0.2f ? 1 : 2;
                    var r = RoomAt(lv, cx, front ? D0 + 0.3f : D1 - 0.3f);
                    float fa = front ? D0 : D1 - 0.03f, fb = front ? D0 + 0.03f : D1;
                    if (r != null && ya >= Y[lv] - 0.01f && yb <= C[lv] + 0.01f) Skin2(r, xa, xb, ya, yb, fa, fb, front);
                }
            }
            foreach (var h in holes) if (h.win || h.kind == 1 || h.kind == 3 || h.kind == 4) HoleFill(h, front);
        }
        void Skin2(Room r, float xa, float xb, float ya, float yb, float fa, float fb, bool front)
        {
            bool wet = r.type == "bath" || r.type == "wc"; var col = WallColour(r);
            if (wet && yb <= Y[r.lv] + 1.5f) Piece(Surf(H.wallBath, 0.6f, Color.white, 0.5f), 'u', fa, fb, xa, xb, ya, yb);
            else Piece(Surf("plaster_smooth", 1.5f, wet ? Shade(col, 0.95f) : col), 'u', fa, fb, xa, xb, ya, yb);
            if (ya <= Y[r.lv] + 0.01f && !wet) Piece(skirtM, 'u', front ? fa : fa - 0.012f, front ? fb + 0.012f : fb, xa, xb, ya, ya + 0.1f);
        }

        /// <summary>What goes in a hole in an outer wall: windows get a frame, glass and a sill; the garage door gets an inner panel; the back door is made by the caller.</summary>
        void HoleFill(Hole h, bool front)
        {
            float dA = front ? 0.02f : D1 - 0.0f, dB = front ? D0 : D - 0.02f; // glass lies in the wall's outer part
            if (h.win)
            {
                var white = Flat(Shade(H.trim, 0.98f), 0.5f);
                float fr = 0.05f, dd = front ? 0.05f : D - 0.05f, t = 0.04f;
                Bx(fin, white, h.u0, h.u1, h.y0, h.y0 + fr, dd - t / 2, dd + t / 2); Bx(fin, white, h.u0, h.u1, h.y1 - fr, h.y1, dd - t / 2, dd + t / 2);
                Bx(fin, white, h.u0, h.u0 + fr, h.y0, h.y1, dd - t / 2, dd + t / 2); Bx(fin, white, h.u1 - fr, h.u1, h.y0, h.y1, dd - t / 2, dd + t / 2);
                int mull = Mathf.Max(0, Mathf.RoundToInt((h.u1 - h.u0) / 1.2f) - 1);
                for (int m = 1; m <= mull; m++) { float uu = Mathf.Lerp(h.u0, h.u1, m / (float)(mull + 1)); Bx(fin, white, uu - 0.025f, uu + 0.025f, h.y0, h.y1, dd - t / 2, dd + t / 2); }
                // glass pane
                Bx(glass, glassM, h.u0 + fr, h.u1 - fr, h.y0 + fr, h.y1 - fr, dd - 0.004f, dd + 0.004f);
                solids.Add((P((h.u0 + h.u1) * 0.5f, (h.y0 + h.y1) * 0.5f, dd), new Vector3(h.u1 - h.u0, h.y1 - h.y0, 0.06f), 0f));
                // sill board inside
                Bx(fin, white, h.u0 - 0.03f, h.u1 + 0.03f, h.y0 - 0.005f, h.y0 + 0.02f, front ? D0 - 0.02f : D1 - 0.08f, front ? D0 + 0.12f : D1 + 0.02f);
                var wr = new Win { lv = h.y0 < 2.5f ? 0 : h.y0 < 5.1f ? 1 : 2, front = front, u0 = h.u0, u1 = h.u1, sill = h.y0 - Y[h.y0 < 2.5f ? 0 : h.y0 < 5.1f ? 1 : 2], head = h.y1 - Y[h.y0 < 2.5f ? 0 : h.y0 < 5.1f ? 1 : 2] };
                wins.Add(wr);
            }
            else if (h.kind == 1)
            {   // garage up-and-over: a painted panel on its inside face, solid
                Bx(shell, Flat(new Color(0.7f, 0.7f, 0.68f), 0.3f), h.u0, h.u1, 0f, h.y1, D0 + 0.001f, D0 + 0.05f, true);
            }
            else if (h.kind == 4)
            {   // the cream panel under the window band: solid, skinned inside
                float pa = front ? 0.02f : D1, pb = front ? D0 : D - 0.02f;
                Bx(shell, ceilM, h.u0, h.u1, h.y0, h.y1, pa, pb, true, front ? 32 : 16);
                int plv = h.y0 < 5.1f ? 1 : 2; var pr = RoomAt(plv, (h.u0 + h.u1) * 0.5f, front ? D0 + 0.3f : D1 - 0.3f);
                if (pr != null) Skin2(pr, h.u0, h.u1, h.y0, h.y1, front ? D0 : D1 - 0.03f, front ? D0 + 0.03f : D1, front);
            }
            else if (h.kind == 3)
            {   // back door: a frame and a leaf
                float dw = h.u1 - h.u0; float uc = (h.u0 + h.u1) * 0.5f;
                Vector3 p = P(uc, Y[0], D1 + 0.02f);
                var d = PropDoor.Make(root, "door", p - root.localPosition, 0f, "back");
                if (d != null) { doors.Add(new Door { lv = 0, axis = 'u', at = D1, c = uc, leaf = d, tag = "back" }); home.doors.Add(new Home.DoorRec { door = d, tag = "back", lv = 0, tier = rng.NextDouble() < 0.4 ? 0 : 1 }); }
            }
        }

        /// <summary>The porch door (the one in the porch's back wall at d = 1.0 is made by Walls(); this just covers the outside faces of the lower hall wall).</summary>
        void Facings() { }
    }
}
