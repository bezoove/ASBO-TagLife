using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Lived-in clutter from the scanned models: pots and plants by the front doors, garden gnomes on the lawns,
    /// dustbins round the side, a hose on the wall; weeds and dandelions along the wall bottoms and in the gutter, grass
    /// tufts on the verges, ferns inside the estate wall, and a rocky clump with fir trees out in the fields. Laid out
    /// the same every time (fixed seed), loaded from Resources/Props.
    /// </summary>
    public static class Dressing
    {
        static Transform root;
        static readonly int Mask = ~((1 << 2) | (1 << 31) | (1 << Person.VehicleLayer) | (1 << Person.RagdollLayer));
        static readonly string[] Weeds = { "weed_plant_02_a", "weed_plant_02_b", "weed_plant_02_c", "weed_plant_02_d", "weed_plant_02_e", "dandelion_01_a", "dandelion_01_b", "dandelion_01_c", "dandelion_01_d", "dandelion_01_e" };
        static readonly string[] Flowers = { "celandine_01_a", "celandine_01_b", "celandine_01_c", "celandine_01_d", "periwinkle_plant_01", "periwinkle_plant_02", "periwinkle_plant_03" };
        static readonly string[] Grass = { "grass_medium_01_large_a", "grass_medium_01_large_b", "grass_medium_01_large_c", "grass_medium_01_mid_a", "grass_medium_01_mid_b", "grass_medium_01_mid_c", "grass_medium_01_tall_a", "grass_medium_01_tall_b", "grass_medium_01_tall_c" };
        static readonly string[] Ferns = { "fern_02_a", "fern_02_b", "fern_02_c", "fern_02_d" };
        static readonly string[] Stones = { "rock_07", "rock_09", "stone_01" };
        static System.Random rng;

        public static void Place(Transform parent)
        {
            if (Resources.Load<GameObject>("Props/garden_gnome") == null) { Debug.Log("[ASBO] dressing: no props in this build"); return; }
            root = new GameObject("Dressing").transform; root.SetParent(parent, false);
            rng = new System.Random(4242);
            Physics.SyncTransforms();
            foreach (var h in Estate.Houses) House(h);
            Verges();
            Wrecks();
            Outcrop(new Vector3(92f, 0f, -70f), 1f);   // out past the estate's back gardens, on the rising fields
            Outcrop(new Vector3(-92f, 0f, -60f), 0.8f);
            Debug.Log($"[ASBO] dressing: {root.childCount} pieces placed");
            CouncilEstate.Dress(parent);
            CraneSlew.Attach();
            Crows.Attach();
        }

        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static string Any(string[] l) => l[rng.Next(l.Length)];

        static GameObject Put(string name, Vector3 pos, float yaw, float scale = 1f, bool ground = true)
        {
            var pf = Resources.Load<GameObject>("Props/" + name); if (pf == null) return null;
            if (ground && Physics.Raycast(pos + Vector3.up * 2.5f, Vector3.down, out var hit, 6f, Mask, QueryTriggerInteraction.Ignore)) pos.y = hit.point.y;
            var g = Object.Instantiate(pf, pos, Quaternion.Euler(0f, yaw, 0f), root);
            if (!Mathf.Approximately(scale, 1f)) g.transform.localScale = Vector3.one * scale;
            return g;
        }

        static void Sink(GameObject g, float by) { if (g != null) g.transform.position += Vector3.down * by; } // bed rocks into the ground

        static bool Clear(CotHouse h, Vector3 p, float margin)
        {
            Vector3 l = h.root.InverseTransformPoint(p);
            foreach (var z in h.zones) if (z.In(l.x, l.z, margin)) return false;
            return true;
        }

        static void House(CotHouse h)
        {
            Vector3 door = h.root.TransformPoint(h.FrontDoorLocal);
            Vector3 outw = h.root.TransformDirection(h.FrontOutLocal); outw.y = 0f; outw.Normalize();
            Vector3 along = Vector3.Cross(Vector3.up, outw);
            float yawOut = Quaternion.LookRotation(outw).eulerAngles.y;
            // the front wall's actual face, found by looking back at it from the doorstep
            Vector3 wall = door - outw * 0.8f;
            if (Physics.Raycast(door + Vector3.up * 0.9f, -outw, out var wh, 2.5f, Mask, QueryTriggerInteraction.Ignore)) wall = new Vector3(wh.point.x, door.y, wh.point.z);
            int s = rng.Next(2) == 0 ? 1 : -1;

            // pots either side of the door
            Put(rng.Next(2) == 0 ? "potted_plant_02" : "potted_plant_01", wall + outw * 0.4f + along * s * 0.85f, R(0, 360));
            Put("planter_pot_clay", wall + outw * 0.3f - along * s * 0.75f, R(0, 360));
            if (rng.NextDouble() < 0.6) Put("potted_plant_04", wall + outw * 0.28f - along * s * 1.0f, R(0, 360));
            // hose on the wall
            if (rng.NextDouble() < 0.6)
            {
                Vector3 hp = wall - along * s * 1.75f + Vector3.up * 0.95f;
                if (Physics.Raycast(hp + outw * 0.6f, -outw, out var hh, 1.2f, Mask, QueryTriggerInteraction.Ignore)) hp = hh.point + outw * 0.01f;
                Put("garden_hose_wall_mounted_01", hp, yawOut, 1f, false);
            }
            // gnomes on the lawn, looking down the path
            if (rng.NextDouble() < 0.65) Put("garden_gnome", door + outw * R(1.2f, 1.8f) + along * s * R(2.2f, 3.4f), yawOut + R(-35f, 35f));
            if (rng.NextDouble() < 0.25) Put("garden_gnome", door + outw * R(1.2f, 1.8f) - along * s * R(2.2f, 3.4f), yawOut + R(-35f, 35f), 0.9f);
            // dustbins round the side
            int bins = 0;
            foreach (int side in new[] { s, -s })
                foreach (float d in new[] { 4.8f, 5.6f, 6.6f, 7.6f })
                {
                    if (bins >= 1 + (rng.NextDouble() < 0.4 ? 1 : 0)) break;
                    Vector3 p = wall + outw * 0.5f + along * side * d;
                    if (!Clear(h, p, 0.45f)) continue;
                    Put(rng.NextDouble() < 0.55 ? "metal_trash_can_rust" : "metal_trash_can", p, R(0, 360)); bins++;
                    if (rng.NextDouble() < 0.5) Put(Any(Weeds), p + along * side * 0.5f + outw * 0.2f, R(0, 360));
                    break;
                }
            // flowers and ferns where the beds run out
            foreach (int side in new[] { -1, 1 })
            {
                Vector3 p = wall + outw * R(0.4f, 0.9f) + along * side * R(4.4f, 5.2f);
                if (Clear(h, p, 0.2f)) Put(rng.NextDouble() < 0.5 ? Any(Flowers) : Any(Ferns), p, R(0, 360));
            }
        }

        static void Verges()
        {
            float north = World.RoadZ + 7.6f, south = World.RoadZ - 7.6f, kerbN = World.RoadZ + 4.5f, kerbS = World.RoadZ - 4.5f;
            // the bottom of the estate wall, and the grass edge beyond it
            for (float x = -70f; x < 70f; x += R(1.2f, 2.2f))
            {
                if (Mathf.Abs(x) < 5.8f) continue; // the gate
                bool wallRun = Mathf.Abs(x) < 45f;
                if (rng.NextDouble() < (wallRun ? 0.45 : 0.6))
                    Put(wallRun ? Any(Weeds) : Any(Grass), new Vector3(x + R(-0.3f, 0.3f), 0, north - R(0.08f, 0.2f)), R(0, 360), wallRun ? R(1.6f, 2.4f) : R(2.2f, 3.2f));
            }
            // the gutters, where the weeds get a hold against the kerb
            for (float x = -80f; x < 80f; x += R(4f, 9f))
            {
                if (rng.NextDouble() < 0.35) Put(Any(Weeds), new Vector3(x, 0, kerbN - R(0.05f, 0.15f)), R(0, 360), R(1.3f, 1.9f));
                if (rng.NextDouble() < 0.35) Put(Any(Weeds), new Vector3(x + R(-2f, 2f), 0, kerbS + R(0.05f, 0.15f)), R(0, 360), R(1.3f, 1.9f));
            }
            // the far verge: tufts, dandelions, celandine and the odd stone
            for (float x = -90f; x < 90f; x += R(0.8f, 1.6f))
            {
                if (rng.NextDouble() < 0.7)
                {
                    double t = rng.NextDouble();
                    string n = t < 0.6 ? Any(Grass) : t < 0.8 ? Any(Weeds) : t < 0.93 ? Any(Flowers) : Any(Stones);
                    Put(n, new Vector3(x + R(-0.3f, 0.3f), 0, south - R(0.1f, 1.4f)), R(0, 360), t < 0.6 ? R(2.2f, 3.4f) : t < 0.93 ? R(1.5f, 2.3f) : R(1.5f, 3f));
                }
            }
            // inside the estate wall: ferns and periwinkle in its shade
            for (float x = -43f; x < 43f; x += R(2.5f, 4f))
            {
                if (Mathf.Abs(x) < 6.5f) continue;
                if (rng.NextDouble() < 0.55) Put(rng.NextDouble() < 0.6 ? Any(Ferns) : Any(Flowers), new Vector3(x, 0, -21.55f + R(0.1f, 0.4f)), R(0, 360), R(1.1f, 1.6f));
            }
        }

        /// <summary>Dumped cars: a burnt-out saloon on the verge, a torched supermini by the field gate, a smashed-up 4x4.</summary>
        static void Wrecks()
        {
            void Dump(string id, CarKit.Wear w, Vector3 p, float yaw, int seed)
            {
                if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out var hit, 8f, Mask, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
                CarKit.Scenery(root, id, w, p, yaw, seed);
            }
            Dump("saloon90", CarKit.Wear.Burnt, new Vector3(-52f, 0f, World.RoadZ - 10.5f), 78f, 11);
            Dump("supermini", CarKit.Wear.Burnt, new Vector3(26f, 0f, -50f), 200f, 12);   // dumped on the estate green
            Dump("jeep", CarKit.Wear.Wreck, new Vector3(12f, 0f, -60f), 140f, 13);
            Dump("hatch90", CarKit.Wear.Wreck, new Vector3(70f, 0f, World.RoadZ - 11f), 95f, 14);
        }

        /// <summary>A rocky clump out in the fields: mossy boulders, a couple of young firs, stones and ferns round it.</summary>
        static void Outcrop(Vector3 c, float size)
        {
            string[] big = { "rock_moss_set_01_rock01", "rock_moss_set_01_rock02", "rock_moss_set_01_rock03", "rock_moss_set_01_rock04", "rock_moss_set_01_rock05", "rock_moss_set_01_rock06" };
            string[] boulders = { "namaqualand_boulder_02", "namaqualand_boulder_03", "namaqualand_boulder_04" };
            for (int i = 0; i < 4; i++) Sink(Put(Any(big), c + new Vector3(R(-5f, 5f), 0, R(-4f, 4f)) * size, R(0, 360), R(1.4f, 2.2f) * size), 0.3f);
            Sink(Put(Any(boulders), c + new Vector3(R(-7f, 7f), 0, R(-6f, 6f)) * size, R(0, 360), 1.5f * size), 0.2f);
            Put("fir_sapling_medium_a", c + new Vector3(R(3f, 7f), 0, R(2f, 6f)) * size, R(0, 360), R(0.9f, 1.1f));
            Put(rng.Next(2) == 0 ? "fir_sapling_medium_b" : "fir_sapling_medium_c", c + new Vector3(R(-8f, -4f), 0, R(-2f, 4f)) * size, R(0, 360), R(0.9f, 1.15f));
            for (int i = 0; i < 10; i++)
            {
                Vector3 p = c + new Vector3(R(-9f, 9f), 0, R(-8f, 8f)) * size;
                Put(i % 3 == 0 ? Any(Ferns) : i % 3 == 1 ? Any(Stones) : Any(Grass), p, R(0, 360), i % 3 == 1 ? R(2f, 4f) : R(1.4f, 2.6f));
            }
        }
    }
}
