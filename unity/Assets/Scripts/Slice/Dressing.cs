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
            Street();
            Outcrop(new Vector3(92f, 0f, -70f), 1f);   // out past the estate's back gardens, on the rising fields
            Outcrop(new Vector3(-92f, 0f, -60f), 0.8f);
            Debug.Log($"[ASBO] dressing: {root.childCount} pieces placed");
            CouncilEstate.Dress(parent);
            Estates.Dress(parent); // cars in the new estates' car parks
            Industrial.Dress(parent); // wrecks in the scrapyard
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
                if (x < -45.5f) continue;            // the high street's shops start here
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
                if (x < -84f) continue; // the south parade of shops
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
            Dump("supermini", CarKit.Wear.Burnt, new Vector3(-12f, 0f, -84.5f), 200f, 12); // dumped on the green by the lock-ups
            Dump("jeep", CarKit.Wear.Wreck, new Vector3(12f, 0f, -60f), 140f, 13);
            Dump("hatch90", CarKit.Wear.Wreck, new Vector3(70f, 0f, World.RoadZ - 11f), 95f, 14);
        }

        // ------------------------------------------------------------------------------ the town's street furniture and junk
        static Material embers;
        static bool NearJunction(Vector3 p, float d) { foreach (var j in TownPlan.Junctions) if ((j.p - p).WithY(0).magnitude < d) return true; return false; }

        /// <summary>Put a model where nothing solid is already standing (ground found below it).</summary>
        static GameObject PutClear(string name, Vector3 pos, float yaw, Vector3 half)
        {
            if (Physics.Raycast(pos + Vector3.up * 2.5f, Vector3.down, out var hit, 6f, Mask, QueryTriggerInteraction.Ignore)) pos.y = hit.point.y;
            var q = Quaternion.Euler(0f, yaw, 0f);
            if (Physics.CheckBox(pos + Vector3.up * (half.y + 0.12f), half, q, Mask, QueryTriggerInteraction.Ignore)) return null;
            return Put(name, pos, yaw, 1f, false);
        }

        /// <summary>A model fixed to a wall: its back (local -Z) against the wall point, facing n, its base at y; tilt pitches it down.</summary>
        static GameObject OnWall(string name, Vector3 wall, Vector3 n, float depth, float tilt = 0f)
        {
            var pf = Resources.Load<GameObject>("Props/" + name); if (pf == null) return null;
            n.y = 0f; n.Normalize();
            return Object.Instantiate(pf, wall + n * (depth * 0.5f), Quaternion.LookRotation(n) * Quaternion.Euler(tilt, 0f, 0f), root);
        }

        /// <summary>A tyre (or wheel rim) lying flat, centred at c, k up a stack.</summary>
        static void Flat(string name, Vector3 c, int k, float yaw, float radius, float thick)
        {
            var pf = Resources.Load<GameObject>("Props/" + name); if (pf == null) return;
            if (Physics.Raycast(c + Vector3.up * 2.5f, Vector3.down, out var hit, 6f, Mask, QueryTriggerInteraction.Ignore)) c.y = hit.point.y;
            var q = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(90f, 0f, 0f);
            var g = Object.Instantiate(pf, c + Vector3.up * (thick * 0.5f + k * thick * 0.98f) - q * new Vector3(0f, radius, 0f), q, root);
            var rb = g.GetComponent<Rigidbody>(); if (rb != null) rb.Sleep();
        }

        /// <summary>
        /// The town's street furniture and junk (the Drive models): green cabinets on the pavements, manhole covers in the
        /// road, CCTV on the casino, the council offices, the mill, the car park and the corner shop, bulkhead lamps by the
        /// doors, telegraph poles down the back alleys, shuttered back windows, the mill yard's gates and covered cars,
        /// tyres and rims dumped about, footballs on the greens, and burning barrels where the gangs hang about.
        /// </summary>
        static void Street()
        {
            if (Resources.Load<GameObject>("Props/utility_box_02") == null) return;
            int n0 = root.childCount;
            // cabinets at the kerb edge of the pavement (backs to the road), and manhole covers in the lanes
            foreach (var r in TownPlan.Roads)
            {
                Vector3 d = r.b - r.a; float len = d.magnitude; d /= len; Vector3 side = Vector3.Cross(Vector3.up, d);
                for (float s = R(30f, 60f); s < len - 20f; s += R(75f, 115f))
                {
                    Vector3 c = r.a + d * s; if (NearJunction(c, 20f)) continue;
                    int sd = rng.Next(2) == 0 ? 1 : -1;
                    PutClear("utility_box_02", c + side * sd * (TownPlan.Half + TownPlan.KerbW + 0.5f), Quaternion.LookRotation(side * sd).eulerAngles.y, new Vector3(0.48f, 0.5f, 0.24f));
                }
                for (float s = R(8f, 25f); s < len - 5f; s += R(28f, 46f))
                {
                    Vector3 c = r.a + d * s; if (NearJunction(c, 15f)) continue;
                    Sink(Put("water_manhole_cover", c + side * (rng.Next(2) == 0 ? 1f : -1f) * TownPlan.LaneOff * R(0.5f, 1.05f), R(0f, 360f)), 0.055f);
                }
            }
            // CCTV
            OnWall("security_camera_01", new Vector3(163f, 4.4f, -2f), Vector3.back, 0.55f, 14f);       // the casino's front corners and west side
            OnWall("security_camera_01", new Vector3(213f, 4.4f, -2f), Vector3.back, 0.55f, 14f);
            OnWall("security_camera_01", new Vector3(162f, 4.4f, 12f), Vector3.left, 0.55f, 14f);
            OnWall("security_camera_01", new Vector3(-95.7f, 3.2f, 117.4f), Vector3.forward, 0.55f, 12f); // Northway House
            OnWall("security_camera_01", new Vector3(-56.3f, 3.2f, 117.4f), Vector3.forward, 0.55f, 12f);
            OnWall("security_camera_02", new Vector3(-100.4f, 4.6f, 66f), Vector3.back, 0.34f, 15f);    // the mill's corners
            OnWall("security_camera_02", new Vector3(-135.6f, 4.6f, 66f), Vector3.back, 0.34f, 15f);
            OnWall("security_camera_02", new Vector3(-80.6f, 3.0f, 140f), Vector3.back, 0.34f, 15f);    // the car park's way in
            OnWall("security_camera_02", new Vector3(-152.2f, 4.7f, 117.4f), Vector3.forward, 0.34f, 15f); // the bookies on the corner
            // bulkhead lamps by the doors (lit), and round the back of the mill
            void Lamp(Vector3 w, Vector3 n)
            {
                var g = OnWall("industrial_wall_lamp", w + Vector3.down * 0.2f, n, 0.14f); if (g == null) return;
                var lg = new GameObject("Light"); lg.transform.SetParent(g.transform, false); lg.transform.localPosition = new Vector3(0f, 0.2f, 0.25f);
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.8f, 0.55f); l.range = 6f; l.intensity = 1.2f; l.shadows = LightShadows.None;
            }
            Lamp(new Vector3(-89.8f, 3.4f, 66f), Vector3.back); Lamp(new Vector3(-94.2f, 3.4f, 66f), Vector3.back);          // engine house
            Lamp(new Vector3(-116.8f, 3.1f, 52f), Vector3.forward); Lamp(new Vector3(-111.2f, 3.1f, 52f), Vector3.forward);  // snooker club
            Lamp(new Vector3(-73.2f, 2.6f, 66f), Vector3.back); Lamp(new Vector3(-68.8f, 2.6f, 66f), Vector3.back);          // Kestrel Court
            foreach (float x in new[] { -105f, -118f, -131f }) Lamp(new Vector3(x, 4.2f, 82f), Vector3.forward);               // the mill's back, over the parking
            // telegraph poles down the back alleys
            foreach (float z in new[] { 16f, 34f, 52f }) PutClear("electricity_pole_02", new Vector3(-133.9f, 0f, z), 90f, new Vector3(0.2f, 2f, 0.2f));
            foreach (float z in new[] { 190f, 170f }) PutClear("electricity_pole_02", new Vector3(102.5f, 0f, z), 90f, new Vector3(0.2f, 2f, 0.2f));
            foreach (float z in new[] { 16f, 0f }) PutClear("electricity_pole_03", new Vector3(102.6f, 0f, z), 90f, new Vector3(0.2f, 2f, 0.2f));
            // shutters over the back windows of Kestrel Parade
            foreach (float u in new[] { 6.5f, 19f, 31.5f, 44f }) OnWall("rollershutter_window_02_graffiti", new Vector3(-141.4f, 0.9f, 60.4f + u + 3.6f), Vector3.right, 0.17f);
            // the mill yard: its gates, two cars under covers in the bays
            Put("large_iron_gate", new Vector3(-138.9f, 0f, 66.1f), 0f);
            Put("covered_car", new Vector3(-128.9f, 0f, 94.6f), 180f); Put("covered_car", new Vector3(-113.3f, 0f, 94.4f), 2f);
            // tyres and rims dumped: behind the snooker club, down the alleys, on the waste ground
            for (int k = 0; k < 4; k++) Flat("old_tyre", new Vector3(-126f, 0f, 35f), k, R(0, 360), 0.3f, 0.16f);
            for (int k = 0; k < 3; k++) Flat("old_tyre", new Vector3(-124.8f, 0f, 35.1f), k, R(0, 360), 0.3f, 0.16f);
            foreach (var p in new[] { new Vector3(-133.2f, 0f, 22f), new Vector3(-133.4f, 0f, 47f), new Vector3(-78f, 0f, 28f), new Vector3(-74f, 0f, 31f), new Vector3(103.2f, 0f, 178f), new Vector3(-91f, 0f, 97f) })
                PutClear("old_tyre", p, R(0, 360), new Vector3(0.3f, 0.3f, 0.1f));
            Flat("rusted_wheel_rim_01", new Vector3(-122.6f, 0f, 35f), 0, R(0, 360), 0.2f, 0.15f);
            foreach (var p in new[] { new Vector3(-76f, 0f, 29.5f), new Vector3(103f, 0f, 2f) }) PutClear("rusted_wheel_rim_01", p, R(0, 360), new Vector3(0.2f, 0.2f, 0.08f));
            // footballs on the greens
            foreach (var p in new[] { new Vector3(-76f, 0f, 63f), new Vector3(10f, 0f, -62f), new Vector3(-66f, 0f, 30f) }) PutClear("football", p, R(0, 360), new Vector3(0.12f, 0.1f, 0.12f));
            // burning barrels where the gangs hang about, and one in the mill yard
            if (embers == null)
            {
                embers = new Material(Gfx.Lit) { name = "Embers" }; embers.SetColor("_BaseColor", new Color(0.2f, 0.06f, 0.02f));
                embers.EnableKeyword("_EMISSION"); embers.SetColor("_EmissionColor", new Color(1f, 0.38f, 0.08f) * 3.5f); embers.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            foreach (var p in new[] { new Vector3(-19.5f, 0f, -162.5f), new Vector3(-193.5f, 0f, 47f), new Vector3(264.5f, 0f, 97f), new Vector3(-89.5f, 0f, 98.5f) })
            {
                var g = PutClear("barrel_stove", p, R(0, 360), new Vector3(0.3f, 0.42f, 0.3f)); if (g == null) continue;
                var e = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(e.GetComponent<Collider>());
                e.transform.SetParent(g.transform, false); e.transform.localPosition = new Vector3(0f, 0.78f, 0f); e.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);
                var mr = e.GetComponent<MeshRenderer>(); mr.sharedMaterial = embers; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var lg = new GameObject("Fire"); lg.transform.SetParent(g.transform, false); lg.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.55f, 0.2f); l.range = 7f; l.intensity = 2.6f; l.shadows = LightShadows.None;
                var src = lg.AddComponent<AudioSource>(); src.spatialBlend = 1f; src.rolloffMode = AudioRolloffMode.Linear; src.minDistance = 1f; src.maxDistance = 12f; src.volume = 0.45f; src.playOnAwake = false;
                lg.AddComponent<FireFlicker>();
            }
            Debug.Log($"[ASBO] dressing: {root.childCount - n0} pieces of street furniture and junk");
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
