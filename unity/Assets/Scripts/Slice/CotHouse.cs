using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// Builds one Abbots Wend cottage from its plan (ported from the browser build's Cot.* system):
    /// thick limewashed stone walls carved with real door/window/fireplace openings, two floors with stairs,
    /// thatch/tile/slate roofs with eyebrow dormers and hips, chimneys, porches, leaded windows, and furnished rooms.
    /// Local frame: x along the front, y up, z = plan depth d (front wall at d=0 facing -z).
    /// </summary>
    public partial class CotHouse
    {
        public readonly CPlot pl; public readonly CPlan P;
        public Transform root;
        public Vector3 FrontDoorLocal; // outside the front door, for paths
        public readonly List<CRect> zones = new List<CRect>();
        MeshKit solid, deco, roofKit;
        Material limeM, frameM, doorM, strawM;
        float Y1 => P.Y1;

        public CotHouse(CPlot plot, CPlan plan) { pl = plot; P = plan; }

        /// <summary>Centre of the largest ground-floor living room (local), for debug teleports.</summary>
        public Vector3 MainRoomLocal()
        {
            CRect best = null;
            foreach (var r in P.rooms) if (r.lv == 0 && r.type != "hall" && r.type != "passage" && r.type != "lobby") { var m = r.Main; if (m != null && (best == null || m.Area > best.Area)) best = m; }
            return best == null ? FrontDoorLocal : new Vector3(best.Cx, 0.1f, best.d0 + 0.8f);
        }

        // ======================= roofs =======================
        class RPiece
        {
            public string kind, axis, mat; public CWing w;
            public float a0, a1, b0, b1, eave, kk, R, k, up, T, cx, cd, r, ov;
            public bool hip0, hip1; public float h0y, h0k, h1y, h1k;
            public List<float[]> brows; public float[] reg;
        }
        readonly List<RPiece> roofs = new List<RPiece>();

        static float TCeil(string mat) => mat == "thatch" ? 0.45f : mat == "tiles" ? 0.3f : mat == "slate" ? 0.26f : mat == "glass" ? 0.08f : 0.4f;

        static float Brow(float[] B, float t, float s)
        {
            float A = B.Length > 3 && B[3] > 0 ? B[3] : 0.62f;
            float hw = (B[2] - B[1]) / 2f, tc = (B[1] + B[2]) / 2f, dt = Mathf.Abs(t - tc), a = hw + 0.12f, e = hw + 0.95f;
            float S = dt <= a ? 1f : dt >= e ? 0f : 0.5f + 0.5f * Mathf.Cos(Mathf.PI * (dt - a) / (e - a));
            if (S == 0f) return 0f;
            float E = s <= 0 ? 1f : s >= 1.9f ? 0f : 0.5f + 0.5f * Mathf.Cos(Mathf.PI * s / 1.9f);
            return A * S * E;
        }

        float PieceH(RPiece R, float x, float d, bool noReg)
        {
            if (R.kind == "cone")
            {
                float rr = Mathf.Sqrt((x - R.cx) * (x - R.cx) + (d - R.cd) * (d - R.cd));
                if (rr > R.r + R.ov && !noReg) return float.NegativeInfinity;
                return R.eave + R.k * (R.r - rr);
            }
            var g = R.reg;
            if (!noReg && (x < g[0] || x > g[1] || d < g[2] || d > g[3])) return float.NegativeInfinity;
            bool ax = R.axis == "x"; float t = ax ? x : d, b = ax ? d : x;
            if (R.kind == "lean") { float s = R.up > 0 ? b - R.b0 : R.b1 - b; return R.eave + R.k * s; }
            float s0 = b - R.b0, s1 = R.b1 - b;
            float h = R.eave + R.kk * Mathf.Min(s0, s1);
            if (R.brows != null) foreach (var B in R.brows) { if (B[0] < 0 && s0 < 2.2f) h += Brow(B, t, s0); else if (B[0] > 0 && s1 < 2.2f) h += Brow(B, t, s1); }
            if (R.hip0) h = Mathf.Min(h, R.h0y + R.h0k * (t - R.a0));
            if (R.hip1) h = Mathf.Min(h, R.h1y + R.h1k * (R.a1 - t));
            return h;
        }

        RPiece TopAt(float x, float d, out float h)
        {
            h = float.NegativeInfinity; RPiece best = null;
            foreach (var R in roofs) { float v = PieceH(R, x, d, false); if (v > h) { h = v; best = R; } }
            return best;
        }
        public float RoofH(float x, float d) { TopAt(x, d, out float h); return h; }
        public float RoofCeil(float x, float d) { var R = TopAt(x, d, out float h); return R == null ? 99f : h - TCeil(R.mat); }

        static bool Abuts(CRect a, CRect b, string side)
        {
            const float e = 0.06f;
            bool ovD = Mathf.Min(a.d1, b.d1) - Mathf.Max(a.d0, b.d0) > 0.3f, ovX = Mathf.Min(a.x1, b.x1) - Mathf.Max(a.x0, b.x0) > 0.3f;
            switch (side)
            {
                case "L": return ovD && Mathf.Abs(b.x1 - a.x0) < e;
                case "R": return ovD && Mathf.Abs(b.x0 - a.x1) < e;
                case "F": return ovX && Mathf.Abs(b.d1 - a.d0) < e;
                default: return ovX && Mathf.Abs(b.d0 - a.d1) < e;
            }
        }

        void MakeRoofs()
        {
            foreach (var w in P.wings)
            {
                string mat = w.mat ?? "thatch";
                float ov = w.ov >= 0 ? w.ov : (mat == "thatch" ? 0.45f : 0.25f), vg = w.vg >= 0 ? w.vg : (mat == "thatch" ? 0.32f : 0.18f);
                if (w.circle != null) { roofs.Add(new RPiece { kind = "cone", w = w, mat = mat, cx = w.circle.cx, cd = w.circle.cd, r = w.circle.r, ov = ov, eave = w.eave, k = w.kk > 0 ? w.kk : 1.9f, T = 0.3f }); continue; }
                if (w.roof == "none") continue;
                var r = w.r;
                bool Ab(string side) { foreach (var v in P.wings) if (v != w && v.circle == null && Abuts(w.r, v.r, side) && v.eave > w.eave + 0.3f) return true; return false; }
                var R = new RPiece { w = w, mat = mat, T = mat == "thatch" ? 0.42f : 0.12f, axis = w.ridge ?? "x" };
                bool ax = R.axis == "x";
                if (w.roof == "lean")
                {
                    R.kind = "lean"; R.b0 = ax ? r.d0 : r.x0; R.b1 = ax ? r.d1 : r.x1; R.up = w.up; R.eave = w.eave; R.k = w.kk > 0 ? w.kk : 0.6f;
                }
                else
                {
                    R.kind = "prism";
                    R.a0 = ax ? r.x0 : r.d0; R.a1 = ax ? r.x1 : r.d1; R.b0 = ax ? r.d0 : r.x0; R.b1 = ax ? r.d1 : r.x1;
                    R.eave = w.eave; R.kk = w.kk > 0 ? w.kk : (P.kk > 0 ? P.kk : 1.05f); R.R = R.eave + R.kk * (R.b1 - R.b0) / 2f;
                    if (w.hip != null)
                    {
                        if (w.hip.Length > 0 && w.hip[0] != 0) { R.hip0 = true; if (w.hip[0] == 1f) { R.h0y = R.eave; R.h0k = R.kk * (w.hipK > 0 ? w.hipK : 1f); } else { R.h0y = R.R - w.hip[0]; R.h0k = R.kk * (w.hipK > 0 ? w.hipK : 1.3f); } }
                        if (w.hip.Length > 1 && w.hip[1] != 0) { R.hip1 = true; if (w.hip[1] == 1f) { R.h1y = R.eave; R.h1k = R.kk * (w.hipK > 0 ? w.hipK : 1f); } else { R.h1y = R.R - w.hip[1]; R.h1k = R.kk * (w.hipK > 0 ? w.hipK : 1.3f); } }
                    }
                    R.brows = w.brows;
                }
                float sL = Ab("L") ? 0 : ax ? vg : ov, sR = Ab("R") ? 0 : ax ? vg : ov, sF = Ab("F") ? 0 : ax ? ov : vg, sB = Ab("B") ? 0 : ax ? ov : vg;
                if (R.hip0) { if (ax) sL = Ab("L") ? 0 : ov; else sF = Ab("F") ? 0 : ov; }
                if (R.hip1) { if (ax) sR = Ab("R") ? 0 : ov; else sB = Ab("B") ? 0 : ov; }
                R.reg = w.roofR != null ? (float[])w.roofR.Clone() : new[] { r.x0 - sL, r.x1 + sR, r.d0 - sF, r.d1 + sB };
                if (w.roofExt != null) { var e = w.roofExt; R.reg = new[] { Mathf.Min(R.reg[0], e[0]), Mathf.Max(R.reg[1], e[1]), Mathf.Min(R.reg[2], e[2]), Mathf.Max(R.reg[3], e[3]) }; }
                roofs.Add(R);
            }
        }

        Material RoofMat(RPiece R) => R.mat == "thatch" ? strawM : R.mat == "tiles" ? CotMats.tiles : R.mat == "slate" ? CotMats.slate : CotMats.glassClear;

        /// <summary>Roof heightfield over all pieces (max), with smooth normals, plaster underside and fascia skirts.</summary>
        void BuildRoof()
        {
            var pieces = roofs;
            if (pieces.Count == 0) return;
            var gx = new List<float>(); var gd = new List<float>();
            float bx0 = 1e9f, bx1 = -1e9f, bd0 = 1e9f, bd1 = -1e9f;
            foreach (var R in pieces)
            {
                float[] g = R.kind == "cone" ? new[] { R.cx - R.r - R.ov, R.cx + R.r + R.ov, R.cd - R.r - R.ov, R.cd + R.r + R.ov } : R.reg;
                bx0 = Mathf.Min(bx0, g[0]); bx1 = Mathf.Max(bx1, g[1]); bd0 = Mathf.Min(bd0, g[2]); bd1 = Mathf.Max(bd1, g[3]);
                gx.Add(g[0]); gx.Add(g[1]); gd.Add(g[2]); gd.Add(g[3]);
                if (R.kind == "prism") { float rl = (R.b0 + R.b1) / 2f; if (R.axis == "x") gd.Add(rl); else gx.Add(rl); }
                if (R.kind == "cone") { gx.Add(R.cx); gd.Add(R.cd); }
            }
            foreach (var w in P.wings) if (w.circle == null) { gx.Add(w.r.x0); gx.Add(w.r.x1); gd.Add(w.r.d0); gd.Add(w.r.d1); }
            var X = Fill(gx, bx0, bx1, 0.3f); var D = Fill(gd, bd0, bd1, 0.3f);
            int nX = X.Count, nD = D.Count;
            var cells = new RPiece[(nX - 1) * (nD - 1)];
            var cellSig = new int[cells.Length];
            var cellSet = new List<RPiece>[cells.Length];
            for (int j = 0; j < nD - 1; j++)
                for (int i = 0; i < nX - 1; i++)
                {
                    float cx = (X[i] + X[i + 1]) / 2f, cd = (D[j] + D[j + 1]) / 2f;
                    List<RPiece> set = null; RPiece top = null; float hb = float.NegativeInfinity; int sig = 0;
                    for (int q = 0; q < pieces.Count; q++)
                    {
                        if (PieceH(pieces[q], cx, cd, false) == float.NegativeInfinity) continue;
                        (set ??= new List<RPiece>()).Add(pieces[q]); sig |= 1 << q;
                        float v = PieceH(pieces[q], cx, cd, true); if (v > hb) { hb = v; top = pieces[q]; }
                    }
                    int k = j * (nX - 1) + i; cells[k] = top; cellSig[k] = sig; cellSet[k] = set;
                }
            float Hset(List<RPiece> set, float x, float d) { float b = float.NegativeInfinity; foreach (var R in set) { float v = PieceH(R, x, d, true); if (v > b) b = v; } return b; }
            var vmap = new Dictionary<long, int>();
            var umap = new Dictionary<long, int>();
            var k2 = roofKit;
            int VTop(int k, int i, int j)
            {
                var top = cells[k]; long key = ((long)i * 100000 + j) * 4096 + (cellSig[k] & 4095) * 8 + (pieces.IndexOf(top) & 7);
                if (vmap.TryGetValue(key, out int v)) return v;
                float x = X[i], d = D[j], h = Hset(cellSet[k], x, d);
                Vector2 uv = RoofUV(top, x, d, h);
                v = k2.Vert(new Vector3(x, h, d), Vector3.up, uv); vmap[key] = v; return v;
            }
            int VBot(int k, int i, int j)
            {
                var top = cells[k]; long key = ((long)i * 100000 + j) * 4096 + (cellSig[k] & 4095) * 8 + (pieces.IndexOf(top) & 7);
                if (umap.TryGetValue(key, out int v)) return v;
                float x = X[i], d = D[j], h = Hset(cellSet[k], x, d) - top.T;
                v = k2.Vert(new Vector3(x, h, d), Vector3.down, new Vector2(x, d) / 1.5f); umap[key] = v; return v;
            }
            bool Has(int i, int j) => i >= 0 && j >= 0 && i < nX - 1 && j < nD - 1 && cells[j * (nX - 1) + i] != null;
            for (int j = 0; j < nD - 1; j++)
                for (int i = 0; i < nX - 1; i++)
                {
                    int k = j * (nX - 1) + i; var top = cells[k]; if (top == null) continue;
                    var m = RoofMat(top);
                    int a = VTop(k, i, j), b = VTop(k, i + 1, j), c = VTop(k, i + 1, j + 1), d = VTop(k, i, j + 1);
                    // top surface faces up: in local space (x right, z forward) the clockwise-from-above order is a,d,c,b
                    k2.Tri(m, a, d, c); k2.Tri(m, a, c, b);
                    if (top.mat != "glass")
                    {
                        int ua = VBot(k, i, j), ub = VBot(k, i + 1, j), uc = VBot(k, i + 1, j + 1), ud = VBot(k, i, j + 1);
                        k2.Tri(CotMats.plaster, ua, uc, ud); k2.Tri(CotMats.plaster, ua, ub, uc);
                    }
                    // skirts on open edges
                    float T = top.T;
                    void Skirt(int i0, int j0, int i1, int j1, Vector3 n)
                    {
                        float xa = X[i0], da = D[j0], xb = X[i1], db = D[j1];
                        float ha = Hset(cellSet[k], xa, da), hb = Hset(cellSet[k], xb, db);
                        k2.QuadUV(m, new Vector3(xa, ha, da), new Vector3(xb, hb, db), new Vector3(xb, hb - T, db), new Vector3(xa, ha - T, da), n,
                            new Vector2(0, 0), new Vector2(Mathf.Abs(xb - xa + db - da) / 1.4f, 0), new Vector2(Mathf.Abs(xb - xa + db - da) / 1.4f, T / 1.4f), new Vector2(0, T / 1.4f));
                    }
                    if (!Has(i - 1, j)) Skirt(i, j, i, j + 1, Vector3.left);
                    if (!Has(i + 1, j)) Skirt(i + 1, j, i + 1, j + 1, Vector3.right);
                    if (!Has(i, j - 1)) Skirt(i, j, i + 1, j, Vector3.back);
                    if (!Has(i, j + 1)) Skirt(i, j + 1, i + 1, j + 1, Vector3.forward);
                }
        }

        Vector2 RoofUV(RPiece R, float x, float d, float h)
        {
            float t = R.mat == "thatch" ? 1.4f : 1.6f;
            if (R.kind == "prism") { bool ax = R.axis == "x"; float along = ax ? x : d, across = ax ? d : x; return new Vector2(along / t, Mathf.Abs(across - (R.b0 + R.b1) / 2f) * 1.45f / t); }
            if (R.kind == "cone") { float a = Mathf.Atan2(d - R.cd, x - R.cx); return new Vector2(a * R.r / t, -h / t); }
            return new Vector2((R.axis == "x" ? x : d) / t, h / t * 1.3f);
        }

        static List<float> Fill(List<float> vals, float lo, float hi, float step)
        {
            var a = new List<float>();
            foreach (var v in vals) if (v >= lo - 1e-4f && v <= hi + 1e-4f) a.Add(v);
            a.Sort();
            var outL = new List<float> { a[0] };
            for (int k = 1; k < a.Count; k++)
            {
                float p = outL[outL.Count - 1], q = a[k];
                if (q - p < 0.04f) continue;
                int n = Mathf.CeilToInt((q - p) / step);
                for (int j = 1; j < n; j++) outL.Add(p + (q - p) * j / n);
                outL.Add(q);
            }
            return outL;
        }

        // ======================= wall grid =======================
        const float RES = 0.05f;
        float gx0, gd0; int nx, nd;
        bool[] foot, open0, open1, has1, near1, ext, room1, glz;
        List<Vector2>[] col; int[] sig;

        int Cell(int i, int j) => i < 0 || j < 0 || i >= nx || j >= nd ? -1 : j * nx + i;
        int CellAt(float x, float d) => Cell(Mathf.FloorToInt((x - gx0) / RES), Mathf.FloorToInt((d - gd0) / RES));
        float Cx(int i) => gx0 + (i + 0.5f) * RES;
        float Cd(int j) => gd0 + (j + 0.5f) * RES;

        public bool InFoot(float x, float d, float e = 0f)
        {
            foreach (var w in P.wings)
            {
                if (w.circle != null) { if (Mathf.Sqrt((x - w.circle.cx) * (x - w.circle.cx) + (d - w.circle.cd) * (d - w.circle.cd)) <= w.circle.r + e) return true; }
                else if (w.r.In(x, d, e)) return true;
            }
            return false;
        }
        CRoom RoomAt(int lv, float x, float d) { foreach (var r in P.rooms) if (r.lv == lv && r.In(x, d)) return r; return null; }
        bool FlightOpen(int lv, float x, float d)
        {
            foreach (var s in P.stairs)
            {
                bool hit = s.lo == lv || s.hi == lv || (lv == 0 && s.lo == 4);
                if (!hit) continue;
                foreach (var f in s.f) if (f.r.In(x, d)) return true;
            }
            return false;
        }

        static Vector2 SideN(string side) => side == "F" ? new Vector2(0, -1) : side == "B" ? new Vector2(0, 1) : side == "L" ? new Vector2(-1, 0) : new Vector2(1, 0);

        /// <summary>Room edge on a side at along-coordinate m (x for F/B, d for L/R).</summary>
        float RoomEdge(CRoom rm, string side, float m)
        {
            var n = SideN(side); float e = float.NaN;
            foreach (var r in rm.r)
            {
                bool inSpan = n.x != 0 ? m >= r.d0 - 0.01f && m <= r.d1 + 0.01f : m >= r.x0 - 0.01f && m <= r.x1 + 0.01f;
                if (!inSpan) continue;
                float v = n.x != 0 ? (n.x < 0 ? r.x0 : r.x1) : (n.y < 0 ? r.d0 : r.d1);
                if (float.IsNaN(e) || (n.x + n.y) * (v - e) > 0) e = v;
            }
            return e;
        }
        float OuterFace(string side, float e, float m)
        {
            var n = SideN(side);
            for (float s = 0.02f; s < 1.6f; s += 0.02f)
            {
                float x = n.x != 0 ? e + n.x * s : m, d = n.x != 0 ? m : e + n.y * s;
                if (!InFoot(x, d, -0.001f)) return e + (n.x + n.y) * s;
            }
            return e + (n.x + n.y) * 0.5f;
        }

        void Classify()
        {
            float x0 = 1e9f, x1 = -1e9f, d0 = 1e9f, d1 = -1e9f;
            foreach (var w in P.wings) { x0 = Mathf.Min(x0, w.r.x0); x1 = Mathf.Max(x1, w.r.x1); d0 = Mathf.Min(d0, w.r.d0); d1 = Mathf.Max(d1, w.r.d1); }
            gx0 = x0 - 0.1f; gd0 = d0 - 0.1f;
            nx = Mathf.CeilToInt((x1 - x0 + 0.2f) / RES); nd = Mathf.CeilToInt((d1 - d0 + 0.2f) / RES);
            int n = nx * nd;
            foot = new bool[n]; open0 = new bool[n]; open1 = new bool[n]; has1 = new bool[n]; near1 = new bool[n]; ext = new bool[n]; room1 = new bool[n]; glz = new bool[n];
            col = new List<Vector2>[n]; sig = new int[n];
            bool any1 = false; foreach (var r in P.rooms) if (r.lv == 1) any1 = true;
            for (int j = 0; j < nd; j++)
                for (int i = 0; i < nx; i++)
                {
                    int k = j * nx + i; float x = Cx(i), d = Cd(j);
                    if (!InFoot(x, d)) continue;
                    foot[k] = true;
                    open0[k] = RoomAt(0, x, d) != null || FlightOpen(0, x, d);
                    room1[k] = RoomAt(1, x, d) != null;
                    open1[k] = room1[k] || FlightOpen(1, x, d);
                    has1[k] = any1 && RoofCeil(x, d) > Y1 + 0.9f;
                    const float E = 0.62f;
                    ext[k] = !InFoot(x - E, d) || !InFoot(x + E, d) || !InFoot(x, d - E) || !InFoot(x, d + E);
                    foreach (var w in P.wings) if (w.glazed && w.r.In(x, d)) glz[k] = true;
                }
            foreach (var r in P.rooms)
            {
                if (r.lv != 1) continue;
                foreach (var q in r.r) MarkRect(new CRect(q.x0 - 0.4f, q.x1 + 0.4f, q.d0 - 0.4f, q.d1 + 0.4f), near1, true);
            }
            foreach (var s in P.stairs) if (s.lo == 1 || s.hi == 1) foreach (var f in s.f) MarkRect(new CRect(f.r.x0 - 0.4f, f.r.x1 + 0.4f, f.r.d0 - 0.4f, f.r.d1 + 0.4f), near1, true);
            // chimney breasts projecting into rooms
            foreach (var h in P.hearths)
            {
                if (h.proj <= 0.01f) continue;
                var rm = P.Room(h.room); if (rm == null || rm.circle != null) continue;
                var nn = SideN(h.side); float m = (h.a + h.b) / 2f, e = RoomEdge(rm, h.side, m); if (float.IsNaN(e)) continue;
                float lo = Mathf.Min(e, e - (nn.x + nn.y) * h.proj), hi = Mathf.Max(e, e - (nn.x + nn.y) * h.proj);
                var rr = nn.x != 0 ? new CRect(lo, hi, h.a - 0.3f, h.b + 0.3f) : new CRect(h.a - 0.3f, h.b + 0.3f, lo, hi);
                var target = rm.lv == 0 ? open0 : open1;
                MarkRect(rr, target, false, true);
            }
        }

        void MarkRect(CRect r, bool[] arr, bool val, bool footOnly = false)
        {
            int i0 = Mathf.Max(0, Mathf.FloorToInt((r.x0 - gx0) / RES)), i1 = Mathf.Min(nx - 1, Mathf.CeilToInt((r.x1 - gx0) / RES));
            int j0 = Mathf.Max(0, Mathf.FloorToInt((r.d0 - gd0) / RES)), j1 = Mathf.Min(nd - 1, Mathf.CeilToInt((r.d1 - gd0) / RES));
            for (int j = j0; j <= j1; j++) for (int i = i0; i <= i1; i++)
                {
                    float x = Cx(i), d = Cd(j); if (!r.In(x, d)) continue;
                    int k = j * nx + i; if (footOnly && !foot[k]) continue;
                    arr[k] = val;
                }
        }

        void Columns()
        {
            for (int j = 0; j < nd; j++)
                for (int i = 0; i < nx; i++)
                {
                    int k = j * nx + i; if (!foot[k]) continue;
                    float x = Cx(i), d = Cd(j);
                    float rc = RoofCeil(x, d); if (rc > 50f) rc = Y1 + 0.2f;
                    bool w0 = !open0[k];
                    bool w1 = has1[k] && !open1[k] && (ext[k] || near1[k]);
                    float top1 = ext[k] ? rc : Mathf.Min(Y1 + P.CC, rc);
                    Vector2 iv;
                    if (glz[k] && ext[k]) iv = new Vector2(0, 0.62f);
                    else if (w0 && w1) iv = new Vector2(0, Mathf.Max(top1, Y1));
                    else if (w0) iv = new Vector2(0, has1[k] && !ext[k] ? Y1 : rc);
                    else if (w1) iv = new Vector2(Y1 - 0.2f, top1);
                    else continue;
                    if (iv.y - iv.x < 0.02f) continue;
                    col[k] = new List<Vector2> { iv };
                }
        }

        void Carve(int k, float y0, float y1)
        {
            if (k < 0 || col[k] == null) return;
            var outL = new List<Vector2>();
            foreach (var iv in col[k])
            {
                if (y1 <= iv.x || y0 >= iv.y) { outL.Add(iv); continue; }
                if (y0 > iv.x + 0.01f) outL.Add(new Vector2(iv.x, y0));
                if (y1 < iv.y - 0.01f) outL.Add(new Vector2(y1, iv.y));
            }
            col[k] = outL.Count > 0 ? outL : null;
        }

        /// <summary>Carve along a side from the room edge outward through the wall.</summary>
        void CarveThrough(string side, float e, float a, float b, float y0, float y1, float maxDepth, bool stopAtOutside)
        {
            var n = SideN(side);
            for (float t = a + RES * 0.5f; t < b; t += RES)
                for (float s = 0.01f; s < maxDepth; s += RES * 0.5f)
                {
                    float x = n.x != 0 ? e + n.x * s : t, d = n.x != 0 ? t : e + n.y * s;
                    int k = CellAt(x, d); if (k < 0) break;
                    if (stopAtOutside && !foot[k]) break;
                    Carve(k, y0, y1);
                }
        }

        void Openings()
        {
            foreach (var W in P.wins)
            {
                var rm = P.Room(W.room); if (rm == null || rm.circle != null) continue;
                float m = (W.a + W.b) / 2f, e = RoomEdge(rm, W.side, m); if (float.IsNaN(e)) continue;
                CarveThrough(W.side, e, W.a, W.b, rm.y + W.y0, rm.y + W.y1, 1.4f, true);
            }
            foreach (var E in P.ext)
            {
                var rm = P.Room(E.room); if (rm == null || rm.circle != null) continue;
                float m = (E.a + E.b) / 2f, e = RoomEdge(rm, E.side, m); if (float.IsNaN(e)) continue;
                CarveThrough(E.side, e, E.a, E.b, rm.y - 0.1f, rm.y + E.hgt, 1.4f, true);
            }
            foreach (var D in P.doors)
            {
                float y = D.lv == 0 ? 0f : D.lv == 1 ? Y1 : D.lv == 3 ? P.Y2 : -2.3f;
                if (D.lv == 4) continue;
                for (float t = D.a + RES * 0.5f; t < D.b; t += RES)
                    foreach (int sgn in new[] { -1, 1 })
                        for (float s = 0f; s < 0.66f; s += RES * 0.5f)
                        {
                            float x = D.line == "X" ? D.at + sgn * s : t, d = D.line == "X" ? t : D.at + sgn * s;
                            int k = CellAt(x, d); if (k < 0) break;
                            if (col[k] == null) { if (s > 0.08f) break; else continue; }
                            Carve(k, y - 0.05f, y + D.hgt);
                        }
            }
            foreach (var h in P.hearths)
            {
                var rm = P.Room(h.room); if (rm == null || rm.circle != null) continue;
                var nn = SideN(h.side); float m = (h.a + h.b) / 2f, e = RoomEdge(rm, h.side, m); if (float.IsNaN(e)) continue;
                float face = e - (nn.x + nn.y) * h.proj; // breast face inside the room
                float hgt = HearthH(h);
                CarveThrough(h.side, face, h.a, h.b, rm.y - 0.05f, rm.y + hgt, h.proj + h.depth, false);
            }
        }
        static float HearthH(CHearth h) => h.kind == "inglenook" ? 1.6f : h.kind == "range" ? 1.3f : h.kind == "forge" ? 1.05f : h.kind == "stove" ? 1.15f : 1.0f;

        // ----- greedy merge of wall columns into boxes -----
        void EmitWalls()
        {
            var ids = new Dictionary<string, int>(); var sb = new System.Text.StringBuilder();
            for (int k = 0; k < col.Length; k++)
            {
                if (col[k] == null) continue;
                sb.Clear(); foreach (var iv in col[k]) sb.Append(Mathf.RoundToInt(iv.x * 100)).Append(',').Append(Mathf.RoundToInt(iv.y * 100)).Append(';');
                sb.Append(ext[k] ? 'e' : 'i');
                string key = sb.ToString();
                if (!ids.TryGetValue(key, out int id)) { id = ids.Count + 1; ids[key] = id; }
                sig[k] = id;
            }
            var done = new bool[col.Length];
            for (int j = 0; j < nd; j++)
                for (int i = 0; i < nx; i++)
                {
                    int k = j * nx + i; if (sig[k] == 0 || done[k]) continue;
                    int s = sig[k], i1 = i + 1;
                    while (i1 < nx && sig[j * nx + i1] == s && !done[j * nx + i1]) i1++;
                    int j1 = j + 1;
                    while (j1 < nd)
                    {
                        bool ok = true;
                        for (int q = i; q < i1; q++) { int kk = j1 * nx + q; if (sig[kk] != s || done[kk]) { ok = false; break; } }
                        if (!ok) break; j1++;
                    }
                    for (int jj = j; jj < j1; jj++) for (int q = i; q < i1; q++) done[jj * nx + q] = true;
                    EmitRect(i, j, i1, j1, col[k], ext[k]);
                }
        }

        void EmitRect(int i0, int j0, int i1, int j1, List<Vector2> cl, bool isExt)
        {
            float xa = gx0 + i0 * RES, xb = gx0 + i1 * RES, da = gd0 + j0 * RES, db = gd0 + j1 * RES;
            var cap = isExt ? limeM : CotMats.plaster;
            foreach (var iv in cl)
            {
                solid.Quad(cap, new Vector3(xa, iv.y, da), new Vector3(xb, iv.y, da), new Vector3(xb, iv.y, db), new Vector3(xa, iv.y, db), Vector3.up);
                if (iv.x > 0.01f) solid.Quad(cap, new Vector3(xa, iv.x, da), new Vector3(xb, iv.x, da), new Vector3(xb, iv.x, db), new Vector3(xa, iv.x, db), Vector3.down);
            }
            SideRuns(cl, isExt, true, i0 - 1, j0, j1, xa, -1);
            SideRuns(cl, isExt, true, i1, j0, j1, xb, 1);
            SideRuns(cl, isExt, false, j0 - 1, i0, i1, da, -1);
            SideRuns(cl, isExt, false, j1, i0, i1, db, 1);
        }

        int NKey(int k) => k < 0 || !foot[k] ? -1 : col[k] == null ? -2 : sig[k];

        void SideRuns(List<Vector2> cl, bool isExt, bool xSide, int fixedIdx, int s0, int s1, float fixedCoord, int sign)
        {
            int s = s0;
            while (s < s1)
            {
                int k = xSide ? Cell(fixedIdx, s) : Cell(s, fixedIdx);
                int key = NKey(k), e = s + 1;
                while (e < s1 && NKey(xSide ? Cell(fixedIdx, e) : Cell(e, fixedIdx)) == key) e++;
                bool outside = key == -1, openRoom = key == -2;
                List<Vector2> nc = (!outside && !openRoom) ? col[k] : null;
                var m = outside ? limeM : openRoom ? CotMats.plaster : (isExt ? limeM : CotMats.plaster);
                float ta = (xSide ? gd0 : gx0) + s * RES, tb = (xSide ? gd0 : gx0) + e * RES;
                Vector3 nrm = xSide ? new Vector3(sign, 0, 0) : new Vector3(0, 0, sign);
                foreach (var iv in cl)
                    foreach (var u in Subtract(iv, nc))
                    {
                        if (xSide) solid.Quad(m, new Vector3(fixedCoord, u.x, ta), new Vector3(fixedCoord, u.y, ta), new Vector3(fixedCoord, u.y, tb), new Vector3(fixedCoord, u.x, tb), nrm);
                        else solid.Quad(m, new Vector3(ta, u.x, fixedCoord), new Vector3(ta, u.y, fixedCoord), new Vector3(tb, u.y, fixedCoord), new Vector3(tb, u.x, fixedCoord), nrm);
                    }
                s = e;
            }
        }

        static List<Vector2> Subtract(Vector2 iv, List<Vector2> cut)
        {
            var res = new List<Vector2> { iv };
            if (cut == null) return res;
            foreach (var c in cut)
            {
                var nxt = new List<Vector2>();
                foreach (var r in res)
                {
                    if (c.y <= r.x || c.x >= r.y) { nxt.Add(r); continue; }
                    if (c.x > r.x + 0.005f) nxt.Add(new Vector2(r.x, c.x));
                    if (c.y < r.y - 0.005f) nxt.Add(new Vector2(c.y, r.y));
                }
                res = nxt;
            }
            return res;
        }

        // ======================= floors / ceilings =======================
        List<RectInt> GreedyBool(System.Func<int, bool> on)
        {
            var outL = new List<RectInt>(); var done = new bool[nx * nd];
            for (int j = 0; j < nd; j++)
                for (int i = 0; i < nx; i++)
                {
                    int k = j * nx + i; if (done[k] || !on(k)) continue;
                    int i1 = i + 1; while (i1 < nx && !done[j * nx + i1] && on(j * nx + i1)) i1++;
                    int j1 = j + 1;
                    while (j1 < nd) { bool ok = true; for (int q = i; q < i1; q++) { int kk = j1 * nx + q; if (done[kk] || !on(kk)) { ok = false; break; } } if (!ok) break; j1++; }
                    for (int jj = j; jj < j1; jj++) for (int q = i; q < i1; q++) done[jj * nx + q] = true;
                    outL.Add(new RectInt(i, j, i1 - i, j1 - j));
                }
            return outL;
        }

        static bool Flaggy(string t) => t == "kitchen" || t == "scullery" || t == "larder" || t == "hall" || t == "passage" || t == "boot" || t == "lobby" || t == "forge" || t == "wc" || t == "garden" || t == "cellar";

        void Floors()
        {
            // ground floors per room (flagstones or boards), stairs' foot area as boards
            foreach (var rm in P.rooms)
            {
                if (rm.lv != 0) continue;
                var m = Flaggy(rm.type) ? CotMats.flag : CotMats.boards;
                if (rm.circle != null) { solid.Cyl(m, new Vector3(rm.circle.cx, -0.02f, rm.circle.cd), rm.circle.r, 0.06f, 24); continue; }
                foreach (var r in rm.r) solid.Box(m, new Vector3(r.x0 - 0.03f, -0.02f, r.d0 - 0.03f), new Vector3(r.x1 + 0.03f, 0.04f, r.d1 + 0.03f), 4);
            }
            foreach (var s in P.stairs) foreach (var f in s.f) if (s.lo == 0 || s.lo == 4) solid.Box(CotMats.boards, new Vector3(f.r.x0, -0.02f, f.r.d0), new Vector3(f.r.x1, 0.04f, f.r.d1), 4);

            // first floor slab: over level-1 rooms
            foreach (var r in GreedyBool(k => foot[k] && room1[k] && has1[k]))
                Slab(r, Y1 - 0.2f, Y1, CotMats.boards, CotMats.plaster);
            // ground ceilings where there is no room above (flat ceilings)
            foreach (var r in GreedyBool(k => { if (!foot[k] || room1[k] || col[k] != null) return false; int i = k % nx, j = k / nx; var rm = RoomAt(0, Cx(i), Cd(j)); return rm != null && rm.ceil == "flat" && !FlightOpen(1, Cx(i), Cd(j)); }))
                Slab(r, Y1 - 0.2f, Y1 - 0.14f, null, CotMats.plaster);
            // exposed oak joists and a spine beam under flat ground-floor ceilings
            foreach (var rm in P.rooms)
            {
                if (rm.lv != 0 || rm.ceil != "flat" || rm.circle != null) continue;
                foreach (var r in rm.r)
                {
                    float w = r.x1 - r.x0, dd = r.d1 - r.d0; if (w < 1.2f || dd < 1.2f) continue;
                    float yb = Y1 - 0.2f;
                    bool spanD = w >= dd; // joists span the short way
                    float len = spanD ? w : dd;
                    int n = Mathf.FloorToInt(len / 0.55f);
                    for (int q = 1; q < n; q++)
                    {
                        float t = (spanD ? r.x0 : r.d0) + q * len / n;
                        if (spanD) deco.Box(CotMats.oak, new Vector3(t - 0.05f, yb - 0.13f, r.d0), new Vector3(t + 0.05f, yb, r.d1));
                        else deco.Box(CotMats.oak, new Vector3(r.x0, yb - 0.13f, t - 0.05f), new Vector3(r.x1, yb, t + 0.05f));
                    }
                    if (len > 3f)
                    {
                        if (spanD) deco.Box(CotMats.oak, new Vector3(r.x0, yb - 0.24f, r.Cd - 0.11f), new Vector3(r.x1, yb - 0.12f, r.Cd + 0.11f));
                        else deco.Box(CotMats.oak, new Vector3(r.Cx - 0.11f, yb - 0.24f, r.d0), new Vector3(r.Cx + 0.11f, yb - 0.12f, r.d1));
                    }
                }
            }
            // capped ceilings under the roof (upper rooms, and ground rooms open to a roof)
            foreach (var rm in P.rooms)
            {
                if (rm.lv > 1 || (rm.lv == 0 && rm.ceil == "flat") || rm.cmax > 50f) continue;
                float cm = rm.cmax; var R = rm;
                foreach (var r in GreedyBool(k => { if (!foot[k] || col[k] != null) return false; int i = k % nx, j = k / nx; float x = Cx(i), d = Cd(j); return R.In(x, d) && RoofCeil(x, d) > cm + 0.03f; }))
                    Slab(r, cm, cm + 0.06f, null, CotMats.plaster);
            }
        }

        void Slab(RectInt r, float y0, float y1, Material top, Material bottom)
        {
            float xa = gx0 + r.xMin * RES, xb = gx0 + r.xMax * RES, da = gd0 + r.yMin * RES, db = gd0 + r.yMax * RES;
            if (top != null) solid.Quad(top, new Vector3(xa, y1, da), new Vector3(xb, y1, da), new Vector3(xb, y1, db), new Vector3(xa, y1, db), Vector3.up);
            solid.Quad(bottom, new Vector3(xa, y0, da), new Vector3(xb, y0, da), new Vector3(xb, y0, db), new Vector3(xa, y0, db), Vector3.down);
            // exposed edges (stair wells) in oak
            void Edge(bool xSide, int fixedIdx, int a, int b, float fixedC, int sign)
            {
                for (int s = a; s < b; s++)
                {
                    int k = xSide ? Cell(fixedIdx, s) : Cell(s, fixedIdx);
                    if (k >= 0 && (col[k] != null || (foot[k] && room1[k] && has1[k]))) continue;
                    float ta = (xSide ? gd0 : gx0) + s * RES, tb = ta + RES;
                    if (xSide) solid.Quad(CotMats.oak, new Vector3(fixedC, y0, ta), new Vector3(fixedC, y1, ta), new Vector3(fixedC, y1, tb), new Vector3(fixedC, y0, tb), new Vector3(sign, 0, 0));
                    else solid.Quad(CotMats.oak, new Vector3(ta, y0, fixedC), new Vector3(ta, y1, fixedC), new Vector3(tb, y1, fixedC), new Vector3(tb, y0, fixedC), new Vector3(0, 0, sign));
                }
            }
            if (top != null)
            {
                Edge(true, r.xMin - 1, r.yMin, r.yMax, xa, -1); Edge(true, r.xMax, r.yMin, r.yMax, xb, 1);
                Edge(false, r.yMin - 1, r.xMin, r.xMax, da, -1); Edge(false, r.yMax, r.xMin, r.xMax, db, 1);
            }
        }

        // ======================= stairs =======================
        void Stairs()
        {
            foreach (var s in P.stairs)
            {
                if (s.lo == 4) { foreach (var f in s.f) Hatch(f.r); continue; }
                foreach (var f in s.f)
                {
                    if (s.kind == "ladder" || s.kind == "attic") { Ladder(f); continue; }
                    if (f.dir == "0") { solid.Box(CotMats.oak, new Vector3(f.r.x0, f.y0 - 0.05f, f.r.d0), new Vector3(f.r.x1, f.y0 + 0.02f, f.r.d1)); continue; }
                    bool alongX = f.dir.Contains("x"); bool pos = f.dir[0] == '+';
                    float L = alongX ? f.r.x1 - f.r.x0 : f.r.d1 - f.r.d0;
                    int n = Mathf.Max(2, Mathf.RoundToInt((f.y1 - f.y0) / 0.19f));
                    float rise = (f.y1 - f.y0) / n, tread = L / n;
                    for (int q = 0; q < n; q++)
                    {
                        float u0 = q * tread, u1 = (q + 1) * tread, top = f.y0 + (q + 1) * rise;
                        Vector3 mn, mx;
                        if (alongX) { float a = pos ? f.r.x0 + u0 : f.r.x1 - u1, b = pos ? f.r.x0 + u1 : f.r.x1 - u0; mn = new Vector3(a, f.y0, f.r.d0); mx = new Vector3(b, top, f.r.d1); }
                        else { float a = pos ? f.r.d0 + u0 : f.r.d1 - u1, b = pos ? f.r.d0 + u1 : f.r.d1 - u0; mn = new Vector3(f.r.x0, f.y0, a); mx = new Vector3(f.r.x1, top, b); }
                        solid.Box(CotMats.oak, mn, mx, 4);
                        // nosing strip
                        Vector3 c = (mn + mx) / 2f;
                        deco.BoxB(CotMats.boards, new Vector3(c.x, top, c.z), new Vector3(mx.x - mn.x + 0.01f, 0.012f, mx.z - mn.z + 0.01f));
                    }
                    if (s.open) Balusters(f, alongX, pos, L);
                }
            }
        }

        void Balusters(CFlight f, bool alongX, bool pos, float L)
        {
            int n = Mathf.Max(3, Mathf.RoundToInt(L / 0.25f));
            for (int q = 0; q <= n; q++)
            {
                float u = q / (float)n, y = Mathf.Lerp(f.y0, f.y1, u);
                Vector3 p = alongX ? new Vector3(pos ? f.r.x0 + u * L : f.r.x1 - u * L, y, f.r.d0 + 0.04f) : new Vector3(f.r.x0 + 0.04f, y, pos ? f.r.d0 + u * L : f.r.d1 - u * L);
                deco.BoxB(CotMats.white, p, new Vector3(0.035f, 0.9f, 0.035f));
            }
            Vector3 a = alongX ? new Vector3(pos ? f.r.x0 : f.r.x1, f.y0 + 0.9f, f.r.d0 + 0.04f) : new Vector3(f.r.x0 + 0.04f, f.y0 + 0.9f, pos ? f.r.d0 : f.r.d1);
            Vector3 b = alongX ? new Vector3(pos ? f.r.x1 : f.r.x0, f.y1 + 0.9f, f.r.d0 + 0.04f) : new Vector3(f.r.x0 + 0.04f, f.y1 + 0.9f, pos ? f.r.d1 : f.r.d0);
            Beam(deco, CotMats.oak, a, b, 0.06f, 0.05f);
        }

        void Ladder(CFlight f)
        {
            Vector3 c = new Vector3(f.r.Cx, f.y0, f.r.Cd);
            bool alongX = f.dir.Contains("x");
            float w = alongX ? f.r.d1 - f.r.d0 : f.r.x1 - f.r.x0; w = Mathf.Min(w, 0.6f);
            float h = f.y1 - f.y0;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 off = alongX ? new Vector3(0, 0, s * w / 2) : new Vector3(s * w / 2, 0, 0);
                deco.BoxB(CotMats.oak, c + off, new Vector3(0.05f, h + 0.9f, 0.05f));
            }
            for (float y = 0.3f; y < h; y += 0.3f) deco.BoxB(CotMats.oak, c + Vector3.up * y, alongX ? new Vector3(0.04f, 0.04f, w) : new Vector3(w, 0.04f, 0.04f));
        }

        void Hatch(CRect r)
        {
            deco.Box(CotMats.oak, new Vector3(r.x0 + 0.05f, 0.04f, r.d0 + 0.05f), new Vector3(r.x0 + 0.85f, 0.065f, r.d0 + 0.85f));
            deco.Cyl(CotMats.iron, new Vector3(r.x0 + 0.45f, 0.065f, r.d0 + 0.75f), 0.04f, 0.02f, 8);
        }

        /// <summary>A rectangular beam from a to b (any direction), w wide, h tall.</summary>
        public static void Beam(MeshKit k, Material m, Vector3 a, Vector3 b, float w, float h)
        {
            Vector3 dir = b - a; float len = dir.magnitude; if (len < 0.001f) return;
            var rot = Quaternion.LookRotation(dir / len, Vector3.up);
            if (Mathf.Abs(Vector3.Dot(dir / len, Vector3.up)) > 0.99f) rot = Quaternion.LookRotation(dir / len, Vector3.forward);
            k.Push(Matrix4x4.TRS(a, rot, Vector3.one));
            k.Box(m, new Vector3(-w / 2, -h / 2, 0), new Vector3(w / 2, h / 2, len));
            k.Pop();
        }

        // ======================= windows / doors / porch =======================
        Vector3 L3(string side, float along, float y, float depth) // depth = coordinate across the wall (x for L/R, d for F/B)
        {
            var n = SideN(side);
            return n.x != 0 ? new Vector3(depth, y, along) : new Vector3(along, y, depth);
        }

        void Windows()
        {
            foreach (var W in P.wins)
            {
                var rm = P.Room(W.room); if (rm == null || rm.circle != null) continue;
                float m = (W.a + W.b) / 2f, e = RoomEdge(rm, W.side, m); if (float.IsNaN(e)) continue;
                float o = OuterFace(W.side, e, m);
                var n = SideN(W.side); float sg = n.x + n.y; // +1 if outward is +axis
                float y0 = rm.y + W.y0, y1 = rm.y + W.y1, a = W.a, b = W.b;
                float plane = o - sg * 0.14f;
                bool barn = W.Opt("barn"), frost = W.Opt("frost"), vent = W.Opt("vent");
                var fm = barn ? CotMats.oak : frameM;
                Vector3 nn = n.x != 0 ? new Vector3(n.x, 0, 0) : new Vector3(0, 0, n.y);
                // frame
                FrameBar(W.side, a, a + 0.06f, y0, y1, plane, fm);
                FrameBar(W.side, b - 0.06f, b, y0, y1, plane, fm);
                FrameBar(W.side, a, b, y0, y0 + 0.07f, plane, fm);
                FrameBar(W.side, a, b, y1 - 0.06f, y1, plane, fm);
                int lights = Mathf.Max(1, (int)W.OptF("lights", (b - a) > 1.15f ? 2 : 1));
                if (barn) lights = Mathf.Max(3, lights);
                for (int q = 1; q < lights; q++) { float t = Mathf.Lerp(a, b, q / (float)lights); FrameBar(W.side, t - 0.03f, t + 0.03f, y0, y1, plane, fm); }
                if (barn) FrameBar(W.side, a, b, y0 + (y1 - y0) * 0.62f, y0 + (y1 - y0) * 0.62f + 0.06f, plane, fm);
                if (vent)
                    for (float y = y0 + 0.08f; y < y1 - 0.06f; y += 0.07f) FrameBar(W.side, a + 0.06f, b - 0.06f, y, y + 0.035f, plane, fm);
                else
                {
                    var gm = frost ? CotMats.glassFrost : barn ? CotMats.glassClear : CotMats.glassLead;
                    Vector3 p0 = L3(W.side, a + 0.05f, y0 + 0.06f, plane), p1 = L3(W.side, b - 0.05f, y0 + 0.06f, plane), p2 = L3(W.side, b - 0.05f, y1 - 0.05f, plane), p3 = L3(W.side, a + 0.05f, y1 - 0.05f, plane);
                    float s = 1f / MeshKit.TileOf(gm);
                    deco.QuadUV(gm, p0, p1, p2, p3, nn, new Vector2(a, y0) * s, new Vector2(b, y0) * s, new Vector2(b, y1) * s, new Vector2(a, y1) * s);
                }
                // stone sill outside, oak sill inside
                Vector3 sa = L3(W.side, a - 0.06f, y0 - 0.06f, o - sg * 0.18f), sb2 = L3(W.side, b + 0.06f, y0 + 0.01f, o + sg * 0.07f);
                solid.Box(CotMats.stone, Vector3.Min(sa, sb2), Vector3.Max(sa, sb2));
                if (W.Opt("seat"))
                {
                    Vector3 ia = L3(W.side, a, rm.y + 0.42f, e), ib = L3(W.side, b, rm.y + 0.48f, e - sg * 0.45f);
                    solid.Box(CotMats.oak, Vector3.Min(ia, ib), Vector3.Max(ia, ib));
                    Vector3 ca = L3(W.side, a + 0.05f, rm.y + 0.48f, e - sg * 0.02f), cb = L3(W.side, b - 0.05f, rm.y + 0.56f, e - sg * 0.42f);
                    deco.Box(CotMats.check, Vector3.Min(ca, cb), Vector3.Max(ca, cb));
                }
                else if (rm.lv <= 1 && !vent)
                {
                    Vector3 ia = L3(W.side, a - 0.05f, y0 - 0.03f, e + sg * 0.02f), ib = L3(W.side, b + 0.05f, y0 + 0.02f, e - sg * 0.12f);
                    deco.Box(CotMats.oak, Vector3.Min(ia, ib), Vector3.Max(ia, ib));
                }
            }
        }

        void FrameBar(string side, float a, float b, float y0, float y1, float plane, Material m)
        {
            Vector3 p = L3(side, a, y0, plane - 0.035f), q = L3(side, b, y1, plane + 0.035f);
            deco.Box(m, Vector3.Min(p, q), Vector3.Max(p, q));
        }

        void Doors()
        {
            foreach (var D in P.doors)
            {
                if (D.lv == 4 || D.lv == 3) continue;
                float y = D.lv == 0 ? 0f : Y1;
                bool X = D.line == "X";
                // lintel beam across the opening on each face
                float half = WallHalfAt(D);
                Vector3 la = X ? new Vector3(D.at - half, y + D.hgt, D.a - 0.06f) : new Vector3(D.a - 0.06f, y + D.hgt, D.at - half);
                Vector3 lb = X ? new Vector3(D.at + half, y + D.hgt + 0.1f, D.b + 0.06f) : new Vector3(D.b + 0.06f, y + D.hgt + 0.1f, D.at + half);
                deco.Box(CotMats.oak, la, lb);
                if (D.kind == "opening") continue;
                // ledged door leaf standing open 95 degrees, hinged at 'a'
                float w = D.b - D.a - 0.04f, h = D.hgt - 0.03f;
                Vector3 hinge = X ? new Vector3(D.at, y + 0.02f, D.a + 0.02f) : new Vector3(D.a + 0.02f, y + 0.02f, D.at);
                float yaw = X ? 0f : 90f; // leaf extends +z (X line) or +x (D line) from the hinge when closed
                deco.Push(hinge, yaw + 95f);
                deco.Box(CotMats.oak, new Vector3(-0.02f, 0, 0), new Vector3(0.02f, h, w));
                for (int q = 0; q < 3; q++) deco.Box(CotMats.oak, new Vector3(0.02f, 0.15f + q * (h - 0.3f) / 2f, 0.05f), new Vector3(0.045f, 0.27f + q * (h - 0.3f) / 2f, w - 0.05f));
                deco.Box(CotMats.iron, new Vector3(-0.06f, 1.0f, w - 0.12f), new Vector3(0.06f, 1.03f, w - 0.06f));
                deco.Pop();
            }
        }

        float WallHalfAt(CDoor D)
        {
            float y = D.lv == 0 ? 0.5f : Y1 + 0.5f, best = 0.06f;
            for (float s = 0.02f; s < 0.66f; s += 0.02f)
            {
                float m = (D.a + D.b) / 2;
                int k1 = D.line == "X" ? CellAt(D.at + s, m) : CellAt(m, D.at + s);
                int k2 = D.line == "X" ? CellAt(D.at - s, m) : CellAt(m, D.at - s);
                bool w1 = k1 >= 0 && col[k1] != null, w2 = k2 >= 0 && col[k2] != null;
                if (!w1 && !w2) break;
                best = s;
            }
            return best + 0.02f;
        }

        void ExtDoors()
        {
            foreach (var E in P.ext)
            {
                var rm = P.Room(E.room); if (rm == null || rm.circle != null) continue;
                float m = (E.a + E.b) / 2f, e = RoomEdge(rm, E.side, m); if (float.IsNaN(e)) continue;
                float o = OuterFace(E.side, e, m);
                var n = SideN(E.side); float sg = n.x + n.y;
                float y = rm.y, h = E.hgt, w = E.b - E.a;
                var leafM = E.kind == "front" ? doorM : CotMats.Paint(Color.Lerp(pl.door, new Color(0.3f, 0.25f, 0.2f), 0.5f));
                float plane = o - sg * 0.12f;
                // frame
                FrameBar(E.side, E.a - 0.07f, E.a, y, y + h + 0.07f, plane, frameM);
                FrameBar(E.side, E.b, E.b + 0.07f, y, y + h + 0.07f, plane, frameM);
                FrameBar(E.side, E.a - 0.07f, E.b + 0.07f, y + h, y + h + 0.08f, plane, frameM);
                // step
                Vector3 s0 = L3(E.side, E.a - 0.15f, -0.05f, o - sg * 0.12f), s1 = L3(E.side, E.b + 0.15f, 0.08f, o + sg * 0.35f);
                solid.Box(CotMats.stone, Vector3.Min(s0, s1), Vector3.Max(s0, s1));
                // leaf open inward ~80 degrees, hinged at a
                Vector3 hinge = L3(E.side, E.a + 0.02f, y + 0.02f, plane - sg * 0.03f);
                // the closed leaf runs along +along; for F/B along = +x, for L/R along = +z
                float baseYaw = n.x != 0 ? 0f : 90f;
                float open = (n.x != 0 ? -sg : sg) * 80f;
                deco.Push(hinge, baseYaw + open);
                deco.Box(leafM, new Vector3(-0.025f, 0, 0), new Vector3(0.025f, E.stable ? h * 0.5f - 0.01f : h, w - 0.04f));
                if (E.stable) deco.Box(leafM, new Vector3(-0.025f, h * 0.5f + 0.01f, 0), new Vector3(0.025f, h, w - 0.04f));
                for (int q = 0; q < 2; q++) deco.Box(leafM, new Vector3(-0.04f, 0.25f + q * 0.9f, 0.08f), new Vector3(0.04f, 0.75f + q * 0.9f, w - 0.12f));
                deco.Sphere(CotMats.brass, new Vector3(-0.06f, 1.0f, w - 0.12f), Vector3.one * 0.035f, 8, 4);
                if (E.kind == "front") deco.Box(CotMats.brass, new Vector3(-0.035f, 1.3f, w * 0.5f - 0.12f), new Vector3(-0.025f, 1.36f, w * 0.5f + 0.12f));
                deco.Pop();
                if (E.kind == "front")
                {
                    FrontDoorLocal = L3(E.side, m, 0, o + sg * 0.8f);
                    Porch(E, o, sg, n);
                }
            }
        }

        void Porch(CExt E, float o, float sg, Vector2 n)
        {
            float big = P.porchBig ? 1.35f : 1f, w = (E.b - E.a) + 1.0f * big, out1 = 1.25f * big, h = E.hgt + 0.25f;
            float m = (E.a + E.b) / 2f;
            Vector3 P0(float along, float y, float depthOut) => L3(E.side, m + along, y, o + sg * depthOut);
            switch (P.porch)
            {
                case "thatch":
                    {
                        foreach (int s in new[] { -1, 1 }) deco.BoxB(CotMats.oak, P0(s * w / 2f, 0, out1 - 0.1f), new Vector3(0.12f, h, 0.12f));
                        if (P.porchOpen) foreach (int s in new[] { -1, 1 }) deco.BoxB(CotMats.oak, P0(s * (w / 2f - 0.25f), 0, out1 * 0.5f), n.x != 0 ? new Vector3(out1 * 0.7f, 0.45f, 0.35f) : new Vector3(0.35f, 0.45f, out1 * 0.7f));
                        // little gabled thatch canopy: two slopes from a ridge running out from the wall
                        float rise = 0.7f * big, T = 0.28f;
                        for (int s = -1; s <= 1; s += 2)
                        {
                            Vector3 r0 = P0(0, h + rise, -0.05f), r1 = P0(0, h + rise, out1 + 0.25f);
                            Vector3 e0 = P0(s * (w / 2f + 0.35f), h - 0.15f, -0.05f), e1 = P0(s * (w / 2f + 0.35f), h - 0.15f, out1 + 0.25f);
                            Vector3 up = Vector3.Cross(r1 - r0, e0 - r0).normalized; if (up.y < 0) up = -up;
                            deco.Quad(strawM, r0, r1, e1, e0, up);
                            deco.Quad(CotMats.plaster, r0 - Vector3.up * T, r1 - Vector3.up * T, e1 - Vector3.up * T, e0 - Vector3.up * T, -up);
                            var dn = e1 - r1; deco.Quad(strawM, e0, e1, e1 - Vector3.up * T, e0 - Vector3.up * T, new Vector3(dn.x, 0, dn.z));
                        }
                        Vector3 g0 = P0(-w / 2f - 0.35f, h - 0.15f, out1 + 0.25f), g1 = P0(w / 2f + 0.35f, h - 0.15f, out1 + 0.25f), g2 = P0(0, h + rise, out1 + 0.25f);
                        deco.TriN(strawM, g0, g1, g2, n.x != 0 ? new Vector3(sg, 0, 0) : new Vector3(0, 0, sg));
                        break;
                    }
                case "doorcase":
                    {
                        foreach (int s in new[] { -1, 1 }) deco.BoxB(CotMats.white, P0(s * ((E.b - E.a) / 2f + 0.18f), 0, 0.06f), n.x != 0 ? new Vector3(0.12f, h, 0.24f) : new Vector3(0.24f, h, 0.12f));
                        Vector3 ea = P0(-(E.b - E.a) / 2f - 0.35f, h, 0f), eb = P0((E.b - E.a) / 2f + 0.35f, h + 0.22f, 0.22f);
                        deco.Box(CotMats.white, Vector3.Min(ea, eb), Vector3.Max(ea, eb));
                        Vector3 t0 = P0(-(E.b - E.a) / 2f - 0.35f, h + 0.22f, 0.12f), t1 = P0((E.b - E.a) / 2f + 0.35f, h + 0.22f, 0.12f), t2 = P0(0, h + 0.62f, 0.12f);
                        deco.TriN(CotMats.white, t0, t1, t2, n.x != 0 ? new Vector3(sg, 0, 0) : new Vector3(0, 0, sg));
                        // fanlight
                        Vector3 f0 = P0(-(E.b - E.a) / 2f + 0.05f, h - 0.32f, 0.02f), f1 = P0((E.b - E.a) / 2f - 0.05f, h - 0.05f, 0.03f);
                        deco.Box(CotMats.glassLead, Vector3.Min(f0, f1), Vector3.Max(f0, f1));
                        break;
                    }
                default: // hood on brackets
                    {
                        Vector3 ha = P0(-w / 2f, h, 0f), hb = P0(w / 2f, h + 0.1f, 0.7f * big);
                        deco.Box(CotMats.lead, Vector3.Min(ha, hb), Vector3.Max(ha, hb));
                        foreach (int s in new[] { -1, 1 }) { Vector3 b0 = P0(s * (w / 2f - 0.1f), h - 0.45f, 0f), b1 = P0(s * (w / 2f - 0.1f), h, 0.6f * big); deco.Box(frameM, Vector3.Min(b0, b1) - new Vector3(0.03f, 0, 0.03f), Vector3.Max(b0, b1) + new Vector3(0.03f, 0, 0.03f)); }
                        if (P.horseshoe) { Vector3 hp = P0(0, E.hgt + 0.05f, 0.02f); deco.BoxB(CotMats.iron, hp, new Vector3(0.14f, 0.16f, 0.14f)); }
                        break;
                    }
            }
        }

        // ======================= chimneys / hearths =======================
        void Chimneys()
        {
            foreach (var ch in P.chims)
            {
                if (ch.flue != null)
                {
                    float fx = ch.flue[0], fd = ch.flue[1], top = RoofH(fx, fd); if (top < -1e6f) top = 4f;
                    deco.Cyl(CotMats.iron, new Vector3(fx, 0.9f, fd), 0.08f, top - 0.9f + 1.0f, 10);
                    deco.Cyl(CotMats.iron, new Vector3(fx, top + 0.95f, fd), 0.16f, 0.06f, 10, true, 0.02f);
                    continue;
                }
                if (ch.r == null) continue;
                var r = ch.r;
                float hmax = float.NegativeInfinity;
                foreach (var p in new[] { new Vector2(r.x0, r.d0), new Vector2(r.x1, r.d0), new Vector2(r.x0, r.d1), new Vector2(r.x1, r.d1), new Vector2(r.Cx, r.Cd) })
                    hmax = Mathf.Max(hmax, RoofH(p.x, p.y));
                foreach (var R in roofs) if (R.kind == "prism") hmax = Mathf.Max(hmax, Mathf.Min(R.R, PieceH(R, r.Cx, r.Cd, true)));
                if (hmax < 0f || float.IsInfinity(hmax)) hmax = 4.5f;
                float topY = hmax + 0.75f * ch.tall;
                bool inside = InFoot(r.Cx, r.Cd);
                var m = ch.brick ? CotMats.brick : CotMats.stone;
                float baseY = inside ? Mathf.Max(0f, RoofCeil(r.Cx, r.Cd) - 0.3f) : 0f;
                if (!inside && ch.shoulder)
                {
                    // wider base with a weathered shoulder
                    bool alongD = r.x1 - r.x0 < r.d1 - r.d0;
                    var grow = alongD ? new Vector3(0, 0, 0.22f) : new Vector3(0.22f, 0, 0);
                    float sh = Mathf.Max(1.6f, topY - 2.8f);
                    solid.Box(m, new Vector3(r.x0, 0, r.d0) - grow, new Vector3(r.x1, sh, r.d1) + grow);
                    solid.Box(m, new Vector3(r.x0, sh, r.d0) - grow * 0.5f, new Vector3(r.x1, sh + 0.35f, r.d1) + grow * 0.5f);
                }
                solid.Box(m, new Vector3(r.x0, baseY, r.d0), new Vector3(r.x1, topY, r.d1));
                solid.Box(m, new Vector3(r.x0 - 0.06f, topY - 0.12f, r.d0 - 0.06f), new Vector3(r.x1 + 0.06f, topY, r.d1 + 0.06f));
                // pots in a row along the longer side
                bool longX = r.x1 - r.x0 >= r.d1 - r.d0;
                for (int q = 0; q < ch.pots; q++)
                {
                    float t = (q + 0.5f) / ch.pots;
                    Vector3 pp = longX ? new Vector3(Mathf.Lerp(r.x0, r.x1, t), topY, r.Cd) : new Vector3(r.Cx, topY, Mathf.Lerp(r.d0, r.d1, t));
                    deco.Cyl(CotMats.terracotta, pp, 0.13f, 0.5f + (q % 2) * 0.12f, 10, true, 0.1f);
                }
            }
        }

        readonly List<Vector3> fireSpots = new List<Vector3>();
        static AudioClip FireClip;

        /// <summary>Conservatory glazing: white posts and glass from the dwarf wall up to the eaves.</summary>
        void Glazing()
        {
            foreach (var w in P.wings)
            {
                if (!w.glazed) continue;
                var r = w.r;
                foreach (var side in new[] { "F", "B", "L", "R" })
                {
                    bool abut = false;
                    foreach (var v in P.wings) if (v != w && v.circle == null && Abuts(r, v.r, side)) abut = true;
                    if (abut) continue;
                    var n = SideN(side); float sg = n.x + n.y;
                    float fixedC = n.x != 0 ? (n.x < 0 ? r.x0 : r.x1) : (n.y < 0 ? r.d0 : r.d1);
                    float a = n.x != 0 ? r.d0 : r.x0, b = n.x != 0 ? r.d1 : r.x1;
                    float plane = fixedC - sg * 0.1f;
                    int cnt = Mathf.Max(1, Mathf.RoundToInt((b - a) / 0.75f));
                    float H(float t) { float x = n.x != 0 ? plane : t, d = n.x != 0 ? t : plane; float h = RoofH(x, d); return (h < 0f || h > 20f ? 2.3f : h) - 0.06f; }
                    for (int q = 0; q <= cnt; q++)
                    {
                        float t = Mathf.Lerp(a + 0.05f, b - 0.05f, q / (float)cnt);
                        FrameBar(side, t - 0.035f, t + 0.035f, 0.62f, H(t), plane, CotMats.white);
                    }
                    for (int q = 0; q < cnt; q++)
                    {
                        float t0 = Mathf.Lerp(a + 0.05f, b - 0.05f, q / (float)cnt) + 0.035f, t1 = Mathf.Lerp(a + 0.05f, b - 0.05f, (q + 1) / (float)cnt) - 0.035f;
                        Vector3 nn = n.x != 0 ? new Vector3(n.x, 0, 0) : new Vector3(0, 0, n.y);
                        deco.Quad(CotMats.glassClear, L3(side, t0, 0.64f, plane), L3(side, t1, 0.64f, plane), L3(side, t1, H(t1), plane), L3(side, t0, H(t0), plane), nn);
                    }
                    FrameBar(side, a, b, 0.6f, 0.68f, plane, CotMats.white);
                }
            }
        }

        void Hearths()
        {
            foreach (var h in P.hearths)
            {
                var rm = P.Room(h.room); if (rm == null || rm.circle != null) continue;
                var n = SideN(h.side); float sg = n.x + n.y, m = (h.a + h.b) / 2f, e = RoomEdge(rm, h.side, m); if (float.IsNaN(e)) continue;
                float face = e - sg * h.proj, back = face + sg * (h.proj + h.depth), y = rm.y, hh = HearthH(h);
                // soot lining at the back and hearth stone in front
                Vector3 b0 = L3(h.side, h.a, y, back - sg * 0.02f), b1 = L3(h.side, h.b, y + hh, back);
                deco.Box(CotMats.soot, Vector3.Min(b0, b1), Vector3.Max(b0, b1));
                Vector3 s0 = L3(h.side, h.a - 0.2f, y + 0.04f, back), s1 = L3(h.side, h.b + 0.2f, y + 0.075f, face - sg * 0.45f);
                solid.Box(CotMats.flag, Vector3.Min(s0, s1), Vector3.Max(s0, s1));
                // oak bressummer over wide openings
                if (h.kind == "inglenook" || h.kind == "range" || h.kind == "forge")
                {
                    Vector3 o0 = L3(h.side, h.a - 0.2f, y + hh, face - sg * 0.02f), o1 = L3(h.side, h.b + 0.2f, y + hh + 0.26f, face + sg * 0.22f);
                    deco.Box(CotMats.oak, Vector3.Min(o0, o1), Vector3.Max(o0, o1));
                }
                else
                {
                    // mantel shelf for a parlour fire
                    Vector3 o0 = L3(h.side, h.a - 0.25f, y + hh + 0.05f, face - sg * 0.18f), o1 = L3(h.side, h.b + 0.25f, y + hh + 0.12f, face);
                    deco.Box(CotMats.oak, Vector3.Min(o0, o1), Vector3.Max(o0, o1));
                    deco.Cyl(CotMats.brass, L3(h.side, h.a - 0.05f, y + hh + 0.12f, face - sg * 0.08f), 0.03f, 0.25f, 8);
                    deco.Cyl(CotMats.brass, L3(h.side, h.b + 0.05f, y + hh + 0.12f, face - sg * 0.08f), 0.03f, 0.25f, 8);
                    deco.BoxB(CotMats.oak, L3(h.side, m, y + hh + 0.12f, face - sg * 0.07f), new Vector3(0.22f, 0.32f, 0.1f));
                }
                Vector3 mid = L3(h.side, m, y, (face + back) / 2f);
                if (h.kind == "range" || h.range)
                {
                    float rw = Mathf.Min(1.1f, h.b - h.a - 0.1f);
                    var rmn = L3(h.side, m - rw / 2, y, back - sg * 0.02f); var rmx = L3(h.side, m + rw / 2, y + 0.85f, back - sg * Mathf.Min(0.6f, h.depth + h.proj - 0.05f));
                    solid.Box(CotMats.iron, Vector3.Min(rmn, rmx), Vector3.Max(rmn, rmx));
                    deco.BoxB(CotMats.brass, L3(h.side, m, y + 0.86f, face - sg * 0.03f), n.x != 0 ? new Vector3(0.03f, 0.03f, rw) : new Vector3(rw, 0.03f, 0.03f));
                    deco.Cyl(CotMats.copper, L3(h.side, m - 0.25f, y + 0.86f, (face + back) / 2f), 0.12f, 0.16f, 10);
                    if (h.kind == "range") continue;
                }
                if (h.kind == "stove")
                {
                    deco.BoxB(CotMats.iron, mid, new Vector3(0.5f, 0.6f, 0.45f));
                    deco.Cyl(CotMats.iron, mid + Vector3.up * 0.6f, 0.07f, hh - 0.6f, 8);
                    deco.Sphere(CotMats.fire, mid + Vector3.up * 0.25f + new Vector3(-n.x, 0, -n.y) * 0.23f, new Vector3(0.12f, 0.08f, 0.12f), 8, 4);
                }
                else
                {
                    // dog grate, logs and fire
                    deco.BoxB(CotMats.iron, mid, n.x != 0 ? new Vector3(0.35f, 0.12f, 0.6f) : new Vector3(0.6f, 0.12f, 0.35f));
                    for (int q = 0; q < 3; q++)
                    {
                        float t = (q - 1) * 0.12f;
                        Vector3 c = mid + Vector3.up * (0.16f + (q == 1 ? 0.06f : 0)) + (n.x != 0 ? new Vector3(t, 0, 0) : new Vector3(0, 0, t));
                        deco.Push(Matrix4x4.TRS(c, Quaternion.Euler(0, n.x != 0 ? 90f : 0f, 0), Vector3.one));
                        deco.CylX(CotMats.oak, Vector3.zero, 0.06f, 0.55f, 7);
                        deco.Pop();
                    }
                    deco.Sphere(CotMats.fire, mid + Vector3.up * 0.28f, new Vector3(0.22f, 0.18f, 0.22f), 8, 5);
                    if (h.kind == "forge")
                    {
                        Vector3 anv = L3(h.side, m, y, face - sg * 1.2f);
                        deco.BoxB(CotMats.oak, anv, new Vector3(0.4f, 0.5f, 0.4f));
                        deco.BoxB(CotMats.iron, anv + Vector3.up * 0.5f, new Vector3(0.6f, 0.22f, 0.2f));
                    }
                }
                fireSpots.Add(mid + Vector3.up * 0.45f + new Vector3(-n.x, 0, -n.y) * 0.4f);
            }
        }

        // ======================= build =======================
        public GameObject Build(Transform parent, Vector3 worldPos, float yawDeg)
        {
            CotMats.Init();
            limeM = CotMats.Lime(pl.walls); frameM = CotMats.Paint(pl.frame); doorM = CotMats.Paint(pl.door); strawM = CotMats.Straw(pl.thatch);
            var g = new GameObject(pl.name); root = g.transform; root.SetParent(parent, false);
            root.localPosition = worldPos; root.localRotation = Quaternion.Euler(0, yawDeg, 0);
            solid = new MeshKit(); deco = new MeshKit(); roofKit = new MeshKit(); furn = new MeshKit();

            MakeRoofs();
            Classify(); Columns(); Openings(); EmitWalls();
            Floors(); Stairs(); Glazing(); Windows(); Doors(); ExtDoors(); Chimneys(); Hearths();
            BuildRoof();
            Furnish();
            if (P.sign != null) Sign();

            solid.Build("Shell", root, true);
            roofKit.Build("Roof", root, false, ShadowCastingMode.On, true);
            var decoGo = deco.Build("Interior", root, false, ShadowCastingMode.Off);
            var furnGo = furn.Build("Furniture", root, true, ShadowCastingMode.Off);

            // fire lights and crackle
            var lights = new List<Light>();
            foreach (var p in fireSpots)
            {
                var lg = new GameObject("FireLight"); lg.transform.SetParent(root, false); lg.transform.localPosition = p;
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 6f; l.color = new Color(1f, 0.58f, 0.3f); l.intensity = 2.5f; l.shadows = LightShadows.None;
                lg.AddComponent<FireFlicker>(); lights.Add(l);
                var src = lg.AddComponent<AudioSource>(); src.clip = FireClip ??= AudioSynth.Fire(); src.loop = true; src.spatialBlend = 1f; src.minDistance = 1.2f; src.maxDistance = 10f; src.volume = 0.5f; src.rolloffMode = AudioRolloffMode.Linear; src.Play();
            }
            // interior zones for audio muffling / footstep surface
            foreach (var w in P.wings) zones.Add(w.r);
            GameAudio.RegisterZone(root, zones, Mathf.Max(Y1 + 2.6f, 5f));
            var rz = g.AddComponent<AudioReverbZone>();
            rz.reverbPreset = AudioReverbPreset.Livingroom;
            float diag = 0f; foreach (var w in P.wings) diag = Mathf.Max(diag, Mathf.Max(w.r.x1, w.r.d1));
            rz.minDistance = diag * 0.35f; rz.maxDistance = diag * 0.55f;
            var lod = g.AddComponent<CotLOD>(); lod.Setup(decoGo, furnGo, lights);
            return g;
        }

        void Sign()
        {
            foreach (var W in P.wins)
            {
                if (!W.Opt("barn")) continue;
                var rm = P.Room(W.room); float m = (W.a + W.b) / 2f, e = RoomEdge(rm, W.side, m), o = OuterFace(W.side, e, m);
                var n = SideN(W.side); float sg = n.x + n.y, y = rm.y + W.y1 + 0.25f;
                Vector3 b0 = L3(W.side, W.a, y, o), b1 = L3(W.side, W.b, y + 0.34f, o + sg * 0.05f);
                deco.Box(CotMats.Paint(new Color(0.16f, 0.16f, 0.15f)), Vector3.Min(b0, b1), Vector3.Max(b0, b1));
                var tg = new GameObject("Sign"); tg.transform.SetParent(root, false);
                tg.transform.localPosition = L3(W.side, m, y + 0.17f, o + sg * 0.06f);
                tg.transform.localRotation = Quaternion.LookRotation(new Vector3(-n.x, 0, -n.y));
                var tm = tg.AddComponent<TextMesh>(); tm.text = P.sign; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
                tm.characterSize = 0.05f; tm.fontSize = 48; tm.color = new Color(0.85f, 0.78f, 0.55f);
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); tm.font = font; tg.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                break;
            }
        }
    }

    /// <summary>Hides furniture/interior detail and fire lights when the camera is far away (mobile budget).</summary>
    public class CotLOD : MonoBehaviour
    {
        GameObject deco, furn; List<Light> lights; float next;
        public void Setup(GameObject d, GameObject f, List<Light> l) { deco = d; furn = f; lights = l; }
        void Update()
        {
            if (Time.time < next) return; next = Time.time + 0.4f + Random.value * 0.2f;
            var cam = Camera.main; if (cam == null) return;
            float dist = Vector3.Distance(cam.transform.position, transform.position);
            bool near = dist < 42f;
            if (deco != null && deco.activeSelf != near) deco.SetActive(near);
            if (furn != null && furn.activeSelf != near) furn.SetActive(near);
            foreach (var l in lights) if (l != null) l.enabled = dist < 30f;
        }
    }
}
