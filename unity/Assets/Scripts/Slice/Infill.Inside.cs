using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// The insides of the gap streets' single-room buildings: the snooker club (four tables under their lamps, the bar, the
    /// fruit machine, the gents), the gospel hall (pews, the lectern, the piano, the vestry with the collection), and the old
    /// engine house by the mill (the boiler, pipes, a workbench and junk). Each is lined inside its photographed walls, with
    /// the windows cut through, a real front door up its steps, and things to sit on, rummage and nick.
    /// </summary>
    public static partial class Infill
    {
        /// <summary>Two stone steps up to a door at x on the front (z = 0) of a building standing on a plinth.</summary>
        static void Steps(Area a, float x, float plinth)
        {
            a.k.Box(stone, V(x - 0.75f, 0f, 0f), V(x + 0.75f, plinth, 0.35f)); a.Solid(V(x - 0.75f, 0f, 0f), V(x + 0.75f, plinth, 0.35f));
            a.k.Box(stone, V(x - 0.85f, 0f, 0.35f), V(x + 0.85f, plinth * 0.5f, 0.7f)); a.Solid(V(x - 0.85f, 0f, 0.35f), V(x + 0.85f, plinth * 0.5f, 0.7f));
        }

        delegate void Fill(Bld b, Home h, float L, float D, float y, System.Random rng);

        /// <summary>A one-room building lined inside its walls (front z = 0 facing +z, back z = -D), the floor at y, the ceiling at ceil, the front door at doorX.</summary>
        static void HallInside(Area a, string name, float L, float D, float y, float ceil, float doorX, List<(Vector3 p0, Vector3 p1, float ya, float yb)> wins, Fill fill)
        {
            var rng = new System.Random(name.GetHashCode());
            var b = new Bld(a.root, name); var home = b.Root.gameObject.AddComponent<Home>();
            var hh = Household.Make(Mathf.Abs(name.GetHashCode()) % 90000 + 7000, rng); hh.kind = Household.Kind.Couple; hh.wealth = 0.45f; hh.stuff = 0.5f; hh.family = name;
            home.H = hh; home.label = name; home.variant = "hall";
            const float t = 0.3f;
            var paint = TownMats.lobbyWall; var floorM = HomeBuilder.Surf("wood_slats_dark", 1.4f, new Color(0.85f, 0.75f, 0.62f), 0.25f); var ceilM = HomeBuilder.Flat(new Color(0.9f, 0.89f, 0.86f), 0.1f);
            var r = new Rect(t, -D + t, L - 2 * t, D - 2 * t);
            b.Slab(r, y, floorM, null); b.Slab(r, ceil + 0.25f, null, ceilM, 0.25f); b.Solid(V(0f, ceil, -D), V(L, ceil + 0.3f, 0f));
            // the linings, with the windows through them
            var sides = new (Vector3 a, Vector3 c, Vector3 inw)[] { (V(0, 0, 0), V(L, 0, 0), Vector3.back), (V(0, 0, -D), V(L, 0, -D), Vector3.forward), (V(0, 0, 0), V(0, 0, -D), Vector3.right), (V(L, 0, 0), V(L, 0, -D), Vector3.left) };
            for (int si = 0; si < 4; si++)
            {
                var (pa, pc, inw) = sides[si]; Vector3 d = (pc - pa).normalized; float len = (pc - pa).magnitude;
                var holes = new List<Rect>();
                foreach (var (p0, p1, ya, yb) in wins)
                {
                    // on this wall?
                    if (Mathf.Abs(Vector3.Dot(p0 - pa, inw)) > 0.05f || Mathf.Abs(Vector3.Dot(p1 - pa, inw)) > 0.05f) continue;
                    float s0 = Vector3.Dot(p0 - pa, d), s1 = Vector3.Dot(p1 - pa, d); if (s1 < s0) { var q = s0; s0 = s1; s1 = q; }
                    s0 = Mathf.Max(s0, t + 0.05f); s1 = Mathf.Min(s1, len - t - 0.05f); float y0 = Mathf.Max(ya, y + 0.1f), y1 = Mathf.Min(yb, ceil - 0.1f);
                    if (s1 - s0 > 0.3f && y1 - y0 > 0.3f) holes.Add(Rect.MinMaxRect(s0, y0, s1, y1));
                }
                if (si == 0) holes.Add(Rect.MinMaxRect(doorX - 0.55f, y, doorX + 0.55f, y + 2.2f));
                Lining(b, pa, d, inw, len, y, ceil, t, paint, holes, si == 0 ? doorX : -99f);
            }
            var door = PropDoor.Make(b.Root, "door", V(doorX, y, -t * 0.5f), 0f, "front");
            if (door != null) home.doors.Add(new Home.DoorRec { door = door, tag = "front", lv = 0, tier = -1 });
            fill(b, home, L, D, y, rng);
            b.Done();
            b.Root.localPosition = a.f.GetColumn(3); b.Root.localRotation = a.f.rotation;
            home.rooms.Add(new Home.RoomRec { type = "hall", use = name, lv = 0, centre = V(L * 0.5f, y, -D * 0.5f), u0 = t, u1 = L - t, d0 = t, d1 = D - t });
            home.zoneRects.Add(Rect.MinMaxRect(t, -D + t, L - t, -t)); home.centre = V(L * 0.5f, y + 1.3f, -D * 0.5f); home.radius = Mathf.Max(L, D); home.midZ = -D * 0.5f; home.halfD = D * 0.5f;
        }

        /// <summary>The inside face of an outside wall: plaster in cells round its holes (windows get a frame, a sill and glass), a collider through the wall's thickness.</summary>
        static void Lining(Bld b, Vector3 pa, Vector3 d, Vector3 inw, float len, float y0, float y1, float t, Material m, List<Rect> holes, float doorX)
        {
            var xs = new SortedSet<float> { 0f, len }; var ys = new SortedSet<float> { y0, y1 };
            foreach (var h in holes) { xs.Add(h.xMin); xs.Add(h.xMax); ys.Add(h.yMin); ys.Add(h.yMax); }
            var X = new List<float>(xs); var Y = new List<float>(ys);
            for (int j = 0; j + 1 < Y.Count; j++)
                for (int i = 0; i + 1 < X.Count; i++)
                {
                    float sa = X[i], sb = X[i + 1], ya = Y[j], yb = Y[j + 1]; if (sb - sa < 1e-3f || yb - ya < 1e-3f) continue;
                    var c = new Vector2((sa + sb) * 0.5f, (ya + yb) * 0.5f); bool hole = false; foreach (var h in holes) if (h.Contains(c)) { hole = true; break; }
                    if (hole) continue;
                    Vector3 q0 = pa + d * sa + inw * t, q1 = pa + d * sb + inw * t;
                    b.inK.Quad(m, q0 + Vector3.up * ya, q1 + Vector3.up * ya, q1 + Vector3.up * yb, q0 + Vector3.up * yb, inw);
                    b.Solid(Vector3.Min(pa + d * sa, q1) + Vector3.up * ya, Vector3.Max(pa + d * sa, q1) + Vector3.up * yb);
                }
            var white = CouncilEstate.M.white;
            foreach (var h in holes)
            {
                Vector3 p0 = pa + d * h.xMin, p1 = pa + d * h.xMax; bool isDoor = Mathf.Abs((h.xMin + h.xMax) * 0.5f - doorX) < 0.05f;
                // reveals
                foreach (var (pp, sd) in new[] { (p0, d), (p1, -d) }) b.inK.Quad(m, pp + Vector3.up * h.yMin, pp + inw * t + Vector3.up * h.yMin, pp + inw * t + Vector3.up * h.yMax, pp + Vector3.up * h.yMax, sd);
                b.inK.Quad(m, p0 + Vector3.up * h.yMax, p1 + Vector3.up * h.yMax, p1 + inw * t + Vector3.up * h.yMax, p0 + inw * t + Vector3.up * h.yMax, Vector3.down);
                if (isDoor) continue;
                b.inK.Box(white, Vector3.Min(p0 + inw * (t - 0.02f), p1 + inw * (t + 0.1f)) + Vector3.up * (h.yMin - 0.03f), Vector3.Max(p0 + inw * (t - 0.02f), p1 + inw * (t + 0.1f)) + Vector3.up * h.yMin);
                Vector3 mid = inw * (t * 0.5f);
                b.glassK.Quad(CotMats.glassClear, p0 + mid + Vector3.up * h.yMin, p1 + mid + Vector3.up * h.yMin, p1 + mid + Vector3.up * h.yMax, p0 + mid + Vector3.up * h.yMax, inw);
                b.Solid(Vector3.Min(p0 + mid - inw * 0.03f, p1 + mid + inw * 0.03f) + Vector3.up * h.yMin, Vector3.Max(p0 + mid - inw * 0.03f, p1 + mid + inw * 0.03f) + Vector3.up * h.yMax);
            }
        }

        static void Spot(Home h, string kind, string label, string tag, Vector3 p, float yaw = 0f, bool hidden = false, float seat = 0f) =>
            h.spots.Add(new Home.SpotRec { kind = kind, label = label, tag = tag, pos = p, yaw = yaw, room = 0, lv = 0, hidden = hidden, h = seat });

        // ------------------------------------------------------------------------------------------ the snooker club
        static void Snooker(Bld b, Home h, float L, float D, float y, System.Random rng)
        {
            var felt = HomeBuilder.Flat(new Color(0.08f, 0.38f, 0.14f), 0.15f); var mah = HomeBuilder.Flat(new Color(0.28f, 0.12f, 0.07f), 0.5f); var shade = HomeBuilder.Flat(new Color(0.12f, 0.2f, 0.14f), 0.4f);
            var carpet = HomeBuilder.Surf("carpet_hex", 1.2f, new Color(0.45f, 0.1f, 0.12f), 0.1f);
            b.inK.Quad(carpet, V(0.3f, y + 0.005f, -0.3f), V(L - 0.3f, y + 0.005f, -0.3f), V(L - 0.3f, y + 0.005f, -D + 0.3f), V(0.3f, y + 0.005f, -D + 0.3f), Vector3.up);
            string[] lines = { "You miss an easy red. Somebody at the bar laughs.", "You pot the black off three cushions. Nobody saw.", "Twenty pence a frame, says the sign. You haven't got twenty pence." };
            int n = 0;
            foreach (float tx in new[] { L * 0.3f, L * 0.7f })
                foreach (float tz in new[] { -4.2f, -9.4f })
                {
                    var c = V(tx, y, tz);
                    foreach (var o in new[] { V(-1.6f, 0, -0.75f), V(1.6f, 0, -0.75f), V(-1.6f, 0, 0.75f), V(1.6f, 0, 0.75f), V(0, 0, -0.75f), V(0, 0, 0.75f) }) b.furnK.Box(mah, c + o + V(-0.09f, 0f, -0.09f), c + o + V(0.09f, 0.62f, 0.09f));
                    b.Box(mah, c + V(-1.95f, 0.62f, -1.05f), c + V(1.95f, 0.8f, 1.05f), true);
                    b.furnK.Box(felt, c + V(-1.8f, 0.8f, -0.9f), c + V(1.8f, 0.82f, 0.9f));
                    foreach (var (mn, mx) in new[] { (V(-1.95f, 0.8f, -1.05f), V(1.95f, 0.88f, -0.9f)), (V(-1.95f, 0.8f, 0.9f), V(1.95f, 0.88f, 1.05f)), (V(-1.95f, 0.8f, -0.9f), V(-1.8f, 0.88f, 0.9f)), (V(1.8f, 0.8f, -0.9f), V(1.95f, 0.88f, 0.9f)) }) b.furnK.Box(mah, c + mn, c + mx);
                    for (int i = 0; i < 6; i++) b.furnK.Sphere(HomeBuilder.Flat(i == 0 ? Color.white : i == 5 ? Color.black : new Color(0.75f, 0.08f, 0.06f), 0.8f), c + V(-0.6f + i * 0.28f, 0.85f, (float)rng.NextDouble() * 0.8f - 0.4f), V(0.026f, 0.026f, 0.026f), 6, 4);
                    // the lamp over it
                    b.furnK.Box(shade, c + V(-1.3f, 2.1f, -0.35f), c + V(1.3f, 2.3f, 0.35f)); b.furnK.Box(CouncilEstate.M.glow, c + V(-1.2f, 2.08f, -0.25f), c + V(1.2f, 2.1f, 0.25f));
                    b.furnK.Box(TownMats.steel, c + V(-0.01f, 2.3f, -0.01f), c + V(0.01f, Mathf.Max(2.31f, (y + 7.8f) - y - 0.3f), 0.01f));
                    b.Light(c + V(0, 1.9f, 0), new Color(1f, 0.95f, 0.8f), 5f);
                    Spot(h, "say", "Have a frame", lines[n % 3], c + V(-2.3f, 0f, 0f), 90f); n++;
                }
            // cue racks on the side walls, benches under them
            foreach (float zz in new[] { -4.2f, -9.4f })
            {
                b.Box(mah, V(0.3f, y + 0.9f, zz - 0.8f), V(0.38f, y + 1.9f, zz + 0.8f));
                for (int i = 0; i < 8; i++) b.furnK.Box(HomeBuilder.Flat(new Color(0.75f, 0.6f, 0.4f), 0.4f), V(0.38f, y + 0.4f, zz - 0.7f + i * 0.2f), V(0.41f, y + 1.85f, zz - 0.68f + i * 0.2f));
                Spot(h, "rummage", "Go through the cue lockers", "cabinet", V(0.9f, y, zz));
                b.Seats(V(L - 0.6f, y, zz - 1.2f), -90f, 4);
                Spot(h, "sit", "Sit and watch", "bench", V(L - 0.9f, y, zz), -90f, false, 0.45f);
            }
            // the bar across the back: counter, optics, stools, the till; the gents in the corner
            float bz = -D + 1.6f;
            b.Counter(V(L * 0.5f + 2f, y, bz), 0f, 7f, false);
            b.Shelf(V(L * 0.5f - 1.5f, y, -D + 0.45f), V(L * 0.5f + 5.5f, y, -D + 0.45f), 1.9f);
            for (int i = 0; i < 10; i++) b.furnK.Cyl(HomeBuilder.Flat(Color.HSVToRGB((float)rng.NextDouble(), 0.6f, 0.6f), 0.8f), V(L * 0.5f - 1.2f + i * 0.65f, y + 1.45f, -D + 0.42f), 0.04f, 0.3f, 8);
            for (int i = 0; i < 5; i++) { b.Chair(V(L * 0.5f - 1f + i * 1.3f, y, bz + 0.9f), 180f); Spot(h, "sit", "Sit at the bar", "stool", V(L * 0.5f - 1f + i * 1.3f, y, bz + 0.9f), 180f, false, 0.7f); }
            Spot(h, "rummage", "Go in the till", "till", V(L * 0.5f + 4.5f, y, bz - 0.6f), 0f);
            Spot(h, "rummage", "Look under the bar", "counter", V(L * 0.5f + 1f, y, bz - 0.6f), 0f, true);
            b.Vending(V(1.2f, y, -1.4f), 90f); Spot(h, "rummage", "Jemmy the fruit machine", "till", V(1.9f, y, -1.4f), 90f);
            b.Telly(V(L * 0.5f, y + 2.6f, -D + 0.35f), Vector3.forward); Spot(h, "tv", "Turn the telly on", "tv", V(L * 0.5f, y, -D + 2.6f));
            b.Part(V(2.8f, 0, -D + 0.3f), V(2.8f, 0, -D + 3.6f), y, 2.5f, TownMats.lobbyWall); b.Part(V(0.3f, 0, -D + 3.6f), V(2.8f, 0, -D + 3.6f), y, 2.5f, TownMats.lobbyWall, (1.6f, 0.85f));
            b.Box(CotMats.ceramic, V(0.5f, y, -D + 0.45f), V(0.9f, y + 0.4f, -D + 1.0f), true); Spot(h, "flush", "Flush", "flush", V(0.7f, y, -D + 1.5f));
            for (int i = 0; i < 4; i++) b.Strip(V(L * (0.2f + i * 0.2f), y + 7.5f, -D * 0.5f), i % 2 == 0);
            b.Poster(V(L * 0.5f, y + 2.0f, -0.32f), Vector3.back, 2.2f, 1.2f, TownMats.notice);
        }

        // ------------------------------------------------------------------------------------------ the gospel hall
        static void Chapel(Bld b, Home h, float L, float D, float y, System.Random rng)
        {
            var oak = HomeBuilder.Flat(new Color(0.42f, 0.27f, 0.14f), 0.4f); var cloth = HomeBuilder.Flat(new Color(0.45f, 0.08f, 0.1f), 0.1f);
            // pews either side of the aisle, front to back
            for (int row = 0; row < 8; row++)
            {
                float z = -3.2f - row * 1.4f;
                foreach (var (x0, x1) in new[] { (0.6f, L * 0.5f - 0.7f), (L * 0.5f + 0.7f, L - 0.6f) })
                {
                    b.Box(oak, V(x0, y + 0.42f, z - 0.2f), V(x1, y + 0.47f, z + 0.25f), true); b.Box(oak, V(x0, y + 0.47f, z + 0.25f), V(x1, y + 1.0f, z + 0.3f), false);
                    b.furnK.Box(oak, V(x0, y, z - 0.2f), V(x0 + 0.05f, y + 0.95f, z + 0.3f)); b.furnK.Box(oak, V(x1 - 0.05f, y, z - 0.2f), V(x1, y + 0.95f, z + 0.3f));
                    if (row % 2 == 0) Spot(h, "sit", "Sit in the pew", "pew", V((x0 + x1) * 0.5f, y, z), 0f, false, 0.45f);
                }
            }
            // at the back (far end): the dais, lectern, table, piano, hymn board
            float fz = -D + 0.3f;
            b.Box(oak, V(1f, y, fz), V(L - 1f, y + 0.3f, fz + 2.6f), true);
            b.Box(oak, V(L * 0.5f - 0.35f, y + 0.3f, fz + 1.8f), V(L * 0.5f + 0.35f, y + 1.45f, fz + 2.2f), true);
            b.Box(cloth, V(L * 0.5f - 1.2f, y + 0.3f, fz + 0.4f), V(L * 0.5f + 1.2f, y + 1.1f, fz + 1.2f), true);
            Spot(h, "rummage", "Look in the collection plate", "till", V(L * 0.5f, y + 0.3f, fz + 1.6f));
            b.Box(HomeBuilder.Flat(new Color(0.08f, 0.06f, 0.05f), 0.6f), V(L - 2.4f, y + 0.3f, fz + 0.1f), V(L - 1.0f, y + 1.5f, fz + 0.7f), true);
            Spot(h, "say", "Play the piano", "You plonk out half of Chopsticks. The hall echoes back, unimpressed.", V(L - 1.7f, y + 0.3f, fz + 1.2f));
            b.Box(oak, V(0.5f, y + 1.6f, fz + 0.02f), V(1.4f, y + 2.6f, fz + 0.06f));
            Spot(h, "read", "Read the notices", "notice", V(1.0f, y + 0.3f, fz + 0.8f));
            // the vestry: a cupboard room by the door with the cash box and the tea things
            b.Part(V(L - 3.2f, 0, -0.3f), V(L - 3.2f, 0, -2.6f), y, 2.6f, TownMats.lobbyWall, (1.6f, 0.85f)); b.Part(V(L - 3.2f, 0, -2.6f), V(L - 0.3f, 0, -2.6f), y, 2.6f, TownMats.lobbyWall);
            b.Box(oak, V(L - 0.75f, y, -2.5f), V(L - 0.35f, y + 1.9f, -1.3f), true); Spot(h, "rummage", "Open the vestry cupboard", "cabinet", V(L - 1.2f, y, -1.9f), -90f);
            b.Box(TownMats.steel, V(L - 3.0f, y + 0.9f, -0.45f), V(L - 2.4f, y + 1.0f, -0.35f)); Spot(h, "tap", "Put the urn on", "kettle", V(L - 2.7f, y, -0.9f));
            for (int i = 0; i < 3; i++) b.Strip(V(L * 0.5f, y + 6.8f, -4f - i * 5f), i == 1);
            b.Light(V(L * 0.5f, y + 4f, -D * 0.5f), new Color(1f, 0.9f, 0.75f), 12f);
        }

        // ------------------------------------------------------------------------------------------ the engine house
        static void Engine(Bld b, Home h, float L, float D, float y, System.Random rng)
        {
            var iron = HomeBuilder.Flat(new Color(0.18f, 0.16f, 0.15f), 0.5f); var rust = TownMats.rust ?? iron; var wood = HomeBuilder.Flat(new Color(0.45f, 0.33f, 0.2f), 0.3f);
            b.furnK.CylX(rust, V(L * 0.5f, y + 1.3f, -D * 0.6f), 1.1f, 6.5f, 16);
            b.Solid(V(L * 0.5f - 3.25f, y, -D * 0.6f - 1.1f), V(L * 0.5f + 3.25f, y + 2.4f, -D * 0.6f + 1.1f));
            b.Box(iron, V(L * 0.5f - 3.4f, y, -D * 0.6f - 0.9f), V(L * 0.5f + 3.4f, y + 0.4f, -D * 0.6f + 0.9f));
            for (int i = 0; i < 3; i++) b.furnK.Cyl(iron, V(L * 0.5f - 2f + i * 2f, y + 2.4f, -D * 0.6f), 0.12f, 4.5f, 8);
            b.Box(wood, V(0.4f, y + 0.85f, -2.0f), V(1.0f, y + 0.9f, -4.4f), true); for (int i = 0; i < 4; i++) b.furnK.Box(iron, V(0.45f, y + 0.9f, -2.2f - i * 0.5f), V(0.75f, y + 1.05f, -2.0f - i * 0.5f));
            Spot(h, "rummage", "Search the workbench", "table", V(1.4f, y, -3.2f), 90f);
            for (int i = 0; i < 6; i++) { float x = L - 1f - (float)rng.NextDouble() * 3f, z = -1.5f - (float)rng.NextDouble() * 3f; b.Box(HomeBuilder.Flat(new Color(0.55f, 0.42f, 0.28f), 0.2f), V(x - 0.3f, y, z - 0.25f), V(x + 0.3f, y + 0.4f + (float)rng.NextDouble() * 0.4f, z + 0.25f), true); }
            Spot(h, "rummage", "Poke about in the junk", "bag", V(L - 2.5f, y, -4.8f), 0f, true);
            b.Strip(V(L * 0.5f, y + 7f, -D * 0.4f), true);
        }
    }
}
