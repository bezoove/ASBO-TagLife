using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>Furnishes each cottage room by type, placing pieces against free wall runs (away from doors, windows, hearths, stairs).</summary>
    public partial class CotHouse
    {
        MeshKit furn;
        System.Random rng;

        class WallRun
        {
            public string side; public float edge, a0, a1;
            public readonly List<Vector2> blocked = new List<Vector2>(); // blocks everything
            public readonly List<Vector2> windows = new List<Vector2>(); // blocks tall items
        }

        Dictionary<string, WallRun> Runs(CRoom rm, CRect R)
        {
            var runs = new Dictionary<string, WallRun>
            {
                ["F"] = new WallRun { side = "F", edge = R.d0, a0 = R.x0, a1 = R.x1 },
                ["B"] = new WallRun { side = "B", edge = R.d1, a0 = R.x0, a1 = R.x1 },
                ["L"] = new WallRun { side = "L", edge = R.x0, a0 = R.d0, a1 = R.d1 },
                ["R"] = new WallRun { side = "R", edge = R.x1, a0 = R.d0, a1 = R.d1 },
            };
            foreach (var D in P.doors)
            {
                if (D.lv != rm.lv) continue;
                var blk = new Vector2(D.a - 0.3f, D.b + 0.75f);
                if (D.line == "X") { if (Mathf.Abs(D.at - R.x0) < 0.45f) runs["L"].blocked.Add(blk); if (Mathf.Abs(D.at - R.x1) < 0.45f) runs["R"].blocked.Add(blk); }
                else { if (Mathf.Abs(D.at - R.d0) < 0.45f) runs["F"].blocked.Add(blk); if (Mathf.Abs(D.at - R.d1) < 0.45f) runs["B"].blocked.Add(blk); }
            }
            foreach (var E in P.ext) if (E.room == rm.key && runs.ContainsKey(E.side)) runs[E.side].blocked.Add(new Vector2(E.a - 0.4f, E.b + 0.9f));
            foreach (var W in P.wins) if (W.room == rm.key && runs.ContainsKey(W.side)) { runs[W.side].windows.Add(new Vector2(W.a - 0.05f, W.b + 0.05f)); if (W.y0 < 0.5f) runs[W.side].blocked.Add(new Vector2(W.a, W.b)); }
            foreach (var h in P.hearths) if (h.room == rm.key && runs.ContainsKey(h.side)) runs[h.side].blocked.Add(new Vector2(h.a - 0.4f, h.b + 0.4f));
            foreach (var s in P.stairs)
                foreach (var f in s.f)
                {
                    var q = f.r;
                    if (q.x1 < R.x0 - 0.1f || q.x0 > R.x1 + 0.1f || q.d1 < R.d0 - 0.1f || q.d0 > R.d1 + 0.1f) continue;
                    if (q.d0 - R.d0 < 1.2f) runs["F"].blocked.Add(new Vector2(q.x0 - 0.3f, q.x1 + 0.3f));
                    if (R.d1 - q.d1 < 1.2f) runs["B"].blocked.Add(new Vector2(q.x0 - 0.3f, q.x1 + 0.3f));
                    if (q.x0 - R.x0 < 1.2f) runs["L"].blocked.Add(new Vector2(q.d0 - 0.3f, q.d1 + 0.3f));
                    if (R.x1 - q.x1 < 1.2f) runs["R"].blocked.Add(new Vector2(q.d0 - 0.3f, q.d1 + 0.3f));
                }
            return runs;
        }

        static bool Hit(List<Vector2> l, float a, float b) { foreach (var v in l) if (a < v.y && b > v.x) return true; return false; }

        /// <summary>Find a free spot of width w on a run; returns NaN if none. Tall items avoid windows and low ceilings.</summary>
        float Spot(CRoom rm, WallRun run, float w, float depth, float height, bool tall, float pref = float.NaN)
        {
            float lo = run.a0 + 0.04f + w / 2f, hi = run.a1 - 0.04f - w / 2f;
            if (hi < lo) return float.NaN;
            float c0 = float.IsNaN(pref) ? (lo + hi) / 2f : Mathf.Clamp(pref, lo, hi);
            for (int step = 0; step < 200; step++)
            {
                float off = (step + 1) / 2 * 0.08f * (step % 2 == 0 ? 1 : -1);
                float c = c0 + off; if (c < lo - 0.001f || c > hi + 0.001f) { if (off > hi - c0 + 0.1f && -off > c0 - lo + 0.1f) break; continue; }
                if (Hit(run.blocked, c - w / 2, c + w / 2)) continue;
                if (tall && Hit(run.windows, c - w / 2, c + w / 2)) continue;
                var p = Pos(run, c, depth);
                if (RoofCeil(p.x, p.z) < rm.y + height + 0.05f) continue;
                var pb = Pos(run, c, 0.05f);
                if (RoofCeil(pb.x, pb.z) < rm.y + height * 0.7f) continue;
                return c;
            }
            return float.NaN;
        }

        static Vector3 Pos(WallRun run, float c, float depth)
        {
            switch (run.side)
            {
                case "F": return new Vector3(c, 0, run.edge + depth / 2f);
                case "B": return new Vector3(c, 0, run.edge - depth / 2f);
                case "L": return new Vector3(run.edge + depth / 2f, 0, c);
                default: return new Vector3(run.edge - depth / 2f, 0, c);
            }
        }
        static float Yaw(string side) => side == "F" ? 0f : side == "B" ? 180f : side == "L" ? 90f : -90f;

        /// <summary>Place an item against a wall: tries sides in order; draw() gets a kit pushed to the item's local frame (back at -z).</summary>
        bool Place(CRoom rm, Dictionary<string, WallRun> runs, string order, float w, float depth, float height, bool tall, System.Action draw, float pad = 0.06f)
        {
            foreach (char ch in order)
            {
                var run = runs[ch.ToString()];
                float c = Spot(rm, run, w, depth, height, tall);
                if (float.IsNaN(c)) continue;
                var p = Pos(run, c, depth); p.y = rm.y + (rm.lv == 0 ? 0.04f : 0f);
                furn.Push(p, Yaw(run.side)); deco.Push(p, Yaw(run.side));
                draw();
                furn.Pop(); deco.Pop();
                run.blocked.Add(new Vector2(c - w / 2 - pad, c + w / 2 + pad));
                return true;
            }
            return false;
        }

        void AtCentre(CRoom rm, CRect R, float yaw, System.Action draw)
        {
            var p = new Vector3(R.Cx, rm.y + (rm.lv == 0 ? 0.04f : 0f), R.Cd);
            furn.Push(p, yaw); deco.Push(p, yaw); draw(); furn.Pop(); deco.Pop();
        }

        string Order(CRect R, string first) // prefer long walls for big items
        {
            bool wide = R.x1 - R.x0 >= R.d1 - R.d0;
            string s = wide ? "BFLR" : "LRBF";
            if (first != null) s = first + s.Replace(first, "");
            return s;
        }

        void Furnish()
        {
            rng = new System.Random(pl.id.GetHashCode());
            foreach (var rm in P.rooms)
            {
                if (rm.lv > 1 || rm.lv < 0) continue;
                var R = rm.Main; if (R == null) continue;
                if (R.x1 - R.x0 < 0.7f || R.d1 - R.d0 < 0.7f) continue;
                var runs = Runs(rm, R);
                var fab = CotMats.fabrics[rng.Next(CotMats.fabrics.Length)];
                float rw = R.x1 - R.x0, rd = R.d1 - R.d0;
                bool wideR = rw >= rd;
                switch (rm.type)
                {
                    case "bed":
                    case "loft":
                        {
                            string style = rm.OptS("bed") ?? "iron"; bool master = rm.Opt("master");
                            float bw = master || style == "four" ? 1.45f : 1.0f;
                            if (!Place(rm, runs, Order(R, null), bw + 1.0f, 2.05f, style == "four" ? 2.15f : 1.2f, style == "four", () => Bed(bw, style, fab)))
                                Place(rm, runs, "LRBF", bw, 2.05f, 1.0f, false, () => Bed(bw, "iron", fab));
                            Place(rm, runs, Order(R, null), 1.1f, 0.6f, 1.95f, true, Wardrobe);
                            Place(rm, runs, Order(R, null), 1.0f, 0.5f, 0.95f, false, Chest);
                            Rug(rm, R, rw * 0.5f, rd * 0.45f, CotMats.rug2);
                            if (rm.Opt("trophies")) Place(rm, runs, "LRFB", 0.8f, 0.3f, 1.8f, true, () => Trophies());
                            break;
                        }
                    case "nursery":
                        Place(rm, runs, Order(R, null), 0.75f, 1.3f, 1.0f, false, Cot);
                        Place(rm, runs, Order(R, null), 0.9f, 0.5f, 0.9f, false, Chest);
                        Place(rm, runs, Order(R, null), 0.9f, 0.4f, 0.9f, false, RockingHorse);
                        Rug(rm, R, rw * 0.5f, rd * 0.5f, CotMats.rug2);
                        break;
                    case "kitchen":
                        {
                            Place(rm, runs, Order(R, null), 1.5f, 0.52f, 2.0f, true, () => Dresser(1.5f));
                            string sinkSide = null; foreach (var W in P.wins) if (W.room == rm.key && runs.ContainsKey(W.side)) { sinkSide = W.side; break; }
                            Place(rm, runs, Order(R, sinkSide), 1.3f, 0.62f, 1.0f, false, Sink);
                            if (rw > 2.4f && rd > 2.4f) AtCentre(rm, R, wideR ? 0f : 90f, () => Table(Mathf.Min(1.7f, Mathf.Max(rw, rd) - 1.6f), 0.85f, 4, CotMats.boards));
                            Place(rm, runs, Order(R, null), 0.9f, 0.5f, 0.95f, false, () => Cupboard(0.9f));
                            break;
                        }
                    case "dining":
                        AtCentre(rm, R, wideR ? 0f : 90f, () => Table(Mathf.Min(2.1f, Mathf.Max(rw, rd) - 1.8f), 0.95f, 6, CotMats.oak, true));
                        Place(rm, runs, Order(R, null), 1.6f, 0.5f, 0.95f, false, () => Sideboard(1.6f));
                        Rug(rm, R, rw * 0.7f, rd * 0.65f, CotMats.rug);
                        Pictures(rm, runs, 2);
                        break;
                    case "parlour":
                    case "sitting":
                    case "drawing":
                    case "snug":
                    case "studio":
                    case "garden":
                    case "roundel":
                        {
                            bool garden = rm.type == "garden";
                            CHearth hh = null; foreach (var h in P.hearths) if (h.room == rm.key) hh = h;
                            if (!garden) Place(rm, runs, Order(R, hh != null ? Opp(hh.side) : null), 1.9f, 0.88f, 0.9f, false, () => Sofa(1.9f, fab));
                            // two armchairs facing the fire (or the room centre)
                            for (int q = 0; q < 2; q++) Chair2(rm, R, hh, q, garden ? CotMats.canvas : CotMats.fabrics[(rng.Next(5))]);
                            Rug(rm, R, rw * 0.55f, rd * 0.5f, rng.Next(2) == 0 ? CotMats.rug : CotMats.rug2);
                            if (!garden) AtCentre(rm, R, 0, () => { furn.BoxB(CotMats.oak, new Vector3(0, 0, 0), new Vector3(0.9f, 0.42f, 0.5f)); deco.BoxB(CotMats.books, new Vector3(0.15f, 0.42f, 0), new Vector3(0.25f, 0.05f, 0.18f)); });
                            if (rm.type == "studio") Place(rm, runs, Order(R, null), 0.9f, 0.8f, 1.9f, false, Easel);
                            else if (!garden) Place(rm, runs, Order(R, null), 1.0f, 0.36f, 2.0f, true, Bookcase);
                            if (rm.Opt("piano")) Place(rm, runs, Order(R, null), 1.5f, 0.62f, 1.3f, false, Piano);
                            if (garden) { for (int q = 0; q < 4; q++) Place(rm, runs, "FBLR", 0.5f, 0.5f, 1.2f, false, Plant); }
                            else { Place(rm, runs, Order(R, null), 0.4f, 0.4f, 1.6f, false, Lamp); Pictures(rm, runs, 3); }
                            break;
                        }
                    case "bar":
                        {
                            // the bar along the back wall: counter with pumps, shelves of bottles and glasses behind, stools in front
                            float bw = Mathf.Min(4.2f, Mathf.Max(rw, rd) * 0.4f);
                            if (!Place(rm, runs, Order(R, "B"), bw, 1.75f, 2.0f, true, () => BarCounter(bw))) Place(rm, runs, Order(R, "B"), 3.2f, 1.75f, 2.0f, false, () => BarCounter(3.2f));
                            // old oak tables and chairs dotted about the floor, a threadbare carpet down the middle
                            var trng = new System.Random(rm.key.GetHashCode());
                            for (float tx = R.x0 + 2.0f; tx < R.x1 - 1.6f; tx += 2.9f)
                                for (float td = R.d0 + 1.7f; td < R.d1 - 3.3f; td += 2.6f)
                                {
                                    var tp = new Vector3(tx + (float)trng.NextDouble() * 0.5f, rm.y + (rm.lv == 0 ? 0.04f : 0f), td + (float)trng.NextDouble() * 0.4f);
                                    if (FireNear(rm, tp, 1.4f) || DoorNear(rm, tp, 1.6f)) continue;
                                    float yaw = (float)trng.NextDouble() * 20f - 10f;
                                    furn.Push(tp, yaw); deco.Push(tp, yaw); PubTable(trng.Next(3)); furn.Pop(); deco.Pop();
                                }
                            Rug(rm, R, rw * 0.5f, rd * 0.35f, CotMats.rug2);
                            // settles along the front wall under the windows
                            for (int q = 0; q < 3; q++) Place(rm, runs, "FLR", 1.7f, 0.55f, 1.2f, false, Settle);
                            Pictures(rm, runs, 5);
                            break;
                        }
                    case "study":
                        Place(rm, runs, Order(R, null), 1.4f, 1.4f, 1.0f, false, Desk);
                        Place(rm, runs, Order(R, null), 1.0f, 0.36f, 2.0f, true, Bookcase);
                        Place(rm, runs, Order(R, null), 1.0f, 0.36f, 2.0f, true, Bookcase);
                        if (rm.Opt("guns")) Place(rm, runs, Order(R, null), 0.85f, 0.42f, 1.9f, true, GunCabinet);
                        if (rm.Opt("safe")) Place(rm, runs, Order(R, null), 0.65f, 0.6f, 0.85f, false, Safe);
                        Chair2(rm, R, null, 0, CotMats.velvet);
                        Rug(rm, R, rw * 0.6f, rd * 0.55f, CotMats.rug);
                        Pictures(rm, runs, 2);
                        break;
                    case "bath":
                        Place(rm, runs, Order(R, null), 1.75f, 0.8f, 0.7f, false, Bath);
                        Place(rm, runs, Order(R, null), 0.6f, 0.5f, 1.0f, false, Basin);
                        Place(rm, runs, Order(R, null), 0.5f, 0.7f, 1.0f, false, WC);
                        break;
                    case "wc":
                        Place(rm, runs, Order(R, null), 0.5f, 0.7f, 1.0f, false, WC);
                        Place(rm, runs, Order(R, null), 0.5f, 0.4f, 1.0f, false, Basin);
                        break;
                    case "hall":
                    case "lobby":
                    case "passage":
                    case "landing":
                        {
                            if (rm.type == "hall" && rm.lv == 0) Place(rm, runs, Order(R, null), 0.5f, 0.32f, 2.1f, true, Clock);
                            Place(rm, runs, Order(R, null), 0.9f, 0.38f, 0.85f, false, Console);
                            if (rm.lv == 0) Place(rm, runs, Order(R, null), 0.45f, 0.45f, 1.8f, false, CoatStand);
                            Rug(rm, R, Mathf.Max(0.6f, rw - 0.7f), Mathf.Max(0.6f, rd - 0.7f), CotMats.rug);
                            if (rm.type == "landing" || rm.type == "hall") Pictures(rm, runs, 2);
                            break;
                        }
                    case "larder":
                    case "scullery":
                        Place(rm, runs, Order(R, null), Mathf.Min(2.0f, Mathf.Max(rw, rd) - 0.3f), 0.42f, 1.9f, true, () => Shelves(Mathf.Min(2.0f, Mathf.Max(rw, rd) - 0.3f)));
                        if (rm.type == "scullery") { Place(rm, runs, Order(R, null), 1.2f, 0.62f, 1.0f, false, Sink); Place(rm, runs, Order(R, null), 0.7f, 0.7f, 1.0f, false, Copper); }
                        else Place(rm, runs, Order(R, null), 1.0f, 0.5f, 0.85f, false, () => Cupboard(1.0f));
                        break;
                    case "boot":
                        Place(rm, runs, Order(R, null), 1.3f, 0.42f, 0.5f, false, Bench);
                        Place(rm, runs, Order(R, null), 1.0f, 0.12f, 1.8f, false, Pegs);
                        break;
                    case "dressing":
                        Place(rm, runs, Order(R, null), 1.1f, 0.6f, 1.95f, true, Wardrobe);
                        Place(rm, runs, Order(R, null), 1.0f, 0.5f, 0.95f, false, Chest);
                        break;
                    case "box":
                    case "attic":
                        Place(rm, runs, Order(R, null), 0.9f, 0.55f, 0.6f, false, Trunk);
                        Place(rm, runs, Order(R, null), 0.9f, 0.55f, 0.6f, false, Trunk);
                        Place(rm, runs, Order(R, null), 1.0f, 2.0f, 1.0f, false, () => Bed(1.0f, "iron", fab));
                        break;
                    case "forge":
                        Place(rm, runs, Order(R, null), 2.0f, 0.75f, 1.0f, false, Workbench);
                        Place(rm, runs, Order(R, null), 1.4f, 0.7f, 0.9f, false, () => Sofa(1.6f, CotMats.fabrics[3]));
                        Rug(rm, R, rw * 0.4f, rd * 0.4f, CotMats.rug);
                        break;
                }
            }
        }

        static string Opp(string s) => s == "F" ? "B" : s == "B" ? "F" : s == "L" ? "R" : "L";

        void Rug(CRoom rm, CRect R, float w, float d, Material m)
        {
            w = Mathf.Min(w, R.x1 - R.x0 - 0.4f); d = Mathf.Min(d, R.d1 - R.d0 - 0.4f); if (w < 0.5f || d < 0.5f) return;
            float y = rm.y + (rm.lv == 0 ? 0.045f : 0.005f);
            deco.QuadUV(m, new Vector3(R.Cx - w / 2, y, R.Cd - d / 2), new Vector3(R.Cx + w / 2, y, R.Cd - d / 2), new Vector3(R.Cx + w / 2, y, R.Cd + d / 2), new Vector3(R.Cx - w / 2, y, R.Cd + d / 2), Vector3.up,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
        }

        /// <summary>Armchair angled toward the hearth (or room centre).</summary>
        void Chair2(CRoom rm, CRect R, CHearth hh, int q, Material fab)
        {
            Vector3 target; Vector3 p;
            float y = rm.y + (rm.lv == 0 ? 0.04f : 0f);
            if (hh != null)
            {
                var n = SideN(hh.side); float m = (hh.a + hh.b) / 2f, e = RoomEdge(rm, hh.side, m) - (n.x + n.y) * hh.proj;
                target = n.x != 0 ? new Vector3(e, y, m) : new Vector3(m, y, e);
                Vector3 inward = new Vector3(-n.x, 0, -n.y), along = new Vector3(n.y, 0, n.x);
                p = target + inward * 1.5f + along * (q == 0 ? -1f : 1f) * 1.0f;
            }
            else
            {
                target = new Vector3(R.Cx, y, R.Cd);
                p = target + new Vector3(q == 0 ? -1f : 1f, 0, 0.9f);
            }
            if (!R.In(p.x, p.z, -0.45f)) return;
            Vector3 dir = target - p; dir.y = 0;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            furn.Push(new Vector3(p.x, y, p.z), yaw); deco.Push(new Vector3(p.x, y, p.z), yaw);
            Armchair(fab);
            furn.Pop(); deco.Pop();
        }

        void Pictures(CRoom rm, Dictionary<string, WallRun> runs, int count)
        {
            int placed = 0;
            foreach (var key in new[] { "B", "L", "R", "F" })
            {
                if (placed >= count) break;
                var run = runs[key];
                float c = float.NaN, w = 0.55f + (float)rng.NextDouble() * 0.35f;
                for (float t = (run.a0 + run.a1) / 2f, k = 0; k < 12; k++, t += (k % 2 == 0 ? 1 : -1) * k * 0.15f)
                {
                    if (t - w / 2 < run.a0 + 0.1f || t + w / 2 > run.a1 - 0.1f) continue;
                    if (Hit(run.windows, t - w / 2, t + w / 2)) continue;
                    var pp = Pos(run, t, 0.04f); if (RoofCeil(pp.x, pp.z) < rm.y + 2.0f) continue;
                    c = t; break;
                }
                if (float.IsNaN(c)) continue;
                var p = Pos(run, c, 0.04f); p.y = rm.y + 1.45f;
                deco.Push(p, Yaw(run.side));
                float h = w * (0.7f + (float)rng.NextDouble() * 0.4f);
                deco.Box(rng.Next(2) == 0 ? CotMats.brass : CotMats.oak, new Vector3(-w / 2, -h / 2, -0.02f), new Vector3(w / 2, h / 2, 0.02f));
                var art = CotMats.Paint(Color.HSVToRGB((float)rng.NextDouble(), 0.35f + (float)rng.NextDouble() * 0.3f, 0.35f + (float)rng.NextDouble() * 0.4f));
                deco.Box(art, new Vector3(-w / 2 + 0.05f, -h / 2 + 0.05f, 0.02f), new Vector3(w / 2 - 0.05f, h / 2 - 0.05f, 0.025f));
                deco.Pop();
                placed++;
            }
        }

        // ------------------------- pieces (local frame: floor centre, back at -z, front +z) -------------------------
        void Bed(float w, string style, Material fab)
        {
            float L = 2.0f, z0 = -1.0f;
            var frame = style == "iron" ? CotMats.iron : style == "brass" ? CotMats.brass : CotMats.oak;
            furn.Box(CotMats.oak, new Vector3(-w / 2, 0.18f, z0 + 0.05f), new Vector3(w / 2, 0.36f, z0 + L));
            furn.Box(CotMats.linen, new Vector3(-w / 2 + 0.03f, 0.36f, z0 + 0.08f), new Vector3(w / 2 - 0.03f, 0.58f, z0 + L - 0.03f));
            deco.Box(CotMats.quilt, new Vector3(-w / 2 - 0.01f, 0.4f, z0 + 0.65f), new Vector3(w / 2 + 0.01f, 0.61f, z0 + L + 0.01f));
            int pillows = w > 1.2f ? 2 : 1;
            for (int q = 0; q < pillows; q++) { float x = pillows == 1 ? 0 : (q == 0 ? -w / 4 : w / 4); deco.Sphere(CotMats.linen, new Vector3(x, 0.64f, z0 + 0.3f), new Vector3(w / (2.4f * pillows) + 0.05f, 0.08f, 0.2f), 10, 6); }
            foreach (float x in new[] { -w / 2, w / 2 })
            {
                furn.BoxB(frame, new Vector3(x, 0, z0 + 0.04f), new Vector3(0.07f, style == "four" ? 2.15f : 1.15f, 0.07f));
                furn.BoxB(frame, new Vector3(x, 0, z0 + L), new Vector3(0.07f, style == "four" ? 2.15f : 0.8f, 0.07f));
            }
            if (style == "four")
            {
                furn.Box(CotMats.oak, new Vector3(-w / 2, 0.6f, z0), new Vector3(w / 2, 1.3f, z0 + 0.06f));
                foreach (float x in new[] { -w / 2, w / 2 }) deco.Box(CotMats.oak, new Vector3(x - 0.04f, 2.1f, z0), new Vector3(x + 0.04f, 2.18f, z0 + L + 0.04f));
                foreach (float z in new[] { z0, z0 + L }) deco.Box(CotMats.oak, new Vector3(-w / 2, 2.1f, z - 0.04f), new Vector3(w / 2, 2.18f, z + 0.04f));
                deco.Box(fab, new Vector3(-w / 2 - 0.05f, 1.9f, z0 + L - 0.01f), new Vector3(w / 2 + 0.05f, 2.15f, z0 + L + 0.06f));
                foreach (float x in new[] { -w / 2, w / 2 }) deco.Box(fab, new Vector3(x - 0.05f, 1.9f, z0), new Vector3(x + 0.05f, 2.15f, z0 + L));
                deco.Box(fab, new Vector3(-w / 2 - 0.02f, 0.65f, z0 - 0.01f), new Vector3(-w / 2 + 0.22f, 2.1f, z0 + 0.06f));
                deco.Box(fab, new Vector3(w / 2 - 0.22f, 0.65f, z0 - 0.01f), new Vector3(w / 2 + 0.02f, 2.1f, z0 + 0.06f));
            }
            else
            {
                for (int q = 0; q < 7; q++) { float x = Mathf.Lerp(-w / 2 + 0.1f, w / 2 - 0.1f, q / 6f); deco.Box(frame, new Vector3(x - 0.012f, 0.55f, z0 + 0.03f), new Vector3(x + 0.012f, 1.1f, z0 + 0.06f)); }
                deco.Box(frame, new Vector3(-w / 2, 1.08f, z0 + 0.02f), new Vector3(w / 2, 1.13f, z0 + 0.07f));
                if (style == "brass") foreach (float x in new[] { -w / 2, w / 2 }) deco.Sphere(CotMats.brass, new Vector3(x, 1.2f, z0 + 0.04f), Vector3.one * 0.05f, 8, 4);
            }
            // bedside tables with a candle/lamp
            foreach (float x in new[] { -w / 2 - 0.32f, w / 2 + 0.32f })
            {
                furn.BoxB(CotMats.oak, new Vector3(x, 0, z0 + 0.25f), new Vector3(0.42f, 0.55f, 0.4f));
                deco.Cyl(CotMats.ceramic, new Vector3(x, 0.55f, z0 + 0.25f), 0.06f, 0.18f, 8);
                deco.Cyl(CotMats.linen, new Vector3(x, 0.73f, z0 + 0.25f), 0.12f, 0.14f, 10, true, 0.07f);
            }
        }

        void Wardrobe()
        {
            furn.BoxB(CotMats.oak, new Vector3(0, 0, 0), new Vector3(1.1f, 1.95f, 0.6f));
            deco.Box(CotMats.oak, new Vector3(-0.58f, 1.95f, -0.32f), new Vector3(0.58f, 2.02f, 0.32f));
            deco.Box(CotMats.boards, new Vector3(-0.5f, 0.15f, 0.3f), new Vector3(-0.02f, 1.8f, 0.31f));
            deco.Box(CotMats.boards, new Vector3(0.02f, 0.15f, 0.3f), new Vector3(0.5f, 1.8f, 0.31f));
            deco.Sphere(CotMats.brass, new Vector3(-0.06f, 1.0f, 0.33f), Vector3.one * 0.02f, 6, 4);
            deco.Sphere(CotMats.brass, new Vector3(0.06f, 1.0f, 0.33f), Vector3.one * 0.02f, 6, 4);
        }

        void Chest()
        {
            furn.BoxB(CotMats.oak, Vector3.zero, new Vector3(1.0f, 0.88f, 0.5f));
            for (int q = 0; q < 3; q++)
            {
                float y = 0.12f + q * 0.25f;
                deco.Box(CotMats.boards, new Vector3(-0.45f, y, 0.25f), new Vector3(0.45f, y + 0.21f, 0.26f));
                deco.Sphere(CotMats.brass, new Vector3(-0.25f, y + 0.1f, 0.28f), Vector3.one * 0.02f, 6, 4);
                deco.Sphere(CotMats.brass, new Vector3(0.25f, y + 0.1f, 0.28f), Vector3.one * 0.02f, 6, 4);
            }
            deco.Box(CotMats.oak, new Vector3(-0.3f, 0.88f, -0.24f), new Vector3(0.3f, 1.55f, -0.2f));
            deco.Box(CotMats.glassFrost, new Vector3(-0.25f, 0.93f, -0.2f), new Vector3(0.25f, 1.5f, -0.19f));
        }

        void Dresser(float w)
        {
            var pine = CotMats.Paint(new Color(0.56f, 0.64f, 0.55f));
            furn.BoxB(pine, Vector3.zero, new Vector3(w, 0.9f, 0.52f));
            deco.Box(CotMats.boards, new Vector3(-w / 2 - 0.03f, 0.9f, -0.28f), new Vector3(w / 2 + 0.03f, 0.94f, 0.28f));
            furn.Box(pine, new Vector3(-w / 2, 0.94f, -0.26f), new Vector3(w / 2, 1.95f, -0.04f));
            for (int s = 0; s < 3; s++)
            {
                float y = 1.15f + s * 0.27f;
                deco.Box(pine, new Vector3(-w / 2, y, -0.06f), new Vector3(w / 2, y + 0.02f, 0.02f));
                for (int q = 0; q < 6; q++)
                {
                    float x = Mathf.Lerp(-w / 2 + 0.15f, w / 2 - 0.15f, q / 5f);
                    deco.Push(Matrix4x4.TRS(new Vector3(x, y + 0.13f, -0.03f), Quaternion.Euler(90f, 0, 0), Vector3.one));
                    deco.Cyl(q % 2 == 0 ? CotMats.ceramic : CotMats.Paint(new Color(0.25f, 0.4f, 0.7f)), Vector3.zero, 0.1f, 0.015f, 12);
                    deco.Pop();
                }
            }
            for (int q = 0; q < 3; q++) deco.Box(CotMats.boards, new Vector3(-w / 2 + 0.05f + q * w / 3, 0.1f, 0.26f), new Vector3(-w / 2 + (q + 1) * w / 3 - 0.05f, 0.8f, 0.27f));
        }

        void Sink()
        {
            furn.BoxB(CotMats.Paint(new Color(0.85f, 0.83f, 0.76f)), Vector3.zero, new Vector3(1.3f, 0.82f, 0.62f));
            deco.Box(CotMats.oak, new Vector3(-0.66f, 0.82f, -0.32f), new Vector3(0.66f, 0.86f, 0.32f));
            furn.BoxB(CotMats.ceramic, new Vector3(-0.2f, 0.62f, 0.03f), new Vector3(0.62f, 0.3f, 0.5f));
            deco.Box(CotMats.soot, new Vector3(-0.48f, 0.9f, -0.19f), new Vector3(0.08f, 0.93f, 0.25f));
            deco.Cyl(CotMats.brass, new Vector3(-0.2f, 0.92f, -0.28f), 0.02f, 0.28f, 6);
            deco.Box(CotMats.brass, new Vector3(-0.22f, 1.18f, -0.28f), new Vector3(-0.18f, 1.2f, -0.12f));
            for (int q = 0; q < 6; q++) deco.Box(CotMats.boards, new Vector3(0.3f, 0.86f, -0.25f + q * 0.08f), new Vector3(0.6f, 0.88f, -0.22f + q * 0.08f));
        }

        void Table(float L, float w, int chairs, Material top, bool candles = false)
        {
            if (L < 0.8f) L = 0.8f;
            furn.Box(top, new Vector3(-L / 2, 0.72f, -w / 2), new Vector3(L / 2, 0.77f, w / 2));
            foreach (float x in new[] { -L / 2 + 0.08f, L / 2 - 0.08f }) foreach (float z in new[] { -w / 2 + 0.08f, w / 2 - 0.08f }) furn.BoxB(CotMats.oak, new Vector3(x, 0, z), new Vector3(0.07f, 0.72f, 0.07f));
            int perSide = chairs >= 6 ? 2 : 1;
            for (int s = -1; s <= 1; s += 2)
                for (int q = 0; q < perSide; q++)
                {
                    float x = perSide == 1 ? 0 : (q == 0 ? -L / 4 : L / 4);
                    furn.Push(new Vector3(x, 0, s * (w / 2 + 0.25f)), s > 0 ? 180f : 0f); deco.Push(new Vector3(x, 0, s * (w / 2 + 0.25f)), s > 0 ? 180f : 0f);
                    Windsor(); furn.Pop(); deco.Pop();
                }
            if (chairs >= 6) for (int s = -1; s <= 1; s += 2) { furn.Push(new Vector3(s * (L / 2 + 0.3f), 0, 0), s > 0 ? -90f : 90f); deco.Push(new Vector3(s * (L / 2 + 0.3f), 0, 0), s > 0 ? -90f : 90f); Windsor(); furn.Pop(); deco.Pop(); }
            if (candles) for (int q = -1; q <= 1; q += 2) { deco.Cyl(CotMats.brass, new Vector3(q * L / 5, 0.77f, 0), 0.05f, 0.25f, 8, true, 0.02f); deco.Cyl(CotMats.linen, new Vector3(q * L / 5, 1.02f, 0), 0.015f, 0.15f, 6); }
            else { deco.Cyl(CotMats.ceramic, new Vector3(0.1f, 0.77f, 0), 0.12f, 0.1f, 12, true, 0.15f); deco.Sphere(CotMats.flowers[1], new Vector3(0.1f, 0.88f, 0), new Vector3(0.1f, 0.05f, 0.1f), 8, 4); }
        }

        /// <summary>The bar: shelves of bottles and glasses against the wall, the serving gap, the counter with its pumps, stools out front (back at -z).</summary>
        void BarCounter(float w)
        {
            const float back = -0.875f;
            // back shelves
            furn.Box(CotMats.oak, new Vector3(-w / 2, 0f, back), new Vector3(w / 2, 0.9f, back + 0.45f));
            for (int sh = 0; sh < 3; sh++)
            {
                float y = 1.15f + sh * 0.32f;
                deco.Box(CotMats.oak, new Vector3(-w / 2, y - 0.03f, back), new Vector3(w / 2, y, back + 0.25f));
                for (float x = -w / 2 + 0.12f; x < w / 2 - 0.1f; x += 0.14f)
                {
                    var gm = ((int)(x * 7f + sh) % 3) switch { 0 => CotMats.glassClear, 1 => CotMats.lead, _ => CotMats.pewter };
                    deco.Cyl(gm, new Vector3(x, y, back + 0.12f), 0.035f, 0.24f + (sh == 0 ? 0.04f : 0f), 6, true, 0.012f);
                }
            }
            deco.Box(CotMats.glassFrost, new Vector3(-w / 2 + 0.1f, 0.95f, back), new Vector3(w / 2 - 0.1f, 2.05f, back + 0.02f)); // the old mirror behind the bottles
            // the counter: panelled front, dark top, brass rail
            float cz = 0.35f;
            furn.Box(CotMats.oak, new Vector3(-w / 2, 0f, cz), new Vector3(w / 2, 1.02f, cz + 0.5f));
            deco.Box(CotMats.boards, new Vector3(-w / 2 - 0.05f, 1.02f, cz - 0.05f), new Vector3(w / 2 + 0.05f, 1.08f, cz + 0.6f));
            for (float x = -w / 2 + 0.3f; x < w / 2 - 0.2f; x += 0.6f) deco.Box(CotMats.boards, new Vector3(x, 0.15f, cz + 0.5f), new Vector3(x + 0.45f, 0.9f, cz + 0.52f));
            deco.CylX(CotMats.brass, new Vector3(0f, 0.18f, cz + 0.68f), 0.025f, w, 6);
            // beer pumps
            for (int q = 0; q < 4; q++)
            {
                float x = -0.6f + q * 0.4f;
                deco.Cyl(CotMats.brass, new Vector3(x, 1.08f, cz + 0.2f), 0.04f, 0.12f, 8);
                deco.Box(CotMats.soot, new Vector3(x - 0.02f, 1.2f, cz + 0.18f), new Vector3(x + 0.02f, 1.5f, cz + 0.22f));
                deco.Box(CotMats.fabrics[q % CotMats.fabrics.Length], new Vector3(x - 0.035f, 1.32f, cz + 0.225f), new Vector3(x + 0.035f, 1.42f, cz + 0.235f));
            }
            // stools
            for (float x = -w / 2 + 0.5f; x < w / 2 - 0.3f; x += 0.85f)
            {
                deco.Cyl(CotMats.oak, new Vector3(x, 0f, cz + 0.95f), 0.025f, 0.72f, 6);
                furn.Cyl(CotMats.oak, new Vector3(x, 0.72f, cz + 0.95f), 0.18f, 0.05f, 10);
            }
        }

        /// <summary>A pub table: round or square, two to four chairs or stools round it, a glass or two and a beer mat.</summary>
        void PubTable(int kind)
        {
            if (kind == 0) { furn.Cyl(CotMats.oak, new Vector3(0, 0.7f, 0), 0.45f, 0.05f, 14); deco.Cyl(CotMats.oak, Vector3.zero, 0.05f, 0.7f, 8); deco.Cyl(CotMats.soot, new Vector3(0, 0f, 0), 0.25f, 0.03f, 10); }
            else { furn.Box(CotMats.oak, new Vector3(-0.42f, 0.7f, -0.38f), new Vector3(0.42f, 0.75f, 0.38f)); foreach (float x in new[] { -0.36f, 0.36f }) foreach (float z in new[] { -0.32f, 0.32f }) deco.BoxB(CotMats.oak, new Vector3(x, 0, z), new Vector3(0.06f, 0.7f, 0.06f)); }
            int seats = kind == 2 ? 4 : 3;
            for (int q = 0; q < seats; q++)
            {
                float a = q * 360f / seats + 20f; var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                var p = dir * 0.78f;
                furn.Push(p, a + 180f); deco.Push(p, a + 180f); Windsor(); furn.Pop(); deco.Pop();
            }
            deco.Cyl(CotMats.glassClear, new Vector3(0.12f, 0.75f, 0.05f), 0.04f, 0.15f, 8, true, 0.035f);
            deco.Cyl(CotMats.brass, new Vector3(0.12f, 0.75f, 0.05f), 0.036f, 0.11f, 8, true, 0.032f);
            deco.Box(CotMats.linen, new Vector3(-0.2f, 0.75f, -0.15f), new Vector3(-0.1f, 0.752f, -0.05f));
        }

        /// <summary>An old high-backed oak settle (back at -z).</summary>
        void Settle()
        {
            furn.Box(CotMats.oak, new Vector3(-0.85f, 0.42f, -0.25f), new Vector3(0.85f, 0.47f, 0.25f));
            furn.Box(CotMats.oak, new Vector3(-0.85f, 0f, -0.27f), new Vector3(0.85f, 1.25f, -0.2f));
            foreach (float x in new[] { -0.85f, 0.81f }) furn.Box(CotMats.oak, new Vector3(x, 0f, -0.27f), new Vector3(x + 0.04f, 0.75f, 0.25f));
            deco.Box(CotMats.cushion, new Vector3(-0.8f, 0.47f, -0.2f), new Vector3(0.8f, 0.53f, 0.22f));
        }

        /// <summary>Is p within r of a doorway on this floor (tables and chairs keep the way through clear)?</summary>
        bool DoorNear(CRoom rm, Vector3 p, float r)
        {
            foreach (var D in P.doors)
            {
                if (D.lv != rm.lv) continue;
                Vector3 c = D.line == "X" ? new Vector3(D.at, 0, (D.a + D.b) * 0.5f) : new Vector3((D.a + D.b) * 0.5f, 0, D.at);
                if (new Vector2(p.x - c.x, p.z - c.z).sqrMagnitude < r * r) return true;
            }
            return false;
        }

        bool FireNear(CRoom rm, Vector3 p, float r)
        {
            foreach (var h in P.hearths)
            {
                if (h.room != rm.key) continue;
                var R = rm.Main; float m = (h.a + h.b) * 0.5f;
                Vector3 c = h.side == "F" ? new Vector3(m, 0, R.d0) : h.side == "B" ? new Vector3(m, 0, R.d1) : h.side == "L" ? new Vector3(R.x0, 0, m) : new Vector3(R.x1, 0, m);
                if (new Vector2(p.x - c.x, p.z - c.z).magnitude < r + 1.2f) return true;
            }
            return false;
        }

        void Windsor()
        {
            furn.Box(CotMats.oak, new Vector3(-0.22f, 0.43f, -0.2f), new Vector3(0.22f, 0.47f, 0.2f));
            foreach (float x in new[] { -0.18f, 0.18f }) foreach (float z in new[] { -0.16f, 0.16f }) deco.BoxB(CotMats.oak, new Vector3(x, 0, z), new Vector3(0.035f, 0.43f, 0.035f));
            for (int q = 0; q < 5; q++) { float x = Mathf.Lerp(-0.18f, 0.18f, q / 4f); deco.Box(CotMats.oak, new Vector3(x - 0.012f, 0.47f, -0.2f), new Vector3(x + 0.012f, 0.95f, -0.17f)); }
            deco.Box(CotMats.oak, new Vector3(-0.22f, 0.93f, -0.21f), new Vector3(0.22f, 0.98f, -0.15f));
        }

        void Cupboard(float w)
        {
            furn.BoxB(CotMats.oak, Vector3.zero, new Vector3(w, 0.85f, 0.5f));
            deco.Box(CotMats.boards, new Vector3(-w / 2 + 0.05f, 0.1f, 0.25f), new Vector3(-0.02f, 0.78f, 0.26f));
            deco.Box(CotMats.boards, new Vector3(0.02f, 0.1f, 0.25f), new Vector3(w / 2 - 0.05f, 0.78f, 0.26f));
            for (int q = 0; q < 3; q++) deco.Cyl(CotMats.ceramic, new Vector3(-w / 3 + q * w / 3, 0.85f, 0), 0.07f, 0.18f + q * 0.04f, 10);
        }

        void Sideboard(float w)
        {
            furn.BoxB(CotMats.oak, Vector3.zero, new Vector3(w, 0.9f, 0.5f));
            for (int q = 0; q < 3; q++) deco.Box(CotMats.boards, new Vector3(-w / 2 + 0.05f + q * w / 3, 0.1f, 0.25f), new Vector3(-w / 2 + (q + 1) * w / 3 - 0.05f, 0.8f, 0.26f));
            deco.Cyl(CotMats.pewter, new Vector3(-0.4f, 0.9f, 0), 0.15f, 0.02f, 12);
            deco.Cyl(CotMats.glassClear, new Vector3(0.3f, 0.9f, 0), 0.07f, 0.28f, 10, true, 0.03f);
            deco.Box(CotMats.oak, new Vector3(-0.5f, 1.2f, -0.25f), new Vector3(0.5f, 1.85f, -0.22f));
            deco.Box(CotMats.glassFrost, new Vector3(-0.45f, 1.25f, -0.22f), new Vector3(0.45f, 1.8f, -0.215f));
        }

        void Sofa(float w, Material fab)
        {
            furn.Box(fab, new Vector3(-w / 2, 0.1f, -0.42f), new Vector3(w / 2, 0.45f, 0.42f));
            furn.Box(fab, new Vector3(-w / 2, 0.45f, -0.44f), new Vector3(w / 2, 0.9f, -0.22f));
            foreach (float x in new[] { -w / 2, w / 2 - 0.18f }) furn.Box(fab, new Vector3(x, 0.45f, -0.42f), new Vector3(x + 0.18f, 0.66f, 0.42f));
            for (int q = 0; q < 2; q++) deco.Box(fab, new Vector3(-w / 2 + 0.2f + q * (w - 0.4f) / 2f, 0.45f, -0.22f), new Vector3(-w / 2 + 0.18f + (q + 1) * (w - 0.4f) / 2f, 0.55f, 0.4f));
            deco.Sphere(CotMats.check, new Vector3(-w / 2 + 0.35f, 0.65f, -0.15f), new Vector3(0.18f, 0.16f, 0.07f), 8, 5);
            foreach (float x in new[] { -w / 2 + 0.06f, w / 2 - 0.06f }) foreach (float z in new[] { -0.36f, 0.36f }) deco.BoxB(CotMats.oak, new Vector3(x, 0, z), new Vector3(0.05f, 0.1f, 0.05f));
        }

        void Armchair(Material fab)
        {
            furn.Box(fab, new Vector3(-0.42f, 0.1f, -0.4f), new Vector3(0.42f, 0.45f, 0.4f));
            furn.Box(fab, new Vector3(-0.42f, 0.45f, -0.42f), new Vector3(0.42f, 1.05f, -0.24f));
            foreach (float x in new[] { -0.42f, 0.27f }) furn.Box(fab, new Vector3(x, 0.45f, -0.4f), new Vector3(x + 0.15f, 0.66f, 0.4f));
            deco.Box(fab, new Vector3(-0.27f, 0.45f, -0.24f), new Vector3(0.27f, 0.55f, 0.38f));
            // wings
            foreach (float x in new[] { -0.42f, 0.36f }) deco.Box(fab, new Vector3(x, 0.66f, -0.42f), new Vector3(x + 0.06f, 1.0f, -0.1f));
            foreach (float x in new[] { -0.36f, 0.36f }) foreach (float z in new[] { -0.34f, 0.34f }) deco.BoxB(CotMats.oak, new Vector3(x, 0, z), new Vector3(0.05f, 0.1f, 0.05f));
        }

        void Bookcase()
        {
            furn.BoxB(CotMats.oak, new Vector3(0, 0, -0.02f), new Vector3(1.0f, 2.0f, 0.32f));
            deco.QuadUV(CotMats.books, new Vector3(-0.45f, 0.1f, 0.15f), new Vector3(0.45f, 0.1f, 0.15f), new Vector3(0.45f, 1.9f, 0.15f), new Vector3(-0.45f, 1.9f, 0.15f), Vector3.forward,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 2f), new Vector2(0, 2f));
            for (int s = 0; s < 5; s++) deco.Box(CotMats.oak, new Vector3(-0.48f, 0.08f + s * 0.45f, 0.13f), new Vector3(0.48f, 0.11f + s * 0.45f, 0.16f));
        }

        void Desk()
        {
            furn.Box(CotMats.oak, new Vector3(-0.65f, 0.72f, -0.62f), new Vector3(0.65f, 0.77f, 0.02f));
            foreach (float x in new[] { -0.45f, 0.45f }) furn.Box(CotMats.oak, new Vector3(x - 0.18f, 0f, -0.6f), new Vector3(x + 0.18f, 0.72f, 0f));
            deco.Box(CotMats.velvet, new Vector3(-0.4f, 0.77f, -0.5f), new Vector3(0.4f, 0.775f, -0.1f));
            deco.Cyl(CotMats.brass, new Vector3(0.5f, 0.77f, -0.45f), 0.08f, 0.02f, 10);
            deco.Cyl(CotMats.brass, new Vector3(0.5f, 0.79f, -0.45f), 0.012f, 0.35f, 6);
            deco.Cyl(CotMats.velvet, new Vector3(0.5f, 1.08f, -0.45f), 0.14f, 0.1f, 10, true, 0.06f);
            deco.Box(CotMats.books, new Vector3(-0.55f, 0.77f, -0.55f), new Vector3(-0.35f, 0.83f, -0.35f));
            furn.Push(new Vector3(0, 0, 0.45f), 180f); deco.Push(new Vector3(0, 0, 0.45f), 180f); Windsor(); furn.Pop(); deco.Pop();
        }

        void GunCabinet()
        {
            furn.BoxB(CotMats.oak, Vector3.zero, new Vector3(0.85f, 1.9f, 0.42f));
            deco.Box(CotMats.soot, new Vector3(-0.36f, 0.15f, 0.2f), new Vector3(0.36f, 1.75f, 0.205f));
            for (int q = 0; q < 4; q++)
            {
                float x = -0.27f + q * 0.18f;
                deco.Box(CotMats.oak, new Vector3(x - 0.025f, 0.2f, 0.17f), new Vector3(x + 0.025f, 0.75f, 0.2f));
                deco.Box(CotMats.iron, new Vector3(x - 0.012f, 0.75f, 0.18f), new Vector3(x + 0.012f, 1.65f, 0.195f));
            }
            deco.Box(CotMats.glassClear, new Vector3(-0.38f, 0.12f, 0.215f), new Vector3(0.38f, 1.78f, 0.22f));
        }

        void Safe()
        {
            furn.BoxB(CotMats.iron, Vector3.zero, new Vector3(0.65f, 0.85f, 0.6f));
            deco.Cyl(CotMats.brass, new Vector3(0.1f, 0.5f, 0.3f), 0.06f, 0.03f, 12);
            deco.Box(CotMats.brass, new Vector3(-0.2f, 0.45f, 0.3f), new Vector3(-0.15f, 0.6f, 0.33f));
        }

        void Piano()
        {
            furn.BoxB(CotMats.Paint(new Color(0.12f, 0.07f, 0.05f)), new Vector3(0, 0, -0.08f), new Vector3(1.5f, 1.3f, 0.45f));
            furn.Box(CotMats.Paint(new Color(0.12f, 0.07f, 0.05f)), new Vector3(-0.75f, 0.68f, 0.12f), new Vector3(0.75f, 0.74f, 0.32f));
            deco.Box(CotMats.ceramic, new Vector3(-0.7f, 0.74f, 0.14f), new Vector3(0.7f, 0.76f, 0.3f));
            for (int q = 0; q < 36; q++) { if (q % 7 == 2 || q % 7 == 6) continue; float x = -0.68f + q * 0.038f; deco.Box(CotMats.soot, new Vector3(x, 0.76f, 0.14f), new Vector3(x + 0.018f, 0.775f, 0.22f)); }
            deco.Cyl(CotMats.brass, new Vector3(-0.5f, 1.3f, -0.1f), 0.05f, 0.3f, 8, true, 0.02f);
        }

        void Clock()
        {
            furn.BoxB(CotMats.oak, Vector3.zero, new Vector3(0.48f, 1.7f, 0.3f));
            furn.BoxB(CotMats.oak, new Vector3(0, 1.7f, 0), new Vector3(0.52f, 0.45f, 0.32f));
            deco.Push(Matrix4x4.TRS(new Vector3(0, 1.92f, 0.16f), Quaternion.Euler(90f, 0, 0), Vector3.one));
            deco.Cyl(CotMats.ceramic, Vector3.zero, 0.15f, 0.01f, 16);
            deco.Pop();
            deco.Box(CotMats.soot, new Vector3(-0.005f, 1.88f, 0.172f), new Vector3(0.005f, 2.02f, 0.175f));
            deco.Box(CotMats.glassClear, new Vector3(-0.12f, 0.6f, 0.15f), new Vector3(0.12f, 1.4f, 0.155f));
            deco.Cyl(CotMats.brass, new Vector3(0, 0.75f, 0.1f), 0.06f, 0.02f, 10);
        }

        void Console()
        {
            furn.Box(CotMats.oak, new Vector3(-0.45f, 0.78f, -0.19f), new Vector3(0.45f, 0.82f, 0.19f));
            foreach (float x in new[] { -0.4f, 0.4f }) foreach (float z in new[] { -0.14f, 0.14f }) furn.BoxB(CotMats.oak, new Vector3(x, 0, z), new Vector3(0.04f, 0.78f, 0.04f));
            deco.Cyl(CotMats.ceramic, new Vector3(-0.2f, 0.82f, 0), 0.08f, 0.22f, 10, true, 0.05f);
            deco.Sphere(CotMats.flowers[rng.Next(5)], new Vector3(-0.2f, 1.08f, 0), new Vector3(0.12f, 0.1f, 0.12f), 8, 5);
            deco.Box(CotMats.oak, new Vector3(-0.3f, 1.2f, -0.19f), new Vector3(0.3f, 1.85f, -0.16f));
            deco.Box(CotMats.glassFrost, new Vector3(-0.26f, 1.24f, -0.16f), new Vector3(0.26f, 1.81f, -0.155f));
        }

        void CoatStand()
        {
            deco.Cyl(CotMats.oak, Vector3.zero, 0.2f, 0.04f, 10);
            furn.Cyl(CotMats.oak, Vector3.zero, 0.03f, 1.8f, 8);
            deco.Sphere(CotMats.fabrics[2], new Vector3(0.1f, 1.45f, 0), new Vector3(0.18f, 0.35f, 0.12f), 8, 6);
            deco.Sphere(CotMats.velvet, new Vector3(0, 1.82f, 0), new Vector3(0.12f, 0.05f, 0.12f), 8, 4);
        }

        void Bath()
        {
            furn.Box(CotMats.ceramic, new Vector3(-0.85f, 0.15f, -0.38f), new Vector3(0.85f, 0.62f, 0.38f));
            deco.Box(CotMats.Paint(new Color(0.75f, 0.85f, 0.9f)), new Vector3(-0.78f, 0.6f, -0.31f), new Vector3(0.78f, 0.625f, 0.31f));
            foreach (float x in new[] { -0.75f, 0.75f }) foreach (float z in new[] { -0.3f, 0.3f }) deco.Sphere(CotMats.brass, new Vector3(x, 0.08f, z), new Vector3(0.05f, 0.08f, 0.05f), 6, 4);
            deco.Cyl(CotMats.brass, new Vector3(-0.8f, 0.62f, -0.1f), 0.02f, 0.2f, 6);
            deco.Cyl(CotMats.brass, new Vector3(-0.8f, 0.62f, 0.1f), 0.02f, 0.2f, 6);
            deco.Box(CotMats.linen, new Vector3(0.2f, 0.62f, -0.4f), new Vector3(0.6f, 0.95f, -0.37f));
        }

        void Basin()
        {
            furn.Cyl(CotMats.ceramic, new Vector3(0, 0, 0), 0.1f, 0.78f, 10, true, 0.08f);
            furn.Box(CotMats.ceramic, new Vector3(-0.27f, 0.78f, -0.22f), new Vector3(0.27f, 0.88f, 0.2f));
            deco.Cyl(CotMats.brass, new Vector3(0, 0.88f, -0.16f), 0.015f, 0.14f, 6);
            deco.Box(CotMats.oak, new Vector3(-0.25f, 1.2f, -0.25f), new Vector3(0.25f, 1.75f, -0.22f));
            deco.Box(CotMats.glassFrost, new Vector3(-0.21f, 1.24f, -0.22f), new Vector3(0.21f, 1.71f, -0.215f));
        }

        void WC()
        {
            furn.Cyl(CotMats.ceramic, new Vector3(0, 0, 0.05f), 0.17f, 0.4f, 12, true, 0.2f);
            deco.Cyl(CotMats.oak, new Vector3(0, 0.4f, 0.05f), 0.21f, 0.03f, 12);
            furn.Box(CotMats.ceramic, new Vector3(-0.22f, 0.4f, -0.34f), new Vector3(0.22f, 0.85f, -0.16f));
            deco.Cyl(CotMats.brass, new Vector3(0.18f, 0.85f, -0.25f), 0.01f, 0.25f, 4);
        }

        void Shelves(float w)
        {
            furn.Box(CotMats.boards, new Vector3(-w / 2, 0, -0.21f), new Vector3(w / 2, 1.9f, -0.18f));
            for (int s = 0; s < 4; s++)
            {
                float y = 0.4f + s * 0.45f;
                furn.Box(s == 0 ? CotMats.stone : CotMats.boards, new Vector3(-w / 2, y, -0.2f), new Vector3(w / 2, y + 0.03f, 0.2f));
                int n = Mathf.FloorToInt(w / 0.18f);
                for (int q = 0; q < n; q++)
                {
                    float x = -w / 2 + 0.1f + q * 0.18f; int kind = (q * 7 + s * 3) % 4;
                    if (kind == 0) deco.Cyl(CotMats.glassClear, new Vector3(x, y + 0.03f, 0), 0.05f, 0.16f, 8);
                    else if (kind == 1) deco.Cyl(CotMats.terracotta, new Vector3(x, y + 0.03f, 0), 0.06f, 0.2f, 8, true, 0.05f);
                    else if (kind == 2) deco.Cyl(CotMats.ceramic, new Vector3(x, y + 0.03f, 0), 0.06f, 0.12f, 8);
                }
            }
        }

        void Copper()
        {
            furn.BoxB(CotMats.brick, Vector3.zero, new Vector3(0.7f, 0.8f, 0.7f));
            deco.Cyl(CotMats.copper, new Vector3(0, 0.8f, 0), 0.28f, 0.08f, 14);
            deco.Cyl(CotMats.oak, new Vector3(0, 0.88f, 0), 0.3f, 0.03f, 14);
        }

        void Bench()
        {
            furn.Box(CotMats.oak, new Vector3(-0.65f, 0.42f, -0.2f), new Vector3(0.65f, 0.47f, 0.2f));
            foreach (float x in new[] { -0.55f, 0.55f }) furn.Box(CotMats.oak, new Vector3(x - 0.03f, 0, -0.18f), new Vector3(x + 0.03f, 0.42f, 0.18f));
            for (int q = 0; q < 3; q++) { float x = -0.4f + q * 0.4f; deco.Box(CotMats.soot, new Vector3(x - 0.06f, 0, 0.05f), new Vector3(x + 0.06f, 0.3f, 0.2f)); deco.Box(CotMats.soot, new Vector3(x - 0.06f, 0, 0.2f), new Vector3(x + 0.06f, 0.08f, 0.35f)); }
        }

        void Pegs()
        {
            deco.Box(CotMats.oak, new Vector3(-0.5f, 1.55f, -0.06f), new Vector3(0.5f, 1.65f, -0.03f));
            for (int q = 0; q < 4; q++)
            {
                float x = -0.36f + q * 0.24f;
                deco.Box(CotMats.oak, new Vector3(x - 0.015f, 1.58f, -0.03f), new Vector3(x + 0.015f, 1.61f, 0.06f));
                if (q % 2 == 0) deco.Sphere(q == 0 ? CotMats.fabrics[1] : CotMats.fabrics[3], new Vector3(x, 1.25f, 0.06f), new Vector3(0.2f, 0.4f, 0.08f), 8, 6);
            }
        }

        void Trunk()
        {
            furn.BoxB(CotMats.Paint(new Color(0.3f, 0.2f, 0.12f)), Vector3.zero, new Vector3(0.9f, 0.5f, 0.55f));
            foreach (float x in new[] { -0.3f, 0.3f }) deco.Box(CotMats.brass, new Vector3(x - 0.02f, 0, -0.28f), new Vector3(x + 0.02f, 0.51f, 0.28f));
        }

        void Cot()
        {
            furn.Box(CotMats.white, new Vector3(-0.35f, 0.3f, -0.62f), new Vector3(0.35f, 0.4f, 0.62f));
            deco.Box(CotMats.quilt, new Vector3(-0.32f, 0.4f, -0.3f), new Vector3(0.32f, 0.47f, 0.6f));
            for (int q = 0; q <= 12; q++) { float z = Mathf.Lerp(-0.62f, 0.62f, q / 12f); foreach (float x in new[] { -0.35f, 0.35f }) deco.Box(CotMats.white, new Vector3(x - 0.012f, 0, z - 0.012f), new Vector3(x + 0.012f, 0.95f, z + 0.012f)); }
            foreach (float x in new[] { -0.35f, 0.35f }) deco.Box(CotMats.white, new Vector3(x - 0.03f, 0.92f, -0.64f), new Vector3(x + 0.03f, 0.97f, 0.64f));
        }

        void RockingHorse()
        {
            var dap = CotMats.Paint(new Color(0.85f, 0.8f, 0.72f));
            foreach (float x in new[] { -0.15f, 0.15f }) deco.Box(CotMats.oak, new Vector3(x - 0.02f, 0, -0.45f), new Vector3(x + 0.02f, 0.08f, 0.45f));
            furn.Sphere(dap, new Vector3(0, 0.6f, 0), new Vector3(0.14f, 0.15f, 0.35f), 10, 6);
            deco.Sphere(dap, new Vector3(0, 0.85f, 0.3f), new Vector3(0.08f, 0.14f, 0.12f), 8, 5);
            foreach (float x in new[] { -0.1f, 0.1f }) foreach (float z in new[] { -0.22f, 0.22f }) deco.Box(dap, new Vector3(x - 0.025f, 0.08f, z - 0.025f), new Vector3(x + 0.025f, 0.5f, z + 0.025f));
            deco.Box(CotMats.cushion, new Vector3(-0.1f, 0.72f, -0.1f), new Vector3(0.1f, 0.76f, 0.1f));
        }

        void Easel()
        {
            Beam(deco, CotMats.oak, new Vector3(-0.3f, 0, 0.1f), new Vector3(-0.05f, 1.9f, -0.05f), 0.04f, 0.04f);
            Beam(deco, CotMats.oak, new Vector3(0.3f, 0, 0.1f), new Vector3(0.05f, 1.9f, -0.05f), 0.04f, 0.04f);
            Beam(deco, CotMats.oak, new Vector3(0, 0, -0.4f), new Vector3(0, 1.8f, -0.05f), 0.04f, 0.04f);
            furn.Box(CotMats.canvas, new Vector3(-0.35f, 0.8f, 0.02f), new Vector3(0.35f, 1.55f, 0.05f));
            deco.Box(CotMats.Paint(new Color(0.4f, 0.55f, 0.7f)), new Vector3(-0.3f, 1.15f, 0.051f), new Vector3(0.3f, 1.5f, 0.052f));
            deco.Box(CotMats.Paint(new Color(0.35f, 0.5f, 0.25f)), new Vector3(-0.3f, 0.85f, 0.051f), new Vector3(0.3f, 1.15f, 0.052f));
        }

        void Workbench()
        {
            furn.Box(CotMats.oak, new Vector3(-1f, 0.85f, -0.37f), new Vector3(1f, 0.92f, 0.37f));
            foreach (float x in new[] { -0.9f, 0.9f }) foreach (float z in new[] { -0.3f, 0.3f }) furn.BoxB(CotMats.oak, new Vector3(x, 0, z), new Vector3(0.08f, 0.85f, 0.08f));
            deco.Box(CotMats.iron, new Vector3(0.6f, 0.92f, 0.1f), new Vector3(0.8f, 1.05f, 0.3f));
            deco.Box(CotMats.iron, new Vector3(-0.6f, 0.92f, -0.2f), new Vector3(-0.2f, 0.95f, -0.15f));
            deco.Box(CotMats.oak, new Vector3(-0.9f, 1.3f, -0.37f), new Vector3(0.9f, 1.33f, -0.3f));
        }

        void Plant()
        {
            furn.Cyl(CotMats.terracotta, Vector3.zero, 0.18f, 0.35f, 10, true, 0.22f);
            deco.Sphere(CotMats.leaf, new Vector3(0, 0.65f, 0), new Vector3(0.3f, 0.35f, 0.3f), 8, 6);
        }

        void Lamp()
        {
            deco.Cyl(CotMats.brass, Vector3.zero, 0.15f, 0.03f, 10);
            furn.Cyl(CotMats.brass, Vector3.zero, 0.02f, 1.45f, 6);
            deco.Cyl(CotMats.Paint(new Color(0.95f, 0.85f, 0.65f)), new Vector3(0, 1.3f, 0), 0.22f, 0.28f, 12, false, 0.12f);
        }

        void Trophies()
        {
            deco.Box(CotMats.oak, new Vector3(-0.4f, 1.4f, -0.05f), new Vector3(0.4f, 1.8f, -0.02f));
            for (int q = 0; q < 3; q++) deco.Cyl(CotMats.brass, new Vector3(-0.25f + q * 0.25f, 0.95f, 0), 0.05f, 0.25f, 8, true, 0.09f);
            furn.BoxB(CotMats.oak, Vector3.zero, new Vector3(0.8f, 0.95f, 0.3f));
        }
    }
}
