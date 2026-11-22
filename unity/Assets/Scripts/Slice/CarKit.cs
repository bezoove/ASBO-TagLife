using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// The modelled UK cars (built in Blender from the reference photos: 90s hatch and saloon, boxy 80s saloon,
    /// supermini, 70s chrome-bumper hatch, rally saloon with a wing, wedge coupé, square estate, small 4x4).
    /// Each prefab in Resources/Cars has a clean and a battered (dented) body at two levels of detail and four wheels.
    /// The paint is made per car from a baked wear map (dirt, rust, shading, panel ids): clean, tired and faded,
    /// filthy, rusty with a mismatched panel, a smashed-up wreck, or a burnt-out shell.
    /// </summary>
    public static class CarKit
    {
        public enum Wear { Clean, Tired, Dirty, Rusty, Wreck, Burnt }

        static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, Color32[]> wearPx = new Dictionary<string, Color32[]>();
        static readonly Dictionary<string, Material> paintCache = new Dictionary<string, Material>();
        static Material smashed, gone, darkLamps;

        public static bool Has(string id) => Prefab(id) != null;
        static GameObject Prefab(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (!prefabs.TryGetValue(id, out var p)) { p = Resources.Load<GameObject>("Cars/" + id); prefabs[id] = p; }
            return p;
        }

        /// <summary>How rough estate traffic is: mostly tired, some clean, some filthy or rusting, the odd wreck.</summary>
        public static Wear PickWear(System.Random r, bool rough)
        {
            if (!rough) return Wear.Clean;
            double t = r.NextDouble();
            return t < 0.42 ? Wear.Clean : t < 0.76 ? Wear.Tired : t < 0.87 ? Wear.Dirty : t < 0.97 ? Wear.Rusty : Wear.Wreck;
        }

        public static Transform[] Build(Transform parent, CarBody.Spec s, Color paint, bool castShadow, int seed, bool rough)
        {
            var rng = new System.Random(seed >= 0 ? seed : Random.Range(0, int.MaxValue));
            return Build(parent, s.m.id, paint, PickWear(rng, rough), castShadow, rng);
        }

        /// <summary>Builds the car under parent (origin on the ground at its middle, front = +z). Returns the wheels FL, FR, RL, RR.</summary>
        public static Transform[] Build(Transform parent, string id, Color paint, Wear wear, bool castShadow, System.Random rng)
        {
            if (wear <= Wear.Dirty && Pack(id) != null) return BuildPack(parent, id, paint, wear, castShadow, rng);
            var root = Object.Instantiate(Prefab(id), parent, false).transform;
            root.name = "Body"; root.localPosition = Vector3.zero; root.localRotation = Quaternion.identity;
            bool dented = wear >= Wear.Rusty || wear == Wear.Dirty && rng.NextDouble() < 0.35;
            Transform c0 = root.Find("car_LOD0"), c1 = root.Find("car_LOD1"), d0 = root.Find("dent_LOD0"), d1 = root.Find("dent_LOD1");
            if (d0 == null) dented = false;
            Transform b0 = dented ? d0 : c0, b1 = dented ? d1 : c1;
            foreach (var t in new[] { c0, c1, d0, d1 }) if (t != null) t.gameObject.SetActive(t == b0 || t == b1);

            // materials: paint made for this car, glass and lamps by condition, its own plates
            var r0 = b0.GetComponent<MeshRenderer>(); var r1 = b1 != null ? b1.GetComponent<MeshRenderer>() : null;
            var mats = r0.sharedMaterials;
            int mismatch = wear >= Wear.Rusty && rng.NextDouble() < 0.6 ? 4 + rng.Next(8) : wear == Wear.Tired && rng.NextDouble() < 0.12 ? 4 + rng.Next(8) : -1;
            Color panelCol = CarBody.RandomPaint(rng);
            float fade = wear == Wear.Tired ? 0.15f + (float)rng.NextDouble() * 0.2f : wear >= Wear.Dirty ? 0.25f : (float)rng.NextDouble() * 0.06f;
            bool metallic = wear <= Wear.Tired && rng.NextDouble() < 0.5;
            if (mats.Length > 0) mats[0] = Paint(id, paint, wear, mismatch, panelCol, fade, metallic);
            if (mats.Length > 3 && (wear == Wear.Wreck || wear == Wear.Burnt)) mats[3] = wear == Wear.Burnt ? Gone() : Smashed();
            if (mats.Length > 4 && wear == Wear.Burnt) mats[4] = DarkLamps(mats[1]);
            if (mats.Length > 5)
            {
                if (wear == Wear.Burnt) mats[5] = mats[1];
                else mats[5] = PlateMat(mats[5], rng);
            }
            r0.sharedMaterials = mats;
            if (r1 != null) r1.sharedMaterials = mats;
            var sh = castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r0.shadowCastingMode = sh; if (r1 != null) r1.shadowCastingMode = sh;
            // the driver's door is its own part: hung on a hinge at its front edge so it can swing open
            Transform dr0 = root.Find("door_LOD0"), dr1 = root.Find("dentdoor_LOD0"), door = dented && dr1 != null ? dr1 : dr0;
            foreach (var t in new[] { dr0, dr1 }) if (t != null) t.gameObject.SetActive(t == door);
            MeshRenderer doorR = null;
            if (door != null && door.GetComponent<MeshFilter>() is MeshFilter dmf && dmf.sharedMesh != null)
            {
                doorR = door.GetComponent<MeshRenderer>(); doorR.sharedMaterials = mats; doorR.shadowCastingMode = sh;
                var b = dmf.sharedMesh.bounds; Vector3 mn = Vector3.one * 1e9f, mx = -Vector3.one * 1e9f;
                for (int k = 0; k < 8; k++)
                {
                    var corner = new Vector3((k & 1) == 0 ? b.min.x : b.max.x, (k & 2) == 0 ? b.min.y : b.max.y, (k & 4) == 0 ? b.min.z : b.max.z);
                    var q = root.InverseTransformPoint(door.TransformPoint(corner)); mn = Vector3.Min(mn, q); mx = Vector3.Max(mx, q);
                }
                float side = Mathf.Sign(mn.x + mx.x);
                var hinge = new GameObject("DoorHinge").transform; hinge.SetParent(root, false);
                hinge.localPosition = new Vector3(side > 0 ? mx.x : mn.x, (mn.y + mx.y) * 0.5f, mx.z);
                door.SetParent(hinge, true);
                hinge.name = "DoorHinge|" + (mx.z - mn.z).ToString("F2", System.Globalization.CultureInfo.InvariantCulture); // its length, for whoever opens it
            }

            // wheels: wrapped so the vehicle code can steer and spin them the way it always has
            var wheels = new Transform[4]; var wheelRs = new List<Renderer>();
            string[] names = { "wheel_FL", "wheel_FR", "wheel_RL", "wheel_RR" };
            for (int i = 0; i < 4; i++)
            {
                var w = root.Find(names[i]);
                if (w == null) { wheels[i] = new GameObject(names[i]).transform; wheels[i].SetParent(root, false); continue; }
                var wrap = new GameObject(names[i] + "_spin").transform;
                wrap.SetParent(root, false); wrap.localPosition = w.localPosition;
                w.SetParent(wrap, true);
                w.localRotation = Quaternion.Euler(0f, 0f, -90f) * w.localRotation;
                w.localPosition = Vector3.zero;
                wrap.localRotation = Quaternion.Euler(0f, 0f, 90f); // the vehicle code sets Euler(steer) * Euler(spin, 0, 90)
                var wr = w.GetComponent<MeshRenderer>(); if (wr != null) { wr.shadowCastingMode = sh; wheelRs.Add(wr); }
                wheels[i] = wrap;
            }
            if (wear == Wear.Burnt) root.localPosition = Vector3.down * 0.07f; // sat on its rims
            // a soft dark patch under the car so it sits on the road whatever the sun's doing
            var shadowM = Resources.Load<Material>("CarMats/Shadow");
            if (shadowM != null)
            {
                var spec = CarBody.Get(id).m;
                var blob = new GameObject("ContactShadow"); blob.transform.SetParent(parent, false); blob.transform.localPosition = new Vector3(0, 0.025f, 0);
                float hw2 = spec.W * 0.5f + 0.18f, hl = spec.L * 0.5f + 0.22f;
                var shm = new Mesh { name = "CarShadow" };
                shm.vertices = new[] { new Vector3(-hw2, 0, -hl), new Vector3(hw2, 0, -hl), new Vector3(hw2, 0, hl), new Vector3(-hw2, 0, hl) };
                shm.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                shm.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up }; shm.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                blob.AddComponent<MeshFilter>().sharedMesh = shm;
                var shr = blob.AddComponent<MeshRenderer>(); shr.sharedMaterial = shadowM; shr.shadowCastingMode = ShadowCastingMode.Off; shr.receiveShadows = false;
                shr.lightProbeUsage = LightProbeUsage.Off; shr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            var lg = root.GetComponent<LODGroup>(); if (lg == null) lg = root.gameObject.AddComponent<LODGroup>();
            var l0 = new List<Renderer>(wheelRs) { r0 }; if (doorR != null) l0.Add(doorR); var l1 = new List<Renderer>(wheelRs); if (r1 != null) l1.Add(r1);
            lg.SetLODs(new[] { new LOD(0.035f, l0.ToArray()), new LOD(0.006f, l1.ToArray()) }); // the detailed car right out to the distance; the low one only far off
            lg.RecalculateBounds();
            return wheels;
        }

        // number plates come from a pool of registrations: cars that share one share a material (fewer draws, less memory)
        static readonly Dictionary<(Material, int), Material> platePool = new Dictionary<(Material, int), Material>();
        const int PlatePoolSize = 14;
        static Material PlateMat(Material tpl, System.Random rng)
        {
            int k = rng.Next(PlatePoolSize);
            if (platePool.TryGetValue((tpl, k), out var m) && m != null) return m;
            m = new Material(tpl) { name = "Plates " + k }; var pt = CarBody.PlateAtlas(new System.Random(7919 * (k + 1))); m.SetTexture("_BaseMap", pt); m.mainTexture = pt;
            platePool[(tpl, k)] = m; return m;
        }

        /// <summary>
        /// A parked car is looked at from further off than one you're chasing: its detailed model gives way to the low one
        /// sooner, and far off the low one goes without its wheels (a dozen draws a car down to a few).
        /// </summary>
        public static void SceneryLod(Transform car)
        {
            var lg = car.GetComponentInChildren<LODGroup>(); if (lg == null) return;
            var lods = lg.GetLODs(); if (lods.Length < 2) return;
            var body = new List<Renderer>(); foreach (var r in lods[1].renderers) if (r != null && !r.name.StartsWith("wheel")) body.Add(r);
            lg.SetLODs(new[] { new LOD(0.12f, lods[0].renderers), new LOD(0.035f, lods[1].renderers), new LOD(0.008f, body.ToArray()) });
        }

        // ------------------------------------------------------------------------------------------- pack models
        static readonly Dictionary<string, GameObject> packs = new Dictionary<string, GameObject>();
        /// <summary>The Asset Store model standing in for this kind of car, if it's in the build (ASBO/Cars/Use Pack Cars).</summary>
        static GameObject Pack(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (!packs.TryGetValue(id, out var p)) { p = Resources.Load<GameObject>("CarsPack/" + id); packs[id] = p; }
            return p;
        }

        /// <summary>
        /// The pack's detailed model for this car (the hatch and the saloon): its own paint colour (faded if tired, grimy
        /// if dirty), its door on a hinge, its wheels wrapped for the driving code. Far off it swaps to our own low model.
        /// </summary>
        static Transform[] BuildPack(Transform parent, string id, Color paint, Wear wear, bool castShadow, System.Random rng)
        {
            var root = Object.Instantiate(Pack(id), parent, false).transform;
            root.name = "Body"; root.localPosition = Vector3.zero; root.localRotation = Quaternion.identity;
            float fade = wear == Wear.Tired ? 0.15f + (float)rng.NextDouble() * 0.2f : wear == Wear.Dirty ? 0.25f : (float)rng.NextDouble() * 0.06f;
            Color col = Color.Lerp(paint, new Color(0.62f, 0.62f, 0.6f), fade);
            if (wear == Wear.Dirty) col = Color.Lerp(col, new Color(0.33f, 0.29f, 0.22f), 0.3f);
            var sh = castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
            Material paintM = null, plateM = null;
            var near = new List<Renderer>();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (mats[i].name.EndsWith("_Paint"))
                    {
                        if (paintM == null) { paintM = new Material(mats[i]) { name = id + " paint" }; paintM.SetColor("_BaseColor", col); paintM.SetFloat("_Smoothness", wear == Wear.Clean ? 0.66f : wear == Wear.Tired ? 0.45f : 0.28f); }
                        mats[i] = paintM;
                    }
                    else if (mats[i].name.EndsWith("_Plates"))
                    {
                        // its own registration (the pack's plates said "SEDAN" and "1988")
                        if (plateM == null) plateM = PlateMat(mats[i], rng);
                        mats[i] = plateM;
                    }
                }
                r.sharedMaterials = mats; r.shadowCastingMode = sh; near.Add(r);
            }
            var wheels = new Transform[4]; string[] names = { "wheel_FL", "wheel_FR", "wheel_RL", "wheel_RR" };
            for (int i = 0; i < 4; i++) { var w = root.Find(names[i]); if (w == null) { w = new GameObject(names[i]).transform; w.SetParent(root, false); } wheels[i] = w; }
            // far off: our own low model in the same colour (no wheels to draw, a fraction of the triangles)
            Renderer far = null; var ours = Prefab(id);
            var low = ours != null ? ours.transform.Find("car_LOD1") : null;
            if (low != null)
            {
                var lg0 = Object.Instantiate(low.gameObject, root, false); lg0.name = "far_LOD1"; lg0.SetActive(true);
                far = lg0.GetComponent<MeshRenderer>();
                if (far != null) { var m = far.sharedMaterials; if (m.Length > 0) m[0] = Paint(id, col, Wear.Clean, -1, col, 0f, false); far.sharedMaterials = m; far.shadowCastingMode = ShadowCastingMode.Off; }
            }
            var lg = root.gameObject.AddComponent<LODGroup>();
            lg.SetLODs(far != null ? new[] { new LOD(0.07f, near.ToArray()), new LOD(0.006f, new[] { far }) } : new[] { new LOD(0.006f, near.ToArray()) });
            lg.RecalculateBounds();
            ContactShadow(parent, id);
            return wheels;
        }

        static void ContactShadow(Transform parent, string id)
        {
            var shadowM = Resources.Load<Material>("CarMats/Shadow"); if (shadowM == null) return;
            var spec = CarBody.Get(id).m;
            var blob = new GameObject("ContactShadow"); blob.transform.SetParent(parent, false); blob.transform.localPosition = new Vector3(0, 0.025f, 0);
            float hw2 = spec.W * 0.5f + 0.18f, hl = spec.L * 0.5f + 0.22f;
            var shm = new Mesh { name = "CarShadow" };
            shm.vertices = new[] { new Vector3(-hw2, 0, -hl), new Vector3(hw2, 0, -hl), new Vector3(hw2, 0, hl), new Vector3(-hw2, 0, hl) };
            shm.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            shm.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up }; shm.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            blob.AddComponent<MeshFilter>().sharedMesh = shm;
            var shr = blob.AddComponent<MeshRenderer>(); shr.sharedMaterial = shadowM; shr.shadowCastingMode = ShadowCastingMode.Off; shr.receiveShadows = false;
            shr.lightProbeUsage = LightProbeUsage.Off; shr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        // ------------------------------------------------------------------------------------------- paint
        static Color32[] WearMap(string id)
        {
            if (wearPx.TryGetValue(id, out var px)) return px;
            var t = Resources.Load<Texture2D>("Cars/" + id + "_wear");
            px = t != null && t.isReadable ? t.GetPixels32() : null;
            wearPx[id] = px; return px;
        }

        static Material Paint(string id, Color paint, Wear wear, int mismatch, Color panelCol, float fade, bool metallic)
        {
            // cars that look alike share a material (keeps memory and draw setup down)
            Color32 pc = paint; Color32 mc = panelCol;
            string key = $"{id}|{pc.r},{pc.g},{pc.b}|{wear}|{mismatch}|{(mismatch >= 0 ? $"{mc.r},{mc.g},{mc.b}" : "")}|{Mathf.RoundToInt(fade * 10)}|{metallic}";
            if (paintCache.TryGetValue(key, out var have) && have != null) return have;
            var tpl = Resources.Load<Material>("CarMats/KitPaint");
            var m = tpl != null ? new Material(tpl) : new Material(Gfx.Lit);
            m.name = "Paint " + id;
            m.SetFloat("_Metallic", metallic ? 0.45f : 0.1f); // half the cars in metallic paint, like the real thing
            var src = WearMap(id);
            if (src != null)
            {
                int n = src.Length, size = Mathf.RoundToInt(Mathf.Sqrt(n));
                var px = new Color32[n];
                float dirtAmt = wear switch { Wear.Clean => 0.02f, Wear.Tired => 0.12f, Wear.Dirty => 0.75f, Wear.Rusty => 0.4f, Wear.Wreck => 0.8f, _ => 0.5f };
                float rustAmt = wear switch { Wear.Clean => 0f, Wear.Tired => 0.18f, Wear.Dirty => 0.25f, Wear.Rusty => 0.85f, Wear.Wreck => 1f, _ => 1f };
                float gloss = wear switch { Wear.Clean => 0.88f, Wear.Tired => 0.72f, Wear.Dirty => 0.5f, _ => 0.4f };
                Color faded = Color.Lerp(paint, Color.Lerp(paint, new Color(0.8f, 0.79f, 0.76f), 0.7f), fade);
                Color fadedPanel = Color.Lerp(panelCol, new Color(0.8f, 0.79f, 0.76f), fade * 0.5f);
                Color dirtC = new Color(0.27f, 0.23f, 0.18f), rustD = new Color(0.22f, 0.1f, 0.05f), rustL = new Color(0.58f, 0.3f, 0.12f);
                Color soot = new Color(0.07f, 0.065f, 0.06f), ash = new Color(0.47f, 0.45f, 0.42f);
                for (int i = 0; i < n; i++)
                {
                    var w = src[i];
                    float dirt = w.r / 255f, rust = w.g / 255f, ao = w.b / 255f; int pid = (int)(w.a / 255f * 16f);
                    float shade = Mathf.Lerp(0.62f, 1f, ao);
                    Color c; float sm;
                    if (wear == Wear.Burnt)
                    {
                        // scorched bare metal, orange rust coming through, a bit of paint left on the ends
                        c = Color.Lerp(soot, ash, Mathf.Clamp01(dirt * 1.2f) * 0.6f);
                        c = Color.Lerp(c, Color.Lerp(rustD, rustL, rust), Mathf.SmoothStep(0.15f, 0.55f, rust));
                        if (pid == 12 && rust < 0.4f) c = Color.Lerp(c, faded * 0.8f, 0.7f);
                        c *= shade; sm = 0.12f;
                    }
                    else
                    {
                        c = (pid == mismatch ? fadedPanel : faded) * shade;
                        c = Color.Lerp(c, dirtC * shade, Mathf.Clamp01(dirt * dirtAmt) * 0.9f);
                        float rm = Mathf.SmoothStep(0.25f, 0.6f, rust * rustAmt);
                        c = Color.Lerp(c, Color.Lerp(rustD, rustL, rust) * shade, rm);
                        sm = Mathf.Lerp(gloss, 0.15f, Mathf.Max(Mathf.Clamp01(dirt * dirtAmt), rm));
                    }
                    px[i] = new Color(c.r, c.g, c.b, sm);
                }
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = m.name, wrapMode = TextureWrapMode.Clamp, anisoLevel = 2 };
                tex.SetPixels32(px); tex.Apply(true, true);
                m.SetTexture("_BaseMap", tex); m.mainTexture = tex;
                m.SetColor("_BaseColor", Color.white);
            }
            else m.SetColor("_BaseColor", paint);
            paintCache[key] = m; return m;
        }

        // ------------------------------------------------------------------------------------------- glass for wrecks
        static Material Cutout(string name, Texture2D tex)
        {
            var tpl = Resources.Load<Material>("GoreMats/BloodSplat"); // an alpha-tested template: a runtime one loses its cut-out variant in builds
            var m = tpl != null ? new Material(tpl) : new Material(Gfx.Lit);
            m.name = name; m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.5f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.85f);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            return m;
        }

        /// <summary>Smashed windows: jagged holes with shards round the edges, crazed glass left in places.</summary>
        static Material Smashed()
        {
            if (smashed != null) return smashed;
            const int S = 128; var px = new Color32[S * S]; var r = new System.Random(5);
            var holes = new List<Vector3>();
            for (int i = 0; i < 9; i++) holes.Add(new Vector3((float)r.NextDouble() * S, (float)r.NextDouble() * S, 14f + (float)r.NextDouble() * 40f));
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float a = 1f;
                    foreach (var h in holes)
                    {
                        float dx = x - h.x, dy = y - h.y; float ang = Mathf.Atan2(dy, dx);
                        float rr = h.z * (0.75f + 0.25f * Mathf.Sin(ang * 7f + h.x) + 0.15f * Mathf.Sin(ang * 13f));
                        if (dx * dx + dy * dy < rr * rr) a = 0f;
                    }
                    float crack = Mathf.Abs(Mathf.Sin((x * 0.31f + y * 0.17f)) * Mathf.Sin(x * 0.07f - y * 0.23f));
                    byte g = (byte)(crack > 0.92f ? 150 : 22);
                    px[y * S + x] = new Color32(g, (byte)(g + 4), (byte)(g + 6), (byte)(a * 255));
                }
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "Smashed" };
            tex.SetPixels32(px); tex.Apply(true, true);
            smashed = Cutout("SmashedGlass", tex); smashed.SetTextureScale("_BaseMap", new Vector2(9f, 9f));
            return smashed;
        }

        /// <summary>Burnt out: the glass has gone (only a few melted scraps round the frames).</summary>
        static Material Gone()
        {
            if (gone != null) return gone;
            const int S = 64; var px = new Color32[S * S]; var r = new System.Random(9);
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(20, 18, 16, (byte)(r.NextDouble() < 0.04 ? 255 : 0));
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "NoGlass" };
            tex.SetPixels32(px); tex.Apply(false, true);
            gone = Cutout("NoGlass", tex); return gone;
        }

        static Material DarkLamps(Material dark) { if (darkLamps == null) darkLamps = dark; return darkLamps; }

        /// <summary>A parked or abandoned car as scenery: wrecks and burnt-out shells for the estate's corners.</summary>
        public static GameObject Scenery(Transform parent, string id, Wear wear, Vector3 pos, float yaw, int seed)
        {
            if (!Has(id)) return null;
            bool drivable = wear < Wear.Wreck;
            var g = new GameObject((drivable ? "Parked " : "Wreck ") + id); g.transform.SetParent(parent, false);
            // sat on whatever is under it (a road's surface is a few centimetres up: at y=0 the tyres sank into the tarmac)
            if (Physics.Raycast(pos + Vector3.up * 2.5f, Vector3.down, out var gh, 6f, ~((1 << 2) | (1 << Person.VehicleLayer) | (1 << Person.RagdollLayer) | (1 << World.CamBlockLayer)), QueryTriggerInteraction.Ignore)) pos.y = gh.point.y + 0.005f;
            g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            Color paint = CarBody.RandomPaint(new System.Random(seed ^ 0x5bd1e995)); // its own stream, so the car rebuilds the same when someone gets in
            Build(g.transform, id, paint, wear, true, new System.Random(seed));
            SceneryLod(g.transform);
            var spec = CarBody.Get(id).m;
            var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(spec.W, 1.3f, spec.L); bc.center = new Vector3(0f, 0.7f, 0f);
            Dents.Add(g, g.transform, 1200f);
            var ob = g.AddComponent<UnityEngine.AI.NavMeshObstacle>(); ob.shape = UnityEngine.AI.NavMeshObstacleShape.Box; // people walk round it
            ob.center = bc.center; ob.size = bc.size + new Vector3(0.4f, 0f, 0.4f); ob.carving = true; ob.carveOnlyStationary = true;
            if (drivable) { var pc = g.AddComponent<ParkedCar>(); pc.id = id; pc.wear = wear; pc.paint = paint; pc.seed = seed; }
            return g;
        }
    }

    /// <summary>
    /// A parked car you can get into (anything short of a wreck). Get in and it becomes a proper drivable car of the
    /// same kind, paint and condition, dents and all, handling like that kind of car does.
    /// </summary>
    public class ParkedCar : MonoBehaviour
    {
        public string id; public CarKit.Wear wear; public Color paint; public int seed;
        public static readonly List<ParkedCar> All = new List<ParkedCar>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static ParkedCar Nearest(Vector3 p, float reach)
        {
            ParkedCar best = null; float bd = reach * reach;
            foreach (var c in All) { if (c == null) continue; var d = c.transform.position - p; d.y = 0f; if (d.sqrMagnitude < bd) { bd = d.sqrMagnitude; best = c; } }
            return best;
        }

        /// <summary>Swap this parked car for a drivable one in the same spot.</summary>
        public PlayerCar TakeOver()
        {
            var c = PlayerCar.Create(transform.position + Vector3.up * 0.12f, transform.eulerAngles.y, paint, id, wear, seed, transform.parent);
            gameObject.SetActive(false); Destroy(gameObject);
            return c;
        }
    }
}
