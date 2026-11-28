using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>Furnishing: places pieces against the walls and in the open, avoiding doors, windows, stairs and each other, then adds what the household's habits leave lying about.</summary>
    public partial class HomeBuilder
    {
        readonly List<Rect>[] clear = { new List<Rect>(), new List<Rect>(), new List<Rect>() };
        readonly List<Rect>[] hard = { new List<Rect>(), new List<Rect>(), new List<Rect>() };
        MeshKit deco = new MeshKit(); readonly HashSet<Room> skip = new HashSet<Room>();
        void HardRect(int lv, Rect fp) { hard[lv].Add(fp); }
        Material wood, wood2, metal, black, ceramic, worktop, fabric, lino;
        public float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        bool Chance(double p) => rng.NextDouble() < p;

        static Material FlatM(Color c, float smooth, float metal)
        {
            string key = "fm|" + c.ToString() + smooth + metal; if (mats.TryGetValue(key, out var m)) return m;
            m = Gfx.Mat(c, null, smooth, metal); MeshKit.Tile[m] = 1f; mats[key] = m; return m;
        }
        Material Fab(Color c) => Surf("fabric_wool", 0.6f, c, 0.1f);

        void InitClear()
        {
            foreach (var w in walls)
                foreach (var g in w.gaps)
                {
                    Rect q = w.axis == 'u' ? new Rect(g.c - g.w / 2 - 0.05f, w.at - 0.95f, g.w + 0.1f, 1.9f) : new Rect(w.at - 0.95f, g.c - g.w / 2 - 0.05f, 1.9f, g.w + 0.1f);
                    clear[w.lv].Add(q);
                }
            clear[0].Add(new Rect(2.15f, D1 - 0.95f, 1.0f, 0.95f));        // back door swing
            foreach (var v in voids) clear[v.lv].Add(new Rect(v.r.x - 0.2f, v.r.y - 0.2f, v.r.width + 0.4f, v.r.height + 0.4f));
            for (int l = 0; l < 3; l++) foreach (var b in blocks[l]) clear[l].Add(new Rect(b.x - 0.05f, b.y - 0.05f, b.width + 0.1f, b.height + 0.1f));
            // the stairs' landings need room to move
            clear[0].Add(new Rect(4.3f, 3.4f, 0.9f, 1.1f)); clear[1].Add(new Rect(U0, D0, 1.4f, 1.4f)); clear[1].Add(new Rect(3.3f, D0, 1.9f, 1.0f)); clear[2].Add(new Rect(U0, 4.0f, 1.2f, 1.0f));
        }

        Rect Inner(Room r)
        {
            float a = Mathf.Abs(r.u0 - U0) < 0.01f ? 0f : 0.09f, b = Mathf.Abs(r.u1 - U1) < 0.01f ? 0f : 0.09f, c = Mathf.Abs(r.d0 - D0) < 0.01f ? 0f : 0.09f, d = Mathf.Abs(r.d1 - D1) < 0.01f ? 0f : 0.09f;
            return Rect.MinMaxRect(r.u0 + a, r.d0 + c, r.u1 - b, r.d1 - d);
        }

        bool Ok(Room r, Rect fp, float h, char wall, float pad)
        {
            var inn = Inner(r);
            if (fp.xMin < inn.xMin - 0.005f || fp.xMax > inn.xMax + 0.005f || fp.yMin < inn.yMin - 0.005f || fp.yMax > inn.yMax + 0.005f) return false;
            var big = new Rect(fp.x - pad, fp.y - pad, fp.width + pad * 2, fp.height + pad * 2);
            foreach (var t in r.taken) if (t.Overlaps(big)) return false;
            foreach (var c in clear[r.lv]) if (c.Overlaps(fp)) return false;
            foreach (var w in wins) if (w.lv == r.lv && (wall == 'F' ? w.front : wall == 'B' ? !w.front : false) && h > w.sill - 0.02f && fp.xMax > w.u0 && fp.xMin < w.u1) return false;
            if (wall == 'x') foreach (var w in wins) if (w.lv == r.lv) { }
            return true;
        }

        struct Cand { public Rect fp; public float cu, cd, fu, fd; public char wall; }

        /// <summary>Put a piece with its back to a wall (walls: F front, B back, L left, R right; tried in the order given). Returns true if it fitted.</summary>
        public bool Put(Room r, float w, float dep, float h, string sides, System.Action draw, string kind = null, string label = null, string tag = null, bool hidden = false, float pad = 0.03f, bool corner = false, bool collide = true, float seat = 0f)
        {
            var inn = Inner(r); var list = new List<Cand>();
            foreach (char ch in sides)
            {
                var mine = new List<Cand>();
                if (ch == 'F' || ch == 'B')
                    for (float c = inn.xMin + w / 2; c <= inn.xMax - w / 2 + 0.001f; c += 0.1f)
                    {
                        float cd = ch == 'F' ? inn.yMin + dep / 2 : inn.yMax - dep / 2;
                        mine.Add(new Cand { fp = new Rect(c - w / 2, cd - dep / 2, w, dep), cu = c, cd = cd, fu = 0f, fd = ch == 'F' ? 1f : -1f, wall = ch });
                    }
                else
                    for (float c = inn.yMin + w / 2; c <= inn.yMax - w / 2 + 0.001f; c += 0.1f)
                    {
                        float cu = ch == 'L' ? inn.xMin + dep / 2 : inn.xMax - dep / 2;
                        mine.Add(new Cand { fp = new Rect(cu - dep / 2, c - w / 2, dep, w), cu = cu, cd = c, fu = ch == 'L' ? 1f : -1f, fd = 0f, wall = ch });
                    }
                mine.RemoveAll(q => !Ok(r, q.fp, h, q.wall, pad));
                if (mine.Count > 0) { list.AddRange(mine); if (!(sides.Length > 1 && Chance(0.35))) break; }
            }
            for (int tries = 0; tries < 10 && list.Count > 0; tries++)
            {
                int ix = corner ? (Chance(0.5) ? 0 : list.Count - 1) : rng.Next(list.Count); var pick = list[ix]; list.RemoveAt(ix);
                if (Commit(r, pick, w, dep, h, draw, kind, label, tag, hidden, collide, seat)) return true;
            }
            return false;
        }

        bool Commit(Room r, Cand c, float w, float dep, float h, System.Action draw, string kind, string label, string tag, bool hidden, bool collide, float seat)
        {
            if (collide && h > 0.35f) { hard[r.lv].Add(c.fp); if (!Connected(out _, r.lv)) { hard[r.lv].RemoveAt(hard[r.lv].Count - 1); return false; } }
            lastCand = c; lastWall = c.wall; r.taken.Add(c.fp);
            furn.Push(P(c.cu, Y[r.lv], c.cd), WorldYaw(c.fu, c.fd)); draw(); furn.Pop();
            if (collide && h > 0.05f) solids.Add((P(c.cu, Y[r.lv] + h / 2, c.cd), new Vector3(c.fp.width, h, c.fp.height), 0f));
            if (kind != null) AddSpot(r, kind, label, tag, c.cu, c.cd, Y[r.lv], c.fu, c.fd, hidden, seat);
            return true;
        }

        /// <summary>A piece in the open (tables, beds in the middle): near the wanted point, as close as it fits.</summary>
        public bool PutAt(Room r, float pu, float pd, float w, float dep, float h, bool alongU, System.Action draw, string kind = null, string label = null, string tag = null, bool hidden = false, float pad = 0.05f, float seat = 0f)
        {
            var inn = Inner(r); var cands = new List<(float dist, Cand c)>();
            for (float u = inn.xMin; u <= inn.xMax; u += 0.1f)
                for (float d = inn.yMin; d <= inn.yMax; d += 0.1f)
                {
                    float ex = alongU ? w : dep, ez = alongU ? dep : w; var fp = new Rect(u - ex / 2, d - ez / 2, ex, ez);
                    if (!Ok(r, fp, h, 'x', pad)) continue;
                    var cd = new Cand { fp = fp, cu = u, cd = d, fu = alongU ? 0f : 1f, fd = alongU ? 1f : 0f, wall = 'x' };
                    cands.Add(((u - pu) * (u - pu) + (d - pd) * (d - pd), cd));
                }
            cands.Sort((a, b) => a.dist.CompareTo(b.dist));
            for (int i = 0, tries = 0; i < cands.Count && tries < 6; i += 3, tries++) if (Commit(r, cands[i].c, w, dep, h, draw, kind, label, tag, hidden, true, seat)) return true;
            return false;
        }

        /// <summary>Small stuff on the floor: anywhere free, any way round.</summary>
        void Scatter(Room r, float w, float dep, float h, System.Action<float> draw, int tries = 14)
        {
            var inn = Inner(r);
            for (int t = 0; t < tries; t++)
            {
                float u = R(inn.xMin + w / 2, inn.xMax - w / 2), d = R(inn.yMin + dep / 2, inn.yMax - dep / 2); float yaw = R(0f, 360f);
                var fp = new Rect(u - w / 2, d - dep / 2, w, dep);
                if (!Ok(r, fp, h, 'x', 0.02f)) continue;
                r.taken.Add(fp);
                furn.Push(P(u, Y[r.lv], d), yaw); draw(yaw); furn.Pop();
                return;
            }
        }

        public void AddSpot(Room r, string kind, string label, string tag, float u, float d, float y, float fu, float fd, bool hidden = false, float seat = 0f)
        {
            home.spots.Add(new Home.SpotRec { kind = kind, label = label, tag = tag, pos = P(u, y, d), yaw = WorldYaw(fu, fd), room = r.idx, lv = r.lv, hidden = hidden, h = seat });
        }

        // ------------------------------------------------------------------ small drawing helpers (item frame: origin floor centre, front towards +z)
        void B(Material m, float x, float y, float z, float w, float h, float d) { furn.BoxB(m, new Vector3(x, y, z), new Vector3(w, h, d)); }
        void Cy(Material m, float x, float y, float z, float rad, float h, int seg = 10) { furn.Cyl(m, new Vector3(x, y, z), rad, h, seg); }

        // ------------------------------------------------------------------ furniture
        void Sofa(float w, Material m, bool corner = false)
        {
            B(m, 0, 0.1f, 0, w, 0.32f, 0.88f); B(m, 0, 0.42f, -0.34f, w, 0.45f, 0.2f);
            B(m, -w / 2 + 0.09f, 0.42f, 0.02f, 0.18f, 0.2f, 0.76f); B(m, w / 2 - 0.09f, 0.42f, 0.02f, 0.18f, 0.2f, 0.76f);
            B(black, -w / 2 + 0.06f, 0f, 0.38f, 0.06f, 0.1f, 0.06f); B(black, w / 2 - 0.06f, 0f, 0.38f, 0.06f, 0.1f, 0.06f); B(black, -w / 2 + 0.06f, 0f, -0.38f, 0.06f, 0.1f, 0.06f); B(black, w / 2 - 0.06f, 0f, -0.38f, 0.06f, 0.1f, 0.06f);
            int n = Mathf.Max(2, Mathf.RoundToInt((w - 0.4f) / 0.6f)); float cw = (w - 0.4f) / n;
            for (int i = 0; i < n; i++) B(Fab(Shade(H.sofa, 1.08f)), -w / 2 + 0.2f + cw * (i + 0.5f), 0.42f, 0.07f, cw - 0.02f, 0.1f, 0.62f);
            if (H.era >= 2 && Chance(0.7)) B(Fab(Color.Lerp(H.sofa, H.bedding, 0.6f)), -w / 2 + 0.35f, 0.52f, -0.1f, 0.4f, 0.36f, 0.12f);
        }
        void Armchair(Material m) { Sofa(0.95f, m); }
        void TvUnit(float w)
        {
            if (H.era <= 1 && Chance(0.8))
            {   // a tube telly on a stand
                B(wood, 0, 0f, 0f, w * 0.7f, 0.5f, 0.5f); B(black, 0, 0.5f, 0.02f, 0.62f, 0.5f, 0.46f); B(FlatM(new Color(0.02f, 0.03f, 0.04f), 0.8f, 0f), 0, 0.55f, 0.25f, 0.5f, 0.38f, 0.02f);
                B(black, 0.1f, 0.5f + 0.5f, 0.02f, 0.25f, 0.03f, 0.2f);
            }
            else
            {
                B(H.era >= 3 ? wood2 : wood, 0, 0f, 0f, w, 0.45f, 0.42f); B(black, 0, 0.45f, 0.05f, 0.35f, 0.02f, 0.2f); B(black, 0, 0.47f, 0.05f, 0.04f, 0.18f, 0.04f);
                B(black, 0, 0.55f, 0.02f, Mathf.Min(1.1f, w - 0.1f), 0.62f, 0.05f); B(FlatM(new Color(0.01f, 0.01f, 0.015f), 0.9f, 0f), 0, 0.57f, 0.05f, Mathf.Min(1.1f, w - 0.1f) - 0.06f, 0.56f, 0.012f);
                if (H.gamer) B(black, w * 0.3f, 0.2f, 0.2f, 0.28f, 0.06f, 0.22f);
            }
        }
        void CoffeeTable()
        {
            var top = H.era >= 3 ? FlatM(new Color(0.5f, 0.6f, 0.62f, 1f), 0.9f, 0f) : wood;
            B(top, 0, 0.38f, 0, 1.0f, 0.04f, 0.5f);
            foreach (float x in new[] { -0.45f, 0.45f }) foreach (float z in new[] { -0.2f, 0.2f }) B(black, x, 0f, z, 0.04f, 0.38f, 0.04f);
            if (H.era <= 1) B(wood, 0, 0.1f, 0, 0.9f, 0.02f, 0.42f);
        }
        void Sideboard(float w) { B(wood, 0, 0.05f, 0, w, 0.8f, 0.44f); B(black, 0, 0f, 0, w - 0.1f, 0.05f, 0.38f); for (int i = 0; i < 3; i++) { float x = -w / 3 + i * w / 3; B(Shade(wood.color, 0.8f).Mat(), x, 0.12f, 0.221f, w / 3 - 0.05f, 0.62f, 0.012f); B(CotMats.brass, x + 0.1f, 0.5f, 0.23f, 0.03f, 0.03f, 0.02f); } B(wood, 0, 0.85f, 0, w + 0.04f, 0.03f, 0.47f); }
        void Bookcase(float w, float hgt = 1.8f)
        {
            B(wood, -w / 2 + 0.01f, 0f, 0f, 0.02f, hgt, 0.3f); B(wood, w / 2 - 0.01f, 0f, 0f, 0.02f, hgt, 0.3f); B(wood, 0, 0f, -0.14f, w, hgt, 0.02f);
            for (float y = 0.05f; y < hgt; y += 0.35f) { B(wood, 0, y, 0, w - 0.02f, 0.025f, 0.3f); if (y < hgt - 0.3f) B(CotMats.books, 0, y + 0.025f, 0.0f, w - 0.1f, 0.28f, 0.22f); }
            B(wood, 0, hgt - 0.02f, 0, w, 0.025f, 0.3f);
        }
        void Shelving(float w, float hgt, bool boxes)
        {
            B(metal, -w / 2 + 0.02f, 0f, 0f, 0.04f, hgt, 0.4f); B(metal, w / 2 - 0.02f, 0f, 0f, 0.04f, hgt, 0.4f);
            for (float y = 0.2f; y < hgt; y += 0.45f)
            {
                B(wood, 0, y, 0, w, 0.03f, 0.4f);
                if (boxes) for (float x = -w / 2 + 0.2f; x < w / 2 - 0.2f; x += 0.35f) B(Flat(new Color(R(0.45f, 0.7f), R(0.35f, 0.55f), R(0.2f, 0.35f))), x, y + 0.03f, 0f, R(0.22f, 0.32f), R(0.15f, 0.32f), 0.3f);
            }
        }
        void DiningTable(float w, float d, int chairs, float h = 0.76f)
        {
            var top = H.era >= 2 ? FlatM(new Color(0.85f, 0.85f, 0.83f), 0.6f, 0f) : wood;
            B(top, 0, h - 0.04f, 0, w, 0.04f, d); foreach (float x in new[] { -w / 2 + 0.06f, w / 2 - 0.06f }) foreach (float z in new[] { -d / 2 + 0.06f, d / 2 - 0.06f }) B(wood, x, 0f, z, 0.05f, h - 0.04f, 0.05f);
        }
        void Chair(float yawOff = 0f)
        {
            B(wood, 0, 0.42f, 0, 0.42f, 0.04f, 0.42f); B(wood, 0, 0.46f, -0.19f, 0.42f, 0.4f, 0.03f);
            foreach (float x in new[] { -0.18f, 0.18f }) foreach (float z in new[] { -0.18f, 0.18f }) B(wood, x, 0f, z, 0.035f, 0.42f, 0.035f);
            if (H.era <= 1) B(Fab(H.curtain), 0, 0.46f, 0f, 0.38f, 0.04f, 0.38f);
        }
        void StandardLamp() { Cy(black, 0, 0, 0, 0.12f, 0.02f); Cy(black, 0, 0.02f, 0, 0.012f, 1.45f, 6); furn.Cyl(Fab(new Color(0.9f, 0.85f, 0.7f)), new Vector3(0, 1.38f, 0), 0.17f, 0.24f, 10, false, 0.12f); }
        void Plant(float h = 0.9f) { Cy(CotMats.terracotta, 0, 0, 0, 0.15f, 0.3f); Cy(CotMats.soil, 0, 0.28f, 0, 0.13f, 0.03f); furn.Sphere(CotMats.leaf, new Vector3(0, 0.3f + h * 0.5f, 0), new Vector3(0.28f, h * 0.45f, 0.28f), 8, 5); }
        void Radiator(float w = 0.9f) { B(Flat(new Color(0.95f, 0.95f, 0.93f), 0.5f), 0, 0.15f, 0.03f, w, 0.55f, 0.07f); for (int i = 0; i < 8; i++) B(Flat(new Color(0.9f, 0.9f, 0.88f)), -w / 2 + 0.06f + i * (w - 0.12f) / 7, 0.15f, 0.07f, 0.02f, 0.55f, 0.03f); }
        void FireplaceOld() { B(wood, 0, 0f, 0, 1.2f, 1.0f, 0.2f); B(black, 0, 0.1f, 0.05f, 0.55f, 0.55f, 0.1f); B(CotMats.brass, 0, 0.1f, 0.09f, 0.5f, 0.05f, 0.04f); B(wood, 0, 1.0f, 0.02f, 1.35f, 0.05f, 0.28f); B(Flat(new Color(0.9f, 0.5f, 0.2f)), 0, 0.2f, 0.1f, 0.4f, 0.3f, 0.02f); }
        void Rug(Room r, float cu, float cd, float w, float d, Material m)
        {
            // flat rugs lie under the furniture: no footprint, drawn first
            Bx(furn, m, cu - w / 2, cu + w / 2, Y[r.lv], Y[r.lv] + 0.012f, cd - d / 2, cd + d / 2);
        }
        void Bed(float w, bool bunk, bool cot, Color bedc)
        {
            float L = cot ? 1.2f : 2.0f; float hw = cot ? 0.65f : w;
            var frame = H.era >= 3 ? black : wood; var bed = Fab(bedc); var pil = Fab(new Color(0.95f, 0.95f, 0.92f));
            // head at -z, foot towards +z
            B(frame, 0, 0.1f, 0, hw, 0.2f, L); B(bed, 0, 0.3f, 0.02f, hw - 0.04f, 0.2f, L - 0.06f); B(frame, 0, 0f, -L / 2 + 0.04f, hw, 1.0f, 0.06f);
            if (!cot) { B(pil, -hw / 4, 0.5f, -L / 2 + 0.3f, hw / 2 - 0.1f, 0.12f, 0.35f); if (w > 1.2f) B(pil, hw / 4, 0.5f, -L / 2 + 0.3f, hw / 2 - 0.1f, 0.12f, 0.35f); B(Fab(Shade(bedc, 0.85f)), 0, 0.5f, 0.35f, hw - 0.06f, 0.06f, L * 0.55f); }
            else { B(frame, -hw / 2, 0.3f, 0, 0.03f, 0.55f, L); B(frame, hw / 2, 0.3f, 0, 0.03f, 0.55f, L); B(frame, 0, 0.3f, L / 2, hw, 0.55f, 0.03f); }
            if (bunk) { B(frame, 0, 1.15f, 0, hw, 0.12f, L); B(bed, 0, 1.27f, 0.02f, hw - 0.04f, 0.2f, L - 0.06f); foreach (float x in new[] { -hw / 2 + 0.03f, hw / 2 - 0.03f }) foreach (float z in new[] { -L / 2 + 0.03f, L / 2 - 0.03f }) B(frame, x, 0.2f, z, 0.05f, 1.6f, 0.05f); for (int k = 0; k < 5; k++) B(frame, hw / 2 - 0.03f, 0.25f + k * 0.25f, L / 2 - 0.15f, 0.03f, 0.03f, 0.35f); }
        }
        void Wardrobe(float w, bool mirror)
        {
            B(wood, 0, 0f, 0, w, 1.95f, 0.58f); B(Shade(wood.color, 0.82f).Mat(), -w / 4, 0.05f, 0.295f, w / 2 - 0.03f, 1.8f, 0.012f); B(Shade(wood.color, 0.82f).Mat(), w / 4, 0.05f, 0.295f, w / 2 - 0.03f, 1.8f, 0.012f);
            if (mirror) B(FlatM(new Color(0.75f, 0.8f, 0.82f), 0.95f, 0.6f), w / 4, 0.2f, 0.302f, w / 2 - 0.12f, 1.5f, 0.006f);
            B(CotMats.brass, -0.03f, 0.95f, 0.31f, 0.02f, 0.14f, 0.02f); B(CotMats.brass, 0.03f, 0.95f, 0.31f, 0.02f, 0.14f, 0.02f);
        }
        void Drawers(float w, float h = 0.85f) { B(wood, 0, 0f, 0, w, h, 0.46f); for (int i = 0; i < 4; i++) { B(Shade(wood.color, 0.85f).Mat(), 0, 0.06f + i * (h - 0.1f) / 4, 0.235f, w - 0.06f, (h - 0.14f) / 4 - 0.01f, 0.01f); B(CotMats.brass, 0, 0.06f + i * (h - 0.1f) / 4 + 0.08f, 0.25f, 0.1f, 0.02f, 0.02f); } }
        void Bedside() { B(wood, 0, 0f, 0, 0.42f, 0.52f, 0.38f); B(Shade(wood.color, 0.85f).Mat(), 0, 0.28f, 0.19f, 0.36f, 0.18f, 0.01f); Cy(black, 0.08f, 0.52f, -0.05f, 0.05f, 0.02f); Cy(black, 0.08f, 0.54f, -0.05f, 0.012f, 0.18f, 6); furn.Cyl(Fab(new Color(0.9f, 0.85f, 0.7f)), new Vector3(0.08f, 0.68f, -0.05f), 0.1f, 0.14f, 10, false, 0.07f); }
        void Desk(bool pc)
        {
            B(wood, 0, 0.7f, 0, 1.2f, 0.04f, 0.6f); B(wood, -0.55f, 0f, 0, 0.04f, 0.7f, 0.55f); B(wood, 0.55f, 0f, 0, 0.04f, 0.7f, 0.55f);
            if (pc) { B(black, -0.15f, 0.74f, -0.2f, 0.58f, 0.36f, 0.03f); B(FlatM(new Color(0.1f, 0.12f, 0.2f), 0.9f, 0f), -0.15f, 0.76f, -0.18f, 0.54f, 0.32f, 0.01f); B(black, 0.35f, 0.74f, -0.1f, 0.2f, 0.42f, 0.4f); B(Flat(new Color(0.9f, 0.1f, 0.5f)), 0.35f, 0.74f, 0.1f, 0.19f, 0.01f, 0.01f); B(black, -0.15f, 0.74f, 0.1f, 0.42f, 0.015f, 0.14f); }
            else { B(Flat(new Color(0.9f, 0.9f, 0.9f)), -0.3f, 0.74f, 0f, 0.3f, 0.02f, 0.4f); B(Flat(new Color(0.2f, 0.3f, 0.6f)), 0.3f, 0.74f, 0.05f, 0.2f, 0.12f, 0.28f); }
        }
        void OfficeChair() { Cy(black, 0, 0f, 0, 0.25f, 0.05f, 6); Cy(black, 0, 0.05f, 0, 0.025f, 0.4f, 6); B(black, 0, 0.45f, 0, 0.45f, 0.08f, 0.45f); B(black, 0, 0.5f, -0.2f, 0.42f, 0.5f, 0.06f); }
        void Bath(bool shower)
        {
            if (!shower) { B(ceramic, 0, 0f, 0, 0.72f, 0.56f, 1.7f); B(FlatM(new Color(0.85f, 0.9f, 0.95f), 0.9f, 0f), 0, 0.5f, 0, 0.58f, 0.04f, 1.56f); B(CotMats.pewter, 0, 0.55f, -0.8f, 0.2f, 0.12f, 0.04f); B(FlatM(new Color(0.8f, 0.9f, 0.95f, 1f), 0.9f, 0f), 0.2f, 0.56f, 0f, 0.02f, 1.0f, 0.7f); }
            else { B(ceramic, 0, 0f, 0, 0.9f, 0.08f, 0.9f); B(FlatM(new Color(0.75f, 0.88f, 0.92f), 0.95f, 0f), 0, 0.08f, 0.44f, 0.9f, 1.85f, 0.012f); B(FlatM(new Color(0.75f, 0.88f, 0.92f), 0.95f, 0f), 0.44f, 0.08f, 0, 0.012f, 1.85f, 0.9f); B(CotMats.pewter, 0, 1.2f, -0.43f, 0.2f, 0.2f, 0.04f); }
        }
        void Toilet(bool cistern = true) { B(ceramic, 0, 0f, 0.05f, 0.36f, 0.4f, 0.5f); B(Flat(new Color(0.95f, 0.95f, 0.93f)), 0, 0.4f, 0.08f, 0.38f, 0.03f, 0.44f); B(ceramic, 0, 0.35f, -0.2f, 0.4f, 0.4f, 0.18f); B(CotMats.pewter, 0.1f, 0.78f, -0.2f, 0.05f, 0.02f, 0.05f); }
        void Basin() { B(ceramic, 0, 0f, 0, 0.14f, 0.8f, 0.14f); B(ceramic, 0, 0.8f, 0, 0.55f, 0.12f, 0.42f); B(CotMats.pewter, 0, 0.92f, -0.15f, 0.05f, 0.1f, 0.05f); }
        void MirrorCab() { B(wood, 0, 1.2f, 0, 0.5f, 0.6f, 0.14f); B(FlatM(new Color(0.8f, 0.85f, 0.88f), 0.95f, 0.6f), 0, 1.22f, 0.072f, 0.46f, 0.56f, 0.006f); }
        void Fridge(float h = 1.75f, bool tall = true) { B(Flat(new Color(0.93f, 0.93f, 0.95f), 0.6f), 0, 0f, 0, 0.6f, h, 0.63f); B(CotMats.pewter, 0.22f, 0.9f, 0.32f, 0.025f, 0.35f, 0.025f); B(Flat(new Color(0.8f, 0.8f, 0.82f)), 0, h * 0.46f, 0.316f, 0.58f, 0.008f, 0.005f); if (H.era <= 1 && Chance(0.7)) B(Flat(new Color(0.85f, 0.8f, 0.55f), 0.5f), 0, 0f, 0, 0.6f, 0.01f, 0.63f); }
        void Washer() { B(Flat(new Color(0.94f, 0.94f, 0.95f), 0.6f), 0, 0f, 0, 0.6f, 0.85f, 0.58f); Cy(black, 0, 0.25f, 0.29f, 0.19f, 0.01f, 12); furn.CylX(Flat(new Color(0.3f, 0.4f, 0.5f), 0.9f), new Vector3(0, 0.45f, 0.29f), 0.17f, 0.02f, 12); }
        void Cooker() { B(Flat(new Color(0.9f, 0.9f, 0.9f), 0.5f), 0, 0f, 0, 0.6f, 0.9f, 0.6f); B(black, 0, 0.9f, 0, 0.6f, 0.02f, 0.6f); foreach (float x in new[] { -0.15f, 0.15f }) foreach (float z in new[] { -0.15f, 0.15f }) Cy(FlatM(new Color(0.15f, 0.15f, 0.16f), 0.5f, 0.5f), x, 0.92f, z, 0.08f, 0.012f); B(black, 0, 0.35f, 0.301f, 0.5f, 0.35f, 0.006f); B(Flat(new Color(0.4f, 0.4f, 0.42f)), 0, 0.72f, 0.302f, 0.5f, 0.05f, 0.01f); }
        void KitchenUnit(float w, bool sink) { B(wood, 0, 0f, 0, w, 0.87f, 0.58f); B(worktop, 0, 0.87f, 0.01f, w + 0.01f, 0.04f, 0.62f); B(Shade(wood.color, 0.82f).Mat(), 0, 0.05f, 0.295f, w - 0.04f, 0.78f, 0.012f); B(CotMats.pewter, w / 2 - 0.1f, 0.5f, 0.31f, 0.02f, 0.14f, 0.02f); if (sink) { B(CotMats.pewter, 0, 0.9f, 0.02f, 0.5f, 0.012f, 0.4f); B(CotMats.pewter, 0, 0.92f, -0.22f, 0.04f, 0.2f, 0.04f); } }
        void WallUnit(float w) { B(wood, 0, 1.45f, 0.12f, w, 0.7f, 0.3f); B(Shade(wood.color, 0.82f).Mat(), 0, 1.47f, 0.272f, w - 0.04f, 0.66f, 0.01f); }
        void Workbench() { B(wood, 0, 0.82f, 0, 1.6f, 0.05f, 0.6f); B(wood, -0.7f, 0f, 0, 0.06f, 0.82f, 0.55f); B(wood, 0.7f, 0f, 0, 0.06f, 0.82f, 0.55f); B(wood, 0, 0.3f, 0, 1.4f, 0.03f, 0.5f); B(metal, -0.5f, 0.87f, 0.1f, 0.14f, 0.12f, 0.12f); B(Flat(new Color(0.75f, 0.2f, 0.1f)), 0.3f, 0.87f, 0f, 0.3f, 0.06f, 0.1f); }
        void Freezer() { B(Flat(new Color(0.92f, 0.92f, 0.94f), 0.6f), 0, 0f, 0, 0.65f, 0.9f, 0.62f); B(CotMats.pewter, 0.25f, 0.55f, 0.32f, 0.02f, 0.2f, 0.02f); }
        void Bike() { var tyre = FlatM(new Color(0.05f, 0.05f, 0.05f), 0.3f, 0f); furn.CylX(tyre, new Vector3(0, 0.3f, 0.5f), 0.3f, 0.04f, 14); furn.CylX(tyre, new Vector3(0, 0.3f, -0.5f), 0.3f, 0.04f, 14); B(Flat(new Color(0.15f, 0.25f, 0.55f)), 0, 0.3f, 0, 0.04f, 0.04f, 1.0f); B(Flat(new Color(0.15f, 0.25f, 0.55f)), 0, 0.3f, 0.2f, 0.04f, 0.45f, 0.04f); B(black, 0, 0.78f, -0.3f, 0.2f, 0.04f, 0.1f); B(black, 0, 0.75f, 0.5f, 0.45f, 0.03f, 0.03f); }
        void Pile(Color c, float s) { B(Flat(c, 0.1f), 0, 0, 0, s, s * 0.35f, s * 0.8f); B(Flat(Shade(c, 1.15f), 0.1f), 0.03f, s * 0.3f, 0.02f, s * 0.7f, s * 0.2f, s * 0.6f); }
        void Bag(Color c) { furn.Sphere(Flat(c, 0.6f), new Vector3(0, 0.22f, 0), new Vector3(0.22f, 0.24f, 0.2f), 8, 5); B(Flat(c, 0.6f), 0, 0.42f, 0, 0.05f, 0.1f, 0.05f); }
        void Pizza() { B(Flat(new Color(0.8f, 0.65f, 0.45f), 0.1f), 0, 0, 0, 0.36f, 0.03f, 0.36f); B(Flat(new Color(0.75f, 0.6f, 0.4f), 0.1f), 0.03f, 0.03f, 0.02f, 0.36f, 0.02f, 0.34f); }
        void Cans(int n) { for (int i = 0; i < n; i++) Cy(FlatM(new Color(R(0.4f, 0.8f), R(0.4f, 0.8f), R(0.4f, 0.9f)), 0.6f, 0.7f), R(-0.12f, 0.12f), 0, R(-0.12f, 0.12f), 0.033f, 0.12f, 8); }
        void Toys() { for (int i = 0; i < 4; i++) B(Flat(Color.HSVToRGB(R(0f, 1f), 0.75f, 0.9f), 0.5f), R(-0.25f, 0.25f), 0, R(-0.25f, 0.25f), R(0.08f, 0.18f), R(0.08f, 0.16f), R(0.08f, 0.18f)); }
        void Ashtray() { Cy(Flat(new Color(0.5f, 0.5f, 0.52f), 0.5f), 0, 0, 0, 0.07f, 0.02f); for (int i = 0; i < 3; i++) B(Flat(new Color(0.9f, 0.85f, 0.75f)), R(-0.04f, 0.04f), 0.02f, R(-0.04f, 0.04f), 0.01f, 0.01f, 0.05f); }
        void DogBed() { Cy(Fab(new Color(0.5f, 0.35f, 0.25f)), 0, 0, 0, 0.35f, 0.12f, 12); Cy(Fab(new Color(0.65f, 0.5f, 0.4f)), 0, 0.1f, 0, 0.28f, 0.03f, 12); Cy(FlatM(new Color(0.7f, 0.7f, 0.7f), 0.7f, 0.5f), 0.5f, 0, 0.1f, 0.1f, 0.05f, 8); }
        void Airer() { foreach (float x in new[] { -0.4f, 0.4f }) B(metal, x, 0f, 0, 0.03f, 0.9f, 0.5f); for (int i = 0; i < 5; i++) B(Flat(Color.HSVToRGB(R(0f, 1f), 0.35f, 0.9f), 0.05f), 0, 0.85f - i * 0.06f, 0, 0.85f, 0.04f, 0.5f - i * 0.02f); }
        void Curtains(float u0, float u1, float y0, float y1, bool front, int lv)
        {
            float d = front ? D0 + 0.08f : D1 - 0.1f; var m = H.era <= 1 ? Fab(H.curtain) : Fab(H.curtain);
            float net = H.era == 0 ? 1f : H.era == 1 ? 0.6f : 0f;
            // pelmet rail and two drops
            Bx(deco, black, u0 - 0.1f, u1 + 0.1f, y1 + 0.08f, y1 + 0.1f, d - 0.03f, d + 0.03f);
            float w = Mathf.Clamp((u1 - u0) * 0.22f, 0.22f, 0.5f);
            if (H.era <= 2) { Bx(deco, m, u0 - 0.08f, u0 - 0.08f + w, y0 - 0.25f, y1 + 0.08f, d - 0.03f, d + 0.04f); Bx(deco, m, u1 + 0.08f - w, u1 + 0.08f, y0 - 0.25f, y1 + 0.08f, d - 0.03f, d + 0.04f); }
            else Bx(deco, Flat(new Color(0.88f, 0.88f, 0.86f), 0.1f), u0, u1, y0 + (y1 - y0) * 0.1f, y1 + 0.06f, d - 0.015f, d + 0.015f);   // roller blind, half down
            if (net > 0.5f) Bx(deco, Flat(new Color(0.95f, 0.95f, 0.92f), 0.05f), u0, u1, y0, y1, d - 0.012f, d - 0.008f);
        }
    }

    static class MatExt
    {
        /// <summary>A material for a shade of an existing colour: used for door fronts a touch darker than the carcass.</summary>
        public static Material Mat(this Color c) => HomeBuilder.Flat(c, 0.4f);
    }
}
