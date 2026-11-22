using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// A building kit for the town's public buildings (police and fire stations, the hospital, the precinct, the
    /// industrial units): outside walls with real openings (windows with glass and frames, doors), floor slabs and
    /// ceilings, interior partitions with doorways, and the furniture that makes a room read as what it is (counters,
    /// desks with computers, plastic chairs, beds with curtains, lockers, cells, kitchens). Built in world space on the
    /// axes. Three meshes per building: the outside (casts shadows), the inside (no shadows), the glass; plus box
    /// colliders for every wall piece so doorways stay walkable.
    /// </summary>
    public class Bld
    {
        public readonly MeshKit outK = new MeshKit(), inK = new MeshKit(), glassK = new MeshKit(), furnK = new MeshKit();
        /// <summary>Furniture and small details: on a layer the camera stops drawing beyond a set distance (Perf).</summary>
        public const int DetailLayer = 27;
        readonly Transform g; readonly List<(Vector3 mn, Vector3 mx)> solid = new List<(Vector3, Vector3)>();
        readonly List<(Vector3 p, Color c, float r)> lights = new List<(Vector3, Color, float)>();
        public static CouncilEstate.Mats E => CouncilEstate.M;
        public const float WallT = 0.3f, PartT = 0.12f;

        public Bld(Transform parent, string name) { g = new GameObject(name).transform; g.SetParent(parent, false); }
        public Transform Root => g;

        public struct Hole { public float s0, s1, y0, y1; public bool door, frosted, atlas; public int cell; }
        public static Hole Win(float s0, float s1, float y0 = 0.9f, float y1 = 2.3f, bool frosted = false) => new Hole { s0 = s0, s1 = s1, y0 = y0, y1 = y1, frosted = frosted };
        /// <summary>A window you can't see into (floors nobody goes up to): a unit from the tower window atlas, blinds, nets or curtains.</summary>
        public static Hole Blind(float s0, float s1, float y0, float y1, int cell) => new Hole { s0 = s0, s1 = s1, y0 = y0, y1 = y1, atlas = true, cell = cell };
        static readonly int[] OfficeCells = { 3, 3, 3, 0, 4, 11, 3, 5 };
        /// <summary>Office-ish window units for upper floors (mostly blinds), picked by position.</summary>
        public static int OfficeCell(float s, float y) => OfficeCells[Mathf.Abs(Mathf.RoundToInt(s * 7.3f + y * 3.1f)) % OfficeCells.Length];
        public static Hole Door(float s0, float s1, float h = 2.2f) => new Hole { s0 = s0, s1 = s1, y0 = 0f, y1 = h, door = true };

        /// <summary>
        /// An outside wall from a to b (on the ground, any axis-aligned direction) from y0 to y1, its outside face
        /// towards 'outward', with openings at distances along it. Thickness goes inwards. Windows get glass and a frame.
        /// </summary>
        public void Wall(Vector3 a, Vector3 b, Vector3 outward, float y0, float y1, Material outside, Material inside, params Hole[] holes)
        {
            a = a.WithY(0); b = b.WithY(0); Vector3 d = (b - a); float len = d.magnitude; d /= len; Vector3 n = outward.normalized, inw = -n * WallT;
            var hs = new List<Hole>(holes); hs.Sort((p, q) => p.s0.CompareTo(q.s0));
            void Rect(float s0, float s1, float ya, float yb)
            {
                if (s1 - s0 < 0.01f || yb - ya < 0.01f) return;
                Vector3 p0 = a + d * s0, p1 = a + d * s1;
                outK.Quad(outside, p0 + Vector3.up * ya, p1 + Vector3.up * ya, p1 + Vector3.up * yb, p0 + Vector3.up * yb, n);
                if (inside != null) inK.Quad(inside, p0 + inw + Vector3.up * ya, p1 + inw + Vector3.up * ya, p1 + inw + Vector3.up * yb, p0 + inw + Vector3.up * yb, -n);
                solid.Add((Vector3.Min(p0, p1 + inw) + Vector3.up * ya, Vector3.Max(p0, p1 + inw) + Vector3.up * yb));
            }
            float s = 0f;
            foreach (var h in hs)
            {
                Rect(s, h.s0, y0, y1);                          // the wall up to the opening
                Rect(h.s0, h.s1, y0, Mathf.Max(y0, h.y0));      // under it (sill)
                Rect(h.s0, h.s1, Mathf.Min(y1, h.y1), y1);      // over it (lintel)
                Vector3 p0 = a + d * h.s0, p1 = a + d * h.s1;
                // reveals round the opening
                foreach (var (pp, side) in new[] { (p0, d), (p1, -d) }) outK.Quad(outside, pp + Vector3.up * h.y0, pp + inw + Vector3.up * h.y0, pp + inw + Vector3.up * h.y1, pp + Vector3.up * h.y1, side);
                outK.Quad(outside, p0 + Vector3.up * h.y1, p1 + Vector3.up * h.y1, p1 + inw + Vector3.up * h.y1, p0 + inw + Vector3.up * h.y1, Vector3.down);
                if (!h.door) outK.Quad(E.white, p0 + Vector3.up * h.y0 + n * 0.06f, p1 + Vector3.up * h.y0 + n * 0.06f, p1 + inw + Vector3.up * h.y0, p0 + inw + Vector3.up * h.y0, Vector3.up); // sill
                if (h.atlas)
                {
                    Vector3 m = n * -0.06f; var c = h.cell; var uv = new Rect((c % 4) * 0.25f + 0.004f, 1f - (c / 4 + 1) * 0.25f + 0.004f, 0.242f, 0.242f);
                    outK.QuadUV(TownMats.towerWin, p0 + m + Vector3.up * h.y0, p1 + m + Vector3.up * h.y0, p1 + m + Vector3.up * h.y1, p0 + m + Vector3.up * h.y1, n,
                        new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax));
                }
                else if (!h.door)
                {
                    // glass set in a white frame, a mullion in the middle of wide ones
                    Vector3 m = n * -0.08f;
                    glassK.Quad(h.frosted ? TownMats.panel : TownMats.glass, p0 + m + Vector3.up * h.y0, p1 + m + Vector3.up * h.y0, p1 + m + Vector3.up * h.y1, p0 + m + Vector3.up * h.y1, n);
                    float fw = 0.05f;
                    Frame(p0 + m, p1 + m, h.y0, h.y1, fw, n, d);
                    if (h.s1 - h.s0 > 1.4f) { Vector3 c = (p0 + p1) * 0.5f + m; outK.Box(E.white, Vector3.Min(c - d * fw, c + d * fw) + Vector3.up * h.y0 - Vabs(n) * 0.03f, Vector3.Max(c - d * fw, c + d * fw) + Vector3.up * h.y1 + Vabs(n) * 0.03f); }
                }
                s = h.s1;
            }
            Rect(s, len, y0, y1);
        }
        static Vector3 Vabs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        void Frame(Vector3 p0, Vector3 p1, float y0, float y1, float w, Vector3 n, Vector3 d)
        {
            Vector3 t = Vabs(n) * 0.04f;
            outK.Box(E.white, Vector3.Min(p0, p1) + Vector3.up * y0 - t, Vector3.Max(p0, p1) + Vector3.up * (y0 + w) + t);
            outK.Box(E.white, Vector3.Min(p0, p1) + Vector3.up * (y1 - w) - t, Vector3.Max(p0, p1) + Vector3.up * y1 + t);
            foreach (var p in new[] { p0, p1 }) outK.Box(E.white, Vector3.Min(p - d * w, p + d * w) + Vector3.up * y0 - t, Vector3.Max(p - d * w, p + d * w) + Vector3.up * y1 + t);
        }

        /// <summary>The four outside walls of a box footprint, each with its own openings (S, E, N, W = -z, +x, +z, -x faces).</summary>
        public void Shell(Rect r, float y0, float y1, Material outside, Material inside, Hole[] south, Hole[] east, Hole[] north, Hole[] west)
        {
            Wall(new Vector3(r.xMin, 0, r.yMin), new Vector3(r.xMax, 0, r.yMin), Vector3.back, y0, y1, outside, inside, south ?? new Hole[0]);
            Wall(new Vector3(r.xMax, 0, r.yMin), new Vector3(r.xMax, 0, r.yMax), Vector3.right, y0, y1, outside, inside, east ?? new Hole[0]);
            Wall(new Vector3(r.xMax, 0, r.yMax), new Vector3(r.xMin, 0, r.yMax), Vector3.forward, y0, y1, outside, inside, north ?? new Hole[0]);
            Wall(new Vector3(r.xMin, 0, r.yMax), new Vector3(r.xMin, 0, r.yMin), Vector3.left, y0, y1, outside, inside, west ?? new Hole[0]);
        }

        /// <summary>A flat roof with a parapet and coping over a footprint (top of the walls at y).</summary>
        public void Roof(Rect r, float y, float parapet = 0.6f, Material coping = null)
        {
            coping ??= TownMats.panel;
            outK.Quad(E.felt != null ? E.felt : E.black, new Vector3(r.xMin, y + 0.05f, r.yMin), new Vector3(r.xMax, y + 0.05f, r.yMin), new Vector3(r.xMax, y + 0.05f, r.yMax), new Vector3(r.xMin, y + 0.05f, r.yMax), Vector3.up);
            float t = WallT;
            outK.Box(coping, new Vector3(r.xMin, y, r.yMin), new Vector3(r.xMax, y + parapet, r.yMin + t));
            outK.Box(coping, new Vector3(r.xMin, y, r.yMax - t), new Vector3(r.xMax, y + parapet, r.yMax));
            outK.Box(coping, new Vector3(r.xMin, y, r.yMin), new Vector3(r.xMin + t, y + parapet, r.yMax));
            outK.Box(coping, new Vector3(r.xMax - t, y, r.yMin), new Vector3(r.xMax, y + parapet, r.yMax));
        }

        /// <summary>A floor (top) and the ceiling of the floor below it (bottom) over a rect.</summary>
        public void Slab(Rect r, float y, Material floor, Material ceiling, float thick = 0.25f)
        {
            if (floor != null) { inK.Quad(floor, new Vector3(r.xMin, y, r.yMin), new Vector3(r.xMax, y, r.yMin), new Vector3(r.xMax, y, r.yMax), new Vector3(r.xMin, y, r.yMax), Vector3.up); solid.Add((new Vector3(r.xMin, y - thick, r.yMin), new Vector3(r.xMax, y, r.yMax))); }
            if (ceiling != null) inK.Quad(ceiling, new Vector3(r.xMin, y - thick, r.yMin), new Vector3(r.xMax, y - thick, r.yMin), new Vector3(r.xMax, y - thick, r.yMax), new Vector3(r.xMin, y - thick, r.yMax), Vector3.down);
        }

        /// <summary>An inside wall from a to b with doorways (centre along the wall, width) cut through it.</summary>
        public void Part(Vector3 a, Vector3 b, float y0, float h, Material m, params (float at, float w)[] doors)
        {
            a = a.WithY(0); b = b.WithY(0); Vector3 d = (b - a); float len = d.magnitude; d /= len; Vector3 sd = new Vector3(-d.z, 0, d.x) * (PartT * 0.5f);
            var ds = new List<(float at, float w)>(doors); ds.Sort((p, q) => p.at.CompareTo(q.at));
            void Seg(float s0, float s1, float ya, float yb)
            {
                if (s1 - s0 < 0.01f) return;
                Vector3 p0 = a + d * s0, p1 = a + d * s1;
                var mn = Vector3.Min(p0 - sd, p1 + sd) + Vector3.up * ya; var mx = Vector3.Max(p0 - sd, p1 + sd) + Vector3.up * yb;
                inK.Box(m, mn, mx); solid.Add((mn, mx));
            }
            float s = 0f;
            foreach (var (at, w) in ds)
            {
                Seg(s, at - w * 0.5f, y0, y0 + h); Seg(at - w * 0.5f, at + w * 0.5f, y0 + 2.15f, y0 + h);
                // a door frame
                Vector3 c0 = a + d * (at - w * 0.5f), c1 = a + d * (at + w * 0.5f);
                foreach (var c in new[] { c0, c1 }) inK.Box(E.white, Vector3.Min(c - d * 0.04f - sd * 1.3f, c + d * 0.04f + sd * 1.3f) + Vector3.up * y0, Vector3.Max(c - d * 0.04f - sd * 1.3f, c + d * 0.04f + sd * 1.3f) + Vector3.up * (y0 + 2.15f));
                s = at + w * 0.5f;
            }
            Seg(s, len, y0, y0 + h);
        }

        public void Box(Material m, Vector3 mn, Vector3 mx, bool isSolid = false, bool outside = false) { (outside ? outK : inK).Box(m, mn, mx); if (isSolid) solid.Add((mn, mx)); }
        public void Solid(Vector3 mn, Vector3 mx) => solid.Add((mn, mx));
        public void Light(Vector3 p, Color c, float range = 8f) => lights.Add((p, c, range));
        /// <summary>A strip light in the ceiling (and a real light for every other one).</summary>
        public void Strip(Vector3 p, bool real) { furnK.Box(E.glow, p + new Vector3(-0.6f, -0.05f, -0.1f), p + new Vector3(0.6f, 0f, 0.1f)); if (real) Light(p + Vector3.down * 0.3f, new Color(1f, 0.96f, 0.9f), 9f); }

        /// <summary>A sign board from an atlas cell, flat on a wall facing n (centre, width, height).</summary>
        public void Sign(Material atlas, Rect uv, Vector3 c, Vector3 n, float w, float h, bool outside = true)
        {
            n = n.normalized; Vector3 r = Vector3.Cross(Vector3.up, n).normalized * (w * 0.5f), u = Vector3.up * (h * 0.5f), o = n * 0.02f;
            var k = outside ? outK : inK;
            k.QuadUV(atlas, c - r - u + o, c + r - u + o, c + r + u + o, c - r + u + o, n, new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMin, uv.yMax), new Vector2(uv.xMax, uv.yMax));
            Vector3 bc = c - n * 0.03f, he = Vabs(r) * 1.02f + u * 1.04f + Vabs(n) * 0.04f; // a dark backing board behind it
            k.Box(E.black, bc - he, bc + he);
        }
        /// <summary>The service signs atlas (2 x 12 cells of 1024 x 170 on 2048 x 2048).</summary>
        public static Rect SignCell(int i) { int c = i % 2, r = i / 2; float y0 = r * 170f, y1 = y0 + 170f; return new Rect(c * 0.5f + 0.001f, 1f - y1 / 2048f, 0.498f, 170f / 2048f); }

        // ------------------------------------------------------------------ furniture
        public void Desk(Vector3 p, float yaw, bool pc = true)
        {
            furnK.Push(p, yaw);
            furnK.Box(E.fence, new Vector3(-0.7f, 0.72f, -0.35f), new Vector3(0.7f, 0.76f, 0.35f));
            foreach (float x in new[] { -0.65f, 0.65f }) furnK.Box(TownMats.steel, new Vector3(x - 0.03f, 0f, -0.32f), new Vector3(x + 0.03f, 0.72f, 0.32f));
            furnK.Box(TownMats.panel, new Vector3(0.2f, 0.1f, -0.3f), new Vector3(0.6f, 0.7f, 0.3f)); // drawers
            if (pc)
            {
                furnK.Box(E.black, new Vector3(-0.25f, 0.8f, 0.15f), new Vector3(0.25f, 1.12f, 0.18f)); furnK.Box(E.black, new Vector3(-0.04f, 0.76f, 0.17f), new Vector3(0.04f, 0.8f, 0.22f));
                furnK.Box(TownMats.glassDark, new Vector3(-0.23f, 0.82f, 0.145f), new Vector3(0.23f, 1.1f, 0.15f));
                furnK.Box(E.black, new Vector3(-0.22f, 0.76f, -0.15f), new Vector3(0.22f, 0.78f, 0.0f)); // keyboard
            }
            furnK.Pop();
            Chair(p + Quaternion.Euler(0, yaw, 0) * new Vector3(0f, 0f, -0.7f), yaw);
            solid.Add((p - new Vector3(0.75f, 0f, 0.75f), p + new Vector3(0.75f, 0.76f, 0.75f)));
        }
        public void Chair(Vector3 p, float yaw, Material seat = null)
        {
            seat ??= TownMats.nhsBlue != null ? TownMats.nhsBlue : E.black;
            furnK.Push(p, yaw);
            furnK.Box(seat, new Vector3(-0.22f, 0.42f, -0.22f), new Vector3(0.22f, 0.47f, 0.22f));
            furnK.Box(seat, new Vector3(-0.22f, 0.47f, -0.26f), new Vector3(0.22f, 0.9f, -0.22f));
            foreach (float x in new[] { -0.19f, 0.19f }) foreach (float z in new[] { -0.19f, 0.19f }) furnK.Box(TownMats.steel, new Vector3(x - 0.015f, 0f, z - 0.015f), new Vector3(x + 0.015f, 0.42f, z + 0.015f));
            furnK.Pop();
        }
        /// <summary>A row of joined plastic waiting-room seats on a steel beam.</summary>
        public void Seats(Vector3 a, float yaw, int n, Material seat = null)
        {
            seat ??= TownMats.nhsBlue != null ? TownMats.nhsBlue : E.black;
            furnK.Push(a, yaw);
            furnK.Box(TownMats.steel, new Vector3(-0.1f, 0.3f, -0.05f), new Vector3(n * 0.52f + 0.1f, 0.36f, 0.05f));
            foreach (float x in new[] { 0f, n * 0.52f }) furnK.Box(TownMats.steel, new Vector3(x - 0.03f, 0f, -0.2f), new Vector3(x + 0.03f, 0.36f, 0.2f));
            for (int i = 0; i < n; i++)
            {
                float x = i * 0.52f + 0.04f;
                furnK.Box(seat, new Vector3(x, 0.4f, -0.22f), new Vector3(x + 0.44f, 0.45f, 0.2f));
                furnK.Box(seat, new Vector3(x, 0.45f, 0.18f), new Vector3(x + 0.44f, 0.85f, 0.23f));
            }
            furnK.Pop();
        }
        public void Table(Vector3 p, float w, float d, float h = 0.74f, Material top = null)
        {
            top ??= E.fence;
            furnK.Box(top, p + new Vector3(-w / 2, h - 0.04f, -d / 2), p + new Vector3(w / 2, h, d / 2));
            foreach (float x in new[] { -w / 2 + 0.06f, w / 2 - 0.06f }) foreach (float z in new[] { -d / 2 + 0.06f, d / 2 - 0.06f }) furnK.Box(TownMats.steel, p + new Vector3(x - 0.025f, 0f, z - 0.025f), p + new Vector3(x + 0.025f, h - 0.04f, z + 0.025f));
            solid.Add((p + new Vector3(-w / 2, 0f, -d / 2), p + new Vector3(w / 2, h, d / 2)));
        }
        /// <summary>A counter with a glass screen on top (front counter, A&E reception), length along x (yaw turns it).</summary>
        public void Counter(Vector3 p, float yaw, float len, bool screen)
        {
            furnK.Push(p, yaw);
            furnK.Box(TownMats.panelDark, new Vector3(-len / 2, 0f, -0.3f), new Vector3(len / 2, 1.05f, 0.3f));
            furnK.Box(E.fence, new Vector3(-len / 2 - 0.03f, 1.05f, -0.36f), new Vector3(len / 2 + 0.03f, 1.1f, 0.36f));
            furnK.Pop();
            if (screen) { glassK.Push(p, yaw); glassK.Box(TownMats.glass, new Vector3(-len / 2, 1.1f, -0.02f), new Vector3(len / 2, 2.1f, 0.02f)); glassK.Pop(); }
            var q = Quaternion.Euler(0, yaw, 0); Vector3 e = Vabs(q * new Vector3(len / 2, 0, 0.36f));
            solid.Add((p - e, p + e + Vector3.up * (screen ? 2.1f : 1.1f)));
        }
        public void Lockers(Vector3 a, float yaw, int n, Material m = null)
        {
            m ??= TownMats.steel;
            furnK.Push(a, yaw);
            for (int i = 0; i < n; i++)
            {
                float x = i * 0.42f;
                furnK.Box(m, new Vector3(x, 0f, 0f), new Vector3(x + 0.4f, 1.85f, 0.45f));
                furnK.Box(E.black, new Vector3(x + 0.05f, 1.55f, -0.005f), new Vector3(x + 0.35f, 1.6f, 0f)); // vents
                furnK.Box(E.black, new Vector3(x + 0.32f, 0.95f, -0.02f), new Vector3(x + 0.35f, 1.05f, 0f));  // handle
            }
            furnK.Pop();
        }
        /// <summary>A hospital bed with a cellular blanket, a pillow, a drip stand and (if curtains) a curtained bay round it.</summary>
        public void Bed(Vector3 p, float yaw, bool curtains)
        {
            furnK.Push(p, yaw);
            furnK.Box(TownMats.steel, new Vector3(-0.45f, 0.3f, -1f), new Vector3(0.45f, 0.4f, 1f));
            furnK.Box(TownMats.bed != null ? TownMats.bed : E.white, new Vector3(-0.44f, 0.4f, -0.98f), new Vector3(0.44f, 0.58f, 0.98f));
            furnK.Box(E.white, new Vector3(-0.3f, 0.58f, 0.6f), new Vector3(0.3f, 0.7f, 0.92f));
            furnK.Box(TownMats.steel, new Vector3(-0.45f, 0.3f, 0.98f), new Vector3(0.45f, 0.95f, 1.02f));
            foreach (float x in new[] { -0.4f, 0.4f }) foreach (float z in new[] { -0.9f, 0.9f }) furnK.Box(E.black, new Vector3(x - 0.03f, 0f, z - 0.03f), new Vector3(x + 0.03f, 0.3f, z + 0.03f));
            furnK.Box(TownMats.steel, new Vector3(0.65f, 0f, 0.7f), new Vector3(0.68f, 1.9f, 0.73f)); furnK.Box(TownMats.glass, new Vector3(0.6f, 1.6f, 0.68f), new Vector3(0.72f, 1.8f, 0.75f));
            if (curtains)
            {
                // ceiling rail and a curtain half drawn round
                var cm = TownMats.curtain != null ? TownMats.curtain : TownMats.nhsBlue;
                furnK.Box(cm, new Vector3(-1.25f, 0.25f, -1.4f), new Vector3(-1.22f, 2.3f, 1.4f));
                furnK.Box(cm, new Vector3(1.22f, 0.25f, -1.4f), new Vector3(1.25f, 2.3f, 0.2f));
                furnK.Box(TownMats.steel, new Vector3(-1.25f, 2.4f, -1.42f), new Vector3(1.25f, 2.43f, -1.38f));
            }
            furnK.Pop();
            solid.Add((p - new Vector3(0.6f, 0f, 0.6f) - Vabs(Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, 1f)), p + new Vector3(0.6f, 0.7f, 0.6f) + Vabs(Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, 1f))));
        }
        /// <summary>A custody cell: concrete bench bed with a blue mattress, a steel toilet, a heavy door with a wicket hatch (the door faces -n).</summary>
        public void Cell(Rect r, Vector3 doorAt, Vector3 into)
        {
            var mat = TownMats.nhsBlue != null ? TownMats.nhsBlue : E.black;
            Vector3 back = new Vector3(r.center.x, 0, r.center.y) + into.normalized * (Vabs(into.normalized).x > 0.5f ? r.width : r.height) * 0.35f;
            Vector3 side = Vector3.Cross(Vector3.up, into.normalized);
            Vector3 b0 = back - side * 0.4f - into.normalized * 0.4f, b1 = back + side * 0.9f + into.normalized * 0.4f;
            furnK.Box(TownMats.panel, Vector3.Min(b0, b1), Vector3.Max(b0, b1) + Vector3.up * 0.45f);
            furnK.Box(mat, Vector3.Min(b0, b1) + Vector3.up * 0.45f, Vector3.Max(b0, b1) + Vector3.up * 0.55f);
            Vector3 t = back - side * 1.0f; furnK.Box(TownMats.steel, t - new Vector3(0.2f, 0, 0.2f), t + new Vector3(0.2f, 0.42f, 0.2f));
            // the door: heavy, grey-blue, with a hatch and a spy hole
            Vector3 dn = into.normalized; Vector3 ds = side * 0.5f;
            furnK.Box(TownMats.panelDark, Vector3.Min(doorAt - ds - dn * 0.03f, doorAt + ds + dn * 0.05f), Vector3.Max(doorAt - ds - dn * 0.03f, doorAt + ds + dn * 0.05f) + Vector3.up * 2.1f);
            furnK.Box(E.black, Vector3.Min(doorAt - side * 0.2f - dn * 0.05f, doorAt + side * 0.2f) + Vector3.up * 1.05f, Vector3.Max(doorAt - side * 0.2f - dn * 0.05f, doorAt + side * 0.2f) + Vector3.up * 1.3f);
            solid.Add((Vector3.Min(doorAt - ds - dn * 0.03f, doorAt + ds + dn * 0.05f), Vector3.Max(doorAt - ds - dn * 0.03f, doorAt + ds + dn * 0.05f) + Vector3.up * 2.1f));
        }
        public void Kitchen(Vector3 a, Vector3 along, float len)
        {
            along = along.normalized; Vector3 dep = Vector3.Cross(along, Vector3.up) * -1f; // units against the wall behind 'a'
            Vector3 b = a + along * len;
            furnK.Box(E.white, Vector3.Min(a, b + dep * 0.6f), Vector3.Max(a, b + dep * 0.6f) + Vector3.up * 0.88f);
            furnK.Box(TownMats.panelDark, Vector3.Min(a, b + dep * 0.62f) + Vector3.up * 0.88f, Vector3.Max(a, b + dep * 0.62f) + Vector3.up * 0.92f);
            furnK.Box(E.white, Vector3.Min(a, b + dep * 0.35f) + Vector3.up * 1.45f, Vector3.Max(a, b + dep * 0.35f) + Vector3.up * 2.1f);
            Vector3 k = a + along * (len * 0.7f) + dep * 0.3f; furnK.Box(TownMats.steel, k - Vabs(along) * 0.3f - Vabs(dep) * 0.22f + Vector3.up * 0.9f, k + Vabs(along) * 0.3f + Vabs(dep) * 0.22f + Vector3.up * 0.93f); // sink
            Vector3 kt = a + along * (len * 0.25f) + dep * 0.3f; furnK.Box(E.black, kt - new Vector3(0.1f, 0, 0.1f) + Vector3.up * 0.92f, kt + new Vector3(0.1f, 0.25f, 0.1f) + Vector3.up * 0.92f); // kettle
            solid.Add((Vector3.Min(a, b + dep * 0.62f), Vector3.Max(a, b + dep * 0.62f) + Vector3.up * 0.92f));
        }
        public void Sofa(Vector3 p, float yaw, Material m = null)
        {
            m ??= TownMats.redPaint != null ? TownMats.redPaint : E.black;
            furnK.Push(p, yaw);
            furnK.Box(m, new Vector3(-1f, 0f, -0.45f), new Vector3(1f, 0.45f, 0.45f)); furnK.Box(m, new Vector3(-1f, 0.45f, 0.25f), new Vector3(1f, 0.9f, 0.45f));
            foreach (float x in new[] { -1f, 0.8f }) furnK.Box(m, new Vector3(x, 0.45f, -0.45f), new Vector3(x + 0.2f, 0.65f, 0.45f));
            furnK.Pop();
            solid.Add((p - new Vector3(1f, 0f, 1f), p + new Vector3(1f, 0.5f, 1f)));
        }
        public void Telly(Vector3 p, Vector3 facing)
        {
            Vector3 s = Vector3.Cross(Vector3.up, facing.normalized) * 0.55f;
            furnK.Box(E.black, Vector3.Min(p - s, p + s - facing.normalized * 0.06f), Vector3.Max(p - s, p + s - facing.normalized * 0.06f) + Vector3.up * 0.62f);
            furnK.Box(TownMats.glassDark, Vector3.Min(p - s * 0.95f + facing.normalized * 0.005f, p + s * 0.95f) + Vector3.up * 0.03f, Vector3.Max(p - s * 0.95f + facing.normalized * 0.005f, p + s * 0.95f) + Vector3.up * 0.59f);
        }
        public void Vending(Vector3 p, float yaw)
        {
            furnK.Push(p, yaw);
            furnK.Box(TownMats.redPaint != null ? TownMats.redPaint : E.black, new Vector3(-0.45f, 0f, -0.4f), new Vector3(0.45f, 1.85f, 0.4f));
            furnK.Box(TownMats.glassDark, new Vector3(-0.38f, 0.6f, -0.405f), new Vector3(0.18f, 1.75f, -0.4f));
            for (int r = 0; r < 5; r++) for (int c = 0; c < 4; c++) furnK.Box(r % 2 == 0 ? E.white : TownMats.hiVis, new Vector3(-0.34f + c * 0.13f, 0.7f + r * 0.21f, -0.41f), new Vector3(-0.26f + c * 0.13f, 0.82f + r * 0.21f, -0.404f));
            furnK.Pop();
            solid.Add((p - new Vector3(0.5f, 0f, 0.5f), p + new Vector3(0.5f, 1.85f, 0.5f)));
        }
        public void Shelf(Vector3 a, Vector3 b, float h, Material m = null)
        {
            m ??= TownMats.steel; Vector3 d = (b - a).normalized, s = Vector3.Cross(Vector3.up, d) * 0.2f;
            for (float y = 0.1f; y < h; y += 0.45f) furnK.Box(m, Vector3.Min(a - s, b + s) + Vector3.up * y, Vector3.Max(a - s, b + s) + Vector3.up * (y + 0.03f));
            foreach (var p in new[] { a, b }) furnK.Box(m, Vector3.Min(p - s, p + s) - Vabs(d) * 0.02f, Vector3.Max(p - s, p + s) + Vabs(d) * 0.02f + Vector3.up * h);
        }
        public void Plant(Vector3 p)
        {
            furnK.Cyl(TownMats.panelDark, p, 0.25f, 0.45f, 10, true);
            furnK.Sphere(TownMats.hedge != null ? TownMats.hedge : E.railGreen, p + Vector3.up * 0.9f, new Vector3(0.4f, 0.55f, 0.4f), 8, 5);
        }
        public void Poster(Vector3 c, Vector3 n, float w, float h, Material m) { n = n.normalized; Vector3 r = Vector3.Cross(Vector3.up, n).normalized * w * 0.5f, u = Vector3.up * h * 0.5f, o = n * 0.015f; furnK.Quad(m, c - r - u + o, c + r - u + o, c + r + u + o, c - r + u + o, n); }

        /// <summary>Make the meshes, colliders and lights.</summary>
        public Transform Done()
        {
            outK.Build("Outside", g, false, ShadowCastingMode.On);
            inK.Build("Inside", g, false, ShadowCastingMode.Off);
            glassK.Build("Glass", g, false, ShadowCastingMode.Off);
            var fu = furnK.Build("Furniture", g, false, ShadowCastingMode.Off); if (fu != null) fu.layer = DetailLayer;
            var col = new GameObject("Solid").transform; col.SetParent(g, false);
            foreach (var (mn, mx) in solid)
            {
                var sz = mx - mn; if (sz.x < 0.02f || sz.y < 0.02f || sz.z < 0.02f) continue;
                var bc = col.gameObject.AddComponent<BoxCollider>(); bc.center = (mn + mx) * 0.5f - g.position; bc.size = sz;
            }
            foreach (var (p, c, r) in lights)
            {
                var lg = new GameObject("Light"); lg.transform.SetParent(g, false); lg.transform.position = p;
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = r; l.intensity = 1.1f; l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForceVertex;
            }
            return g;
        }
    }
}
