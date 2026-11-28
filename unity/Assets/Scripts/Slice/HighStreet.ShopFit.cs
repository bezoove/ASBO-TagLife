using System;
using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    public static partial class HighStreet
    {
        sealed partial class ShopB
        {
            // ------------------------------------------------------------------------------------------ placing
            struct Spot { public string kind, label, tag; public bool hidden; public float seat; public float along; }
            class Pend { public Rect fp; public char wall; public float h; public Action draw; public List<Spot> spots = new List<Spot>(); public bool essential, ghost; public float yaw; public Vector2 contact; }
            readonly List<Pend> pend = new List<Pend>();
            Room cur; float R(float a, float b) => a + (float)rng.NextDouble() * (b - a); bool Chance(double p) => rng.NextDouble() < p;
            T Pick<T>(params T[] a) => a[rng.Next(a.Length)];

            Rect Inner(Room r)
            {
                float L0 = SW, L1 = w - SW; float pad = 0.06f;
                float a = r.lx0 <= L0 + 0.01f ? r.lx0 : r.lx0 + pad, b = r.lx1 >= L1 - 0.01f ? r.lx1 : r.lx1 - pad;
                float c = r.d0 <= Fd(r.lv) + 0.01f ? r.d0 : r.d0 + pad, d = r.d1 >= DBack - 0.01f ? r.d1 : r.d1 - pad;
                return Rect.MinMaxRect(a, c, b, d);
            }

            bool Free(Room r, Rect fp, float gap = 0.0f)
            {
                var inn = Inner(r);
                if (fp.xMin < inn.xMin - 0.005f || fp.xMax > inn.xMax + 0.005f || fp.yMin < inn.yMin - 0.005f || fp.yMax > inn.yMax + 0.005f) return false;
                var big = new Rect(fp.x - gap, fp.y - gap, fp.width + gap * 2, fp.height + gap * 2);
                foreach (var p in pend) if (p.fp.Overlaps(big)) return false;
                foreach (var c in clear[r.lv]) if (c.Overlaps(fp)) return false;
                foreach (var c in hard[r.lv]) if (c.Overlaps(fp)) return false;
                return true;
            }

            Rect WallRect(Room r, char wall, float centre, float wd, float dp)
            {
                var inn = Inner(r);
                switch (wall)
                {
                    case 'F': return new Rect(centre - wd / 2, inn.yMin, wd, dp);
                    case 'B': return new Rect(centre - wd / 2, inn.yMax - dp, wd, dp);
                    case 'L': return new Rect(inn.xMin, centre - wd / 2, dp, wd);
                    default: return new Rect(inn.xMax - dp, centre - wd / 2, dp, wd);
                }
            }

            /// <summary>Put a piece with its back to a wall (F front, B back, L west, R east). At a fixed centre along the wall if along >= 0, else the first place it fits from a random start.</summary>
            Pend Put(Room r, char wall, float wd, float dp, float h, Action draw, float along = -1f, bool essential = false, params Spot[] spots)
            {
                var inn = Inner(r); bool horiz = wall == 'F' || wall == 'B';
                float lo = (horiz ? inn.xMin : inn.yMin) + wd / 2, hi = (horiz ? inn.xMax : inn.yMax) - wd / 2;
                if (hi < lo - 0.001f) return null;
                Rect fp = default; bool ok = false;
                if (along >= 0f) { fp = WallRect(r, wall, Mathf.Clamp(along, lo, hi), wd, dp); ok = Free(r, fp); }
                else
                {
                    int n = Mathf.Max(1, Mathf.FloorToInt((hi - lo) / 0.15f)); int s0 = rng.Next(n + 1);
                    for (int i = 0; i <= n && !ok; i++) { float c = lo + ((s0 + i) % (n + 1)) * (hi - lo) / n; fp = WallRect(r, wall, c, wd, dp); ok = Free(r, fp); }
                }
                if (!ok) return null;
                return Add(r, fp, wall, h, draw, essential, spots);
            }

            /// <summary>A free-standing piece centred at (cx, cd); wide along lx.</summary>
            Pend PutAt(Room r, float cx, float cd, float wd, float dp, float h, Action draw, char facing = 'F', bool essential = false, params Spot[] spots)
            {
                var fp = new Rect(cx - wd / 2, cd - dp / 2, wd, dp);
                if (!Free(r, fp)) return null;
                return Add(r, fp, facing == 'X' ? 'F' : facing, h, draw, essential, spots, true);
            }

            Pend Add(Room r, Rect fp, char wall, float h, Action draw, bool essential, Spot[] spots, bool free = false)
            {
                var p = new Pend { fp = fp, wall = wall, h = h, draw = draw, essential = essential }; p.spots.AddRange(spots);
                switch (wall)
                {
                    case 'F': p.yaw = 180f; p.contact = free ? new Vector2(fp.center.x, fp.center.y) : new Vector2(fp.center.x, fp.yMin); break;
                    case 'B': p.yaw = 0f; p.contact = free ? new Vector2(fp.center.x, fp.center.y) : new Vector2(fp.center.x, fp.yMax); break;
                    case 'L': p.yaw = 90f; p.contact = free ? new Vector2(fp.center.x, fp.center.y) : new Vector2(fp.xMin, fp.center.y); break;
                    default: p.yaw = -90f; p.contact = free ? new Vector2(fp.center.x, fp.center.y) : new Vector2(fp.xMax, fp.center.y); break;
                }
                pend.Add(p); return p;
            }

            // the portals of a room (the middle of each doorway, a step inside), for the connectivity check
            List<Vector2> Portals(Room r)
            {
                var l = new List<Vector2>();
                foreach (var wl in walls)
                {
                    if (wl.lv != r.lv) continue;
                    foreach (var gp in wl.gaps)
                    {
                        if (wl.ax == 'x')
                        {
                            if (gp.c < r.lx0 - 0.01f || gp.c > r.lx1 + 0.01f) continue;
                            if (Mathf.Abs(wl.at - r.d0) < 0.2f) l.Add(new Vector2(gp.c, r.d0 + 0.5f)); else if (Mathf.Abs(wl.at - r.d1) < 0.2f) l.Add(new Vector2(gp.c, r.d1 - 0.5f));
                        }
                        else
                        {
                            if (gp.c < r.d0 - 0.01f || gp.c > r.d1 + 0.01f) continue;
                            if (Mathf.Abs(wl.at - r.lx0) < 0.2f) l.Add(new Vector2(r.lx0 + 0.5f, gp.c)); else if (Mathf.Abs(wl.at - r.lx1) < 0.2f) l.Add(new Vector2(r.lx1 - 0.5f, gp.c));
                        }
                    }
                }
                return l;
            }

            bool Connected(Room r, List<Vector2> pts)
            {
                if (pts.Count < 2) return true;
                var inn = Inner(r); const float cs = 0.1f, rad = 0.31f;
                int nx = Mathf.CeilToInt(inn.width / cs), nz = Mathf.CeilToInt(inn.height / cs);
                var blocked = new bool[nx * nz];
                for (int ix = 0; ix < nx; ix++)
                    for (int iz = 0; iz < nz; iz++)
                    {
                        float x = inn.xMin + (ix + 0.5f) * cs, z = inn.yMin + (iz + 0.5f) * cs;
                        bool b = x < inn.xMin + 0.22f || x > inn.xMax - 0.22f || z < inn.yMin + 0.22f || z > inn.yMax - 0.22f;
                        // walls are inside the room rect edge, items are inflated by the player's radius
                        if (!b) foreach (var p in pend) { var q = p.fp; if (x > q.xMin - rad && x < q.xMax + rad && z > q.yMin - rad && z < q.yMax + rad) { b = true; break; } }
                        if (!b) foreach (var q in hard[r.lv]) if (x > q.xMin - rad && x < q.xMax + rad && z > q.yMin - rad && z < q.yMax + rad) { b = true; break; }
                        blocked[ix * nz + iz] = b;
                    }
                (int, int) Cell(Vector2 p) => (Mathf.Clamp(Mathf.FloorToInt((p.x - inn.xMin) / cs), 0, nx - 1), Mathf.Clamp(Mathf.FloorToInt((p.y - inn.yMin) / cs), 0, nz - 1));
                var s = Cell(pts[0]); blocked[s.Item1 * nz + s.Item2] = false;
                var seen = new bool[nx * nz]; var qu = new Queue<(int, int)>(); qu.Enqueue(s); seen[s.Item1 * nz + s.Item2] = true;
                int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
                while (qu.Count > 0)
                {
                    var (cx, cz) = qu.Dequeue();
                    for (int k = 0; k < 4; k++)
                    {
                        int ax = cx + dx[k], az = cz + dz[k]; if (ax < 0 || az < 0 || ax >= nx || az >= nz) continue;
                        int id = ax * nz + az; if (seen[id] || blocked[id]) continue; seen[id] = true; qu.Enqueue((ax, az));
                    }
                }
                for (int i = 1; i < pts.Count; i++) { var c = Cell(pts[i]); int id = c.Item1 * nz + c.Item2; if (!seen[id] && !SeenNear(seen, nx, nz, c)) return false; }
                return true;
            }
            static bool SeenNear(bool[] seen, int nx, int nz, (int, int) c)
            {
                for (int a = -2; a <= 2; a++) for (int b = -2; b <= 2; b++) { int x = c.Item1 + a, z = c.Item2 + b; if (x >= 0 && z >= 0 && x < nx && z < nz && seen[x * nz + z]) return true; }
                return false;
            }

            // ------------------------------------------------------------------------------------------ commit a room's pieces
            void Commit(Room r)
            {
                var portals = Portals(r);
                // drop the least important pieces until the doors all connect
                while (!Connected(r, portals))
                {
                    int drop = -1; for (int i = pend.Count - 1; i >= 0; i--) if (!pend[i].essential) { drop = i; break; }
                    if (drop < 0) drop = pend.Count - 1; if (drop < 0) break;
                    pend.RemoveAt(drop);
                }
                foreach (var p in pend)
                {
                    float y = Yf[r.lv]; Vector3 o = P(p.contact.x, y, p.contact.y);
                    furn.Push(o, p.yaw); p.draw?.Invoke(); furn.Pop();
                    if (p.ghost) continue;
                    Solid(p.fp.xMin, p.fp.xMax, y, y + p.h, p.fp.yMin, p.fp.yMax);
                    hard[r.lv].Add(p.fp);
                    foreach (var s in p.spots)
                    {
                        // the front face of the piece, where you stand to use it
                        Vector2 f = p.wall == 'F' ? new Vector2(p.fp.center.x, p.fp.yMax) : p.wall == 'B' ? new Vector2(p.fp.center.x, p.fp.yMin) : p.wall == 'L' ? new Vector2(p.fp.xMax, p.fp.center.y) : new Vector2(p.fp.xMin, p.fp.center.y);
                        if (s.along != 0f) { Vector2 side = p.wall == 'F' || p.wall == 'B' ? new Vector2(1, 0) : new Vector2(0, 1); f += side * s.along; }
                        home.spots.Add(new Home.SpotRec { kind = s.kind, label = s.label, tag = s.tag, pos = P(f.x, y, f.y), yaw = p.yaw, room = r.idx, lv = r.lv, hidden = s.hidden, h = s.seat });
                    }
                }
                pend.Clear();
            }

            // ------------------------------------------------------------------------------------------ drawing helpers (local frame: x across, z out from the wall)
            Material Flat(Color c, float s = 0.35f) => HomeBuilder.Flat(c, s);
            Material Wood(Color c) => HomeBuilder.Surf("coated_pine", 1f, c, 0.3f);
            Material Fab(Color c) => HomeBuilder.Surf("fabric_wool", 0.6f, c, 0.1f);
            void B(Material m, float x, float y, float z, float wd, float h, float dp) { furn.BoxB(m, new Vector3(x, y, z), new Vector3(wd, h, dp)); }
            void Cy(Material m, float x, float y, float z, float rad, float h, int seg = 10) { furn.Cyl(m, new Vector3(x, y, z), rad, h, seg); }
            static readonly Color[] GoodsCols =
            {
                new Color(0.8f, 0.15f, 0.12f), new Color(0.15f, 0.35f, 0.75f), new Color(0.95f, 0.8f, 0.15f), new Color(0.2f, 0.6f, 0.3f), new Color(0.9f, 0.9f, 0.88f),
                new Color(0.9f, 0.45f, 0.1f), new Color(0.55f, 0.25f, 0.6f), new Color(0.3f, 0.3f, 0.32f), new Color(0.8f, 0.55f, 0.55f), new Color(0.15f, 0.55f, 0.6f)
            };
            Material GoodsMat(int i) => Flat(GoodsCols[i % GoodsCols.Length], 0.5f);

            /// <summary>Shelves of stock: a back, uprights, shelf boards and rows of boxes and tins.</summary>
            void ShelfUnit(float wd, float dp, float h, int fill, bool both = false, float fullness = 0.85f)
            {
                var frame = Flat(new Color(0.78f, 0.8f, 0.82f), 0.6f); var back = Flat(new Color(0.55f, 0.57f, 0.6f), 0.5f);
                B(back, 0, 0, dp * 0.5f - 0.01f, wd, h, 0.02f);
                if (both) B(back, 0, 0, 0, wd, h, 0.02f);
                B(frame, -wd / 2 + 0.02f, 0, dp / 2, 0.04f, h, dp); B(frame, wd / 2 - 0.02f, 0, dp / 2, 0.04f, h, dp);
                int rows = Mathf.Max(3, Mathf.RoundToInt(h / 0.4f));
                for (int i = 0; i <= rows; i++)
                {
                    float y = 0.12f + i * (h - 0.16f) / rows; B(frame, 0, y, dp / 2, wd - 0.04f, 0.025f, dp - 0.02f);
                    if (i == rows) break;
                    int n = Mathf.Max(3, Mathf.RoundToInt((wd - 0.1f) / 0.17f));
                    for (int j = 0; j < n; j++)
                    {
                        if (rng.NextDouble() > fullness) continue;
                        float bw = R(0.09f, 0.16f), bh = R(0.12f, Mathf.Min(0.36f, (h - 0.16f) / rows - 0.06f)), bx = -wd / 2 + 0.07f + (j + 0.5f) * (wd - 0.14f) / n;
                        B(GoodsMat(rng.Next(97) + fill * 3 + i), bx, y + 0.025f, dp * 0.5f, bw, bh, Mathf.Min(0.3f, dp - 0.12f));
                    }
                }
            }

            void CounterDraw(float len, float dp, bool glassTop, bool till, Color body)
            {
                B(Wood(body), 0, 0, dp / 2, len, 1.0f, dp);
                B(Flat(new Color(0.12f, 0.12f, 0.13f), 0.5f), 0, 1.0f, dp / 2, len + 0.06f, 0.05f, dp + 0.06f);
                B(Flat(body * 0.7f), 0, 0, 0.01f + dp - 0.02f, len - 0.1f, 0.12f, 0.03f);
                if (glassTop) { B(Flat(new Color(0.75f, 0.85f, 0.9f), 0.9f), 0, 1.05f, dp / 2, len - 0.1f, 0.3f, dp - 0.12f); }
                if (till)
                {
                    float tx = Mathf.Clamp(len * 0.3f, -len / 2 + 0.4f, len / 2 - 0.4f);
                    B(Flat(new Color(0.2f, 0.2f, 0.22f), 0.4f), tx, 1.05f, dp * 0.5f, 0.38f, 0.14f, 0.32f);
                    furn.Push(new Vector3(0, 0, 0), 0); B(Flat(new Color(0.1f, 0.1f, 0.12f), 0.6f), tx, 1.19f, dp * 0.5f + 0.06f, 0.26f, 0.2f, 0.03f); furn.Pop();
                    B(Flat(new Color(0.55f, 0.1f, 0.1f), 0.3f), tx + 0.3f, 1.05f, dp * 0.5f, 0.12f, 0.06f, 0.1f);
                }
            }

            void ChillerDraw(float wd, float dp, float h, int fill)
            {
                B(Flat(new Color(0.9f, 0.9f, 0.92f), 0.5f), 0, 0, dp / 2, wd, h, dp);
                B(Flat(new Color(0.7f, 0.82f, 0.9f), 0.9f), 0, 0.15f, dp - 0.02f, wd - 0.1f, h - 0.4f, 0.03f);
                for (int i = 0; i < 4; i++) for (int j = 0; j < Mathf.RoundToInt(wd / 0.12f) - 1; j++) B(GoodsMat(i * 3 + j + fill), -wd / 2 + 0.12f + j * 0.12f, 0.25f + i * 0.4f, dp - 0.15f, 0.08f, 0.26f, 0.08f);
                B(Flat(new Color(0.9f, 0.9f, 0.7f), 0.3f), 0, h - 0.12f, dp - 0.02f, wd - 0.1f, 0.08f, 0.02f);
            }

            void RailDraw(float wd, int fill)
            {
                B(steel, -wd / 2 + 0.04f, 0, 0.3f, 0.03f, 1.5f, 0.03f); B(steel, wd / 2 - 0.04f, 0, 0.3f, 0.03f, 1.5f, 0.03f); B(steel, 0, 1.5f, 0.3f, wd, 0.025f, 0.025f);
                B(steel, -wd / 2 + 0.04f, 0, 0.02f, 0.03f, 0.04f, 0.5f); B(steel, wd / 2 - 0.04f, 0, 0.02f, 0.03f, 0.04f, 0.5f);
                int n = Mathf.RoundToInt(wd / 0.07f);
                for (int i = 0; i < n; i++) B(Fab(Color.HSVToRGB((float)rng.NextDouble(), R(0.2f, 0.7f), R(0.3f, 0.9f))), -wd / 2 + 0.1f + i * (wd - 0.2f) / n, R(0.75f, 0.95f), 0.3f, 0.05f, R(0.5f, 0.7f), 0.4f);
            }

            void SofaDraw(float wd, Color c)
            {
                var m = Fab(c); B(m, 0, 0.1f, 0.45f, wd, 0.32f, 0.88f); B(m, 0, 0.42f, 0.1f, wd, 0.45f, 0.2f); B(m, -wd / 2 + 0.09f, 0.42f, 0.45f, 0.18f, 0.2f, 0.76f); B(m, wd / 2 - 0.09f, 0.42f, 0.45f, 0.18f, 0.2f, 0.76f);
                int n = Mathf.Max(2, Mathf.RoundToInt((wd - 0.4f) / 0.6f)); float cw = (wd - 0.4f) / n;
                for (int i = 0; i < n; i++) B(Fab(HomeBuilder.Shade(c, 1.08f)), -wd / 2 + 0.2f + cw * (i + 0.5f), 0.42f, 0.5f, cw - 0.02f, 0.1f, 0.62f);
                B(black, -wd / 2 + 0.06f, 0f, 0.1f, 0.06f, 0.1f, 0.06f); B(black, wd / 2 - 0.06f, 0f, 0.1f, 0.06f, 0.1f, 0.06f); B(black, -wd / 2 + 0.06f, 0f, 0.8f, 0.06f, 0.1f, 0.06f); B(black, wd / 2 - 0.06f, 0f, 0.8f, 0.06f, 0.1f, 0.06f);
            }

            void TableDraw(float wd, float dp, float hh, Color c)
            {
                B(Wood(c), 0, hh - 0.04f, 0, wd, 0.04f, dp);
                foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f }) B(Wood(c * 0.8f), sx * (wd / 2 - 0.05f), 0, sz * (dp / 2 - 0.05f), 0.05f, hh - 0.04f, 0.05f);
            }
            void ChairDraw(Color c) { B(Wood(c), 0, 0.42f, 0, 0.42f, 0.04f, 0.42f); B(Wood(c), 0, 0.46f, -0.19f, 0.42f, 0.45f, 0.04f); foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f }) B(Wood(c * 0.8f), sx * 0.18f, 0, sz * 0.18f, 0.04f, 0.42f, 0.04f); }
            void StoolDraw(Color c) { Cy(Flat(c), 0, 0, 0, 0.17f, 0.04f); Cy(steel, 0, 0.04f, 0, 0.02f, 0.5f, 6); B(Flat(c), 0, 0.54f, 0, 0.36f, 0.06f, 0.36f); }

            void CrateDraw(int fill, float wd = 0.5f, float dp = 0.4f, float h = 0.3f)
            {
                B(Wood(new Color(0.6f, 0.45f, 0.28f)), 0, 0, dp / 2, wd, h, dp);
                for (int i = 0; i < 6; i++) B(GoodsMat(fill + i), R(-wd / 2 + 0.08f, wd / 2 - 0.08f), h, R(0.1f, dp - 0.1f), 0.1f, 0.1f, 0.1f);
            }
            void BoxStack(int n, int seed)
            {
                var cb = Flat(new Color(0.65f, 0.5f, 0.33f), 0.15f);
                for (int i = 0; i < n; i++) B(cb, R(-0.12f, 0.12f), i * 0.3f, 0.3f, R(0.45f, 0.55f), 0.3f, R(0.38f, 0.45f));
            }
            void BinDraw() { Cy(Flat(new Color(0.2f, 0.28f, 0.2f)), 0, 0, 0.25f, 0.2f, 0.7f, 10); }

            // ------------------------------------------------------------------------------------------ rooms
            void FitRoom(Room r)
            {
                cur = r; pend.Clear();
                bool vacant = r.lv == 0 ? kind == "empty" : H.kind == Household.Kind.Vacant;
                if (r.type == "wc") { FitWc(r); Commit(r); return; }
                if (r.noFurnish) { Lights(r); return; }
                switch (r.type)
                {
                    case "shop": if (kind == "empty") BareShop(r); else Shop(r); break;
                    case "stock": StockRoom(r); break;
                    case "kitchen": if (r.lv == 0) CafeKitchen(r); else Kitchen(r, vacant); break;
                    case "rear": Lobby(r); break;
                    case "hall": Landing(r, vacant); break;
                    case "living": Living(r, vacant); break;
                    case "bedroom": Bedroom(r, vacant); break;
                    case "bath": Bath(r, vacant); break;
                }
                Commit(r); Lights(r);
                if (vacant && r.lv > 0) Squalor(r);
            }

            // a ceiling strip light and a switch by each door
            void Lights(Room r)
            {
                float y = Yf[r.lv], top = Cf[r.lv];
                bool shopfloor = r.type == "shop";
                int n = shopfloor ? Mathf.Max(2, Mathf.RoundToInt((r.lx1 - r.lx0) / 1.6f)) : 1;
                for (int i = 0; i < n; i++)
                {
                    float cx = shopfloor ? r.lx0 + (i + 0.5f) * (r.lx1 - r.lx0) / n : r.Cx;
                    if (shopfloor || r.type == "stock" || r.type == "kitchen" || r.type == "rear") Bx(deco, whiteM, cx - 0.6f, cx + 0.6f, top - 0.06f, top, r.Cd - 0.08f, r.Cd + 0.08f);
                    else { var gl = Flat(new Color(0.96f, 0.93f, 0.8f), 0.3f); Bx(deco, gl, cx - 0.14f, cx + 0.14f, top - 0.1f, top, r.Cd - 0.14f, r.Cd + 0.14f); }
                }
                // a switch beside the first doorway
                foreach (var wl in walls)
                {
                    if (wl.lv != r.lv || wl.gaps.Count == 0) continue;
                    foreach (var gp in wl.gaps)
                    {
                        if (gp.tag == "tall") continue;
                        Vector2 at; bool hit = false; float sx = 0, sd = 0; float off = gp.wd / 2 + 0.2f;
                        if (wl.ax == 'x') { if (gp.c < r.lx0 || gp.c > r.lx1) continue; if (Mathf.Abs(wl.at - r.d0) < 0.2f) { sd = r.d0 + 0.07f; hit = true; } else if (Mathf.Abs(wl.at - r.d1) < 0.2f) { sd = r.d1 - 0.07f; hit = true; } sx = Mathf.Clamp(gp.c + off, r.lx0 + 0.15f, r.lx1 - 0.15f); }
                        else { if (gp.c < r.d0 || gp.c > r.d1) continue; if (Mathf.Abs(wl.at - r.lx0) < 0.2f) { sx = r.lx0 + 0.07f; hit = true; } else if (Mathf.Abs(wl.at - r.lx1) < 0.2f) { sx = r.lx1 - 0.07f; hit = true; } sd = Mathf.Clamp(gp.c + off, r.d0 + 0.15f, r.d1 - 0.15f); }
                        if (!hit) continue; at = new Vector2(sx, sd);
                        Bx(deco, whiteM, at.x - 0.04f, at.x + 0.04f, y + 1.2f, y + 1.3f, at.y - 0.015f, at.y + 0.015f);
                        home.spots.Add(new Home.SpotRec { kind = "light", label = "Light switch", tag = "switch", pos = P(at.x, y, at.y), yaw = 0f, room = r.idx, lv = r.lv });
                        return;
                    }
                }
            }

            // ------------------------------------------------------------------------------------------ the shop floor
            void Shop(Room r)
            {
                var inn = Inner(r); float wd = inn.width;
                Color body = Pick(new Color(0.15f, 0.25f, 0.45f), new Color(0.5f, 0.12f, 0.1f), new Color(0.2f, 0.4f, 0.28f), new Color(0.3f, 0.3f, 0.32f), new Color(0.75f, 0.7f, 0.6f));
                r.wall = Pick(new Color(0.92f, 0.9f, 0.84f), new Color(0.85f, 0.88f, 0.92f), new Color(0.95f, 0.93f, 0.8f), new Color(0.9f, 0.84f, 0.84f));
                float ct = Mathf.Max(inn.xMin + 2.5f, 2.6f); float cl = inn.xMax - ct - 0.1f;   // counter span at the back right
                switch (kind)
                {
                    case "grocer":
                    case "pound":
                        Counter(r, ct, cl, body, "grocer", true);
                        for (float d = 1.3f; d < inn.yMax - 1.0f; d += 1.0f) if (Put(r, 'L', 0.95f, 0.45f, 1.9f, () => ShelfUnit(0.95f, 0.45f, 1.9f, rng.Next(50)), -1f, false, new Spot { kind = "rummage", label = "Go through the stock", tag = "stock" }) == null) break;
                        for (float d = 0.8f; d < inn.yMax - 1.3f; d += 1.0f) if (Put(r, 'R', 0.95f, 0.45f, 1.9f, () => ShelfUnit(0.95f, 0.45f, 1.9f, rng.Next(50)), -1f, false, new Spot { kind = "rummage", label = "Go through the stock", tag = "stock" }) == null) break;
                        Put(r, 'R', 1.0f, 0.75f, 1.9f, () => ChillerDraw(1.0f, 0.75f, 1.9f, rng.Next(30)), -1f, false, new Spot { kind = "fridge", label = "Look in the chiller", tag = "fridge" });
                        for (int i = 0; i < (kind == "pound" ? 3 : 2); i++) PutAt(r, R(inn.xMin + 1.6f, inn.xMax - 1.6f), R(1.5f, 3.0f), 1.0f, 0.62f, 1.4f, () => { ShelfUnit(1.0f, 0.3f, 1.4f, rng.Next(50), true); furn.Push(new Vector3(0, 0, 0.3f), 180f); ShelfUnit(1.0f, 0.3f, 1.4f, rng.Next(50), true); furn.Pop(); }, 'F', false, new Spot { kind = "rummage", label = "Go through the stock", tag = "stock" });
                        if (kind == "grocer") Put(r, 'F', 0.5f, 0.4f, 0.4f, () => CrateDraw(rng.Next(9)), -1f, false, new Spot { kind = "rummage", label = "Go through the veg crates", tag = "stock" });
                        break;
                    case "food":
                    case "bakery":
                        Counter(r, inn.xMin + 0.4f, inn.width - 0.8f, body, kind, false);
                        Put(r, 'F', 0.45f, 0.45f, 0.95f, () => StoolDraw(body), inn.xMax - 0.7f, false);
                        Put(r, 'F', 0.45f, 0.45f, 0.95f, () => StoolDraw(body), inn.xMax - 1.4f, false);
                        Put(r, 'R', 1.2f, 0.5f, 0.9f, () => { B(Wood(body), 0, 0, 0.25f, 1.2f, 0.85f, 0.5f); }, -1f, false, new Spot { kind = "rummage", label = "Look in the cupboard", tag = "cupboard" });
                        if (kind == "bakery") for (int i = 0; i < 2; i++) Put(r, 'L', 1.2f, 0.5f, 1.6f, () => ShelfUnit(1.2f, 0.5f, 1.6f, 30 + i, false, 0.5f), -1f, false, new Spot { kind = "rummage", label = "Go through the display", tag = "stock" });
                        break;
                    case "bookie":
                        Counter(r, ct, cl, body, "bookie", true);
                        for (int i = 0; i < 3; i++) Put(r, 'R', 1.2f, 0.12f, 2.2f, () => { B(black, 0, 1.3f, 0.06f, 1.2f, 0.75f, 0.1f); B(Flat(new Color(0.1f, 0.35f, 0.15f), 0.4f), 0, 1.35f, 0.115f, 1.1f, 0.65f, 0.01f); }, -1f, false);
                        for (int i = 0; i < 3; i++) Put(r, 'L', 0.65f, 0.5f, 1.5f, () => { B(black, 0, 0, 0.25f, 0.6f, 1.45f, 0.5f); B(Flat(new Color(0.2f, 0.5f, 0.9f), 0.2f), 0, 0.95f, 0.5f, 0.5f, 0.4f, 0.01f); }, -1f, false, new Spot { kind = "rummage", label = "Shake the machine", tag = "stock" });
                        PutAt(r, (inn.xMin + inn.xMax) * 0.5f, 2.4f, 1.6f, 0.4f, 1.1f, () => { B(Wood(new Color(0.3f, 0.3f, 0.3f)), 0, 0, 0, 1.5f, 1.05f, 0.35f); B(Flat(Color.white), 0, 1.05f, 0, 1.5f, 0.03f, 0.4f); });
                        break;
                    case "laundry":
                        for (int i = 0; i < 6; i++) Put(r, 'L', 0.64f, 0.66f, 0.9f, () => WasherDraw(i % 2 == 0), -1f, false, new Spot { kind = "rummage", label = "Check the machine", tag = "washer" });
                        for (int i = 0; i < 4; i++) Put(r, 'R', 0.64f, 0.7f, 1.8f, () => { WasherDraw(false); furn.Push(new Vector3(0, 0.9f, 0), 0); WasherDraw(true); furn.Pop(); }, -1f, false, new Spot { kind = "rummage", label = "Check the dryer", tag = "washer" });
                        Counter(r, ct, cl, body, "laundry", false);
                        PutAt(r, (inn.xMin + inn.xMax) * 0.5f, 2.3f, 1.6f, 0.9f, 0.85f, () => TableDraw(1.6f, 0.9f, 0.85f, new Color(0.7f, 0.7f, 0.7f)));
                        Put(r, 'F', 0.6f, 0.6f, 1.5f, () => { B(steel, 0, 0, 0.3f, 0.6f, 1.5f, 0.6f); B(black, 0, 1.0f, 0.6f, 0.4f, 0.2f, 0.01f); }, inn.xMax - 0.8f, false, new Spot { kind = "rummage", label = "Shake the change machine", tag = "till" });
                        break;
                    case "salon":
                        Counter(r, ct, cl, body, "salon", false);
                        for (int i = 0; i < 3; i++) Put(r, 'R', 0.9f, 0.9f, 1.9f, () => SalonChair(body), -1f, false);
                        Put(r, 'L', 1.6f, 0.9f, 0.9f, () => SofaDraw(1.6f, body), -1f, false, new Spot { kind = "sit", label = "Sit down", tag = "sofa", seat = 0.46f });
                        Put(r, 'L', 0.6f, 0.3f, 1.0f, () => { B(Wood(body), 0, 0, 0.15f, 0.6f, 0.6f, 0.3f); }, -1f, false, new Spot { kind = "rummage", label = "Open the drawers", tag = "drawers" });
                        break;
                    case "charity":
                        Counter(r, ct, cl, body, "charity", false);
                        for (int i = 0; i < 4; i++) PutAt(r, R(inn.xMin + 1.2f, inn.xMax - 1.2f), R(1.3f, 3.3f), 1.2f, 0.55f, 1.6f, () => RailDraw(1.2f, i), 'F', false, new Spot { kind = "rummage", label = "Go through the rail", tag = "rail" });
                        for (int i = 0; i < 3; i++) Put(r, 'L', 0.95f, 0.45f, 1.9f, () => ShelfUnit(0.95f, 0.45f, 1.9f, 60 + i, false, 0.6f), -1f, false, new Spot { kind = "rummage", label = "Go through the bric-a-brac", tag = "stock" });
                        for (int i = 0; i < 2; i++) Put(r, 'R', 1.2f, 0.55f, 1.6f, () => RailDraw(1.2f, 9 + i), -1f, false, new Spot { kind = "rummage", label = "Go through the rail", tag = "rail" });
                        Put(r, 'F', 0.7f, 0.5f, 0.55f, () => CrateDraw(11, 0.7f, 0.5f, 0.5f), -1f, false, new Spot { kind = "rummage", label = "Dig through the toy bin", tag = "stock" });
                        break;
                    case "post":
                        Counter(r, inn.xMin + 0.4f, inn.width - 2.2f, body, "post", true);
                        for (int i = 0; i < 2; i++) Put(r, 'L', 0.95f, 0.45f, 1.9f, () => ShelfUnit(0.95f, 0.45f, 1.9f, 80 + i, false, 0.7f), -1f, false, new Spot { kind = "rummage", label = "Go through the parcels", tag = "stock" });
                        Put(r, 'R', 1.2f, 0.45f, 1.0f, () => { B(Flat(new Color(0.85f, 0.1f, 0.1f), 0.5f), 0, 0, 0.225f, 1.2f, 1.0f, 0.45f); }, -1f, false);
                        break;
                    case "pharmacy":
                        Counter(r, ct, cl, body, "pharmacy", true);
                        for (int i = 0; i < 4; i++) Put(r, 'L', 0.95f, 0.4f, 1.9f, () => ShelfUnit(0.95f, 0.4f, 1.9f, 90 + i, false, 0.95f), -1f, false, new Spot { kind = "rummage", label = "Go through the shelves", tag = "stock" });
                        for (int i = 0; i < 3; i++) Put(r, 'R', 0.95f, 0.4f, 1.9f, () => ShelfUnit(0.95f, 0.4f, 1.9f, 95 + i, false, 0.95f), -1f, false, new Spot { kind = "rummage", label = "Go through the shelves", tag = "stock" });
                        Put(r, 'F', 1.2f, 0.45f, 0.48f, () => SofaDraw(1.2f, new Color(0.2f, 0.4f, 0.5f)), inn.xMax - 1.0f, false, new Spot { kind = "sit", label = "Sit down", tag = "sofa", seat = 0.46f });
                        break;
                    case "vape":
                        Counter(r, ct, cl, body, "vape", true);
                        for (int i = 0; i < 4; i++) Put(r, 'L', 0.95f, 0.3f, 1.9f, () => ShelfUnit(0.95f, 0.3f, 1.9f, 100 + i, false, 1.0f), -1f, false, new Spot { kind = "rummage", label = "Go through the displays", tag = "stock" });
                        for (int i = 0; i < 3; i++) Put(r, 'R', 1.2f, 0.55f, 1.0f, () => { CounterDraw(1.2f, 0.55f, true, false, body); }, -1f, false, new Spot { kind = "rummage", label = "Try the glass cabinet", tag = "stock" });
                        Put(r, 'F', 1.6f, 0.9f, 0.9f, () => SofaDraw(1.6f, new Color(0.15f, 0.15f, 0.18f)), inn.xMax - 1.2f, false, new Spot { kind = "sit", label = "Sit down", tag = "sofa", seat = 0.46f });
                        break;
                }
                // a shop radio on, a clock and posters are in the deco layer
                Poster(r, body);
            }

            void Counter(Room r, float xFrom, float len, Color body, string k, bool glassTop)
            {
                len = Mathf.Max(1.4f, len); float cx = xFrom + len / 2;
                var t = Put(r, 'B', len, 0.7f, 1.05f, () => CounterDraw(len, 0.7f, glassTop, true, body), cx, true,
                    new Spot { kind = "rummage", label = "Jemmy the till", tag = "till", along = Mathf.Clamp(len * 0.3f, -len / 2 + 0.4f, len / 2 - 0.4f) },
                    new Spot { kind = "rummage", label = "Reach under the counter", tag = "counter", hidden = true, along = -len * 0.25f });
                // the wall display behind the counter (nobody can get at it)
                if (t != null) { var inn = Inner(r); float xa = xFrom, xb = xFrom + len; furnWall(r, xa, xb, inn.yMax, k); }
            }
            void furnWall(Room r, float xa, float xb, float dBack, string k)
            {
                Bx(furn, Flat(new Color(0.3f, 0.3f, 0.32f), 0.4f), xa, xb, Yf[0] + 1.35f, Yf[0] + 2.2f, dBack - 0.22f, dBack - 0.02f);
                int n = Mathf.RoundToInt((xb - xa) / 0.14f);
                for (int i = 0; i < n; i++) Bx(furn, GoodsMat(i * 7 + k.Length), xa + 0.05f + i * (xb - xa - 0.1f) / n, xa + 0.05f + i * (xb - xa - 0.1f) / n + 0.1f, Yf[0] + 1.5f + (i % 3) * 0.25f, Yf[0] + 1.7f + (i % 3) * 0.25f, dBack - 0.2f, dBack - 0.06f);
            }
            void WasherDraw(bool open)
            {
                B(whiteM, 0, 0, 0.33f, 0.62f, 0.88f, 0.64f); Cy(Flat(new Color(0.12f, 0.14f, 0.18f), 0.9f), 0, 0.3f, 0.65f, 0.22f, 0.02f, 14);
                B(black, 0.18f, 0.74f, 0.65f, 0.15f, 0.08f, 0.02f);
            }
            void SalonChair(Color body)
            {
                B(Flat(new Color(0.12f, 0.12f, 0.14f), 0.5f), 0, 0, 0.45f, 0.55f, 0.08f, 0.55f); Cy(steel, 0, 0.08f, 0.45f, 0.05f, 0.35f, 8); B(Flat(body, 0.6f), 0, 0.43f, 0.45f, 0.5f, 0.1f, 0.5f); B(Flat(body, 0.6f), 0, 0.5f, 0.2f, 0.48f, 0.7f, 0.1f);
                B(Flat(new Color(0.8f, 0.9f, 0.95f), 0.95f), 0, 1.0f, 0.04f, 1.0f, 0.9f, 0.02f); B(black, 0, 0.95f, 0.02f, 1.04f, 0.04f, 0.02f);
            }
            void Poster(Room r, Color c)
            {
                var inn = Inner(r); float y = Yf[0];
                for (int i = 0; i < 3; i++)
                {
                    float x = R(inn.xMin + 0.5f, inn.xMax - 0.5f); Bx(deco, GoodsMat(rng.Next(10)), x, x + 0.5f, y + 1.7f, y + 2.2f, inn.yMin + 0.02f, inn.yMin + 0.03f);
                }
            }

            void BareShop(Room r)
            {
                var inn = Inner(r);
                r.wall = new Color(0.82f, 0.8f, 0.76f);
                // what the last tenant left: a bit of shelving, boxes, a counter shell, litter
                if (Chance(0.7)) Put(r, 'L', 1.2f, 0.45f, 1.9f, () => ShelfUnit(1.2f, 0.45f, 1.9f, rng.Next(20), false, 0.1f), -1f, false);
                if (Chance(0.5)) Put(r, 'R', 1.2f, 0.45f, 1.9f, () => ShelfUnit(1.2f, 0.45f, 1.9f, rng.Next(20), false, 0.05f), -1f, false);
                if (Chance(0.6)) Put(r, 'B', 2.0f, 0.7f, 1.05f, () => CounterDraw(2.0f, 0.7f, false, false, new Color(0.55f, 0.5f, 0.45f)), -1f, false, new Spot { kind = "rummage", label = "Look behind the counter", tag = "counter", hidden = true });
                for (int i = 0; i < 3; i++) if (Chance(0.6)) Put(r, Pick('F', 'L', 'R'), 0.55f, 0.45f, 0.9f, () => BoxStack(rng.Next(1, 4), i), -1f, false);
                if (Chance(0.4)) Put(r, 'F', 0.5f, 0.5f, 0.8f, () => { B(Fab(new Color(0.3f, 0.3f, 0.4f)), 0, 0, 0.25f, 0.5f, 0.1f, 0.5f); B(Fab(new Color(0.55f, 0.2f, 0.2f)), 0, 0.1f, 0.25f, 0.45f, 0.25f, 0.45f); }, -1f, false, new Spot { kind = "rummage", label = "Go through the sleeping bag", tag = "bag", hidden = true });
                // post and leaflets on the mat
                Bx(deco, Flat(new Color(0.9f, 0.9f, 0.85f), 0.2f), 1.0f, 1.3f, Yf[0] + 0.005f, Yf[0] + 0.008f, 0.4f, 0.55f); Bx(deco, Flat(new Color(0.85f, 0.4f, 0.4f), 0.2f), 1.4f, 1.6f, Yf[0] + 0.005f, Yf[0] + 0.008f, 0.5f, 0.65f);
                // paper on the floor: a few dusty marks
                for (int i = 0; i < 6; i++) { float x = R(inn.xMin + 0.3f, inn.xMax - 0.3f), d = R(inn.yMin + 0.3f, inn.yMax - 0.3f); Bx(deco, Flat(new Color(0.78f, 0.77f, 0.72f), 0.2f), x, x + R(0.1f, 0.3f), Yf[0] + 0.004f, Yf[0] + 0.007f, d, d + R(0.1f, 0.25f)); }
            }

            // ------------------------------------------------------------------------------------------ behind the shop
            void StockRoom(Room r)
            {
                var inn = Inner(r); r.wall = new Color(0.82f, 0.82f, 0.8f);
                bool empty = kind == "empty";
                int shelves = empty ? 1 : 3;
                for (int i = 0; i < shelves; i++) Put(r, Pick('L', 'B', 'R'), 1.0f, 0.5f, 1.9f, () => ShelfUnit(1.0f, 0.5f, 1.9f, rng.Next(99), false, empty ? 0.1f : 0.8f), -1f, false, new Spot { kind = "rummage", label = "Go through the stock", tag = "stock" });
                int stacks = empty ? 1 : 3; for (int i = 0; i < stacks; i++) Put(r, Pick('L', 'R', 'B'), 0.55f, 0.45f, 0.95f, () => BoxStack(rng.Next(2, 4), i), -1f, false, new Spot { kind = "rummage", label = "Open a box", tag = "stock" });
                if (!empty) Put(r, 'R', 0.6f, 0.6f, 0.85f, () => { Cy(steel, 0, 0, 0.3f, 0.3f, 0.85f, 10); }, -1f, false, new Spot { kind = "rummage", label = "Look in the cupboard", tag = "cupboard" });
                if (!empty && Chance(0.5)) Put(r, 'F', 0.9f, 0.7f, 0.8f, () => TableDraw(0.9f, 0.6f, 0.75f, new Color(0.6f, 0.5f, 0.35f)), -1f, false, new Spot { kind = "rummage", label = "Go through the desk drawer", tag = "drawers" });
                if (!empty && Chance(0.5)) Put(r, 'R', 0.7f, 0.5f, 1.3f, () => { B(steel, 0, 0, 0.25f, 0.7f, 1.3f, 0.5f); B(black, 0, 1.0f, 0.5f, 0.4f, 0.05f, 0.01f); }, -1f, false, new Spot { kind = "rummage", label = "Try the safe", tag = "safe", hidden = true });
            }
            void CafeKitchen(Room r)
            {
                r.wall = new Color(0.9f, 0.9f, 0.9f);
                Put(r, 'L', 1.8f, 0.8f, 0.95f, () => { B(steel, 0, 0, 0.4f, 1.8f, 0.9f, 0.8f); for (int i = 0; i < 3; i++) B(black, -0.55f + i * 0.55f, 0.9f, 0.4f, 0.4f, 0.05f, 0.5f); }, -1f, true, new Spot { kind = "rummage", label = "Check the fryers", tag = "cupboard" });
                Put(r, 'R', 1.4f, 0.7f, 0.95f, () => { B(steel, 0, 0, 0.35f, 1.4f, 0.9f, 0.7f); B(Flat(new Color(0.7f, 0.5f, 0.4f)), 0, 0.9f, 0.35f, 1.2f, 0.04f, 0.6f); }, -1f, false, new Spot { kind = "rummage", label = "Go through the drawers", tag = "drawers" });
                Put(r, 'B', 0.8f, 0.7f, 1.9f, () => { B(steel, 0, 0, 0.35f, 0.8f, 1.9f, 0.7f); B(Flat(new Color(0.7f, 0.82f, 0.9f), 0.9f), 0, 0.2f, 0.7f, 0.7f, 1.4f, 0.01f); }, -1f, false, new Spot { kind = "fridge", label = "Look in the fridge", tag = "fridge" });
                Put(r, 'F', 1.2f, 0.7f, 0.9f, () => TableDraw(1.2f, 0.7f, 0.9f, new Color(0.7f, 0.7f, 0.72f)), -1f, false, new Spot { kind = "rummage", label = "Go through the stock", tag = "stock" });
                Put(r, 'B', 0.4f, 0.4f, 0.8f, () => BinDraw(), -1f, false, new Spot { kind = "rummage", label = "Go through the bin", tag = "bin" });
            }
            void Lobby(Room r)
            {
                r.wall = new Color(0.85f, 0.85f, 0.82f);
                pend.Add(new Pend { fp = wcRect, wall = 'F', essential = true, ghost = true });
                Put(r, 'R', 0.5f, 0.5f, 0.9f, () => { Cy(Flat(new Color(0.2f, 0.4f, 0.8f)), 0, 0, 0.25f, 0.22f, 0.7f); B(steel, 0.0f, 0.7f, 0.25f, 0.04f, 0.8f, 0.04f); }, -1f, false);
                Put(r, 'F', 0.5f, 0.35f, 1.5f, () => { B(black, 0, 0, 0.17f, 0.5f, 0.05f, 0.35f); B(Wood(new Color(0.5f, 0.35f, 0.2f)), 0, 1.4f, 0.1f, 0.5f, 0.04f, 0.2f); for (int i = 0; i < 3; i++) B(Fab(GoodsCols[i + 3]), -0.15f + i * 0.15f, 0.9f, 0.1f, 0.1f, 0.5f, 0.1f); }, -1f, false, new Spot { kind = "rummage", label = "Go through the coats", tag = "coats" });
            }
            void FitWc(Room r)
            {
                float y = Yf[0]; var tile = Flat(new Color(0.9f, 0.9f, 0.88f), 0.5f);
                Quad(shell, HomeBuilder.Surf("tiles_chevron", 0.8f, Color.white, 0.4f), r.lx0, r.lx1, r.d0, r.d1, y + 0.002f, Vector3.up); Quad(shell, ceil, r.lx0, r.lx1, r.d0, r.d1, Cf[0], Vector3.down);
                // toilet against the north wall, basin on the west wall
                Put(r, 'F', 0.4f, 0.7f, 0.8f, () => { B(whiteM, 0, 0, 0.33f, 0.38f, 0.4f, 0.55f); B(whiteM, 0, 0.4f, 0.1f, 0.4f, 0.45f, 0.18f); }, -1f, true, new Spot { kind = "flush", label = "Flush", tag = "flush" }, new Spot { kind = "rummage", label = "Lift the cistern lid", tag = "cistern", hidden = true });
                Put(r, 'L', 0.45f, 0.4f, 0.9f, () => { B(whiteM, 0, 0.78f, 0.18f, 0.42f, 0.1f, 0.34f); B(whiteM, 0, 0, 0.1f, 0.12f, 0.78f, 0.12f); }, -1f, false, new Spot { kind = "tap", label = "Wash your hands", tag = "basin" });
                // the walls of the cubicle are tiled to head height on the inside
            }
            void Landing(Room r, bool vacant)
            {
                r.wall = H.wallHall;
                if (vacant) return;
                Put(r, 'L', 0.6f, 0.4f, 0.8f, () => { B(Wood(new Color(0.45f, 0.3f, 0.2f)), 0, 0, 0.2f, 0.6f, 0.75f, 0.4f); B(Flat(new Color(0.2f, 0.2f, 0.25f)), 0.0f, 0.75f, 0.2f, 0.2f, 0.06f, 0.15f); }, -1f, false, new Spot { kind = "rummage", label = "Check the phone table drawer", tag = "phone" });
                Put(r, 'F', 0.9f, 0.3f, 1.8f, () => { B(Wood(new Color(0.4f, 0.3f, 0.2f)), 0, 1.5f, 0.1f, 0.9f, 0.04f, 0.2f); for (int i = 0; i < 3; i++) B(Fab(GoodsCols[i + 5]), -0.3f + i * 0.3f, 0.7f, 0.1f, 0.12f, 0.8f, 0.1f); }, -1f, false, new Spot { kind = "rummage", label = "Go through the coats", tag = "coats" });
            }

            // ------------------------------------------------------------------------------------------ the flats
            void Living(Room r, bool vacant)
            {
                var inn = Inner(r); Household h = H;
                if (vacant) return;
                var sofaC = h.sofa;
                bool tv = true;
                Put(r, 'B', Mathf.Min(2.2f, inn.width - 0.9f), 0.9f, 0.9f, () => SofaDraw(Mathf.Min(2.2f, inn.width - 0.9f), sofaC), -1f, true, new Spot { kind = "rummage", label = "Check down the sofa", tag = "sofa" }, new Spot { kind = "sit", label = "Sit down", tag = "sofa", seat = 0.46f });
                if (tv) Put(r, 'F', 1.3f, 0.45f, 1.1f, () => { B(Wood(new Color(0.3f, 0.25f, 0.2f)), 0, 0, 0.22f, 1.3f, 0.5f, 0.45f); B(black, 0, 0.5f, 0.22f, 0.95f, 0.55f, 0.06f); }, -1f, true, new Spot { kind = "tv", label = "Turn the telly on", tag = "tv" });
                Put(r, 'L', 1.1f, 0.45f, 0.8f, () => { B(Wood(new Color(0.4f, 0.3f, 0.22f)), 0, 0, 0.22f, 1.1f, 0.8f, 0.45f); }, -1f, false, new Spot { kind = "rummage", label = "Open the sideboard", tag = "sideboard" });
                if (inn.width > 3.2f) Put(r, 'R', 0.9f, 0.3f, 1.8f, () => { ShelfUnit(0.9f, 0.3f, 1.8f, rng.Next(30), false, 0.4f); }, -1f, false, new Spot { kind = "rummage", label = "Go through the shelf", tag = "shelf" });
                PutAt(r, inn.center.x, inn.center.y, 1.0f, 0.5f, 0.4f, () => TableDraw(1.0f, 0.5f, 0.4f, new Color(0.4f, 0.3f, 0.2f)));
                if (r.use == "kitchenette") KitchenRun(r, 'L');
                if (r.use == "studio") BedAt(r, 'R');
                Mess(r);
            }
            void Bedroom(Room r, bool vacant)
            {
                var inn = Inner(r); if (vacant) return;
                BedAt(r, inn.width >= inn.height ? 'L' : 'F');
                Put(r, 'R', 1.0f, 0.55f, 2.0f, () => { B(Wood(new Color(0.55f, 0.45f, 0.35f)), 0, 0, 0.275f, 1.0f, 2.0f, 0.55f); B(black, -0.02f, 0.9f, 0.55f, 0.02f, 0.1f, 0.02f); }, -1f, false, new Spot { kind = "rummage", label = "Go through the wardrobe", tag = "wardrobe" });
                Put(r, 'B', 0.9f, 0.45f, 0.85f, () => { B(Wood(new Color(0.5f, 0.4f, 0.3f)), 0, 0, 0.225f, 0.9f, 0.85f, 0.45f); }, -1f, false, new Spot { kind = "rummage", label = "Go through the drawers", tag = "drawers" });
                Mess(r);
            }
            void BedAt(Room r, char wall)
            {
                var c = H.bedding;
                float len = 2.0f, wd = 1.45f; bool alongWall = wall == 'L' || wall == 'R';
                Put(r, wall, wd, len, 0.6f, () =>
                {
                    B(Wood(new Color(0.45f, 0.33f, 0.22f)), 0, 0, len / 2, wd, 0.3f, len); B(Fab(new Color(0.88f, 0.88f, 0.9f)), 0, 0.3f, len / 2, wd - 0.06f, 0.18f, len - 0.06f);
                    B(Fab(c), 0, 0.46f, len / 2 + 0.2f, wd - 0.04f, 0.1f, len - 0.5f); B(Wood(new Color(0.4f, 0.3f, 0.2f)), 0, 0, 0.03f, wd, 0.9f, 0.05f);
                    B(Fab(Color.white), -0.32f, 0.48f, 0.3f, 0.5f, 0.12f, 0.32f); B(Fab(Color.white), 0.32f, 0.48f, 0.3f, 0.5f, 0.12f, 0.32f);
                }, -1f, true, new Spot { kind = "lie", label = "Sit on the bed", tag = "bed" }, new Spot { kind = "rummage", label = "Look under the bed", tag = "underbed", hidden = true, along = 0.4f });
            }
            void Kitchen(Room r, bool vacant)
            {
                if (vacant) return;
                var inn = Inner(r); KitchenRun(r, inn.width >= inn.height ? 'L' : 'B');
                Put(r, 'R', 0.7f, 0.65f, 1.85f, () => { B(whiteM, 0, 0, 0.32f, 0.65f, 1.85f, 0.64f); B(Flat(new Color(0.7f, 0.7f, 0.72f), 0.8f), 0.28f, 0.7f, 0.65f, 0.03f, 0.5f, 0.02f); }, -1f, false, new Spot { kind = "fridge", label = "Open the fridge", tag = "fridge" });
                if (inn.width > 2.9f && inn.height > 2.4f) PutAt(r, inn.center.x, inn.center.y, 0.8f, 0.8f, 0.75f, () => { TableDraw(0.8f, 0.8f, 0.75f, new Color(0.7f, 0.65f, 0.55f)); }, 'F', false, new Spot { kind = "rummage", label = "Look on the table", tag = "table" });
                Mess(r);
            }
            void KitchenRun(Room r, char wall)
            {
                var inn = Inner(r); bool horiz = wall == 'F' || wall == 'B'; float len = Mathf.Min(3.0f, (horiz ? inn.width : inn.height) - 0.9f); len = Mathf.Max(1.2f, len);
                Color cab = H.era >= 3 ? new Color(0.2f, 0.22f, 0.25f) : H.era == 0 ? new Color(0.55f, 0.4f, 0.28f) : new Color(0.85f, 0.85f, 0.83f);
                Put(r, wall, len, 0.62f, 0.92f, () =>
                {
                    B(Wood(cab), 0, 0, 0.31f, len, 0.86f, 0.6f); B(Flat(new Color(0.35f, 0.35f, 0.37f), 0.7f), 0, 0.86f, 0.31f, len + 0.02f, 0.04f, 0.64f);
                    B(steel, -len * 0.25f, 0.9f, 0.3f, 0.5f, 0.02f, 0.4f); B(black, len * 0.25f, 0.9f, 0.3f, 0.5f, 0.02f, 0.4f);
                    B(Wood(cab), 0, 1.45f, 0.17f, len, 0.7f, 0.32f);
                }, -1f, true, new Spot { kind = "tap", label = "Run the tap", tag = "kettle", along = -len * 0.25f }, new Spot { kind = "rummage", label = "Search the cupboards", tag = "cupboard", along = len * 0.2f }, new Spot { kind = "rummage", label = "Look under the sink", tag = "sink", hidden = true, along = -len * 0.25f });
            }
            void Bath(Room r, bool vacant)
            {
                if (vacant) return;
                Put(r, 'B', 1.7f, 0.72f, 0.55f, () => { B(whiteM, 0, 0, 0.36f, 1.7f, 0.55f, 0.72f); B(Flat(new Color(0.7f, 0.85f, 0.95f), 0.9f), 0, 0.5f, 0.36f, 1.55f, 0.02f, 0.58f); B(steel, -0.78f, 0.55f, 0.1f, 0.05f, 0.1f, 0.05f); }, -1f, true, new Spot { kind = "tap", label = "Run a bath", tag = "bath" });
                Put(r, 'F', 0.4f, 0.7f, 0.8f, () => { B(whiteM, 0, 0, 0.33f, 0.38f, 0.4f, 0.55f); B(whiteM, 0, 0.4f, 0.1f, 0.4f, 0.45f, 0.18f); }, -1f, true, new Spot { kind = "flush", label = "Flush", tag = "flush" }, new Spot { kind = "rummage", label = "Lift the cistern lid", tag = "cistern", hidden = true });
                Put(r, 'L', 0.5f, 0.4f, 0.9f, () => { B(whiteM, 0, 0.78f, 0.2f, 0.45f, 0.1f, 0.36f); B(whiteM, 0, 0, 0.1f, 0.12f, 0.78f, 0.12f); B(Flat(new Color(0.8f, 0.85f, 0.9f), 0.9f), 0, 1.2f, 0.02f, 0.4f, 0.55f, 0.02f); }, -1f, false, new Spot { kind = "tap", label = "Wash your hands", tag = "basin" }, new Spot { kind = "rummage", label = "Open the bathroom cabinet", tag = "bathcab", hidden = true });
                Put(r, 'R', 0.4f, 0.35f, 0.5f, () => { B(Wood(new Color(0.55f, 0.4f, 0.3f)), 0, 0, 0.17f, 0.4f, 0.45f, 0.35f); }, -1f, false, new Spot { kind = "rummage", label = "Look in the basket", tag = "basket" });
            }

            /// <summary>Clutter by how the household lives: laundry, takeaways, bottles, boxes.</summary>
            void Mess(Room r)
            {
                var inn = Inner(r); float amt = (1f - H.tidy) * 0.7f + H.stuff * 0.6f; int n = Mathf.RoundToInt(amt * 6f);
                for (int i = 0; i < n; i++)
                {
                    float x = R(inn.xMin + 0.3f, inn.xMax - 0.3f), d = R(inn.yMin + 0.3f, inn.yMax - 0.3f), y = Yf[r.lv] + 0.004f;
                    var fp = new Rect(x - 0.15f, d - 0.15f, 0.3f, 0.3f); if (!Free(r, fp)) continue;
                    int k = rng.Next(4);
                    var m = k == 0 ? Fab(Color.HSVToRGB((float)rng.NextDouble(), 0.4f, 0.7f)) : k == 1 ? Flat(new Color(0.9f, 0.88f, 0.8f)) : k == 2 ? Flat(new Color(0.15f, 0.4f, 0.2f), 0.8f) : GoodsMat(rng.Next(9));
                    Bx(deco, m, x - 0.12f, x + 0.12f + R(0f, 0.1f), y, y + (k == 0 ? 0.08f : 0.05f), d - 0.1f, d + 0.1f + R(0f, 0.1f));
                }
            }
            void Squalor(Room r)
            {
                var inn = Inner(r); float y = Yf[r.lv] + 0.004f;
                for (int i = 0; i < 5; i++) { float x = R(inn.xMin + 0.3f, inn.xMax - 0.3f), d = R(inn.yMin + 0.3f, inn.yMax - 0.3f); Bx(deco, Flat(new Color(0.75f, 0.74f, 0.7f), 0.2f), x, x + R(0.1f, 0.3f), y, y + 0.003f, d, d + R(0.1f, 0.2f)); }
            }
        }
    }
}
