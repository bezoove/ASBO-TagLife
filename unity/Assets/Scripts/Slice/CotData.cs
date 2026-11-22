using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>Tiny JSON reader (objects, arrays, strings, numbers, bools, null).</summary>
    public static class MiniJson
    {
        public static object Parse(string s) { int i = 0; return Val(s, ref i); }
        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
        static object Val(string s, ref int i)
        {
            Ws(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>(); i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Ws(s, ref i); var k = (string)Val(s, ref i); Ws(s, ref i); i++; // ':'
                    d[k] = Val(s, ref i); Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; return d;
                }
            }
            if (c == '[')
            {
                var l = new List<object>(); i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Val(s, ref i)); Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; return l;
                }
            }
            if (c == '"')
            {
                var sb = new StringBuilder(); i++;
                while (s[i] != '"')
                {
                    if (s[i] == '\\') { i++; char e = s[i]; sb.Append(e == 'n' ? '\n' : e == 't' ? '\t' : e); i++; }
                    else sb.Append(s[i++]);
                }
                i++; return sb.ToString();
            }
            if (c == 't') { i += 4; return true; }
            if (c == 'f') { i += 5; return false; }
            if (c == 'n') { i += 4; return null; }
            int st = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(st, i - st), CultureInfo.InvariantCulture);
        }

        public static float F(object o) => o == null || o is string || o is bool ? (o is bool b && b ? 1f : 0f) : (float)System.Convert.ToDouble(o, CultureInfo.InvariantCulture);
        public static List<object> L(object o) => o as List<object>;
        public static Dictionary<string, object> M(object o) => o as Dictionary<string, object>;
        public static object G(Dictionary<string, object> d, string k) => d != null && d.TryGetValue(k, out var v) ? v : null;
        public static float GF(Dictionary<string, object> d, string k, float def = 0f) { var v = G(d, k); return v == null ? def : F(v); }
        public static string GS(Dictionary<string, object> d, string k) => G(d, k) as string;
        public static bool GB(Dictionary<string, object> d, string k) { var v = G(d, k); return v is bool b ? b : v is double x && x != 0; }
        public static float[] FA(object o) { var l = L(o); if (l == null) return null; var a = new float[l.Count]; for (int i = 0; i < a.Length; i++) a[i] = F(l[i]); return a; }
    }

    public class CRect
    {
        public float x0, x1, d0, d1;
        public CRect(float a, float b, float c, float d) { x0 = a; x1 = b; d0 = c; d1 = d; }
        public CRect(float[] a) : this(a[0], a[1], a[2], a[3]) { }
        public bool In(float x, float d, float e = 0f) => x >= x0 - e && x <= x1 + e && d >= d0 - e && d <= d1 + e;
        public float Area => (x1 - x0) * (d1 - d0);
        public float Cx => (x0 + x1) * 0.5f;
        public float Cd => (d0 + d1) * 0.5f;
    }
    public class CCircle { public float cx, cd, r; }
    public class CWing
    {
        public string id, ridge = "x", roof, mat = "thatch";
        public CRect r; public CCircle circle;
        public float eave, up, kk, hipK, ov = -1f, vg = -1f;
        public float[] hip, roofExt, roofR;
        public List<float[]> brows = new List<float[]>();
        public bool glazed;
    }
    public class CRoom
    {
        public string key, name, type, ceil;
        public int lv; public float y, cmax;
        public List<CRect> r = new List<CRect>();
        public CCircle circle; public float clip;
        public Dictionary<string, object> o;
        public bool Opt(string k) => MiniJson.GB(o, k);
        public string OptS(string k) => MiniJson.GS(o, k);
        public CRect Main { get { CRect best = null; foreach (var q in r) if (best == null || q.Area > best.Area) best = q; return best; } }
        public bool In(float x, float d, float e = 0f)
        {
            if (circle != null) return Mathf.Sqrt((x - circle.cx) * (x - circle.cx) + (d - circle.cd) * (d - circle.cd)) <= circle.r + e && (clip == 0f || x >= clip - e);
            foreach (var q in r) if (q.In(x, d, e)) return true;
            return false;
        }
    }
    public class CFlight { public CRect r; public string dir; public float y0, y1; }
    public class CStair { public int lo, hi; public string kind = "cottage"; public bool open; public List<CFlight> f = new List<CFlight>(); }
    public class CDoor { public int lv; public string line, kind = "latch"; public float at, a, b, hgt = 1.9f; }
    public class CExt { public string kind, room, side; public float a, b, hgt; public bool stable; }
    public class CWin { public string room, side; public float a, b, y0, y1; public Dictionary<string, object> o; public bool Opt(string k) => MiniJson.GB(o, k); public float OptF(string k, float d) => MiniJson.GF(o, k, d); }
    public class CHearth { public string room, side, kind; public float a, b, proj, depth; public bool range; }
    public class CChim { public CRect r; public int pots = 2; public bool brick, shoulder; public float tall = 1f; public float[] flue; }
    public class CPlan
    {
        public string id, kind, porch = "hood", sign;
        public bool porchOpen, porchBig, horseshoe;
        public float ox, fg, Y1, CE0, CC = 2.35f, kk = 1.05f, Y2;
        public List<CWing> wings = new List<CWing>();
        public List<CRoom> rooms = new List<CRoom>();
        public List<CStair> stairs = new List<CStair>();
        public List<CDoor> doors = new List<CDoor>();
        public List<CExt> ext = new List<CExt>();
        public List<CWin> wins = new List<CWin>();
        public List<CHearth> hearths = new List<CHearth>();
        public List<CChim> chims = new List<CChim>();
        public CRoom Room(string k) { foreach (var r in rooms) if (r.key == k) return r; return null; }
    }
    public class CPlot
    {
        public string id, name, side, arch;
        public float[] r; public bool big;
        public Color walls, door, frame, thatch;
    }

    /// <summary>Loads the ten Abbots Wend cottage plans exported from the browser build (Resources/Cottages/plans.json).</summary>
    public static class CotData
    {
        public static List<CPlot> Plots;
        public static Dictionary<string, CPlan> Plans;
        public static Dictionary<string, object> Estate;

        static Color Col(object o, Color def) => o is string s && ColorUtility.TryParseHtmlString(s, out var c) ? c : def;

        public static bool Load()
        {
            if (Plans != null) return true;
            var ta = Resources.Load<TextAsset>("Cottages/plans");
            if (ta == null) { Debug.LogError("[ASBO] Resources/Cottages/plans.json missing"); return false; }
            var root = MiniJson.M(MiniJson.Parse(ta.text));
            Estate = MiniJson.M(MiniJson.G(root, "estate"));
            Plots = new List<CPlot>();
            foreach (var po in MiniJson.L(MiniJson.G(root, "plots")))
            {
                var d = MiniJson.M(po);
                Plots.Add(new CPlot
                {
                    id = MiniJson.GS(d, "id"), name = MiniJson.GS(d, "name"), side = MiniJson.GS(d, "side"), arch = MiniJson.GS(d, "arch"),
                    r = MiniJson.FA(MiniJson.G(d, "r")), big = MiniJson.GB(d, "big"),
                    walls = Col(MiniJson.G(d, "walls"), new Color(0.95f, 0.92f, 0.86f)), door = Col(MiniJson.G(d, "door"), new Color(0.2f, 0.3f, 0.2f)),
                    frame = Col(MiniJson.G(d, "frame"), new Color(0.92f, 0.9f, 0.83f)), thatch = Col(MiniJson.G(d, "thatch"), new Color(0.9f, 0.86f, 0.78f)),
                });
            }
            Plans = new Dictionary<string, CPlan>();
            foreach (var kv in MiniJson.M(MiniJson.G(root, "plans"))) Plans[kv.Key] = ParsePlan(MiniJson.M(kv.Value));
            return true;
        }

        public static CPlan ParsePlan(Dictionary<string, object> d)
        {
            var p = new CPlan
            {
                id = MiniJson.GS(d, "id"), kind = MiniJson.GS(d, "kind"),
                Y1 = MiniJson.GF(d, "Y1", 2.42f), CE0 = MiniJson.GF(d, "CE0", 2.2f), CC = MiniJson.GF(d, "CC", 2.35f), kk = MiniJson.GF(d, "kk", 1.05f), Y2 = MiniJson.GF(d, "Y2", 0f),
                sign = MiniJson.GS(d, "sign")
            };
            var place = MiniJson.M(MiniJson.G(d, "place")); p.ox = MiniJson.GF(place, "ox"); p.fg = MiniJson.GF(place, "fg");
            var porch = MiniJson.M(MiniJson.G(d, "porch"));
            if (porch != null) { p.porch = MiniJson.GS(porch, "kind") ?? "hood"; p.porchOpen = MiniJson.GB(porch, "open"); p.porchBig = MiniJson.GB(porch, "big"); p.horseshoe = MiniJson.GB(porch, "horseshoe"); }

            foreach (var wo in MiniJson.L(MiniJson.G(d, "wings")))
            {
                var w = MiniJson.M(wo);
                var cw = new CWing
                {
                    id = MiniJson.GS(w, "id"), ridge = MiniJson.GS(w, "ridge") ?? "x", roof = MiniJson.GS(w, "roof"), mat = MiniJson.GS(w, "mat") ?? "thatch",
                    eave = MiniJson.GF(w, "eave", 2.4f), up = MiniJson.GF(w, "up", 1f), kk = MiniJson.GF(w, "kk", 0f), hipK = MiniJson.GF(w, "hipK", 0f),
                    ov = MiniJson.GF(w, "ov", -1f), vg = MiniJson.GF(w, "vg", -1f),
                    hip = MiniJson.FA(MiniJson.G(w, "hip")), roofExt = MiniJson.FA(MiniJson.G(w, "roofExt")), roofR = MiniJson.FA(MiniJson.G(w, "roofR")),
                    glazed = MiniJson.GB(w, "glazed")
                };
                var r = MiniJson.FA(MiniJson.G(w, "r")); if (r != null) cw.r = new CRect(r);
                var c = MiniJson.M(MiniJson.G(w, "circle"));
                if (c != null) { cw.circle = new CCircle { cx = MiniJson.GF(c, "cx"), cd = MiniJson.GF(c, "cd"), r = MiniJson.GF(c, "r") }; cw.r = new CRect(cw.circle.cx - cw.circle.r, cw.circle.cx + cw.circle.r, cw.circle.cd - cw.circle.r, cw.circle.cd + cw.circle.r); }
                var br = MiniJson.L(MiniJson.G(w, "brows")); if (br != null) foreach (var b in br) cw.brows.Add(MiniJson.FA(b));
                p.wings.Add(cw);
            }

            foreach (var ro in MiniJson.L(MiniJson.G(d, "rooms")))
            {
                var r = MiniJson.M(ro);
                var cr = new CRoom { key = MiniJson.GS(r, "k"), name = MiniJson.GS(r, "n"), type = MiniJson.GS(r, "type"), lv = (int)MiniJson.GF(r, "lv"), o = MiniJson.M(MiniJson.G(r, "o")) };
                var rl = MiniJson.L(MiniJson.G(r, "r"));
                if (rl != null) foreach (var q in rl) { var a = MiniJson.FA(q); if (a != null && a.Length >= 4) cr.r.Add(new CRect(a)); }
                var circ = MiniJson.M(MiniJson.G(r, "circle"));
                if (circ != null)
                {
                    cr.circle = new CCircle { cx = MiniJson.GF(circ, "cx"), cd = MiniJson.GF(circ, "cd"), r = MiniJson.GF(circ, "r") };
                    cr.r.Clear(); cr.r.Add(new CRect(cr.circle.cx - cr.circle.r, cr.circle.cx + cr.circle.r, cr.circle.cd - cr.circle.r, cr.circle.cd + cr.circle.r));
                }
                cr.clip = MiniJson.GF(cr.o, "clip", 0f);
                cr.ceil = MiniJson.GS(cr.o, "ceil") ?? (cr.lv == 0 ? "flat" : cr.lv == 4 ? "vault" : "roof");
                float cm = MiniJson.GF(cr.o, "cmax", 0f);
                cr.cmax = cm > 0 ? cm : cr.lv == 0 ? (cr.ceil == "open" ? 99f : cr.ceil == "roof" ? 2.9f : p.CE0) : cr.lv == 1 ? p.Y1 + p.CC : 99f;
                cr.y = cr.lv == 0 ? 0f : cr.lv == 1 ? p.Y1 : cr.lv == 3 ? (p.Y2 > 0 ? p.Y2 : 99f) : -2.3f;
                p.rooms.Add(cr);
            }

            foreach (var so in MiniJson.L(MiniJson.G(d, "stairs")))
            {
                var s = MiniJson.M(so);
                var cs = new CStair { lo = (int)MiniJson.GF(s, "lo", 0f), hi = (int)MiniJson.GF(s, "hi", 1f), kind = MiniJson.GS(s, "kind") ?? "cottage", open = MiniJson.GB(s, "open") };
                float LvY(int lv) => lv == 0 ? 0f : lv == 1 ? p.Y1 : lv == 3 ? (p.Y2 > 0 ? p.Y2 : p.Y1 + 2.4f) : -2.3f;
                float y0 = LvY(cs.lo), y1 = LvY(cs.hi);
                var fl = MiniJson.L(MiniJson.G(s, "f"));
                float tot = 0f; var lens = new List<float>();
                foreach (var fo in fl)
                {
                    var f = MiniJson.L(fo); string dir = f[4] as string;
                    float L = dir == "0" ? 0f : dir.Contains("x") ? MiniJson.F(f[1]) - MiniJson.F(f[0]) : MiniJson.F(f[3]) - MiniJson.F(f[2]);
                    lens.Add(L); tot += L;
                }
                float y = y0;
                for (int i = 0; i < fl.Count; i++)
                {
                    var f = MiniJson.L(fl[i]);
                    float b = y + (y1 - y0) * (tot > 0 ? lens[i] / tot : 0f);
                    cs.f.Add(new CFlight { r = new CRect(MiniJson.F(f[0]), MiniJson.F(f[1]), MiniJson.F(f[2]), MiniJson.F(f[3])), dir = f[4] as string, y0 = y, y1 = b });
                    y = b;
                }
                p.stairs.Add(cs);
            }

            foreach (var dd in MiniJson.L(MiniJson.G(d, "doors")))
            {
                var a = MiniJson.L(dd);
                var cd = new CDoor { lv = (int)MiniJson.F(a[0]), line = a[1] as string, at = MiniJson.F(a[2]), a = MiniJson.F(a[3]), b = MiniJson.F(a[4]) };
                for (int i = 5; i < a.Count; i++)
                {
                    if (a[i] is string k) cd.kind = k;
                    else if (a[i] is Dictionary<string, object> o) { float h = MiniJson.GF(o, "h", 0f); if (h > 0) cd.hgt = h; }
                }
                if (cd.kind == "cellar") cd.hgt = 1.85f;
                cd.hgt = Mathf.Max(cd.hgt, cd.kind == "cellar" ? 1.95f : 2.02f); // room to walk through without catching your head on the lintel (floors sit a little proud of the ground)
                p.doors.Add(cd);
            }
            foreach (var eo in MiniJson.L(MiniJson.G(d, "ext")))
            {
                var a = MiniJson.L(eo);
                var ce = new CExt { kind = a[0] as string, room = a[1] as string, side = a[2] as string, a = MiniJson.F(a[3]), b = MiniJson.F(a[4]) };
                var o = a.Count > 5 ? MiniJson.M(a[5]) : null;
                ce.stable = MiniJson.GB(o, "stable");
                ce.hgt = Mathf.Max(2.02f, MiniJson.GF(o, "h", ce.kind == "front" ? 1.95f : 1.9f));
                p.ext.Add(ce);
            }
            foreach (var wo in MiniJson.L(MiniJson.G(d, "wins")))
            {
                var a = MiniJson.L(wo);
                p.wins.Add(new CWin { room = a[0] as string, side = a[1] as string, a = MiniJson.F(a[2]), b = MiniJson.F(a[3]), y0 = MiniJson.F(a[4]), y1 = MiniJson.F(a[5]), o = a.Count > 6 ? MiniJson.M(a[6]) : null });
            }
            foreach (var ho in MiniJson.L(MiniJson.G(d, "hearths")))
            {
                var h = MiniJson.M(ho);
                p.hearths.Add(new CHearth { room = MiniJson.GS(h, "room"), side = MiniJson.GS(h, "side"), kind = MiniJson.GS(h, "kind") ?? "fire", a = MiniJson.GF(h, "a"), b = MiniJson.GF(h, "b"), proj = MiniJson.GF(h, "proj"), depth = MiniJson.GF(h, "depth", 0.4f), range = MiniJson.GB(h, "range") });
            }
            foreach (var co in MiniJson.L(MiniJson.G(d, "chimneys")))
            {
                var c = MiniJson.M(co);
                var cc = new CChim { pots = (int)MiniJson.GF(c, "pots", 2f), brick = MiniJson.GB(c, "brick"), shoulder = MiniJson.GB(c, "shoulder"), tall = MiniJson.GF(c, "tall", 1f), flue = MiniJson.FA(MiniJson.G(c, "flue")) };
                var r = MiniJson.FA(MiniJson.G(c, "r")); if (r != null) cc.r = new CRect(r);
                p.chims.Add(cc);
            }
            return p;
        }
    }
}
