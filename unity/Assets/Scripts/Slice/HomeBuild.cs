using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// The inside of one council terrace house (5.4 m wide, 8.6 deep, three floors, a garage at the front on one side).
    /// Every house gets the same structural frame (stairs and floors have to line up) but its own arrangement of rooms:
    /// the ground floor is one of three layouts, the first and second floors one of three each, the living rooms may
    /// be upstairs, the garage may be a garage or a bedroom or a gym, a bedroom may have been knocked through or split.
    /// All of it is worked in "u, d" space: u across the house from the garage-side party wall, d back from the front wall,
    /// so the handing of the pair (they mirror each other) is just a mapping at the end.
    /// </summary>
    public partial class HomeBuilder
    {
        public const float W = CouncilEstate.W, D = CouncilEstate.D, FF = CouncilEstate.FF;
        public const float U0 = 0.2f, U1 = W - 0.2f, D0 = 0.12f, D1 = D - 0.12f, PT = 0.1f;
        public static readonly float[] Y = { 0.15f, 2.6f, 5.2f };      // floor tops
        public static readonly float[] C = { 2.4f, 5.0f, 7.6f };       // ceiling undersides

        public class Room
        {
            public int lv; public string type, use, role; public float u0, u1, d0, d1; public int idx;
            public List<Rect> taken = new List<Rect>();
            public float Cu => (u0 + u1) * 0.5f; public float Cd => (d0 + d1) * 0.5f;
            public bool Has(float u, float d) => u >= u0 - 0.01f && u <= u1 + 0.01f && d >= d0 - 0.01f && d <= d1 + 0.01f;
            public Rect Rect => new Rect(u0, d0, u1 - u0, d1 - d0);
            public string Floor; public Color wall; public bool wet;
        }
        public class Wall
        {
            public int lv; public char axis; public float at, from, to, thick = PT; public List<(float c, float w, bool leaf, string tag)> gaps = new List<(float, float, bool, string)>();
        }
        public class Door { public int lv; public char axis; public float at, c; public Room a, b; public PropDoor leaf; public string tag; }
        public class Win { public int lv; public bool front; public float u0, u1, sill, head; }
        public class Void { public int lv; public Rect r; }

        // set by the caller
        public Transform parent; public float x0; public bool mirror; public System.Random rng; public Household H; public int seed; public string name;
        public MeshKit shell = new MeshKit(), fin = new MeshKit(), furn = new MeshKit(), glass = new MeshKit();
        public readonly List<Room> rooms = new List<Room>(); public readonly List<Wall> walls = new List<Wall>(); public readonly List<Door> doors = new List<Door>();
        public readonly List<Win> wins = new List<Win>(); public readonly List<Void> voids = new List<Void>(); public readonly List<Rect>[] blocks = { new List<Rect>(), new List<Rect>(), new List<Rect>() };
        public readonly List<(Vector3 c, Vector3 s, float yaw)> solids = new List<(Vector3, Vector3, float)>();
        public Transform root; public Home home; public bool endU0, endW;
        public int ground, upper1, upper2; // layout variants

        // ------------------------------------------------------------------ space mapping
        public Vector3 P(float u, float y, float d) => new Vector3(mirror ? x0 + W - u : x0 + u, y, -d);
        public float WorldYaw(float du, float dd) { float dx = mirror ? -du : du, dz = -dd; return Mathf.Atan2(dx, dz) * Mathf.Rad2Deg; }

        /// <summary>A solid box in u,d,y space; with collide, a box collider is made for it too.</summary>
        public void Bx(MeshKit k, Material m, float u0, float u1, float y0, float y1, float d0, float d1, bool collide = false, int skip = 0)
        {
            Vector3 a = P(u0, y0, d0), b = P(u1, y1, d1);
            Vector3 mn = Vector3.Min(a, b), mx = Vector3.Max(a, b);
            // skip bits are in world axes; callers pass 0 mostly
            k.Box(m, mn, mx, skip);
            if (collide) solids.Add(((mn + mx) * 0.5f, mx - mn, 0f));
        }

        public Room AddRoom(int lv, string type, float u0, float u1, float d0, float d1)
        {
            var r = new Room { lv = lv, type = type, u0 = u0, u1 = u1, d0 = d0, d1 = d1, idx = rooms.Count }; rooms.Add(r); return r;
        }
        public Wall AddWall(int lv, char axis, float at, float from, float to, float thick = PT)
        {
            var w = new Wall { lv = lv, axis = axis, at = at, from = from, to = to, thick = thick }; walls.Add(w); return w;
        }
        /// <summary>A doorway in a wall (a leaf = a real door that opens; no leaf = an open archway).</summary>
        public void Gap(Wall w, float c, float width, bool leaf = true, string tag = "") { w.gaps.Add((c, width, leaf, tag)); }
        public Room RoomAt(int lv, float u, float d) { foreach (var r in rooms) if (r.lv == lv && r.Has(u, d)) return r; return null; }
        public Room Find(int lv, string type) { foreach (var r in rooms) if (r.lv == lv && r.type == type) return r; return null; }

        // ------------------------------------------------------------------ layouts
        /// <summary>Choose and lay out the three floors for this house.</summary>
        public void Layout()
        {
            ground = rng.Next(3); upper1 = rng.Next(3); upper2 = rng.Next(3);
            Ground(); First(); Second();
        }

        // ---- ground floor: garage, hall (stairs up the right), then the back of the house
        void Ground()
        {
            const int L = 0;
            var gar = AddRoom(L, "garage", U0, 2.85f, D0, 5.0f);
            var hall = AddRoom(L, "hall", 2.85f, U1, 1.0f, 3.5f);
            var foot = AddRoom(L, "hall", 4.3f, U1, D0, 1.0f); foot.use = "stairfoot";
            // walls: garage side wall round the porch; garage / hall; hall / back
            var wPorch = AddWall(L, 'd', 2.925f, D0, 1.0f, 0.15f);                    // the porch's left wall, thick
            var wGH = AddWall(L, 'd', 2.85f, 1.0f, 5.0f); Gap(wGH, 2.1f, 0.9f);       // garage / hall (and garage / back)
            var wGBack = AddWall(L, 'u', 5.0f, U0, 2.85f);                              // garage rear wall
            blocks[0].Add(new Rect(4.3f, 1.0f, 0.9f, 2.5f));                           // the stairs
            var wFoot = AddWall(L, 'd', 4.3f, D0, 1.0f); wFoot.thick = 0.1f;            // porch right wall (porch is outside; this is the hall's front-right corner)
            AddWall(L, 'u', 1.0f, 2.85f, 3.0f);                                         // front of the hall, left of the porch
            var wFront = AddWall(L, 'u', 1.0f, 3.0f, 4.3f); Gap(wFront, 3.65f, 0.92f, true, "front"); // the porch's back wall and the front door

            switch (ground)
            {
                case 0: // kitchen-diner right across the back, dining nook behind the hall
                    {
                        var nook = AddRoom(L, "dining", 2.85f, U1, 3.5f, 5.0f);
                        var kit = AddRoom(L, "kitchen", U0, U1, 5.0f, D1);
                        var wHN = AddWall(L, 'u', 3.5f, 2.85f, U1); Gap(wHN, 3.7f, 0.9f); Gap(wHN, 4.75f, 0.9f, false, "tall");
                        // nook open to the kitchen: no wall, the garage wall carries on across
                        kit.use = "diner"; break;
                    }
                case 1: // small kitchen at the back left, a back room (den / dining) on the right, both off the hall via the den
                    {
                        var den = AddRoom(L, "den", 3.25f, U1, 3.5f, D1);
                        var kit = AddRoom(L, "kitchen", U0, 3.25f, 5.0f, D1);
                        var wHN = AddWall(L, 'u', 3.5f, 2.85f, U1); Gap(wHN, 3.9f, 0.9f); Gap(wHN, 4.75f, 0.9f, false, "tall");
                        var wKD = AddWall(L, 'd', 3.25f, 5.0f, D1); Gap(wKD, 6.6f, 0.9f);
                        var rear = AddRoom(L, "cupboard", 2.85f, 3.25f, 3.5f, 5.0f); rear.use = "rearlobby";
                        var wRP = AddWall(L, 'd', 3.25f, 3.5f, 5.0f); // lobby / den
                        Gap(wRP, 4.2f, 0.9f);
                        // lobby is the hall's back; open to the hall by an archway in the hall's back wall
                        break;
                    }
                default: // knocked-through living space with a utility cupboard / WC at the back left
                    {
                        var liv = AddRoom(L, "kitchen", 1.6f, U1, 5.0f, D1); liv.use = "open";
                        var wc = AddRoom(L, "wc", U0, 1.6f, 5.0f, 6.3f);
                        var util = AddRoom(L, "utility", U0, 1.6f, 6.3f, D1);
                        var nook = AddRoom(L, "dining", 2.85f, U1, 3.5f, 5.0f);
                        var wHN = AddWall(L, 'u', 3.5f, 2.85f, U1); Gap(wHN, 3.7f, 0.9f); Gap(wHN, 4.75f, 0.9f, false, "tall");
                        var wWC = AddWall(L, 'd', 1.6f, 5.0f, D1); Gap(wWC, 5.7f, 0.8f); Gap(wWC, 7.3f, 0.8f);
                        AddWall(L, 'u', 6.3f, U0, 1.6f);
                        break;
                    }
            }
        }

        // ---- first floor: living room at the front (the stairs on up on its left), landing and back rooms
        void First()
        {
            const int L = 1;
            voids.Add(new Void { lv = 1, r = new Rect(4.3f, 1.0f, 0.9f, 2.5f) });
            var lg = AddRoom(L, "lounge", U0, 4.3f, D0, 4.2f);
            blocks[1].Add(new Rect(U0, 1.2f, 0.9f, 3.0f));                               // the stairs on up
            var top = AddRoom(L, "landing", 4.3f, U1, D0, 1.0f); top.use = "stairtop";      // the stairs come up here, at the front
            var ac = AddRoom(L, "cupboard", 4.3f, U1, 3.5f, 4.2f);
            var lb = AddRoom(L, "landing", 3.0f, U1, 4.2f, 5.4f);
            var wLR = AddWall(L, 'u', 4.2f, U0, 4.3f); Gap(wLR, 3.65f, 0.9f);
            var wLS = AddWall(L, 'd', 4.3f, D0, 3.5f); Gap(wLS, 0.56f, 0.8f, false);                                   // lounge / stairwell
            var wLA = AddWall(L, 'd', 4.3f, 3.5f, 4.2f); Gap(wLA, 3.85f, 0.7f, false);   // archway lounge / stair head
            switch (upper1)
            {
                case 0: // two back bedrooms and a bathroom
                    {
                        var b3 = AddRoom(L, "bed", U0, 3.0f, 4.2f, D1); var ba = AddRoom(L, "bath", 3.0f, U1, 5.4f, 6.6f); var b4 = AddRoom(L, "bed", 3.0f, U1, 6.6f, D1);
                        var w1 = AddWall(L, 'd', 3.0f, 4.2f, D1); Gap(w1, 4.85f, 0.9f); Gap(w1, 7.5f, 0.9f);
                        var w2 = AddWall(L, 'u', 5.4f, 3.0f, U1); Gap(w2, 3.7f, 0.8f);
                        var w3 = AddWall(L, 'u', 6.6f, 3.0f, U1);
                        break;
                    }
                case 1: // one big back bedroom, big bathroom
                    {
                        var b3 = AddRoom(L, "bed", U0, 3.0f, 4.2f, D1); b3.use = "master";
                        var ba = AddRoom(L, "bath", 3.0f, U1, 5.4f, D1); ba.use = "bigbath";
                        var w1 = AddWall(L, 'd', 3.0f, 4.2f, D1); Gap(w1, 4.85f, 0.9f);
                        var w2 = AddWall(L, 'u', 5.4f, 3.0f, U1); Gap(w2, 3.7f, 0.8f);
                        break;
                    }
                default: // kitchen-diner upstairs at the back, bedroom beside it
                    {
                        var kd = AddRoom(L, "kitchen", U0, 3.0f, 4.2f, D1); kd.use = "upstairs";
                        var b3 = AddRoom(L, "bed", 3.0f, U1, 6.6f, D1); var ba = AddRoom(L, "bath", 3.0f, U1, 5.4f, 6.6f);
                        var w1 = AddWall(L, 'd', 3.0f, 4.2f, D1); Gap(w1, 4.85f, 0.9f); Gap(w1, 7.5f, 0.9f);
                        var w2 = AddWall(L, 'u', 5.4f, 3.0f, U1); Gap(w2, 3.7f, 0.8f);
                        var w3 = AddWall(L, 'u', 6.6f, 3.0f, U1);
                        break;
                    }
            }
        }

        // ---- second floor: the stairs come up the front left; bedrooms and a bathroom
        void Second()
        {
            const int L = 2;
            voids.Add(new Void { lv = 2, r = new Rect(U0, 1.2f, 0.9f, 3.0f) });
            var b1 = AddRoom(L, "bed", U0, U1, D0, 4.0f); b1.use = "front";
            var lb = AddRoom(L, "landing", U0, 3.0f, 4.2f, 5.4f);
            var wBL = AddWall(L, 'u', 4.1f, 1.1f, U1); Gap(wBL, 2.2f, 0.9f);
            blocks[2].Add(new Rect(U0, D0, 0.9f, 1.08f));                                  // sliver in front of the stairwell: boxed in
            switch (upper2)
            {
                case 0: // back bedroom, bathroom beside the landing
                    {
                        var b2 = AddRoom(L, "bed", U0, U1, 5.4f, D1); var ba = AddRoom(L, "bath", 3.0f, U1, 4.2f, 5.4f);
                        var wa = AddWall(L, 'u', 5.4f, U0, U1); Gap(wa, 2.0f, 0.9f);
                        var wb = AddWall(L, 'd', 3.0f, 4.2f, 5.4f); Gap(wb, 4.8f, 0.8f);
                        var wc = AddWall(L, 'u', 4.2f, 3.0f, U1);
                        break;
                    }
                case 1: // two small back rooms
                    {
                        var b2 = AddRoom(L, "bed", U0, 2.8f, 5.4f, D1); var b5 = AddRoom(L, "bed", 2.8f, U1, 5.4f, D1); b5.use = "box";
                        var ba = AddRoom(L, "bath", 3.0f, U1, 4.2f, 5.4f);
                        var wa = AddWall(L, 'u', 5.4f, U0, U1); Gap(wa, 1.5f, 0.9f);
                        var wb = AddWall(L, 'd', 2.8f, 5.4f, D1); Gap(wb, 6.9f, 0.8f);
                        var wb2 = AddWall(L, 'd', 3.0f, 4.2f, 5.4f); Gap(wb2, 4.8f, 0.8f);
                        var wc = AddWall(L, 'u', 4.2f, 3.0f, U1);
                        break;
                    }
                default: // en-suite across the back, one big bedroom through the landing
                    {
                        var b2 = AddRoom(L, "bed", U0, 3.6f, 5.4f, D1); b2.use = "master";
                        var es = AddRoom(L, "bath", 3.6f, U1, 5.4f, D1); es.use = "ensuite";
                        var ba = AddRoom(L, "bath", 3.0f, U1, 4.2f, 5.4f); ba.use = "shower";
                        var wa = AddWall(L, 'u', 5.4f, U0, 3.6f); Gap(wa, 2.0f, 0.9f);
                        var wb = AddWall(L, 'd', 3.6f, 5.4f, D1); Gap(wb, 6.9f, 0.8f);
                        var wb2 = AddWall(L, 'd', 3.0f, 4.2f, 5.4f); Gap(wb2, 4.8f, 0.8f);
                        var wc = AddWall(L, 'u', 4.2f, 3.0f, U1);
                        AddWall(L, 'u', 5.4f, 3.6f, U1);
                        break;
                    }
            }
        }
    }
}
