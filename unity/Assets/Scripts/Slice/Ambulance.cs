using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ASBO.Slice
{
    /// <summary>
    /// A UK ambulance call-out, start to finish. Blues and twos down the road, parks up (siren off, lights on). Two
    /// paramedics in dark green get out, go round the back, open the doors and take the kit: response bag,
    /// defibrillator, oxygen. They put the bags down by the casualty, kneel and treat them (hands on, oxygen mask on).
    /// One fetches the stretcher and wheels it over; together they lift the casualty onto it, cover them with the
    /// blanket (a sheet over the face if they've gone), and push it to the back. The stretcher goes in, the doors shut,
    /// they climb in and drive off. The van is solid: cars stop for it and people walk round it.
    /// </summary>
    public class Ambulance : MonoBehaviour
    {
        public static string Status = "";
        static readonly HashSet<Person> dispatched = new HashSet<Person>();
        public static readonly List<Ambulance> All = new List<Ambulance>();
        public static void ResetAll() { dispatched.Clear(); Status = ""; }

        Light blueA, blueB; Material lightOn, lightOff; Renderer[] barA, barB;
        Transform doorL, doorR; Transform[] wheels; float wheelSpin, wheelR = 0.33f; Vector3 lastPos;
        Quaternion doorBaseL = Quaternion.identity, doorBaseR = Quaternion.identity; float doorSignL = 1f, doorSignR = -1f; // + swings a left-hinged door out
        float floorY = 0.42f;
        float hw, Le, roof; AudioSource siren; NavMeshObstacle obstacle;
        readonly List<(Transform kit, Person who, bool left)> carried = new List<(Transform, Person, bool)>();
        public float HalfLength => Le; public float HalfWidth => hw;

        public static void Dispatch(Person victim)
        {
            if (!dispatched.Add(victim)) return;
            var go = new GameObject("Ambulance");
            go.transform.SetParent(World.Root, false);
            go.layer = Person.VehicleLayer;
            var a = go.AddComponent<Ambulance>();
            a.BuildVehicle();
            a.StartCoroutine(a.Run(victim));
        }
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        // ================================================================================================ the van
        /// <summary>
        /// The panel-van ambulance in high-visibility yellow: green and yellow Battenburg squares down both sides,
        /// red and yellow chevrons on the back doors (which open), AMBULANCE along the sides and mirror-written on the
        /// front, a blue light bar with corner beacons and flashing blues in the grille, side scene lamps, roof pod.
        /// </summary>
        void BuildVehicle()
        {
            if (!BuildModel()) BuildPanelVan();
            // solid: one box for the whole van, so the player, cars and the player's car hit it; people path round it
            var col = gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, roof * 0.5f + 0.1f, 0); col.size = new Vector3(hw * 2f, roof - 0.15f, Le * 2f);
            var rb = gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate;
            obstacle = gameObject.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Box; obstacle.center = col.center; obstacle.size = col.size + new Vector3(0.3f, 0, 0.3f);
            obstacle.carving = true; obstacle.carveOnlyStationary = true;
            siren = gameObject.AddComponent<AudioSource>();
            siren.clip = SirenClip(); siren.loop = true; siren.spatialBlend = 1f; siren.minDistance = 10f; siren.maxDistance = 160f; siren.rolloffMode = AudioRolloffMode.Linear; siren.volume = 0.5f;
            siren.Play();
            lastPos = transform.position;
        }

        /// <summary>
        /// The modelled box ambulance (Resources/Cars/ambulance from tools/art/cars/ambforge.py), as in the photos: a
        /// yellow box body over a van cab, two-row green Battenburg from behind the front arch, EMERGENCY AMBULANCE on
        /// the sides, AMBULANCE mirror-written on the front, light bar and beacons, red and yellow chevrons on two rear
        /// doors that open onto a white treatment room (cabinets, bench, seat, bags). False if it isn't in the build.
        /// </summary>
        bool BuildModel()
        {
            var pf = Resources.Load<GameObject>("Cars/ambulance"); if (pf == null) return false;
            var root = Instantiate(pf, transform, false).transform;
            root.name = "Body"; root.localPosition = Vector3.zero; root.localRotation = Quaternion.identity;
            hw = 1.05f; Le = 3.45f; roof = 2.96f; wheelR = 0.36f; floorY = 0.56f;
            // its own number plates
            Material plates = null; var rng = new System.Random(777 + Random.Range(0, 9999));
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var ms = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < ms.Length; i++)
                {
                    if (ms[i] == null || !ms[i].name.StartsWith("KitPlate")) continue;
                    if (plates == null) { plates = new Material(ms[i]) { name = "Plates" }; var tex = CarBody.PlateAtlas(rng); plates.SetTexture("_BaseMap", tex); plates.mainTexture = tex; }
                    ms[i] = plates; changed = true;
                }
                if (changed) r.sharedMaterials = ms;
            }
            // wheels: wrapped like the other modelled vehicles, so Update spins them with Euler(spin, 0, 90)
            string[] names = { "wheel_FL", "wheel_FR", "wheel_RL", "wheel_RR" }; var ws = new List<Transform>();
            foreach (var n in names)
            {
                var w = root.Find(n); if (w == null) continue;
                var wrap = new GameObject(n + "_spin").transform; wrap.SetParent(root, false); wrap.localPosition = w.localPosition;
                w.SetParent(wrap, true); w.localRotation = Quaternion.Euler(0f, 0f, -90f) * w.localRotation; w.localPosition = Vector3.zero;
                wrap.localRotation = Quaternion.Euler(0f, 0f, 90f);
                ws.Add(wrap);
            }
            wheels = ws.ToArray();
            // rear doors, hinged at the outside edges: work out which way each swings out (away from the back)
            doorL = root.Find("door_L"); doorR = root.Find("door_R");
            if (doorL == null || doorR == null) { Destroy(root.gameObject); return false; }
            doorBaseL = doorL.localRotation; doorBaseR = doorR.localRotation;
            doorSignL = OutSign(root, doorL); doorSignR = OutSign(root, doorR);
            // flashing lamps: the left half of the bar with the left beacon and grille blue, then the right
            lightOn = Gfx.Emissive(new Color(0.1f, 0.3f, 1f), 4f);
            Renderer[] Parts(params string[] ns) { var l = new List<Renderer>(); foreach (var n in ns) { var t = root.Find(n); if (t != null && t.TryGetComponent<Renderer>(out var r)) l.Add(r); } return l.ToArray(); }
            barA = Parts("bar_L", "beacon_L", "grille_L"); barB = Parts("bar_R", "beacon_R", "grille_R");
            lightOff = barA.Length > 0 ? barA[0].sharedMaterial : Gfx.Mat(new Color(0.05f, 0.1f, 0.3f));
            float barZ = barA.Length > 0 ? root.InverseTransformPoint(barA[0].bounds.center).z : Le - 1.6f;
            blueA = MakeLight(new Vector3(-0.5f, roof + 0.4f, barZ)); blueB = MakeLight(new Vector3(0.5f, roof + 0.4f, barZ));
            // a soft dark patch under it, like the cars
            var shadowM = Resources.Load<Material>("CarMats/Shadow");
            if (shadowM != null)
            {
                var blob = new GameObject("ContactShadow"); blob.transform.SetParent(transform, false); blob.transform.localPosition = new Vector3(0, 0.025f, 0);
                float a = hw + 0.18f, b = Le + 0.22f;
                var shm = new Mesh { name = "AmbShadow" };
                shm.vertices = new[] { new Vector3(-a, 0, -b), new Vector3(a, 0, -b), new Vector3(a, 0, b), new Vector3(-a, 0, b) };
                shm.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                shm.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up }; shm.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                blob.AddComponent<MeshFilter>().sharedMesh = shm;
                var shr = blob.AddComponent<MeshRenderer>(); shr.sharedMaterial = shadowM; shr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; shr.receiveShadows = false;
            }
            return true;
        }

        /// <summary>+1 or -1: the turn about the vertical that swings this door's free edge out behind the van.</summary>
        static float OutSign(Transform root, Transform door)
        {
            var r = door.GetComponentInChildren<Renderer>(); if (r == null) return 1f;
            Vector3 c = root.InverseTransformPoint(r.bounds.center) - door.localPosition;
            // turning +a about Y takes (x, z) to (x cos a + z sin a, -x sin a + z cos a): the edge goes back (-z) when a has x's sign
            return c.x >= 0f ? 1f : -1f;
        }

        void BuildPanelVan()
        {
            const float S = 1.14f;
            var spec = CarBody.Get("van_a");
            var vis = new GameObject("Body").transform; vis.SetParent(transform, false); vis.localScale = Vector3.one * S;
            wheels = CarBody.Build(vis, spec, new Color(0.97f, 0.84f, 0.06f), true, 777, false);
            wheelR = spec.m.rad * S;
            var m = spec.m; hw = m.hw * S; Le = m.L * 0.5f * S; roof = (m.gh.roof + m.gh.crown) * S;
            foreach (var c in GetComponentsInChildren<Collider>()) Destroy(c);
            var yellow = Gfx.Mat(new Color(0.98f, 0.86f, 0.05f), null, 0.6f);
            var green = Gfx.Mat(new Color(0.03f, 0.5f, 0.24f), null, 0.6f);
            var white = Gfx.Mat(new Color(0.93f, 0.93f, 0.92f), null, 0.55f);
            var dark = Gfx.Mat(new Color(0.06f, 0.065f, 0.07f), null, 0.3f);
            // Battenburg: two rows of squares from the back to the front wheel arch, both sides
            float sq = 0.3f, zA = -Le + 0.12f, zB = (m.wxF - m.archR) * S - 0.05f; int n = Mathf.Max(4, Mathf.FloorToInt((zB - zA) / sq));
            for (int i = 0; i < n; i++)
                foreach (int sd in new[] { -1, 1 })
                    for (int r = 0; r < 2; r++)
                        Gfx.Box(transform, new Vector3(sd * (hw - 0.01f), 0.62f + r * sq, zA + (i + 0.5f) * sq), new Vector3(0.012f, sq, sq), ((i + r) % 2 == 0) ? green : yellow, 0, "Batten", false);
            // lettering on the bodywork: along both sides above the squares, mirror-written on the front
            var ink = new Color(0.03f, 0.36f, 0.17f);
            foreach (int sd in new[] { -1, 1 })
                Lettering.Decal(transform, "AMBULANCE", ink, new Vector3(sd * (hw + 0.002f), 1.42f, -Le * 0.25f), new Vector3(sd, 0, 0), Vector3.up, 0.24f);
            Lettering.Decal(transform, "AMBULANCE", ink, new Vector3(0, roof - 0.32f, (m.gh.fr * S) + 0.06f), new Vector3(0, 0.25f, 1f), Vector3.up, 0.12f, true);
            // the back: an opening (dark, lit inside) behind two doors hinged at the corners, chevrons on the doors
            float doorH = roof - 0.5f, doorY = 0.42f + doorH * 0.5f;
            Gfx.Box(transform, new Vector3(0, doorY, -Le - 0.004f), new Vector3(hw * 1.8f, doorH, 0.006f), dark, 0, "Opening", false);
            Gfx.Box(transform, new Vector3(0, doorY + doorH * 0.45f, -Le + 0.002f), new Vector3(hw * 1.4f, 0.03f, 0.006f), Gfx.Emissive(new Color(1f, 0.97f, 0.9f), 1.6f), 0, "InteriorLight", false);
            var chev = Gfx.Mat(Color.white, ChevronTex(), 0.5f);
            doorL = Door(-1, doorH, doorY, chev); doorR = Door(1, doorH, doorY, chev);
            // rear step
            Gfx.Box(transform, new Vector3(0, 0.36f, -Le - 0.12f), new Vector3(hw * 1.5f, 0.05f, 0.22f), Gfx.Mat(new Color(0.3f, 0.3f, 0.32f), null, 0.4f, 0.6f), 0, "Step", false);
            // roof: light bar front, corner beacons at the back, air-con pod, scene lamps on the sides
            lightOn = Gfx.Emissive(new Color(0.1f, 0.3f, 1f), 4f); lightOff = Gfx.Mat(new Color(0.05f, 0.1f, 0.3f));
            float barZ = m.gh.fr * S - 0.25f;
            Gfx.Box(transform, new Vector3(0, roof + 0.03f, barZ), new Vector3(1.3f, 0.05f, 0.26f), white, 0, "BarBase", false);
            var a = Gfx.Box(transform, new Vector3(-0.36f, roof + 0.11f, barZ), new Vector3(0.62f, 0.11f, 0.22f), lightOff, 0, "BarA", false);
            var b = Gfx.Box(transform, new Vector3(0.36f, roof + 0.11f, barZ), new Vector3(0.62f, 0.11f, 0.22f), lightOff, 0, "BarB", false);
            var a2 = Gfx.Box(transform, new Vector3(-hw + 0.14f, roof - 0.02f, -Le + 0.08f), new Vector3(0.2f, 0.12f, 0.12f), lightOff, 0, "BeaconA", false);
            var b2 = Gfx.Box(transform, new Vector3(hw - 0.14f, roof - 0.02f, -Le + 0.08f), new Vector3(0.2f, 0.12f, 0.12f), lightOff, 0, "BeaconB", false);
            var a3 = Gfx.Box(transform, new Vector3(-0.42f, 0.55f, Le + 0.01f), new Vector3(0.14f, 0.05f, 0.02f), lightOff, 0, "GrilleA", false);
            var b3 = Gfx.Box(transform, new Vector3(0.42f, 0.55f, Le + 0.01f), new Vector3(0.14f, 0.05f, 0.02f), lightOff, 0, "GrilleB", false);
            barA = new[] { a.GetComponent<Renderer>(), a2.GetComponent<Renderer>(), a3.GetComponent<Renderer>() };
            barB = new[] { b.GetComponent<Renderer>(), b2.GetComponent<Renderer>(), b3.GetComponent<Renderer>() };
            Gfx.Box(transform, new Vector3(0, roof + 0.07f, -Le * 0.35f), new Vector3(0.75f, 0.14f, 0.95f), white, 0, "RoofPod", false);
            foreach (int sd in new[] { -1, 1 }) Gfx.Box(transform, new Vector3(sd * (hw + 0.01f), roof - 0.12f, -Le * 0.55f), new Vector3(0.03f, 0.1f, 0.3f), Gfx.Emissive(new Color(1f, 1f, 0.95f), 0.6f), 0, "SceneLamp", false);
            blueA = MakeLight(new Vector3(-0.5f, roof + 0.4f, barZ)); blueB = MakeLight(new Vector3(0.5f, roof + 0.4f, barZ));
        }

        Transform Door(int side, float h, float y, Material chev)
        {
            var hinge = new GameObject(side < 0 ? "DoorL" : "DoorR").transform; hinge.SetParent(transform, false);
            hinge.localPosition = new Vector3(side * hw * 0.92f, y, -Le - 0.012f);
            var panel = Gfx.Box(hinge, new Vector3(-side * hw * 0.46f, 0, 0), new Vector3(hw * 0.9f, h, 0.03f), chev, 0, "Panel", false);
            Gfx.Box(hinge, new Vector3(-side * hw * 0.46f, h * 0.28f, -0.017f), new Vector3(hw * 0.6f, h * 0.24f, 0.006f), Gfx.Mat(new Color(0.08f, 0.1f, 0.12f), null, 0.9f), 0, "Window", false);
            return hinge;
        }

        static Texture2D chevTex;
        /// <summary>Red and yellow chevrons (diagonal stripes pointing up the middle), as on the back of real ambulances.</summary>
        static Texture2D ChevronTex()
        {
            if (chevTex != null) return chevTex;
            const int W = 128, H = 256; var px = new Color32[W * H];
            Color32 rd = new Color32(200, 20, 18, 255), yl = new Color32(250, 215, 10, 255);
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) { float u = x / (float)W; float k = (y / (float)W + Mathf.Abs(u - 0.5f)) * 3.2f; px[y * W + x] = ((int)Mathf.Floor(k)) % 2 == 0 ? rd : yl; }
            chevTex = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear }; chevTex.SetPixels32(px); chevTex.Apply(true, true);
            return chevTex;
        }

        Light MakeLight(Vector3 p)
        {
            var g = new GameObject("Blue"); g.transform.SetParent(transform, false); g.transform.localPosition = p;
            var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(0.2f, 0.4f, 1f); l.range = 9f; l.intensity = 0f; l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForceVertex;
            return l;
        }

        static AudioClip sirenClip;
        static AudioClip SirenClip()
        {
            if (sirenClip != null) return sirenClip;
            int sr = 22050, n = sr * 2; var d = new float[n]; float ph = 0;
            for (int i = 0; i < n; i++)
            {
                float f = ((i / (sr * 0.55f)) % 2f) < 1f ? 660f : 880f;
                ph += 2f * Mathf.PI * f / sr; d[i] = Mathf.Sign(Mathf.Sin(ph)) * 0.25f + Mathf.Sin(ph) * 0.25f;
            }
            sirenClip = AudioClip.Create("siren", n, 1, sr, false); sirenClip.SetData(d, 0); return sirenClip;
        }

        void Update()
        {
            float moved = Vector3.Dot(transform.position - lastPos, transform.forward); lastPos = transform.position;
            wheelSpin += moved / Mathf.Max(0.2f, wheelR) * Mathf.Rad2Deg;
            if (wheels != null) foreach (var w in wheels) w.localRotation = Quaternion.Euler(wheelSpin, 0f, 90f);
            bool a = ((int)(Time.time * 5f)) % 2 == 0;
            blueA.intensity = a ? 40f : 0f; blueB.intensity = a ? 0f : 40f;
            foreach (var r in barA) r.sharedMaterial = a ? lightOn : lightOff;
            foreach (var r in barB) r.sharedMaterial = a ? lightOff : lightOn;
        }

        void LateUpdate()
        {
            // kit carried in a hand hangs from it
            foreach (var (kit, who, left) in carried)
            {
                if (kit == null || who == null || who.anim == null) continue;
                var hand = who.anim.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand); if (hand == null) continue;
                kit.position = hand.position - Vector3.up * 0.04f; kit.rotation = Quaternion.LookRotation(who.transform.right * (left ? -1f : 1f), Vector3.up);
            }
        }

        // ================================================================================================ the call-out
        Vector3 Rear(float extra = 1.1f) => transform.position - transform.forward * (Le + extra);
        Vector3 CabDoor(int side) => transform.position + transform.forward * (Le - 1.3f) + transform.right * side * (hw + 0.55f);
        Vector3 Corner(int side, bool back) => transform.position + transform.forward * (back ? -(Le + 0.7f) : Le - 1.3f) + transform.right * side * (hw + 0.7f);

        IEnumerator Run(Person victim)
        {
            Status = "Ambulance dispatched";
            yield return new WaitForSeconds(3f);
            Vector3 vp = victim.BodyPos; vp.y = 0;
            float laneZ = World.LaneZ; float parkX = Mathf.Clamp(vp.x - 7f, -150f, 150f);
            transform.position = new Vector3(parkX - 120f, 0, laneZ); transform.rotation = Quaternion.Euler(0, 90f, 0);
            yield return DriveTo(parkX, 17f, true);
            Status = "Ambulance on scene";
            siren.Stop(); // blues stay on, siren off at the scene

            // the crew get out of the cab, one each side
            var crew = new List<Person>();
            for (int i = 0; i < 2; i++)
            {
                var p = World.MakePerson(CabDoor(i == 0 ? -1 : 1), new Color(0.05f, 0.2f, 0.12f), new Color(0.04f, 0.15f, 0.1f), "Paramedic");
                p.isParamedic = true; crew.Add(p);
            }
            float t0 = Time.time; while (Time.time - t0 < 4f && (crew[0].anim == null || crew[1].anim == null)) yield return null;
            foreach (var c in crew) if (c.anim != null) { MedicKit.DressParamedic(c.anim); if (c.anim.GetComponent<MedicHands>() == null) c.anim.gameObject.AddComponent<MedicHands>(); }
            // round the back and open up
            yield return WalkPath(crew[0], new[] { Corner(-1, true), Rear() + transform.right * -0.5f }, 1.7f);
            StartCoroutine(WalkPath(crew[1], new[] { Corner(1, true), Rear() + transform.right * 0.5f }, 1.7f));
            yield return new WaitForSeconds(0.6f);
            yield return Doors(true);
            // take the kit: bag and defib for one, oxygen for the other
            var bag = MedicKit.Bag(true); var defib = MedicKit.Defib(); var o2 = MedicKit.Oxygen(); var bag2 = MedicKit.Bag(false);
            foreach (var k in new[] { bag, defib, o2, bag2 }) k.SetParent(World.Root, true);
            carried.Add((bag, crew[0], false)); carried.Add((defib, crew[0], true)); carried.Add((o2, crew[1], false)); carried.Add((bag2, crew[1], true));
            yield return new WaitForSeconds(0.5f);
            // to the casualty
            Vector3 toV = (vp - Rear()).WithY(0).normalized; Vector3 sideV = Vector3.Cross(Vector3.up, toV);
            Vector3 kneelA = vp - toV * 0.3f + sideV * 0.75f, kneelB = vp + toV * 0.2f - sideV * 0.75f;
            StartCoroutine(WalkPath(crew[1], new[] { kneelB }, 1.9f));
            yield return WalkPath(crew[0], new[] { kneelA }, 1.9f);
            Status = victim.dead ? "Paramedics confirm death" : "Paramedics treating casualty";
            // bags down, kneel, hands on
            carried.Clear();
            PutDown(bag, kneelA + toV * 0.55f + sideV * 0.25f); PutDown(defib, kneelA - toV * 0.5f + sideV * 0.25f); PutDown(o2, kneelB + toV * 0.5f - sideV * 0.25f); PutDown(bag2, kneelB - toV * 0.45f - sideV * 0.3f);
            foreach (var c in crew) { if (Busy(c)) continue; c.transform.rotation = Quaternion.LookRotation((vp - c.transform.position).WithY(0)); c.SetKneel(true); }
            var chest = victim.anim != null ? victim.anim.GetBoneTransform(HumanBodyBones.Chest) ?? victim.anim.GetBoneTransform(HumanBodyBones.Spine) : null;
            var head = victim.anim != null ? victim.anim.GetBoneTransform(HumanBodyBones.Head) : null;
            yield return new WaitForSeconds(0.8f);
            Hands(crew[0])?.Reach(chest, chest); Hands(crew[1])?.Reach(head, head);
            if (GameAudio.I != null) GameAudio.I.PlayVoice("medic", vp, 0.2f, 1f, 0);
            yield return new WaitForSeconds(2.5f);
            Transform mask = null;
            if (!victim.dead && head != null) { mask = MedicKit.Mask(); FaceMask(victim, head, mask); }
            if (GameAudio.I != null) { if (victim.dead) GameAudio.I.PlayVoice("medicdead", vp, 0f, 1f, 0); else GameAudio.I.PlayVoice("medic", vp, 0f, 1f, 1); }
            yield return new WaitForSeconds(3f);
            // one stays with them; the other fetches the stretcher
            Hands(crew[1])?.Release(); crew[1].SetKneel(false);
            yield return new WaitForSeconds(0.7f);
            yield return WalkPath(crew[1], new[] { Rear(1.6f) }, 2f);
            var st = MedicKit.Stretcher(out float bedTop, out var castors); st.SetParent(World.Root, true);
            st.position = transform.position - transform.forward * (Le - 0.6f) + Vector3.up * floorY; st.rotation = transform.rotation; // head end into the van (-z of the stretcher points forward... flip below)
            st.rotation = Quaternion.LookRotation(-transform.forward);
            // pull it out of the back
            Vector3 outPos = Rear(1.2f);
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.3f) { st.position = Vector3.Lerp(transform.position - transform.forward * (Le - 0.6f) + Vector3.up * floorY, outPos, Mathf.SmoothStep(0, 1, t)); crew[1].Step(st.position + st.forward * 1.25f, 1f, false); yield return null; }
            st.position = outPos;
            // wheel it to the casualty: it only rolls while the medic is at the handle
            Vector3 bedSpot = vp - sideV * 1.1f;
            yield return WheelPath(st, castors, new List<Person> { crew[1] }, bedSpot, Quaternion.LookRotation(toV));
            // both at the body: lift onto the stretcher
            crew[0].SetKneel(false); Hands(crew[0])?.Release();
            yield return WalkPath(crew[0], new[] { vp + toV * 0.9f }, 1.4f);
            yield return WalkPath(crew[1], new[] { vp - toV * 1.0f }, 1.4f);
            foreach (var c in crew) { c.transform.rotation = Quaternion.LookRotation((vp - c.transform.position).WithY(0)); c.SetKneel(true); }
            yield return new WaitForSeconds(0.9f);
            Vector3 hipsOn = new Vector3(0f, bedTop + 0.11f, 0.15f);
            victim.LieOn(st, hipsOn);
            // lift: the body rises from the ground onto the mattress
            Vector3 endLocal = victim.transform.localPosition, startW = victim.transform.position - Vector3.up * (bedTop + 0.05f) - sideV * 0.8f;
            Vector3 startLocal = st.InverseTransformPoint(startW);
            foreach (var c in crew) Hands(c)?.Reach(victim.anim != null ? victim.anim.GetBoneTransform(HumanBodyBones.Hips) : null, victim.anim != null ? victim.anim.GetBoneTransform(HumanBodyBones.Hips) : null);
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.6f) { victim.transform.localPosition = Vector3.Lerp(startLocal, endLocal, Mathf.SmoothStep(0, 1, t)); yield return null; }
            victim.transform.localPosition = endLocal;
            foreach (var c in crew) { Hands(c)?.Release(); c.SetKneel(false); }
            var blanket = MedicKit.Blanket(victim.dead); blanket.SetParent(st, false); blanket.localPosition = new Vector3(0, bedTop + 0.22f, victim.dead ? 0f : 0.3f);
            if (victim.dead && mask != null) Destroy(mask.gameObject);
            // kit on the end of the stretcher
            foreach (var k in new[] { bag, defib, o2, bag2 }) { k.SetParent(st, true); }
            bag.localPosition = new Vector3(0.12f, bedTop + 0.3f, 0.85f); defib.localPosition = new Vector3(-0.14f, bedTop + 0.24f, 0.8f); o2.localPosition = new Vector3(0f, bedTop + 0.45f, 0.6f); o2.localRotation = Quaternion.Euler(90f, 0, 0); bag2.localPosition = new Vector3(0f, bedTop + 0.3f, -0.85f);
            if (GameAudio.I != null) GameAudio.I.PlayVoice("medic", vp, 0.3f, 1f, 2);
            Status = "Casualty on the stretcher";
            yield return new WaitForSeconds(0.6f);
            // both push it back to the van, head end first
            yield return WheelPath(st, castors, crew, Rear(1.3f), Quaternion.LookRotation(transform.forward));
            // in it goes, then the doors shut
            Vector3 inPos = transform.position - transform.forward * (Le - 1.1f) + Vector3.up * floorY, from = st.position;
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.6f) { st.position = Vector3.Lerp(from, inPos, Mathf.SmoothStep(0, 1, t)); foreach (var c in crew) if (!Busy(c)) c.Step(st.position - st.forward * 1.2f + (c == crew[0] ? -transform.right : transform.right) * 0.35f, 0.8f, false); yield return null; }
            st.gameObject.SetActive(false); victim.gameObject.SetActive(false);
            yield return WalkAll(crew, new[] { Rear(1.4f) + transform.right * -0.6f, Rear(1.4f) + transform.right * 0.6f }, 1.4f);
            yield return Doors(false);
            Status = "En route to Wend Valley Hospital";
            // into the cab, round the sides
            StartCoroutine(WalkPath(crew[1], new[] { Corner(1, true), CabDoor(1) }, 1.8f));
            yield return WalkPath(crew[0], new[] { Corner(-1, true), CabDoor(-1) }, 1.8f);
            yield return new WaitForSeconds(0.6f);
            foreach (var c in crew) if (c != null && !Busy(c)) c.gameObject.SetActive(false); // anyone still scrapping gets left behind
            siren.Play();
            obstacle.enabled = false;
            yield return DriveTo(parkX + 260f, 22f, false);
            Destroy(st.gameObject); Destroy(victim.gameObject);
            foreach (var c in crew) if (c != null && !c.gameObject.activeSelf) Destroy(c.gameObject);
            Destroy(gameObject);
        }

        // what the van won't drive through: buildings, walls, cars and people (not bodies on the floor, not triggers)
        static readonly int AheadMask = ~((1 << Person.RagdollLayer) | (1 << 2) | (1 << 31));

        /// <summary>
        /// Drive along the lane to x: brakes for anything in front (a car, someone in the road, a wall), waits, and if
        /// it stays blocked pulls out into the other lane, gets past and pulls back in. Stops at x if told to.
        /// </summary>
        IEnumerator DriveTo(float toX, float top, bool stopAtEnd)
        {
            float v = 0f, blocked = 0f, off = 0f, wantOff = 0f, passFrom = 0f, baseZ = World.LaneZ;
            while (toX - transform.position.x > 0.3f)
            {
                float dt = Time.deltaTime, dist = toX - transform.position.x;
                float want = stopAtEnd ? Mathf.Clamp(dist * 0.8f, 0f, top) : top;
                float look = Le + 2.5f + v * 1.2f;
                Vector3 o = transform.position + transform.forward * (Le + 0.2f) + Vector3.up * 1.15f;
                bool hitSomething = false;
                foreach (var h in Physics.BoxCastAll(o, new Vector3(hw * 0.95f, 0.7f, 0.1f), transform.forward, transform.rotation, look, AheadMask, QueryTriggerInteraction.Ignore))
                {
                    if (h.transform.IsChildOf(transform) || h.distance <= 0f) continue;
                    hitSomething = true; want = Mathf.Min(want, Mathf.Max(0f, (h.distance - 1.6f) * 1.4f));
                }
                if (hitSomething && v < 1.2f) blocked += dt; else blocked = Mathf.Max(0f, blocked - dt);
                if (blocked > 1.3f && wantOff == 0f) { wantOff = -4.2f; passFrom = transform.position.x; blocked = 0f; } // pull out round it
                if (wantOff != 0f && transform.position.x - passFrom > Le * 2f + 14f && LaneClear(baseZ)) wantOff = 0f; // past it and the lane's clear: back in
                if (!stopAtEnd || dist > 2f) want = Mathf.Max(want, hitSomething ? 0f : 1f);
                v = Mathf.MoveTowards(v, want, (want < v ? 9f : 6f) * dt);
                float newOff = Mathf.MoveTowards(off, wantOff, 2.2f * dt * Mathf.Clamp01(v / 3f + 0.2f));
                Vector3 step = new Vector3(v * dt, 0f, newOff - off); off = newOff;
                transform.position += step;
                if (step.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.Euler(0f, 90f - Mathf.Atan2(step.z, step.x) * Mathf.Rad2Deg, 0f);
                yield return null;
            }
        }

        /// <summary>Nothing (but us) in our own lane alongside and just ahead.</summary>
        bool LaneClear(float laneZ)
        {
            foreach (var c in Physics.OverlapBox(new Vector3(transform.position.x + 2f, 1.15f, laneZ), new Vector3(Le + 3f, 0.7f, hw), Quaternion.identity, AheadMask, QueryTriggerInteraction.Ignore))
                if (!c.transform.IsChildOf(transform)) return false;
            return true;
        }

        static readonly NavMeshPath navPath = new NavMeshPath();
        /// <summary>The way on foot from one spot to another round buildings, walls and cars (the walking mesh), ending exactly at to.</summary>
        static List<Vector3> NavCorners(Vector3 from, Vector3 to)
        {
            var list = new List<Vector3>();
            if (NavMesh.SamplePosition(from, out var a, 2.5f, NavMesh.AllAreas) && NavMesh.SamplePosition(to, out var b, 2.5f, NavMesh.AllAreas)
                && NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, navPath) && navPath.status != NavMeshPathStatus.PathInvalid)
                for (int i = 1; i < navPath.corners.Length; i++) list.Add(navPath.corners[i]);
            if (list.Count > 0 && (list[list.Count - 1] - to).WithY(0).magnitude < 0.8f) list[list.Count - 1] = to; else list.Add(to);
            return list;
        }

        /// <summary>Push the stretcher along the walking route, a leg at a time.</summary>
        IEnumerator WheelPath(Transform st, Transform[] castors, List<Person> pushers, Vector3 to, Quaternion endRot)
        {
            var pts = NavCorners(st.position, to);
            for (int i = 0; i < pts.Count; i++)
            {
                bool last = i == pts.Count - 1;
                Vector3 next = last ? to : pts[i];
                Quaternion face = last ? endRot : Quaternion.LookRotation((next - st.position).WithY(0).sqrMagnitude > 0.01f ? (next - st.position).WithY(0) : st.forward);
                yield return Wheel(st, castors, pushers, next.WithY(st.position.y), face);
            }
        }

        static MedicHands Hands(Person p) => p != null && p.anim != null ? p.anim.GetComponent<MedicHands>() : null;

        static void PutDown(Transform kit, Vector3 at)
        {
            kit.position = at + Vector3.up * (kit.name == "Oxygen" ? 0.36f : kit.name == "Defib" ? 0.27f : 0.35f);
            kit.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        }

        /// <summary>Over the nose and mouth: in front of the head, on the face side (found from the eyes).</summary>
        static void FaceMask(Person v, Transform head, Transform mask)
        {
            var le = v.anim.GetBoneTransform(HumanBodyBones.LeftEye); var re = v.anim.GetBoneTransform(HumanBodyBones.RightEye);
            Vector3 face = le != null && re != null ? ((le.position + re.position) * 0.5f - head.position) : head.up * 0.05f;
            Vector3 dir = face.sqrMagnitude > 1e-6f ? face.normalized : head.forward;
            mask.SetParent(head, true);
            mask.position = head.position + dir * 0.11f - (le != null ? (le.position - head.position).normalized * 0f : Vector3.zero);
            mask.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        IEnumerator Doors(bool open)
        {
            for (float t = 0; t <= 1f; t += Time.deltaTime / 0.9f)
            {
                SetDoors(Mathf.SmoothStep(0, 1, open ? t : 1f - t));
                yield return null;
            }
            SetDoors(open ? 1f : 0f);
        }

        /// <summary>Both back doors swung out by k (0 shut, 1 open right round against the sides).</summary>
        void SetDoors(float k)
        {
            const float Open = 160f; // barn doors fold back along the body
            doorL.localRotation = Quaternion.AngleAxis(doorSignL * Open * k, Vector3.up) * doorBaseL;
            doorR.localRotation = Quaternion.AngleAxis(doorSignR * Open * k, Vector3.up) * doorBaseR;
        }

        /// <summary>
        /// Push the stretcher to a spot: the medics walk at the handles (one at each end with two), and it only moves
        /// when they're there to push it. Castors roll, hands on the handles.
        /// </summary>
        IEnumerator Wheel(Transform st, Transform[] castors, List<Person> pushers, Vector3 to, Quaternion endRot)
        {
            float guard = 0f; const float L = 1.95f;
            while (guard < 30f)
            {
                guard += Time.deltaTime;
                Vector3 d = (to - st.position).WithY(0); float dist = d.magnitude;
                if (dist < 0.08f) break;
                Quaternion face = dist > 0.6f ? Quaternion.LookRotation(d.normalized) : endRot;
                // handle positions: behind (the end you push from), and in front with a second medic
                Vector3 back = st.position - st.forward * (L * 0.5f + 0.42f), front = st.position + st.forward * (L * 0.5f + 0.42f);
                bool ready = true;
                for (int i = 0; i < pushers.Count; i++)
                {
                    var p = pushers[i]; if (Busy(p)) continue;
                    Vector3 spot = i == 0 ? back : front;
                    if ((spot - p.transform.position).WithY(0).magnitude > 0.35f) { p.Step(spot, 1.8f, false); ready = false; }
                    else
                    {
                        p.transform.rotation = Quaternion.RotateTowards(p.transform.rotation, Quaternion.LookRotation((i == 0 ? st.forward : -st.forward)), 360f * Time.deltaTime);
                        var h = Hands(p); if (h != null) h.ReachTo(st.position + st.forward * (i == 0 ? -1f : 1f) * (L * 0.5f + 0.09f) - st.right * 0.2f + Vector3.up * 0.65f, st.position + st.forward * (i == 0 ? -1f : 1f) * (L * 0.5f + 0.09f) + st.right * 0.2f + Vector3.up * 0.65f);
                    }
                }
                if (ready)
                {
                    float step = Mathf.Min(1.1f * Time.deltaTime, dist);
                    st.position += d.normalized * step;
                    st.rotation = Quaternion.RotateTowards(st.rotation, face, 90f * Time.deltaTime);
                    foreach (var c in castors) if (c != null) c.Rotate(Vector3.up, step / 0.06f * Mathf.Rad2Deg, Space.Self);
                    foreach (var p in pushers) if (!Busy(p)) p.Animate(1.1f);
                }
                yield return null;
            }
            st.rotation = endRot;
            foreach (var p in pushers) Hands(p)?.Release();
        }

        static bool Busy(Person c) { if (c == null || c.state != Person.State.Walking) return true; var f = c.GetComponent<Fighter>(); return f != null && f.Fighting; }

        /// <summary>Walk through the points, each leg by the walking route (round houses and walls, not through them).</summary>
        IEnumerator WalkPath(Person p, Vector3[] pts, float spd)
        {
            foreach (var pt in pts)
            {
                var legs = NavCorners(p.transform.position, pt);
                for (int i = 0; i < legs.Count; i++)
                {
                    Vector3 c = legs[i]; bool last = i == legs.Count - 1; float guard = 0f;
                    while (guard < 15f && !Busy(p) && (c - p.transform.position).WithY(0).magnitude > (last ? 0.3f : 0.5f)) { guard += Time.deltaTime; p.Step(c, spd); yield return null; }
                }
            }
        }

        IEnumerator WalkAll(List<Person> crew, Vector3[] targets, float spd)
        {
            float guard = 0;
            while (guard < 25f)
            {
                guard += Time.deltaTime; bool all = true;
                for (int i = 0; i < crew.Count; i++)
                {
                    if (Busy(crew[i])) continue;
                    if ((targets[i] - crew[i].transform.position).WithY(0).magnitude > 0.35f) { crew[i].Step(targets[i], spd); all = false; }
                }
                if (all) break;
                yield return null;
            }
        }
    }
}
