using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// Mill Lane Industrial Estate, down Mill Lane off the Long Road: Wend Motors (MOT, servicing, a ramp and a lift),
    /// A1 Taxis' office with its bays, a row of lock-ups, Briggs Scrap Metal (corrugated fence, stacks of crushed cars,
    /// a grab crane, the portakabin and the weighbridge), Swap Shop Pawnbrokers' trade counter, Carpet King's warehouse
    /// and two units to let on Mill Lane East. Portal-frame sheds clad in profiled steel on a brick plinth, roller
    /// doors, fascia boards; the garage, the taxi office and the pawn shop have insides.
    /// </summary>
    public static class Industrial
    {
        public const string RootName = "Industrial";
        static CouncilEstate.Mats E;
        /// <summary>Where wrecks sit in the scrapyard and taxis wait outside A1 (filled at runtime).</summary>
        public static readonly List<(Vector3 p, float yaw)> ScrapSpots = new List<(Vector3, float)>(), TaxiBays = new List<(Vector3, float)>();

        public static Transform Build(Transform parent)
        {
            if (!TownMats.Load()) return null; E = CouncilEstate.M; ScrapSpots.Clear(); TaxiBays.Clear();
            var root = new GameObject(RootName).transform; root.SetParent(parent, false);
            Motors(root); Taxis(root); LockUps(root); Scrapyard(root); Pawn(root); East(root);
            Debug.Log("[ASBO] industrial estate built");
            return root;
        }
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static Rect R(float x0, float z0, float x1, float z1) => new Rect(x0, z0, x1 - x0, z1 - z0);
        static Material Clad => TownMats.metalSheet != null ? TownMats.metalSheet : E.concrete;

        /// <summary>
        /// A portal-frame shed on a footprint with its front to the west or east: brick plinth, steel cladding, a shallow
        /// pitched roof (ridge running front to back), the given openings in the front. Returns the Bld for its inside.
        /// </summary>
        static Bld Shed(Transform root, string name, Rect fp, bool frontEast, float eave, Bld.Hole[] front, Bld.Hole[] sides = null)
        {
            var b = new Bld(root, name); var brick = TownMats.brownBrick; var paint = TownMats.panel;
            var none = new Bld.Hole[0];
            Bld.Hole[] Plinth(Bld.Hole[] hs) { var l = new List<Bld.Hole>(); foreach (var h in hs) if (h.y0 < 1.2f) l.Add(new Bld.Hole { s0 = h.s0, s1 = h.s1, y0 = h.y0, y1 = 1.2f, door = h.door }); return l.ToArray(); }
            Bld.Hole[] Above(Bld.Hole[] hs) { var l = new List<Bld.Hole>(); foreach (var h in hs) if (h.y1 > 1.2f) l.Add(new Bld.Hole { s0 = h.s0, s1 = h.s1, y0 = Mathf.Max(1.2f, h.y0), y1 = h.y1, door = h.door, frosted = h.frosted }); return l.ToArray(); }
            // openings are measured along each wall the way Shell runs it: an east front from its south end, a west front from its north end
            Bld.Hole[] W = frontEast ? (sides ?? none) : front, Eh = frontEast ? front : (sides ?? none);
            b.Shell(fp, 0f, 1.2f, brick, paint, none, Plinth(Eh), none, Plinth(W));
            b.Shell(fp, 1.2f, eave, Clad, paint, none, Above(Eh), none, Above(W));
            // the roof: two slopes falling to the long sides (north and south), gables in cladding front and back
            float rise = fp.height * 0.5f * 0.12f, zc = fp.center.y; var k = b.outK;
            foreach (var (z0, z1) in new[] { (fp.yMin - 0.3f, zc), (fp.yMax + 0.3f, zc) })
            {
                Vector3 a = V(fp.xMin - 0.3f, eave, z0), c = V(fp.xMax + 0.3f, eave, z0), d = V(fp.xMax + 0.3f, eave + rise, z1), e = V(fp.xMin - 0.3f, eave + rise, z1);
                k.Quad(Clad, a, c, d, e, (Vector3.up + V(0, 0, z0 < zc ? -0.12f : 0.12f)).normalized);
                k.Quad(Clad, a + Vector3.down * 0.05f, c + Vector3.down * 0.05f, d + Vector3.down * 0.05f, e + Vector3.down * 0.05f, Vector3.down);
            }
            foreach (float x in new[] { fp.xMin, fp.xMax }) k.TriN(Clad, V(x, eave, fp.yMin), V(x, eave, fp.yMax), V(x, eave + rise, zc), x == fp.xMin ? Vector3.left : Vector3.right);
            b.Slab(fp, 0.05f, TownMats.panelDark, null);
            // gutters and downpipes
            foreach (float z in new[] { fp.yMin - 0.32f, fp.yMax + 0.18f }) k.Box(E.black, V(fp.xMin - 0.3f, eave - 0.15f, z), V(fp.xMax + 0.3f, eave, z + 0.14f));
            return b;
        }
        /// <summary>A roller door on an x-facing front (closed: the slats; open: just its box at the top).</summary>
        static void Roller(Bld b, float x, float z0, float z1, float h, bool open, bool facesEast)
        {
            float o = facesEast ? 0.08f : -0.08f;
            if (!open) { b.Box(TownMats.rollerDoor, V(Mathf.Min(x, x + o), 0.05f, z0), V(Mathf.Max(x, x + o), h, z1), true, true); }
            b.Box(TownMats.steel, V(Mathf.Min(x, x + o * 4f), h, z0 - 0.1f), V(Mathf.Max(x, x + o * 4f), h + 0.5f, z1 + 0.1f), false, true);
            foreach (float z in new[] { z0 - 0.08f, z1 }) b.Box(TownMats.steel, V(Mathf.Min(x, x + o * 1.5f), 0.05f, z), V(Mathf.Max(x, x + o * 1.5f), h, z + 0.08f), false, true);
        }
        static void Fascia(Bld b, int cell, float x, float zc, float y, float w, bool facesEast) => b.Sign(TownMats.signsServices, Bld.SignCell(cell), V(x + (facesEast ? 0.05f : -0.05f), y, zc), facesEast ? Vector3.right : Vector3.left, w, w * 0.166f);
        static void Apron(MeshKit k, Rect r, Material m = null) { m ??= E.asphalt; k.Quad(m, V(r.xMin, 0.04f, r.yMin), V(r.xMax, 0.04f, r.yMin), V(r.xMax, 0.04f, r.yMax), V(r.xMin, 0.04f, r.yMax), Vector3.up); }
        static void CorrFence(MeshKit k, Vector3 a, Vector3 b2, float h = 2.4f)
        {
            a = a.WithY(0); b2 = b2.WithY(0); Vector3 d = b2 - a; float len = d.magnitude; d /= len; Vector3 s = new Vector3(-d.z, 0, d.x) * 0.03f;
            k.Box(Clad, Vector3.Min(a - s, b2 + s), Vector3.Max(a - s, b2 + s) + Vector3.up * h);
            for (float t = 0; t <= len; t += 3f) { var p = a + d * t; k.Box(E.black, p - V(0.06f, 0, 0.06f), p + V(0.06f, h + 0.1f, 0.06f)); }
        }

        // ------------------------------------------------------------------ Wend Motors
        static void Motors(Transform root)
        {
            var fp = R(160f, -76f, 184f, -56f); const float eave = 6.2f;
            // front (east) measured from the south end: office door and window, two roller doors (the north one up)
            var b = Shed(root, "Wend Motors", fp, true, eave, new[] { Bld.Door(1.2f, 2.2f), Bld.Win(2.8f, 5f, 1.2f, 2.4f), Bld.Door(6.5f, 11f, 4.2f), Bld.Door(13f, 17.5f, 4.2f) }, new[] { Bld.Win(8f, 12f, 3.5f, 5f) });
            Roller(b, fp.xMax, -69.5f, -65f, 4.2f, false, true); Roller(b, fp.xMax, -63f, -58.5f, 4.2f, true, true);
            Fascia(b, 14, fp.xMax, -66f, 5.4f, 10f, true);
            // inside: a two-post lift with its arms, the MOT bay lines, tool chests, a tyre stack, the bench, the office
            var red = TownMats.redPaint != null ? TownMats.redPaint : E.black;
            foreach (float z in new[] { -63.8f, -57.7f }) b.Box(TownMats.steel, V(176.5f, 0.05f, z), V(177.1f, 4f, z + 0.5f), true);
            b.Box(TownMats.steel, V(176.6f, 3.8f, -63.8f), V(177f, 4.1f, -57.2f));
            foreach (float z in new[] { -63.3f, -58.2f }) b.Box(TownMats.steel, V(173.5f, 1.6f, z - 0.06f), V(176.5f, 1.7f, z + 0.06f));
            foreach (float z in new[] { -69.3f, -65.2f }) b.Box(TownMats.yellow, V(166f, 0.06f, z), V(183.5f, 0.065f, z + 0.12f));
            b.Box(E.black, V(172f, 0.05f, -68.4f), V(178f, 0.07f, -66.1f)); // the inspection pit cover
            for (int i = 0; i < 3; i++) b.Box(red, V(160.4f, 0.05f, -64f + i * 1.3f), V(161.1f, 1.1f, -62.9f + i * 1.3f), true);
            for (int i = 0; i < 6; i++) b.Box(TownMats.tyre != null ? TownMats.tyre : E.black, V(181.8f, 0.05f + i * 0.21f, -74.6f), V(182.6f, 0.25f + i * 0.21f, -73.8f));
            b.Box(TownMats.steel, V(160.3f, 0.05f, -60f), V(161f, 0.95f, -57f), true); b.Shelf(V(160.5f, 0.05f, -74f), V(160.5f, 0.05f, -70f), 2.2f);
            b.Part(V(180f, 0, -76f), V(180f, 0, -71f), 0.05f, 2.8f, TownMats.lobbyWall, (2f, 1f)); b.Part(V(180f, 0, -71f), V(184f, 0, -71f), 0.05f, 2.8f, TownMats.lobbyWall);
            b.Desk(V(182f, 0.05f, -74f), 0f); b.Poster(V(180.08f, 1.6f, -73f), Vector3.right, 0.5f, 0.7f, TownMats.notice);
            b.Strip(V(172f, eave - 0.3f, -66f), true); b.Strip(V(172f, eave - 0.3f, -60f), true); b.Strip(V(182f, 2.8f, -73.5f), false);
            var k = new MeshKit(); Apron(k, R(184f, -76f, 192.4f, -56f)); k.Build("Forecourt", b.Root, true, ShadowCastingMode.Off);
            b.Done();
        }

        // ------------------------------------------------------------------ A1 Taxis
        static void Taxis(Transform root)
        {
            var b = new Bld(root, "A1 Taxis"); var fp = R(176f, -92f, 186f, -84f);
            b.Shell(fp, 0f, 2.9f, TownMats.render, TownMats.lobbyWall, null, new[] { Bld.Win(1f, 3.6f, 0.9f, 2.2f), Bld.Door(5f, 6.1f), Bld.Win(6.5f, 7.5f, 1.2f, 2.2f, true) }, null, null);
            b.Roof(fp, 2.9f, 0.3f); b.Slab(fp, 0.05f, TownMats.lino, null); b.Slab(fp, 2.9f, null, E.white);
            b.Sign(TownMats.signsServices, Bld.SignCell(15), V(186.05f, 2.45f, -88f), Vector3.right, 4.6f, 0.76f);
            b.Counter(V(182.5f, 0.05f, -88f), 90f, 3f, true); b.Desk(V(180.5f, 0.05f, -88f), 90f); b.Box(E.black, V(180f, 0.8f, -89.2f), V(180.4f, 1.05f, -88.8f)); // the radio
            b.Box(E.fence, V(184.5f, 0.45f, -91.6f), V(185.7f, 0.5f, -89.4f), true); b.Telly(V(184f, 1.7f, -84.3f), Vector3.back); b.Vending(V(185.2f, 0.05f, -85f), -90f);
            b.Strip(V(181f, 2.6f, -88f), true);
            var k = new MeshKit(); Apron(k, R(186f, -98f, 192.4f, -80f));
            for (int i = 0; i < 4; i++) { float z = -97f + i * 4.2f; k.Box(E.line, V(186f, 0.045f, z), V(192f, 0.05f, z + 0.1f)); if (i < 3) TaxiBays.Add((V(189f, 0, z + 2.1f), -90f)); }
            k.Build("Bays", b.Root, true, ShadowCastingMode.Off);
            b.Done();
        }

        static void LockUps(Transform root)
        {
            var g = new GameObject("Mill Lane Lock-ups").transform; g.SetParent(root, false);
            Towers.Garages(g, "Lock-ups", V(191f, 0, -108f), 180f, 10, 31); // x 162..191, facing north onto a yard
            var k = new MeshKit(); Apron(k, R(162f, -108f, 192.4f, -99.5f)); k.Build("Yard", g, true, ShadowCastingMode.Off);
        }

        // ------------------------------------------------------------------ Briggs Scrap Metal
        static void Scrapyard(Transform root)
        {
            var g = new GameObject("Briggs Scrap Metal").transform; g.SetParent(root, false);
            var k = new MeshKit(); var r = new System.Random(55); var yard = R(160f, -150f, 192f, -118f);
            k.Quad(TownMats.gravel != null ? TownMats.gravel : E.asphalt, V(yard.xMin, 0.03f, yard.yMin), V(yard.xMax, 0.03f, yard.yMin), V(yard.xMax, 0.03f, yard.yMax), V(yard.xMin, 0.03f, yard.yMax), Vector3.up);
            CorrFence(k, V(160f, 0, -150f), V(160f, 0, -118f)); CorrFence(k, V(160f, 0, -118f), V(192f, 0, -118f)); CorrFence(k, V(160f, 0, -150f), V(192f, 0, -150f));
            CorrFence(k, V(192f, 0, -150f), V(192f, 0, -138f)); CorrFence(k, V(192f, 0, -130f), V(192f, 0, -118f));
            k.Box(E.railGreen, V(191.9f, 0f, -138f), V(192.1f, 2.2f, -134.2f)); // one gate leaf open, one shut
            // stacks of flattened cars in faded colours
            // (existing paints only: the editor saves this into the scene, and a material made here wouldn't be kept)
            var mats = new List<Material>(); foreach (var m in new[] { TownMats.redPaint, TownMats.nhsBlue, TownMats.panelDark, TownMats.rust, E.garage[0], E.garage[1], E.garage[3] }) if (m != null) mats.Add(m);
            foreach (var (sx, sz) in new[] { (163f, -147f), (163f, -141f), (163f, -135f), (169f, -147f), (175f, -147f) })
            {
                int n = 3 + r.Next(4);
                for (int i = 0; i < n; i++)
                {
                    float y = i * 0.55f, jx = (float)(r.NextDouble() - 0.5) * 0.4f, jz = (float)(r.NextDouble() - 0.5) * 0.4f;
                    k.Box(mats[r.Next(mats.Count)], V(sx + jx, y, sz + jz), V(sx + 4f + jx, y + 0.5f, sz + 1.7f + jz));
                    k.Box(TownMats.glassDark, V(sx + 1.2f + jx, y + 0.45f, sz + 0.3f + jz), V(sx + 2.6f + jx, y + 0.52f, sz + 1.4f + jz));
                }
                var col = new GameObject("Stack"); col.transform.SetParent(g, false); var bc = col.AddComponent<BoxCollider>(); bc.center = V(sx + 2f, n * 0.275f, sz + 0.85f); bc.size = V(4.4f, n * 0.55f, 2.1f);
            }
            // the grab crane: tracked base, cab, boom and a grab hanging
            Vector3 cb = V(176f, 0f, -132f);
            k.Box(E.black, cb + V(-1.8f, 0f, -1.4f), cb + V(1.8f, 0.9f, -0.6f)); k.Box(E.black, cb + V(-1.8f, 0f, 0.6f), cb + V(1.8f, 0.9f, 1.4f));
            k.Box(TownMats.yellow, cb + V(-1.5f, 0.9f, -1.3f), cb + V(1.5f, 2.2f, 1.3f)); k.Box(TownMats.glassDark, cb + V(0.4f, 2.2f, -1.2f), cb + V(1.5f, 3.6f, -0.1f)); k.Box(TownMats.yellow, cb + V(0.35f, 3.6f, -1.25f), cb + V(1.55f, 3.75f, -0.05f));
            k.Push(cb + V(0.5f, 2.6f, 0.4f), 0f); k.Push(Matrix4x4.Rotate(Quaternion.Euler(0, 0, 38f))); k.Box(TownMats.yellow, V(0f, -0.25f, -0.25f), V(7f, 0.25f, 0.25f)); k.Pop(); k.Pop();
            Vector3 tip = cb + V(0.5f, 2.6f, 0.4f) + Quaternion.Euler(0, 0, 38f) * V(7f, 0, 0);
            k.Push(Matrix4x4.Rotate(Quaternion.identity)); k.Box(TownMats.yellow, tip + V(-0.15f, -3.2f, -0.15f), tip + V(0.15f, 0f, 0.15f)); k.Pop();
            k.Box(E.black, tip + V(-0.7f, -4.2f, -0.6f), tip + V(0.7f, -3.2f, 0.6f));
            var cc = new GameObject("Crane"); cc.transform.SetParent(g, false); var cbx = cc.AddComponent<BoxCollider>(); cbx.center = cb + V(0, 1.4f, 0); cbx.size = V(3.6f, 2.8f, 2.8f);
            // the portakabin office and the weighbridge
            var b = new Bld(g, "Scrapyard Office"); var of = R(182f, -126f, 189f, -121f);
            b.Shell(of, 0.4f, 3.1f, TownMats.metalSheet != null ? TownMats.metalSheet : E.concrete, TownMats.lobbyWall, new[] { Bld.Win(1f, 3f, 1.3f, 2.4f), Bld.Door(4.5f, 5.4f, 2.4f) }, null, null, null);
            b.Roof(of, 3.1f, 0.1f); b.Slab(of, 0.4f, TownMats.lino, null); b.Box(E.black, V(of.xMin, 0f, of.yMin), V(of.xMax, 0.4f, of.yMax), true, true);
            b.Box(E.slabs, V(186.4f, 0f, -127f), V(187.6f, 0.2f, -126f), true, true); b.Box(E.slabs, V(186.4f, 0f, -126.6f), V(187.6f, 0.4f, -126f), true, true);
            b.Desk(V(185f, 0.4f, -122.5f), 180f); b.Strip(V(185.5f, 2.8f, -123.5f), true);
            b.Sign(TownMats.signsServices, Bld.SignCell(17), V(192.15f, 2.9f, -127f), Vector3.right, 5.4f, 0.9f);
            b.Done();
            k.Box(TownMats.steel, V(178f, 0.03f, -142f), V(188f, 0.12f, -138.6f)); // the weighbridge plate
            for (int i = 0; i < 8; i++) { var p = V(165f + (i % 4) * 1.1f, 0.03f, -124f - (i / 4) * 1.1f); k.Cyl(i % 3 == 0 ? (TownMats.redPaint ?? E.black) : TownMats.nhsBlue ?? E.black, p, 0.3f, 0.88f, 10); }
            for (int i = 0; i < 9; i++) { var p = V(170f + r.Next(6), 0.03f + (i % 3) * 0.22f, -122f); k.Cyl(TownMats.tyre != null ? TownMats.tyre : E.black, p, 0.33f, 0.22f, 10); }
            foreach (var spot in new[] { (V(181f, 0, -146f), 20f), (V(186f, 0, -146.5f), 70f), (V(170f, 0, -127f), 140f), (V(183f, 0, -131f), 260f) }) ScrapSpots.Add(spot);
            k.Build("Yard", g, true, ShadowCastingMode.On);
        }

        // ------------------------------------------------------------------ Swap Shop Pawnbrokers
        static void Pawn(Transform root)
        {
            var fp = R(214f, -62f, 230f, -48f);
            var b = Shed(root, "Swap Shop Pawnbrokers", fp, false, 5f, new[] { Bld.Win(1.5f, 6f, 0.6f, 2.8f), Bld.Door(7f, 8.4f, 2.4f), Bld.Win(9.5f, 12.5f, 0.6f, 2.8f) });
            Fascia(b, 16, fp.xMin, -55f, 3.7f, 9f, false);
            // a security grille over the windows
            for (float z = -60.5f; z < -49.5f; z += 0.15f) if (z < -56.5f || z > -54.9f) b.Box(TownMats.steel, V(213.86f, 0.6f, z), V(213.9f, 2.8f, z + 0.02f), false, true);
            // inside: glass display counters, shelves of tellies, guitars, tools and games, the counter with its screen
            for (int i = 0; i < 3; i++) { var c = V(219f, 0.05f, -60f + i * 3f); b.Box(TownMats.panelDark, c + V(-0.4f, 0, -1.1f), c + V(0.4f, 0.75f, 1.1f), true); b.Box(TownMats.glass, c + V(-0.4f, 0.75f, -1.1f), c + V(0.4f, 1.05f, 1.1f)); for (int j = 0; j < 4; j++) b.Box(j % 2 == 0 ? TownMats.yellow : TownMats.steel, c + V(-0.2f, 0.78f, -0.9f + j * 0.5f), c + V(0.1f, 0.82f, -0.7f + j * 0.5f)); }
            b.Shelf(V(229.6f, 0.05f, -61f), V(229.6f, 0.05f, -49f), 2.2f);
            for (int i = 0; i < 6; i++) b.Telly(V(229.3f, 0.55f + (i % 2) * 0.9f, -60f + (i / 2) * 3.5f), Vector3.left);
            for (int i = 0; i < 4; i++) b.Box(i % 2 == 0 ? (TownMats.redPaint ?? E.black) : E.black, V(225f + i * 0.5f, 0.6f, -61.7f), V(225.3f + i * 0.5f, 1.6f, -61.6f)); // guitars on the wall
            b.Counter(V(226f, 0.05f, -52f), 90f, 4f, true);
            b.Strip(V(222f, 4.7f, -58f), true); b.Strip(V(222f, 4.7f, -51f), false);
            var k = new MeshKit(); Apron(k, R(207.7f, -64f, 214f, -46f), TownMats.panel); k.Build("Forecourt", b.Root, true, ShadowCastingMode.Off);
            b.Done();
        }

        // ------------------------------------------------------------------ Mill Lane East: Carpet King, units to let, the estate sign
        static void East(Transform root)
        {
            var g = new GameObject("Mill Lane East").transform; g.SetParent(root, false);
            var k = new MeshKit(); var kerb = new MeshKit();
            // the estate road east from Mill Lane at z -90
            k.Quad(E.asphalt, V(207.6f, 0.04f, -93.2f), V(288f, 0.04f, -93.2f), V(288f, 0.04f, -86.8f), V(207.6f, 0.04f, -86.8f), Vector3.up);
            foreach (float z in new[] { -93.35f, -86.65f }) kerb.Box(E.kerb, V(207.6f, 0f, z - 0.08f), V(288f, 0.14f, z + 0.08f));
            // Carpet King (north side, front to the south) and two units to let (south side, fronts north)
            var ck = new Bld(g, "Carpet King"); var cf = R(236f, -82f, 288f, -50f); var brick = TownMats.brownBrick;
            ck.Shell(cf, 0f, 1.2f, brick, TownMats.panel, new[] { Bld.Door(6f, 8f, 1.2f), Bld.Door(30f, 36f, 1.2f) }, null, null, null);
            ck.Shell(cf, 1.2f, 8f, Clad, TownMats.panel, new[] { Bld.Door(6f, 8f, 2.4f), Bld.Win(9f, 16f, 1.2f, 3f), Bld.Door(30f, 36f, 5f) }, null, null, null);
            ck.Roof(cf, 8f, 0.8f, Clad); ck.Slab(cf, 0.05f, TownMats.panelDark, null);
            ck.Box(TownMats.rollerDoor, V(266f, 0.05f, -82.08f), V(272f, 5f, -82f), true, true);
            ck.Sign(TownMats.signsServices, Bld.SignCell(19), V(256f, 6.6f, -82.05f), Vector3.back, 12f, 2f);
            for (int i = 0; i < 6; i++) ck.Box(i % 2 == 0 ? (TownMats.redPaint ?? E.black) : TownMats.nhsBlue ?? E.black, V(240f + i * 3f, 0.05f, -78f), V(242.4f + i * 3f, 0.9f, -60f), true); // rolls of carpet
            ck.Strip(V(250f, 7.6f, -70f), true); ck.Strip(V(274f, 7.6f, -70f), true);
            ck.Done();
            foreach (var (x0, x1) in new[] { (214f, 248f), (254f, 288f) })
            {
                var u = new Bld(g, "Unit to let " + x0); var uf = R(x0, -128f, x1, -100f);
                u.Shell(uf, 0f, 1.2f, brick, TownMats.panel, null, null, new[] { Bld.Door(4f, 6f, 1.2f), Bld.Door(12f, 18f, 1.2f) }, null);
                u.Shell(uf, 1.2f, 7f, Clad, TownMats.panel, null, null, new[] { Bld.Door(4f, 6f, 2.4f), Bld.Door(12f, 18f, 5f) }, null);
                u.Roof(uf, 7f, 0.6f, Clad); u.Slab(uf, 0.05f, TownMats.panelDark, null);
                u.Box(TownMats.rollerDoor, V(x1 - 18f, 0.05f, -100f), V(x1 - 12f, 5f, -99.92f), true, true);
                u.Sign(TownMats.signsServices, Bld.SignCell(18), V((x0 + x1) * 0.5f + 4f, 5.8f, -99.95f), Vector3.forward, 6f, 1f);
                u.Done();
            }
            Apron(k, R(236f, -86.6f, 288f, -82f)); Apron(k, R(214f, -100f, 288f, -93.4f));
            // the estate sign at the top of Mill Lane
            var s = new Bld(g, "Estate Sign");
            s.Box(E.black, V(208.6f, 0f, -47.1f), V(208.7f, 2.4f, -47f), true, true); s.Box(E.black, V(208.6f, 0f, -43.1f), V(208.7f, 2.4f, -43f), true, true);
            s.Sign(TownMats.signsServices, Bld.SignCell(13), V(208.75f, 1.9f, -45f), Vector3.left, 4.2f, 0.7f);
            s.Done();
            k.Build("Road", g, true, ShadowCastingMode.Off); kerb.Build("Kerbs", g, true, ShadowCastingMode.Off);
        }

        /// <summary>Wrecks in the scrapyard (runtime: they're the cars from CarKit).</summary>
        public static void Dress(Transform parent)
        {
            if (ScrapSpots.Count == 0) { ScrapSpots.Add((V(181f, 0, -146f), 20f)); ScrapSpots.Add((V(186f, 0, -146.5f), 70f)); ScrapSpots.Add((V(170f, 0, -127f), 140f)); ScrapSpots.Add((V(183f, 0, -131f), 260f)); }
            var root = new GameObject("Scrapyard Wrecks").transform; root.SetParent(parent, false);
            string[] kinds = { "saloon90", "hatch70", "boxy80", "estate90" }; int i = 0;
            foreach (var (p, yaw) in ScrapSpots) CarKit.Scenery(root, kinds[i % kinds.Length], i++ % 2 == 0 ? CarKit.Wear.Wreck : CarKit.Wear.Burnt, p + Vector3.up * 0.05f, yaw, 900 + i);
        }
    }
}
