using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// The town's emergency services, inside and out:
    ///  - Wend Police Station on the Long Road: a 70s brown-brick two-storey block with a blue lamp, front counter and
    ///    waiting area, custody suite with four cells, interview room, response office, locker room; a gated yard at
    ///    the side and back for the patrol cars.
    ///  - Wend Fire Station: a tall two-bay appliance room with red doors onto a KEEP CLEAR apron, kit lockers, hose
    ///    reels and a pole down from the floor above; watch room and mess; a drill tower out the back.
    ///  - Wend General Hospital on Northway: a five-storey block with a glazed entrance, A&E with a canopied
    ///    ambulance bay, reception, waiting room, cubicles and resus; outpatients and a ward on the ground floor; a car
    ///    park and a drop-off road. (No real health service branding: our own blue and our own names.)
    /// Built in the editor into the baked world.
    /// </summary>
    public static class Services
    {
        public const string RootName = "Services";
        static CouncilEstate.Mats E;
        /// <summary>Where the police park their cars and the fire engine stands (filled at runtime).</summary>
        public static readonly List<(Vector3 p, float yaw)> PoliceBays = new List<(Vector3, float)>();
        public static readonly Vector3 PoliceDesk = new Vector3(-210f, 0, -11.6f), FireBay = new Vector3(239f, 0, -5f), AandE = new Vector3(50f, 0, 144f);

        static Material Signs => TownMats.signsServices;

        public static Transform Build(Transform parent)
        {
            if (!TownMats.Load()) return null; E = CouncilEstate.M; PoliceBays.Clear();
            var root = new GameObject(RootName).transform; root.SetParent(parent, false);
            Police(root); Fire(root); Hospital(root);
            Debug.Log("[ASBO] services: police station, fire station and hospital built");
            return root;
        }

        static Rect R(float x0, float z0, float x1, float z1) => new Rect(x0, z0, x1 - x0, z1 - z0);
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        /// <summary>Concrete block paving over a rect (forecourts, aprons), a touch above the ground.</summary>
        static void Paving(MeshKit k, Rect r, Material m, float y = 0.07f)
        {
            k.Quad(m, V(r.xMin, y, r.yMin), V(r.xMax, y, r.yMin), V(r.xMax, y, r.yMax), V(r.xMin, y, r.yMax), Vector3.up);
            foreach (var (a, b, n) in new[] { (V(r.xMin, 0, r.yMin), V(r.xMax, 0, r.yMin), Vector3.back), (V(r.xMax, 0, r.yMin), V(r.xMax, 0, r.yMax), Vector3.right), (V(r.xMax, 0, r.yMax), V(r.xMin, 0, r.yMax), Vector3.forward), (V(r.xMin, 0, r.yMax), V(r.xMin, 0, r.yMin), Vector3.left) })
                k.Quad(E.kerb, a, b, b + Vector3.up * y, a + Vector3.up * y, n);
        }
        /// <summary>Steel palisade fencing (pointed pales) from a to b.</summary>
        static void Palisade(MeshKit k, Vector3 a, Vector3 b, float h = 2.4f, Material m = null)
        {
            m ??= E.railGreen; a = a.WithY(0); b = b.WithY(0); Vector3 d = b - a; float len = d.magnitude; d /= len; Vector3 s = new Vector3(-d.z, 0, d.x) * 0.02f;
            for (float t = 0; t <= len; t += 0.22f) { Vector3 p = a + d * t; k.Box(m, p - Vector3.Scale(new Vector3(0.035f, 0, 0.035f), Vector3.one) - s, p + new Vector3(0.035f, h, 0.035f) + s); }
            foreach (float y in new[] { 0.3f, h - 0.4f }) k.Box(m, Vector3.Min(a, b) + Vector3.up * y - new Vector3(0.03f, 0, 0.03f), Vector3.Max(a, b) + Vector3.up * (y + 0.06f) + new Vector3(0.03f, 0, 0.03f));
        }

        // ------------------------------------------------------------------ police
        static void Police(Transform root)
        {
            var b = new Bld(root, "Wend Police Station");
            var fp = R(-230f, -16f, -190f, -2f); const float H = 3.4f, top = H * 2f;
            var brick = TownMats.brownBrick; var paint = TownMats.lobbyWall; var floorM = TownMats.lino; var ceil = E.white;
            // outside: ground floor and first floor, a concrete band between, windows in rows, the glazed entrance
            var gS = new[] { Bld.Win(1.5f, 3.1f, 1.6f, 2.4f, true), Bld.Win(6.5f, 8.1f, 1.6f, 2.4f, true), Bld.Win(11.5f, 13.3f), Bld.Door(17.8f, 22.2f, 2.6f), Bld.Win(26f, 28.5f), Bld.Win(31f, 33.5f), Bld.Win(36f, 38.5f) };
            var gN = new[] { Bld.Door(3f, 5.2f, 2.4f), Bld.Win(9f, 10.6f, 1.6f, 2.4f, true), Bld.Win(16f, 18f), Bld.Win(24f, 26f), Bld.Win(31f, 33f) };
            var gE = new[] { Bld.Win(4f, 6f), Bld.Door(10.5f, 11.7f) };
            var gW = new[] { Bld.Win(6f, 7.4f, 1.6f, 2.4f, true) };
            b.Shell(fp, 0f, H, brick, paint, gS, gE, gN, gW);
            var up = new List<Bld.Hole>(); for (float s = 1.5f; s < 38f; s += 4f) up.Add(Bld.Blind(s, s + 2.2f, 0.9f, 2.4f, Bld.OfficeCell(s, 1f)));
            var upSide = new[] { Bld.Blind(3f, 5.2f, 0.9f, 2.4f, 3), Bld.Blind(8.5f, 10.7f, 0.9f, 2.4f, 0) };
            // the first floor walls start at H (Wall builds from y0 to y1 with holes measured from 0: shift them)
            Bld.Hole[] Up(IEnumerable<Bld.Hole> hs) { var l = new List<Bld.Hole>(); foreach (var h in hs) { var q = h; q.y0 += H; q.y1 += H; l.Add(q); } return l.ToArray(); }
            b.Shell(fp, H, top, brick, paint, Up(up), Up(upSide), Up(up), Up(upSide));
            b.Box(TownMats.panel, V(fp.xMin - 0.08f, H - 0.15f, fp.yMin - 0.08f), V(fp.xMax + 0.08f, H + 0.15f, fp.yMin + 0.02f), false, true); // band
            b.Roof(fp, top);
            b.Slab(fp, 0.06f, floorM, null); b.Slab(fp, H, floorM, ceil); b.Slab(fp, top, null, ceil);
            // the entrance: a canopy, the blue lamp and the sign
            b.Box(TownMats.panel, V(-213f, 2.9f, -18.4f), V(-207f, 3.1f, -16f), false, true);
            b.Sign(Signs, Bld.SignCell(0), V(-210f, 3.35f, -16.02f), Vector3.back, 3.6f, 0.6f);
            var lamp = new MeshKit();
            lamp.Cyl(E.black, V(-214.2f, 0f, -18.6f), 0.06f, 3.2f, 8);
            lamp.Box(TownMats.blueLamp != null ? TownMats.blueLamp : E.glow, V(-214.5f, 3.2f, -18.9f), V(-213.9f, 3.75f, -18.3f));
            lamp.Box(E.white, V(-214.52f, 3.3f, -18.92f), V(-213.88f, 3.42f, -18.28f));
            lamp.Box(E.black, V(-214.55f, 3.75f, -18.95f), V(-213.85f, 3.82f, -18.25f));
            // forecourt paving, bollards, the free-standing name board
            Paving(lamp, R(-230f, -22.3f, -190f, -16f), TownMats.paviors);
            for (float x = -228f; x <= -192f; x += 2f) if (Mathf.Abs(x + 210f) > 3f) lamp.Cyl(E.black, V(x, 0.07f, -21.6f), 0.09f, 0.9f, 8);
            lamp.Box(E.black, V(-200.9f, 0f, -21f), V(-200.8f, 1.9f, -20.9f)); lamp.Box(E.black, V(-197.2f, 0f, -21f), V(-197.1f, 1.9f, -20.9f));
            b.Sign(Signs, Bld.SignCell(1), V(-199f, 1.55f, -21.02f), Vector3.back, 3.8f, 0.63f);

            // inside, ground floor: waiting area and front counter, custody, interview, office, lockers, corridor
            float y0 = 0.06f, h = H - 0.25f - 0.06f;
            b.Part(V(-216f, 0, -16f), V(-216f, 0, -2f), y0, h, paint, (11.5f, 1.2f));                     // custody | the rest
            b.Part(V(-216f, 0, -9f), V(-190f, 0, -9f), y0, h, paint, (3.5f, 1.1f), (17f, 1.1f), (23f, 1.1f));  // front rooms | corridor
            b.Part(V(-216f, 0, -7f), V(-190f, 0, -7f), y0, h, paint, (10f, 1.2f), (20f, 1.1f));               // corridor | back office
            b.Part(V(-204f, 0, -16f), V(-204f, 0, -9f), y0, h, paint);                                       // waiting | interview
            b.Part(V(-196f, 0, -16f), V(-196f, 0, -9f), y0, h, paint);                                       // interview | lockers
            b.Counter(V(-210f, 0.06f, -12.2f), 0f, 11.6f, true);                                             // the front counter across the waiting room
            b.Seats(V(-215.3f, 0.06f, -15.5f), 0f, 5, TownMats.nhsBlue); b.Seats(V(-207.8f, 0.06f, -15.5f), 0f, 5, TownMats.nhsBlue);
            b.Poster(V(-215.85f, 1.6f, -14f), Vector3.right, 0.6f, 0.85f, TownMats.notice); b.Poster(V(-204.15f, 1.6f, -14.5f), Vector3.left, 0.6f, 0.85f, TownMats.notice);
            b.Desk(V(-212f, 0.06f, -10f), 180f); b.Desk(V(-208f, 0.06f, -10f), 180f);
            // custody: the custody sergeant's high desk, four cells along the back
            b.Counter(V(-223f, 0.06f, -11.5f), 0f, 4f, false);
            b.Box(E.black, V(-224.5f, 1.1f, -11.3f), V(-223.9f, 1.42f, -11.25f)); // a screen on the desk
            for (int i = 0; i < 4; i++)
            {
                float x0 = -230f + i * 3.5f; var cr = R(x0, -6f, x0 + 3.5f, -2f);
                b.Part(V(x0 + 3.5f, 0, -6f), V(x0 + 3.5f, 0, -2f), y0, h, TownMats.panel);
                b.Part(V(x0, 0, -6f), V(x0 + 3.5f, 0, -6f), y0, h, TownMats.panel, (1.75f, 1f));
                b.Cell(cr, V(x0 + 1.75f, 0.06f, -6f), Vector3.forward);
                b.Strip(V(x0 + 1.75f, H - 0.26f, -4f), i % 2 == 0);
            }
            b.Sign(Signs, Bld.SignCell(22), V(-216.08f, 2.4f, -12.6f), Vector3.right, 1.6f, 0.27f, false);
            // interview room: a table, three chairs, the recorder
            b.Table(V(-200f, 0.06f, -12.5f), 1.4f, 0.8f); b.Chair(V(-200.4f, 0.06f, -13.3f), 0f, E.black); b.Chair(V(-199.6f, 0.06f, -13.3f), 0f, E.black); b.Chair(V(-200f, 0.06f, -11.7f), 180f, E.black);
            b.Box(E.black, V(-200.5f, 0.8f, -12.4f), V(-200.1f, 0.9f, -12.2f));
            // lockers and a bench
            b.Lockers(V(-195.8f, 0.06f, -15.9f), 0f, 13); b.Box(E.fence, V(-195.5f, 0.45f, -12.4f), V(-190.5f, 0.5f, -12f), true);
            // response office: desks with computers, a map and a whiteboard
            for (int i = 0; i < 4; i++) b.Desk(V(-213f + i * 4.5f, 0.06f, -4.2f), 0f);
            b.Poster(V(-206f, 1.7f, -2.32f), Vector3.back, 2.4f, 1.2f, E.white); b.Poster(V(-200f, 1.7f, -2.32f), Vector3.back, 1.8f, 1.2f, TownMats.notice);
            // lights
            for (float x = -228f; x < -192f; x += 4f) b.Strip(V(x, H - 0.26f, -8f), ((int)x) % 8 == 0);
            b.Strip(V(-210f, H - 0.26f, -14f), true); b.Strip(V(-200f, H - 0.26f, -12.5f), true); b.Strip(V(-223f, H - 0.26f, -12f), true); b.Strip(V(-205f, H - 0.26f, -4.5f), true);

            // the yard: tarmac at the side and back, palisade fencing, a sliding gate, bays for the patrol cars
            var yard = new MeshKit();
            yard.Quad(E.asphalt, V(-188f, 0.04f, -22.3f), V(-174f, 0.04f, -22.3f), V(-174f, 0.04f, 18f), V(-188f, 0.04f, 18f), Vector3.up);
            yard.Quad(E.asphalt, V(-232f, 0.04f, -2f), V(-188f, 0.04f, -2f), V(-188f, 0.04f, 18f), V(-232f, 0.04f, 18f), Vector3.up);
            Palisade(yard, V(-232f, 0, -2f), V(-232f, 0, 18f)); Palisade(yard, V(-232f, 0, 18f), V(-174f, 0, 18f)); Palisade(yard, V(-174f, 0, 18f), V(-174f, 0, -19f));
            Palisade(yard, V(-188f, 0, -16f), V(-188f, 0, -19f)); Palisade(yard, V(-186.5f, 0, -19f), V(-188f, 0, -19f));
            yard.Box(E.railGreen, V(-186.5f, 0f, -19.05f), V(-176f, 1.6f, -18.95f)); // the sliding gate (half open)
            for (int i = 0; i < 6; i++)
            {
                float x = -228f + i * 3f; var p = V(x + 1.5f, 0, 12f);
                yard.Quad(E.line, V(x, 0.045f, 9.6f), V(x + 0.1f, 0.045f, 9.6f), V(x + 0.1f, 0.045f, 14.4f), V(x, 0.045f, 14.4f), Vector3.up);
                PoliceBays.Add((p, 0f));
            }
            b.Sign(Signs, Bld.SignCell(22), V(-228f, 1.6f, -1.98f), Vector3.forward, 1.8f, 0.3f);
            lamp.Build("Frontage", b.Root, true, ShadowCastingMode.On); yard.Build("Yard", b.Root, true, ShadowCastingMode.Off);
            b.Done();
        }

        // ------------------------------------------------------------------ fire
        static void Fire(Transform root)
        {
            var b = new Bld(root, "Wend Fire Station");
            var bay = R(228f, -14f, 250f, 4f); var block = R(250f, -14f, 266f, 2f); const float BH = 6.2f, FH = 3.2f;
            var brick = TownMats.redBrick != null ? TownMats.redBrick : TownMats.brownBrick; var paint = TownMats.lobbyWall;
            // the appliance room: two tall doorways (the left door up, the right one down), windows along the side
            b.Shell(bay, 0f, BH, brick, paint, new[] { Bld.Door(3.6f, 8.4f, 4.6f), Bld.Door(13.6f, 18.4f, 4.6f) }, null, new[] { Bld.Door(8f, 12f, 4.4f) }, new[] { Bld.Win(3f, 6f, 2.6f, 4.4f), Bld.Win(9f, 12f, 2.6f, 4.4f), Bld.Win(15f, 17f, 2.6f, 4.4f) });
            b.Roof(bay, BH);
            b.Slab(bay, 0.05f, TownMats.panelDark, null); b.Slab(bay, BH, null, E.white);
            var door = TownMats.redPaint != null ? TownMats.redPaint : E.black;
            b.Box(door, V(241.6f, 0f, -14.05f), V(246.4f, 4.6f, -13.9f), true, true); // the right door, shut
            b.Box(TownMats.glassDark, V(242f, 3.4f, -14.08f), V(246f, 4.2f, -14.04f), false, true); // a window strip in the door
            b.Box(door, V(231.6f, 4.15f, -14.05f), V(236.4f, 4.6f, -13.6f), false, true); // the left one rolled up into its box
            b.Sign(Signs, Bld.SignCell(2), V(239f, 5.3f, -14.02f), Vector3.back, 7f, 1.16f);
            // the two-storey watch room / mess block
            b.Shell(block, 0f, FH, brick, paint, new[] { Bld.Win(1f, 3.5f), Bld.Door(5f, 6.2f), Bld.Win(8f, 11f), Bld.Win(12.5f, 15f) }, new[] { Bld.Win(3f, 5f), Bld.Win(10f, 12f) }, new[] { Bld.Win(3f, 5f), Bld.Door(9f, 10.2f) }, null);
            var up = new[] { Bld.Blind(1f, 3.5f, FH + 0.9f, FH + 2.3f, 3), Bld.Blind(5.5f, 8f, FH + 0.9f, FH + 2.3f, 5), Bld.Blind(9.5f, 12f, FH + 0.9f, FH + 2.3f, 0), Bld.Blind(13f, 15f, FH + 0.9f, FH + 2.3f, 3) };
            b.Shell(block, FH, FH * 2f, brick, paint, up, new[] { Bld.Blind(3f, 5f, FH + 0.9f, FH + 2.3f, 4) }, up, null);
            b.Roof(block, FH * 2f); b.Slab(block, 0.05f, TownMats.lino, null); b.Slab(block, FH, TownMats.lino, E.white); b.Slab(block, FH * 2f, null, E.white);
            // inside the bay: yellow lines, kit lockers with helmets, hose reels, a workbench, the pole down from upstairs
            for (int i = 0; i < 2; i++) { float cx = 234f + i * 10f; b.Box(TownMats.yellow, V(cx - 2.6f, 0.06f, -13.5f), V(cx - 2.5f, 0.065f, 3.5f)); b.Box(TownMats.yellow, V(cx + 2.5f, 0.06f, -13.5f), V(cx + 2.6f, 0.065f, 3.5f)); }
            for (int i = 0; i < 12; i++)
            {
                float z = -12f + i * 1.2f; b.Box(TownMats.steel, V(228.3f, 0.05f, z), V(228.9f, 2.1f, z + 1.1f));
                b.Box(TownMats.hiVis != null ? TownMats.hiVis : E.white, V(228.9f, 0.9f, z + 0.25f), V(229.2f, 1.7f, z + 0.85f)); // a tunic on its hook
                b.Box(TownMats.yellow, V(228.95f, 1.85f, z + 0.35f), V(229.25f, 2.05f, z + 0.75f)); // a helmet on the shelf
            }
            b.Solid(V(228.3f, 0f, -12f), V(229.25f, 2.1f, 2.4f));
            for (int i = 0; i < 3; i++) { var c = V(249.5f, 1.2f, -10f + i * 2.2f); b.Box(door, c - V(0.15f, 0.6f, 0.6f), c + V(0.15f, 0.6f, 0.6f)); }
            b.Box(TownMats.steel, V(244f, 0.05f, 2.8f), V(249.6f, 0.95f, 3.7f), true);
            b.Box(TownMats.steel, V(247.9f, 0.05f, 1.1f), V(248.02f, BH, 1.22f), true); // the pole
            b.Box(E.black, V(247.4f, BH - 0.27f, 0.6f), V(248.5f, BH - 0.25f, 1.7f));
            b.Part(V(250f, 0, -14f), V(250f, 0, 2f), 0.05f, FH - 0.3f, paint, (12f, 1.2f));
            // watch room: the desk, the radio console, the turnout board; mess: kitchen, table, sofa and telly
            b.Part(V(258f, 0, -14f), V(258f, 0, -4f), 0.05f, FH - 0.3f, paint, (8.5f, 1f));
            b.Part(V(250f, 0, -4f), V(266f, 0, -4f), 0.05f, FH - 0.3f, paint, (4f, 1f), (12f, 1f));
            b.Desk(V(253f, 0.05f, -11.8f), 0f); b.Box(E.black, V(255f, 0.8f, -13.5f), V(257f, 1.4f, -13.1f)); b.Poster(V(254f, 1.8f, -13.68f), Vector3.forward, 2.2f, 1f, E.white);
            b.Kitchen(V(259f, 0.05f, -13.7f), Vector3.right, 6.5f);
            b.Table(V(262f, 0.05f, -9.8f), 2.4f, 1f); foreach (float x in new[] { 261f, 262f, 263f }) { b.Chair(V(x, 0.05f, -10.6f), 0f, E.black); b.Chair(V(x, 0.05f, -9f), 180f, E.black); }
            b.Sofa(V(262.5f, 0.05f, -5.2f), 180f); b.Telly(V(265.6f, 0.6f, -6.5f), Vector3.left);
            for (float z = -12f; z < 2f; z += 4f) { b.Strip(V(234f, BH - 0.26f, z), ((int)z) % 8 == 0); b.Strip(V(244f, BH - 0.26f, z), false); }
            b.Strip(V(254f, FH - 0.26f, -9f), true); b.Strip(V(262f, FH - 0.26f, -9f), true); b.Strip(V(258f, FH - 0.26f, -1f), false);
            // the drill tower out the back: five floors, open on one side, a ladder up it
            var t = new MeshKit(); var tw = R(258f, 7f, 265f, 13f);
            for (int f = 0; f <= 5; f++) { float y = f * 3f; t.Box(TownMats.panel, V(tw.xMin, y, tw.yMin), V(tw.xMax, y + 0.25f, tw.yMax)); }
            t.Box(brick, V(tw.xMin, 0f, tw.yMax - 0.3f), V(tw.xMax, 15.6f, tw.yMax)); t.Box(brick, V(tw.xMin, 0f, tw.yMin), V(tw.xMin + 0.3f, 15.6f, tw.yMax)); t.Box(brick, V(tw.xMax - 0.3f, 0f, tw.yMin), V(tw.xMax, 15.6f, tw.yMax));
            for (int f = 0; f < 5; f++) t.Box(brick, V(tw.xMin, f * 3f + 0.25f, tw.yMin), V(tw.xMax, f * 3f + 1.1f, tw.yMin + 0.3f)); // waist-high fronts with openings above
            for (float y = 0.3f; y < 15f; y += 0.3f) t.Box(TownMats.steel, V(tw.xMin + 0.6f, y, tw.yMin - 0.35f), V(tw.xMin + 1.2f, y + 0.03f, tw.yMin - 0.3f));
            // apron and yard, KEEP CLEAR
            Paving(t, R(228f, -22.3f, 250f, -14f), TownMats.panel, 0.05f);
            t.Quad(E.asphalt, V(228f, 0.04f, 4f), V(268f, 0.04f, 4f), V(268f, 0.04f, 18f), V(228f, 0.04f, 18f), Vector3.up);
            b.Sign(Signs, Bld.SignCell(3), V(253.5f, 1.2f, -22.0f), Vector3.back, 2.4f, 0.4f);
            t.Box(E.black, V(252.25f, 0f, -21.98f), V(252.33f, 1.4f, -21.92f)); t.Box(E.black, V(254.67f, 0f, -21.98f), V(254.75f, 1.4f, -21.92f));
            Palisade(t, V(268f, 0, -14f), V(268f, 0, 18f)); Palisade(t, V(268f, 0, 18f), V(226f, 0, 18f)); Palisade(t, V(226f, 0, 18f), V(226f, 0, 4f));
            t.Build("Yard", b.Root, true, ShadowCastingMode.On);
            b.Done();
        }

        // ------------------------------------------------------------------ hospital
        static void Hospital(Transform root)
        {
            var b = new Bld(root, "Wend General Hospital");
            var main = R(-40f, 150f, 40f, 174f); var ae = R(40f, 146f, 62f, 168f); const float FH = 3.6f; const int floors = 5;
            var clad = TownMats.render != null ? TownMats.render : TownMats.panel; var blue = TownMats.nhsBlue != null ? TownMats.nhsBlue : E.black; var paint = TownMats.lobbyWall;
            // the main block: a grid of windows on every floor, a glazed entrance in the middle of the front
            for (int f = 0; f < floors; f++)
            {
                float y0 = f * FH; var front = new List<Bld.Hole>(); var back = new List<Bld.Hole>(); var side = new List<Bld.Hole>();
                for (float s = 1.5f; s < 78f; s += 3.2f)
                {
                    if (f == 0 && s > 30f && s < 48f) continue; // the entrance
                    // the ground floor you can look into; the wards and offices upstairs have blinds and curtains
                    front.Add(f == 0 ? Bld.Win(s, s + 2.2f, y0 + 0.9f, y0 + 2.6f) : Bld.Blind(s, s + 2.2f, y0 + 0.9f, y0 + 2.6f, new[] { 3, 0, 5, 3, 11, 2 }[(int)(s * 1.7f + f) % 6]));
                    back.Add(f == 0 ? Bld.Win(s, s + 2.2f, y0 + 0.9f, y0 + 2.6f) : Bld.Blind(s, s + 2.2f, y0 + 0.9f, y0 + 2.6f, new[] { 3, 0, 11, 3 }[(int)(s + f) % 4]));
                }
                for (float s = 2f; s < 22f; s += 3.2f) side.Add(Bld.Blind(s, s + 2.2f, y0 + 0.9f, y0 + 2.6f, 3));
                if (f == 0) front.Add(Bld.Door(32f, 48f, 3.2f));
                b.Shell(main, y0, y0 + FH, clad, paint, front.ToArray(), f == 0 ? new[] { Bld.Door(10f, 12f, 2.4f) } : side.ToArray(), back.ToArray(), side.ToArray());
                b.Box(blue, V(main.xMin - 0.05f, y0 + FH - 0.35f, main.yMin - 0.06f), V(main.xMax + 0.05f, y0 + FH, main.yMin + 0.01f), false, true); // a blue band at every floor
                b.Slab(main, y0 + (f == 0 ? 0.06f : 0f), f == 0 ? TownMats.lino : null, f == 0 ? null : E.white);
            }
            b.Roof(main, FH * floors, 0.9f);
            b.Box(TownMats.panelDark, V(-12f, FH * floors, 156f), V(6f, FH * floors + 3f, 168f), false, true); // plant room on the roof
            b.Sign(Signs, Bld.SignCell(4), V(0f, FH * floors - 1.2f, 149.98f), Vector3.back, 14f, 2.3f);
            // the glass entrance porch
            var porch = R(-8f, 144f, 8f, 150f);
            b.Box(TownMats.panel, V(porch.xMin - 1f, 6.8f, porch.yMin - 2f), V(porch.xMax + 1f, 7.2f, porch.yMax), false, true);
            var gk = new MeshKit();
            foreach (var (a, c, n) in new[] { (V(porch.xMin, 0, porch.yMin), V(porch.xMax, 0, porch.yMin), Vector3.back), (V(porch.xMin, 0, porch.yMin), V(porch.xMin, 0, porch.yMax), Vector3.left), (V(porch.xMax, 0, porch.yMin), V(porch.xMax, 0, porch.yMax), Vector3.right) })
            {
                gk.Quad(TownMats.glass, a + Vector3.up * 0.06f, c + Vector3.up * 0.06f, c + Vector3.up * 6.8f, a + Vector3.up * 6.8f, n);
                Vector3 d = (c - a); float len = d.magnitude; d /= len;
                for (float s2 = 0f; s2 <= len + 0.01f; s2 += 2f) { var p = a + d * s2; b.Box(TownMats.steel, p - V(0.05f, 0, 0.05f), p + V(0.05f, 6.8f, 0.05f), false, true); }
            }
            b.Solid(V(porch.xMin, 0, porch.yMin), V(-1.5f, 6.8f, porch.yMin + 0.1f)); b.Solid(V(1.5f, 0, porch.yMin), V(porch.xMax, 6.8f, porch.yMin + 0.1f));
            b.Solid(V(porch.xMin, 0, porch.yMin), V(porch.xMin + 0.1f, 6.8f, porch.yMax)); b.Solid(V(porch.xMax - 0.1f, 0, porch.yMin), V(porch.xMax, 6.8f, porch.yMax));
            b.Slab(porch, 0.06f, TownMats.lino, null); b.Box(TownMats.panel, V(porch.xMin, 6.6f, porch.yMin), V(porch.xMax, 6.8f, porch.yMax));
            b.Sign(Signs, Bld.SignCell(6), V(0f, 5.6f, 143.98f), Vector3.back, 7f, 1.16f);
            // A&E: single storey, sliding doors under a canopy, the ambulance bay
            b.Shell(ae, 0f, 4.6f, clad, paint, new[] { Bld.Door(8f, 12f, 2.6f), Bld.Win(1.5f, 6f, 1f, 2.6f), Bld.Win(14f, 20f, 1f, 2.6f) }, new[] { Bld.Win(3f, 6f), Bld.Win(9f, 12f), Bld.Win(15f, 18f) }, null, null);
            b.Roof(ae, 4.6f); b.Slab(ae, 0.06f, TownMats.lino, null); b.Slab(ae, 4.6f, null, E.white);
            b.Box(blue, V(ae.xMin, 3.9f, ae.yMin - 0.06f), V(ae.xMax, 4.6f, ae.yMin + 0.01f), false, true);
            b.Box(TownMats.panel, V(40f, 4.1f, 136f), V(64f, 4.45f, 146f), false, true); // the canopy over the bay
            foreach (float x in new[] { 41f, 52f, 63f }) b.Box(TownMats.steel, V(x - 0.15f, 0f, 136.4f), V(x + 0.15f, 4.1f, 136.7f), true, true);
            b.Sign(Signs, Bld.SignCell(5), V(51f, 3.6f, 135.95f), Vector3.back, 8f, 1.33f);
            b.Sign(Signs, Bld.SignCell(7), V(44f, 1.3f, 136.4f), Vector3.back, 2.4f, 0.4f);
            // inside the main entrance: reception, seating, a coffee kiosk, plants; a corridor runs the length
            float yy = 0.06f, h = FH - 0.3f;
            b.Counter(V(0f, yy, 156.5f), 0f, 5f, false); b.Box(E.black, V(-1.5f, 1.1f, 156.7f), V(-1f, 1.4f, 156.75f)); b.Box(E.black, V(1f, 1.1f, 156.7f), V(1.5f, 1.4f, 156.75f));
            b.Seats(V(-7f, yy, 152f), 90f, 6); b.Seats(V(7f, yy, 155f), -90f, 6);
            b.Plant(V(-7.5f, yy, 146f)); b.Plant(V(7.5f, yy, 146f));
            b.Counter(V(-5f, yy, 158.5f), 90f, 2.4f, false); b.Box(E.fence, V(-5.6f, 2.2f, 157.3f), V(-4.4f, 2.6f, 159.7f));
            b.Part(V(-40f, 0, 160f), V(40f, 0, 160f), yy, h, paint, (12f, 1.8f), (36f, 4f), (44f, 4f), (66f, 1.8f));
            b.Part(V(-40f, 0, 163f), V(40f, 0, 163f), yy, h, paint, (8f, 1.6f), (30f, 1.6f), (58f, 2f), (74f, 1.6f));
            b.Part(V(-12f, 0, 150f), V(-12f, 0, 160f), yy, h, paint, (5f, 1.6f)); b.Part(V(12f, 0, 150f), V(12f, 0, 160f), yy, h, paint, (5f, 1.6f));
            // outpatients waiting (west front), pharmacy hatch
            for (int r = 0; r < 3; r++) b.Seats(V(-36f, yy, 153f + r * 2.2f), 0f, 10);
            b.Counter(V(-20f, yy, 158.8f), 0f, 3f, true); b.Shelf(V(-22f, yy, 159.6f), V(-18f, yy, 159.6f), 2f);
            // a ward along the back east: six beds with curtains, the nurses' station
            for (int i = 0; i < 3; i++) { b.Bed(V(16f + i * 4f, yy, 171.6f), 180f, true); b.Bed(V(16f + i * 4f, yy, 165.6f), 0f, true); }
            b.Counter(V(32f, yy, 168f), 90f, 3f, false); b.Desk(V(35f, yy, 168f), -90f);
            // west back: offices
            for (int i = 0; i < 3; i++) b.Desk(V(-34f + i * 6f, yy, 170f), 0f);
            // A&E inside: reception behind glass, waiting room, vending machine, telly; cubicles and resus behind
            b.Part(V(40f, 0, 157f), V(62f, 0, 157f), yy, 4.2f, paint, (4f, 1.6f), (16f, 2f));
            b.Part(V(48f, 0, 146f), V(48f, 0, 157f), yy, 4.2f, paint, (8f, 1.2f));
            b.Counter(V(44f, yy, 151.5f), 90f, 4f, true); b.Desk(V(42f, yy, 151.5f), 90f);
            for (int r = 0; r < 3; r++) b.Seats(V(50f, yy, 150f + r * 2f), 0f, 9, TownMats.nhsBlue);
            b.Vending(V(61.2f, yy, 155.8f), -90f); b.Telly(V(55f, 2.1f, 156.85f), Vector3.back);
            b.Poster(V(48.08f, 1.6f, 155f), Vector3.right, 0.6f, 0.85f, TownMats.notice);
            for (int i = 0; i < 4; i++) b.Bed(V(43f + i * 3.6f, yy, 165.5f), 180f, true);
            b.Bed(V(59f, yy, 162f), 90f, false); b.Box(TownMats.steel, V(60.5f, yy, 164.5f), V(61.6f, 1.6f, 165.6f)); b.Box(E.black, V(60.6f, 1.3f, 164.6f), V(61.5f, 1.6f, 164.65f)); // resus: a monitor stack
            for (float x = -36f; x < 40f; x += 6f) b.Strip(V(x, FH - 0.26f, 161.5f), ((int)x) % 12 == 0);
            foreach (var p in new[] { V(0f, FH - 0.26f, 153f), V(-24f, FH - 0.26f, 155f), V(24f, FH - 0.26f, 168f), V(-24f, FH - 0.26f, 168f), V(55f, 4.3f, 152f), V(51f, 4.3f, 164f) }) b.Strip(p, true);

            // grounds: car park (west), the drop-off road along the front, the ambulance bay
            var k = new MeshKit(); var marks = new MeshKit();
            k.Quad(E.asphalt, V(-62f, 0.04f, 132.6f), V(-42f, 0.04f, 132.6f), V(-42f, 0.04f, 190f), V(-62f, 0.04f, 190f), Vector3.up);
            k.Quad(E.asphalt, V(48.4f, 0.04f, 132.6f), V(55.6f, 0.04f, 132.6f), V(55.6f, 0.04f, 136.5f), V(48.4f, 0.04f, 136.5f), Vector3.up); // in from Northway
            k.Quad(E.asphalt, V(-42f, 0.04f, 136.5f), V(64f, 0.04f, 136.5f), V(64f, 0.04f, 143f), V(-42f, 0.04f, 143f), Vector3.up);
            for (int i = 0; i < 20; i++)
            {
                float z = 137f + i * 2.5f;
                marks.Quad(E.line, V(-62f, 0.046f, z), V(-57.2f, 0.046f, z), V(-57.2f, 0.046f, z + 0.1f), V(-62f, 0.046f, z + 0.1f), Vector3.up);
                marks.Quad(E.line, V(-46.8f, 0.046f, z), V(-42f, 0.046f, z), V(-42f, 0.046f, z + 0.1f), V(-46.8f, 0.046f, z + 0.1f), Vector3.up);
                Estates.Bays.Add((V(-59.6f, 0, z + 1.25f), 90f, -1)); Estates.Bays.Add((V(-44.4f, 0, z + 1.25f), -90f, -1));
            }
            foreach (float x in new[] { 44f, 50f, 56f }) marks.Quad(TownMats.yellow, V(x, 0.046f, 137f), V(x + 0.12f, 0.046f, 137f), V(x + 0.12f, 0.046f, 143f), V(x, 0.046f, 143f), Vector3.up);
            Paving(k, R(-40f, 143f, 40f, 150f), TownMats.paviors, 0.08f); Paving(k, R(40f, 143f, 62f, 146f), TownMats.paviors, 0.08f);
            b.Sign(Signs, Bld.SignCell(21), V(-52f, 1.6f, 133.5f), Vector3.back, 1.8f, 0.3f);
            k.Box(E.black, V(-52.95f, 0f, 133.55f), V(-52.85f, 1.45f, 133.65f)); k.Box(E.black, V(-51.15f, 0f, 133.55f), V(-51.05f, 1.45f, 133.65f));
            k.Box(TownMats.steel, V(-43.2f, 0f, 145f), V(-42.6f, 1.6f, 145.4f)); // the pay machine
            k.Build("Grounds", b.Root, true, ShadowCastingMode.Off); marks.Build("Markings", b.Root, false, ShadowCastingMode.Off);
            gk.Build("Porch", b.Root, false, ShadowCastingMode.Off);
            b.Done();
        }
    }
}
