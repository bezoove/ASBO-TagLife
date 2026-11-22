using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// Wend Precinct, the 1960s shopping precinct and community hub between Northway and Abbots Wend: four parades of
    /// shops with maisonettes over them round a paved square, concrete canopies along the shop fronts, a bandstand in the
    /// middle (the community stage), raised planters, benches, heritage lamps, a clock, a phone kiosk, a cash machine;
    /// the Community Hub at the west end (drop-in, advice, food bank) and public toilets at the east end. Ways in from
    /// the pelican crossing on Northway and from the car park behind. Most shops are fronts with their window displays;
    /// six you can walk into (the newsagent, the chippy, the café, the bookies, the pound shop and the hub).
    /// Every shop name is made up (Art/Town/precinct_shops.png).
    /// </summary>
    public static class Precinct
    {
        public const string RootName = "Precinct";
        static CouncilEstate.Mats E;
        public const float ShopH = 3.4f, FasciaH = 0.75f, UpperH = 2.9f;
        /// <summary>The bandstand: the middle of the square, where things happen.</summary>
        public static readonly Vector3 Bandstand = new Vector3(40f, 0, 93f);

        // the parades: (name, start x, end x, front z, faces +z?, shop cells in order; negative = a walk-in shop of that cell)
        struct Parade { public string name; public float x0, x1, zf; public bool facesNorth; public int[] shops; }
        static readonly Parade[] Parades =
        {
            new Parade { name = "North Parade West", x0 = -28f, x1 = 14f, zf = 104f, facesNorth = false, shops = new[] { 3, 17, 6, -22, 9, 20, 1 } },
            new Parade { name = "North Parade East", x0 = 26f, x1 = 108f, zf = 104f, facesNorth = false, shops = new[] { -15, 2, 7, 21, 11, 4, 16, 8, 20, 18, 23, 13, 0 } },
            new Parade { name = "South Parade West", x0 = -28f, x1 = 40f, zf = 82f, facesNorth = true, shops = new[] { 12, -5, 19, 21, 14, -10, 6, 20, -3, 17, 9 } },
            new Parade { name = "South Parade East", x0 = 56f, x1 = 108f, zf = 82f, facesNorth = true, shops = new[] { 1, 18, 20, 22, 11, 21, 7, 16 } },
        };

        public static Transform Build(Transform parent)
        {
            if (!TownMats.Load()) return null; E = CouncilEstate.M;
            var root = new GameObject(RootName).transform; root.SetParent(parent, false);
            foreach (var p in Parades) Build(root, p);
            Hub(root); Toilets(root); Square(root);
            Debug.Log("[ASBO] precinct: four parades, the square, the bandstand, the hub");
            return root;
        }

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static Rect Fascia(int shop) { int c = shop % 4, r = shop / 4; float y0 = r * 340f, y1 = y0 + 72f; return new Rect(c * 0.25f + 0.002f, 1f - y1 / 2048f, 0.246f, 72f / 2048f); }
        static Rect Front(int shop) { int c = shop % 4, r = shop / 4; float y0 = r * 340f + 72f, y1 = r * 340f + 340f; return new Rect(c * 0.25f + 0.002f, 1f - y1 / 2048f, 0.246f, (y1 - y0) / 2048f); }
        static Rect TowerCell(int c) => new Rect((c % 4) * 0.25f + 0.004f, 1f - (c / 4 + 1) * 0.25f + 0.004f, 0.242f, 0.242f);

        /// <summary>A quad on a wall facing n, from (s0, y0) to (s1, y1) along it, mapped to an atlas rect (left of the picture on the left as you look at it).</summary>
        static void Face(MeshKit k, Material m, Vector3 o, Vector3 along, Vector3 n, float s0, float s1, float y0, float y1, Rect uv, float off = 0.02f)
        {
            Vector3 a = o + along * s0 + n * off, b = o + along * s1 + n * off;
            // seen from outside (looking along -n), the viewer's right is -cross(up, n): if 'along' runs the other way, mirror the picture
            bool flip = Vector3.Dot(Vector3.Cross(Vector3.up, n), along) > 0f;
            float uL = flip ? uv.xMax : uv.xMin, uR = flip ? uv.xMin : uv.xMax;
            k.QuadUV(m, a + Vector3.up * y0, b + Vector3.up * y0, b + Vector3.up * y1, a + Vector3.up * y1, n, new Vector2(uL, uv.yMin), new Vector2(uR, uv.yMin), new Vector2(uR, uv.yMax), new Vector2(uL, uv.yMax));
        }

        static void Build(Transform root, Parade p)
        {
            var g = new GameObject(p.name).transform; g.SetParent(root, false);
            var k = new MeshKit(); var rng = new System.Random(p.name.GetHashCode());
            Vector3 n = p.facesNorth ? Vector3.forward : Vector3.back; float depth = 11f; float zb = p.facesNorth ? p.zf - depth : p.zf + depth; // back wall z
            Vector3 along = Vector3.right; Vector3 o = V(0, 0, p.zf);
            float top = ShopH + FasciaH + UpperH, w = (p.x1 - p.x0) / p.shops.Length;
            var shop = TownMats.shopfronts2 != null ? TownMats.shopfronts2 : E.windows; var brick = TownMats.brownBrick; var panel = TownMats.panel;
            for (int i = 0; i < p.shops.Length; i++)
            {
                float x0 = p.x0 + i * w, x1 = x0 + w; int cell = Mathf.Abs(p.shops[i]); bool walkIn = p.shops[i] < 0;
                // the fascia board
                Face(k, shop, o, along, n, x0 + 0.15f, x1 - 0.15f, ShopH, ShopH + FasciaH, Fascia(cell), 0.12f);
                k.Box(E.black, V(x0 + 0.1f, ShopH - 0.05f, Mathf.Min(p.zf, p.zf + n.z * 0.1f)), V(x1 - 0.1f, ShopH + FasciaH + 0.05f, Mathf.Max(p.zf, p.zf + n.z * 0.1f)));
                if (!walkIn) Face(k, shop, o, along, n, x0 + 0.15f, x1 - 0.15f, 0f, ShopH, Front(cell), 0.02f);
                // brick piers between the units
                k.Box(brick, V(x0 - 0.15f, 0f, Mathf.Min(p.zf, p.zf + n.z * 0.15f)), V(x0 + 0.15f, ShopH + FasciaH, Mathf.Max(p.zf, p.zf + n.z * 0.15f)));
                // upstairs: the maisonettes' windows in panel, a concrete band
                for (int wi = 0; wi < 2; wi++)
                {
                    float wx0 = x0 + w * (0.12f + wi * 0.5f), wx1 = wx0 + w * 0.34f;
                    int c = rng.NextDouble() < 0.7 ? new[] { 0, 1, 2, 3, 5, 7, 14 }[rng.Next(7)] : new[] { 4, 6, 15, 12 }[rng.Next(4)];
                    Face(k, TownMats.towerWin, o, along, n, wx0, wx1, ShopH + FasciaH + 0.8f, ShopH + FasciaH + 2.2f, TowerCell(c), 0.03f);
                }
                if (walkIn) Shop(g, p, x0, x1, cell);
            }
            k.Box(brick, V(p.x1 - 0.15f, 0f, Mathf.Min(p.zf, p.zf + n.z * 0.15f)), V(p.x1 + 0.15f, ShopH + FasciaH, Mathf.Max(p.zf, p.zf + n.z * 0.15f)));
            // the walls: front upstairs, back, ends; flat roof with a parapet
            float zMin = Mathf.Min(p.zf, zb), zMax = Mathf.Max(p.zf, zb);
            k.Quad(panel, V(p.x0, ShopH + FasciaH, p.zf), V(p.x1, ShopH + FasciaH, p.zf), V(p.x1, top, p.zf), V(p.x0, top, p.zf), n);
            k.Quad(brick, V(p.x0, 0, zb), V(p.x1, 0, zb), V(p.x1, top, zb), V(p.x0, top, zb), -n);
            k.Quad(brick, V(p.x0, 0, zMin), V(p.x0, 0, zMax), V(p.x0, top, zMax), V(p.x0, top, zMin), Vector3.left);
            k.Quad(brick, V(p.x1, 0, zMin), V(p.x1, 0, zMax), V(p.x1, top, zMax), V(p.x1, top, zMin), Vector3.right);
            k.Quad(E.felt, V(p.x0, top, zMin), V(p.x1, top, zMin), V(p.x1, top, zMax), V(p.x0, top, zMax), Vector3.up);
            foreach (float z in new[] { zMin, zMax - 0.3f }) k.Box(panel, V(p.x0, top, z), V(p.x1, top + 0.5f, z + 0.3f));
            k.Box(panel, V(p.x0, ShopH + FasciaH - 0.1f, Mathf.Min(p.zf, p.zf + n.z * 0.2f)), V(p.x1, ShopH + FasciaH + 0.1f, Mathf.Max(p.zf, p.zf + n.z * 0.2f)));
            // back windows upstairs and back doors with their bins
            for (float x = p.x0 + 1.5f; x < p.x1 - 2f; x += w)
            {
                Face(k, TownMats.towerWin, V(0, 0, zb), along, -n, x, x + 1.6f, ShopH + FasciaH + 0.8f, ShopH + FasciaH + 2.2f, TowerCell(rng.Next(16)), 0.03f);
                k.Box(TownMats.steel, V(x + 2f, 0f, Mathf.Min(zb, zb - n.z * 0.08f)), V(x + 3f, 2.1f, Mathf.Max(zb, zb - n.z * 0.08f)));
                if (rng.NextDouble() < 0.5) { Vector3 bc = V(x + 4f, 0, zb - n.z * 0.8f); k.Box(E.black, bc - V(0.35f, 0, 0.35f), bc + V(0.35f, 1.05f, 0.35f)); }
            }
            // the canopy along the fronts, on slim columns
            float cz0 = p.zf, cz1 = p.zf + n.z * 3.2f;
            k.Box(panel, V(p.x0 - 0.3f, ShopH - 0.05f, Mathf.Min(cz0, cz1)), V(p.x1 + 0.3f, ShopH + 0.25f, Mathf.Max(cz0, cz1)));
            k.Box(E.glow, V(p.x0, ShopH - 0.07f, Mathf.Min(cz0, cz1) + 1.4f), V(p.x1, ShopH - 0.05f, Mathf.Max(cz0, cz1) - 1.4f)); // strip lights under it
            for (float x = p.x0; x <= p.x1 + 0.01f; x += w * 2f) { Vector3 c = V(x, 0, p.zf + n.z * 3f); k.Box(TownMats.steel, c - V(0.08f, 0, 0.08f), c + V(0.08f, ShopH, 0.08f)); }
            k.Build("Parade", g, true, ShadowCastingMode.On);
            // solid behind the shop fronts (the walk-ins carve their own way in)
            var col = new GameObject("Solid"); col.transform.SetParent(g, false);
            float s0 = p.x0;
            for (int i = 0; i <= p.shops.Length; i++)
            {
                bool end = i == p.shops.Length; bool walkIn = !end && p.shops[i] < 0;
                if (end || walkIn) { float s1 = p.x0 + i * w; if (s1 - s0 > 0.1f) { var bc = col.AddComponent<BoxCollider>(); bc.center = V((s0 + s1) * 0.5f, top * 0.5f, (zMin + zMax) * 0.5f); bc.size = V(s1 - s0, top, depth); } s0 = s1 + w; }
            }
        }

        /// <summary>A shop you can walk into: a glazed front with a door, the room behind with what that shop has in it.</summary>
        static void Shop(Transform g, Parade p, float x0, float x1, int cell)
        {
            var b = new Bld(g, "Shop " + cell); float depth = 11f, zb = p.facesNorth ? p.zf - depth : p.zf + depth;
            float zMin = Mathf.Min(p.zf, zb), zMax = Mathf.Max(p.zf, zb); var r = new Rect(x0 + 0.15f, zMin, x1 - x0 - 0.3f, depth);
            Vector3 n = p.facesNorth ? Vector3.forward : Vector3.back;
            float w = r.width;
            var front = new[] { Bld.Win(0.3f, w - 1.7f, 0.5f, ShopH - 0.2f), Bld.Door(w - 1.45f, w - 0.35f, 2.3f) };
            var frontRev = new[] { Bld.Door(0.35f, 1.45f, 2.3f), Bld.Win(1.7f, w - 0.3f, 0.5f, ShopH - 0.2f) };
            var paint = TownMats.lobbyWall; var brick = TownMats.brownBrick;
            if (p.facesNorth) b.Wall(V(r.xMax, 0, r.yMax), V(r.xMin, 0, r.yMax), Vector3.forward, 0f, ShopH, TownMats.steel, paint, frontRev);
            else b.Wall(V(r.xMin, 0, r.yMin), V(r.xMax, 0, r.yMin), Vector3.back, 0f, ShopH, TownMats.steel, paint, front);
            // side and back walls (inside faces only: the parade's own walls are the outside)
            b.Part(V(r.xMin + 0.06f, 0, r.yMin), V(r.xMin + 0.06f, 0, r.yMax), 0f, ShopH, paint);
            b.Part(V(r.xMax - 0.06f, 0, r.yMin), V(r.xMax - 0.06f, 0, r.yMax), 0f, ShopH, paint);
            float back = p.facesNorth ? r.yMin + 0.06f : r.yMax - 0.06f;
            b.Part(V(r.xMin, 0, back), V(r.xMax, 0, back), 0f, ShopH, paint);
            b.Slab(r, 0.05f, TownMats.lino, null); b.Slab(r, ShopH, null, E.white, 0.02f);
            float zf = p.zf - n.z * 0.4f; float mid = r.center.x; float inward = -n.z; // +1 when the room runs north from the front
            Vector3 Z(float x, float d) => V(x, 0.05f, p.zf + inward * d);
            switch (cell)
            {
                case 22: // newsagent: counter with the till, magazine racks, sweets, the paper stand, a fridge
                    b.Counter(Z(mid + 1.2f, 7.5f), 0f, 3.2f, false); b.Box(E.black, Z(mid + 0.5f, 7.5f) + V(-0.2f, 1.1f, -0.15f), Z(mid + 0.5f, 7.5f) + V(0.2f, 1.35f, 0.15f));
                    b.Shelf(Z(r.xMin + 0.5f, 2f), Z(r.xMin + 0.5f, 9f), 1.9f); b.Shelf(Z(mid - 0.8f, 2.5f), Z(mid - 0.8f, 6f), 1.4f);
                    b.Box(TownMats.glassDark, Z(r.xMax - 0.9f, 2f) + V(0, 0, 0), Z(r.xMax - 0.3f, 4.2f) + V(0, 2f, 0), true); b.Strip(Z(mid, 4f) + Vector3.up * (ShopH - 0.07f), true); break;
                case 15: // the chippy: the stainless counter and fryers, the menu board, a ledge and stools
                    b.Box(TownMats.steel, Z(r.xMin + 0.3f, 6.5f) + V(0, 0, -0.4f * inward), Z(r.xMax - 0.3f, 6.5f) + V(0, 1.05f, 0.4f * inward), true);
                    b.Box(TownMats.steel, Z(r.xMin + 0.3f, 9.5f), Z(r.xMax - 0.3f, 10.3f) + V(0, 1.1f, 0), true);
                    b.Box(TownMats.glass, Z(r.xMin + 0.5f, 6.4f) + V(0, 1.05f, 0), Z(r.xMax - 0.5f, 6.6f) + V(0, 1.45f, 0));
                    b.Poster(Z(mid, 10.9f) + Vector3.up * 2.4f, -n * -1f, 3f, 0.9f, TownMats.notice);
                    b.Box(E.fence, Z(r.xMin + 0.3f, 1f) + V(0, 1.0f, 0), Z(r.xMin + 0.7f, 5f) + V(0, 1.05f, 0), true);
                    b.Strip(Z(mid, 4f) + Vector3.up * (ShopH - 0.07f), true); b.Strip(Z(mid, 8f) + Vector3.up * (ShopH - 0.07f), false); break;
                case 10: // the café: tables and chairs, the counter at the back with the urn and the cake stand
                    for (int t = 0; t < 3; t++) { var c = Z(mid - 0.8f + (t % 2) * 1.6f, 2.2f + t * 2f); b.Table(c, 0.8f, 0.8f); b.Chair(c + V(0, 0, -0.6f), 0f, E.black); b.Chair(c + V(0, 0, 0.6f), 180f, E.black); }
                    b.Counter(Z(mid, 9f), 0f, w - 1.2f, false); b.Box(TownMats.steel, Z(mid - 1f, 9f) + V(-0.15f, 1.1f, -0.15f), Z(mid - 1f, 9f) + V(0.15f, 1.55f, 0.15f));
                    b.Strip(Z(mid, 3f) + Vector3.up * (ShopH - 0.07f), true); b.Strip(Z(mid, 7.5f) + Vector3.up * (ShopH - 0.07f), false); break;
                case 5: // the bookies: screens on the wall, a counter, the betting slips shelf, stools
                    for (int t = 0; t < 4; t++) b.Telly(Z(r.xMin + 0.1f, 2f + t * 1.6f) + Vector3.up * 1.6f, Vector3.right);
                    b.Counter(Z(mid + 0.5f, 9.4f), 0f, w - 2f, true); b.Box(E.fence, Z(r.xMax - 0.5f, 1.5f) + V(0, 1f, 0), Z(r.xMax - 0.2f, 7f) + V(0, 1.05f, 0), true);
                    b.Strip(Z(mid, 4f) + Vector3.up * (ShopH - 0.07f), true); break;
                case 3: default: // pound shop: aisles of shelving, the till by the door
                    for (int a = 0; a < 2; a++) b.Shelf(Z(r.xMin + 1.4f + a * 1.8f, 2.5f), Z(r.xMin + 1.4f + a * 1.8f, 9.5f), 1.8f);
                    b.Counter(Z(r.xMax - 0.8f, 1.8f), 90f, 1.8f, false);
                    b.Strip(Z(mid, 3f) + Vector3.up * (ShopH - 0.07f), true); b.Strip(Z(mid, 8f) + Vector3.up * (ShopH - 0.07f), false); break;
            }
            b.Done();
        }

        /// <summary>The Community Hub: a single-storey hall with big windows, chairs and tables, a noticeboard, a hatch kitchen.</summary>
        static void Hub(Transform root)
        {
            var b = new Bld(root, "Community Hub"); var r = new Rect(-44f, 82f, 14f, 22f);
            var brick = TownMats.brownBrick; var paint = TownMats.lobbyWall;
            b.Shell(r, 0f, 4.4f, brick, paint, new[] { Bld.Win(2f, 5f, 1f, 3.2f), Bld.Win(9f, 12f, 1f, 3.2f) },
                new[] { Bld.Win(1.5f, 5f, 1f, 3.4f), Bld.Door(7.5f, 10f, 2.6f), Bld.Win(12f, 15.5f, 1f, 3.4f), Bld.Win(17f, 20.5f, 1f, 3.4f) },
                new[] { Bld.Win(2f, 5f, 1f, 3.2f), Bld.Win(9f, 12f, 1f, 3.2f) }, new[] { Bld.Door(3f, 4.2f), Bld.Win(8f, 11f), Bld.Win(15f, 18f) });
            b.Roof(r, 4.4f); b.Slab(r, 0.06f, TownMats.lino, null); b.Slab(r, 4.4f, null, E.white);
            b.Sign(TownMats.signsServices, Bld.SignCell(9), V(-29.98f, 3.7f, 90.8f), Vector3.right, 4.8f, 0.8f);
            b.Box(TownMats.panel, V(-30f, 2.8f, 87f), V(-27.6f, 3f, 94.6f), false, true); // canopy over the door
            for (int i = 0; i < 3; i++) { var c = V(-38f + (i % 2) * 2.5f, 0.06f, 86f + i * 4f); b.Table(c, 1.6f, 0.8f); b.Chair(c + V(-0.4f, 0, -0.65f), 0f); b.Chair(c + V(0.4f, 0, -0.65f), 0f); b.Chair(c + V(-0.4f, 0, 0.65f), 180f); b.Chair(c + V(0.4f, 0, 0.65f), 180f); }
            b.Kitchen(V(-43.65f, 0.06f, 97f), Vector3.forward, 5f); b.Part(V(-40f, 0, 96f), V(-40f, 0, 104f), 0.06f, 4f, paint, (3.5f, 1.1f));
            b.Poster(V(-43.82f, 1.6f, 90f), Vector3.right, 2.4f, 1.2f, TownMats.notice); b.Box(E.fence, V(-36f, 0.06f, 102.6f), V(-31f, 0.95f, 103.6f), true); // food bank tables
            for (int i = 0; i < 8; i++) b.Box(i % 2 == 0 ? TownMats.panel : E.white, V(-35.8f + i * 0.6f, 0.95f, 102.8f), V(-35.4f + i * 0.6f, 1.25f, 103.3f)); // boxes of food
            b.Strip(V(-37f, 4.1f, 88f), true); b.Strip(V(-37f, 4.1f, 98f), true);
            b.Done();
        }

        static void Toilets(Transform root)
        {
            var b = new Bld(root, "Public Toilets"); var r = new Rect(100f, 86f, 8f, 10f);
            b.Shell(r, 0f, 3f, TownMats.brownBrick, E.white, null, null, null, new[] { Bld.Door(2f, 3f), Bld.Door(7f, 8f) });
            b.Roof(r, 3f, 0.3f); b.Slab(r, 0.05f, TownMats.lino, null);
            b.Part(V(104f, 0, 86f), V(104f, 0, 96f), 0.05f, 2.8f, E.white);
            b.Box(TownMats.steel, V(100.4f, 0.05f, 93f), V(101f, 0.5f, 93.6f)); b.Box(TownMats.steel, V(104.4f, 0.05f, 88f), V(105f, 0.5f, 88.6f));
            b.Done();
        }

        /// <summary>The square: paving, planters, benches, bins, heritage lamps, the bandstand, a clock, the phone kiosk.</summary>
        static void Square(Transform root)
        {
            var g = new GameObject("Square").transform; g.SetParent(root, false);
            var k = new MeshKit(); var pave = TownMats.paviors != null ? TownMats.paviors : E.pave;
            void Pave(float x0, float z0, float x1, float z1) { k.Quad(pave, V(x0, 0.08f, z0), V(x1, 0.08f, z0), V(x1, 0.08f, z1), V(x0, 0.08f, z1), Vector3.up); }
            Pave(-30f, 82f, 108f, 104f); Pave(14f, 104f, 26f, 117.4f); Pave(40f, 70f, 56f, 82f); Pave(-30f, 104f, -28f, 115f);
            foreach (var (a, b2, nn) in new[] { (V(-30f, 0, 82f), V(-30f, 0, 104f), Vector3.left), (V(108f, 0, 82f), V(108f, 0, 104f), Vector3.right) }) k.Quad(E.kerb, a, b2, b2 + Vector3.up * 0.08f, a + Vector3.up * 0.08f, nn);
            // the bandstand: octagonal brick plinth with steps, eight green posts, a lead roof with a finial
            Vector3 c = Bandstand; const int N = 8; float R = 5.2f;
            var ring = new Vector3[N]; for (int i = 0; i < N; i++) { float a = (i + 0.5f) * Mathf.PI * 2f / N; ring[i] = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)); }
            for (int i = 0; i < N; i++)
            {
                Vector3 a0 = c + ring[i] * R, a1 = c + ring[(i + 1) % N] * R; Vector3 nn = ((ring[i] + ring[(i + 1) % N]) * 0.5f).normalized;
                k.Quad(TownMats.redBrick != null ? TownMats.redBrick : E.brick, a0 + Vector3.up * 0.08f, a1 + Vector3.up * 0.08f, a1 + Vector3.up * 0.9f, a0 + Vector3.up * 0.9f, nn);
                k.TriN(E.slabs, c + Vector3.up * 0.9f, a0 + Vector3.up * 0.9f, a1 + Vector3.up * 0.9f, Vector3.up);
                Vector3 p = c + ring[i] * (R - 0.25f);
                k.Cyl(E.railGreen, p + Vector3.up * 0.9f, 0.09f, 3.2f, 8);
                Vector3 r0 = c + ring[i] * (R - 0.25f) + Vector3.up * 1.85f, r1 = c + ring[(i + 1) % N] * (R - 0.25f) + Vector3.up * 1.85f;
                if (i != 0) k.Box(E.railGreen, Vector3.Min(r0, r1) - V(0.03f, 0.03f, 0.03f), Vector3.Max(r0, r1) + V(0.03f, 0.03f, 0.03f)); // the rail (open on the steps side)
                // roof: a shallow cone in two rings
                Vector3 e0 = c + ring[i] * (R + 0.5f) + Vector3.up * 4.1f, e1 = c + ring[(i + 1) % N] * (R + 0.5f) + Vector3.up * 4.1f, apex = c + Vector3.up * 6.2f;
                k.TriN(E.felt, e0, e1, apex, (nn + Vector3.up * 1.4f).normalized);
                k.TriN(E.white, e0, e1, apex, (-nn - Vector3.up * 0.4f).normalized); // the painted underside
                k.Quad(E.white, e0, e1, e1 + Vector3.down * 0.35f, e0 + Vector3.down * 0.35f, nn); // fascia
            }
            k.Cyl(E.railGreen, c + Vector3.up * 6.2f, 0.06f, 0.8f, 6); k.Sphere(E.railGreen, c + Vector3.up * 7f, Vector3.one * 0.15f, 6, 4);
            Vector3 sd = c + V(0, 0, -R); for (int s = 0; s < 4; s++) k.Box(E.slabs, sd + V(-1.4f, 0f, -0.35f * (s + 1)), sd + V(1.4f, 0.9f - s * 0.22f, -0.35f * s)); // steps at the south
            // the plinth as two squares turned 45 degrees to each other (an octagon), and the posts
            var col = new GameObject("Bandstand"); col.transform.SetParent(g, false); col.transform.position = c;
            var top = col.AddComponent<BoxCollider>(); top.center = Vector3.up * 0.45f; top.size = V(R * 1.84f, 0.9f, R * 1.84f);
            var col2 = new GameObject("Bandstand45"); col2.transform.SetParent(col.transform, false); col2.transform.localRotation = Quaternion.Euler(0, 45f, 0);
            var top2 = col2.AddComponent<BoxCollider>(); top2.center = Vector3.up * 0.45f; top2.size = V(R * 1.84f, 0.9f, R * 1.84f);
            // planters (raised brick beds with shrubs), benches, bins, lamps, a clock post, the phone kiosk, a cash machine
            foreach (var pc in new[] { V(0f, 0, 93f), V(20f, 0, 88f), V(60f, 0, 98f), V(80f, 0, 88f), V(-18f, 0, 98f), V(95f, 0, 98f) })
            {
                k.Box(E.brick, pc + V(-2.2f, 0f, -1.2f), pc + V(2.2f, 0.65f, 1.2f)); k.Box(E.slabs, pc + V(-2.3f, 0.65f, -1.3f), pc + V(2.3f, 0.72f, 1.3f));
                for (int i = 0; i < 3; i++) k.Sphere(TownMats.hedge != null ? TownMats.hedge : E.railGreen, pc + V(-1.3f + i * 1.3f, 1.1f, 0f), V(0.7f, 0.55f, 0.6f), 8, 5);
                var pcol = new GameObject("Planter"); pcol.transform.SetParent(g, false); var pb = pcol.AddComponent<BoxCollider>(); pb.center = pc + Vector3.up * 0.36f; pb.size = V(4.6f, 0.72f, 2.6f);
            }
            foreach (var (bp, yaw) in new[] { (V(0f, 0, 90.9f), 0f), (V(0f, 0, 95.1f), 180f), (V(60f, 0, 95.9f), 0f), (V(80f, 0, 90.1f), 180f), (V(28f, 0, 99f), 150f), (V(52f, 0, 99f), 210f), (V(40f, 0, 85f), 0f) })
            {
                k.Push(bp, yaw); k.Box(E.fence, V(-0.9f, 0.42f, -0.22f), V(0.9f, 0.47f, 0.22f)); k.Box(E.fence, V(-0.9f, 0.5f, 0.18f), V(0.9f, 0.85f, 0.22f));
                foreach (float x in new[] { -0.75f, 0.75f }) k.Box(E.black, V(x - 0.04f, 0f, -0.2f), V(x + 0.04f, 0.45f, 0.22f)); k.Pop();
            }
            foreach (var lp in new[] { V(-20f, 0, 87f), V(10f, 0, 99f), V(30f, 0, 86f), V(50f, 0, 100f), V(70f, 0, 86f), V(90f, 0, 99f), V(18f, 0, 110f), V(22f, 0, 110f), V(48f, 0, 76f) }) Roads.HeritageLamp(k, lp + Vector3.up * 0.08f, 4.2f);
            foreach (var bp in new[] { V(-10f, 0, 92f), V(25f, 0, 93f), V(55f, 0, 93f), V(88f, 0, 93f) }) { k.Cyl(E.black, bp, 0.04f, 0.6f, 6); k.Cyl(E.railGreen, bp + Vector3.up * 0.45f, 0.22f, 0.55f, 10, true); }
            // the clock: a green post with a two-faced clock
            Vector3 cp = V(28f, 0.08f, 93f); k.Cyl(E.railGreen, cp, 0.12f, 3.4f, 8); k.Box(E.railGreen, cp + V(-0.45f, 3.4f, -0.12f), cp + V(0.45f, 4.3f, 0.12f));
            k.Cyl(E.white, cp + V(0f, 3.85f, 0f) + V(0, 0, 0), 0.35f, 0.01f, 16); k.Box(E.white, cp + V(-0.36f, 3.5f, -0.14f), cp + V(0.36f, 4.2f, 0.14f));
            k.Box(E.black, cp + V(-0.02f, 3.85f, -0.15f), cp + V(0.02f, 4.1f, 0.15f)); k.Box(E.black, cp + V(-0.02f, 3.83f, -0.15f), cp + V(0.18f, 3.87f, 0.15f));
            // a modern phone kiosk (steel and glass), the cash machine in the bookies' pier
            Vector3 kp = V(12f, 0.08f, 100f); k.Box(TownMats.steel, kp + V(-0.5f, 0f, -0.5f), kp + V(0.5f, 0.1f, 0.5f)); k.Box(TownMats.steel, kp + V(-0.5f, 2.2f, -0.5f), kp + V(0.5f, 2.4f, 0.5f));
            foreach (var (x, z) in new[] { (-0.5f, -0.5f), (0.45f, -0.5f), (-0.5f, 0.45f), (0.45f, 0.45f) }) k.Box(TownMats.steel, kp + V(x, 0f, z), kp + V(x + 0.05f, 2.2f, z + 0.05f));
            k.Box(TownMats.glassDark, kp + V(-0.5f, 0.1f, 0.47f), kp + V(0.5f, 2.2f, 0.5f)); k.Box(E.black, kp + V(-0.2f, 1f, 0.38f), kp + V(0.2f, 1.6f, 0.47f));
            var kc = new GameObject("Kiosk"); kc.transform.SetParent(g, false); var kb = kc.AddComponent<BoxCollider>(); kb.center = kp + Vector3.up * 1.2f; kb.size = V(1f, 2.4f, 1f);
            k.Box(TownMats.steel, V(-6.5f, 0.9f, 82.12f), V(-5.6f, 1.7f, 82.25f)); k.Box(TownMats.glassDark, V(-6.35f, 1.3f, 82.25f), V(-5.75f, 1.6f, 82.27f));
            // the precinct's name over the way in from Northway
            var sk = new MeshKit();
            sk.Box(TownMats.steel, V(13.8f, 0f, 115f), V(14.2f, 5f, 115.4f)); sk.Box(TownMats.steel, V(25.8f, 0f, 115f), V(26.2f, 5f, 115.4f)); sk.Box(TownMats.steel, V(13.8f, 5f, 115f), V(26.2f, 5.2f, 115.4f));
            var su = Bld.SignCell(8); sk.QuadUV(TownMats.signsServices, V(15f, 4.1f, 115.42f), V(25f, 4.1f, 115.42f), V(25f, 4.95f, 115.42f), V(15f, 4.95f, 115.42f), Vector3.forward, new Vector2(su.xMax, su.yMin), new Vector2(su.xMin, su.yMin), new Vector2(su.xMin, su.yMax), new Vector2(su.xMax, su.yMax));
            sk.QuadUV(TownMats.signsServices, V(25f, 4.1f, 114.98f), V(15f, 4.1f, 114.98f), V(15f, 4.95f, 114.98f), V(25f, 4.95f, 114.98f), Vector3.back, new Vector2(su.xMax, su.yMin), new Vector2(su.xMin, su.yMin), new Vector2(su.xMin, su.yMax), new Vector2(su.xMax, su.yMax));
            sk.Box(E.black, V(15f, 4.05f, 115.0f), V(25f, 5f, 115.4f));
            k.Build("Square", g, true, ShadowCastingMode.On); sk.Build("Arch", g, false, ShadowCastingMode.On);
        }
    }
}
