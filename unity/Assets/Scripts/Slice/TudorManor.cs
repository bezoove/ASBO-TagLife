using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// The Eggy Duck: a grand old Tudor manor house turned pub, on its own grounds at the east end of Abbots Wend,
    /// facing the main road. H-plan: a great hall range (the bar, with a big inglenook) between two gabled cross-wings,
    /// black oak framing on white limewash over a brick plinth, steep clay-tiled roofs, tall clustered brick chimneys,
    /// leaded mullion windows, and an old dairy wing out the back onto the beer garden. Inside it's old, worn and
    /// comfortable: flagged and boarded floors, beams, threadbare rugs, settles and fires.
    /// Ground floor: the great hall bar, the snug, the games room, the lounge, kitchen, toilets, a back hall with the
    /// stairs, an old store with its own door to the outside, the old dairy, and a cellar under the bar (hatch behind
    /// the counter). Upstairs: the long room over the bar, the landlord's rooms, two letting rooms, a bathroom, and the
    /// priest hole: a room with no window that you only find from the landing.
    /// Built with the cottage builder (CotHouse) from the plan below; the timber framing goes on after.
    /// </summary>
    public static class TudorManor
    {
        public const string RootName = "EggyDuckManor", PlotId = "eggyduck";
        /// <summary>Front-left corner of the house; its front faces the main road (south, -z).</summary>
        public static readonly Vector3 Origin = new Vector3(55f, 0f, -12f);
        /// <summary>The house, its car park and beer garden (the ground is levelled and cleared of trees here).</summary>
        public static readonly Rect Plot = new Rect(47f, -23f, 50f, 42f);

        // plan coordinates: x along the front (0..32), d back from the front (0..21); rooms sit inside 0.5 m walls
        const string Json = @"{""id"":""eggyduck"",""kind"":""Tudor manor (pub)"",""Y1"":3.0,""Y2"":6.4,""CE0"":2.85,""CC"":2.6,""kk"":1.2,
""wings"":[
 {""id"":""west"",""r"":[0,9,0,17],""eave"":5.9,""kk"":1.25,""ridge"":""d"",""mat"":""tiles""},
 {""id"":""hall"",""r"":[9,23,3,14],""eave"":5.9,""kk"":1.2,""ridge"":""x"",""mat"":""tiles""},
 {""id"":""east"",""r"":[23,32,0,17],""eave"":5.9,""kk"":1.25,""ridge"":""d"",""mat"":""tiles""},
 {""id"":""dairy"",""r"":[10,16.5,14,21],""eave"":3.4,""kk"":1.1,""ridge"":""d"",""mat"":""tiles""}],
""rooms"":[
 {""k"":""a"",""n"":""the snug"",""lv"":0,""type"":""snug"",""r"":[[0.5,8.5,0.5,7.4]]},
 {""k"":""b"",""n"":""the games room"",""lv"":0,""type"":""sitting"",""r"":[[0.5,8.5,7.52,12.4]]},
 {""k"":""c"",""n"":""the old store"",""lv"":0,""type"":""larder"",""r"":[[0.5,3.0,12.52,16.5]]},
 {""k"":""d"",""n"":""the back hall"",""lv"":0,""type"":""hall"",""r"":[[3.12,8.5,12.52,16.5]]},
 {""k"":""e"",""n"":""the great hall"",""lv"":0,""type"":""bar"",""r"":[[9.0,23.0,3.5,13.5]]},
 {""k"":""f"",""n"":""the lounge"",""lv"":0,""type"":""dining"",""r"":[[23.5,31.5,0.5,7.4]]},
 {""k"":""g"",""n"":""the kitchen"",""lv"":0,""type"":""kitchen"",""r"":[[23.5,31.5,7.52,12.4]]},
 {""k"":""i"",""n"":""the back passage"",""lv"":0,""type"":""passage"",""r"":[[23.5,27.0,12.52,16.5]]},
 {""k"":""h"",""n"":""the toilets"",""lv"":0,""type"":""wc"",""r"":[[27.12,31.5,12.52,16.5]]},
 {""k"":""j"",""n"":""the old dairy"",""lv"":0,""type"":""sitting"",""r"":[[10.5,16.0,14.0,20.5]],""o"":{""ceil"":""roof"",""cmax"":3.4}},
 {""k"":""k"",""n"":""the cellar"",""lv"":4,""type"":""cellar"",""r"":[[9.5,22.5,4.0,13.5]]},
 {""k"":""l"",""n"":""the landlord's sitting room"",""lv"":1,""type"":""sitting"",""r"":[[0.5,8.5,0.5,7.4]]},
 {""k"":""m"",""n"":""the landlord's bedroom"",""lv"":1,""type"":""bed"",""r"":[[0.5,8.5,7.52,12.4]],""o"":{""bed"":""four"",""master"":1}},
 {""k"":""o"",""n"":""the priest hole"",""lv"":1,""type"":""box"",""r"":[[0.5,3.0,12.52,16.5]]},
 {""k"":""n"",""n"":""the west landing"",""lv"":1,""type"":""landing"",""r"":[[3.12,8.5,12.52,16.5]]},
 {""k"":""p"",""n"":""the long room"",""lv"":1,""type"":""drawing"",""r"":[[9.0,23.0,3.5,13.5]]},
 {""k"":""q"",""n"":""the oak room"",""lv"":1,""type"":""bed"",""r"":[[23.5,31.5,0.5,7.4]],""o"":{""bed"":""brass""}},
 {""k"":""r"",""n"":""the duck room"",""lv"":1,""type"":""bed"",""r"":[[23.5,31.5,7.52,12.4]],""o"":{""bed"":""iron""}},
 {""k"":""s"",""n"":""the east landing"",""lv"":1,""type"":""landing"",""r"":[[23.5,27.0,12.52,16.5]]},
 {""k"":""t"",""n"":""the bathroom"",""lv"":1,""type"":""bath"",""r"":[[27.12,31.5,12.52,16.5]]}],
""stairs"":[
 {""f"":[[3.3,6.6,15.3,16.4,""+x""]],""open"":1},
 {""f"":[[23.7,26.9,15.3,16.4,""+x""]]},
 {""lo"":4,""hi"":0,""f"":[[20.4,22.8,12.3,13.4,""-x""]],""kind"":""cellar"",""open"":1}],
""doors"":[
 [0,""X"",8.75,5.0,6.0],[0,""X"",8.75,9.2,10.2],[0,""X"",8.75,12.65,13.4],[0,""D"",7.46,2.0,2.9],[0,""D"",12.46,5.0,5.9],[0,""X"",3.06,14.0,14.85],
 [0,""X"",23.25,5.0,6.0],[0,""X"",23.25,9.5,10.4],[0,""X"",23.25,12.65,13.4],[0,""X"",27.06,13.0,13.85],[0,""D"",12.46,24.0,24.9],[0,""D"",7.46,29.6,30.5],
 [0,""D"",13.75,11.5,12.4],
 [1,""D"",7.46,2.0,2.9],[1,""D"",12.46,5.0,5.9],[1,""X"",3.06,14.0,14.85],[1,""X"",8.75,12.65,13.4],[1,""X"",23.25,12.65,13.4],[1,""D"",12.46,24.0,24.9],
 [1,""D"",7.46,24.0,24.9],[1,""X"",27.06,13.0,13.85]],
""ext"":[[""front"",""e"",""F"",15.4,16.6,{""h"":2.2}],[""back"",""c"",""L"",14.6,15.5],[""back"",""g"",""R"",11.0,11.9,{""stable"":1}],[""garden"",""j"",""B"",12.6,13.5,{""glazed"":1}]],
""wins"":[
 [""a"",""F"",1.4,3.8,0.7,2.3,{""lights"":4}],[""a"",""F"",5.2,7.6,0.7,2.3,{""lights"":4}],[""a"",""L"",5.6,6.8,0.8,2.2],[""b"",""L"",8.6,10.6,0.8,2.2,{""lights"":3}],
 [""c"",""L"",12.9,13.6,1.3,1.8,{""vent"":1}],[""e"",""F"",10.2,13.6,0.7,2.4,{""lights"":5,""seat"":1}],[""e"",""F"",18.4,21.8,0.7,2.4,{""lights"":5,""seat"":1}],
 [""e"",""B"",20.6,22.2,0.9,2.3],[""f"",""F"",24.4,26.8,0.7,2.3,{""lights"":4}],[""f"",""F"",28.2,30.6,0.7,2.3,{""lights"":4}],[""f"",""R"",1.2,2.8,0.8,2.2],
 [""g"",""R"",8.4,10.2,0.9,2.0,{""lights"":3}],[""h"",""R"",13.6,14.6,1.4,2.0,{""frost"":1}],[""i"",""B"",23.9,25.0,0.9,2.0],[""h"",""B"",28.5,29.5,1.4,2.0,{""frost"":1}],
 [""j"",""L"",16.0,18.2,0.7,2.2,{""lights"":3}],[""j"",""R"",16.0,18.2,0.7,2.2,{""lights"":3}],[""j"",""B"",10.9,12.1,0.8,2.1],[""j"",""B"",14.2,15.6,0.8,2.1],
 [""l"",""F"",1.4,3.8,0.5,2.0,{""lights"":4}],[""l"",""F"",5.2,7.6,0.5,2.0,{""lights"":4}],[""m"",""L"",8.6,10.6,0.5,1.9],[""n"",""B"",4.0,5.0,0.6,1.8],
 [""p"",""F"",10.0,12.6,0.5,2.1,{""lights"":4}],[""p"",""F"",14.7,17.3,0.5,2.1,{""lights"":4}],[""p"",""F"",19.4,22.0,0.5,2.1,{""lights"":4}],[""p"",""B"",20.6,22.3,0.6,1.9],
 [""q"",""F"",24.4,26.8,0.5,2.0,{""lights"":4}],[""q"",""F"",28.2,30.6,0.5,2.0,{""lights"":4}],[""r"",""R"",8.6,10.6,0.5,1.9],[""t"",""R"",13.6,14.6,0.7,1.6,{""frost"":1}],
 [""s"",""B"",24.0,25.0,0.6,1.8]],
""hearths"":[
 {""room"":""a"",""side"":""L"",""a"":3.2,""b"":4.8,""kind"":""fire"",""proj"":0.42,""depth"":0.4},{""room"":""l"",""side"":""L"",""a"":3.2,""b"":4.8,""kind"":""fire"",""proj"":0.42,""depth"":0.4},
 {""room"":""f"",""side"":""R"",""a"":4.5,""b"":6.1,""kind"":""fire"",""proj"":0.42,""depth"":0.4},{""room"":""q"",""side"":""R"",""a"":4.5,""b"":6.1,""kind"":""fire"",""proj"":0.42,""depth"":0.4},
 {""room"":""e"",""side"":""B"",""a"":17.4,""b"":19.6,""kind"":""inglenook"",""proj"":0.3,""depth"":0.6},{""room"":""p"",""side"":""B"",""a"":17.6,""b"":19.4,""kind"":""fire"",""proj"":0.42,""depth"":0.4}],
""chimneys"":[{""r"":[-0.9,0,3.0,5.0],""pots"":3,""brick"":1,""tall"":1.5,""shoulder"":1},{""r"":[32,32.9,4.3,6.3],""pots"":3,""brick"":1,""tall"":1.5,""shoulder"":1},{""r"":[17.0,20.0,14.0,15.0],""pots"":4,""brick"":1,""tall"":1.7}],
""porch"":{""kind"":""hood""}}";

        static CPlan plan;
        public static CPlan Plan { get { if (plan == null) plan = CotData.ParsePlan(MiniJson.M(MiniJson.Parse(Json))); return plan; } }
        static CPlot Plot0 => new CPlot
        {
            id = PlotId, name = "The Eggy Duck", side = "+z", arch = "gentry", big = true,
            walls = new Color(0.94f, 0.91f, 0.84f), door = new Color(0.17f, 0.11f, 0.07f), frame = new Color(0.1f, 0.08f, 0.06f), thatch = new Color(0.62f, 0.32f, 0.22f),
        };

        /// <summary>Build the manor and its grounds under parent (edit time, with the rest of the town).</summary>
        public static Transform Build(Transform parent)
        {
            CotMats.Init();
            var root = new GameObject(RootName).transform; root.SetParent(parent, false);
            var h = new CotHouse(Plot0, Plan);
            GameObject g;
            try { g = h.Build(root, Origin, 0f); }
            catch (System.Exception e) { Debug.LogError("[ASBO] Eggy Duck manor failed: " + e); Object.DestroyImmediate(root.gameObject); return null; }
            Timbers(g.transform, Plan);
            Grounds(root);
            Debug.Log("[ASBO] the Eggy Duck: Tudor manor built, " + Plan.rooms.Count + " rooms");
            return root;
        }

        /// <summary>Baked scene: register the manor's indoors for sound (muffled outside, its own reverb).</summary>
        public static void Attach()
        {
            var r = GameObject.Find(RootName); if (r == null) return;
            var house = r.transform.Find("The Eggy Duck"); if (house == null) return;
            var zones = new List<CRect>(); foreach (var w in Plan.wings) zones.Add(w.r);
            GameAudio.RegisterZone(house, zones, 6.5f);
        }

        // ------------------------------------------------------------------ timber framing
        static Material oakBlack;
        /// <summary>
        /// Black oak framing over the limewash: a brick plinth, sole plate, close studding on both floors, the big
        /// bressumer beam at the first floor, the wall plate under the eaves, frames round every window and door, curved
        /// braces at the corners upstairs, and studded gables. Only on outside walls (not where wings meet).
        /// </summary>
        static void Timbers(Transform house, CPlan P)
        {
            oakBlack ??= Gfx.Mat(new Color(0.085f, 0.07f, 0.055f), null, 0.16f);
            var k = new MeshKit(); var brick = CotMats.brick != null ? CotMats.brick : oakBlack;
            foreach (var w in P.wings) foreach (var side in new[] { "F", "B", "L", "R" }) Side(k, P, w, side, brick);
            var go = k.Build("Timbers", house, false);
            if (go != null) go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        struct Hole { public float a, b, y0, y1; }

        static void Side(MeshKit k, CPlan P, CWing w, string side, Material brick)
        {
            bool alongX = side == "F" || side == "B";
            float a0 = alongX ? w.r.x0 : w.r.d0, a1 = alongX ? w.r.x1 : w.r.d1;
            float line = side == "F" ? w.r.d0 : side == "B" ? w.r.d1 : side == "L" ? w.r.x0 : w.r.x1;
            float n = side == "F" || side == "L" ? -1f : 1f; // outward
            // which stretches of this side are outside walls (no other wing, chimney or lean-to against them)
            bool Outside(float t)
            {
                float px = alongX ? t : line + n * 0.3f, pd = alongX ? line + n * 0.3f : t;
                foreach (var o in P.wings) if (o != w && o.r.In(px, pd)) return false;
                foreach (var c in P.chims) if (c.r != null && c.r.In(px, pd, 0.15f)) return false;
                return true;
            }
            // the openings in this wall
            var holes = new List<Hole>();
            foreach (var W in P.wins) { var rm = P.Room(W.room); if (W.side == side && OnLine(rm, side, line)) holes.Add(new Hole { a = W.a, b = W.b, y0 = rm.y + W.y0, y1 = rm.y + W.y1 }); }
            foreach (var E in P.ext) { var rm = P.Room(E.room); if (E.side == side && OnLine(rm, side, line)) holes.Add(new Hole { a = E.a, b = E.b, y0 = rm.y, y1 = rm.y + E.hgt }); }
            foreach (var hh in P.hearths) { var rm = P.Room(hh.room); if (hh.side == side && OnLine(rm, side, line)) holes.Add(new Hole { a = hh.a - 0.2f, b = hh.b + 0.2f, y0 = -1f, y1 = 99f }); } // chimney breasts: leave plain

            float Y1 = P.Y1, eave = w.eave; bool two = eave > Y1 + 1.2f;
            const float plinth = 0.45f, stud = 0.16f, gap = 0.42f, th = 0.05f;
            // a piece of timber from (t0..t1 along, y0..y1 up), sitting proud of the wall face
            void Piece(Material m, float t0, float t1, float y0, float y1, float proud = th)
            {
                if (t1 - t0 < 0.01f || y1 - y0 < 0.01f) return;
                float f0 = line, f1 = line + n * proud;
                Vector3 mn = alongX ? new Vector3(t0, y0, Mathf.Min(f0, f1)) : new Vector3(Mathf.Min(f0, f1), y0, t0);
                Vector3 mx = alongX ? new Vector3(t1, y1, Mathf.Max(f0, f1)) : new Vector3(Mathf.Max(f0, f1), y1, t1);
                k.Box(m, mn, mx);
            }
            // walk the side in outside stretches
            float t = a0;
            while (t < a1 - 0.05f)
            {
                while (t < a1 - 0.05f && !Outside(t + 0.05f)) t += 0.1f;
                if (t >= a1 - 0.05f) break;
                float s0 = t; while (t < a1 - 0.01f && Outside(t + 0.05f)) t += 0.1f;
                float s1 = Mathf.Min(t, a1); if (s1 - s0 < 0.5f) continue;
                // plinth, sole plate, bressumer, wall plate, corner posts
                Piece(brick, s0, s1, 0f, plinth, th + 0.03f);
                Piece(oakBlack, s0, s1, plinth, plinth + 0.18f);
                if (two) Piece(oakBlack, s0, s1, Y1 - 0.08f, Y1 + 0.24f, th + 0.05f);
                Piece(oakBlack, s0, s1, eave - 0.24f, eave, th + 0.02f);
                Piece(oakBlack, s0, s0 + 0.24f, plinth, eave); Piece(oakBlack, s1 - 0.24f, s1, plinth, eave);
                // close studding, storey by storey, stopping at the openings
                var storeys = two ? new[] { (plinth + 0.18f, Y1 - 0.08f), (Y1 + 0.24f, eave - 0.24f) } : new[] { (plinth + 0.18f, eave - 0.24f) };
                foreach (var (y0, y1) in storeys)
                {
                    for (float x = s0 + 0.24f + gap; x < s1 - 0.24f - stud; x += gap + stud)
                    {
                        float bot = y0, top = y1; bool cut = false;
                        foreach (var hl in holes)
                        {
                            if (x + stud < hl.a - 0.12f || x > hl.b + 0.12f || hl.y1 < y0 || hl.y0 > y1) continue;
                            cut = true;
                            if (hl.y0 - y0 > 0.2f) Piece(oakBlack, x, x + stud, y0, hl.y0 - 0.1f);
                            if (y1 - hl.y1 > 0.2f) Piece(oakBlack, x, x + stud, hl.y1 + 0.12f, y1);
                        }
                        if (!cut) Piece(oakBlack, x, x + stud, bot, top);
                    }
                    // a mid rail on the ground floor, as the old builders did
                    if (y0 < 1f && two) foreach (var seg in Gaps(holes, s0 + 0.24f, s1 - 0.24f, 1.55f)) Piece(oakBlack, seg.x, seg.y, 1.5f, 1.62f);
                }
                // frame round each opening in this stretch
                foreach (var hl in holes)
                {
                    if (hl.b < s0 || hl.a > s1 || hl.y0 < -0.5f) continue;
                    Piece(oakBlack, hl.a - 0.14f, hl.a - 0.02f, Mathf.Max(plinth, hl.y0 - 0.12f), hl.y1 + 0.14f, th + 0.01f);
                    Piece(oakBlack, hl.b + 0.02f, hl.b + 0.14f, Mathf.Max(plinth, hl.y0 - 0.12f), hl.y1 + 0.14f, th + 0.01f);
                    Piece(oakBlack, hl.a - 0.14f, hl.b + 0.14f, hl.y1 + 0.02f, hl.y1 + 0.16f, th + 0.01f);
                    if (hl.y0 > plinth + 0.3f) Piece(oakBlack, hl.a - 0.14f, hl.b + 0.14f, hl.y0 - 0.14f, hl.y0 - 0.02f, th + 0.01f);
                }
                // curved braces up from the corner posts on the top floor
                if (two)
                {
                    foreach (int end in new[] { 0, 1 })
                    {
                        float px = end == 0 ? s0 + 0.24f : s1 - 0.24f, dir = end == 0 ? 1f : -1f;
                        bool clear = true; foreach (var hl in holes) if (hl.y1 > Y1 && hl.y0 < eave && Mathf.Min(hl.a, hl.b) < Mathf.Max(px, px + dir * 1.3f) && Mathf.Max(hl.a, hl.b) > Mathf.Min(px, px + dir * 1.3f)) clear = false;
                        if (!clear) continue;
                        Brace(k, alongX, line, n, px, Y1 + 0.24f, px + dir * 1.15f, eave - 0.24f);
                    }
                }
                // a studded gable above the eaves on the gabled ends
                bool gableEnd = (w.ridge == "d" && alongX) || (w.ridge == "x" && !alongX);
                if (gableEnd && Mathf.Abs(s0 - a0) < 0.15f && Mathf.Abs(s1 - a1) < 0.15f)
                {
                    float kk = w.kk > 0 ? w.kk : P.kk, half = (a1 - a0) * 0.5f;
                    Piece(oakBlack, a0, a1, eave, eave + 0.22f, th + 0.04f); // tie beam
                    for (float x = a0 + 0.3f; x < a1 - 0.3f; x += gap + stud)
                    {
                        float top = eave + kk * Mathf.Min(x - a0, a1 - x - stud) - 0.25f;
                        if (top > eave + 0.45f) Piece(oakBlack, x, x + stud, eave + 0.22f, top);
                    }
                    Piece(oakBlack, a0 + half - 0.12f, a0 + half + 0.12f, eave, eave + kk * half - 0.15f, th + 0.03f); // king post
                }
            }
        }

        static bool OnLine(CRoom rm, string side, float line)
        {
            if (rm == null) return false;
            float e = side == "F" ? float.MaxValue : side == "L" ? float.MaxValue : float.MinValue;
            foreach (var q in rm.r)
            {
                if (side == "F") e = Mathf.Min(e, q.d0); else if (side == "B") e = Mathf.Max(e, q.d1);
                else if (side == "L") e = Mathf.Min(e, q.x0); else e = Mathf.Max(e, q.x1);
            }
            return Mathf.Abs(Mathf.Abs(e - line) - 0.5f) < 0.35f || Mathf.Abs(e - line) < 0.35f;
        }

        /// <summary>The bits of [a, b] at height y that aren't in an opening.</summary>
        static List<Vector2> Gaps(List<Hole> holes, float a, float b, float y)
        {
            var cuts = new List<Vector2>();
            foreach (var h in holes) if (h.y0 <= y && h.y1 >= y) cuts.Add(new Vector2(h.a - 0.16f, h.b + 0.16f));
            cuts.Sort((p, q) => p.x.CompareTo(q.x));
            var res = new List<Vector2>(); float t = a;
            foreach (var c in cuts) { if (c.x > t) res.Add(new Vector2(t, Mathf.Min(c.x, b))); t = Mathf.Max(t, c.y); }
            if (t < b) res.Add(new Vector2(t, b));
            return res;
        }

        static void Brace(MeshKit k, bool alongX, float line, float n, float t0, float y0, float t1, float y1)
        {
            // a slightly bowed brace: three straight pieces
            Vector2 p0 = new Vector2(t0, y0), p3 = new Vector2(t1, y1), mid = (p0 + p3) * 0.5f + new Vector2(-(p3.y - p0.y), p3.x - p0.x).normalized * 0.12f * Mathf.Sign(t1 - t0);
            Vector2[] pts = { p0, Vector2.Lerp(p0, mid, 0.9f), Vector2.Lerp(mid, p3, 0.1f), p3 };
            for (int i = 0; i < 3; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1], d = b - a; float len = d.magnitude; if (len < 0.01f) continue;
                float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; Vector2 c = (a + b) * 0.5f;
                float face = line + n * 0.04f;
                Vector3 centre = alongX ? new Vector3(c.x, c.y, face) : new Vector3(face, c.y, c.x);
                Quaternion q = alongX ? Quaternion.Euler(0f, 0f, ang) : Quaternion.Euler(0f, -90f, 0f) * Quaternion.Euler(0f, 0f, ang);
                k.Push(Matrix4x4.TRS(centre, q, Vector3.one));
                k.Box(oakBlack, new Vector3(-len * 0.5f - 0.04f, -0.07f, -0.035f), new Vector3(len * 0.5f + 0.04f, 0.07f, 0.035f));
                k.Pop();
            }
        }

        // ------------------------------------------------------------------ grounds
        /// <summary>
        /// Gravel car park out front behind a low brick wall, a flagged path to the door, the sign on its post by the
        /// road, and the beer garden behind: lawn, picnic benches, a low wall round it.
        /// </summary>
        static void Grounds(Transform root)
        {
            var k = new MeshKit();
            var gravel = CotMats.gravel != null ? CotMats.gravel : CotMats.flag; var flag = CotMats.flag != null ? CotMats.flag : gravel;
            var brick = CotMats.brick; var stone = CotMats.stone != null ? CotMats.stone : brick; var wood = CotMats.oak;
            float x0 = Origin.x - 6f, x1 = Origin.x + 38f, front = Origin.z, road = World.RoadZ + 7.7f;
            // car park and forecourt
            k.Box(gravel, new Vector3(x0, 0f, road + 0.35f), new Vector3(x1, 0.04f, front));
            k.Box(gravel, new Vector3(Origin.x + 9f, 0f, front), new Vector3(Origin.x + 23f, 0.04f, front + 2.95f)); // the court between the wings
            k.Box(flag, new Vector3(Origin.x + 15.2f, 0f, road + 0.35f), new Vector3(Origin.x + 16.8f, 0.06f, front + 2.98f));
            // low wall along the pavement with the gateway to the path and a wide gap for cars
            void Wall(float a, float b) { k.Box(brick, new Vector3(a, 0f, road + 0.05f), new Vector3(b, 0.75f, road + 0.35f)); k.Box(stone, new Vector3(a - 0.04f, 0.75f, road + 0.01f), new Vector3(b + 0.04f, 0.85f, road + 0.39f)); }
            Wall(x0, Origin.x + 4f); Wall(Origin.x + 11.5f, Origin.x + 14.8f); Wall(Origin.x + 17.2f, x1);
            foreach (float px in new[] { Origin.x + 14.4f, Origin.x + 17.0f }) k.Box(stone, new Vector3(px, 0f, road), new Vector3(px + 0.45f, 1.25f, road + 0.45f));
            // parking bays marked with old sleepers
            for (int i = 0; i < 6; i++) { float bx = x0 + 2.5f + i * 2.8f; if (bx > Origin.x + 9f) break; k.Box(wood, new Vector3(bx, 0f, front - 1.4f), new Vector3(bx + 0.18f, 0.12f, front - 0.2f)); }
            // the sign: a tall post by the road with the painted board hanging off its arm
            var signM = Resources.Load<Material>("Town/TownPubSign");
            Vector3 sp = new Vector3(Origin.x - 3.5f, 0f, road + 1.2f);
            k.Box(wood, sp + new Vector3(-0.12f, 0f, -0.12f), sp + new Vector3(0.12f, 4.6f, 0.12f));
            k.Box(wood, sp + new Vector3(-0.08f, 4.3f, -0.08f), sp + new Vector3(1.5f, 4.45f, 0.08f));
            k.Box(wood, sp + new Vector3(-0.06f, 3.4f, -0.06f), sp + new Vector3(0.9f, 3.5f, 0.06f));
            if (signM != null)
            {
                Vector3 b0 = sp + new Vector3(0.25f, 2.65f, 0f);
                k.QuadUV(signM, b0 + new Vector3(0, 0, -0.03f), b0 + new Vector3(1.15f, 0, -0.03f), b0 + new Vector3(1.15f, 1.5f, -0.03f), b0 + new Vector3(0, 1.5f, -0.03f), Vector3.back, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
                k.QuadUV(signM, b0 + new Vector3(1.15f, 0, 0.03f), b0 + new Vector3(0, 0, 0.03f), b0 + new Vector3(0, 1.5f, 0.03f), b0 + new Vector3(1.15f, 1.5f, 0.03f), Vector3.forward, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
                k.Box(wood, b0 + new Vector3(-0.05f, -0.05f, -0.04f), b0 + new Vector3(1.2f, 0f, 0.04f)); k.Box(wood, b0 + new Vector3(-0.05f, 1.5f, -0.04f), b0 + new Vector3(1.2f, 1.55f, 0.04f));
                foreach (float cx in new[] { 0.1f, 1.05f }) k.Box(CotMats.iron != null ? CotMats.iron : wood, b0 + new Vector3(cx - 0.01f, 1.55f, -0.01f), b0 + new Vector3(cx + 0.01f, 1.8f, 0.01f));
            }
            // the beer garden behind: low wall round a lawn, picnic benches
            float gz0 = Origin.z + 17f, gz1 = Origin.z + 30f, gx0 = Origin.x - 2f, gx1 = Origin.x + 34f;
            void GWall(Vector3 a, Vector3 b) { k.Box(brick, Vector3.Min(a, b) - new Vector3(0.15f, 0, 0.15f), Vector3.Max(a, b) + new Vector3(0.15f, 0.9f, 0.15f)); k.Box(stone, Vector3.Min(a, b) - new Vector3(0.19f, -0.9f, 0.19f), Vector3.Max(a, b) + new Vector3(0.19f, 1.0f, 0.19f)); }
            GWall(new Vector3(gx0, 0, gz0), new Vector3(gx0, 0, gz1)); GWall(new Vector3(gx0, 0, gz1), new Vector3(gx1, 0, gz1)); GWall(new Vector3(gx1, 0, gz1), new Vector3(gx1, 0, gz0));
            k.Box(flag, new Vector3(Origin.x + 9.5f, 0f, Origin.z + 21f), new Vector3(Origin.x + 17f, 0.05f, Origin.z + 24f)); // patio outside the dairy door
            var rng = new System.Random(19);
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = new Vector3(gx0 + 4f + (i % 4) * 8.5f + (float)rng.NextDouble(), 0f, gz0 + (i < 4 ? 8.5f : 11f) + (float)rng.NextDouble());
                if (c.x > Origin.x + 8.5f && c.x < Origin.x + 18f && c.z < Origin.z + 25f) c.z += 4f; // keep clear of the dairy and its patio
                k.Push(Matrix4x4.TRS(c, Quaternion.Euler(0f, (float)rng.NextDouble() * 30f - 15f, 0f), Vector3.one));
                k.Box(wood, new Vector3(-0.9f, 0.72f, -0.38f), new Vector3(0.9f, 0.77f, 0.38f));
                foreach (float s in new[] { -1f, 1f })
                {
                    k.Box(wood, new Vector3(-0.9f, 0.43f, s * 0.62f - 0.13f), new Vector3(0.9f, 0.47f, s * 0.62f + 0.13f));
                    foreach (float lx in new[] { -0.75f, 0.75f }) { k.Box(wood, new Vector3(lx - 0.04f, 0f, s * 0.62f - 0.04f), new Vector3(lx + 0.04f, 0.43f, s * 0.62f + 0.04f)); k.Box(wood, new Vector3(lx - 0.04f, 0f, s * 0.25f - 0.04f), new Vector3(lx + 0.04f, 0.72f, s * 0.25f + 0.04f)); }
                }
                k.Pop();
            }
            var g = k.Build("Grounds", root, true);
            if (g != null) g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }
}
