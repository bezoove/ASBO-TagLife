using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// The high street along the main road west of Abbots Wend, built up over a century like a real one: mostly
    /// Victorian and Edwardian red brick (stone bands, sash windows, slate roofs, chimney stacks) over modern shopfronts,
    /// with a 1930s white-rendered pair (flat roof, stepped parapet, banding, metal windows) and a 1960s flat-roofed infill
    /// block (buff brick, concrete panels, window band, canopy). The Eggy Duck sits on the corner by Abbots Wend: green
    /// glazed tiles, big windows, a hanging sign, dormers, and a walled beer garden with benches behind. Shops on the north
    /// side: Raj's, the Golden Dragon (Mum's flat above), the Codfather, the bookies, the launderette, the barbers and the
    /// hospice shop; on the south side: the bakery, the nail bar, the vape shop, the pound shop, the pharmacy and the post
    /// office; on the estate, Hawthorn Parade with a corner shop and a kebab shop. Ten units stand empty, to let (whitewashed,
    /// boarded, shuttered, or with the last business's faded board still up) for the story. Every name is made up. Built into the scene by the editor with the rest of the town.
    /// </summary>
    public static class HighStreet
    {
        public const string RootName = "HighStreet";
        public const float NorthFront = -22.4f, SouthFront = -37.7f, Depth = 11f, NorthStartX = -52f, SouthEndX = -86f;
        public enum Era { Gap, Victorian, Thirties, Sixties, Pub }
        public struct Unit { public int shop; public float w; public Era era; public int storeys; }
        static Unit U(int shop, float w, Era e, int st) => new Unit { shop = shop, w = w, era = e, storeys = st };

        // shopfront atlas cells that are empty units (to let): whitewashed, boarded, shuttered, the old business's faded board left up, closed down, available
        public static readonly HashSet<int> EmptyCells = new HashSet<int> { 6, 15, 16, 17, 18, 19, 22, 23 };

        /// <summary>North side, from the corner going west (the Eggy Duck has moved out to its own manor house: TudorManor). Six units to let.</summary>
        public static readonly Unit[] North =
        {
            U(22, 6.5f, Era.Victorian, 3), U(19, 6f, Era.Victorian, 2), U(-1, 6.5f, Era.Gap, 0), U(0, 6.5f, Era.Victorian, 3), U(1, 6f, Era.Victorian, 3), U(2, 6f, Era.Victorian, 2),
            U(15, 5.5f, Era.Victorian, 2), U(3, 5.8f, Era.Thirties, 2), U(4, 5.8f, Era.Thirties, 2), U(5, 5f, Era.Victorian, 2), U(6, 5.5f, Era.Victorian, 3),
            U(17, 6f, Era.Victorian, 3), U(7, 6f, Era.Victorian, 3), U(16, 5.5f, Era.Victorian, 2),
        };
        /// <summary>South side, from the west end going east to <see cref="SouthEndX"/>. Three units to let.</summary>
        public static readonly Unit[] South =
        {
            U(19, 5.5f, Era.Victorian, 2), U(13, 6f, Era.Victorian, 2), U(12, 5f, Era.Victorian, 2), U(18, 6f, Era.Victorian, 3), U(11, 5f, Era.Victorian, 2),
            U(10, 12f, Era.Sixties, 2), U(22, 6f, Era.Sixties, 2), U(9, 6f, Era.Victorian, 3), U(8, 7f, Era.Thirties, 2),
        };
        /// <summary>Hawthorn Parade, the estate's own 1960s row of shops with maisonettes over, on the green by the west road. Three to let.</summary>
        public static readonly Unit[] Estate =
        {
            U(20, 5.6f, Era.Sixties, 2), U(23, 5.6f, Era.Sixties, 2), U(21, 5.6f, Era.Sixties, 2), U(15, 5.6f, Era.Sixties, 2), U(16, 5.6f, Era.Sixties, 2),
        };
        public static readonly Vector3 EstateOrigin = new Vector3(-33.5f, 0f, -72f); public const float EstateYaw = -90f;

        public struct Empty { public string parade; public int index; public Vector3 front; public Quaternion facing; }
        /// <summary>The empty shops (ten of them), with where their doors are, for the story to use.</summary>
        public static List<Empty> ToLet()
        {
            var l = new List<Empty>();
            void Scan(string name, Unit[] units, Vector3 o, float yaw)
            {
                float x = 0f; var r = Quaternion.Euler(0, yaw, 0);
                for (int i = 0; i < units.Length; i++)
                {
                    var u = units[i];
                    if (u.era != Era.Gap && EmptyCells.Contains(u.shop)) l.Add(new Empty { parade = name, index = i, front = o + r * new Vector3(x + u.w * 0.5f, 0f, 0.6f), facing = r });
                    x += u.w;
                }
            }
            Scan("North parade", North, new Vector3(NorthStartX, 0f, NorthFront), 180f);
            Scan("South parade", South, new Vector3(SouthEndX - Length(South), 0f, SouthFront), 0f);
            Scan("Hawthorn Parade", Estate, EstateOrigin, EstateYaw);
            return l;
        }
        public static float Length(Unit[] l) { float s = 0; foreach (var u in l) s += u.w; return s; }

        class Mats { public Material brick, stone, render, slate, sash, shops, tiles, sign, paint, frame, pot, buff, concrete, windows, black, white, felt, timber, iron; }
        static Mats M;
        static Material T(string n) => Resources.Load<Material>("Town/" + n);
        static Material E(string n) => Resources.Load<Material>("Estate/" + n);

        static bool LoadMats()
        {
            if (M != null && M.brick != null && M.shops != null) return true;
            M = new Mats
            {
                brick = T("TownBrick"), stone = T("TownStone"), render = T("TownRender"), slate = T("TownSlate"), sash = T("TownSash"), shops = T("TownShops"), tiles = T("TownPubTiles"),
                sign = T("TownPubSign"), paint = T("TownPaint"), frame = T("TownFrame"), pot = T("TownPot"), buff = E("EstateBrick"), concrete = E("EstateConcrete"), windows = E("EstateWindows"),
                black = E("EstateBlack"), white = E("EstateWhite"), felt = E("EstateFelt"), timber = E("EstateFence"), iron = Resources.Load<Material>("Castle/CastleIron"),
            };
            if (M.brick == null || M.shops == null) { Debug.LogWarning("[ASBO] high street: materials missing"); M = null; return false; }
            MeshKit.Tile[M.brick] = 2.7f; MeshKit.Tile[M.stone] = 2f; MeshKit.Tile[M.render] = 2f; MeshKit.Tile[M.slate] = 3f; MeshKit.Tile[M.tiles] = 1f;
            return true;
        }

        public static Transform Build(Transform parent)
        {
            M = null; if (!LoadMats()) return null;
            var root = new GameObject(RootName).transform; root.SetParent(parent, false);
            Parade(root, "North parade", North, new Vector3(NorthStartX, 0f, NorthFront), 180f);
            Parade(root, "South parade", South, new Vector3(SouthEndX - Length(South), 0f, SouthFront), 0f);
            Parade(root, "Hawthorn Parade", Estate, EstateOrigin, EstateYaw);
            Debug.Log($"[ASBO] high street: {North.Length + South.Length + Estate.Length} buildings, {ToLet().Count} shops to let");
            return root;
        }

        /// <summary>
        /// Another parade in the same styles, for the streets that fill the gaps (<see cref="Infill"/>): its own
        /// shopfront atlas (same cell layout) and its own brick. Units run along the parade's +x from origin, fronts face +z.
        /// </summary>
        public static Transform ExtraParade(Transform root, string name, Unit[] units, Vector3 origin, float yaw, Material shops, Material brick, float brickTile)
        {
            if (!LoadMats()) return null;
            Material s0 = M.shops, b0 = M.brick;
            if (shops != null) M.shops = shops;
            if (brick != null) { M.brick = brick; MeshKit.Tile[brick] = brickTile; }
            try { return Parade(root, name, units, origin, yaw); }
            finally { M.shops = s0; M.brick = b0; }
        }
        public static Unit Shop(int cell, float w, Era e, int storeys) => U(cell, w, e, storeys);

        static Transform Parade(Transform root, string name, Unit[] units, Vector3 origin, float yaw)
        {
            var g = new GameObject(name).transform; g.SetParent(root, false); g.localPosition = origin; g.localRotation = Quaternion.Euler(0, yaw, 0);
            var k = new MeshKit(); var rng = new System.Random(name.GetHashCode());
            float x = 0f; var cols = new List<(Vector3, Vector3)>();
            for (int i = 0; i < units.Length; i++)
            {
                var u = units[i]; float x0 = x, x1 = x + u.w; x = x1;
                if (u.era == Era.Gap) continue;
                float h = u.era switch
                {
                    Era.Pub => Pub(k, x0, x1, rng),
                    Era.Thirties => Thirties(k, x0, x1, u, rng),
                    Era.Sixties => Sixties(k, x0, x1, u, rng),
                    _ => Victorian(k, x0, x1, u, rng),
                };
                cols.Add((new Vector3((x0 + x1) * 0.5f, h * 0.5f, -Depth * 0.5f), new Vector3(u.w, h, Depth)));
            }
            var body = k.Build("Buildings", g, false);
            foreach (var (c, s) in cols) { var bc = body.AddComponent<BoxCollider>(); bc.center = c; bc.size = s; }
            if (units[0].era == Era.Pub) BeerGarden(g, 0f, units[0].w);
            return g;
        }

        // ------------------------------------------------------------------------------------------ shared pieces
        /// <summary>Atlas rectangles for a shop's fascia and its shopfront (shopfronts.png: 4 columns of 512 x 384 cells in a 2048 square).</summary>
        const float Cell = 340f;
        static Rect Fascia(int shop) { int c = shop % 4, r = shop / 4; float y0 = r * Cell, y1 = y0 + 72f; return new Rect(c * 0.25f + 0.002f, 1f - y1 / 2048f, 0.246f, 72f / 2048f); }
        static Rect Front(int shop) { int c = shop % 4, r = shop / 4; float y0 = r * Cell + 72f, y1 = r * Cell + Cell; return new Rect(c * 0.25f + 0.002f, 1f - y1 / 2048f, 0.246f, (y1 - y0) / 2048f); }

        /// <summary>A front-facing (+z) picture. Seen from the street (looking -z) the left-hand end is +x, so the texture runs from x1 to x0.</summary>
        static void Face(MeshKit k, Material m, float x0, float x1, float y0, float y1, float z, Rect uv)
            => k.QuadUV(m, new Vector3(x0, y0, z), new Vector3(x1, y0, z), new Vector3(x1, y1, z), new Vector3(x0, y1, z), Vector3.forward,
                new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMin, uv.yMax), new Vector2(uv.xMax, uv.yMax));

        /// <summary>A wall in the front plane (z) of one material from x0..x1, y0..y1, with rectangular holes.</summary>
        static void WallHoles(MeshKit k, Material m, float x0, float x1, float y0, float y1, float z, List<Rect> holes, bool front = true)
        {
            var xs = new SortedSet<float> { x0, x1 }; var ys = new SortedSet<float> { y0, y1 };
            foreach (var h in holes) { xs.Add(h.xMin); xs.Add(h.xMax); ys.Add(h.yMin); ys.Add(h.yMax); }
            var X = new List<float>(xs); var Y = new List<float>(ys); var n = front ? Vector3.forward : Vector3.back;
            for (int j = 0; j < Y.Count - 1; j++)
                for (int i = 0; i < X.Count - 1; i++)
                {
                    float cx = (X[i] + X[i + 1]) * 0.5f, cy = (Y[j] + Y[j + 1]) * 0.5f; bool hole = false;
                    foreach (var h in holes) if (h.Contains(new Vector2(cx, cy))) { hole = true; break; }
                    if (hole || X[i + 1] - X[i] < 1e-3f || Y[j + 1] - Y[j] < 1e-3f) continue;
                    k.Quad(m, new Vector3(X[i], Y[j], z), new Vector3(X[i + 1], Y[j], z), new Vector3(X[i + 1], Y[j + 1], z), new Vector3(X[i], Y[j + 1], z), n);
                }
        }

        /// <summary>A recessed window from an atlas, with reveals in the wall's material, a stone sill and (optionally) a lintel.</summary>
        static void Win(MeshKit k, Material glass, Rect uv, float x0, float x1, float y0, float y1, float z, Material reveal, bool front, bool lintel)
        {
            float s = front ? 1f : -1f, zb = z - s * 0.12f;
            var n = front ? Vector3.forward : Vector3.back;
            if (front) Face(k, glass, x0, x1, y0, y1, zb, uv);
            else k.QuadUV(glass, new Vector3(x1, y0, zb), new Vector3(x0, y0, zb), new Vector3(x0, y1, zb), new Vector3(x1, y1, zb), n,
                new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMin, uv.yMax), new Vector2(uv.xMax, uv.yMax)); // seen from behind, left is -x
            k.Quad(reveal, new Vector3(x0, y0, z), new Vector3(x0, y1, z), new Vector3(x0, y1, zb), new Vector3(x0, y0, zb), Vector3.right);
            k.Quad(reveal, new Vector3(x1, y0, z), new Vector3(x1, y1, z), new Vector3(x1, y1, zb), new Vector3(x1, y0, zb), Vector3.left);
            k.Quad(reveal, new Vector3(x0, y1, z), new Vector3(x1, y1, z), new Vector3(x1, y1, zb), new Vector3(x0, y1, zb), Vector3.down);
            float zo = z + s * 0.07f;
            k.Box(M.stone, new Vector3(x0 - 0.08f, y0 - 0.12f, Mathf.Min(z, zo)), new Vector3(x1 + 0.08f, y0, Mathf.Max(z, zo)));
            if (lintel) k.Box(M.stone, new Vector3(x0 - 0.1f, y1, Mathf.Min(z, z + s * 0.04f)), new Vector3(x1 + 0.1f, y1 + 0.25f, Mathf.Max(z, z + s * 0.04f)));
        }

        /// <summary>The shopfront: recessed glass and door from the atlas, a fascia board with the name, pilasters and a cornice over.</summary>
        static void Shopfront(MeshKit k, int shop, float x0, float x1, float top)
        {
            const float pil = 0.32f, fasciaH = 0.62f; float fy0 = top - fasciaH - 0.15f, fy1 = top - 0.15f;
            float a = x0 + pil, b = x1 - pil;
            // the glazed front set back in the opening, the opening's soffit and sides
            Face(k, M.shops, a, b, 0f, fy0, -0.22f, Front(shop));
            k.Quad(M.frame, new Vector3(a, fy0, 0f), new Vector3(b, fy0, 0f), new Vector3(b, fy0, -0.22f), new Vector3(a, fy0, -0.22f), Vector3.down);
            k.Quad(M.frame, new Vector3(a, 0f, 0f), new Vector3(a, fy0, 0f), new Vector3(a, fy0, -0.22f), new Vector3(a, 0f, -0.22f), Vector3.right);
            k.Quad(M.frame, new Vector3(b, 0f, 0f), new Vector3(b, fy0, 0f), new Vector3(b, fy0, -0.22f), new Vector3(b, 0f, -0.22f), Vector3.left);
            // fascia board, proud, with its sign; console brackets at each end; cornice
            k.Box(M.frame, new Vector3(a - 0.05f, fy0, 0f), new Vector3(b + 0.05f, fy1, 0.14f), 16);
            Face(k, M.shops, a, b, fy0 + 0.03f, fy1 - 0.03f, 0.141f, Fascia(shop));
            k.Box(M.stone, new Vector3(x0, fy1, 0f), new Vector3(x1, top, 0.26f), 16);
            foreach (float px in new[] { x0, b }) k.Box(M.stone, new Vector3(px, 0f, 0f), new Vector3(px + pil, fy1, 0.12f), 16);
        }

        /// <summary>A pitched slate roof over x0..x1 (ridge along the street), with eaves overhang, gutters and gable triangles.</summary>
        static float Roof(MeshKit k, float x0, float x1, float eave, float pitchDeg, Material gable)
        {
            float t = Mathf.Tan(pitchDeg * Mathf.Deg2Rad), zr = -Depth * 0.5f, ridge = eave + Depth * 0.5f * t, oh = 0.3f;
            float ty = MeshKit.TileOf(M.slate);
            foreach (int s in new[] { 1, -1 })
            {
                float ze = s > 0 ? oh : -Depth - oh, ye = eave - oh * t;
                float slope = Mathf.Sqrt((ze - zr) * (ze - zr) + (ridge - ye) * (ridge - ye));
                k.QuadUV(M.slate, new Vector3(x0, ye, ze), new Vector3(x1, ye, ze), new Vector3(x1, ridge, zr), new Vector3(x0, ridge, zr), new Vector3(0, 1, s * t).normalized,
                    new Vector2(x0 / ty, 0), new Vector2(x1 / ty, 0), new Vector2(x1 / ty, slope / ty), new Vector2(x0 / ty, slope / ty));
                k.Box(M.black, new Vector3(x0, ye - 0.16f, s > 0 ? ze - 0.02f : ze - 0.12f), new Vector3(x1, ye - 0.04f, s > 0 ? ze + 0.12f : ze + 0.02f));
                k.Quad(M.black, new Vector3(x0, ye - 0.02f, s > 0 ? 0f : -Depth), new Vector3(x1, ye - 0.02f, s > 0 ? 0f : -Depth), new Vector3(x1, ye - 0.02f, ze), new Vector3(x0, ye - 0.02f, ze), Vector3.down);
            }
            k.Box(M.pot, new Vector3(x0, ridge - 0.05f, zr - 0.14f), new Vector3(x1, ridge + 0.12f, zr + 0.14f));
            k.TriN(gable, new Vector3(x0, eave, 0f), new Vector3(x0, eave, -Depth), new Vector3(x0, ridge, zr), Vector3.left);
            k.TriN(gable, new Vector3(x1, eave, 0f), new Vector3(x1, eave, -Depth), new Vector3(x1, ridge, zr), Vector3.right);
            return ridge;
        }

        static void Chimney(MeshKit k, float x, float ridge, float zc)
        {
            k.Box(M.brick, new Vector3(x - 0.45f, ridge - 1.5f, zc - 0.35f), new Vector3(x + 0.45f, ridge + 1.3f, zc + 0.35f), 4);
            k.Box(M.stone, new Vector3(x - 0.52f, ridge + 1.3f, zc - 0.42f), new Vector3(x + 0.52f, ridge + 1.45f, zc + 0.42f));
            foreach (float dx in new[] { -0.2f, 0.2f }) k.Cyl(M.pot, new Vector3(x + dx, ridge + 1.45f, zc), 0.11f, 0.5f, 8, true, 0.09f);
        }

        /// <summary>Plain side and back walls of a building in a material (the back gets windows and a door).</summary>
        static void SidesAndBack(MeshKit k, Material m, float x0, float x1, float h, System.Random rng, bool sashes)
        {
            k.Quad(m, new Vector3(x0, 0, 0), new Vector3(x0, h, 0), new Vector3(x0, h, -Depth), new Vector3(x0, 0, -Depth), Vector3.left);
            k.Quad(m, new Vector3(x1, 0, 0), new Vector3(x1, h, 0), new Vector3(x1, h, -Depth), new Vector3(x1, 0, -Depth), Vector3.right);
            var holes = new List<Rect>(); float w = x1 - x0;
            float dx = x0 + 0.8f; holes.Add(new Rect(dx, 0f, 0.9f, 2.1f));
            for (float y = 3.9f; y + 1.6f < h; y += 3f) holes.Add(new Rect(x0 + w * 0.5f - 0.5f, y, 1.0f, 1.6f));
            WallHoles(k, m, x0, x1, 0f, h, -Depth, holes, false);
            k.QuadUV(M.frame, new Vector3(dx + 0.9f, 0f, -Depth + 0.08f), new Vector3(dx, 0f, -Depth + 0.08f), new Vector3(dx, 2.1f, -Depth + 0.08f), new Vector3(dx + 0.9f, 2.1f, -Depth + 0.08f), Vector3.back,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            foreach (var hRect in holes) if (hRect.yMin > 1f) Win(k, sashes ? M.sash : M.windows, sashes ? SashUV(rng.Next(4)) : new Rect(0f, 0.004f, 0.5f, 0.242f), hRect.xMin, hRect.xMax, hRect.yMin, hRect.yMax, -Depth, m, false, false);
        }

        static Rect SashUV(int v) => new Rect(v * 0.25f + 0.004f, 0.004f, 0.242f, 0.992f);

        // ------------------------------------------------------------------------------------------ the eras
        /// <summary>Victorian / Edwardian: red brick, a stone band and cornice, sash windows with stone sills and lintels, slate roof and chimney.</summary>
        static float Victorian(MeshKit k, float x0, float x1, Unit u, System.Random rng)
        {
            const float G = 3.9f, FH = 3.0f; float eave = G + (u.storeys - 1) * FH;
            Shopfront(k, u.shop, x0, x1, G);
            int n = (x1 - x0) > 6.2f ? 3 : 2; float w = x1 - x0, ww = 0.95f, gap = (w - n * ww) / n;
            var holes = new List<Rect>();
            for (int f = 1; f < u.storeys; f++)
                for (int i = 0; i < n; i++)
                {
                    float wx0 = x0 + gap * 0.5f + i * (ww + gap), wy0 = G + (f - 1) * FH + 0.75f, wy1 = wy0 + (f == 1 ? 1.75f : 1.5f);
                    holes.Add(new Rect(wx0, wy0, ww, wy1 - wy0));
                    Win(k, M.sash, SashUV(rng.Next(4)), wx0, wx0 + ww, wy0, wy1, 0f, M.brick, true, true);
                }
            WallHoles(k, M.brick, x0, x1, G, eave, 0f, holes);
            k.Box(M.stone, new Vector3(x0, G + 0.45f, 0f), new Vector3(x1, G + 0.6f, 0.04f), 16);        // first-floor band
            k.Box(M.stone, new Vector3(x0, eave - 0.3f, 0f), new Vector3(x1, eave, 0.18f), 16);           // eaves cornice
            SidesAndBack(k, M.brick, x0, x1, eave, rng, true);
            float ridge = Roof(k, x0, x1, eave, 35f, M.brick);
            Chimney(k, x0 + 0.5f, ridge, -Depth * 0.5f);
            return eave;
        }

        /// <summary>1930s: white render, a flat roof behind a stepped parapet, speed-line banding, wide metal windows.</summary>
        static float Thirties(MeshKit k, float x0, float x1, Unit u, System.Random rng)
        {
            const float G = 3.9f, FH = 3.0f; float top = G + (u.storeys - 1) * FH, w = x1 - x0;
            Shopfront(k, u.shop, x0, x1, G);
            var holes = new List<Rect>();
            for (int f = 1; f < u.storeys; f++)
            {
                float wy0 = G + (f - 1) * FH + 0.8f; var r = new Rect(x0 + 0.5f, wy0, w - 1f, 1.4f); holes.Add(r);
                Win(k, M.windows, AtlasStrip(rng.Next(3)), r.xMin, r.xMax, r.yMin, r.yMax, 0f, M.render, true, false);
            }
            WallHoles(k, M.render, x0, x1, G, top + 0.9f, 0f, holes);
            for (int b = 0; b < 3; b++) k.Box(M.render, new Vector3(x0, top + 0.1f + b * 0.2f, 0f), new Vector3(x1, top + 0.18f + b * 0.2f, 0.05f), 16);
            float cx = (x0 + x1) * 0.5f;
            k.Box(M.render, new Vector3(cx - 1.2f, top + 0.9f, -0.3f), new Vector3(cx + 1.2f, top + 1.7f, 0f), 4);   // stepped centre
            k.Box(M.render, new Vector3(cx - 0.6f, top + 1.7f, -0.3f), new Vector3(cx + 0.6f, top + 2.1f, 0f), 4);
            SidesAndBack(k, M.render, x0, x1, top + 0.9f, rng, false);
            k.Quad(M.felt, new Vector3(x0, top + 0.5f, 0f), new Vector3(x1, top + 0.5f, 0f), new Vector3(x1, top + 0.5f, -Depth), new Vector3(x0, top + 0.5f, -Depth), Vector3.up);
            return top + 0.9f;
        }

        static Rect AtlasStrip(int v) { var r = CouncilEstate.AtlasUpper(v); return r; }

        /// <summary>1960s infill: buff brick ground floor, concrete panels and a continuous window band above, a flat roof, a deep canopy.</summary>
        static float Sixties(MeshKit k, float x0, float x1, Unit u, System.Random rng)
        {
            const float G = 3.6f, FH = 3.0f; float top = G + (u.storeys - 1) * FH;
            Shopfront(k, u.shop, x0, x1, G);
            var holes = new List<Rect>();
            for (int f = 1; f < u.storeys; f++)
            {
                var r = new Rect(x0 + 0.3f, G + (f - 1) * FH + 0.9f, x1 - x0 - 0.6f, 1.5f); holes.Add(r);
                Win(k, M.windows, AtlasStrip(rng.Next(3)), r.xMin, r.xMax, r.yMin, r.yMax, 0f, M.concrete, true, false);
            }
            WallHoles(k, M.concrete, x0, x1, G, top + 0.5f, 0f, holes);
            k.Box(M.concrete, new Vector3(x0, G - 0.1f, 0f), new Vector3(x1, G + 0.15f, 1.4f));      // canopy over the shopfront
            k.Box(M.felt, new Vector3(x0 - 0.05f, top + 0.4f, -0.05f), new Vector3(x1 + 0.05f, top + 0.6f, 0.15f));
            SidesAndBack(k, M.buff, x0, x1, top + 0.5f, rng, false);
            k.Quad(M.felt, new Vector3(x0, top + 0.45f, 0f), new Vector3(x1, top + 0.45f, 0f), new Vector3(x1, top + 0.45f, -Depth), new Vector3(x0, top + 0.45f, -Depth), Vector3.up);
            return top + 0.6f;
        }

        /// <summary>The Eggy Duck: Victorian corner pub. Green glazed tiles and big windows downstairs, the name across the front, red brick
        /// and sashes above, a steep slate roof with dormers and chimneys, and the painted sign hanging off the corner.</summary>
        static float Pub(MeshKit k, float x0, float x1, System.Random rng)
        {
            const float G = 4.0f, eave = 7.2f; float w = x1 - x0;
            // ground floor: tiled, with three big windows and the door
            var holes = new List<Rect>(); var front = Front(14);
            float[] wins = { x0 + 0.8f, x0 + 5.6f, x0 + 10.6f };
            foreach (float wx in wins) holes.Add(new Rect(wx, 0.7f, 4.2f, 2.5f));
            var winUV = new Rect(front.x, front.y + front.height * 0.18f, front.width * 0.64f, front.height * 0.78f);
            foreach (float wx in wins) Win(k, M.shops, winUV, wx, wx + 4.2f, 0.7f, 3.2f, 0f, M.paint, true, false);
            WallHoles(k, M.tiles, x0, x1, 0f, G - 0.7f, 0f, holes);
            // the name board across the middle of the front, plain painted board either side
            k.Box(M.paint, new Vector3(x0, G - 0.7f, 0f), new Vector3(x1, G, 0.14f), 16);
            float cxp = (x0 + x1) * 0.5f; Face(k, M.shops, cxp - 2.3f, cxp + 2.3f, G - 0.67f, G - 0.03f, 0.141f, Fascia(14));
            k.Box(M.stone, new Vector3(x0, G, 0f), new Vector3(x1, G + 0.15f, 0.25f), 16);
            // first floor: brick with four tall sashes in stone surrounds
            var up = new List<Rect>();
            for (int i = 0; i < 4; i++) { float wx = x0 + 1.4f + i * (w - 2.8f - 1.0f) / 3f; up.Add(new Rect(wx, G + 0.7f, 1.0f, 1.8f)); Win(k, M.sash, SashUV(rng.Next(3)), wx, wx + 1.0f, G + 0.7f, G + 2.5f, 0f, M.brick, true, true); }
            WallHoles(k, M.brick, x0, x1, G + 0.15f, eave, 0f, up);
            k.Box(M.stone, new Vector3(x0, eave - 0.35f, 0f), new Vector3(x1, eave, 0.2f), 16);
            // the corner end (x0, by Abbots Wend): tiles and a door downstairs, brick above, sign bracket
            var endHoles = new List<Rect> { new Rect(-2.4f, 0f, 1.2f, 2.5f) };
            k.Push(Matrix4x4.TRS(new Vector3(x0, 0, 0), Quaternion.Euler(0, -90f, 0), Vector3.one)); // local x runs from the back (-Depth) to the front (0); the wall faces out of the end
            WallHoles(k, M.tiles, -Depth, 0f, 0f, G, 0f, endHoles);
            Face(k, M.shops, -2.4f, -1.2f, 0f, 2.5f, -0.1f, new Rect(front.x + front.width * 0.7f, front.y, front.width * 0.25f, front.height * 0.9f));
            WallHoles(k, M.brick, -Depth, 0f, G, eave, 0f, new List<Rect>());
            k.Pop();
            // sign on an iron bracket off the corner, readable from both sides
            Vector3 b0 = new Vector3(x0 - 0.05f, G + 1.6f, 0.05f);
            k.Box(M.iron, b0 + new Vector3(-0.06f, -0.04f, 0f), b0 + new Vector3(0f, 0.04f, 1.3f));
            k.Box(M.iron, b0 + new Vector3(-0.05f, -0.6f, 0f), b0 + new Vector3(0f, 0.0f, 0.05f));
            Vector3 s0 = b0 + new Vector3(-0.03f, -0.1f, 0.25f);
            k.QuadUV(M.sign, s0 + new Vector3(0.02f, -1.25f, 0f), s0 + new Vector3(0.02f, -1.25f, 1.0f), s0 + new Vector3(0.02f, 0f, 1.0f), s0 + new Vector3(0.02f, 0f, 0f), Vector3.right,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            k.QuadUV(M.sign, s0 + new Vector3(-0.02f, -1.25f, 1.0f), s0 + new Vector3(-0.02f, -1.25f, 0f), s0 + new Vector3(-0.02f, 0f, 0f), s0 + new Vector3(-0.02f, 0f, 1.0f), Vector3.left,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            // sides, back, roof, dormers, chimneys, lanterns
            k.Quad(M.brick, new Vector3(x1, 0, 0), new Vector3(x1, eave, 0), new Vector3(x1, eave, -Depth), new Vector3(x1, 0, -Depth), Vector3.right);
            var back = new List<Rect> { new Rect(x0 + 2f, 0f, 1.6f, 2.3f) };
            for (int i = 0; i < 3; i++) back.Add(new Rect(x0 + 3f + i * 4.5f, G + 0.7f, 1.0f, 1.7f));
            WallHoles(k, M.brick, x0, x1, 0f, eave, -Depth, back, false);
            foreach (var r in back) if (r.yMin > 1f) Win(k, M.sash, SashUV(3), r.xMin, r.xMax, r.yMin, r.yMax, -Depth, M.brick, false, false);
            k.QuadUV(M.paint, new Vector3(x0 + 3.6f, 0f, -Depth + 0.1f), new Vector3(x0 + 2f, 0f, -Depth + 0.1f), new Vector3(x0 + 2f, 2.3f, -Depth + 0.1f), new Vector3(x0 + 3.6f, 2.3f, -Depth + 0.1f), Vector3.back,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            float ridge = Roof(k, x0, x1, eave, 42f, M.brick);
            for (int i = 0; i < 2; i++)
            {
                float dxm = x0 + w * (0.3f + i * 0.4f);
                k.Box(M.brick, new Vector3(dxm - 0.8f, eave, -1.4f), new Vector3(dxm + 0.8f, eave + 1.9f, -0.5f));
                Win(k, M.sash, SashUV(2), dxm - 0.45f, dxm + 0.45f, eave + 0.3f, eave + 1.6f, -0.5f, M.brick, true, false);
                k.Box(M.slate, new Vector3(dxm - 0.95f, eave + 1.9f, -1.6f), new Vector3(dxm + 0.95f, eave + 2.1f, -0.35f));
            }
            Chimney(k, x0 + 0.6f, ridge, -Depth * 0.5f); Chimney(k, x1 - 0.6f, ridge, -Depth * 0.5f);
            foreach (float lx in new[] { x0 + 0.4f, x1 - 0.4f })
            {
                k.Box(M.iron, new Vector3(lx - 0.03f, G - 1.1f, 0f), new Vector3(lx + 0.03f, G - 1.05f, 0.45f));
                k.Box(E("EstateLampGlow") ?? M.white, new Vector3(lx - 0.12f, G - 1.5f, 0.35f), new Vector3(lx + 0.12f, G - 1.1f, 0.59f));
            }
            return eave;
        }

        /// <summary>The beer garden behind the pub: a low brick wall round it, picnic benches with parasols, a gate onto the side passage.</summary>
        static void BeerGarden(Transform parent, float x0, float w)
        {
            var k = new MeshKit(); const float d = 13f; float z0 = -Depth, z1 = -Depth - d, x1 = x0 + w;
            void Wall(Vector3 a, Vector3 b) { k.Box(M.brick, Vector3.Min(a, b) - new Vector3(0.12f, 0, 0.12f), Vector3.Max(a, b) + new Vector3(0.12f, 1.1f, 0.12f)); k.Box(M.stone, Vector3.Min(a, b) - new Vector3(0.16f, -1.1f, 0.16f), Vector3.Max(a, b) + new Vector3(0.16f, 1.2f, 0.16f)); }
            Wall(new Vector3(x0, 0, z0), new Vector3(x0, 0, z1)); Wall(new Vector3(x0, 0, z1), new Vector3(x1, 0, z1)); Wall(new Vector3(x1, 0, z1), new Vector3(x1, 0, z0 - 3f));
            k.Box(M.stone, new Vector3(x0 + 0.5f, 0f, z0 - 3f), new Vector3(x1 - 0.5f, 0.06f, z0), 0);   // patio by the back door
            var rng = new System.Random(3);
            for (int i = 0; i < 4; i++)
            {
                var c = new Vector3(x0 + 3f + (i % 2) * 7f + (float)rng.NextDouble(), 0f, z0 - 5f - (i / 2) * 4.5f);
                k.Box(M.timber, c + new Vector3(-0.9f, 0.72f, -0.4f), c + new Vector3(0.9f, 0.77f, 0.4f));
                foreach (float s in new[] { -0.7f, 0.7f }) k.Box(M.timber, c + new Vector3(-0.9f, 0.43f, s - 0.13f), c + new Vector3(0.9f, 0.47f, s + 0.13f));
                foreach (float sx in new[] { -0.75f, 0.75f }) foreach (float sz in new[] { -0.6f, 0.6f }) k.Box(M.timber, c + new Vector3(sx - 0.04f, 0f, sz - 0.04f), c + new Vector3(sx + 0.04f, 0.72f, sz + 0.04f));
                if (i % 2 == 0)
                {
                    k.Cyl(M.white, c + Vector3.up * 0.77f, 0.025f, 1.6f, 6);
                    k.Cyl(i == 0 ? M.paint : M.frame, c + Vector3.up * 2.2f, 1.3f, 0.25f, 12, true, 0.08f);
                }
            }
            var g = k.Build("BeerGarden", parent, true);
        }
    }
}
