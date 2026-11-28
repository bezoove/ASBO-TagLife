using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// The streets that fill the gaps between the districts (<see cref="TownPlan.Infill"/>), built into the scene by
    /// the editor with the rest of the town. The big buildings wear the photographed building fronts from the Drive
    /// (CC BY 4.0, Julio Sillet), lined up so their windows sit on the floors, with stone sills, cornices, parapets,
    /// slate roofs and chimneys built on:
    ///  - Kestrel Road and Northway: two more parades of shops with flats over (in the high street's own styles, in
    ///    the packs' red and painted brick), Mafeking Terrace, Wend Mill (red brick, five storeys, its engine house and
    ///    chimney, now flats), Wend Snooker Club, Kestrel Court, Northway House (the 60s council offices) and Mill Street.
    ///  - North of Northway: Kestrel Chambers (1889, on the corner), the multi-storey car park, Kestrel House flats;
    ///    Thornbury Parade, Wend Gospel Hall and a terrace by Thornbury Road.
    ///  - Thornbury Road: a corner parade and houses by the manor, and a parade on the east side before the casino.
    /// Every name is made up (Art/Town/street_shopfronts*.png, street_signs.png).
    /// </summary>
    public static class Infill
    {
        public const string RootName = "Infill";
        // building lines: the back of each road's pavement (the high street's are 7.6 m off the centreline)
        const float KestrelE = -152.4f, NorthwayS = 117.4f, NorthwayN = 132.6f, ThornburyW = 120.4f, ThornburyE = 135.6f, LongN = -22.4f;

        /// <summary>A photographed front: one repeat covers w x h metres; vOff and u0 line its windows up with the floors
        /// and corners; cols / rows say where its windows are in a repeat (for the stone sills); winW their width.</summary>
        class Fac
        {
            public Material m; public float w, h, vOff, u0, winW; public float[] cols, rows;
            public Fac Wide(float nw) => new Fac { m = m, w = nw, h = h, vOff = vOff, u0 = u0, winW = winW, cols = cols, rows = rows };
        }
        static Fac F03, B03, F05, F04, F07, F08, B02, B04, Brick, Brick2, Conc;
        static Material brick, brick2, conc, wood, shutter, sett, pave, slate, felt, black, white, doors, stone, glass, steel, frame, signs, shops1, shops2, asphalt, line, glow, lampCol, concrete, yellow, red;
        static Material Res(string n) => Resources.Load<Material>(n);

        static Material S(string name, float tile, Material fallback)
        {
            var m = Surfaces.Source(name); if (m == null) return fallback;
            MeshKit.Tile[m] = tile; return m;
        }
        static Fac FacOf(string name, float w, float h, float vOff, float u0, float winW, float[] cols, float[] rows, Material fallback)
        {
            var m = Surfaces.Source(name);
            if (m == null) return new Fac { m = fallback, w = 2.7f, h = 2.7f };
            return new Fac { m = m, w = w, h = h, vOff = vOff, u0 = u0, winW = winW, cols = cols, rows = rows };
        }

        static bool Load()
        {
            if (!TownMats.Load()) return false;
            var E = CouncilEstate.M;
            brick = S("red_bricks_01", 2f, TownMats.redBrick); brick2 = S("red_bricks_02", 2f, brick); conc = S("concrete_damaged", 3f, E.concrete);
            wood = S("wood_painted", 2f, E.fence); shutter = S("shutter_roller", 1.5f, TownMats.rollerDoor); pave = S("sidewalk_01", 2f, E.pave);
            sett = S("cobblestone_floor_09", Surfaces.Size["cobblestone_floor_09"], pave);
            slate = Res("Town/TownSlate") ?? E.roof; MeshKit.Tile[slate] = 3f;
            stone = Res("Town/TownStone") ?? E.concrete; if (!MeshKit.Tile.ContainsKey(stone)) MeshKit.Tile[stone] = 2f;
            frame = Res("Town/TownFrame") ?? E.black;
            felt = E.felt; black = E.black; white = E.white; doors = E.doors; asphalt = E.asphalt; line = E.line; glow = E.glow; lampCol = E.lampCol; concrete = E.concrete;
            glass = TownMats.glassDark; steel = TownMats.steel; yellow = TownMats.yellow; red = TownMats.redPaint;
            signs = Res("Town/TownStreetSigns") ?? TownMats.signsServices; shops1 = Res("Town/TownStreetShops"); shops2 = Res("Town/TownStreetShops2");
            // where each front's windows are, measured off the photographs (fractions of one repeat, from its left and bottom)
            F03 = FacOf("facade_03", 6.4f, 6f, -0.07f, 0f, 0.2f, new[] { 0.25f, 0.75f }, new[] { 0.068f, 0.568f }, brick);       // Victorian brown brick, sashes, stone pilaster
            B03 = FacOf("brickfront_03", 9f, 9f, -0.026f, 0.125f, 0.12f, new[] { 0f, 0.25f, 0.5f, 0.75f }, new[] { 0.074f, 0.406f, 0.74f }, brick); // red brick mill, small white windows
            F05 = FacOf("facade_05", 7f, 7f, 0f, 0f, 0f, null, null, concrete);                                                  // 60s concrete and pebble panels, window bands
            F04 = FacOf("facade_04", 12f, 12f, 0.1f, 0f, 0f, null, null, concrete);                                               // car park decks
            F07 = FacOf("facade_07", 7f, 6f, -0.05f, 0f, 0.16f, new[] { 0.25f, 0.75f }, new[] { 0.1f, 0.6f }, brick);              // grey brick flats
            F08 = FacOf("facade_08", 10f, 8.5f, 0f, 0f, 0.137f, new[] { 0.139f, 0.361f, 0.639f, 0.861f }, new[] { 0.184f, 0.684f }, stone); // Victorian stone, arched windows
            B02 = FacOf("brickfront_02", 8f, 8f, 0f, 0f, 0f, null, null, brick);                                                  // chapel lancets
            B04 = FacOf("brickfront_04", 8f, 8f, 0.2f, 0f, 0.266f, new[] { 0.246f, 0.754f }, new[] { 0.387f }, brick);             // tall arched works windows
            Brick = new Fac { m = brick, w = 2f, h = 2f }; Brick2 = new Fac { m = brick2, w = 2f, h = 2f }; Conc = new Fac { m = conc, w = 3f, h = 3f };
            return true;
        }

        // ------------------------------------------------------------------------------------------ build
        class Area
        {
            public Transform root; public readonly MeshKit k = new MeshKit(), flat = new MeshKit(), detail = new MeshKit();
            public readonly List<(Matrix4x4 f, Vector3 c, Vector3 s)> solids = new List<(Matrix4x4, Vector3, Vector3)>();
            public Matrix4x4 f = Matrix4x4.identity;
            public void Frame(Vector3 origin, float yaw) { f = Matrix4x4.TRS(origin, Quaternion.Euler(0, yaw, 0), Vector3.one); k.Push(f); detail.Push(f); flat.Push(f); }
            public void End() { k.Pop(); detail.Pop(); flat.Pop(); f = Matrix4x4.identity; }
            public void Solid(Vector3 mn, Vector3 mx) => solids.Add((f, (mn + mx) * 0.5f, mx - mn));
            /// <summary>Interiors waiting for the colliders they swap out (index into solids).</summary>
            public readonly List<(TowerInterior ti, int idx)> links = new List<(TowerInterior, int)>();
        }

        public static Transform Build(Transform parent)
        {
            if (!Load()) return null;
            var root = new GameObject(RootName).transform; root.SetParent(parent, false);
            int n = 0;
            n += Kestrel(root); n += NorthwayWest(root); n += NorthwayEast(root); n += ThornburyCorner(root); n += ThornburyEast(root);
            Debug.Log($"[ASBO] infill: {n} buildings in the gaps (Kestrel Road, Northway, Thornbury Road)");
            return root;
        }

        static Area Begin(Transform root, string name) { var a = new Area { root = new GameObject(name).transform }; a.root.SetParent(root, false); return a; }

        static void Finish(Area a)
        {
            a.k.Build("Buildings", a.root, false, ShadowCastingMode.On);
            var fl = a.flat.Build("Ground", a.root, false, ShadowCastingMode.Off);
            var de = a.detail.Build("Details", a.root, false, ShadowCastingMode.Off); if (de != null) de.layer = Bld.DetailLayer;
            var col = new GameObject("Solid").transform; col.SetParent(a.root, false);
            var made = new List<BoxCollider>();
            foreach (var (f, c, s) in a.solids)
            {
                var g = new GameObject("c"); g.transform.SetParent(col, false); g.transform.position = f.MultiplyPoint3x4(c); g.transform.rotation = f.rotation;
                var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(Mathf.Max(0.05f, s.x), Mathf.Max(0.05f, s.y), Mathf.Max(0.05f, s.z)); made.Add(bc);
            }
            foreach (var (ti, idx) in a.links) if (ti != null && idx < made.Count) ti.baked.Add(made[idx]);
        }

        static HighStreet.Unit U(int cell, float w, HighStreet.Era e, int storeys) => HighStreet.Shop(cell, w, e, storeys);
        const HighStreet.Era Vic = HighStreet.Era.Victorian, Thirties = HighStreet.Era.Thirties, Sixties = HighStreet.Era.Sixties;

        // ------------------------------------------------------------------------------------------ Kestrel Road / Northway / Mill Street
        static int Kestrel(Transform root)
        {
            var a = Begin(root, "Kestrel Block");
            // two parades: up Kestrel Road to the corner, then along Northway
            HighStreet.ExtraParade(a.root, "Kestrel Parade", new[] { U(19, 6.5f, Vic, 3), U(5, 6f, Vic, 3), U(21, 6.5f, Vic, 2), U(7, 6f, Vic, 3), U(13, 6.5f, Thirties, 2), U(4, 6f, Vic, 3), U(22, 6.5f, Vic, 3), U(0, 6.5f, Vic, 3), U(11, 6.5f, Vic, 3) },
                new Vector3(KestrelE, 0f, 60.4f), -90f, shops1, brick2, 2f);
            HighStreet.ExtraParade(a.root, "Northway Parade", new[] { U(2, 6f, Vic, 3), U(16, 6.5f, Vic, 3), U(3, 6f, Sixties, 2), U(23, 6f, Sixties, 2), U(12, 6.5f, Vic, 3), U(9, 6f, Vic, 2), U(20, 5.5f, Vic, 2) },
                new Vector3(-141.4f, 0f, NorthwayS), 0f, shops1, brick, 2f);

            // Mafeking Terrace: seven houses up Kestrel Road, little front gardens, yards and a back alley
            a.Frame(new Vector3(KestrelE + 2.6f, 0f, 14.4f), -90f); Terrace(a, 7, 5.6f, 8.5f, 2.6f, 5f, F03, 1); a.End();

            // Mill Street: a lane off Kestrel Road past the mill to a turning head, a footway along the club
            a.flat.Quad(asphalt, V(KestrelE, 0.05f, 54.4f), V(-58f, 0.05f, 54.4f), V(-58f, 0.05f, 59.6f), V(KestrelE, 0.05f, 59.6f), Vector3.up);
            a.flat.Quad(asphalt, V(-66f, 0.05f, 50f), V(-56f, 0.05f, 50f), V(-56f, 0.05f, 64f), V(-66f, 0.05f, 64f), Vector3.up);
            a.flat.Box(stone, V(-131f, 0f, 52f), V(-66f, 0.12f, 54.4f), 4);                    // footway along the snooker club
            for (float x = -148f; x < -60f; x += 6f) a.flat.Box(line, V(x, 0.05f, 56.9f), V(x + 3f, 0.062f, 57.1f), 4);  // the centre line, dashed
            a.flat.Box(yellow, V(-140f, 0.05f, 54.5f), V(-70f, 0.062f, 54.6f), 4); a.flat.Box(yellow, V(-140f, 0.05f, 54.7f), V(-70f, 0.062f, 54.8f), 4); // double yellows
            foreach (float x in new[] { -124f, -101f, -76f }) Lamp(a, V(x, 0f, 53.6f), Vector3.forward);
            Lamp(a, V(-146f, 0f, 60f), Vector3.back);
            SignPost(a, V(-151f, 0f, 53.8f), Vector3.left, 9);                                 // MILL STREET
            SignPost(a, V(-136f, 0f, 60.6f), Vector3.back, 10);                                // RESIDENTS PARKING ONLY

            // the mill yard: setts in front of and round the mill, parking along the back
            a.flat.Quad(sett, V(-141f, 0.04f, 59.6f), V(-86f, 0.04f, 59.6f), V(-86f, 0.04f, 100f), V(-141f, 0.04f, 100f), Vector3.up);
            for (int i = 0; i < 16; i++) { float x = -138f + i * 2.6f; a.flat.Box(white, V(x, 0.04f, 92f), V(x + 0.1f, 0.052f, 97f), 4); }
            Mill(a);

            // Wend Snooker Club on Mill Street, its car park behind
            a.Frame(new Vector3(-128f, 0f, 52f), 0f); Works(a, 28f, 16f, 8.2f, 8); a.End();
            a.flat.Quad(asphalt, V(-128f, 0.04f, 14f), V(-88f, 0.04f, 14f), V(-88f, 0.04f, 34f), V(-128f, 0.04f, 34f), Vector3.up);
            a.flat.Quad(asphalt, V(-99f, 0.04f, 34f), V(-93f, 0.04f, 34f), V(-93f, 0.04f, 54.4f), V(-99f, 0.04f, 54.4f), Vector3.up);
            for (int i = 0; i < 15; i++) { float x = -127f + i * 2.5f; a.flat.Box(white, V(x, 0.04f, 15f), V(x + 0.1f, 0.052f, 20f), 4); a.flat.Box(white, V(x, 0.04f, 28f), V(x + 0.1f, 0.052f, 33f), 4); }

            // Kestrel Court: four storeys of grey brick flats facing the lane
            a.Frame(new Vector3(-58f, 0f, 66f), 180f); FlatsName = "Kestrel Court"; Flats(a, 26f, 11f, 4, F07, 13); a.End();
            a.flat.Quad(pave, V(-73f, 0.05f, 59.6f), V(-70f, 0.05f, 59.6f), V(-70f, 0.05f, 66f), V(-73f, 0.05f, 66f), Vector3.up);

            // Northway House: the council's 60s offices on Northway
            a.Frame(new Vector3(-96f, 0f, NorthwayS), 0f); Offices(a, 40f, 14f); a.End();
            Finish(a); return 2 + 7 + 5;
        }

        static int NorthwayWest(Transform root)
        {
            var a = Begin(root, "Northway West");
            a.Frame(new Vector3(-122.4f, 0f, NorthwayN), 180f); Chambers(a, new[] { 1, 8, 9, 2 }, 7.5f, 14f); a.End();
            a.Frame(new Vector3(-76f, 0f, 140f), 180f); CarPark(a, 38f, 36f); a.End();
            a.flat.Quad(asphalt, V(-88.6f, 0.05f, NorthwayN), V(-81.4f, 0.05f, NorthwayN), V(-81.4f, 0.05f, 140.2f), V(-88.6f, 0.05f, 140.2f), Vector3.up);
            a.Frame(new Vector3(KestrelE + 3f, 0f, 152f), -90f); FlatsName = "Kestrel House Flats"; Flats(a, 32f, 11f, 4, F07, 13); a.End();
            a.flat.Quad(pave, V(KestrelE, 0.05f, 166.6f), V(KestrelE + 3f, 0.05f, 166.6f), V(KestrelE + 3f, 0.05f, 169.4f), V(KestrelE, 0.05f, 169.4f), Vector3.up);
            Finish(a); return 3;
        }

        static int NorthwayEast(Transform root)
        {
            var a = Begin(root, "Northway East");
            a.Frame(new Vector3(ThornburyW, 0f, NorthwayN), 180f); FacadeParade(a, new[] { 0, 7, 6, 3, 4, 5 }, 7f, 11f, B03); a.End();
            a.Frame(new Vector3(ThornburyW - 2f, 0f, 162f), 90f); Hall(a, 12f, 20f); a.End();
            a.flat.Quad(pave, V(ThornburyW - 2f, 0.05f, 155.2f), V(ThornburyW, 0.05f, 155.2f), V(ThornburyW, 0.05f, 156.8f), V(ThornburyW - 2f, 0.05f, 156.8f), Vector3.up);
            a.Frame(new Vector3(ThornburyW - 2f, 0f, 194f), 90f); Terrace(a, 5, 5.6f, 8.5f, 2f, 5f, F03, 2); a.End();
            Finish(a); return 7;
        }

        static int ThornburyCorner(Transform root)
        {
            var a = Begin(root, "Thornbury Corner");
            HighStreet.ExtraParade(a.root, "Thornbury Corner Parade", new[] { U(1, 5.6f, Vic, 3), U(8, 5.6f, Vic, 3), U(14, 5.5f, Vic, 2), U(6, 5.6f, Vic, 2) },
                new Vector3(ThornburyW, 0f, LongN), 180f, shops1, brick, 2f);
            a.Frame(new Vector3(ThornburyW - 2f, 0f, 20.4f), 90f); Terrace(a, 4, 5.6f, 8.5f, 2f, 4f, F03, 3); a.End();
            Finish(a); return 2;
        }

        static int ThornburyEast(Transform root)
        {
            var a = Begin(root, "Thornbury East");
            HighStreet.ExtraParade(a.root, "Thornbury Road Parade", new[] { U(17, 6f, Sixties, 2), U(18, 6f, Sixties, 2), U(15, 6f, Thirties, 2), U(10, 6f, Vic, 3), U(21, 6f, Vic, 3), U(22, 6.4f, Vic, 3) },
                new Vector3(ThornburyE, 0f, -16f), -90f, shops1, brick2, 2f);
            Finish(a); return 1;
        }

        // ------------------------------------------------------------------------------------------ the buildings (each in its own frame: front at z = 0 facing +z, depth back to -z)
        /// <summary>A terrace of two-storey houses: the photographed front with doors on it (in mirrored pairs), stone
        /// sills upstairs, a slate roof with a chimney stack on every party wall, brick backs with windows and doors, low
        /// front garden walls with paths, walled back yards with gates, and the back alley.</summary>
        static void Terrace(Area a, int n, float w, float d, float garden, float yard, Fac face, int seed)
        {
            var k = a.k; var f = face.Wide(w); var rng = new System.Random(seed * 31);
            const float eave = 6.2f; float len = n * w;
            k.Box(stone, V(-0.04f, -0.6f, -d - 0.04f), V(len + 0.04f, 0.42f, 0.04f));                       // plinth
            FacWall(k, f, V(0, 0, 0), V(len, 0, 0), 0.42f, eave, 0f, Vector3.forward, 3f, V(len, 0, 0));
            for (int i = 0; i < n; i++)
            {
                float x0 = i * w, x1 = x0 + w; bool left = i % 2 == 0;
                float dx = left ? x0 + 0.25f * w : x1 - 0.25f * w;                                             // the door where a ground floor window would be
                Door(k, dx, rng.Next(4));
                // garden: low wall with a gap at the path, the path, a divider to the next house
                float gz = garden - 0.15f;
                k.Box(brick, V(x0, 0f, gz - 0.11f), V(dx - 0.6f, 0.75f, gz + 0.11f)); k.Box(brick, V(dx + 0.6f, 0f, gz - 0.11f), V(x1, 0.75f, gz + 0.11f));
                k.Box(stone, V(x0, 0.75f, gz - 0.14f), V(dx - 0.6f, 0.82f, gz + 0.14f)); k.Box(stone, V(dx + 0.6f, 0.75f, gz - 0.14f), V(x1, 0.82f, gz + 0.14f));
                if (i > 0) k.Box(brick, V(x0 - 0.1f, 0f, 0.05f), V(x0 + 0.1f, 0.75f, gz));
                a.flat.Quad(pave, V(dx - 0.55f, 0.06f, 0.05f), V(dx + 0.55f, 0.06f, 0.05f), V(dx + 0.55f, 0.06f, garden), V(dx - 0.55f, 0.06f, garden), Vector3.up);
                // wheelie bin in the garden
                float bx = left ? x1 - 1.2f : x0 + 0.6f; Bin(a.detail, V(bx, 0f, gz - 0.9f), rng);
                // the back: plain brick, two windows up, a window and the back door down
                BackWindow(k, x0 + w * 0.3f, 3.8f, -d); BackWindow(k, x0 + w * 0.7f, 3.8f, -d); BackWindow(k, left ? x0 + w * 0.7f : x0 + w * 0.3f, 1.0f, -d);
                float bd = left ? x0 + w * 0.28f : x0 + w * 0.72f;
                k.QuadUV(doors, V(bd + 0.45f, 0.42f, -d - 0.02f), V(bd - 0.45f, 0.42f, -d - 0.02f), V(bd - 0.45f, 2.45f, -d - 0.02f), V(bd + 0.45f, 2.45f, -d - 0.02f), Vector3.back,
                    new Vector2(0.75f + 0.008f, 0), new Vector2(0.992f, 0), new Vector2(0.992f, 1), new Vector2(0.758f, 1));
                // the yard: walls, the gate onto the alley
                float yz = -d - yard;
                if (i > 0) k.Box(brick, V(x0 - 0.1f, 0f, yz), V(x0 + 0.1f, 1.8f, -d));
                float gx = left ? x0 + 0.8f : x1 - 1.8f;
                k.Box(brick, V(x0, 0f, yz - 0.11f), V(gx, 1.8f, yz + 0.11f)); k.Box(brick, V(gx + 1f, 0f, yz - 0.11f), V(x1, 1.8f, yz + 0.11f));
                k.Box(wood, V(gx, 0f, yz - 0.04f), V(gx + 1f, 1.7f, yz + 0.04f));
                a.flat.Quad(concrete, V(x0, 0.04f, -d), V(x1, 0.04f, -d), V(x1, 0.04f, yz), V(x0, 0.04f, yz), Vector3.up);
                if (rng.Next(3) == 0) Bin(a.detail, V(gx + (left ? 1.6f : -0.6f), 0f, yz - 0.8f), rng);
            }
            k.Box(brick, V(-0.11f, 0f, -d - yard), V(0.11f, 1.8f, -d)); k.Box(brick, V(len - 0.11f, 0f, -d - yard), V(len + 0.11f, 1.8f, -d));
            a.flat.Quad(TownMats.paviors, V(-1f, 0.04f, -d - yard - 0.11f), V(len + 1f, 0.04f, -d - yard - 0.11f), V(len + 1f, 0.04f, -d - yard - 2.6f), V(-1f, 0.04f, -d - yard - 2.6f), Vector3.up);
            // ends and back
            FacWall(k, Brick, V(0, 0, 0), V(0, 0, -d), 0.42f, eave, 0f, Vector3.left);
            FacWall(k, Brick, V(len, 0, 0), V(len, 0, -d), 0.42f, eave, 0f, Vector3.right);
            FacWall(k, Brick, V(0, 0, -d), V(len, 0, -d), 0.42f, eave, 0f, Vector3.back);
            k.Box(stone, V(0f, eave - 0.32f, 0f), V(len, eave - 0.12f, 0.12f), 16);                           // dentil course under the gutter
            float ridge = Pitched(k, 0f, len, d, eave, 37f, brick);
            for (int i = 0; i <= n; i++) Chimney(k, Mathf.Clamp(i * w, 0.5f, len - 0.5f), ridge, -d * 0.5f, brick);
            // satellite dishes on a few
            for (int i = 0; i < n; i++) if (rng.Next(3) == 0) Dish(a.detail, V(i * w + 0.6f, eave - 1.2f, 0.02f), Vector3.forward);
            a.Solid(V(0, 0, -d), V(len, eave, 0));
        }

        /// <summary>Wend Mill: five storeys of red brick with the windows on every floor, a cornice and parapet with the
        /// datestone, taking-in doors stacked up the end under a hoist beam, flats' glazed entrance and the developer's
        /// banner; the engine house beside it, and the chimney.</summary>
        static void Mill(Area a)
        {
            var k = a.k; const float L = 36f, D = 16f, top = 16.2f;
            a.Frame(new Vector3(-100f, 0f, 66f), 180f);
            k.Box(stone, V(-0.05f, -0.6f, -D - 0.05f), V(L + 0.05f, 0.45f, 0.05f));
            var mdoors = DoorsFor(L, 3);
            FrontWithDoors(k, B03, L, 0.45f, top, 0f, mdoors);
            FacWall(k, B03, V(0, 0, -D), V(L, 0, -D), 0.45f, top, 0f, Vector3.back); FacWall(k, B03, V(0, 0, 0), V(0, 0, -D), 0.45f, top, 0f, Vector3.left); FacWall(k, B03, V(L, 0, 0), V(L, 0, -D), 0.45f, top, 0f, Vector3.right);
            k.Box(stone, V(-0.25f, 15.3f, -D - 0.25f), V(L + 0.25f, 16.0f, 0.25f));                            // cornice
            k.Box(stone, V(-0.1f, 16.0f, -D - 0.1f), V(L + 0.1f, 16.3f, 0.1f));                                // coping
            for (int i = 1; i < 4; i++) k.Box(stone, V(-0.06f, 3f * i + 0.38f, -D - 0.06f), V(L + 0.06f, 3f * i + 0.5f, 0.06f)); // floor bands
            FlatRoof(k, 0f, L, D, 15.6f, top, B03.m);
            k.Box(stone, V(L * 0.5f - 2.6f, 16.0f, -0.3f), V(L * 0.5f + 2.6f, 17.4f, 0.12f));                  // datestone pediment
            SignQuad(k, Bld.SignCell(0), V(L * 0.5f, 16.7f, 0.13f), Vector3.forward, 4.6f, 0.76f);
            // the flats' entrances: stone heads and black canopies over the three stair doors, the banner up the front
            foreach (float dx in mdoors) { k.Box(stone, V(dx - 0.8f, 2.65f, 0f), V(dx + 0.8f, 2.95f, 0.2f)); k.Box(black, V(dx - 1.3f, 3.0f, 0f), V(dx + 1.3f, 3.15f, 1.3f)); }
            SignQuad(k, Bld.SignCell(1), V(L * 0.25f, 7.6f, 0.06f), Vector3.forward, 9f, 1.5f);
            SignQuad(k, Bld.SignCell(14), V(L * 0.78f, 4.2f, 0.06f), Vector3.forward, 5f, 0.83f);
            // taking-in doors up the east end (local x = 0), hoist beam at the top
            for (int f = 0; f < 5; f++)
            {
                float y0 = 0.45f + f * 3f + (f == 0 ? 0f : 0.4f), y1 = y0 + (f == 0 ? 2.8f : 2.2f);
                k.Box(wood, V(-0.08f, y0, -D * 0.5f - 0.8f), V(0.02f, y1, -D * 0.5f + 0.8f));
                k.Box(stone, V(-0.14f, y1, -D * 0.5f - 1f), V(0.02f, y1 + 0.25f, -D * 0.5f + 1f));
            }
            k.Box(black, V(-1.6f, 16.4f, -D * 0.5f - 0.15f), V(0.2f, 16.7f, -D * 0.5f + 0.15f));
            k.Box(black, V(-1.5f, 15.6f, -D * 0.5f - 0.02f), V(-1.46f, 16.4f, -D * 0.5f + 0.02f));
            // the water tank and stair tower on the roof
            k.Box(brick, V(L - 6f, 16.3f, -D + 0.5f), V(L - 0.5f, 19.6f, -D + 5.5f)); k.Box(stone, V(L - 6.15f, 19.6f, -D + 0.35f), V(L - 0.35f, 19.85f, -D + 5.65f));
            int msi = a.solids.Count; a.Solid(V(0, 0, -D), V(L, top, 0));
            {
                var wins = new List<(Vector3, Vector3, float, float)>();
                FacWins(B03, V(0, 0, 0), V(L, 0, 0), 0.45f, top, 0f, Vector3.forward, 1.35f, wins); FacWins(B03, V(0, 0, -D), V(L, 0, -D), 0.45f, top, 0f, Vector3.back, 1.35f, wins);
                FacWins(B03, V(0, 0, 0), V(0, 0, -D), 0.45f, top, 0f, Vector3.left, 1.35f, wins); FacWins(B03, V(L, 0, 0), V(L, 0, -D), 0.45f, top, 0f, Vector3.right, 1.35f, wins);
                Closes(a, "Wend Mill", L, D, 0.45f, 3f, 5, 3, wins, 1851, msi);
            }
            a.End();
            // the engine house next door (gable to the lane) and the chimney behind it
            a.Frame(new Vector3(-86f, 0f, 66f), 180f);
            k.Box(stone, V(-0.05f, -0.6f, -12.05f), V(12.05f, 0.45f, 0.05f));
            FacBox(k, 0f, 12f, 12f, 0.45f, 8.2f, 0f, B04, B04, Brick, Brick);
            k.Box(stone, V(-0.12f, 7.9f, -12.12f), V(12.12f, 8.2f, 0.12f));
            k.Push(Matrix4x4.TRS(V(0f, 0f, -12f), Quaternion.Euler(0, -90f, 0), Vector3.one)); Pitched(k, 0f, 12f, 12f, 8.2f, 30f, brick); k.Pop();
            k.Box(wood, V(4.6f, 0.45f, 0f), V(7.4f, 4.2f, 0.12f)); k.Box(stone, V(4.4f, 4.2f, -0.02f), V(7.6f, 4.5f, 0.16f));
            a.Solid(V(0, 0, -12f), V(12f, 8.2f, 0)); a.End();
            var c = V(-91.5f, 0f, 84f);
            k.Box(brick, c + V(-2.1f, -0.6f, -2.1f), c + V(2.1f, 4f, 2.1f)); k.Box(stone, c + V(-2.25f, 4f, -2.25f), c + V(2.25f, 4.35f, 2.25f));
            k.Cyl(brick, c + V(0, 4.35f, 0), 1.7f, 29.5f, 14, false, 1.0f);
            k.Cyl(stone, c + V(0, 33.85f, 0), 1.18f, 0.5f, 14, true, 1.18f);
            k.Cyl(black, c + V(0, 34.35f, 0), 0.95f, 0.15f, 14, true, 0.95f);
            a.solids.Add((Matrix4x4.identity, c + V(0, 2f, 0), V(4.2f, 4f, 4.2f)));
            a.solids.Add((Matrix4x4.identity, c + V(0, 18f, 0), V(2.6f, 28f, 2.6f)));
        }

        /// <summary>A brick works building with tall arched windows (the snooker club): a slate roof, a door and its board.</summary>
        static void Works(Area a, float L, float D, float h, int sign)
        {
            var k = a.k;
            k.Box(stone, V(-0.05f, -0.6f, -D - 0.05f), V(L + 0.05f, 0.45f, 0.05f));
            FacBox(k, 0f, L, D, 0.45f, h, 0f, B04, Brick, B04, Brick);
            k.Box(stone, V(-0.12f, h - 0.3f, -D - 0.12f), V(L + 0.12f, h, 0.12f));
            Pitched(k, 0f, L, D, h, 24f, brick);
            // door between the windows: a painted door and a stone surround, the board over, a lamp
            float cx = L * 0.5f;
            k.Box(stone, V(cx - 1.2f, 0.45f, 0f), V(cx + 1.2f, 3.4f, 0.18f), 32);
            k.QuadUV(doors, V(cx + 0.9f, 0.45f, 0.19f), V(cx - 0.9f, 0.45f, 0.19f), V(cx - 0.9f, 3.0f, 0.19f), V(cx + 0.9f, 3.0f, 0.19f), Vector3.forward,
                new Vector2(0.258f, 0), new Vector2(0.492f, 0), new Vector2(0.492f, 1), new Vector2(0.258f, 1));
            k.Box(frame, V(cx - 3.2f, 3.6f, 0f), V(cx + 3.2f, 4.9f, 0.12f));
            SignQuad(k, Bld.SignCell(sign), V(cx, 4.25f, 0.13f), Vector3.forward, 6.2f, 1.03f);
            a.detail.Box(glow, V(cx - 0.15f, 3.45f, 0.2f), V(cx + 0.15f, 3.6f, 0.5f));
            a.Solid(V(0, 0, -D), V(L, h, 0));
        }

        /// <summary>Flats in grey brick: the photographed front on every side, a flat roof behind a parapet, a stair
        /// door with a canopy and the NO BALL GAMES board, satellite dishes, bins.</summary>
        static void Flats(Area a, float L, float D, int storeys, Fac face, int sign)
        {
            var k = a.k; float top = 0.45f + storeys * 3f + 0.6f; var rng = new System.Random((int)(L * 13));
            k.Box(concrete, V(-0.05f, -0.6f, -D - 0.05f), V(L + 0.05f, 0.45f, 0.05f));
            int nc = Mathf.Max(1, Mathf.FloorToInt(L / 11.5f)); var doorXs = DoorsFor(L, nc);
            FrontWithDoors(k, face, L, 0.45f, top, 0.45f, doorXs);
            FacWall(k, face, V(0, 0, -D), V(L, 0, -D), 0.45f, top, 0.45f, Vector3.back);
            FacWall(k, face, V(0, 0, 0), V(0, 0, -D), 0.45f, top, 0.45f, Vector3.left);
            FacWall(k, face, V(L, 0, 0), V(L, 0, -D), 0.45f, top, 0.45f, Vector3.right);
            k.Box(concrete, V(-0.1f, top - 0.2f, -D - 0.1f), V(L + 0.1f, top + 0.1f, 0.1f));
            FlatRoof(k, 0f, L, D, top - 0.6f, top, face.m);
            float cx = doorXs[0];
            foreach (float dx in doorXs) { k.Box(concrete, V(dx - 0.75f, 2.65f, 0f), V(dx + 0.75f, 2.75f, 0.12f)); k.Box(concrete, V(dx - 1.4f, 2.9f, 0f), V(dx + 1.4f, 3.1f, 1.3f)); }
            SignQuad(k, Bld.SignCell(sign), V(cx + 2.6f, 2.1f, 0.03f), Vector3.forward, 2.4f, 0.4f);
            for (int i = 0; i < (int)(L / 3f); i++) if (rng.Next(3) == 0) Dish(a.detail, V(1.5f + i * 3f, 0.45f + (1 + rng.Next(storeys - 1)) * 3f + 2.2f, 0.02f), Vector3.forward);
            for (int i = 0; i < 4; i++) Bin(a.detail, V(cx + 4.5f + i * 0.85f, 0f, 1.6f), rng);
            int si = a.solids.Count; a.Solid(V(0, 0, -D), V(L, top, 0));
            var wins = new List<(Vector3, Vector3, float, float)>();
            FacWins(face, V(0, 0, 0), V(L, 0, 0), 0.45f, top, 0.45f, Vector3.forward, 1.4f, wins); FacWins(face, V(0, 0, -D), V(L, 0, -D), 0.45f, top, 0.45f, Vector3.back, 1.4f, wins);
            FacWins(face, V(0, 0, 0), V(0, 0, -D), 0.45f, top, 0.45f, Vector3.left, 1.4f, wins); FacWins(face, V(L, 0, 0), V(L, 0, -D), 0.45f, top, 0.45f, Vector3.right, 1.4f, wins);
            Closes(a, FlatsName ?? "Flats", L, D, 0.45f, 3f, storeys, nc, wins, sign * 7 + (int)L, si);
        }
        static string FlatsName;

        // ------------------------------------------------------------------------------------------ insides of the facade buildings
        /// <summary>Where the stair doors go along a front of length L split into n closes (area x).</summary>
        static float[] DoorsFor(float L, int n) { var o = new float[n]; float per = L / n; for (int i = 0; i < n; i++) o[i] = L - i * per - (per * 0.5f - 1.15f); return o; }

        /// <summary>The front (z = 0, facing +z) in the photographed face with a doorway cut at each door x (1.1 wide, y0 up to y0 + 2.2).</summary>
        static void FrontWithDoors(MeshKit k, Fac face, float L, float y0, float top, float yBase, float[] doorXs)
        {
            var xs = new List<float>(doorXs); xs.Sort(); float x = 0f; var org = V(L, 0, 0);
            foreach (float d in xs) { FacWall(k, face, V(x, 0, 0), V(d - 0.55f, 0, 0), y0, top, yBase, Vector3.forward, 0.9f, org); FacWall(k, face, V(d - 0.55f, 0, 0), V(d + 0.55f, 0, 0), y0 + 2.2f, top, yBase, Vector3.forward, 0.9f, org); x = d + 0.55f; }
            FacWall(k, face, V(x, 0, 0), V(L, 0, 0), y0, top, yBase, Vector3.forward, 0.9f, org);
            foreach (float d in xs) { k.Box(stone, V(d - 0.62f, y0, 0f), V(d - 0.55f, y0 + 2.25f, 0.06f)); k.Box(stone, V(d + 0.55f, y0, 0f), V(d + 0.62f, y0 + 2.25f, 0.06f)); }
        }

        /// <summary>The windows of a photographed face, as FacWall places their sills: (left foot, right foot, sill y, head y) in the current frame.</summary>
        static void FacWins(Fac f, Vector3 a, Vector3 b, float y0, float y1, float yBase, Vector3 n, float winH, List<(Vector3, Vector3, float, float)> o)
        {
            if (f == null || f.cols == null || f.rows == null) return;
            Vector3 R = -Vector3.Cross(Vector3.up, n); Vector3 left = Vector3.Dot(a, R) <= Vector3.Dot(b, R) ? a : b;
            float Vo(float y) => (y - yBase) / f.h + f.vOff;
            float s0 = Mathf.Min(Vector3.Dot(a - left, R), Vector3.Dot(b - left, R)), s1 = Mathf.Max(Vector3.Dot(a - left, R), Vector3.Dot(b - left, R));
            float hw = f.winW * f.w * 0.5f;
            int t0 = Mathf.FloorToInt(s0 / f.w + f.u0) - 1, t1 = Mathf.CeilToInt(s1 / f.w + f.u0) + 1, r0 = Mathf.FloorToInt(Vo(y0)) - 1, r1 = Mathf.CeilToInt(Vo(y1)) + 1;
            for (int t = t0; t <= t1; t++)
                foreach (float c in f.cols)
                {
                    float s = (c + t - f.u0) * f.w; if (s - hw < s0 + 0.2f || s + hw > s1 - 0.2f) continue;
                    Vector3 p = left + R * s; p.y = 0f;
                    for (int rr = r0; rr <= r1; rr++)
                        foreach (float row in f.rows)
                        {
                            float yb = yBase + (row + rr - f.vOff) * f.h; if (yb < y0 + 0.3f || yb + winH > y1 - 0.3f) continue;
                            o.Add((p - R * hw, p + R * hw, yb, yb + winH));
                        }
                }
        }

        /// <summary>The insides of a block of flats L x D (front at z = 0 facing +z, back at z = -D) in n closes, each its own stair and four flats a floor.</summary>
        static void Closes(Area a, string name, float L, float D, float yFloor, float F, int storeys, int n, List<(Vector3 p0, Vector3 p1, float ya, float yb)> wins, int seed, int solid, bool groundFlats = true)
        {
            float per = L / n, yaw = a.f.rotation.eulerAngles.y + 180f;
            for (int i = 0; i < n; i++)
            {
                float ox = L - i * per; var mine = new List<TowerInterior.WinRec>();
                foreach (var (p0, p1, ya, yb) in wins)
                {
                    char sd; float a0, a1;
                    if (Mathf.Abs(p0.z) < 0.01f && Mathf.Abs(p1.z) < 0.01f) { sd = 'F'; a0 = ox - Mathf.Max(p0.x, p1.x); a1 = ox - Mathf.Min(p0.x, p1.x); }
                    else if (Mathf.Abs(p0.z + D) < 0.01f && Mathf.Abs(p1.z + D) < 0.01f) { sd = 'B'; a0 = ox - Mathf.Max(p0.x, p1.x); a1 = ox - Mathf.Min(p0.x, p1.x); }
                    else if (Mathf.Abs(p0.x - L) < 0.01f) { if (i != 0) continue; sd = 'L'; a0 = Mathf.Min(-p0.z, -p1.z); a1 = Mathf.Max(-p0.z, -p1.z); }
                    else if (Mathf.Abs(p0.x) < 0.01f) { if (i != n - 1) continue; sd = 'R'; a0 = Mathf.Min(-p0.z, -p1.z); a1 = Mathf.Max(-p0.z, -p1.z); }
                    else continue;
                    if ((sd == 'F' || sd == 'B') && (a1 <= 0f || a0 >= per)) continue;
                    if (sd == 'F' || sd == 'B') { a0 = Mathf.Max(a0, 0f); a1 = Mathf.Min(a1, per); }
                    mine.Add(new TowerInterior.WinRec { side = sd, a0 = a0, a1 = a1, y0 = ya - yFloor, y1 = yb - yFloor });
                }
                float cx = per * 0.5f - 1.15f;
                var ti = TowerInterior.Bake(a.root, a.f.MultiplyPoint3x4(V(ox, yFloor, 0f)), yaw, name + (n > 1 ? " " + (char)('A' + i) : ""), per, D, F, F, storeys, cx, 0.15f, cx - 0.55f, cx + 0.55f, 2.2f, 0f, 0f, seed * 11 + i, mine, new List<Collider>(), false, groundFlats);
                ti.SetPlinth(yFloor);
                a.links.Add((ti, solid));
            }
        }

        /// <summary>Northway House: the council's 1960s offices. A recessed glazed ground floor behind concrete columns,
        /// five floors of concrete-and-pebble window bands, a deep entrance canopy with the name, the plant room on the roof.</summary>
        static void Offices(Area a, float L, float D)
        {
            var k = a.k; const float G = 4f, top = 21.5f;
            k.Box(concrete, V(-0.05f, -0.6f, -D - 0.05f), V(L + 0.05f, 0.25f, 0.05f));
            // ground floor: glazing set back 1.6 m behind a row of columns, the soffit over
            const float set = 1.6f;
            k.Quad(glass, V(0.3f, 0.25f, -set), V(L - 0.3f, 0.25f, -set), V(L - 0.3f, G, -set), V(0.3f, G, -set), Vector3.forward);
            for (float x = 0.3f; x <= L - 0.2f; x += 2.5f) k.Box(steel, V(x - 0.04f, 0.25f, -set), V(x + 0.04f, G, -set + 0.06f));
            k.Box(steel, V(0.3f, 2.6f, -set), V(L - 0.3f, 2.68f, -set + 0.06f));
            k.Quad(concrete, V(0f, G, 0f), V(L, G, 0f), V(L, G, -set), V(0f, G, -set), Vector3.down);
            for (float x = 0f; x <= L + 0.01f; x += L / 8f) k.Box(concrete, V(Mathf.Clamp(x - 0.3f, 0f, L - 0.6f), 0.25f, -0.6f), V(Mathf.Clamp(x - 0.3f, 0f, L - 0.6f) + 0.6f, G, 0f));
            FacWall(k, Conc, V(0, 0, -set), V(0, 0, -D), 0.25f, G, 0f, Vector3.left); FacWall(k, Conc, V(L, 0, -set), V(L, 0, -D), 0.25f, G, 0f, Vector3.right);
            FacWall(k, Conc, V(0, 0, -D), V(L, 0, -D), 0.25f, G, 0f, Vector3.back);
            k.Quad(concrete, V(0f, 0.25f, 0f), V(L, 0.25f, 0f), V(L, 0.25f, -set), V(0f, 0.25f, -set), Vector3.up);
            // the floors above, all round
            FacWall(k, F05, V(0, 0, 0), V(L, 0, 0), G, top, G, Vector3.forward); FacWall(k, F05, V(0, 0, -D), V(L, 0, -D), G, top, G, Vector3.back);
            FacWall(k, F05, V(0, 0, 0), V(0, 0, -D), G, top, G, Vector3.left); FacWall(k, F05, V(L, 0, 0), V(L, 0, -D), G, top, G, Vector3.right);
            k.Box(concrete, V(-0.15f, top, -D - 0.15f), V(L + 0.15f, top + 0.8f, 0.15f));
            FlatRoof(k, 0f, L, D, top + 0.2f, top + 0.8f, concrete);
            k.Box(concrete, V(L * 0.6f, top + 0.2f, -D + 3f), V(L * 0.6f + 9f, top + 3.6f, -D + 8.5f));     // plant room
            k.Box(steel, V(L * 0.2f, top + 0.2f, -D + 2f), V(L * 0.2f + 3f, top + 2.2f, -D + 4f));
            // the entrance canopy with the name on its edge, the council's board by the doors, the to-let banner
            k.Box(concrete, V(L * 0.5f - 5f, G - 0.4f, 0f), V(L * 0.5f + 5f, G, 2.6f));
            k.Box(frame, V(L * 0.5f - 3.2f, G, 2.3f), V(L * 0.5f + 3.2f, G + 1.15f, 2.5f));                  // the name, standing on the canopy
            SignQuad(k, Bld.SignCell(2), V(L * 0.5f, G + 0.575f, 2.51f), Vector3.forward, 6.2f, 1.03f);
            SignQuad(k, Bld.SignCell(3), V(L * 0.5f + 3.6f, 1.9f, -set + 0.03f), Vector3.forward, 3.2f, 0.53f);
            SignQuad(k, Bld.SignCell(11), V(L * 0.25f, G + 9f, 0.05f), Vector3.forward, 7f, 1.16f);
            a.Solid(V(0, 0, -D), V(L, top + 0.8f, -set));
            foreach (float x in new[] { 0.3f, L * 0.5f, L - 0.3f }) a.Solid(V(x - 0.3f, 0f, -set), V(x + 0.3f, G, 0f));
        }

        /// <summary>Kestrel Chambers (1889): shops in a stone ground floor, two tall storeys of arched windows over on both
        /// street fronts, a heavy cornice and parapet with the name on a raised panel.</summary>
        static void Chambers(Area a, int[] cells, float unit, float D)
        {
            var k = a.k; const float G = 4.4f, top = 12.9f; float L = cells.Length * unit;
            k.Box(stone, V(-0.05f, -0.6f, -D - 0.05f), V(L + 0.05f, 0.3f, 0.05f));
            for (int i = 0; i < cells.Length; i++) Shopfront(k, shops2, 1024f, cells[i], i * unit, (i + 1) * unit, G);
            // the corner end (local +x: Kestrel Road): stone ground floor with a door to the offices over
            FacWall(k, new Fac { m = stone, w = 2f, h = 2f }, V(L, 0, 0), V(L, 0, -D), 0.3f, G, 0f, Vector3.right);
            k.Quad(glass, V(L + 0.03f, 0.3f, -D * 0.5f + 0.8f), V(L + 0.03f, 0.3f, -D * 0.5f - 0.8f), V(L + 0.03f, 3f, -D * 0.5f - 0.8f), V(L + 0.03f, 3f, -D * 0.5f + 0.8f), Vector3.right);
            k.Box(stone, V(L, 3f, -D * 0.5f - 1.1f), V(L + 0.25f, 3.4f, -D * 0.5f + 1.1f));
            FacWall(k, F08, V(0, 0, 0), V(L, 0, 0), G, top, G, Vector3.forward);
            FacWall(k, F08, V(L, 0, 0), V(L, 0, -D), G, top, G, Vector3.right);
            FacWall(k, Brick, V(0, 0, 0), V(0, 0, -D), 0.3f, top + 1.1f, 0f, Vector3.left);
            FacWall(k, Brick, V(0, 0, -D), V(L, 0, -D), 0.3f, top + 1.1f, 0f, Vector3.back);
            k.Box(stone, V(0f, G - 0.05f, 0f), V(L + 0.3f, G + 0.3f, 0.3f)); k.Box(stone, V(L, G - 0.05f, -D), V(L + 0.3f, G + 0.3f, 0f));
            k.Box(stone, V(-0.05f, top, -D - 0.05f), V(L + 0.45f, top + 0.5f, 0.45f));                         // cornice
            FacWall(k, new Fac { m = stone, w = 2f, h = 2f }, V(0, 0, 0f), V(L, 0, 0f), top + 0.5f, top + 1.1f, 0f, Vector3.forward);
            FacWall(k, new Fac { m = stone, w = 2f, h = 2f }, V(L, 0, 0f), V(L, 0, -D), top + 0.5f, top + 1.1f, 0f, Vector3.right);
            FlatRoof(k, 0f, L, D, top + 0.4f, top + 1.1f, stone);
            k.Box(stone, V(L * 0.5f - 3.4f, top + 1.1f, 0f), V(L * 0.5f + 3.4f, top + 2.4f, 0.25f));
            SignQuad(k, Bld.SignCell(6), V(L * 0.5f, top + 1.75f, 0.26f), Vector3.forward, 6f, 1.0f);
            a.Solid(V(0, 0, -D), V(L, top + 1.1f, 0));
        }

        /// <summary>The multi-storey car park: the photographed decks on every side, the way in with its barrier and
        /// boards, the stair tower, lamp columns and bays on the top deck.</summary>
        static void CarPark(Area a, float L, float D)
        {
            var k = a.k; const float top = 13.1f, ox0 = 6f, ox1 = 12.5f, oy = 2.6f, deep = 7f;
            k.Box(concrete, V(-0.05f, -0.6f, -D - 0.05f), V(L + 0.05f, 0.1f, 0.05f));
            // front with the way in cut out of it (the photograph runs on across the cut)
            var org = V(L, 0, 0);
            FacWall(k, F04, V(0, 0, 0), V(ox0, 0, 0), 0.1f, top, 0f, Vector3.forward, 99f, org);
            FacWall(k, F04, V(ox1, 0, 0), V(L, 0, 0), 0.1f, top, 0f, Vector3.forward, 99f, org);
            FacWall(k, F04, V(ox0, 0, 0), V(ox1, 0, 0), oy, top, 0f, Vector3.forward, 99f, org);
            FacWall(k, F04, V(0, 0, -D), V(L, 0, -D), 0.1f, top, 0f, Vector3.back);
            FacWall(k, F04, V(0, 0, 0), V(0, 0, -D), 0.1f, top, 0f, Vector3.left); FacWall(k, F04, V(L, 0, 0), V(L, 0, -D), 0.1f, top, 0f, Vector3.right);
            // the way in: a dark concrete tunnel, the barrier and the pay machine
            k.Quad(asphalt, V(ox0, 0.06f, 0f), V(ox1, 0.06f, 0f), V(ox1, 0.06f, -deep), V(ox0, 0.06f, -deep), Vector3.up);
            k.Quad(concrete, V(ox0, oy, 0f), V(ox1, oy, 0f), V(ox1, oy, -deep), V(ox0, oy, -deep), Vector3.down);
            k.Quad(conc, V(ox0, 0f, 0f), V(ox0, oy, 0f), V(ox0, oy, -deep), V(ox0, 0f, -deep), Vector3.right);
            k.Quad(conc, V(ox1, 0f, 0f), V(ox1, oy, 0f), V(ox1, oy, -deep), V(ox1, 0f, -deep), Vector3.left);
            k.Quad(black, V(ox0, 0f, -deep), V(ox1, 0f, -deep), V(ox1, oy, -deep), V(ox0, oy, -deep), Vector3.forward);
            a.detail.Box(yellow, V(ox1 - 0.5f, 0f, -1.4f), V(ox1 - 0.2f, 1.05f, -1.1f));
            for (int i = 0; i < 6; i++) a.detail.Box(i % 2 == 0 ? white : red, V(ox0 + 0.4f + i * 0.95f, 0.92f, -1.29f), V(ox0 + 1.35f + i * 0.95f, 1.02f, -1.21f));
            a.detail.Box(steel, V(ox1 + 0.4f, 0f, 0.2f), V(ox1 + 1.0f, 1.6f, 0.6f)); a.detail.Box(glass, V(ox1 + 0.45f, 1.0f, 0.61f), V(ox1 + 0.95f, 1.4f, 0.62f));
            SignQuad(k, Bld.SignCell(5), V((ox0 + ox1) * 0.5f, oy + 0.45f, 0.03f), Vector3.forward, 4.2f, 0.7f);
            SignQuad(k, Bld.SignCell(4), V(L * 0.55f, 10.3f, 0.04f), Vector3.forward, 11f, 1.83f);
            for (float x = ox0 + 0.15f; x < ox1; x += (ox1 - ox0) - 0.3f) a.Solid(V(x - 0.15f, 0f, -deep), V(x + 0.15f, oy, 0f));
            // the stair tower on the far corner, a storey proud of the roof
            float sx0 = L - 5.5f;
            k.Box(conc, V(sx0, 0.1f, -5.2f), V(L + 0.4f, top + 2.6f, 0.4f));
            k.Quad(glass, V(sx0 + 2.2f, 1f, 0.41f), V(sx0 + 3.0f, 1f, 0.41f), V(sx0 + 3.0f, top + 2f, 0.41f), V(sx0 + 2.2f, top + 2f, 0.41f), Vector3.forward);
            k.Box(concrete, V(sx0 - 0.15f, top + 2.6f, -5.35f), V(L + 0.55f, top + 2.85f, 0.55f));
            SignQuad(k, Bld.SignCell(15), V(sx0 + 4.4f, 3f, 0.42f), Vector3.forward, 1.8f, 0.3f);
            SignQuad(k, Bld.SignCell(12), V(sx0 + 1.1f, 2.7f, 0.42f), Vector3.forward, 1.2f, 0.2f);
            k.QuadUV(doors, V(sx0 + 1.6f, 0.1f, 0.42f), V(sx0 + 0.6f, 0.1f, 0.42f), V(sx0 + 0.6f, 2.2f, 0.42f), V(sx0 + 1.6f, 2.2f, 0.42f), Vector3.forward,
                new Vector2(0.758f, 0), new Vector2(0.992f, 0), new Vector2(0.992f, 1), new Vector2(0.758f, 1));
            // the top deck: tarmac, bays, a parapet, lamps
            const float deck = 12f;
            k.Quad(asphalt, V(0.3f, deck, -0.3f), V(L - 0.3f, deck, -0.3f), V(L - 0.3f, deck, -D + 0.3f), V(0.3f, deck, -D + 0.3f), Vector3.up);
            foreach (var (x0, z0, x1, z1, nn) in new[] { (0.3f, -0.3f, L - 0.3f, -0.3f, Vector3.back), (0.3f, -D + 0.3f, L - 0.3f, -D + 0.3f, Vector3.forward), (0.3f, -0.3f, 0.3f, -D + 0.3f, Vector3.right), (L - 0.3f, -0.3f, L - 0.3f, -D + 0.3f, Vector3.left) })
                k.Quad(concrete, V(x0, deck, z0), V(x1, deck, z1), V(x1, top, z1), V(x0, top, z0), nn);
            k.Box(concrete, V(-0.1f, top, -D - 0.1f), V(L + 0.1f, top + 0.15f, 0.1f));
            for (int i = 0; i < 14; i++) { float x = 1.5f + i * 2.5f; a.detail.Box(white, V(x, deck, -5.3f), V(x + 0.1f, deck + 0.012f, -0.6f), 4); a.detail.Box(white, V(x, deck, -D + 0.6f), V(x + 0.1f, deck + 0.012f, -D + 5.3f), 4); }
            foreach (float x in new[] { L * 0.25f, L * 0.75f }) foreach (float z in new[] { -D * 0.33f, -D * 0.67f })
                { k.Cyl(lampCol, V(x, deck, z), 0.08f, 6f, 6); a.detail.Box(glow, V(x - 0.35f, deck + 5.85f, z - 0.15f), V(x + 0.35f, deck + 6f, z + 0.15f)); }
            a.Solid(V(0, 0, -D), V(ox0, top, 0)); a.Solid(V(ox1, 0, -D), V(L, top, 0)); a.Solid(V(ox0, oy, -D), V(ox1, top, 0)); a.Solid(V(ox0, 0, -D), V(ox1, oy, -deep));
        }

        /// <summary>A parade with the photographed brick over its shopfronts (Thornbury Parade): shops between stone
        /// pilasters, two storeys over, a cornice and parapet, the corner end in the same brick.</summary>
        static void FacadeParade(Area a, int[] cells, float unit, float D, Fac upper)
        {
            var k = a.k; const float G = 3.9f; float L = cells.Length * unit, top = G + 6f + 0.9f;
            k.Box(stone, V(-0.05f, -0.6f, -D - 0.05f), V(L + 0.05f, 0.15f, 0.05f));
            for (int i = 0; i < cells.Length; i++) Shopfront(k, shops2, 1024f, cells[i], i * unit, (i + 1) * unit, G);
            FacWall(k, upper, V(0, 0, 0), V(L, 0, 0), G, top, G, Vector3.forward);
            FacWall(k, upper, V(0, 0, 0), V(0, 0, -D), G, top, G, Vector3.left);
            FacWall(k, Brick, V(0, 0, 0), V(0, 0, -D), 0.15f, G, 0f, Vector3.left);
            FacWall(k, Brick, V(L, 0, 0), V(L, 0, -D), 0.15f, top, 0f, Vector3.right);
            FacWall(k, Brick, V(0, 0, -D), V(L, 0, -D), 0.15f, top, 0f, Vector3.back);
            for (int i = 0; i < cells.Length; i++) { BackWindow(k, i * unit + unit * 0.5f, G + 1f, -D); BackWindow(k, i * unit + unit * 0.5f, G + 4f, -D); }
            k.Box(stone, V(-0.25f, top - 1.1f, -D), V(L, top - 0.8f, 0.25f));                                 // cornice
            k.Box(stone, V(-0.12f, top, -D - 0.12f), V(L + 0.12f, top + 0.15f, 0.12f));
            FlatRoof(k, 0f, L, D, top - 0.9f, top, brick);
            a.Solid(V(0, 0, -D), V(L, top, 0));
        }

        /// <summary>Wend Gospel Hall: lancet windows down both sides, the gable to the road with a double door, the board, a steep slate roof.</summary>
        static void Hall(Area a, float W, float D)
        {
            var k = a.k; const float h = 7.5f;
            k.Box(stone, V(-0.05f, -0.6f, -D - 0.05f), V(W + 0.05f, 0.45f, 0.05f));
            FacWall(k, B02, V(0, 0, 0), V(0, 0, -D), 0.45f, h, 0f, Vector3.left); FacWall(k, B02, V(W, 0, 0), V(W, 0, -D), 0.45f, h, 0f, Vector3.right);
            FacWall(k, B02, V(0, 0, 0), V(W, 0, 0), 0.45f, h, 0f, Vector3.forward); FacWall(k, Brick, V(0, 0, -D), V(W, 0, -D), 0.45f, h, 0f, Vector3.back);
            k.Box(stone, V(-0.1f, h - 0.25f, -D - 0.1f), V(W + 0.1f, h, 0.1f));
            k.Push(Matrix4x4.TRS(V(0f, 0f, -D), Quaternion.Euler(0, -90f, 0), Vector3.one)); float ridge = Pitched(k, 0f, D, W, h, 48f, brick); k.Pop();
            // a round window in the gable, the door, the board over it
            k.Push(Matrix4x4.TRS(V(W * 0.5f, h + 2.2f, -0.04f), Quaternion.Euler(90f, 0f, 0f), Vector3.one));
            k.Cyl(stone, Vector3.zero, 0.95f, 0.12f, 16); k.Cyl(glass, V(0f, 0.01f, 0f), 0.72f, 0.12f, 16); k.Pop();
            k.Box(stone, V(W * 0.5f - 1.5f, 0.45f, 0f), V(W * 0.5f + 1.5f, 3.9f, 0.22f), 32);
            k.Box(wood, V(W * 0.5f - 1.05f, 0.45f, 0.18f), V(W * 0.5f + 1.05f, 3.4f, 0.26f));
            k.Box(black, V(W * 0.5f - 0.02f, 0.45f, 0.26f), V(W * 0.5f + 0.02f, 3.4f, 0.28f));
            SignQuad(k, Bld.SignCell(7), V(W * 0.5f, 4.6f, 0.04f), Vector3.forward, 5.4f, 0.9f);
            a.Solid(V(0, 0, -D), V(W, h, 0)); a.Solid(V(0, h, -D), V(W, ridge - 1f, 0));
        }

        // ------------------------------------------------------------------------------------------ pieces
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        /// <summary>A wall from a to b (on the ground) from y0 to y1, facing n, wearing a photographed front: one repeat per
        /// f.w x f.h metres from the viewer's left end (or from uvFrom), rows counted from yBase. Stone sills under its windows above sillFrom.</summary>
        static void FacWall(MeshKit k, Fac f, Vector3 a, Vector3 b, float y0, float y1, float yBase, Vector3 n, float sillFrom = 0.9f, Vector3? uvFrom = null)
        {
            if (f == null || f.m == null || y1 <= y0) return;
            Vector3 R = -Vector3.Cross(Vector3.up, n), up = Vector3.up;
            Vector3 left = uvFrom ?? (Vector3.Dot(a, R) <= Vector3.Dot(b, R) ? a : b);
            float Uo(Vector3 p) => Vector3.Dot(p - left, R) / f.w + f.u0;
            float Vo(float y) => (y - yBase) / f.h + f.vOff;
            k.QuadUV(f.m, a + up * y0, b + up * y0, b + up * y1, a + up * y1, n,
                new Vector2(Uo(a), Vo(y0)), new Vector2(Uo(b), Vo(y0)), new Vector2(Uo(b), Vo(y1)), new Vector2(Uo(a), Vo(y1)));
            if (f.cols == null || f.rows == null || stone == null || sillFrom > 90f) return;
            float s0 = Mathf.Min(Vector3.Dot(a - left, R), Vector3.Dot(b - left, R)), s1 = Mathf.Max(Vector3.Dot(a - left, R), Vector3.Dot(b - left, R));
            float hw = f.winW * f.w * 0.5f + 0.06f;
            int t0 = Mathf.FloorToInt(s0 / f.w + f.u0) - 1, t1 = Mathf.CeilToInt(s1 / f.w + f.u0) + 1;
            int r0 = Mathf.FloorToInt(Vo(y0)) - 1, r1 = Mathf.CeilToInt(Vo(y1)) + 1;
            for (int t = t0; t <= t1; t++)
                foreach (float c in f.cols)
                {
                    float s = (c + t - f.u0) * f.w; if (s - hw < s0 + 0.2f || s + hw > s1 - 0.2f) continue;
                    Vector3 p = left + R * s; p.y = 0f;
                    for (int rr = r0; rr <= r1; rr++)
                        foreach (float row in f.rows)
                        {
                            float yb = yBase + (row + rr - f.vOff) * f.h; if (yb < Mathf.Max(y0 + 0.3f, sillFrom) || yb > y1 - 1.2f) continue;
                            Vector3 c0 = p - R * hw + up * (yb - 0.09f), c1 = p + R * hw + up * (yb + 0.01f) + n * 0.08f;
                            k.Box(stone, Vector3.Min(c0, c1), Vector3.Max(c0, c1));
                        }
                }
        }

        /// <summary>Four walls of a block, x0..x1 along the front, d deep.</summary>
        static void FacBox(MeshKit k, float x0, float x1, float d, float y0, float top, float yBase, Fac front, Fac left, Fac back, Fac right)
        {
            FacWall(k, front, V(x0, 0, 0), V(x1, 0, 0), y0, top, yBase, Vector3.forward);
            FacWall(k, back, V(x0, 0, -d), V(x1, 0, -d), y0, top, yBase, Vector3.back);
            FacWall(k, left, V(x0, 0, 0), V(x0, 0, -d), y0, top, yBase, Vector3.left);
            FacWall(k, right, V(x1, 0, 0), V(x1, 0, -d), y0, top, yBase, Vector3.right);
        }

        /// <summary>A flat roof: felt at deck height inside a parapet up to top (its inner faces in wallInner), stone coping.</summary>
        static void FlatRoof(MeshKit k, float x0, float x1, float d, float deck, float top, Material wallInner)
        {
            const float t = 0.3f;
            k.Quad(felt, V(x0 + t, deck, -t), V(x1 - t, deck, -t), V(x1 - t, deck, -d + t), V(x0 + t, deck, -d + t), Vector3.up);
            var m = wallInner ?? concrete;
            k.Quad(m, V(x0 + t, deck, -t), V(x1 - t, deck, -t), V(x1 - t, top, -t), V(x0 + t, top, -t), Vector3.back);
            k.Quad(m, V(x0 + t, deck, -d + t), V(x1 - t, deck, -d + t), V(x1 - t, top, -d + t), V(x0 + t, top, -d + t), Vector3.forward);
            k.Quad(m, V(x0 + t, deck, -t), V(x0 + t, deck, -d + t), V(x0 + t, top, -d + t), V(x0 + t, top, -t), Vector3.right);
            k.Quad(m, V(x1 - t, deck, -t), V(x1 - t, deck, -d + t), V(x1 - t, top, -d + t), V(x1 - t, top, -t), Vector3.left);
            k.Quad(stone, V(x0, top, 0f), V(x1, top, 0f), V(x1, top, -t), V(x0, top, -t), Vector3.up);
            k.Quad(stone, V(x0, top, -d + t), V(x1, top, -d + t), V(x1, top, -d), V(x0, top, -d), Vector3.up);
            k.Quad(stone, V(x0, top, -t), V(x0 + t, top, -t), V(x0 + t, top, -d + t), V(x0, top, -d + t), Vector3.up);
            k.Quad(stone, V(x1 - t, top, -t), V(x1, top, -t), V(x1, top, -d + t), V(x1 - t, top, -d + t), Vector3.up);
        }

        /// <summary>A pitched slate roof over x0..x1 (ridge along x), d deep, eaves overhanging with gutters, ridge tiles, gable triangles. Returns the ridge height.</summary>
        static float Pitched(MeshKit k, float x0, float x1, float d, float eave, float pitchDeg, Material gable)
        {
            float t = Mathf.Tan(pitchDeg * Mathf.Deg2Rad), zr = -d * 0.5f, ridge = eave + d * 0.5f * t, oh = 0.3f, ty = MeshKit.TileOf(slate);
            foreach (int s in new[] { 1, -1 })
            {
                float ze = s > 0 ? oh : -d - oh, ye = eave - oh * t;
                float slope = Mathf.Sqrt((ze - zr) * (ze - zr) + (ridge - ye) * (ridge - ye));
                k.QuadUV(slate, V(x0 - 0.15f, ye, ze), V(x1 + 0.15f, ye, ze), V(x1 + 0.15f, ridge, zr), V(x0 - 0.15f, ridge, zr), new Vector3(0, 1, s * t).normalized,
                    new Vector2(x0 / ty, 0), new Vector2(x1 / ty, 0), new Vector2(x1 / ty, slope / ty), new Vector2(x0 / ty, slope / ty));
                k.Box(black, V(x0 - 0.15f, ye - 0.16f, s > 0 ? ze - 0.02f : ze - 0.12f), V(x1 + 0.15f, ye - 0.04f, s > 0 ? ze + 0.12f : ze + 0.02f));
                k.Quad(black, V(x0, ye - 0.02f, s > 0 ? 0f : -d), V(x1, ye - 0.02f, s > 0 ? 0f : -d), V(x1, ye - 0.02f, ze), V(x0, ye - 0.02f, ze), Vector3.down);
            }
            k.Box(TownMats.roofTile ?? slate, V(x0 - 0.15f, ridge - 0.05f, zr - 0.14f), V(x1 + 0.15f, ridge + 0.12f, zr + 0.14f));
            k.TriN(gable, V(x0, eave, 0f), V(x0, eave, -d), V(x0, ridge, zr), Vector3.left);
            k.TriN(gable, V(x1, eave, 0f), V(x1, eave, -d), V(x1, ridge, zr), Vector3.right);
            // the down pipes at each end of the front
            foreach (float px in new[] { x0 + 0.12f, x1 - 0.12f }) k.Cyl(black, V(px, 0.3f, 0.1f), 0.05f, eave - 0.45f, 6, false);
            return ridge;
        }

        static void Chimney(MeshKit k, float x, float ridge, float zc, Material m)
        {
            k.Box(m, V(x - 0.5f, ridge - 1.6f, zc - 0.38f), V(x + 0.5f, ridge + 1.2f, zc + 0.38f), 4);
            k.Box(stone, V(x - 0.58f, ridge + 1.2f, zc - 0.46f), V(x + 0.58f, ridge + 1.36f, zc + 0.46f));
            foreach (float dx in new[] { -0.24f, 0f, 0.24f }) k.Cyl(TownMats.roofTile ?? m, V(x + dx, ridge + 1.36f, zc), 0.1f, 0.45f, 8, true, 0.085f);
        }

        /// <summary>A front door on the face at z = 0, centred at x: one of the four doors, a fanlight, a stone surround, lintel and step.</summary>
        static void Door(MeshKit k, float x, int style)
        {
            float u0 = style * 0.25f + 0.008f, u1 = u0 + 0.234f;
            k.QuadUV(doors, V(x + 0.5f, 0.42f, 0.03f), V(x - 0.5f, 0.42f, 0.03f), V(x - 0.5f, 2.45f, 0.03f), V(x + 0.5f, 2.45f, 0.03f), Vector3.forward,
                new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, 1), new Vector2(u0, 1));
            k.Quad(glass, V(x - 0.5f, 2.5f, 0.03f), V(x + 0.5f, 2.5f, 0.03f), V(x + 0.5f, 2.85f, 0.03f), V(x - 0.5f, 2.85f, 0.03f), Vector3.forward);
            k.Box(white, V(x - 0.5f, 2.45f, 0.02f), V(x + 0.5f, 2.5f, 0.06f));
            k.Box(stone, V(x - 0.68f, 0.42f, 0f), V(x - 0.5f, 2.85f, 0.1f)); k.Box(stone, V(x + 0.5f, 0.42f, 0f), V(x + 0.68f, 2.85f, 0.1f));
            k.Box(stone, V(x - 0.75f, 2.85f, 0f), V(x + 0.75f, 3.08f, 0.13f));
            k.Box(stone, V(x - 0.65f, 0.0f, 0f), V(x + 0.65f, 0.42f, 0.38f));
        }

        static void BackWindow(MeshKit k, float x, float y, float z)
        {
            k.Quad(glass, V(x + 0.55f, y, z - 0.02f), V(x - 0.55f, y, z - 0.02f), V(x - 0.55f, y + 1.3f, z - 0.02f), V(x + 0.55f, y + 1.3f, z - 0.02f), Vector3.back);
            k.Box(white, V(x - 0.6f, y - 0.05f, z - 0.06f), V(x + 0.6f, y + 0.05f, z)); k.Box(white, V(x - 0.6f, y + 1.25f, z - 0.06f), V(x + 0.6f, y + 1.35f, z));
            k.Box(white, V(x - 0.6f, y, z - 0.06f), V(x - 0.52f, y + 1.3f, z)); k.Box(white, V(x + 0.52f, y, z - 0.06f), V(x + 0.6f, y + 1.3f, z));
            k.Box(white, V(x - 0.03f, y, z - 0.05f), V(x + 0.03f, y + 1.3f, z));
            k.Box(stone, V(x - 0.68f, y - 0.14f, z - 0.1f), V(x + 0.68f, y - 0.05f, z));
        }

        /// <summary>A shopfront from an atlas of 512 x 340 cells (atlasH pixels tall) between pilasters, fascia and cornice, on the face at z = 0.</summary>
        static void Shopfront(MeshKit k, Material atlas, float atlasH, int cell, float x0, float x1, float top)
        {
            const float pil = 0.32f, fasciaH = 0.62f; float fy0 = top - fasciaH - 0.15f, fy1 = top - 0.15f, a = x0 + pil, b = x1 - pil;
            int c = cell % 4, r = cell / 4; float py = r * 340f;
            var fas = new Rect(c * 0.25f + 0.002f, 1f - (py + 72f) / atlasH, 0.246f, 72f / atlasH);
            var fr = new Rect(c * 0.25f + 0.002f, 1f - (py + 340f) / atlasH, 0.246f, 268f / atlasH);
            void Face(float fx0, float fx1, float y0, float y1, float z, Rect uv) => k.QuadUV(atlas ?? frame, V(fx0, y0, z), V(fx1, y0, z), V(fx1, y1, z), V(fx0, y1, z), Vector3.forward,
                new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMin, uv.yMax), new Vector2(uv.xMax, uv.yMax));
            Face(a, b, 0f, fy0, -0.22f, fr);
            k.Quad(frame, V(a, fy0, 0f), V(b, fy0, 0f), V(b, fy0, -0.22f), V(a, fy0, -0.22f), Vector3.down);
            k.Quad(frame, V(a, 0f, 0f), V(a, fy0, 0f), V(a, fy0, -0.22f), V(a, 0f, -0.22f), Vector3.right);
            k.Quad(frame, V(b, 0f, 0f), V(b, fy0, 0f), V(b, fy0, -0.22f), V(b, 0f, -0.22f), Vector3.left);
            k.Box(frame, V(a - 0.05f, fy0, 0f), V(b + 0.05f, fy1, 0.14f), 16);
            Face(a, b, fy0 + 0.03f, fy1 - 0.03f, 0.141f, fas);
            k.Box(stone, V(x0, fy1, 0f), V(x1, top, 0.26f), 16);
            foreach (float px in new[] { x0, b }) k.Box(stone, V(px, 0f, 0f), V(px + pil, fy1, 0.12f), 16);
        }

        /// <summary>A board from street_signs.png on a wall facing n, centred at c (already just off the wall), w x h.</summary>
        static void SignQuad(MeshKit k, Rect uv, Vector3 c, Vector3 n, float w, float h)
        {
            if (signs == null) return;
            Vector3 R = -Vector3.Cross(Vector3.up, n).normalized * (w * 0.5f), U = Vector3.up * (h * 0.5f);
            k.QuadUV(signs, c - R - U, c + R - U, c + R + U, c - R + U, n, new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax));
        }

        /// <summary>A sign on a post, the board facing n.</summary>
        static void SignPost(Area a, Vector3 p, Vector3 n, int cell)
        {
            a.detail.Cyl(lampCol, p, 0.04f, 2.6f, 6);
            Vector3 R = Vector3.Cross(Vector3.up, n).normalized;
            Vector3 mn = p + V(0, 2.18f, 0) - R * 0.62f - n * 0.03f, mx = p + V(0, 2.42f, 0) + R * 0.62f + n * 0.03f;
            a.detail.Box(white, Vector3.Min(mn, mx), Vector3.Max(mn, mx));
            SignQuad(a.detail, Bld.SignCell(cell), p + V(0, 2.3f, 0) + n * 0.05f, n, 1.2f, 0.2f);
            SignQuad(a.detail, Bld.SignCell(cell), p + V(0, 2.3f, 0) - n * 0.05f, -n, 1.2f, 0.2f);
        }

        /// <summary>A street lamp: column, arm and lantern facing the road (dir).</summary>
        static void Lamp(Area a, Vector3 p, Vector3 dir)
        {
            a.k.Cyl(lampCol, p, 0.09f, 6f, 8, true, 0.06f);
            a.k.Box(lampCol, Vector3.Min(p + V(0, 5.9f, 0), p + V(0, 6f, 0) + dir * 1.1f) - V(0.04f, 0, 0.04f), Vector3.Max(p + V(0, 5.9f, 0), p + V(0, 6f, 0) + dir * 1.1f) + V(0.04f, 0, 0.04f));
            var h = p + V(0, 5.82f, 0) + dir * 1.1f;
            a.detail.Box(glow, h - V(0.22f, 0.06f, 0.22f), h + V(0.22f, 0.04f, 0.22f));
            a.solids.Add((Matrix4x4.identity, p + V(0, 3f, 0), V(0.2f, 6f, 0.2f)));
        }

        /// <summary>A wheelie bin (council grey, green or blue).</summary>
        static void Bin(MeshKit k, Vector3 p, System.Random rng)
        {
            var m = rng.Next(3) switch { 0 => black, 1 => TownMats.courtGreen ?? black, _ => TownMats.nhsBlue ?? black };
            k.Box(m, p + V(-0.29f, 0.05f, -0.36f), p + V(0.29f, 1.0f, 0.36f));
            k.Box(m, p + V(-0.31f, 1.0f, -0.4f), p + V(0.31f, 1.06f, 0.4f));
            k.Box(black, p + V(-0.3f, 0f, -0.42f), p + V(0.3f, 0.2f, -0.3f));
        }

        static void Dish(MeshKit k, Vector3 p, Vector3 n)
        {
            Vector3 e = p + n * 0.25f;
            k.Box(steel, Vector3.Min(p, e) - V(0.03f, 0.03f, 0.03f), Vector3.Max(p, e) + V(0.03f, 0.03f, 0.03f));
            k.Push(Matrix4x4.TRS(e, Quaternion.FromToRotation(Vector3.up, n) * Quaternion.Euler(-15f, 0f, 0f), Vector3.one));
            k.Cyl(white, Vector3.zero, 0.3f, 0.06f, 10, true, 0.26f); k.Pop();
        }
    }
}
