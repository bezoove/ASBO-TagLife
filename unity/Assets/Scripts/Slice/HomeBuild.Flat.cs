using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Flat mode for the home builder: one level, any size, its front door in the wall onto the corridor (d = 0), the far
    /// wall (d = D) outside with windows, and maybe one side wall outside too. The plan is a hall spine from the front door
    /// with rooms off both sides (the inner side gets the bathroom and a kitchen or a store, the outer side the bedrooms)
    /// and a living room right across the far end; which side, how wide, how many bedrooms, whether the kitchen is its own
    /// room or part of the living room, all vary flat to flat. Then the usual furnishing for whoever lives there.
    /// The caller places the builder's frame: P(u, y, d) = (u, y, -d) under the floor's own transform.
    /// </summary>
    public partial class HomeBuilder
    {
        public class FlatSpec
        {
            /// <summary>Which side wall is outside ('L' at u = 0, 'R' at u = W, or none).</summary>
            public char extSide;
            /// <summary>Windows on the far wall and the outside side wall: (along0, along1, y0, y1), along = u on the far wall, d on a side.</summary>
            public List<Vector4> winsB = new List<Vector4>(), winsS = new List<Vector4>();
            /// <summary>The corridor face of the walls (painted block).</summary>
            public Material outside;
            /// <summary>Side walls nobody sees from the other side (another flat or the lift shaft): no outside skin.</summary>
            public bool bareL, bareR;
            /// <summary>Out: where the front door ended up (u).</summary>
            public float doorU;
            /// <summary>The front door's leaf faces this tag ("front").</summary>
            public bool ground;
            // ---- one storey of a two-storey terraced house: the front (d = 0) is outside too, a stair up the hall
            public bool house, upper;
            /// <summary>Front windows (along u, y0, y1 as the far wall's), and the front and back doors (u centres) on the ground floor.</summary>
            public List<Vector4> winsF = new List<Vector4>(); public List<float> doorsF = new List<float>(), doorsB = new List<float>();
            /// <summary>Ground floor: where the ceiling stays open over the stair (the floor above's void).</summary>
            public Rect ceilVoid;
            /// <summary>Floor to floor (for the stair).</summary>
            public float storeyH = 2.9f;
        }
        public FlatSpec fs; string flatVariant = "flat";

        // ------------------------------------------------------------------ the plan
        /// <summary>One storey of a terraced house: the hall from the front door with the stair up the party wall, the front room beside it and the
        /// kitchen across the back; upstairs the front bedroom across the front, the landing round the stairwell, the bathroom, the back bedroom.</summary>
        void HouseLayout()
        {
            const int L = 0;
            float du = fs.doorU; bool left = du < W * 0.5f;
            float h0 = left ? U0 : U1 - 1.9f, h1 = h0 + 1.9f, s0 = left ? U0 : U1 - 0.9f, s1 = s0 + 0.9f, wallU = left ? h1 : h0;
            float run = 15 * 0.25f, sd0 = D0 + 1.2f, sd1 = sd0 + run, pass = left ? (s1 + h1) * 0.5f : (h0 + s0) * 0.5f;
            if (!fs.upper)
            {
                float dh = Mathf.Max(sd1 + 0.25f, D1 - 3.1f);
                AddRoom(L, "hall", h0, h1, D0, dh);
                AddRoom(L, "lounge", left ? h1 : U0, left ? U1 : h0, D0, dh);
                var kit = AddRoom(L, "kitchen", U0, U1, dh, D1); kit.use = "diner";
                var w1 = AddWall(L, 'd', wallU, D0, dh); Gap(w1, D0 + 0.75f, 0.85f);
                var w2 = AddWall(L, 'u', dh, U0, U1); Gap(w2, pass, 0.82f);
                if (Chance(0.4)) Gap(w2, left ? (h1 + U1) * 0.5f : (U0 + h0) * 0.5f, 1.2f, false, "tall");
                blocks[0].Add(new Rect(s0, sd0, s1 - s0, run));
                fs.ceilVoid = new Rect(s0, sd0 + 0.6f, s1 - s0, sd1 - sd0 - 0.6f);
            }
            else
            {
                float fz = sd0 + 0.6f, dh2 = sd1 + 1.1f;
                AddRoom(L, "bed", U0, U1, D0, fz);
                var land = AddRoom(L, "landing", h0, h1, fz, dh2); land.use = "stairtop";
                AddRoom(L, "bath", left ? h1 : U0, left ? U1 : h0, fz, dh2);
                AddRoom(L, "bed", U0, U1, dh2, D1);
                var wf = AddWall(L, 'u', fz, U0, U1); Gap(wf, pass, 0.8f);
                var wb = AddWall(L, 'd', wallU, fz, dh2); Gap(wb, (fz + dh2) * 0.5f, 0.78f);
                var wk = AddWall(L, 'u', dh2, U0, U1); Gap(wk, pass, 0.8f);
                voids.Add(new Void { lv = 0, r = new Rect(s0, fz, s1 - s0, sd1 - fz) });
            }
            // the party walls
            var pl = AddWall(L, 'd', U0 * 0.5f, 0f, D, U0); pl.bare = true;
            var pr = AddWall(L, 'd', W - (W - U1) * 0.5f, 0f, D, W - U1); pr.bare = true;
            stairS0 = s0; stairS1 = s1; stairD0 = sd0; stairD1 = sd1;
            flatVariant = "house-" + (fs.upper ? "up" : "down") + (left ? "-l" : "-r");
        }
        float stairS0, stairS1, stairD0, stairD1;

        /// <summary>The stair (ground floor builds it, up to the floor above), or the guard round the stairwell (upstairs).</summary>
        void HouseStair()
        {
            if (!fs.upper)
            {
                var tread = Surf(H.floorHall.StartsWith("carpet") ? "carpet_hex" : "wood_slats_dark", 1.2f, H.floorHall.StartsWith("carpet") ? H.carpet : Color.white, 0.2f);
                const int n = 16; float rise = fs.storeyH / n;
                for (int i = 1; i < n; i++) Bx(shell, tread, stairS0, stairS1, Y[0], Y[0] + i * rise, stairD0 + (i - 1) * 0.25f, stairD0 + i * 0.25f, true);
                Rail(stairS1, stairD0, stairD1, Y[0], Y[0] + fs.storeyH, 'd');
            }
            else
            {
                var gm = Flat(Shade(H.trim, 0.95f), 0.4f); float fz = stairD0 + 0.6f;
                Bx(fin, gm, stairS1 - 0.03f, stairS1 + 0.03f, Y[0] + 0.9f, Y[0] + 0.95f, fz, stairD1, false);
                { Vector3 ga = P(stairS1 - 0.03f, Y[0], fz), gb = P(stairS1 + 0.03f, Y[0] + 1.0f, stairD1); solids.Add(((ga + gb) * 0.5f, Vector3.Max(ga, gb) - Vector3.Min(ga, gb), 0f)); }
                for (float d = fz + 0.1f; d < stairD1; d += 0.12f) Bx(fin, gm, stairS1 - 0.015f, stairS1 + 0.015f, Y[0], Y[0] + 0.9f, d, d + 0.03f, false);
            }
        }

        void FlatLayout()
        {
            const int L = 0;
            float Wi = U1 - U0, Di = D1 - D0;
            char ext = fs.extSide != '\0' ? fs.extSide : (rng.Next(2) == 0 ? 'L' : 'R');
            bool intLeft = ext == 'R';                          // the inner column sits on the side away from the outside wall
            float hallW = Wi < 5.0f ? 1.0f : 1.1f;
            float endD = Mathf.Clamp(Di * R(0.38f, 0.48f), 3.0f, 4.6f);
            if (Di - endD < 2.6f) endD = Mathf.Max(2.6f, Di - 2.6f);
            float dh = D1 - endD, Lh = dh - D0;
            // inner column width
            float a;
            if (Wi < 5.2f) a = 0f;                                              // studio-ish: hall against the inner wall
            else if (Wi < 6.6f) a = Mathf.Clamp(Wi - hallW - 2.6f, 1.75f, 2.1f);
            else a = Mathf.Clamp(Wi * R(0.24f, 0.32f), 1.9f, 2.9f);
            if (Wi - a - hallW < 2.3f) a = Mathf.Max(0f, Wi - hallW - 2.3f); if (a > 0f && a < 1.6f) a = 0f;
            float e = Wi - a - hallW;
            // columns in u
            float iu0, iu1, hu0, hu1, eu0, eu1;
            if (intLeft) { iu0 = U0; iu1 = U0 + a; hu0 = iu1; hu1 = hu0 + hallW; eu0 = hu1; eu1 = U1; }
            else { eu0 = U0; eu1 = U0 + e; hu0 = eu1; hu1 = hu0 + hallW; iu0 = hu1; iu1 = U1; }
            fs.doorU = (hu0 + hu1) * 0.5f;
            var hall = AddRoom(L, "hall", hu0, hu1, D0, dh);
            bool kitchenDone = false, bathDone = false;
            var wInner = a > 0f ? AddWall(L, 'd', intLeft ? hu0 : hu1, D0, dh) : null;
            var wOuter = AddWall(L, 'd', intLeft ? hu1 : hu0, D0, dh);
            // inner column: the bathroom by the door, then a kitchen or a store
            if (a > 0f)
            {
                float bd = Mathf.Min(Lh, Mathf.Clamp(R(1.9f, 2.4f), 1.8f, Lh));
                var bath = AddRoom(L, "bath", iu0, iu1, D0, D0 + bd); bathDone = true;
                Gap(wInner, Mathf.Clamp(D0 + bd * 0.5f, D0 + 0.5f, D0 + bd - 0.45f), 0.78f);
                float rest = Lh - bd;
                if (rest >= 2.3f && a >= 2.0f && Chance(0.7))
                {
                    var kit = AddRoom(L, "kitchen", iu0, iu1, D0 + bd, dh); kitchenDone = true;
                    AddWall(L, 'u', D0 + bd, iu0, iu1); Gap(wInner, Mathf.Clamp(D0 + bd + rest * 0.5f, D0 + bd + 0.5f, dh - 0.5f), 0.82f);
                }
                else if (rest >= 0.9f)
                {
                    var cup = AddRoom(L, rest >= 1.6f ? "utility" : "cupboard", iu0, iu1, D0 + bd, dh);
                    AddWall(L, 'u', D0 + bd, iu0, iu1); Gap(wInner, Mathf.Clamp(D0 + bd + rest * 0.5f, D0 + bd + 0.45f, dh - 0.45f), Mathf.Min(0.75f, rest - 0.15f));
                }
                else bath.d1 = dh;
            }
            // outer column: bedrooms (and the bathroom if the inner column had no room for it)
            {
                float d = D0;
                if (!bathDone)
                {
                    float bd = Mathf.Clamp(R(1.8f, 2.2f), 1.75f, Lh - 0.9f);
                    var bath = AddRoom(L, "bath", eu0, eu1, d, d + bd); bathDone = true;
                    Gap(wOuter, d + bd * 0.5f, 0.78f); AddWall(L, 'u', d + bd, eu0, eu1); d += bd;
                }
                float rest = dh - d;
                if (rest >= 5.4f && Chance(0.75))
                {
                    float s = d + rest * R(0.45f, 0.55f);
                    AddRoom(L, "bed", eu0, eu1, d, s); AddRoom(L, "bed", eu0, eu1, s, dh);
                    AddWall(L, 'u', s, eu0, eu1);
                    Gap(wOuter, Mathf.Clamp(d + 0.6f, d + 0.5f, s - 0.5f), 0.82f); Gap(wOuter, Mathf.Clamp(dh - 0.6f, s + 0.5f, dh - 0.5f), 0.82f);
                }
                else if (rest >= 2.4f)
                {
                    // one bedroom, or (no kitchen yet and a short run) the kitchen here
                    bool kitHere = !kitchenDone && rest < 3.0f && e >= 2.2f && Chance(0.35);
                    AddRoom(L, kitHere ? "kitchen" : "bed", eu0, eu1, d, dh); if (kitHere) kitchenDone = true;
                    Gap(wOuter, Mathf.Clamp(d + rest * 0.5f, d + 0.5f, dh - 0.5f), 0.82f);
                }
                else if (rest > 0.6f)
                {
                    AddRoom(L, "cupboard", eu0, eu1, d, dh); Gap(wOuter, d + rest * 0.5f, Mathf.Min(0.75f, rest - 0.15f));
                }
            }
            // the far end: living room right across, with the kitchen in it or split off the inner side
            var wEnd = AddWall(L, 'u', dh, U0, U1);
            bool split = !kitchenDone && Wi >= 7.4f && Chance(0.55);
            if (split)
            {
                float su = intLeft ? hu1 : hu0;   // the kitchen takes the inner column and the hall's width
                var kit = intLeft ? AddRoom(L, "kitchen", U0, su, dh, D1) : AddRoom(L, "kitchen", su, U1, dh, D1);
                var liv = intLeft ? AddRoom(L, "lounge", su, U1, dh, D1) : AddRoom(L, "lounge", U0, su, dh, D1);
                Gap(wEnd, fs.doorU, 0.82f, Chance(0.6));
                var wKL = AddWall(L, 'd', su, dh, D1); Gap(wKL, dh + Mathf.Min(1.0f, (D1 - dh) * 0.4f), 0.85f, Chance(0.4), Chance(0.5) ? "tall" : "");
                kitchenDone = true;
            }
            else
            {
                var end = AddRoom(L, kitchenDone ? "lounge" : "kitchen", U0, U1, dh, D1);
                if (!kitchenDone) end.role = "upstairsKitchen";
                Gap(wEnd, fs.doorU, 0.84f, Chance(0.75));
            }
            // the perimeter: the corridor wall with the front door, the inner side walls
            var wf = AddWall(L, 'u', D0 * 0.5f, 0f, W, D0); Gap(wf, fs.doorU, 0.92f, true, "front");
            if (fs.extSide != 'L') { var wl = AddWall(L, 'd', U0 * 0.5f, 0f, D, U0); wl.bare = fs.bareL; }
            if (fs.extSide != 'R') { var wr = AddWall(L, 'd', W - (W - U1) * 0.5f, 0f, D, W - U1); wr.bare = fs.bareR; }
            flatVariant = $"flat-a{a:0.0}-{(intLeft ? "il" : "ir")}-{(split ? "split" : "end")}";
        }

        // ------------------------------------------------------------------ floor, ceiling
        void FlatSlabs()
        {
            // the slab itself is a collider only: the flat below's ceilings are its underside (round the stairwell in a house)
            foreach (var q in Cut(new Rect(0f, 0f, W, D), 0)) solids.Add((P(q.center.x, Y[0] - 0.13f, q.center.y), new Vector3(q.width, 0.26f, q.height), 0f));
            foreach (var r in rooms)
            {
                foreach (var q in Cut(r.Rect, 0)) Bx(fin, FloorMat(r), q.xMin, q.xMax, Y[0] - 0.012f, Y[0], q.yMin, q.yMax, false);
                var cl = new List<Rect> { r.Rect }; if (fs.ceilVoid.width > 0f) cl = RectMinus(r.Rect, fs.ceilVoid);
                foreach (var q in cl) Bx(shell, ceilM, q.xMin, q.xMax, C[0], C[0] + 0.02f, q.yMin, q.yMax, false);
            }
            if (fs.house) HouseStair();
        }

        // ------------------------------------------------------------------ outside walls with their windows
        void FlatOuter()
        {
            if (fs.house) { OuterSide('F', fs.winsF, fs.doorsF); OuterSide('B', fs.winsB, fs.doorsB); return; }
            OuterSide('B', fs.winsB);
            if (fs.extSide == 'L' || fs.extSide == 'R') OuterSide(fs.extSide, fs.winsS);
        }

        /// <summary>One outside wall: solid cells (with colliders) round its window holes, the inside face skinned per room, each window framed and glazed.</summary>
        void OuterSide(char side, List<Vector4> wsrc, List<float> doorUs = null)
        {
            bool alongU = side == 'B' || side == 'F';
            float lo = alongU ? U0 : D0, hi = alongU ? U1 : D1, y0 = Y[0], y1 = C[0];
            // the wall's thickness band across the axis
            float tA = side == 'B' ? D1 : side == 'F' ? 0f : side == 'L' ? 0f : U1, tB = side == 'B' ? D : side == 'F' ? D0 : side == 'L' ? U0 : W;
            float inner = side == 'B' ? D1 : side == 'F' ? D0 : side == 'L' ? U0 : U1;
            // partitions that meet this wall: keep a solid band round them
            var cuts = new List<float>();
            foreach (var w in walls)
            {
                if (w.lv != 0) continue;
                if (alongU && w.axis == 'd' && (side == 'F' ? w.from <= D0 + 0.05f : w.to >= D1 - 0.05f) && w.at > U0 + 0.05f && w.at < U1 - 0.05f) cuts.Add(w.at);
                if (!alongU && w.axis == 'u' && w.at > D0 + 0.05f && w.at < D1 - 0.05f && (side == 'L' ? w.from <= U0 + 0.05f : w.to >= U1 - 0.05f)) cuts.Add(w.at);
            }
            var holes = new List<Vector4>();
            foreach (var h in wsrc)
            {
                float a = Mathf.Max(h.x, lo + 0.08f), b = Mathf.Min(h.y, hi - 0.08f), ya = Mathf.Max(h.z, y0 + 0.02f), yb = Mathf.Min(h.w, y1 - 0.04f);
                if (b - a < 0.4f || yb - ya < 0.4f) continue;
                var parts = new List<Vector2> { new Vector2(a, b) };
                foreach (float c in cuts)
                {
                    var n = new List<Vector2>();
                    foreach (var p in parts) { if (c + 0.1f <= p.x || c - 0.1f >= p.y) { n.Add(p); continue; } if (c - 0.1f - p.x > 0.35f) n.Add(new Vector2(p.x, c - 0.1f)); if (p.y - (c + 0.1f) > 0.35f) n.Add(new Vector2(c + 0.1f, p.y)); }
                    parts = n;
                }
                foreach (var p in parts) holes.Add(new Vector4(p.x, p.y, ya, yb));
            }
            var doorHoles = new List<Vector4>();
            if (doorUs != null) foreach (float du in doorUs) doorHoles.Add(new Vector4(du - 0.46f, du + 0.46f, y0, y0 + 2.05f));
            var allHoles = new List<Vector4>(holes); allHoles.AddRange(doorHoles);
            // grid of cells
            var xs = new SortedSet<float> { 0f, alongU ? W : D }; var ys = new SortedSet<float> { y0, y1 };
            foreach (var h in allHoles) { xs.Add(h.x); xs.Add(h.y); ys.Add(h.z); ys.Add(h.w); }
            var X = new List<float>(xs); var Yl = new List<float>(ys);
            for (int j = 0; j < Yl.Count - 1; j++)
            {
                float ya = Yl[j], yb = Yl[j + 1]; if (yb - ya < 1e-4f) continue;
                for (int i = 0; i < X.Count - 1; i++)
                {
                    float xa = X[i], xb = X[i + 1]; if (xb - xa < 1e-4f) continue;
                    float cx = (xa + xb) * 0.5f, cy = (ya + yb) * 0.5f; bool inHole = false;
                    foreach (var h in allHoles) if (cx > h.x && cx < h.y && cy > h.z && cy < h.w) { inHole = true; break; }
                    if (inHole) continue;
                    if (alongU) Bx(shell, ceilM, xa, xb, ya, yb, tA, tB, true); else Bx(shell, ceilM, tA, tB, ya, yb, xa, xb, true);
                    if (cx < lo || cx > hi) continue;
                    var r = alongU ? RoomAt(0, cx, side == 'F' ? D0 + 0.3f : D1 - 0.3f) : RoomAt(0, side == 'L' ? U0 + 0.3f : U1 - 0.3f, cx);
                    if (r == null) continue;
                    float s0 = side == 'B' || side == 'R' ? inner - 0.03f : inner, s1 = side == 'B' || side == 'R' ? inner : inner + 0.03f;
                    OuterSkin(r, alongU ? 'u' : 'd', s0, s1, Mathf.Max(xa, lo), Mathf.Min(xb, hi), ya, yb, side == 'L' || side == 'F');
                }
            }
            // the doors: a leaf in the middle of the wall's thickness, a frame round it inside
            foreach (var h in doorHoles)
            {
                float uc = (h.x + h.y) * 0.5f, mid = (tA + tB) * 0.5f; bool front = side == 'F';
                var dleaf = PropDoor.Make(root, front ? "door_steel" : "door", P(uc, Y[0], mid) - root.localPosition, 0f, front ? "front" : "back");
                if (dleaf != null) { doors.Add(new Door { lv = 0, axis = 'u', at = mid, c = uc, leaf = dleaf, tag = front ? "front" : "back" }); home.doors.Add(new Home.DoorRec { door = dleaf, tag = front ? "front" : "back", lv = 0, tier = front ? H.doorTier : (rng.NextDouble() < 0.4 ? 0 : 1) }); }
                float din = front ? D0 : D1, k = 0.05f, t = 0.14f;
                Bx(fin, skirtM, h.x - k, h.x, h.z, h.w + 0.05f, din - t * 0.5f, din + t * 0.5f); Bx(fin, skirtM, h.y, h.y + k, h.z, h.w + 0.05f, din - t * 0.5f, din + t * 0.5f);
                Bx(fin, skirtM, h.x - k, h.y + k, h.w, h.w + 0.05f, din - t * 0.5f, din + t * 0.5f);
            }
            // the windows
            var frame = H.era == 0 ? Flat(new Color(0.22f, 0.22f, 0.24f), 0.5f) : Flat(Shade(H.trim, 0.98f), 0.5f);
            foreach (var h in holes)
            {
                float fr = 0.05f, t = 0.04f, mid = (tA + tB) * 0.5f;
                void Bar(float a0, float a1, float b0, float b1, Material m, MeshKit k)
                {
                    if (alongU) Bx(k, m, a0, a1, b0, b1, mid - t / 2, mid + t / 2); else Bx(k, m, mid - t / 2, mid + t / 2, b0, b1, a0, a1);
                }
                Bar(h.x, h.y, h.z, h.z + fr, frame, fin); Bar(h.x, h.y, h.w - fr, h.w, frame, fin); Bar(h.x, h.x + fr, h.z, h.w, frame, fin); Bar(h.y - fr, h.y, h.z, h.w, frame, fin);
                int mull = Mathf.Max(0, Mathf.RoundToInt((h.y - h.x) / 1.1f) - 1);
                for (int m = 1; m <= mull; m++) { float c = Mathf.Lerp(h.x, h.y, m / (float)(mull + 1)); Bar(c - 0.025f, c + 0.025f, h.z, h.w, frame, fin); }
                if (h.z > y0 + 0.4f && h.w - h.z > 0.9f) Bar(h.x, h.y, h.w - 0.45f, h.w - 0.41f, frame, fin);            // the fanlight transom
                Bar(h.x + fr, h.y - fr, h.z + fr, h.w - fr, glassM, glass);
                Vector3 c0 = alongU ? P((h.x + h.y) * 0.5f, (h.z + h.w) * 0.5f, mid) : P(mid, (h.z + h.w) * 0.5f, (h.x + h.y) * 0.5f);
                solids.Add((c0, alongU ? new Vector3(h.y - h.x, h.w - h.z, 0.08f) : new Vector3(0.08f, h.w - h.z, h.y - h.x), 0f));
                // sill board inside (not under a door to a balcony)
                if (h.z > y0 + 0.3f)
                {
                    float sa = side == 'B' ? D1 - 0.1f : side == 'F' ? D0 - 0.02f : side == 'L' ? U0 - 0.02f : U1 - 0.1f, sb = side == 'B' ? D1 + 0.02f : side == 'F' ? D0 + 0.1f : side == 'L' ? U0 + 0.1f : U1 + 0.02f;
                    var white = Flat(Shade(H.trim, 0.98f), 0.5f);
                    if (alongU) Bx(fin, white, h.x - 0.03f, h.y + 0.03f, h.z - 0.005f, h.z + 0.02f, sa, sb); else Bx(fin, white, sa, sb, h.z - 0.005f, h.z + 0.02f, h.x - 0.03f, h.y + 0.03f);
                }
                wins.Add(new Win { lv = 0, side = side, front = false, u0 = h.x, u1 = h.y, sill = h.z - y0, head = h.w - y0 });
            }
        }
        void OuterSkin(Room r, char axis, float s0, float s1, float a, float b, float ya, float yb, bool left)
        {
            if (b - a < 0.002f) return;
            bool wet = r.type == "bath" || r.type == "wc"; var col = WallColour(r);
            if (wet && yb <= Y[0] + 1.5f + 0.001f) Piece(Surf(H.wallBath, 0.6f, Color.white, 0.5f), axis, s0, s1, a, b, ya, yb);
            else if (wet && ya < Y[0] + 1.5f) { Piece(Surf(H.wallBath, 0.6f, Color.white, 0.5f), axis, s0, s1, a, b, ya, Y[0] + 1.5f); Piece(Surf("plaster_smooth", 1.5f, Shade(col, 0.95f)), axis, s0, s1, a, b, Y[0] + 1.5f, yb); }
            else Piece(Surf("plaster_smooth", 1.5f, col), axis, s0, s1, a, b, ya, yb);
            if (ya <= Y[0] + 0.01f && !wet)
            {
                bool neg = axis == 'u' ? true : !left; // skirting stands proud towards the room
                if (axis == 'u') { if (left) Piece(skirtM, 'u', s0, s1 + 0.012f, a, b, ya, ya + 0.1f); else Piece(skirtM, 'u', s0 - 0.012f, s1, a, b, ya, ya + 0.1f); }
                else Piece(skirtM, 'd', left ? s0 : s0 - 0.012f, left ? s1 + 0.012f : s1, a, b, ya, ya + 0.1f);
            }
        }
    }
}
