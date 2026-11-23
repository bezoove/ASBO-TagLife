using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ASBO.Slice
{
    /// <summary>Remote test hooks (called by the editor job runner): screenshots and teleports.</summary>
    public static class DebugCmd
    {
        static int shotN;
        public static void Shot(string name)
        {
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots"); System.IO.Directory.CreateDirectory(dir);
            ScreenCapture.CaptureScreenshot(dir + "/" + (string.IsNullOrEmpty(name) ? "j" + (shotN++) : name) + ".png");
        }
        /// <summary>Capture n frames, interval seconds apart, as burst_N.png (for making a movement clip).</summary>
        public static void Burst(int n, float interval)
        {
            var go = new GameObject("BurstRunner"); go.AddComponent<BurstRunner>().Run(n, interval);
        }
        /// <summary>Scripted movement test: drives the player with fake input for a few seconds while capturing frames.</summary>
        public static void Drive(string pattern)
        {
            var pc = Object.FindFirstObjectByType<PlayerCtl>(); if (pc != null) pc.scripted = pattern;
        }
        /// <summary>Showcase a gait on the spot: side view, then the camera swings round to the front. Frames at a locked 30 fps.</summary>
        public static void ShowGait(float speed, int frames)
        {
            var pc = Object.FindFirstObjectByType<PlayerCtl>(); if (pc == null) return;
            var go = new GameObject("GaitShow"); go.AddComponent<GaitShow>().Run(pc, speed, frames);
        }
        /// <summary>Gait from a fixed camera angle (0 = directly behind, -90 = left side, 180 = front), optional swing.</summary>
        public static void DumpMats()
        {
            var pc = Object.FindFirstObjectByType<PlayerCtl>(); if (pc == null) return;
            foreach (var r in pc.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    var sb = new System.Text.StringBuilder("[ASBO] mat " + r.name + " / " + m.name + " shader=" + m.shader.name + " :");
                    for (int i = 0; i < m.shader.GetPropertyCount(); i++)
                    {
                        var n = m.shader.GetPropertyName(i); var t = m.shader.GetPropertyType(i);
                        sb.Append(" " + n);
                        if (t == UnityEngine.Rendering.ShaderPropertyType.Color) sb.Append("=" + m.GetColor(n));
                        if (t == UnityEngine.Rendering.ShaderPropertyType.Texture) { var tx = m.GetTexture(n); sb.Append("=" + (tx != null ? tx.name : "null")); }
                    }
                    Debug.Log(sb.ToString());
                }
        }
        /// <summary>Drive the RCC car into a resident at the given speed (m/s) and film it from the side: tests UMA ragdolls with real car physics.</summary>
        public static void CrashTest(float speed, int frames)
        {
            var rv = Object.FindFirstObjectByType<RccVehicle>(); if (rv == null) { Debug.LogWarning("[ASBO] no RCC car"); return; }
            Resident best = null; float bd = 1e9f;
            foreach (var r in Object.FindObjectsByType<Resident>(FindObjectsSortMode.None))
            {
                var pp = r.GetComponent<Person>(); if (pp == null || pp.state != Person.State.Walking) continue;
                float d = (r.transform.position - rv.transform.position).sqrMagnitude; if (d < bd) { bd = d; best = r; }
            }
            if (best == null) { Debug.LogWarning("[ASBO] no resident to test"); return; }
            Vector3 want = rv.transform.position + rv.transform.forward * 18f;
            if (Nav.Snap(want, out var q, 6f)) want = q;
            best.HoldAt(want, rv.transform.eulerAngles.y + 90f, 6f);
            var go = new GameObject("CrashShow"); go.AddComponent<CrashShow>().Run(rv, best.GetComponent<Person>(), speed, frames);
        }
        /// <summary>Put the player 4 m from the last casualty, looking at it (to film paramedics etc.).</summary>
        public static void ViewCasualty(float dist, float side)
        {
            var pc = Object.FindFirstObjectByType<PlayerCtl>(); var v = Person.LastHit; if (pc == null || v == null) return;
            Vector3 c = v.BodyPos; c.y = 0f;
            Vector3 from = c + new Vector3(Mathf.Sin(side * Mathf.Deg2Rad), 0f, Mathf.Cos(side * Mathf.Deg2Rad)) * dist;
            if (Nav.Snap(from, out var q, 3f)) from = q;
            Vector3 d = c - from; float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            pc.Teleport(from, yaw, 18f);
        }
        /// <summary>Park one of each traffic body on the road and photograph them from a few angles (Shots/carshow_*).</summary>
        /// <summary>Build one car on the road and log where its wheels and body sit against the ground.</summary>
        public static void CarProbe(string id)
        {
            var g = new GameObject("Probe_" + id).transform;
            Vector3 p = new Vector3(-30f, 5f, World.RoadZ + 1.9f);
            if (Physics.Raycast(p, Vector3.down, out var hit, 20f, ~(1 << 2), QueryTriggerInteraction.Ignore)) p.y = hit.point.y; else p.y = 0f;
            g.position = p;
            var w = CarBody.Build(g, CarBody.Get(id), Color.red, true, 5);
            var sb = new System.Text.StringBuilder();
            foreach (var r in g.GetComponentsInChildren<Renderer>()) sb.Append($"{r.name}:{r.bounds.min.y - p.y:0.00} ");
            Debug.Log($"[ASBO] probe {id} ground {p.y:0.00} hit {(hit.collider != null ? hit.collider.name : "-")} root {g.GetChild(0).localPosition} wheel0 {(w.Length > 0 ? w[0].position.y - p.y : -1):0.00} | {sb}");
        }
        public static void CarShow() { new GameObject("CarShow").AddComponent<CarShowRun>(); }
        /// <summary>Photograph a named object from an angle (degrees round it, 0 = in front), distance and height: Shots/of_NAME_ANGLE.png</summary>
        public static void ShotOf(string name, float angle, float dist, float height)
        {
            var go = GameObject.Find(name); if (go == null) { Debug.LogWarning("[ASBO] ShotOf: no " + name); return; }
            new GameObject("ShotOf").AddComponent<ShotOfRun>().Go(go.transform, angle, dist, height);
        }
        /// <summary>Line up ten generic residents on the road and photograph them (Shots/people_*.png).</summary>
        public static void PeopleShow() { new GameObject("PeopleShow").AddComponent<PeopleShowRun>(); }
        public static void EditControls() { if (TouchControls.I != null) TouchControls.I.ToggleEdit(); }
        public static void DumpType(string name)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(name); if (t == null) continue;
                var sb = new System.Text.StringBuilder("[ASBO] type " + t.FullName + " : " + t.BaseType + "\n");
                const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly;
                foreach (var f in t.GetFields(F)) sb.Append("  field ").Append(f.FieldType.Name).Append(' ').Append(f.Name).Append('\n');
                foreach (var pr in t.GetProperties(F)) sb.Append("  prop ").Append(pr.PropertyType.Name).Append(' ').Append(pr.Name).Append(pr.CanWrite ? " rw" : " r").Append('\n');
                foreach (var m in t.GetMethods(F)) if (!m.IsSpecialName) sb.Append("  method ").Append(m.ReturnType.Name).Append(' ').Append(m.Name).Append('(').Append(string.Join(", ", m.GetParameters().Select(q => q.ParameterType.Name + " " + q.Name))).Append(")\n");
                System.IO.File.WriteAllText(Application.dataPath + "/../Temp/type_" + name + ".txt", sb.ToString());
                Debug.Log("[ASBO] dumped " + name);
            }
        }
        public static void DrunkShow() { new GameObject("DrunkShow").AddComponent<DrunkShowRun>(); }
        public static void FightFilm(int clip, float step, float secs) { var r = new GameObject("FightFilm").AddComponent<FightFilmRun>(); r.clip = clip; r.step = step; r.secs = secs; }
        public static void FightFilmFrom(int clip, float step, float secs, float from) { var r = new GameObject("FightFilm").AddComponent<FightFilmRun>(); r.clip = clip; r.step = step; r.secs = secs; r.from = from; }
        public static void BrawlTest() { new GameObject("BrawlTest").AddComponent<BrawlTestRun>(); }
        /// <summary>The scripted scrap, filmed only at the moment each blow lands (Shots/brawl/hit_##.png): do fists meet faces?</summary>
        public static void CasinoShots() { new GameObject("CasinoShots").AddComponent<CasinoShotsRun>(); }
        /// <summary>Films the moves from the side: a standing jump, getting down into a crouch, sneaking along, standing up, a run pulled up, then the casino's seated punters. Shots/moves/m_N.png.</summary>
        public static void MoveTest() { new GameObject("MoveTest").AddComponent<MoveTestRun>(); }
        /// <summary>Films a carjack (a stopped car, the driver dragged out, away you go) and then a stamp on the driver lying in the road. Shots/moves/j_N.png.</summary>
        public static void JackTest() { new GameObject("JackTest").AddComponent<JackTestRun>(); }
        /// <summary>Climb, run-up wall climb, vault, drop-and-roll, barging into someone, reaching down: on a test slab up in the sky.</summary>
        public static void TraverseTest(int only) { TraverseTestRun.Only = only; new GameObject("TraverseTest").AddComponent<TraverseTestRun>(); }
        public static void MobTest() { new GameObject("MobTest").AddComponent<MobTestRun>(); }
        public static void BrawlHits(float secs) { var r = new GameObject("BrawlTest").AddComponent<BrawlTestRun>(); r.onHit = true; r.secs = secs; r.kickFinish = true; }
        public static void TalkTest() { new GameObject("TalkTest").AddComponent<TalkTestRun>(); }
        public static void SetHour(float h) { GameClock.Set(h); Debug.Log("[ASBO] clock " + GameClock.Text); }
        public static void ClockSpeed(float s) { GameClock.Speed = s; }
        public static void CountOut() { int n = 0; var sb = new System.Text.StringBuilder(); foreach (var r in Object.FindObjectsByType<Resident>(FindObjectsSortMode.None)) { n++; sb.Append(r.Doing + " "); } Debug.Log($"[ASBO] out {GameClock.Text}: {n} ({sb})"); }
        public static void NightTest() { new GameObject("NightTest").AddComponent<NightTestRun>(); }
        /// <summary>Why legs look bent: root height over the ground, hip drop from the foot IK, knee bend, for the player and a few walkers.</summary>
        public static void Legs()
        {
            var sb = new System.Text.StringBuilder("[ASBO] legs:");
            int n = 0;
            foreach (var p in Person.All)
            {
                if (p == null || p.anim == null || !p.anim.isHuman || !p.gameObject.activeInHierarchy) continue;
                if (!p.isPlayer && n >= 4) continue; if (!p.isPlayer) n++;
                var a = p.anim; var fg = a.GetComponent<FootGround>();
                float ground = Physics.Raycast(p.transform.position + Vector3.up * 0.6f, Vector3.down, out var gh, 2f, ~((1 << 2) | (1 << 31) | (1 << 28) | (1 << 29)), QueryTriggerInteraction.Ignore) ? gh.point.y : float.NaN;
                float Knee(HumanBodyBones up, HumanBodyBones lo, HumanBodyBones ft) { var u = a.GetBoneTransform(up); var l = a.GetBoneTransform(lo); var f = a.GetBoneTransform(ft); return u && l && f ? Vector3.Angle(l.position - u.position, f.position - l.position) : -1f; }
                var hips = a.GetBoneTransform(HumanBodyBones.Hips); var lf = a.GetBoneTransform(HumanBodyBones.LeftFoot);
                var ag = p.GetComponent<UnityEngine.AI.NavMeshAgent>();
                sb.Append($" | {(p.isPlayer ? "PLAYER" : p.name)} rootOverGround={(p.transform.position.y - ground):F3} animRootOver={(a.transform.position.y - ground):F3} hipsOver={(hips.position.y - ground):F2} footOver={(lf.position.y - ground):F2} kneeL={Knee(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot):F0} kneeR={Knee(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot):F0} pelvisIK={(fg != null ? fg.Pelvis : 0f):F3} lOff={(fg != null ? fg.LeftOff : 0f):F3} scale={a.humanScale:F2} speed={(ag != null ? ag.velocity.magnitude : 0f):F2}");
            }
            Debug.Log(sb.ToString());
        }
        public static void KnockTest() { new GameObject("KnockTest").AddComponent<KnockTestRun>(); }
        public static void CarTest() { new GameObject("CarTest").AddComponent<CarTestRun>(); }
        /// <summary>Every kind of car on the main road: flat out, brake, handbrake turn. Logs pace and grip per car; a side shot of each.</summary>
        public static void CarTypes(string only) { var r = new GameObject("CarTypes").AddComponent<CarTypesRun>(); r.only = only; }
        public static void CarTrace(string only) { var r = new GameObject("CarTypes").AddComponent<CarTypesRun>(); r.only = only; r.verbose = true; }
        /// <summary>A straight-down picture of the map around (x, z), size metres across, to Shots/overhead/name.png.</summary>
        public static void Overhead(float x, float z, float size, string name)
        {
            var cg = new GameObject("OverCam"); var cam = cg.AddComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = size * 0.5f; cam.aspect = 1f;
            cam.transform.position = new Vector3(x, 150f, z); cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f); cam.farClipPlane = 400f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            var rt = new RenderTexture(1600, 1600, 24); cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(1600, 1600, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 1600), 0, 0); tex.Apply(); RenderTexture.active = null;
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/overhead"); System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllBytes(dir + "/" + name + ".png", tex.EncodeToPNG());
            cam.targetTexture = null; Object.Destroy(rt); Object.Destroy(cg); Debug.Log($"[ASBO] overhead {name} at ({x},{z}) {size} m");
        }
        /// <summary>A picture from (x, y, z) looking at (lx, ly, lz), 1600x900, to Shots/views/name.png.</summary>
        public static void ShotAt(float x, float y, float z, float lx, float ly, float lz, string name)
        {
            var cg = new GameObject("ViewCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 55f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 600f;
            var main = Camera.main; if (main != null) { var src = main.GetComponent<UniversalAdditionalCameraData>(); var dst = cam.GetUniversalAdditionalCameraData(); if (src != null) { dst.renderPostProcessing = src.renderPostProcessing; dst.antialiasing = src.antialiasing; } }
            cam.transform.position = new Vector3(x, y, z); cam.transform.LookAt(new Vector3(lx, ly, lz));
            var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply(); RenderTexture.active = null;
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/views"); System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllBytes(dir + "/" + name + ".png", tex.EncodeToPNG());
            cam.targetTexture = null; Object.Destroy(rt); Object.Destroy(cg); Debug.Log("[ASBO] view " + name);
        }
        /// <summary>Crash test for the dents: a car driven flat out into a parked one on the main road, filmed before and after.</summary>
        public static void DentTest(string id) { var r = new GameObject("DentTest").AddComponent<DentTestRun>(); r.id = string.IsNullOrEmpty(id) ? "hatch90" : id; }
        /// <summary>A parked-up look at one kind of car: side, three-quarter front, and with the driver's door open.</summary>
        public static void CarLook(string id) { var r = new GameObject("CarLook").AddComponent<CarLookRun>(); r.id = id; }
        /// <summary>You in the driver's seat (side, front and from behind), then a passing car's driver up close.</summary>
        public static void SeatTest(string id) { var r = new GameObject("SeatTest").AddComponent<SeatTestRun>(); r.id = string.IsNullOrEmpty(id) ? "hatch90" : id; }
        /// <summary>
        /// Build a town part live (without the long editor rebuild) and photograph it from set viewpoints: which = estates.
        /// Shots/views/prev_N.png. The ground's already levelled for every TownPlan area.
        /// </summary>
        public static void Preview(string which) { var r = new GameObject("Preview").AddComponent<PreviewRun>(); r.which = string.IsNullOrEmpty(which) ? "estates" : which; }
        /// <summary>The ambulance model parked up: side, front three-quarter, and looking in through its open back doors.</summary>
        public static void AmbLook() { new GameObject("AmbLook").AddComponent<AmbLookRun>(); }
        /// <summary>Climb test: in front of a precinct planter (a vault) and the Kestrel lock-ups' yard wall... press jump, see where you end up.</summary>
        public static void ClimbTest() { new GameObject("ClimbTest").AddComponent<ClimbTestRun>(); }
        /// <summary>Gangs: a look at the Kestrel Crew at their cage, a rider going by, then hang about till they come for you; then a go on the wow.</summary>
        public static void GangTest() { new GameObject("GangTest").AddComponent<GangTestRun>(); }
        public static void RigInfo() { Debug.Log($"[ASBO] rig ready={CarRig.Ready} styles=[{CarRig.Styles()}] fixedDt={Time.fixedDeltaTime} cars={PlayerCar.All.Count} parked={ParkedCar.All.Count}"); }
        public static void HardHit() { var v = Object.FindObjectsByType<Resident>(FindObjectsSortMode.None).Select(r => r.GetComponent<Person>()).FirstOrDefault(p => p != null && p.Ready && p.state == Person.State.Walking); var pc = Object.FindFirstObjectByType<PlayerCtl>(); if (v != null && pc != null) { v.GetComponent<Resident>().HoldAt(pc.transform.position + pc.transform.forward * 3f, 0f, 30f); v.Hit(pc.transform.forward * 9f, v.transform.position + Vector3.up, 9f); } }
        public static void AmbTest() { new GameObject("AmbTest").AddComponent<AmbTestRun>(); }
        /// <summary>Photograph the council estate from a few set viewpoints: Shots/estate_N.png.</summary>
        public static void EstateShots() { new GameObject("EstateShots").AddComponent<EstateShotsRun>(); }
        /// <summary>Photograph one set of places: 0 the estate, 1 the building site, 2 the castle (Shots/place_N.png).</summary>
        public static void PlaceShots(int set) { var r = new GameObject("PlaceShots").AddComponent<EstateShotsRun>(); r.set = set; }
        /// <summary>A phone finger on the left stick (a pretend touchscreen, so the touch ids are a phone's), with a second finger looking about.</summary>
        public static void StickTest() { new GameObject("StickTest").AddComponent<StickTestRun>(); }
        /// <summary>Drive flat out down the long road with the real camera: how far back it sits and how steady the car is on screen.</summary>
        public static void CamTest(string id) { var r = new GameObject("CamTest").AddComponent<CamTestRun>(); r.id = string.IsNullOrEmpty(id) ? "hatch90" : id; }
        /// <summary>Where the frame time goes: on foot, sat in a car, then driving (Unity's own timers, a few seconds each).</summary>
        public static void PerfRun(string id) { var r = new GameObject("PerfRun").AddComponent<PerfRunner>(); r.id = string.IsNullOrEmpty(id) ? "hatch90" : id; }
        public static void TouchDrive() { new GameObject("TouchDriveTest").AddComponent<TouchDriveTest>().Run(); }
        public static void EditSwitch() { if (TouchControls.I != null) TouchControls.I.SwitchEditSet(); }
        public static void EditPreset() { if (TouchControls.I != null) TouchControls.I.NextPreset(); }
        public static void Touch(int on) { PlayerPrefs.SetInt("asbo_touch", on); PlayerPrefs.Save(); }
        public static void SA(int on) { PlayerPrefs.SetInt("asbo_sa", on); PlayerPrefs.Save(); Debug.Log("[ASBO] starter-assets walk " + on); }
        public static void Stock(int on) { PlayerPrefs.SetInt("asbo_stock", on); PlayerPrefs.Save(); Debug.Log("[ASBO] stock bodies " + on); }
        public static void ShowGaitFrom(float speed, int frames, float camOffset, float swing, float radius)
        {
            var pc = Object.FindFirstObjectByType<PlayerCtl>(); if (pc == null) return;
            var go = new GameObject("GaitShow"); var g = go.AddComponent<GaitShow>(); g.offset = camOffset; g.swing = swing; if (radius > 0f) g.radius = radius; g.superSize = 2; g.Run(pc, speed, frames);
        }
        public static void Info()
        {
            Debug.Log($"[ASBO] info: baked={Object.FindFirstObjectByType<BakedWorld>() != null} free={FreePackTest.On} houses={Estate.Houses.Count} people={Person.All.Count} traffic={TrafficCar.All.Count} cottageRefs={Object.FindObjectsByType<CottageRef>(FindObjectsSortMode.None).Length} rcc={Object.FindFirstObjectByType<RccVehicle>() != null}");
        }
        public static void Go(float x, float z, float yaw, float pitch)
        {
            var pc = Object.FindFirstObjectByType<PlayerCtl>(); if (pc == null) return;
            pc.Teleport(new Vector3(x, 0.15f, z), yaw, pitch);
        }
        public static void Visit(int i, bool inside)
        {
            var pc = Object.FindFirstObjectByType<PlayerCtl>(); if (pc == null || i >= Estate.Houses.Count) return;
            var h = Estate.Houses[i];
            Vector3 door = h.root.TransformPoint(h.FrontDoorLocal), outw = h.root.TransformDirection(h.FrontOutLocal);
            Vector3 p = inside ? h.root.TransformPoint(h.MainRoomLocal()) : door + outw * 4.5f;
            pc.Teleport(p + Vector3.up * 0.1f, Quaternion.LookRotation(-outw).eulerAngles.y, inside ? 8f : 12f);
        }
    }

    public class GaitShow : MonoBehaviour
    {
        public float offset = -90f, swing = 125f, radius = 3.2f; public int superSize = 1;
        public void Run(PlayerCtl pc, float speed, int frames) { StartCoroutine(Go(pc, speed, frames)); }
        System.Collections.IEnumerator Go(PlayerCtl pc, float speed, int frames)
        {
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/show"); System.IO.Directory.CreateDirectory(dir);
            foreach (var f in System.IO.Directory.GetFiles(dir, "*.png")) { try { System.IO.File.Delete(f); } catch { } }
            World.HideHud = true;
            pc.showSpeed = speed; pc.showRadius = radius; pc.showPitch = 4f;
            float baseYaw = pc.transform.eulerAngles.y + offset;
            pc.showYaw = baseYaw;
            for (int i = 0; i < 45; i++) yield return null; // settle into the cycle
            Time.captureFramerate = 30;
            for (int i = 0; i < frames; i++)
            {
                float t = Mathf.Clamp01((i - frames * 0.45f) / (frames * 0.3f));
                pc.showYaw = baseYaw + Mathf.SmoothStep(0f, 1f, t) * swing;
                ScreenCapture.CaptureScreenshot(dir + "/f_" + i.ToString("000") + ".png", superSize);
                yield return null;
            }
            Time.captureFramerate = 0;
            pc.showSpeed = -1f; pc.showYaw = float.NaN; pc.showRadius = -1f; pc.showPitch = float.NaN;
            World.HideHud = false;
            Destroy(gameObject);
        }
    }

    /// <summary>Presses the real on-screen buttons (same events a finger sends) to check driving works with touch.</summary>
    public class TouchDriveTest : MonoBehaviour
    {
        static GameObject Face(string id) { var tc = TouchControls.I; if (tc == null) return null; var t = tc.transform.Find(id); return t != null ? (t.Find("Face") != null ? t.Find("Face").gameObject : t.gameObject) : null; }
        static void Press(string id, bool down)
        {
            var go = Face(id); if (go == null) { Debug.Log("[ASBO] touchtest: no control " + id); return; }
            var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = go.transform.position };
            if (down) UnityEngine.EventSystems.ExecuteEvents.Execute(go, ev, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
            else UnityEngine.EventSystems.ExecuteEvents.Execute(go, ev, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
        }
        public void Run() { StartCoroutine(Go()); }
        static object RccSettings()
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { var t = asm.GetType("RCCP_Settings"); if (t == null) continue; var p = t.GetProperty("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public); if (p != null) return p.GetValue(null); }
            return null;
        }
        static string M(object o, string n)
        {
            if (o == null) return "-"; const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var t = o.GetType(); var f = t.GetField(n, F); if (f != null) return System.Convert.ToString(f.GetValue(o), System.Globalization.CultureInfo.InvariantCulture);
            var p = t.GetProperty(n, F); if (p != null) return System.Convert.ToString(p.GetValue(o), System.Globalization.CultureInfo.InvariantCulture);
            return "?";
        }
        System.Collections.IEnumerator Go()
        {
            var pc = FindFirstObjectByType<PlayerCtl>(); var rcc = FindFirstObjectByType<RccVehicle>();
            if (pc == null || rcc == null) { Debug.Log("[ASBO] touchtest: no player/rcc"); yield break; }
            pc.transform.position = rcc.transform.position - rcc.transform.right * 2.2f;
            yield return new WaitForSeconds(0.3f);
            Press("car", true); yield return null; yield return null; Press("car", false);
            yield return new WaitForSeconds(0.5f);
            var pad = UnityEngine.InputSystem.Gamepad.current;
            Debug.Log($"[ASBO] touchtest: inCar={pc.InCar} pad={(pad != null ? pad.name : "none")} pads={UnityEngine.InputSystem.Gamepad.all.Count}");
            var cc = rcc.Car;
            if (cc != null) Debug.Log($"[ASBO] touchtest: canControl={M(cc, "canControl")} engineRunning={M(cc, "engineRunning")} externalControl={M(cc, "externalControl")} mobile={M(RccSettings(), "mobileControllerEnabled")}");
            Press("gas", true);
            for (int i = 0; i < 6; i++)
            {
                yield return new WaitForSeconds(0.5f);
                pad = UnityEngine.InputSystem.Gamepad.current;
                Debug.Log($"[ASBO] touchtest t={(i + 1) * 0.5f:F1}s speed={rcc.Speed * 3.6f:F1} km/h trigger={(pad != null ? pad.rightTrigger.ReadValue() : -1f):F2} throttleIn={M(cc, "throttleInput_V")} rpm={M(cc, "engineRPM")}");
            }
            // steer: drag the stick knob to the right like a thumb would
            var knob = TouchControls.I != null ? TouchControls.I.transform.Find("stick/Knob") : null;
            if (knob != null)
            {
                var es = UnityEngine.EventSystems.EventSystem.current;
                Vector2 start = RectTransformUtility.WorldToScreenPoint(null, knob.position);
                var ev = new UnityEngine.EventSystems.PointerEventData(es) { position = start, pressPosition = start, button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
                UnityEngine.EventSystems.ExecuteEvents.Execute(knob.gameObject, ev, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
                float yaw0 = rcc.transform.eulerAngles.y;
                for (int i = 0; i < 6; i++)
                {
                    ev.position = start + new Vector2(Mathf.Min(1f, (i + 1) / 3f) * 140f, 0f);
                    UnityEngine.EventSystems.ExecuteEvents.Execute(knob.gameObject, ev, UnityEngine.EventSystems.ExecuteEvents.dragHandler);
                    yield return new WaitForSeconds(0.25f);
                    pad = UnityEngine.InputSystem.Gamepad.current;
                    Debug.Log($"[ASBO] touchtest steer t={(i + 1) * 0.25f:F2}s stickX={(pad != null ? pad.leftStick.ReadValue().x : -9f):F2} yawChange={Mathf.DeltaAngle(yaw0, rcc.transform.eulerAngles.y):F1} speed={rcc.Speed * 3.6f:F0}");
                }
                UnityEngine.EventSystems.ExecuteEvents.Execute(knob.gameObject, ev, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
            }
            else Debug.Log("[ASBO] touchtest: no stick knob");
            Press("gas", false);
            Destroy(gameObject);
        }
    }

    public class BurstRunner : MonoBehaviour
    {
        public void Run(int n, float dt) { StartCoroutine(Go(n, dt)); }
        System.Collections.IEnumerator Go(int n, float dt)
        {
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/burst"); System.IO.Directory.CreateDirectory(dir);
            for (int i = 0; i < n; i++) { ScreenCapture.CaptureScreenshot(dir + "/b_" + i.ToString("000") + ".png"); yield return new WaitForSecondsRealtime(dt); }
            Destroy(gameObject);
        }
    }

    public class CrashShow : MonoBehaviour
    {
        public void Run(RccVehicle car, Person target, float speed, int frames) { StartCoroutine(Go(car, target, speed, frames)); }
        System.Collections.IEnumerator Go(RccVehicle car, Person target, float speed, int frames)
        {
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/crash"); System.IO.Directory.CreateDirectory(dir);
            foreach (var f in System.IO.Directory.GetFiles(dir, "*.png")) { try { System.IO.File.Delete(f); } catch { } }
            World.HideHud = true;
            yield return null; yield return null;
            var rb = car.GetComponent<Rigidbody>();
            Vector3 tp = target.transform.position;
            Vector3 dirv = tp - car.transform.position; dirv.y = 0f; dirv.Normalize();
            car.transform.rotation = Quaternion.LookRotation(dirv, Vector3.up);
            // film from the side of the line of travel
            var cg = new GameObject("CrashCam"); var cam = cg.AddComponent<Camera>(); cam.depth = 50; cam.fieldOfView = 50f; cam.nearClipPlane = 0.1f;
            var main = Camera.main; if (main != null) { var src = main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); var dst = cam.GetUniversalAdditionalCameraData(); if (src != null) { dst.renderPostProcessing = src.renderPostProcessing; dst.antialiasing = src.antialiasing; } }
            Vector3 side = Vector3.Cross(Vector3.up, dirv);
            cg.transform.position = tp + side * 9f - dirv * 3f + Vector3.up * 1.7f;
            cg.transform.LookAt(tp + dirv * 2.5f + Vector3.up * 0.8f);
            Time.captureFramerate = 30;
            bool hit = false; int hitFrame = -1;
            for (int i = 0; i < frames; i++)
            {
                if (!hit && rb != null && i < 90) { rb.linearVelocity = dirv * speed + Vector3.up * rb.linearVelocity.y; rb.angularVelocity = Vector3.zero; }
                if (!hit && target.state != Person.State.Walking) { hit = true; hitFrame = i; }
                ScreenCapture.CaptureScreenshot(dir + "/c_" + i.ToString("000") + ".png", 2);
                yield return null;
            }
            Time.captureFramerate = 0;
            Debug.Log("[ASBO] crash test: hit " + hit + " at frame " + hitFrame + ", target now " + target.state);
            Destroy(cg); World.HideHud = false; Destroy(gameObject);
        }
    }

    public class CarShowRun : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            foreach (var t in TrafficCar.All.ToArray()) if (t != null) t.gameObject.SetActive(false);
            World.HideHud = true;
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots"); System.IO.Directory.CreateDirectory(dir);
            var ids = new System.Collections.Generic.List<string>(CarBody.Models.Keys);
            var cars = new Transform[ids.Count];
            float z = World.RoadZ + 1.9f; var rng = new System.Random(7);
            for (int i = 0; i < ids.Count; i++)
            {
                var g = new GameObject("ShowCar_" + ids[i]).transform;
                Vector3 p = new Vector3(-60f + i * 7f, 5f, z);
                if (Physics.Raycast(p, Vector3.down, out var hit, 20f, ~(1 << 2), QueryTriggerInteraction.Ignore)) p.y = hit.point.y; else p.y = 0f;
                g.position = p; g.rotation = Quaternion.Euler(0, 90f, 0);
                var spec = CarBody.Get(ids[i]);
                CarBody.Build(g, spec, spec.van ? CarBody.VanPaint(rng) : CarBody.RandomPaint(rng), true, 300 + i * 17);
                cars[i] = g;
            }
            var cg = new GameObject("ShowCam"); var cam = cg.AddComponent<Camera>(); cam.depth = 50; cam.fieldOfView = 40f; cam.nearClipPlane = 0.05f;
            var main = Camera.main; if (main != null) { var src = main.GetComponent<UniversalAdditionalCameraData>(); var dst = cam.GetUniversalAdditionalCameraData(); if (src != null) { dst.renderPostProcessing = src.renderPostProcessing; dst.antialiasing = src.antialiasing; } }
            yield return null; yield return null;
            for (int i = 0; i < cars.Length; i++)
            {
                var c = cars[i]; Vector3 f = c.forward, l = -c.right, u = Vector3.up, at = c.position + u * 0.7f;
                foreach (var (name, pos, look) in new[] { ("front", c.position + f * 4.4f + l * 3.2f + u * 1.3f, at + f * 0.3f), ("rear", c.position - f * 4.4f - l * 3.0f + u * 1.4f, at - f * 0.3f) })
                {
                    cg.transform.position = pos; cg.transform.LookAt(look);
                    yield return null; yield return null;
                    ScreenCapture.CaptureScreenshot(dir + "/cars_" + ids[i] + "_" + name + ".png");
                    yield return null; yield return null;
                }
            }
            Destroy(cg);
            foreach (var c in cars) Destroy(c.gameObject);
            foreach (var t in TrafficCar.All.ToArray()) if (t != null) t.gameObject.SetActive(true);
            World.HideHud = false;
            Debug.Log("[ASBO] car show done");
            Destroy(gameObject);
        }
    }

    /// <summary>Follow-cam clip of an estate drunk walking (Shots/drunk/d_###.png at 30 fps): the stagger can't be judged from a still.</summary>
    public class DrunkShowRun : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            Person who = null;
            for (int tries = 0; tries < 20 && who == null; tries++)
            {
                foreach (var p in Person.All) if (p != null && p.drunk && p.state == Person.State.Walking && p.Ready) { var ag = p.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (ag != null && ag.velocity.magnitude > 0.3f) { who = p; break; } }
                if (who == null) yield return new WaitForSeconds(0.5f);
            }
            // put one out on the lane and send him up it, so the camera isn't stuck behind walls
            Person d0 = null; foreach (var p in Person.All) if (p != null && p.drunk && p.state == Person.State.Walking && p.Ready) { d0 = p; break; }
            if (d0 != null)
            {
                var ag0 = d0.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (ag0 != null && ag0.enabled && Nav.Snap(new Vector3(-12f, 0f, Estate.LaneZ), out var a0, 3f) && Nav.Snap(new Vector3(14f, 0f, Estate.LaneZ), out var b0, 3f))
                { ag0.Warp(a0); ag0.SetDestination(b0); who = d0; yield return new WaitForSeconds(2f); }
            }
            int drunks = 0; foreach (var p in Person.All) if (p != null && p.drunk) drunks++;
            Debug.Log("[ASBO] drunkshow: " + drunks + " drunks, filming " + (who != null ? who.name : "nobody"));
            if (who == null) { Destroy(gameObject); yield break; }
            World.HideHud = true;
            var cg = new GameObject("DrunkCam"); var cam = cg.AddComponent<Camera>(); cam.depth = 50; cam.fieldOfView = 42f; cam.nearClipPlane = 0.05f;
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/drunk"); System.IO.Directory.CreateDirectory(dir);
            foreach (var f in System.IO.Directory.GetFiles(dir, "*.png")) { try { System.IO.File.Delete(f); } catch { } }
            Time.captureFramerate = 30;
            for (int i = 0; i < 90; i++)
            {
                var t = who.transform; Vector3 side = Quaternion.Euler(0, 70f, 0) * t.forward;
                cg.transform.position = Vector3.Lerp(cg.transform.position == Vector3.zero ? t.position + side * 3.4f + Vector3.up * 1.3f : cg.transform.position, t.position + side * 3.4f + Vector3.up * 1.3f, i == 0 ? 1f : 0.15f);
                cg.transform.LookAt(t.position + Vector3.up * 0.9f);
                ScreenCapture.CaptureScreenshot(dir + "/d_" + i.ToString("000") + ".png");
                yield return null;
            }
            Time.captureFramerate = 0;
            Destroy(cg); World.HideHud = false; Destroy(gameObject);
        }
    }

    /// <summary>Plays one clip of Anim/FightTest on the player and photographs it every step seconds from the side (Shots/fight/c{clip}_###.png).</summary>
    public class FightFilmRun : MonoBehaviour
    {
        public int clip; public float step = 0.5f; public float secs = 10f, from = 0f;
        System.Collections.IEnumerator Start()
        {
            var pc = FindFirstObjectByType<PlayerCtl>(); var anim = pc != null ? pc.GetComponentInChildren<Animator>() : null;
            var ctrl = Resources.Load<RuntimeAnimatorController>("Anim/FightTest");
            if (anim == null || ctrl == null) { Debug.Log("[ASBO] fightfilm: no animator/controller"); Destroy(gameObject); yield break; }
            var keep = anim.runtimeAnimatorController; anim.runtimeAnimatorController = ctrl; anim.SetInteger("Clip", clip);
            World.HideHud = true;
            var cg = new GameObject("FightCam"); var cam = cg.AddComponent<Camera>(); cam.depth = 50; cam.fieldOfView = 40f; cam.nearClipPlane = 0.05f;
            var t = pc.transform; cg.transform.position = t.position + t.right * 4.2f + Vector3.up * 1.2f; cg.transform.LookAt(t.position + Vector3.up * 0.9f);
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/fight"); System.IO.Directory.CreateDirectory(dir);
            yield return null; yield return null;
            if (from > 0f) { var info = anim.GetCurrentAnimatorStateInfo(0); anim.Play("C" + clip, 0, from / Mathf.Max(0.01f, info.length)); yield return null; }
            float t0 = Time.time; int n = 0;
            while (Time.time - t0 < secs)
            {
                ScreenCapture.CaptureScreenshot(dir + "/c" + clip + "_" + (n++).ToString("000") + ".png");
                yield return new WaitForSeconds(step);
            }
            anim.runtimeAnimatorController = keep; Destroy(cg); World.HideHud = false;
            Debug.Log("[ASBO] fightfilm: clip " + clip + " " + n + " frames");
            Destroy(gameObject);
        }
    }

    /// <summary>Scripted scrap: the player squares up to a resident and throws punches; side camera films it (Shots/brawl/b_###.png every 0.1 s).</summary>
    public class BrawlTestRun : MonoBehaviour
    {
        public float secs = 22f; public bool kickFinish = true, onHit; int hitShots; string hitDir;
        void OnHitShot(Fighter a, Fighter v, float dmg)
        {
            if (!onHit || hitDir == null || hitShots >= 16 || a == null || v == null) return;
            ScreenCapture.CaptureScreenshot(hitDir + "/hit_" + (hitShots++).ToString("00") + ".png");
            Debug.Log($"[ASBO] brawltest hit {hitShots}: {(a.Body != null && a.Body.isPlayer ? "player" : "foe")} lands, {dmg:F0} dmg, bodies {(a.transform.position - v.transform.position).WithY(0).magnitude:F2} m apart");
        }
        void OnDestroy() { Fighter.OnHit -= OnHitShot; }
        System.Collections.IEnumerator Start()
        {
            Fighter.OnHit += OnHitShot;
            var pc = FindFirstObjectByType<PlayerCtl>(); var me = pc != null ? pc.GetComponent<Fighter>() : null;
            Resident foe = null; float bd = 1e9f;
            for (int tries = 0; tries < 30 && foe == null; tries++)
            {
                foreach (var r in FindObjectsByType<Resident>(FindObjectsSortMode.None))
                {
                    var pp = r.GetComponent<Person>(); if (pp == null || !pp.male || pp.state != Person.State.Walking || !pp.Ready) continue;
                    float d = (r.transform.position - pc.transform.position).sqrMagnitude; if (d < bd) { bd = d; foe = r; }
                }
                if (foe == null) yield return new WaitForSeconds(0.5f);
            }
            if (me == null || foe == null) { int nr = FindObjectsByType<Resident>(FindObjectsSortMode.None).Length, nm = 0; foreach (var r in FindObjectsByType<Resident>(FindObjectsSortMode.None)) { var pp = r.GetComponent<Person>(); if (pp != null && pp.male) nm++; } Debug.Log($"[ASBO] brawltest: pc={(pc != null)} fighter={(me != null)} residents={nr} male={nm}"); Destroy(gameObject); yield break; }
            var fp = foe.GetComponent<Person>(); var ff = foe.GetComponent<Fighter>();
            Vector3 spot = pc.transform.position + pc.transform.forward * 1.0f;
            foe.HoldAt(spot, pc.transform.eulerAngles.y + 180f, 30f);
            Debug.Log("[ASBO] brawltest: player vs " + foe.name + (fp.drunk ? " (drunk)" : ""));
            World.HideHud = true;
            var cg = new GameObject("BrawlCam"); var cam = cg.AddComponent<Camera>(); cam.depth = 50; cam.fieldOfView = 45f; cam.nearClipPlane = 0.05f;
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/brawl"); System.IO.Directory.CreateDirectory(dir);
            foreach (var f in System.IO.Directory.GetFiles(dir, "*.png")) { try { System.IO.File.Delete(f); } catch { } }
            hitDir = dir;
            float t0 = Time.time, nextShot = 0f, nextPunch = 0.6f; int n = 0, throws = 0; var lastState = fp.state;
            while (Time.time - t0 < secs)
            {
                float t = Time.time - t0;
                Vector3 mid = (pc.transform.position + foe.transform.position) * 0.5f;
                if (fp.state != Person.State.Walking) mid = (pc.transform.position + fp.BodyPos) * 0.5f;
                Vector3 side = Vector3.Cross(Vector3.up, (foe.transform.position - pc.transform.position).WithY(0).normalized); if (side.sqrMagnitude < 0.1f) side = Vector3.right;
                cg.transform.position = Vector3.Lerp(cg.transform.position == Vector3.zero ? mid + side * 4f + Vector3.up * 1.4f : cg.transform.position, mid + side * 4f + Vector3.up * 1.4f, n == 0 ? 1f : 0.1f);
                cg.transform.LookAt(mid + Vector3.up * 1.0f);
                if (fp.state != lastState) { Debug.Log($"[ASBO] brawltest t={t:F1} foe state {lastState} -> {fp.state}"); lastState = fp.state; }
                Fighter.TestGuard = t > 11f && t < 14.5f; // stand and block for a bit: the foe's swings should be soaked up
                if (t >= nextPunch && fp.state == Person.State.Walking && !Fighter.TestGuard)
                {
                    nextPunch = t + 0.5f;
                    Vector3 d = (foe.transform.position - pc.transform.position).WithY(0);
                    if (d.magnitude > 1.15f) { pc.transform.position += d.normalized * Mathf.Min(0.4f, d.magnitude - 1f); }
                    if (throws % 6 == 4) me.Attack(Fighter.Move.Hay); else if (kickFinish && throws % 6 == 5) me.Attack(Fighter.Move.Kick); else me.PunchButton();
                    throws++;
                    Debug.Log($"[ASBO] brawltest t={t:F1} throw {throws} foeHP={ff.hp:F0}/{ff.maxHp:F0} foeStam={ff.stam:F0} myHP={me.hp:F0} myStam={me.stam:F0} foe={fp.state} foeMode={ff.mode} fighting={ff.Fighting}");
                }
                if (Fighter.TestGuard && Time.frameCount % 20 == 0) Debug.Log($"[ASBO] brawltest guard t={t:F1} myHP={me.hp:F0} myStam={me.stam:F0} guard={me.guard} foeMode={ff.mode}");
                if (!onHit && t >= nextShot) { ScreenCapture.CaptureScreenshot(dir + "/b_" + (n++).ToString("000") + ".png"); nextShot = t + 0.1f; }
                yield return null;
            }
            Fighter.TestGuard = false;
            Debug.Log($"[ASBO] brawltest end: foe={fp.state} hurt={fp.hurt} foeHP={ff.hp:F0} myHP={me.hp:F0} rep={Rel.Rep} frames={n}");
            Destroy(cg); World.HideHud = false; Destroy(gameObject);
        }
    }

    /// <summary>
    /// Two residents square up to the player: logs who the player is squared up to, how far off each one stands (should
    /// hold round arm's length and circle), who swings when (taking turns), then the player drops their fists at 7 s.
    /// </summary>
    public class MobTestRun : MonoBehaviour
    {
        static string AgentInfo(Fighter f)
        {
            var ag = f.GetComponent<UnityEngine.AI.NavMeshAgent>(); var p = f.Body;
            return ag == null ? "no agent" : $"on {ag.isOnNavMesh} en {ag.enabled} stop {ag.isStopped} path {ag.hasPath} pend {ag.pathPending} rem {ag.remainingDistance:F2} v {ag.velocity.magnitude:F2} spd {ag.speed:F2} upd {ag.updatePosition} drunk {p.drunk} hurt {p.hurt} route {p.route.Count} res {f.GetComponent<Resident>()?.enabled} st {ag.pathStatus} | {f.Dbg} me {f.name}";
        }
        System.Collections.IEnumerator Start()
        {
            var pc = FindFirstObjectByType<PlayerCtl>(); var me = pc != null ? pc.GetComponent<Fighter>() : null;
            var near = FindObjectsByType<Resident>(FindObjectsSortMode.None).Select(r => r.GetComponent<Fighter>()).Where(f => f != null && f.Body != null && f.Body.Ready && !f.Down)
                .OrderBy(f => (f.transform.position - pc.transform.position).sqrMagnitude).Take(2).ToArray();
            if (me == null || near.Length < 2) { Debug.Log("[ASBO] mobtest: not enough people"); Destroy(gameObject); yield break; }
            for (int i = 0; i < 2; i++)
            {
                Vector3 at = pc.transform.position + Quaternion.Euler(0, 70f * (i * 2 - 1), 0) * pc.transform.forward * 2.8f;
                if (UnityEngine.AI.NavMesh.SamplePosition(at, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) at = hit.position;
                near[i].GetComponent<Resident>().HoldAt(at, 0f, 1f);
            }
            yield return new WaitForSeconds(0.4f);
            foreach (var f in near) f.SquareUp(me, true);
            float t0 = Time.time, next = 0f; bool dropped = false;
            while (Time.time - t0 < 13f)
            {
                float t = Time.time - t0;
                if (!dropped && t > 7f) { dropped = true; me.PlayerInput(false, false, false, false, true, true); Debug.Log("[ASBO] mobtest: fists down"); }
                if (t >= next)
                {
                    next = t + 0.5f;
                    string Who(Fighter f) => f == null ? "-" : f == near[0] ? "A" : f == near[1] ? "B" : "?";
                    Debug.Log($"[ASBO] mobtest t={t:F1} target={Who(me.Target)} A {(near[0].transform.position - pc.transform.position).WithY(0).magnitude:F2}m {near[0].mode}{(near[0].Fighting ? "" : " out")} B {(near[1].transform.position - pc.transform.position).WithY(0).magnitude:F2}m {near[1].mode}{(near[1].Fighting ? "" : " out")} AB {(near[0].transform.position - near[1].transform.position).WithY(0).magnitude:F2}m myHP={me.hp:F0} A: {AgentInfo(near[0])}");
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }

    /// <summary>The casino: outside from the car park, inside from the door and across the floor, then each game's screen in play (Shots/casino/c_#.png).</summary>
    public class CasinoShotsRun : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            var pc = FindFirstObjectByType<PlayerCtl>(); if (pc == null) { Destroy(gameObject); yield break; }
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/casino"); System.IO.Directory.CreateDirectory(dir); int n = 0;
            void Shot() { ScreenCapture.CaptureScreenshot(dir + "/c_" + (n++) + ".png"); }
            GameClock.Set(21.5f); // it looks its best lit up at night
            var o = CasinoHall.Origin;
            var views = new[] { (new Vector3(26f, 0.2f, -17f), 0f, 4f), (new Vector3(26f, 0.2f, 1.2f), 0f, 8f), (new Vector3(10.3f, 0.2f, 6.2f), 250f, 10f), (new Vector3(19.6f, 0.2f, 2.0f), 300f, 12f),
                (new Vector3(30f, 0.2f, 10.6f), 0f, 14f), (new Vector3(37.6f, 0.2f, 17.4f), 240f, 12f), (new Vector3(46.4f, 0.2f, 7.5f), 90f, 8f), (new Vector3(50.8f, 0.2f, 2.4f), 0f, 10f),
                (new Vector3(4.8f, 0.2f, 10.4f), 0f, 6f), (new Vector3(6.6f, 0.2f, 13.4f), 270f, 10f), (new Vector3(6.2f, 0.2f, 16.6f), 300f, 12f), (new Vector3(17.6f, 0.2f, 13.0f), 300f, 12f),
                (new Vector3(12.6f, 0.2f, 18.6f), 90f, 10f), (new Vector3(48.6f, 0.2f, 16.1f), 250f, 12f), (new Vector3(40.2f, 0.2f, 3.4f), 60f, 10f) };
            foreach (var (at, yaw, pitch) in views)
            { pc.Teleport(o + at, yaw, pitch); yield return new WaitForSeconds(1.6f); Shot(); yield return new WaitForSeconds(0.3f); }
            // try the things: a pint off the bar, sit on a stool, a door, the PC
            foreach (var (at, yaw) in new[] { (new Vector3(48.5f, 0.05f, 3.1f), 90f), (new Vector3(48.4f, 0.05f, 5.2f), 90f), (new Vector3(10f, 0.05f, 11.3f), 0f), (new Vector3(15.8f, 0.05f, 21.2f), 180f) })
            {
                pc.Teleport(o + at, yaw, 10f); yield return new WaitForSeconds(0.8f);
                bool used = PropWorld.TryUse(); yield return new WaitForSeconds(1.0f);
                Debug.Log($"[ASBO] casino use at {at}: used={used} holding={PropWorld.Holding} seated={pc.OnSeat}");
                Shot(); yield return new WaitForSeconds(0.3f);
                if (pc.OnSeat) pc.StandUp(); else if (PropWorld.Holding) PropWorld.TryUse();
                yield return new WaitForSeconds(0.4f);
            }
            ASBO.Casino.CasinoWallet.Held = 300;
            foreach (var g in new[] { ASBO.Casino.CasinoUI.Game.Roulette, ASBO.Casino.CasinoUI.Game.Blackjack, ASBO.Casino.CasinoUI.Game.Slots, ASBO.Casino.CasinoUI.Game.Poker })
            {
                ASBO.Casino.CasinoUI.Show(g, 2); yield return new WaitForSecondsRealtime(0.5f);
                ASBO.Casino.CasinoUI.DebugAct(); yield return new WaitForSecondsRealtime(g == ASBO.Casino.CasinoUI.Game.Roulette ? 6.5f : g == ASBO.Casino.CasinoUI.Game.Poker ? 5f : 2.5f);
                Shot(); yield return new WaitForSecondsRealtime(0.4f);
                if (g == ASBO.Casino.CasinoUI.Game.Roulette) { ASBO.Casino.CasinoUI.DebugAct(); yield return new WaitForSecondsRealtime(2.6f); Shot(); yield return new WaitForSecondsRealtime(0.4f); }
                ASBO.Casino.CasinoUI.Close(); yield return new WaitForSecondsRealtime(0.3f);
            }
            Debug.Log($"[ASBO] casino shots: {n} taken, chips now {ASBO.Casino.CasinoWallet.Held}");
            Destroy(gameObject);
        }
    }

    /// <summary>A traffic car nudges a resident at low speed: should fall, lie a moment, get up and limp off.</summary>
    public class KnockTestRun : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            var pc = FindFirstObjectByType<PlayerCtl>();
            Person v = null;
            foreach (var r in FindObjectsByType<Resident>(FindObjectsSortMode.None)) { var pp = r.GetComponent<Person>(); if (pp != null && pp.state == Person.State.Walking && pp.Ready) { v = pp; break; } }
            if (v == null) { Destroy(gameObject); yield break; }
            var res = v.GetComponent<Resident>(); res.HoldAt(pc.transform.position + pc.transform.forward * 2f, 0f, 30f);
            yield return new WaitForSeconds(0.3f);
            v.Hit(pc.transform.forward * 4f, v.transform.position + Vector3.up, 4f);
            World.HideHud = true;
            var cg = new GameObject("KnockCam"); var cam = cg.AddComponent<Camera>(); cam.depth = 50; cam.fieldOfView = 45f;
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/knock"); System.IO.Directory.CreateDirectory(dir);
            foreach (var f in System.IO.Directory.GetFiles(dir, "*.png")) { try { System.IO.File.Delete(f); } catch { } }
            for (int i = 0; i < 70; i++)
            {
                Vector3 c = v.BodyPos.WithY(v.transform.position.y);
                cg.transform.position = c + new Vector3(3.2f, 1.6f, 1.6f); cg.transform.LookAt(c + Vector3.up * 0.5f);
                ScreenCapture.CaptureScreenshot(dir + "/k_" + i.ToString("000") + ".png");
                if (i % 10 == 0) Debug.Log($"[ASBO] knocktest t={i * 0.2f:F1} state={v.state} hurt={v.hurt}");
                yield return new WaitForSeconds(0.2f);
            }
            Destroy(cg); World.HideHud = false; Destroy(gameObject);
        }
    }

    /// <summary>Drive the arcade car with the touch flags: forward, steer right, brake, reverse. Logs speed; films from behind.</summary>
    public class CarTestRun : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            var pc = FindFirstObjectByType<PlayerCtl>(); var car = PlayerCar.I;
            if (pc == null || car == null) { Debug.Log("[ASBO] cartest: no player or car"); Destroy(gameObject); yield break; }
            pc.transform.position = car.ExitPoint; yield return new WaitForSeconds(0.3f);
            var m = typeof(PlayerCtl).GetMethod("ToggleCar", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); m.Invoke(pc, null);
            yield return new WaitForSeconds(0.3f);
            Debug.Log("[ASBO] cartest: inCar=" + pc.InCar);
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/cartest"); System.IO.Directory.CreateDirectory(dir);
            foreach (var f in System.IO.Directory.GetFiles(dir, "*.png")) { try { System.IO.File.Delete(f); } catch { } }
            float t0 = Time.time; int n = 0; float nextShot = 0f, nextLog = 0f; Vector3 p0 = car.transform.position;
            while (Time.time - t0 < 15f)
            {
                float t = Time.time - t0;
                TouchCar.Clear();
                if (t < 4f) TouchCar.fwd = true;                       // floor it
                if (t > 2.5f && t < 4f) TouchCar.right = true;          // and turn
                if (t >= 4f && t < 5.5f) TouchCar.brake = true;         // brake to a stop
                if (t >= 6f && t < 8.5f) { TouchCar.brake = true; if (t > 7f) TouchCar.left = true; } // brake again: reverses
                if (t >= 9f && t < 11.5f) TouchCar.fwd = true;         // off again
                if (t >= 11.5f && t < 13f) { TouchCar.hand = true; TouchCar.right = true; } // handbrake turn
                if (t >= nextLog) { nextLog = t + 0.5f; Debug.Log($"[ASBO] cartest t={t:F1} speed={car.SignedSpeed * 3.6f:F0} km/h yaw={car.transform.eulerAngles.y:F0} slip={car.Slip:F0} moved={(car.transform.position - p0).magnitude:F1}m"); }
                if (t >= nextShot) { nextShot = t + 0.25f; ScreenCapture.CaptureScreenshot(dir + "/c_" + (n++).ToString("000") + ".png"); }
                yield return null;
            }
            TouchCar.Clear(); m.Invoke(pc, null);
            Destroy(gameObject);
        }
    }

    /// <summary>Each car type on the long road: 0-100, top speed in 8 s, braking distance, handbrake turn, crash dent. One line per car.</summary>
    public class CarTypesRun : MonoBehaviour
    {
        public string only; public bool verbose;
        System.Collections.IEnumerator Start()
        {
            string[] ids = { "supermini", "hatch70", "hatch90", "saloon90", "estate90", "boxy80", "coupe80", "rally", "jeep" };
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/cartypes"); System.IO.Directory.CreateDirectory(dir);
            World.HideHud = true;
            var traffic = Object.FindObjectsByType<TrafficCar>(FindObjectsSortMode.None); foreach (var tc in traffic) tc.gameObject.SetActive(false); // a clear road
            var cg = new GameObject("CarCam"); var cam = cg.AddComponent<Camera>(); cam.depth = 50; cam.fieldOfView = 38f;
            foreach (var id in ids)
            {
                if (!string.IsNullOrEmpty(only) && only != "all" && !only.Split(',').Contains(id)) continue;
                if (!CarKit.Has(id)) continue;
                Vector3 start = new Vector3(-150f, 0.25f, World.RoadZ - 2.1f);
                var car = PlayerCar.Create(start, 90f, new Color(0.15f, 0.25f, 0.5f), id, CarKit.Wear.Clean, 7);
                yield return new WaitForSeconds(0.8f);
                cg.transform.position = car.transform.position + car.transform.right * -6.5f + Vector3.up * 1.4f; cg.transform.LookAt(car.transform.position + Vector3.up * 0.6f);
                yield return null; ScreenCapture.CaptureScreenshot(dir + "/" + id + "_side.png"); yield return null;
                car.driven = true; yield return new WaitForSeconds(0.3f);
                float t0 = Time.time, t100 = -1f, vmax = 0f; float rest = car.transform.position.y; int gmax = 0; float rpmMax = 0f;
                var rc = car.GetComponent<RCCP_CarController>();
                TouchCar.Clear(); TouchCar.fwd = true;
                while (Time.time - t0 < 8f && car.transform.position.x < 140f)
                {
                    float v = car.SignedSpeed * 3.6f; vmax = Mathf.Max(vmax, v); if (t100 < 0f && v >= 100f) t100 = Time.time - t0;
                    if (rc != null) { gmax = Mathf.Max(gmax, rc.currentGear); rpmMax = Mathf.Max(rpmMax, rc.engineRPM); }
                    if (verbose && Time.frameCount % 25 == 0) Debug.Log($"[ASBO] cartrace {id} t={Time.time - t0:F1} v={v:F0} gear={rc?.currentGear} dir={rc?.direction} rpm={rc?.engineRPM:F0} thrV={rc?.throttleInput_V:F2} brkV={rc?.brakeInput_V:F2} slip={(rc != null && rc.Stability != null ? rc.Stability.TCSEngaged.ToString() : "-")} tq={rc?.producedEngineTorque:F0}");
                    cg.transform.position = car.transform.position - car.transform.forward * 7f + Vector3.up * 2.4f; cg.transform.LookAt(car.transform.position + Vector3.up * 0.8f);
                    yield return new WaitForFixedUpdate();
                }
                float accT = Time.time - t0;
                ScreenCapture.CaptureScreenshot(dir + "/" + id + "_run.png");
                // brake from whatever it's doing to a stop
                float vb = car.SignedSpeed * 3.6f; Vector3 pb = car.transform.position; TouchCar.Clear(); TouchCar.brake = true; float tb = Time.time;
                while (car.SignedSpeed > 0.5f && Time.time - tb < 8f) yield return new WaitForFixedUpdate();
                float bdist = (car.transform.position - pb).WithY(0).magnitude; TouchCar.Clear();
                yield return new WaitForSeconds(0.4f);
                // handbrake turn at about 50 km/h
                TouchCar.fwd = true; float th = Time.time;
                while (car.SignedSpeed * 3.6f < 50f && Time.time - th < 6f)
                {
                    if (verbose && Time.frameCount % 25 == 0) Debug.Log($"[ASBO] cartrace {id} restart t={Time.time - th:F1} v={car.SignedSpeed * 3.6f:F0} gear={rc?.currentGear} dir={rc?.direction} rpm={rc?.engineRPM:F0} thrV={rc?.throttleInput_V:F2} brkV={rc?.brakeInput_V:F2} hbV={rc?.handbrakeInput_V:F2}");
                    yield return new WaitForFixedUpdate();
                }
                float yaw0 = car.transform.eulerAngles.y, vh = car.SignedSpeed * 3.6f; TouchCar.fwd = false; TouchCar.hand = true; TouchCar.right = true; float th2 = Time.time; float slipMax = 0f;
                while (Time.time - th2 < 1.4f) { slipMax = Mathf.Max(slipMax, car.Slip); cg.transform.position = car.transform.position + Vector3.up * 9f - Vector3.right * 4f; cg.transform.LookAt(car.transform.position); yield return new WaitForFixedUpdate(); }
                float turned = Mathf.DeltaAngle(yaw0, car.transform.eulerAngles.y); TouchCar.Clear();
                ScreenCapture.CaptureScreenshot(dir + "/" + id + "_hb.png");
                yield return new WaitForSeconds(0.6f);
                float tilt = Vector3.Angle(car.transform.up, Vector3.up);
                Debug.Log($"[ASBO] cartype {id} ({(car.Physical ? "RCC" : "arcade")}): 0-100 {(t100 < 0 ? "-" : t100.ToString("F1") + "s")}, {vmax:F0} km/h after {accT:F1}s, braking from {vb:F0}: {bdist:F0} m, handbrake at {vh:F0}: turned {turned:F0} deg (slip {slipMax:F0}), tilt now {tilt:F0}, ride {rest:F2}, top gear {gmax} rpm {rpmMax:F0}{(rc != null ? $" thr {rc.throttleInput_V:F2} running {rc.engineRunning} canControl {rc.canControl}" : "")}");
                car.driven = false; yield return new WaitForSeconds(0.2f);
                Destroy(car.gameObject); yield return new WaitForSeconds(0.3f);
            }
            TouchCar.Clear(); Destroy(cg); World.HideHud = false; foreach (var tc in traffic) if (tc != null) tc.gameObject.SetActive(true); Destroy(gameObject);
            Debug.Log("[ASBO] cartypes done");
        }
    }

    public class DentTestRun : MonoBehaviour
    {
        public string id;
        System.Collections.IEnumerator Start()
        {
            var traffic = Object.FindObjectsByType<TrafficCar>(FindObjectsSortMode.None); foreach (var tc in traffic) tc.gameObject.SetActive(false);
            float z = World.RoadZ - 2.1f;
            var parked = CarKit.Scenery(World.Root, "saloon90", CarKit.Wear.Clean, new Vector3(-100f, 0.05f, z), 90f, 5);
            var car = PlayerCar.Create(new Vector3(-145f, 0.25f, z), 90f, new Color(0.6f, 0.1f, 0.1f), id, CarKit.Wear.Clean, 3);
            yield return new WaitForSeconds(0.8f);
            DebugCmd.ShotAt(-104f, 1.6f, z - 5f, -101f, 0.7f, z, "dent_before");
            car.driven = true; TouchCar.Clear(); TouchCar.fwd = true; float t0 = Time.time, vHit = 0f;
            while (Time.time - t0 < 9f && car.transform.position.x < -103.5f) { vHit = car.SignedSpeed * 3.6f; yield return new WaitForFixedUpdate(); }
            yield return new WaitForSeconds(0.25f); TouchCar.Clear(); TouchCar.brake = true;
            yield return new WaitForSeconds(1.5f); TouchCar.Clear();
            var front = car.transform.position + car.transform.forward * 2.2f;
            DebugCmd.ShotAt(front.x + 1.5f, 1.4f, z - 3.6f, front.x, 0.6f, z, "dent_after");
            DebugCmd.ShotAt(front.x + 4.5f, 1.3f, z + 2.8f, front.x + 1.2f, 0.6f, z, "dent_after2");
            var d1 = car.GetComponent<RMD_Deformation>(); var d2 = parked != null ? parked.GetComponent<RMD_Deformation>() : null;
            Debug.Log($"[ASBO] denttest {id}: hit at {vHit:F0} km/h, car dent {(d1 != null ? d1.overallDeform.ToString("F3") : "none")}, parked dent {(d2 != null ? d2.overallDeform.ToString("F3") : "none")}");
            car.driven = false; foreach (var tc in traffic) if (tc != null) tc.gameObject.SetActive(true);
            Destroy(gameObject);
        }
    }

    public class PreviewRun : MonoBehaviour
    {
        public string which;
        System.Collections.IEnumerator Start()
        {
            var root = World.Root != null ? World.Root : null;
            void Fresh(string n) { var o = root != null ? root.Find(n) : null; if (o != null) Destroy(o.gameObject); }
            if (which == "estates") { Fresh(Estates.RootName); Fresh("Estate Cars"); yield return null; Estates.Build(root); Estates.Dress(root); }
            if (which == "services") { Fresh(Services.RootName); yield return null; Services.Build(root); }
            if (which == "precinct") { Fresh(Precinct.RootName); yield return null; Precinct.Build(root); }
            if (which == "industrial") { Fresh(Industrial.RootName); yield return null; Industrial.Build(root); }
            yield return new WaitForSeconds(0.5f);
            // (eye x, y, z, look-at x, y, z)
            var views = which == "estates" ? new[]
            {
                new[] { -150f, 60f, 20f, -232f, 10f, 75f }, new[] { -168f, 1.7f, 64f, -230f, 8f, 75f }, new[] { -200f, 1.7f, 70f, -223f, 20f, 100f },
                new[] { -230f, 45f, 190f, -230f, 5f, 140f }, new[] { 125f, 60f, 10f, 205f, 10f, 75f }, new[] { 134f, 1.7f, 26f, 180f, 10f, 70f },
                new[] { 136f, 1.7f, 80f, 150f, 14f, 80f }, new[] { 225f, 1.7f, 40f, 200f, 20f, 75f }, new[] { 0f, 55f, -110f, -5f, 5f, -170f },
            } : which == "services" ? new[]
            {
                new[] { -195f, 18f, -48f, -210f, 3f, -10f }, new[] { -210f, 1.7f, -24f, -210f, 2.5f, -10f }, new[] { -212f, 1.6f, -14.5f, -205f, 1.2f, -3f },
                new[] { 239f, 14f, -50f, 245f, 3f, 0f }, new[] { 239f, 1.7f, -12f, 236f, 1.6f, 2f }, new[] { 0f, 30f, 100f, 10f, 8f, 160f },
                new[] { 52f, 1.7f, 132f, 50f, 2.5f, 148f }, new[] { 52f, 1.6f, 150f, 45f, 1.2f, 166f }, new[] { 0f, 1.6f, 147f, 0f, 1.6f, 158f },
            } : which == "precinct" ? new[]
            {
                new[] { 40f, 40f, 40f, 40f, 0f, 93f }, new[] { 20f, 1.7f, 120f, 30f, 3f, 93f }, new[] { 40f, 1.7f, 86f, 50f, 4f, 104f },
                new[] { 0f, 1.7f, 95f, -20f, 3f, 82f }, new[] { -26f, 1.7f, 93f, -40f, 2.5f, 93f }, new[] { 8f, 1.6f, 107f, 8f, 1.4f, 112f },
                new[] { 30f, 1.6f, 103f, 30f, 1.4f, 112f }, new[] { 70f, 1.7f, 95f, 100f, 3f, 92f }, new[] { 40f, 20f, 140f, 40f, 0f, 93f },
            } : which == "industrial" ? new[]
            {
                new[] { 210f, 40f, -40f, 200f, 0f, -100f }, new[] { 196f, 1.7f, -60f, 180f, 3f, -66f }, new[] { 182f, 1.6f, -60f, 170f, 1.5f, -62f },
                new[] { 196f, 1.7f, -86f, 182f, 2f, -88f }, new[] { 196f, 6f, -120f, 175f, 1f, -140f }, new[] { 204f, 1.7f, -52f, 214f, 2.5f, -55f },
                new[] { 205f, 1.7f, -90f, 260f, 4f, -80f }, new[] { 175f, 1.6f, -66f, 177f, 2f, -60f }, new[] { 216f, 1.6f, -58f, 225f, 1.4f, -55f },
            } : which == "casino" ? new[]
            {
                new[] { 188f, 6f, -26f, 188f, 3f, -2f }, new[] { 181f, 1.7f, -16f, 188f, 3f, -4f }, new[] { 188f, 1.7f, -9f, 188f, 2f, 0f },
                new[] { 186f, 1.7f, 1f, 176f, 2f, 12f }, new[] { 172f, 1.7f, 2f, 190f, 2.5f, 12f }, new[] { 198f, 1.7f, 8f, 205f, 1.5f, 2f },
                new[] { 205f, 1.7f, 16f, 190f, 2f, 8f }, new[] { 175f, 2.5f, 18f, 188f, 2.6f, 9f }, new[] { 160f, 20f, -40f, 188f, 0f, 0f },
            } : new float[0][];
            for (int i = 0; i < views.Length; i++) { var v = views[i]; DebugCmd.ShotAt(v[0], v[1], v[2], v[3], v[4], v[5], "prev_" + i); yield return null; }
            Debug.Log("[ASBO] preview " + which + ": " + views.Length + " views");
            Destroy(gameObject);
        }
    }

    public class GangTestRun : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            var pc = FindFirstObjectByType<PlayerCtl>(); if (pc == null) yield break;
            pc.Teleport(new Vector3(-196f, 0.2f, 58f), 200f, 10f); yield return new WaitForSeconds(4f);
            DebugCmd.ShotAt(-190f, 2.2f, 60f, -205f, 1.2f, 46f, "gang_cage");
            DebugCmd.ShotAt(-180f, 1.7f, 62f, -215f, 1f, 68f, "gang_close");
            yield return new WaitForSeconds(12f);
            Debug.Log($"[ASBO] gang test: fights on: {Fighter.Session.Hot}");
            DebugCmd.ShotAt(pc.transform.position.x + 4f, 2.4f, pc.transform.position.z + 4f, pc.transform.position.x, 1f, pc.transform.position.z, "gang_fight");
            pc.Teleport(new Vector3(-196f, 0.2f, 47.5f), 180f, 10f); yield return new WaitForSeconds(1f);
            Debug.Log($"[ASBO] gang test: dealer menu {Gangs.TryDeal()}");
            Drugs.Take(Drugs.Kind.Wow); yield return new WaitForSeconds(12f);
            ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/views/gang_wow.png"));
            Destroy(gameObject);
        }
    }

    public class ClimbTestRun : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            var pc = FindFirstObjectByType<PlayerCtl>(); if (pc == null) yield break;
            foreach (var (n, p, yaw) in new[] { ("planter", new Vector3(20f, 0.3f, 85.6f), 0f), ("bandstand side", new Vector3(46.5f, 0.3f, 93f), -90f), ("crane deck", new Vector3(176f, 0.2f, -135f), 0f) })
            {
                pc.Teleport(p, yaw, 8f); yield return new WaitForSeconds(0.6f);
                var cc = pc.GetComponent<CharacterController>(); for (float t = 0; t < 0.8f; t += Time.deltaTime) { cc.Move((pc.transform.forward * 1.5f + Vector3.down) * Time.deltaTime); yield return null; } // walk up to it
                float y0 = pc.transform.position.y; string why = pc.LedgeWhy(); PlayerCtl.jumpTap = true;
                Debug.Log($"[ASBO] climb test {n}: ledge says {why}; grounded {pc.GetComponent<CharacterController>().isGrounded}");
                yield return new WaitForSeconds(2.2f);
                Debug.Log($"[ASBO] climb test {n}: from y {y0:F2} to y {pc.transform.position.y:F2} at {pc.transform.position}");
            }
            Destroy(gameObject);
        }
    }

    public class AmbLookRun : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            var pf = Resources.Load<GameObject>("Cars/ambulance"); if (pf == null) { Debug.Log("[ASBO] amblook: no ambulance prefab"); Destroy(gameObject); yield break; }
            foreach (var tc in Object.FindObjectsByType<TrafficCar>(FindObjectsSortMode.None)) tc.gameObject.SetActive(false);
            var a = Instantiate(pf, new Vector3(-120f, 0.05f, World.RoadZ - 2.1f), Quaternion.Euler(0, 90f, 0));
            var dl = a.transform.Find("door_L"); var dr = a.transform.Find("door_R");
            yield return null;
            Vector3 c = a.transform.position;
            DebugCmd.ShotAt(c.x, 1.6f, c.z - 8f, c.x, 1.4f, c.z, "amb_side");
            DebugCmd.ShotAt(c.x + 7f, 1.8f, c.z - 5f, c.x, 1.3f, c.z, "amb_front");
            DebugCmd.ShotAt(c.x - 1f, 1.8f, c.z + 7f, c.x, 1.4f, c.z, "amb_near");
            if (dl != null) dl.localRotation = Quaternion.AngleAxis(-100f, Vector3.up) * dl.localRotation;
            if (dr != null) dr.localRotation = Quaternion.AngleAxis(100f, Vector3.up) * dr.localRotation;
            yield return null;
            DebugCmd.ShotAt(c.x - 6.5f, 1.7f, c.z - 0.3f, c.x, 1.5f, c.z, "amb_back");
            DebugCmd.ShotAt(c.x - 4.6f, 1.7f, c.z + 1.6f, c.x, 1.4f, c.z - 0.4f, "amb_backL");
            DebugCmd.ShotAt(c.x - 4.6f, 1.7f, c.z - 1.6f, c.x, 1.4f, c.z + 0.4f, "amb_backR");
            Destroy(a); foreach (var tc in TrafficCar.All) if (tc != null) tc.gameObject.SetActive(true);
            Destroy(gameObject);
        }
    }

    public class SeatTestRun : MonoBehaviour
    {
        public string id;
        System.Collections.IEnumerator Start()
        {
            var pc = FindFirstObjectByType<PlayerCtl>(); if (pc == null) yield break;
            var traffic = Object.FindObjectsByType<TrafficCar>(FindObjectsSortMode.None);
            foreach (var tc in traffic) tc.gameObject.SetActive(false);
            var car = PlayerCar.Create(new Vector3(-120f, 0.2f, World.RoadZ - 2.1f), 90f, new Color(0.15f, 0.2f, 0.45f), id, CarKit.Wear.Clean, 5);
            yield return new WaitForSeconds(0.8f);
            pc.Teleport(car.transform.position - car.transform.right * 2.3f, 90f); yield return new WaitForSeconds(0.4f);
            pc.UseCar(); yield return new WaitForSeconds(2.2f);
            Vector3 c = car.transform.position; var head = pc.GetComponentInChildren<Animator>() is Animator an && an.isHuman ? an.GetBoneTransform(HumanBodyBones.Head) : null;
            Debug.Log($"[ASBO] seat test {id}: inCar={pc.InCar} body at {pc.transform.position - c} head at {(head != null ? (head.position - c).ToString() : "?")} roof ~1.4");
            DebugCmd.ShotAt(c.x + 0.3f, 1.25f, c.z - 3.2f, c.x - 0.1f, 1.0f, c.z, id + "_seat_side");
            DebugCmd.ShotAt(c.x + 5f, 1.5f, c.z - 1.2f, c.x, 1.0f, c.z - 0.3f, id + "_seat_front");
            DebugCmd.ShotAt(c.x - 6f, 2.2f, c.z + 0.5f, c.x, 1.0f, c.z, id + "_seat_back");
            pc.UseCar(); yield return new WaitForSeconds(1.5f);
            Destroy(car.gameObject);
            foreach (var tc in traffic) if (tc != null) tc.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            // a passing driver: the nearest moving car, from the pavement side
            TrafficCar best = null; float bd = 1e9f;
            foreach (var tc in TrafficCar.All) { if (tc == null) continue; float d = (tc.transform.position - pc.transform.position).sqrMagnitude; if (d < bd) { bd = d; best = tc; } }
            if (best != null)
            {
                var t = best.transform; Vector3 p = t.position + t.right * 2.6f + t.forward * 0.8f + Vector3.up * 1.2f, at = t.position + t.right * 0.4f + Vector3.up * 1.05f;
                DebugCmd.ShotAt(p.x, p.y, p.z, at.x, at.y, at.z, "traffic_driver_side");
                p = t.position + t.forward * 4.5f + t.right * 1.2f + Vector3.up * 1.4f;
                DebugCmd.ShotAt(p.x, p.y, p.z, at.x, at.y, at.z, "traffic_driver_front");
            }
            Destroy(gameObject);
        }
    }

    public class CarLookRun : MonoBehaviour
    {
        public string id;
        static string Path(Transform t) { string n = t.name; while (t.parent != null) { t = t.parent; n = t.name + "/" + n; } return n; }
        System.Collections.IEnumerator Start()
        {
            foreach (var tc in Object.FindObjectsByType<TrafficCar>(FindObjectsSortMode.None)) tc.gameObject.SetActive(false);
            var hidden = new List<GameObject>();
            foreach (var pk in Object.FindObjectsByType<ParkedCar>(FindObjectsSortMode.None)) { hidden.Add(pk.gameObject); pk.gameObject.SetActive(false); }
            foreach (var other in PlayerCar.All.ToArray()) if (other != null && other.gameObject.activeSelf) { hidden.Add(other.gameObject); other.gameObject.SetActive(false); }
            float z = World.RoadZ - 2.1f; var p = new Vector3(-120f, 0.2f, z);
            var car = PlayerCar.Create(p, 90f, new Color(0.12f, 0.3f, 0.2f), id, CarKit.Wear.Clean, 11);
            yield return new WaitForSeconds(1.2f);
            Vector3 c = car.transform.position;
            // anything drawn round the car that isn't the car
            foreach (var rd in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!rd.enabled || !rd.gameObject.activeInHierarchy || rd.transform.IsChildOf(car.transform)) continue;
                var q = rd.bounds.center - c; if (Mathf.Abs(q.x) < 4f && Mathf.Abs(q.z) < 4f && rd.bounds.size.magnitude < 6f) Debug.Log($"[ASBO] near car: {Path(rd.transform)} at {q} size {rd.bounds.size}");
            }
            foreach (var rd in car.GetComponentsInChildren<Renderer>()) if (rd.enabled && rd.bounds.center.y < c.y + 0.35f && Mathf.Abs(rd.bounds.center.z - c.z) > 1.1f) Debug.Log($"[ASBO] car part out at the side: {Path(rd.transform)} at {rd.bounds.center - c}");
            foreach (var t in car.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("DoorHinge"))
                    foreach (var mr in t.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        var mesh = mr.GetComponent<MeshFilter>().sharedMesh; var sb = new System.Text.StringBuilder();
                        for (int k = 0; k < mesh.subMeshCount; k++) sb.Append($" [{k}:{mesh.GetSubMesh(k).indexCount / 3} tris, {(k < mr.sharedMaterials.Length && mr.sharedMaterials[k] != null ? mr.sharedMaterials[k].name : "-")}]");
                        Debug.Log($"[ASBO] door part {mr.name}: {mesh.vertexCount} verts, uv0 {mesh.uv.Length}{sb}");
                    }
            DebugCmd.ShotAt(c.x, 1.3f, c.z - 6f, c.x, 0.7f, c.z, id + "_look_side");
            DebugCmd.ShotAt(c.x + 4.8f, 1.6f, c.z - 4.2f, c.x, 0.6f, c.z, id + "_look_front");
            car.Door(1f); yield return new WaitForSeconds(1.2f);
            DebugCmd.ShotAt(c.x - 1.9f, 1.6f, c.z - 3.4f, c.x + 0.3f, 0.8f, c.z, id + "_look_door");
            DebugCmd.ShotAt(c.x + 3.2f, 1.5f, c.z - 2.6f, c.x + 0.2f, 0.8f, c.z - 0.6f, id + "_look_doorin");
            DebugCmd.ShotAt(c.x - 0.2f, 3.4f, c.z - 1.6f, c.x + 0.3f, 0.5f, c.z - 0.5f, id + "_look_doortop");
            Destroy(car.gameObject); foreach (var h in hidden) if (h != null) h.SetActive(true);
            Destroy(gameObject);
        }
    }

    public class ShotOfRun : MonoBehaviour
    {
        public void Go(Transform t, float angle, float dist, float height) { StartCoroutine(Run(t, angle, dist, height)); }
        System.Collections.IEnumerator Run(Transform t, float angle, float dist, float height)
        {
            World.HideHud = true;
            var cg = new GameObject("ShotCam"); var cam = cg.AddComponent<Camera>(); cam.depth = 50; cam.fieldOfView = 40f; cam.nearClipPlane = 0.05f;
            var main = Camera.main; if (main != null) { var src = main.GetComponent<UniversalAdditionalCameraData>(); var dst = cam.GetUniversalAdditionalCameraData(); if (src != null) { dst.renderPostProcessing = src.renderPostProcessing; dst.antialiasing = src.antialiasing; } }
            Vector3 dir = Quaternion.Euler(0, angle, 0) * t.forward;
            cg.transform.position = t.position + dir * dist + Vector3.up * height;
            cg.transform.LookAt(t.position + Vector3.up * 0.5f);
            yield return null; yield return null;
            string d = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots"); System.IO.Directory.CreateDirectory(d);
            ScreenCapture.CaptureScreenshot(d + "/of_" + t.name + "_" + Mathf.RoundToInt(angle) + ".png");
            yield return null; yield return null;
            Destroy(cg); World.HideHud = false; Destroy(gameObject);
        }
    }

    public class PeopleShowRun : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            foreach (var t in TrafficCar.All.ToArray()) if (t != null) t.gameObject.SetActive(false);
            World.HideHud = true;
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots"); System.IO.Directory.CreateDirectory(dir);
            var ppl = new System.Collections.Generic.List<Person>();
            float z = World.RoadZ;
            for (int i = 0; i < 10; i++)
            {
                var p = World.MakePerson(new Vector3(-48f + i * 1.1f, 0.05f, z), Color.grey, Color.grey, "Resident");
                p.transform.rotation = Quaternion.Euler(0, 180f, 0);
                ppl.Add(p);
            }
            for (int i = 0; i < 90; i++) yield return null; // let the bodies build
            var cg = new GameObject("ShowCam"); var cam = cg.AddComponent<Camera>(); cam.depth = 50; cam.fieldOfView = 32f; cam.nearClipPlane = 0.05f;
            var main = Camera.main; if (main != null) { var src = main.GetComponent<UniversalAdditionalCameraData>(); var dst = cam.GetUniversalAdditionalCameraData(); if (src != null) { dst.renderPostProcessing = src.renderPostProcessing; dst.antialiasing = src.antialiasing; } }
            for (int g = 0; g < 2; g++)
            {
                float cx = -48f + (g * 5 + 2f) * 1.1f;
                cg.transform.position = new Vector3(cx, 1.2f, z - 7.5f); cg.transform.LookAt(new Vector3(cx, 0.95f, z));
                yield return null; yield return null;
                ScreenCapture.CaptureScreenshot(dir + "/people_" + g + ".png", 2);
                yield return null; yield return null;
            }
            Destroy(cg);
            foreach (var p in ppl) if (p != null) Destroy(p.gameObject);
            foreach (var t in TrafficCar.All.ToArray()) if (t != null) t.gameObject.SetActive(true);
            World.HideHud = false;
            Debug.Log("[ASBO] people show done");
            Destroy(gameObject);
        }
    }

    /// <summary>Talk to the nearest resident (natter twice, flirt), then give another a grudge and let them come over for a word.</summary>
    public class TalkTestRun : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            var pc = FindFirstObjectByType<PlayerCtl>();
            Resident a = null, b = null;
            for (int tries = 0; tries < 30 && (a == null || b == null); tries++)
            {
                a = b = null; float da = 1e9f, db = 1e9f;
                foreach (var r in FindObjectsByType<Resident>(FindObjectsSortMode.None))
                {
                    var pp = r.GetComponent<Person>(); if (pp == null || pp.state != Person.State.Walking || !pp.Ready) continue;
                    float d = (r.transform.position - pc.transform.position).sqrMagnitude;
                    if (d < da) { db = da; b = a; da = d; a = r; } else if (d < db) { db = d; b = r; }
                }
                if (a == null || b == null) yield return new WaitForSeconds(0.5f);
            }
            if (pc == null || a == null || b == null || Social.I == null) { Debug.Log("[ASBO] talktest: missing"); Destroy(gameObject); yield break; }
            var pa = a.GetComponent<Person>();
            a.HoldAt(pc.transform.position + pc.transform.forward * 1.4f, pc.transform.eulerAngles.y + 180f, 40f);
            yield return new WaitForSeconds(0.6f);
            Debug.Log($"[ASBO] talktest: near={(Social.I.Near != null ? Persona.Name(Social.I.Near) : "none")} talking to {Persona.Name(pa)}");
            Social.I.TalkPressed(); yield return new WaitForSeconds(1.2f);
            DialogueUI.Choose(0); yield return new WaitForSeconds(1.2f);   // natter
            DialogueUI.Choose(0); yield return new WaitForSeconds(1.2f);   // natter again (talked out)
            DialogueUI.Choose(1); yield return new WaitForSeconds(1.2f);   // flirt or leave
            Debug.Log($"[ASBO] talktest: friend={Rel.Of(pa).friend:F0} romance={Rel.Of(pa).romance:F0} met={Rel.Of(pa).met} open={DialogueUI.Open}");
            if (DialogueUI.Open) Social.I.EndTalk(true);
            // a grudge: they should come over for a word
            var pb = b.GetComponent<Person>(); var rb = Rel.Of(pb); rb.friend = -45f; rb.grudge = 70f; rb.met = true; Rel.Set(pb, rb);
            Social.I.approachCool = 0f;
            for (float t = 0f; t < 20f; t += 0.5f)
            {
                yield return new WaitForSeconds(0.5f);
                if (Social.I.Near == pb) { Debug.Log($"[ASBO] talktest: {Persona.Name(pb)} came over after {t:F1}s"); Social.I.TalkPressed(); yield return new WaitForSeconds(1f); DialogueUI.Choose(0); break; }
            }
            yield return new WaitForSeconds(3f);
            var fb = b.GetComponent<Fighter>();
            Debug.Log($"[ASBO] talktest end: {Persona.Name(pb)} grudge={Rel.Of(pb).grudge:F0} fighting={(fb != null && fb.Fighting)} cash={Econ.Cash:F2}");
            if (fb != null && fb.Fighting) fb.Leave(null);
            Destroy(gameObject);
        }
    }

    /// <summary>Evening to morning fast: who's out on the street each game hour, the tag at the zone's edge, and a frame of night.</summary>
    public class NightTestRun : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            var pc = FindFirstObjectByType<PlayerCtl>();
            GameClock.Set(21.8f); GameClock.Speed = 6f; // a game hour every 12.5 s: long enough for people to walk home
            int lastHour = -1; float t0 = Time.time; bool shot = false;
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots"); System.IO.Directory.CreateDirectory(dir);
            while (Time.time - t0 < 80f)
            {
                int hr = (int)GameClock.Hour;
                if (hr != lastHour)
                {
                    lastHour = hr; int out_ = 0; foreach (var r in FindObjectsByType<Resident>(FindObjectsSortMode.None)) out_++;
                    Debug.Log($"[ASBO] nighttest {GameClock.Text} residents out={out_} night={GameClock.Night}");
                }
                if (!shot && hr == 23) { shot = true; ScreenCapture.CaptureScreenshot(dir + "/night.png"); }
                if (hr == 0 && GameClock.Minute > 30f) { GameClock.Set(7.6f); }
                if (hr == 9) break;
                yield return null;
            }
            GameClock.Speed = 1f;
            // the tag: walk to the edge of the zone and out
            Vector3 p = pc.transform.position; var z = Tag.Zone;
            pc.Teleport(new Vector3(z.xMax - 6f, p.y + 0.5f, Mathf.Clamp(p.z, z.yMin + 10f, z.yMax - 10f)), 90f, 10f);
            yield return new WaitForSeconds(3f);
            Debug.Log($"[ASBO] nighttest tag near edge: {Tag.HudLine}");
            pc.Teleport(new Vector3(z.xMax + 3f, p.y + 0.5f, Mathf.Clamp(p.z, z.yMin + 10f, z.yMax - 10f)), 90f, 10f);
            yield return new WaitForSeconds(3f);
            Debug.Log($"[ASBO] nighttest tag outside: {Tag.HudLine} outside={Tag.Outside}");
            pc.Teleport(p + Vector3.up * 0.5f, 0f, 10f);
            Destroy(gameObject);
        }
    }

    /// <summary>Hit someone hard, then film the whole ambulance call-out from the side (Shots/amb, every half second) and log each stage.</summary>
    public class AmbTestRun : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            var pc = FindFirstObjectByType<PlayerCtl>();
            DebugCmd.HardHit();
            yield return new WaitForSeconds(2.5f);
            Person victim = Person.LastHit; if (victim == null) { Debug.Log("[ASBO] ambtest: no victim"); Destroy(gameObject); yield break; }
            Vector3 at = victim.BodyPos; pc.Teleport(at + new Vector3(0, 0.5f, 9f), 180f, 10f);
            var cg = new GameObject("AmbCam"); var cam = cg.AddComponent<Camera>(); cam.depth = 50; cam.fieldOfView = 55f; World.HideHud = true;
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/amb"); System.IO.Directory.CreateDirectory(dir);
            foreach (var f in System.IO.Directory.GetFiles(dir, "*.png")) { try { System.IO.File.Delete(f); } catch { } }
            string last = ""; int n = 0; float t0 = Time.time;
            while (Time.time - t0 < 95f)
            {
                Vector3 focus = at; var off = new Vector3(-3f, 4.2f, 9.5f);
                if (Ambulance.All.Count > 0)
                {
                    // from behind the ambulance and off to one side: the back doors, the crew and the casualty in one frame
                    var am = Ambulance.All[0].transform; Vector3 rear = am.position - am.forward * Ambulance.All[0].HalfLength;
                    focus = Vector3.Lerp(rear, at, 0.4f); off = -am.forward * 7.5f + Vector3.up * 3.4f + Vector3.forward * 6.5f;
                }
                cg.transform.position = focus + off; cg.transform.LookAt(focus + Vector3.up * 0.8f);
                if (Ambulance.Status != last) { last = Ambulance.Status; Debug.Log($"[ASBO] ambtest t={Time.time - t0:F1} {last}"); }
                ScreenCapture.CaptureScreenshot(dir + "/a_" + (n++).ToString("000") + ".png");
                if (last.StartsWith("En route") && Ambulance.All.Count == 0) break;
                yield return new WaitForSeconds(1f);
            }
            Destroy(cg); World.HideHud = false; Debug.Log($"[ASBO] ambtest end frames={n}");
            Destroy(gameObject);
        }
    }

    public class EstateShotsRun : MonoBehaviour
    {
        public int set = -1;
        System.Collections.IEnumerator Start()
        {
            var cg = new GameObject("EstateCam"); var cam = cg.AddComponent<Camera>(); cam.depth = 50; cam.fieldOfView = 60f; cam.farClipPlane = 600f; World.HideHud = true;
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots"); System.IO.Directory.CreateDirectory(dir);
            var views = new (Vector3 from, Vector3 at)[]
            {
                (new Vector3(-4f, 1.7f, -46f), new Vector3(16f, 14f, -72f)),
                (new Vector3(0f, 48f, -18f), new Vector3(0f, 0f, -86f)),
                (new Vector3(-3f, 1.7f, -66f), new Vector3(-15f, 0.6f, -56f)),
                (new Vector3(-14f, 1.7f, -82f), new Vector3(-15f, 1.2f, -95f)),
                (new Vector3(-1.5f, 1.7f, -67f), new Vector3(6f, 2.6f, -72.5f)),
                (new Vector3(96f, 1.7f, -32f), new Vector3(124f, 7f, -68f)),
                (new Vector3(124f, 42f, -28f), new Vector3(124f, 0f, -72f)),
                (new Vector3(106f, 1.7f, -48f), new Vector3(130f, 5f, -74f)),
            };
            string prefix = "estate_";
            if (set == 2)
            {
                prefix = "place_";
                views = new (Vector3 from, Vector3 at)[]
                {
                    (new Vector3(-146f, 1.7f, -33f), new Vector3(-172f, 20f, -112f)),
                    (new Vector3(-152f, 20.5f, -95f), new Vector3(-163f, 21f, -106f)),
                    (new Vector3(-140f, 62f, -80f), new Vector3(-175f, 16f, -118f)),
                    (new Vector3(-167f, 19.8f, -112f), new Vector3(-178f, 24f, -124f)),
                };
            }
            if (set == 3)
            {
                prefix = "place_";
                views = new (Vector3 from, Vector3 at)[]
                {
                    (new Vector3(-56f, 1.7f, -30f), new Vector3(-90f, 4f, -17f)),
                    (new Vector3(-47f, 1.7f, -27.5f), new Vector3(-58f, 4f, -17f)),
                    (new Vector3(-90f, 1.7f, -30f), new Vector3(-108f, 3.5f, -42f)),
                    (new Vector3(-60f, 34f, -50f), new Vector3(-92f, 0f, -22f)),
                    (new Vector3(-43f, 1.7f, -48f), new Vector3(-33f, 3f, -62f)),
                    (new Vector3(-120f, 1.7f, -30f), new Vector3(-140f, 3.5f, -20f)),
                };
            }
            for (int i = 0; i < views.Length; i++)
            {
                cg.transform.position = views[i].from; cg.transform.LookAt(views[i].at);
                yield return new WaitForSeconds(0.6f);
                ScreenCapture.CaptureScreenshot(dir + "/" + prefix + i + ".png");
                yield return new WaitForSeconds(0.3f);
            }
            Destroy(cg); World.HideHud = false; Debug.Log("[ASBO] estate shots done");
            Destroy(gameObject);
        }
    }

    public class StickTestRun : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            var set = UnityEngine.InputSystem.InputSystem.settings;
            var oldEd = set.editorInputBehaviorInPlayMode; var oldBg = set.backgroundBehavior;
            set.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            set.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            var ts = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>("TestTouch");
            var pc = FindFirstObjectByType<PlayerCtl>();
            if (pc != null) pc.Teleport(new Vector3(-40f, 0.15f, World.RoadZ - 9f), 90f);
            yield return new WaitForSeconds(0.6f);
            Vector2 p0 = new Vector2(Screen.width * 0.18f, Screen.height * 0.3f);
            void T(int id, UnityEngine.InputSystem.TouchPhase ph, Vector2 at) =>
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(ts, new UnityEngine.InputSystem.LowLevel.TouchState { touchId = id, phase = ph, position = at, pressure = 1f });
            T(7, UnityEngine.InputSystem.TouchPhase.Began, p0);
            yield return null; yield return null;
            Debug.Log($"[ASBO] sticktest down: active={FloatStick.Active} value={FloatStick.Value} screen={Screen.width}x{Screen.height}");
            {
                var t0 = ts.touches[0]; Debug.Log($"[ASBO] sticktest device: added={ts.added} enabled={ts.enabled} current={(UnityEngine.InputSystem.Touchscreen.current == ts)} touch0 id={t0.touchId.ReadValue()} pressed={t0.press.isPressed} pos={t0.position.ReadValue()} phase={t0.phase.ReadValue()}");
                var es = UnityEngine.EventSystems.EventSystem.current;
                var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                if (es != null) es.RaycastAll(new UnityEngine.EventSystems.PointerEventData(es) { position = p0 }, hits);
                var ui = es != null ? es.currentInputModule as UnityEngine.InputSystem.UI.InputSystemUIInputModule : null;
                Debug.Log($"[ASBO] sticktest ui: module={(es != null && es.currentInputModule != null ? es.currentInputModule.GetType().Name : "none")} point={(ui != null && ui.point != null ? string.Join(",", ui.point.action.bindings.Select(bb => bb.effectivePath)) : "-")} click={(ui != null && ui.leftClick != null ? string.Join(",", ui.leftClick.action.bindings.Select(bb => bb.effectivePath)) : "-")} under finger: {string.Join(" > ", hits.Select(h => h.gameObject.name))}");
            }
            Vector3 pos0 = pc != null ? pc.transform.position : Vector3.zero;
            for (int i = 1; i <= 12; i++)
            {
                Vector2 at = p0 + new Vector2(Mathf.Min(1f, i / 4f) * Screen.height * 0.08f, 0f);
                T(7, UnityEngine.InputSystem.TouchPhase.Moved, at);
                if (i == 5) T(8, UnityEngine.InputSystem.TouchPhase.Began, new Vector2(Screen.width * 0.8f, Screen.height * 0.5f)); // a second finger, looking
                if (i > 5 && i < 10) T(8, UnityEngine.InputSystem.TouchPhase.Moved, new Vector2(Screen.width * 0.8f + i * 12f, Screen.height * 0.5f));
                if (i == 10) T(8, UnityEngine.InputSystem.TouchPhase.Ended, new Vector2(Screen.width * 0.8f + 120f, Screen.height * 0.5f));
                yield return new WaitForSeconds(0.1f);
                if (i % 2 == 0) Debug.Log($"[ASBO] sticktest t={i * 0.1f:F1}s active={FloatStick.Active} value=({FloatStick.Value.x:F2},{FloatStick.Value.y:F2}) moved={(pc != null ? (pc.transform.position - pos0).WithY(0).magnitude : 0f):F2} m");
            }
            T(7, UnityEngine.InputSystem.TouchPhase.Ended, p0);
            yield return null; yield return null;
            Debug.Log($"[ASBO] sticktest up: active={FloatStick.Active} value={FloatStick.Value}");
            UnityEngine.InputSystem.InputSystem.RemoveDevice(ts);
            set.editorInputBehaviorInPlayMode = oldEd; set.backgroundBehavior = oldBg;
            Destroy(gameObject);
        }
    }

    public class CamTestRun : MonoBehaviour
    {
        public string id;
        System.Collections.IEnumerator Start()
        {
            var pc = FindFirstObjectByType<PlayerCtl>(); var cam = Camera.main;
            if (pc == null || cam == null) { Debug.Log("[ASBO] camtest: no player/camera"); yield break; }
            var traffic = Object.FindObjectsByType<TrafficCar>(FindObjectsSortMode.None); foreach (var tc in traffic) tc.gameObject.SetActive(false);
            Vector3 start = new Vector3(-150f, 0.25f, World.RoadZ - 2.1f);
            var car = PlayerCar.Create(start, 90f, new Color(0.5f, 0.1f, 0.1f), id, CarKit.Wear.Clean, 3);
            yield return new WaitForSeconds(0.8f);
            pc.Teleport(car.transform.position - car.transform.right * 2.3f, 90f);
            yield return new WaitForSeconds(0.3f);
            pc.UseCar();
            yield return new WaitForSeconds(1.5f);
            var rb = car.GetComponent<Rigidbody>();
            var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            Debug.Log($"[ASBO] camtest {id}: inCar={pc.InCar} physical={car.Physical} interp={rb?.interpolation} fixedDt={Time.fixedDeltaTime} fps cap={Application.targetFrameRate} brain={brain?.UpdateMethod} view={PlayerCtl.CarView}");
            TouchCar.Clear(); TouchCar.fwd = true;
            float t0 = Time.time, nextLog = 1f; Vector3 lastVp = Vector3.zero; bool have = false;
            float sumJ = 0f, maxJ = 0f; int nJ = 0; float distMin = 99f, distMax = 0f;
            while (Time.time - t0 < 10f && car.transform.position.x < 150f)
            {
                yield return new WaitForEndOfFrame();
                Vector3 vp = cam.WorldToViewportPoint(car.transform.position + Vector3.up * 0.7f);
                if (have) { float j = ((Vector2)(vp - lastVp)).magnitude * 1000f; sumJ += j; nJ++; maxJ = Mathf.Max(maxJ, j); }
                lastVp = vp; have = true;
                float dist = (cam.transform.position - car.transform.position).WithY(0).magnitude; distMin = Mathf.Min(distMin, dist); distMax = Mathf.Max(distMax, dist);
                if (Time.time - t0 >= nextLog)
                {
                    Debug.Log($"[ASBO] camtest t={Time.time - t0:F0}s v={car.SignedSpeed * 3.6f:F0} km/h camera {distMin:F1}-{distMax:F1} m behind, car on screen moves {sumJ / Mathf.Max(1, nJ):F1}/1000 per frame (worst {maxJ:F1}), screen pos ({vp.x:F2},{vp.y:F2}) interp={rb?.interpolation}");
                    sumJ = 0f; maxJ = 0f; nJ = 0; distMin = 99f; distMax = 0f; nextLog += 1f;
                    if (Mathf.Abs(nextLog - 7f) < 0.01f) DebugCmd.Shot("camtest_speed");
                }
            }
            TouchCar.Clear(); TouchCar.brake = true; yield return new WaitForSeconds(2.5f); TouchCar.Clear();
            pc.UseCar(); yield return new WaitForSeconds(1.2f);
            foreach (var tc in traffic) if (tc != null) tc.gameObject.SetActive(true);
            Debug.Log("[ASBO] camtest done");
            Destroy(gameObject);
        }
    }

    public class PerfRunner : MonoBehaviour
    {
        public string id; int fixedSteps;
        void FixedUpdate() { fixedSteps++; }
        static readonly (Unity.Profiling.ProfilerCategory cat, string name)[] Marks =
        {
            (Unity.Profiling.ProfilerCategory.Internal, "Main Thread"),
            (Unity.Profiling.ProfilerCategory.Internal, "PlayerLoop"),
            (Unity.Profiling.ProfilerCategory.Physics, "FixedUpdate.PhysicsFixedUpdate"),
            (Unity.Profiling.ProfilerCategory.Scripts, "FixedUpdate.ScriptRunBehaviourFixedUpdate"),
            (Unity.Profiling.ProfilerCategory.Scripts, "Update.ScriptRunBehaviourUpdate"),
            (Unity.Profiling.ProfilerCategory.Scripts, "PreLateUpdate.ScriptRunBehaviourLateUpdate"),
            (Unity.Profiling.ProfilerCategory.Animation, "PreLateUpdate.DirectorUpdateAnimationBegin"),
            (Unity.Profiling.ProfilerCategory.Animation, "PreLateUpdate.DirectorUpdateAnimationEnd"),
            (Unity.Profiling.ProfilerCategory.Render, "PostLateUpdate.FinishFrameRendering"),
            (Unity.Profiling.ProfilerCategory.Render, "Gfx.WaitForPresentOnGfxThread"),
            (Unity.Profiling.ProfilerCategory.Ai, "AI.Navigation"),
            (Unity.Profiling.ProfilerCategory.Audio, "PostLateUpdate.UpdateAudio"),
            (Unity.Profiling.ProfilerCategory.Render, "GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU"),
            (Unity.Profiling.ProfilerCategory.Render, "RenderLoop.DrawSRPBatcher"),
            (Unity.Profiling.ProfilerCategory.Particles, "PreLateUpdate.ParticleSystemBeginUpdateAll"),
            (Unity.Profiling.ProfilerCategory.Scripts, "Update.ScriptRunDelayedDynamicFrameRate"),
            (Unity.Profiling.ProfilerCategory.Render, "Inl_UniversalRenderPipeline.RenderSingleCameraInternal"),
        };
        System.Collections.IEnumerator Measure(string label, float secs)
        {
            var recs = Marks.Select(m => Unity.Profiling.ProfilerRecorder.StartNew(m.cat, m.name, 400)).ToArray();
            var gc = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, "GC.Alloc", 400);
            var dc = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Draw Calls Count");
            var tris = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Triangles Count");
            float t0 = Time.realtimeSinceStartup; int f0 = Time.frameCount; fixedSteps = 0; float worst = 0f;
            while (Time.realtimeSinceStartup - t0 < secs) { yield return null; worst = Mathf.Max(worst, Time.unscaledDeltaTime); }
            int frames = Mathf.Max(1, Time.frameCount - f0); float real = Time.realtimeSinceStartup - t0;
            var parts = new System.Collections.Generic.List<string>();
            for (int i = 0; i < recs.Length; i++)
            {
                var r = recs[i]; if (!r.Valid || r.Count == 0) { parts.Add($"{Marks[i].name}=-"); r.Dispose(); continue; }
                double sum = 0; int n = Mathf.Min(r.Count, r.Capacity); for (int k = 0; k < n; k++) sum += r.GetSample(k).Value; parts.Add($"{Marks[i].name.Replace("PreLateUpdate.", "").Replace("PostLateUpdate.", "").Replace("FixedUpdate.", "F.").Replace("Update.", "")}={sum / n * 1e-6:F1}"); r.Dispose();
            }
            Debug.Log($"[ASBO] perf {label}: {frames / real:F0} fps ({real * 1000f / frames:F1} ms, worst {worst * 1000f:F0}), physics steps/frame {fixedSteps / (float)frames:F2}, draws {dc.LastValue} tris {tris.LastValue / 1000}k, GC {(gc.Valid && gc.Count > 0 ? gc.LastValue / 1024 : -1)} KB/frame; ms: {string.Join(" ", parts)}");
            gc.Dispose(); dc.Dispose(); tris.Dispose();
        }
        /// <summary>Every timer Unity has, for a few seconds: the 30 biggest (parents and children both show).</summary>
        System.Collections.IEnumerator Top(string label, float secs)
        {
            var hs = new System.Collections.Generic.List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle>();
            Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetAvailable(hs);
            var recs = new System.Collections.Generic.List<(string n, Unity.Profiling.ProfilerRecorder r)>();
            foreach (var h in hs)
            {
                var d = Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetDescription(h);
                if (d.UnitType != Unity.Profiling.ProfilerMarkerDataUnit.TimeNanoseconds) continue;
                recs.Add((d.Name, new Unity.Profiling.ProfilerRecorder(d.Category, d.Name, 300)));
            }
            foreach (var x in recs) x.r.Start();
            float t0 = Time.realtimeSinceStartup; int f0 = Time.frameCount;
            while (Time.realtimeSinceStartup - t0 < secs) yield return null;
            int frames = Mathf.Max(1, Time.frameCount - f0);
            var res = new System.Collections.Generic.List<(string n, double ms)>();
            foreach (var x in recs)
            {
                var r = x.r; double sum = 0; int n = r.Valid ? Mathf.Min(r.Count, r.Capacity) : 0;
                for (int k = 0; k < n; k++) sum += r.GetSample(k).Value;
                if (n > 0) res.Add((x.n, sum * 1e-6 / frames)); r.Dispose();
            }
            Debug.Log($"[ASBO] perf top {label} ({recs.Count} timers, {frames} frames): " + string.Join(" | ", res.OrderByDescending(q => q.ms).Take(40).Select(q => $"{q.n} {q.ms:F1}")));
        }

        System.Collections.IEnumerator Start()
        {
            var pc = FindFirstObjectByType<PlayerCtl>();
            if (id == "hay")
            {
                // the held punch, each version in turn, filmed from the side against a stand-in opponent
                var foe = World.MakePerson(pc.transform.position + Vector3.forward * 1.1f, new Color(0.3f, 0.5f, 0.3f), new Color(0.2f, 0.2f, 0.2f), "Pedestrian");
                foe.route.Add(foe.transform.position); foe.speed = 0.01f; foe.transform.rotation = Quaternion.Euler(0, 180f, 0);
                pc.Teleport(new Vector3(-70f, 0.15f, 50f), 0f, 4f); foe.transform.position = new Vector3(-70f, 0f, 51.1f);
                yield return new WaitForSeconds(4f);
                var pa = pc.GetComponent<Person>().anim;
                var camGo = new GameObject("HayCam"); var cam = camGo.AddComponent<Camera>(); cam.depth = 60; cam.fieldOfView = 40f;
                camGo.transform.position = new Vector3(-66.2f, 1.3f, 50.5f); camGo.transform.LookAt(new Vector3(-70f, 1.1f, 50.5f));
                for (int v = 0; v < 3; v++)
                {
                    pa.SetFloat("AVar", v); pa.SetTrigger("Hay");
                    float t0 = Time.time; int f = 0; float[] at = { 0.12f, 0.3f, 0.5f, 0.75f };
                    while (f < at.Length) { yield return null; if (Time.time - t0 >= at[f]) { DebugCmd.Shot($"hay_{v}_{f}"); f++; } }
                    yield return new WaitForSeconds(1.2f);
                }
                Destroy(camGo); Destroy(foe.gameObject);
                Debug.Log("[ASBO] hay filmed"); Debug.Log("[ASBO] perf done"); Destroy(gameObject); yield break;
            }
            if (id == "carhit")
            {
                // a car at town speed into someone standing in the road, filmed from the side: limp and tumbling, or upright?
                foreach (var t in TrafficCar.All.ToArray()) if (t != null) t.gameObject.SetActive(false);
                var lane = TrafficNet.Lanes.FirstOrDefault(l => l.road == 0 && l.dir.x > 0.5f && l.pts[0].x < -100f && l.len > 60f) ?? TrafficNet.Lanes[0];
                Vector3 at = lane.At(40f, out var tan);
                var victim = World.MakePerson(at, new Color(0.6f, 0.2f, 0.2f), new Color(0.2f, 0.2f, 0.3f), "Pedestrian"); victim.route.Add(at); victim.speed = 0.01f;
                yield return new WaitForSeconds(3.5f); // their body builds
                pc.Teleport(at + Vector3.forward * -9f + tan * 2f, 0f, 6f); yield return new WaitForSeconds(0.5f);
                var hitter = TrafficCar.Spawn(World.Root, lane, 4f, new System.Random(4)); hitter.cruise = 13f; hitter.testIgnorePeople = true;
                float t0 = Time.time; bool hit = false; float hitT = 0f; var log = new System.Collections.Generic.List<string>(); int shot = 0;
                var head = victim.anim != null && victim.anim.isHuman ? victim.anim.GetBoneTransform(HumanBodyBones.Head) : null;
                while (Time.time - t0 < 9f)
                {
                    yield return new WaitForSeconds(0.1f);
                    if (!hit && victim.state != Person.State.Walking) { hit = true; hitT = Time.time; }
                    if (hit)
                    {
                        float dt = Time.time - hitT;
                        if (head != null && log.Count < 26) log.Add($"{dt:F1}s pelvis {victim.BodyPos.y:F2} head {head.position.y:F2} {victim.state}");
                        if (shot < 5 && dt > new[] { 0.1f, 0.35f, 0.7f, 1.2f, 2.5f }[shot]) { DebugCmd.Shot("carhit_" + shot); shot++; }
                        if (dt > 3f) break;
                    }
                }
                Debug.Log($"[ASBO] car hit: hit={hit} car {hitter.Speed * 3.6f:F0} km/h: " + string.Join(" | ", log));
                foreach (var t in TrafficCar.All.ToArray()) if (t != null) t.gameObject.SetActive(true);
                Debug.Log("[ASBO] perf done"); Destroy(gameObject); yield break;
            }
            if (id == "doors")
            {
                // walk the player through every ground floor door of the Eggy Duck: who gets through, what stops them
                var cc = pc.GetComponent<CharacterController>(); var rows = new System.Collections.Generic.List<string>(); int ok = 0, n = 0;
                foreach (var D in TudorManor.Plan.doors)
                {
                    if (D.lv != 0) continue;
                    bool X = D.line == "X"; Vector3 c = TudorManor.Origin + (X ? new Vector3(D.at, 0.15f, (D.a + D.b) * 0.5f) : new Vector3((D.a + D.b) * 0.5f, 0.15f, D.at)); Vector3 nrm = X ? Vector3.right : Vector3.forward;
                    foreach (int dir in new[] { 1, -1 })
                    {
                        n++; Vector3 st = c - nrm * dir * 1.3f;
                        pc.Teleport(st, Quaternion.LookRotation(nrm * dir).eulerAngles.y); yield return null; yield return null;
                        for (float t = 0f; t < 1.3f; t += Time.deltaTime) { cc.Move(nrm * dir * 2.2f * Time.deltaTime + Vector3.down * 0.5f * Time.deltaTime); yield return null; }
                        float got = Vector3.Dot(pc.transform.position - c, nrm * dir);
                        bool through = got > 0.4f; if (through) ok++;
                        string why = "";
                        if (!through)
                        {
                            var hits = Physics.CapsuleCastAll(st + Vector3.up * 0.4f, st + Vector3.up * 1.5f, 0.28f, nrm * dir, 2.6f, ~((1 << 2) | (1 << Person.RagdollLayer)), QueryTriggerInteraction.Ignore);
                            why = string.Join(",", hits.Where(h => h.collider.GetComponentInParent<PlayerCtl>() == null).Select(h => h.collider.name + "@" + h.distance.ToString("F1") + (h.collider is MeshCollider ? "(mesh)" : "")));
                        }
                        rows.Add($"{D.kind} {D.line}{D.at:F1} [{D.a:F1}-{D.b:F1}] h{D.hgt:F2} dir{dir}: {(through ? "through" : "BLOCKED at " + got.ToString("F2") + " by " + why)}");
                    }
                }
                Debug.Log($"[ASBO] pub doors: {ok}/{n} passable: " + string.Join(" | ", rows));
                Debug.Log("[ASBO] perf done"); Destroy(gameObject); yield break;
            }
            if (id == "traffic")
            {
                // half a minute of traffic: do any two cars end up inside each other, does anyone get stuck for good
                float t0 = Time.time; int overlaps = 0; float minGap = 99f; var still = new System.Collections.Generic.Dictionary<TrafficCar, float>(); int samples = 0, moving = 0;
                while (Time.time - t0 < 30f)
                {
                    yield return new WaitForSeconds(0.2f); samples++;
                    var cars = TrafficCar.All.Where(c => c != null && c.isActiveAndEnabled).ToList();
                    for (int i = 0; i < cars.Count; i++)
                    {
                        if (cars[i].Speed > 0.5f) moving++;
                        if (cars[i].Speed < 0.2f) { still.TryGetValue(cars[i], out float st); still[cars[i]] = st + 0.2f; } else still[cars[i]] = 0f;
                        for (int j = i + 1; j < cars.Count; j++)
                        {
                            Vector3 d = cars[j].transform.position - cars[i].transform.position; float along = Mathf.Abs(Vector3.Dot(d, cars[i].transform.forward)), side = Mathf.Abs(Vector3.Dot(d, cars[i].transform.right));
                            if (side < 1.6f) { minGap = Mathf.Min(minGap, along); if (along < 3.2f) overlaps++; }
                        }
                    }
                }
                int stuck = still.Count(kv => kv.Value > 20f);
                Debug.Log($"[ASBO] traffic test: {TrafficCar.All.Count} cars, closest nose-to-tail {minGap:F1} m between centres, overlapping {overlaps} times, {stuck} stuck over 20 s, {moving * 100 / Mathf.Max(1, samples * TrafficCar.All.Count)}% of the time moving; lanes {TrafficNet.Lanes.Count}, signals {JunctionSignal.All.Count}");
                foreach (var kv in still.Where(kv => kv.Value > 20f).Take(4)) Debug.Log($"[ASBO] traffic stuck at {kv.Key.transform.position}");
                Debug.Log("[ASBO] perf done"); Destroy(gameObject); yield break;
            }
            if (id == "lod")
            {
                pc.Teleport(new Vector3(0f, 0.15f, -18f), 0f, 8f); yield return new WaitForSeconds(14f);
                var cam = Camera.main.transform.position; var rows = new System.Collections.Generic.List<(float d, string s)>(); long total = 0;
                foreach (var p in Person.All)
                {
                    if (p == null || p.isPlayer) continue;
                    var lod = p.GetComponentInChildren<UMA.Examples.UMASimpleLOD>(); long t = 0;
                    foreach (var r in p.GetComponentsInChildren<SkinnedMeshRenderer>()) if (r.sharedMesh != null) for (int k = 0; k < r.sharedMesh.subMeshCount; k++) t += r.sharedMesh.GetIndexCount(k) / 3;
                    total += t; float d = Vector3.Distance(cam, p.transform.position);
                    rows.Add((d, $"{d:F0}m L{(lod != null ? lod.CurrentLOD : -1)} {t / 1000}k"));
                }
                Debug.Log($"[ASBO] perf people lod: {rows.Count} people, {total / 1000}k triangles in all: " + string.Join(", ", rows.OrderBy(r => r.d).Select(r => r.s)));
                // one far person in detail: does each slot have LOD levels, and what's in the mesh now
                var far = Person.All.Where(q => q != null && !q.isPlayer).OrderByDescending(q => Vector3.Distance(cam, q.transform.position)).FirstOrDefault();
                var ud = far != null ? far.GetComponentInChildren<UMA.UMAData>() : null;
                if (ud != null && ud.umaRecipe != null)
                {
                    var parts = new System.Collections.Generic.List<string>();
                    foreach (var sd in ud.umaRecipe.slotDataList)
                    {
                        if (sd == null || sd.asset == null || sd.asset.meshData == null || sd.asset.meshData.submeshes == null) continue;
                        int sub = Mathf.Clamp(sd.asset.subMeshIndex, 0, sd.asset.meshData.submeshes.Length - 1); var smt = sd.asset.meshData.submeshes[sub];
                        int lc = 0; try { lc = smt.LODCount(); } catch { }
                        string counts = ""; for (int l = 0; l < lc && l < 5; l++) { try { counts += smt.GetTriangleCount(l) + "/"; } catch { counts += "?/"; } }
                        parts.Add($"{sd.slotName}: lods {lc} [{counts}] ranges {(smt.lodRanges != null ? smt.lodRanges.Count : -1)}");
                    }
                    var lodc = far.GetComponentInChildren<UMA.Examples.UMASimpleLOD>();
                    Debug.Log($"[ASBO] perf lod detail {far.name} at L{(lodc != null ? lodc.CurrentLOD : -1)} useInternal {(lodc != null && lodc.useInternalMeshLOD)}: " + string.Join(" | ", parts));
                }
                Debug.Log("[ASBO] perf done"); Destroy(gameObject); yield break;
            }
            if (id.StartsWith("views"))
            {
                // the perf gate: the same viewpoints every time, at the phone quality level
                int mobile = System.Array.IndexOf(QualitySettings.names, "Mobile"); int was = QualitySettings.GetQualityLevel();
                if (mobile >= 0) QualitySettings.SetQualityLevel(mobile, true);
                var views = new (string n, Vector3 p, float yaw)[] { ("estate", new Vector3(5f, 0.15f, -48f), 180f), ("high street", new Vector3(-50f, 0.15f, -30f), 270f), ("cottages", new Vector3(0f, 0.15f, -18f), 0f),
                    ("road east", new Vector3(40f, 0.15f, -33f), 90f), ("site", new Vector3(106f, 0.15f, -36f), 180f), ("castle lane", new Vector3(-140f, 0.15f, -60f), 225f),
                    ("kestrel", new Vector3(-175f, 0.15f, 66f), 270f), ("thornbury", new Vector3(140f, 0.15f, 28f), 60f), ("precinct", new Vector3(20f, 0.2f, 86f), 0f),
                    ("northway", new Vector3(20f, 0.15f, 122f), 90f), ("hospital", new Vector3(10f, 0.15f, 134f), 0f), ("mill lane", new Vector3(200f, 0.15f, -50f), 180f) };
                var names = new[] { "Batches Count", "SetPass Calls Count", "Draw Calls Count", "Triangles Count", "Shadow Casters Count", "Visible Skinned Meshes Count" };
                var rows = new System.Collections.Generic.List<string>();
                foreach (var v in views)
                {
                    pc.Teleport(v.p, v.yaw, 8f); yield return new WaitForSeconds(2.2f);
                    var recs = names.Select(nm => Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, nm, 200)).ToArray();
                    float t0 = Time.realtimeSinceStartup; int f0 = Time.frameCount; float worst = 0f;
                    while (Time.realtimeSinceStartup - t0 < 2f) { yield return null; worst = Mathf.Max(worst, Time.unscaledDeltaTime); }
                    float ms = (Time.realtimeSinceStartup - t0) * 1000f / Mathf.Max(1, Time.frameCount - f0);
                    var vals = recs.Select(r => { long sum = 0; int c = 0; int k = Mathf.Min(r.Count, r.Capacity); for (int i = 0; i < k; i++) { long x = r.GetSample(i).Value; if (x > 0) { sum += x; c++; } } r.Dispose(); return c > 0 ? sum / c : 0; }).ToArray();
                    rows.Add($"{v.n}: {1000f / ms:F0}fps {ms:F1}ms worst {worst * 1000f:F0} | batches {vals[0]} setpass {vals[1]} draws {vals[2]} tris {vals[3] / 1000}k casters {vals[4]} skinned {vals[5]}");
                    if (id == "views shots") DebugCmd.Shot("view_" + v.n.Replace(' ', '_'));
                }
                Debug.Log("[ASBO] perf views (phone quality): " + string.Join(" || ", rows));
                if (mobile >= 0) QualitySettings.SetQualityLevel(was, true);
                Debug.Log("[ASBO] perf done"); Destroy(gameObject); yield break;
            }
            if (id == "shaders")
            {
                pc.Teleport(new Vector3(-150f, 0.15f, World.RoadZ - 6f), 90f);
                yield return new WaitForSeconds(12f);
                var smr = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None).Where(r => r.enabled).ToArray();
                var one = smr.FirstOrDefault(r => r.GetComponentInParent<PlayerCtl>() == null);
                if (one != null)
                {
                    var mesh = one.sharedMesh;
                    for (int i = 0; i < one.sharedMaterials.Length; i++)
                    {
                        var m = one.sharedMaterials[i]; if (m == null) continue;
                        var texs = m.GetTexturePropertyNames().Select(n => (n, t: m.GetTexture(n))).Where(x => x.t != null).Select(x => $"{x.n}={x.t.width}x{x.t.height}");
                        Debug.Log($"[ASBO] perf mat {i}: {m.name} shader {m.shader.name} passes {m.passCount} queue {m.renderQueue} sub tris {(mesh != null && i < mesh.subMeshCount ? mesh.GetIndexCount(i) / 3 : -1)} keywords [{string.Join(" ", m.shaderKeywords)}] tex [{string.Join(" ", texs)}] floats [{string.Join(" ", new[] { "_Surface", "_AlphaClip", "_Cutoff", "_Cull", "_Smoothness", "_Metallic" }.Where(m.HasProperty).Select(n => n + "=" + m.GetFloat(n).ToString("F2")))}]");
                    }
                }
                var all = smr.Select(r => r.sharedMaterials).ToArray();
                var plain = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                yield return Measure("people as built", 2.5f);
                foreach (var key in new[] { "Hair", "Skin", "UMA3_Lit", "Universal" })
                {
                    foreach (var r in smr) r.sharedMaterials = r.sharedMaterials.Select(m => m != null && m.shader.name.Contains(key) ? plain : m).ToArray();
                    yield return new WaitForSeconds(0.5f);
                    yield return Measure("plain " + key, 2.5f);
                    for (int i = 0; i < smr.Length; i++) smr[i].sharedMaterials = all[i];
                    yield return new WaitForSeconds(0.3f);
                }
                Debug.Log("[ASBO] perf done"); Destroy(gameObject); yield break;
            }
            if (id == "skin")
            {
                pc.Teleport(new Vector3(-150f, 0.15f, World.RoadZ - 6f), 90f);
                yield return new WaitForSeconds(12f);
                var smr = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None).Where(r => r.enabled).ToArray();
                long tri = 0; int bs = 0, offscreen = 0, quality4 = 0; var shaders = new System.Collections.Generic.Dictionary<string, int>();
                foreach (var r in smr)
                {
                    if (r.sharedMesh != null) { for (int k = 0; k < r.sharedMesh.subMeshCount; k++) tri += r.sharedMesh.GetIndexCount(k) / 3; bs += r.sharedMesh.blendShapeCount; }
                    if (r.updateWhenOffscreen) offscreen++; if (r.quality == SkinQuality.Bone4 || r.quality == SkinQuality.Auto) quality4++;
                    foreach (var m in r.sharedMaterials) if (m != null) { string n = m.shader.name; shaders[n] = shaders.TryGetValue(n, out var c) ? c + 1 : 1; }
                }
                Debug.Log($"[ASBO] perf skin: {smr.Length} skinned, {tri / 1000}k tris, blendshapes {bs}, updateWhenOffscreen {offscreen}, shaders: {string.Join(", ", shaders.Select(kv => kv.Key + " x" + kv.Value))}; gpu skinning {SystemInfo.supportsComputeShaders} quality skinWeights {QualitySettings.skinWeights}");
                yield return Measure("all people drawn", 2.5f);
                foreach (var r in smr) r.enabled = false;
                yield return new WaitForSeconds(0.5f);
                yield return Measure("people hidden", 2.5f);
                var modes = smr.Select(r => r.shadowCastingMode).ToArray();
                foreach (var r in smr) { r.enabled = true; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
                yield return new WaitForSeconds(0.5f);
                yield return Measure("people, no shadows", 2.5f);
                for (int i = 0; i < smr.Length; i++) smr[i].shadowCastingMode = modes[i];
                var mats = smr.Select(r => r.sharedMaterials).ToArray();
                var plain = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                foreach (var r in smr) r.sharedMaterials = Enumerable.Repeat(plain, r.sharedMaterials.Length).ToArray();
                yield return new WaitForSeconds(0.5f);
                yield return Measure("people, plain material", 2.5f);
                for (int i = 0; i < smr.Length; i++) smr[i].sharedMaterials = mats[i];
                yield return new WaitForSeconds(0.5f);
                yield return Measure("people back", 2.5f);
                Debug.Log("[ASBO] perf done"); Destroy(gameObject); yield break;
            }
            if (id == "time")
            {
                pc.Teleport(new Vector3(-150f, 0.15f, World.RoadZ - 6f), 90f);
                for (int i = 0; i < 9; i++)
                {
                    var ls = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.isActiveAndEnabled).ToArray();
                    var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.isActiveAndEnabled).ToArray();
                    var pss = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
                    var probes = Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Count(r => r.isActiveAndEnabled && r.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime);
                    Debug.Log($"[ASBO] perf state t={Time.timeSinceLevelLoad:F0}: hour {GameClock.Hour:F2}, lights {ls.Length} (shadowed {ls.Count(l => l.shadows != LightShadows.None)}: {string.Join(",", ls.Where(l => l.shadows != LightShadows.None).Take(6).Select(l => l.name + ":" + l.type))}), cameras {cams.Length} ({string.Join(",", cams.Select(c => c.name + (c.targetTexture != null ? "[rt]" : "")))}), particles {pss.Sum(p => p.particleCount)} in {pss.Count(p => p.isPlaying)} playing, realtime probes {probes}, people {Object.FindObjectsByType<Person>(FindObjectsSortMode.None).Length}, skinned {Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None).Count(r => r.isVisible)} visible, renderers visible {Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Count(r => r.isVisible)}");
                    yield return Measure("standing " + i, 2f);
                }
                Debug.Log("[ASBO] perf done"); Destroy(gameObject); yield break;
            }
            if (id == "cost")
            {
                pc.Teleport(new Vector3(-150f, 0.15f, World.RoadZ - 6f), 90f); yield return new WaitForSeconds(2f);
                yield return Measure("nothing", 2.5f);
                foreach (var (model, rccOn) in new[] { ("hatch90", true), ("hatch90", false), ("supermini", true), ("supermini", false) })
                {
                    CarRig.Off = !rccOn;
                    var c = PlayerCar.Create(new Vector3(-150f, 0.25f, World.RoadZ - 2.1f), 90f, new Color(0.5f, 0.1f, 0.1f), model, CarKit.Wear.Clean, 3);
                    CarRig.Off = false;
                    yield return new WaitForSeconds(1.5f);
                    long tri = 0; int rends = 0; foreach (var mf in c.GetComponentsInChildren<MeshFilter>(true)) { if (mf.sharedMesh == null) continue; rends++; for (int sm = 0; sm < mf.sharedMesh.subMeshCount; sm++) tri += mf.sharedMesh.GetIndexCount(sm) / 3; }
                    var ps = c.GetComponentsInChildren<ParticleSystem>(true);
                    Debug.Log($"[ASBO] perf car {model} {(c.Physical ? "RCC" : "arcade")}: {rends} meshes {tri / 1000f:F1}k tris, particles {ps.Length} systems ({ps.Sum(p => p.particleCount)} alive, max {ps.Sum(p => p.main.maxParticles)}, colliding {ps.Count(p => p.collision.enabled)}), audio {c.GetComponentsInChildren<AudioSource>(true).Count(a => a.isPlaying)}/{c.GetComponentsInChildren<AudioSource>(true).Length}, lights {c.GetComponentsInChildren<Light>(true).Count(l => l.isActiveAndEnabled)}, behaviours {c.GetComponentsInChildren<MonoBehaviour>(true).Count(b => b.isActiveAndEnabled)}");
                    yield return Measure(model + (c.Physical ? " RCC" : " arcade"), 2.5f);
                    Destroy(c.gameObject); yield return new WaitForSeconds(1f);
                }
                yield return Measure("nothing again", 2.5f);
                Debug.Log("[ASBO] perf done"); Destroy(gameObject); yield break;
            }
            if (id == "body")
            {
                // what one person is made of: renderers, submeshes, materials, vertices
                Person best = null; float bd = 1e9f;
                foreach (var q in Person.All) { if (q == null || q.isPlayer) continue; float d = (q.transform.position - pc.transform.position).sqrMagnitude; if (d < bd) { bd = d; best = q; } }
                if (best != null)
                    foreach (var r in best.GetComponentsInChildren<Renderer>(true))
                    {
                        Mesh m = r is SkinnedMeshRenderer sk ? sk.sharedMesh : r.TryGetComponent<MeshFilter>(out var mf) ? mf.sharedMesh : null;
                        Debug.Log($"[ASBO] body {best.name}: {r.GetType().Name} '{r.name}' on={r.enabled} {(m != null ? m.vertexCount + " verts, " + m.subMeshCount + " submeshes" : "no mesh")}, mats [{string.Join(", ", r.sharedMaterials.Select(x => x != null ? x.name + "/" + x.shader.name : "null"))}] shadows {r.shadowCastingMode}");
                    }
                Debug.Log("[ASBO] perf done"); Destroy(gameObject); yield break;
            }
            if (id == "drop")
            {
                // the real cost of each part of town at the busiest views: draw calls with it, and with it switched off
                var dc = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Draw Calls Count");
                var sc = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Shadow Casters Count");
                foreach (var (n, p, yaw) in new[] { ("road east", new Vector3(40f, 0.15f, -33f), 90f), ("thornbury", new Vector3(140f, 0.15f, 28f), 60f), ("cottages", new Vector3(0f, 0.15f, -18f), 0f) })
                {
                    pc.Teleport(p, yaw, 8f); yield return new WaitForSeconds(2f);
                    long Avg(Unity.Profiling.ProfilerRecorder r) => r.Valid ? r.LastValue : -1;
                    long all = Avg(dc), allS = Avg(sc); var rows = new System.Collections.Generic.List<string>();
                    var world = World.Root; if (world == null) break;
                    var kids = new System.Collections.Generic.List<Transform>(); foreach (Transform k in world) if (k.gameObject.activeSelf) kids.Add(k);
                    var extra = new System.Collections.Generic.List<GameObject>(); foreach (var tc in TrafficCar.All) if (tc != null) extra.Add(tc.gameObject);
                    foreach (var k in kids)
                    {
                        k.gameObject.SetActive(false); yield return null; yield return null; yield return null;
                        long d = Avg(dc), s2 = Avg(sc); k.gameObject.SetActive(true);
                        if (all - d > 8) rows.Add($"{k.name} {all - d} ({allS - s2} casters)");
                    }
                    foreach (var go in extra) go.SetActive(false); yield return null; yield return null; yield return null; long dt = Avg(dc); foreach (var go in extra) if (go != null) go.SetActive(true);
                    rows.Add($"(traffic) {all - dt}");
                    Debug.Log($"[ASBO] drop at {n}: {all} draws, {allS} casters; parts: " + string.Join(" | ", rows.OrderByDescending(x => x)));
                    yield return null;
                }
                dc.Dispose(); sc.Dispose();
                Debug.Log("[ASBO] perf done"); Destroy(gameObject); yield break;
            }
            if (id == "who")
            {
                // what's being drawn at the busiest views: visible renderers grouped by what they belong to (submeshes = draws before batching)
                foreach (var (n, p, yaw) in new[] { ("road east", new Vector3(40f, 0.15f, -33f), 90f), ("thornbury", new Vector3(140f, 0.15f, 28f), 60f), ("cottages", new Vector3(0f, 0.15f, -18f), 0f) })
                {
                    pc.Teleport(p, yaw, 8f); yield return new WaitForSeconds(2f);
                    var groups = new System.Collections.Generic.Dictionary<string, (int draws, int casters, long tris)>();
                    foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    {
                        if (!r.enabled || !r.isVisible || r is ParticleSystemRenderer) continue;
                        var t = r.transform; string key = t.name;
                        while (t.parent != null && t.parent.parent != null && t.parent.name != "World") { t = t.parent; key = t.name; }
                        if (r.GetComponentInParent<TrafficCar>() != null) key = "(traffic cars)"; else if (r.GetComponentInParent<Person>() != null) key = "(people)"; else if (r.GetComponentInParent<ParkedCar>() != null || key.StartsWith("Parked") || key.StartsWith("Wreck")) key = "(parked cars)";
                        Mesh mesh = r is SkinnedMeshRenderer smr ? smr.sharedMesh : r.TryGetComponent<MeshFilter>(out var mf) ? mf.sharedMesh : null;
                        int sm = mesh != null ? mesh.subMeshCount : 1; long tri = 0; if (mesh != null) for (int q = 0; q < mesh.subMeshCount; q++) tri += mesh.GetIndexCount(q) / 3;
                        groups.TryGetValue(key, out var g); groups[key] = (g.draws + sm, g.casters + (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off ? sm : 0), g.tris + tri);
                    }
                    Debug.Log($"[ASBO] who draws at {n}: " + string.Join(" | ", groups.OrderByDescending(kv => kv.Value.draws).Take(14).Select(kv => $"{kv.Key} {kv.Value.draws} ({kv.Value.casters} cast, {kv.Value.tris / 1000}k)")));
                }
                Debug.Log("[ASBO] perf done"); Destroy(gameObject); yield break;
            }
            if (id.StartsWith("top"))
            {
                string model = id.Length > 4 ? id.Substring(4) : "hatch90";
                pc.Teleport(new Vector3(-150f, 0.15f, World.RoadZ - 6f), 90f); yield return new WaitForSeconds(1.5f);
                var c = PlayerCar.Create(new Vector3(-150f, 0.25f, World.RoadZ - 2.1f), 90f, new Color(0.5f, 0.1f, 0.1f), model, CarKit.Wear.Clean, 3);
                yield return new WaitForSeconds(1.5f);
                pc.Teleport(c.transform.position - c.transform.right * 2.3f, 90f); yield return new WaitForSeconds(0.3f);
                pc.UseCar(); yield return new WaitForSeconds(1.5f);
                yield return Top("sat in " + model, 2f);
                pc.UseCar(); yield return new WaitForSeconds(1.5f); Destroy(c.gameObject);
                Debug.Log("[ASBO] perf done"); Destroy(gameObject); yield break;
            }
            Debug.Log($"[ASBO] perf counts: people {Object.FindObjectsByType<Person>(FindObjectsSortMode.None).Length}, rigidbodies {Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length} (awake {Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Count(r => !r.IsSleeping() && !r.isKinematic)}), colliders {Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length}, wheels {Object.FindObjectsByType<WheelCollider>(FindObjectsSortMode.None).Length}, dents {Object.FindObjectsByType<RMD_Deformation>(FindObjectsSortMode.None).Length}, audio {Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Count(a => a.isPlaying)} playing, lights {Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l => l.isActiveAndEnabled)}, animators {Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Count(a => a.isActiveAndEnabled)}, cars {PlayerCar.All.Count}/{ParkedCar.All.Count}");
            pc.Teleport(new Vector3(-150f, 0.15f, World.RoadZ - 6f), 90f);
            yield return new WaitForSeconds(1.5f);
            yield return Measure("on foot", 3f);
            var car = PlayerCar.Create(new Vector3(-150f, 0.25f, World.RoadZ - 2.1f), 90f, new Color(0.5f, 0.1f, 0.1f), id, CarKit.Wear.Clean, 3);
            yield return new WaitForSeconds(1f);
            yield return Measure("rcc car parked beside", 3f);
            pc.Teleport(car.transform.position - car.transform.right * 2.3f, 90f); yield return new WaitForSeconds(0.3f);
            pc.UseCar(); yield return new WaitForSeconds(1.5f);
            yield return Measure("sat in car", 3f);
            TouchCar.Clear(); TouchCar.fwd = true;
            yield return new WaitForSeconds(1f);
            yield return Measure("driving", 4f);
            TouchCar.Clear(); TouchCar.brake = true; yield return new WaitForSeconds(2f); TouchCar.Clear();
            pc.UseCar(); yield return new WaitForSeconds(1.5f);
            Debug.Log("[ASBO] perf done"); Destroy(gameObject);
        }
    }
}


namespace ASBO.Slice
{
    public class MoveTestRun : MonoBehaviour
    {
        Camera cam; RenderTexture rt; Texture2D tex; string dir; int n; PlayerCtl pc;
        void Shot(string tag)
        {
            Vector3 p = pc.transform.position, r = pc.transform.right;
            cam.transform.position = p + r * 3.6f + Vector3.up * 1.1f; cam.transform.LookAt(p + Vector3.up * 0.9f);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 480), 0, 0); tex.Apply(); RenderTexture.active = null;
            System.IO.File.WriteAllBytes($"{dir}/m_{n++:D2}_{tag}.png", tex.EncodeToPNG());
        }
        System.Collections.IEnumerator Start()
        {
            pc = FindFirstObjectByType<PlayerCtl>(); if (pc == null) { Destroy(gameObject); yield break; }
            dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/moves"); System.IO.Directory.CreateDirectory(dir);
            var cg = new GameObject("MoveCam"); cam = cg.AddComponent<Camera>(); cam.fieldOfView = 50f; cam.nearClipPlane = 0.1f;
            rt = new RenderTexture(640, 480, 24); cam.targetTexture = rt; cam.enabled = false; tex = new Texture2D(640, 480, TextureFormat.RGB24, false);
            GameClock.Set(13f);
            pc.Teleport(new Vector3(-118f, 0.1f, 88f), 90f, 8f); yield return new WaitForSeconds(1.5f);
            Shot("stand");
            PlayerCtl.jumpTap = true; float t0 = Time.time;
            for (int i = 0; i < 9; i++) { yield return new WaitForSeconds(0.07f); Shot("jump" + (Time.time - t0).ToString("F2")); }
            yield return new WaitForSeconds(0.8f);
            PlayerCtl.crouchTap = true; yield return new WaitForSeconds(0.25f); Shot("crouchdown"); yield return new WaitForSeconds(0.6f); Shot("crouched");
            PlayerCtl.TestInput = new Vector2(0f, 1f);
            for (int i = 0; i < 6; i++) { yield return new WaitForSeconds(0.18f); Shot("sneak"); }
            PlayerCtl.TestInput = null; yield return new WaitForSeconds(0.6f);
            PlayerCtl.crouchTap = true; yield return new WaitForSeconds(0.25f); Shot("standup"); yield return new WaitForSeconds(0.8f);
            pc.Teleport(new Vector3(-138f, 0.1f, 88f), 90f, 8f); yield return new WaitForSeconds(0.8f);
            PlayerCtl.TestInput = new Vector2(0f, 1f); yield return new WaitForSeconds(1.6f); Shot("run"); PlayerCtl.TestInput = null;
            for (int i = 0; i < 4; i++) { yield return new WaitForSeconds(0.15f); Shot("stop"); }
            // the casino's seated punters, a few moments apart
            pc.Teleport(CasinoHall.Origin + new Vector3(10.3f, 0.2f, 6.2f), 250f, 10f); yield return new WaitForSeconds(2.5f);
            for (int i = 0; i < 4; i++) { ScreenCapture.CaptureScreenshot($"{dir}/m_{n++:D2}_casino.png"); yield return new WaitForSeconds(1.2f); }
            Debug.Log("[ASBO] move test done");
            cam.targetTexture = null; Destroy(rt); Destroy(cg); Destroy(gameObject);
        }
    }
}


namespace ASBO.Slice
{
    public class JackTestRun : MonoBehaviour
    {
        Camera cam; RenderTexture rt; Texture2D tex; string dir; int n;
        void Shot(Vector3 from, Vector3 at, string tag)
        {
            cam.transform.position = from; cam.transform.LookAt(at);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 480), 0, 0); tex.Apply(); RenderTexture.active = null;
            System.IO.File.WriteAllBytes($"{dir}/j_{n++:D2}_{tag}.png", tex.EncodeToPNG());
        }
        System.Collections.IEnumerator Start()
        {
            var pc = FindFirstObjectByType<PlayerCtl>(); if (pc == null) { Destroy(gameObject); yield break; }
            dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/moves"); System.IO.Directory.CreateDirectory(dir);
            var cg = new GameObject("JackCam"); cam = cg.AddComponent<Camera>(); cam.fieldOfView = 55f; cam.nearClipPlane = 0.1f;
            rt = new RenderTexture(640, 480, 24); cam.targetTexture = rt; cam.enabled = false; tex = new Texture2D(640, 480, TextureFormat.RGB24, false);
            GameClock.Set(13f);
            TrafficCar tc = null; float bd = 1e9f;
            foreach (var c in TrafficCar.All) { if (c == null || string.IsNullOrEmpty(c.id) || c.Wrecked) continue; float d = (c.transform.position - pc.transform.position).sqrMagnitude; if (d < bd) { bd = d; tc = c; } }
            if (tc == null) { Debug.Log("[ASBO] jack test: no traffic"); Destroy(gameObject); yield break; }
            tc.Hijack(); yield return new WaitForSeconds(0.3f);
            Vector3 door = tc.DriverDoor, outw = tc.transform.right, fwd = tc.transform.forward;
            pc.Teleport(door + outw * 0.8f, Mathf.Atan2(-outw.x, -outw.z) * Mathf.Rad2Deg, 8f); yield return new WaitForSeconds(0.6f);
            Vector3 camFrom = door + fwd * 5f + outw * 2.5f + Vector3.up * 1.5f, camAt = door + Vector3.up * 0.9f;
            Shot(camFrom, camAt, "before");
            pc.UseCar();
            for (int i = 0; i < 12; i++) { yield return new WaitForSeconds(0.3f); Shot(camFrom, camAt, "jack"); }
            for (float w = 0; w < 5f && !pc.InCar; w += 0.25f) yield return new WaitForSeconds(0.25f);
            Debug.Log($"[ASBO] jack test: in car {pc.InCar}");
            yield return new WaitForSeconds(1f);
            pc.UseCar(); yield return new WaitForSeconds(1.5f);
            // the driver on the floor: stamp
            Person drv = pc.LastCarjacked;
            Debug.Log($"[ASBO] jack test: driver {(drv != null ? drv.state.ToString() : "none")}");
            if (drv != null)
            {
                if (drv.state == Person.State.Walking) drv.Knockout(Vector3.right, 6f, true);
                yield return new WaitForSeconds(1.2f);
                Vector3 h = drv.HeadPos; Vector3 side = Vector3.Cross(Vector3.up, (h - drv.BodyPos).WithY(0).normalized); if (side.sqrMagnitude < 0.1f) side = Vector3.right;
                pc.Teleport(h + side * 1.0f, Mathf.Atan2(-side.x, -side.z) * Mathf.Rad2Deg, 8f); yield return new WaitForSeconds(0.5f);
                Vector3 sFrom = h + side * 1f + Vector3.Cross(side, Vector3.up) * 3.2f + Vector3.up * 1.2f;
                var f = pc.GetComponent<Fighter>();
                if (f != null) f.PlayerInput(false, true, false, false, false, true);
                for (int i = 0; i < 6; i++) { Shot(sFrom, h + Vector3.up * 0.5f, "stamp"); yield return new WaitForSeconds(0.22f); }
            }
            Debug.Log("[ASBO] jack test done");
            cam.targetTexture = null; Destroy(rt); Destroy(cg); Destroy(gameObject);
        }
    }
}


namespace ASBO.Slice
{
    public class TraverseTestRun : MonoBehaviour
    {
        Camera cam; RenderTexture rt; Texture2D tex; string dir; int n; PlayerCtl pc; Transform root;
        const float Y = 200f; public static int Only; // 0 = every test, else just that one (1 climb .. 6 reach)
        bool Do(int k) => Only == 0 || Only == k;
        void Shot(string tag)
        {
            Vector3 p = pc.transform.position, r = pc.transform.right;
            cam.transform.position = p + r * 4.2f + Vector3.up * 1.3f - pc.transform.forward * 0.6f; cam.transform.LookAt(p + Vector3.up * 1.0f);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 480, 360), 0, 0); tex.Apply(); RenderTexture.active = null;
            System.IO.File.WriteAllBytes($"{dir}/t_{n++:D2}_{tag}.png", tex.EncodeToPNG());
        }
        void Box(string name, Vector3 min, Vector3 max, Color c)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(root, false);
            g.transform.position = (min + max) * 0.5f; g.transform.localScale = max - min;
            var m = new Material(Gfx.Lit); m.color = c; g.GetComponent<Renderer>().sharedMaterial = m;
        }
        float Ahead(float faceX) => faceX - pc.transform.position.x;
        System.Collections.IEnumerator Start()
        {
            pc = FindFirstObjectByType<PlayerCtl>(); if (pc == null) { Destroy(gameObject); yield break; }
            dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots/moves"); System.IO.Directory.CreateDirectory(dir);
            var cg = new GameObject("TravCam"); cam = cg.AddComponent<Camera>(); cam.fieldOfView = 55f; cam.nearClipPlane = 0.1f;
            rt = new RenderTexture(480, 360, 24); cam.targetTexture = rt; cam.enabled = false; tex = new Texture2D(480, 360, TextureFormat.RGB24, false);
            GameClock.Set(13f);
            root = new GameObject("TraverseSlab").transform;
            Box("Slab", new Vector3(-4f, Y - 1f, -4f), new Vector3(32f, Y, 34f), new Color(0.45f, 0.45f, 0.47f));
            Box("ClimbBox", new Vector3(5f, Y, -1.5f), new Vector3(7f, Y + 1.6f, 1.5f), new Color(0.6f, 0.4f, 0.3f));
            Box("HighWall", new Vector3(14f, Y, 4.5f), new Vector3(16f, Y + 2.5f, 7.5f), new Color(0.55f, 0.35f, 0.3f));
            Box("Fence", new Vector3(10f, Y, 10f), new Vector3(10.08f, Y + 1.0f, 14f), new Color(0.3f, 0.25f, 0.2f));
            Box("Tower", new Vector3(-3f, Y, 16.5f), new Vector3(6f, Y + 4f, 19.5f), new Color(0.5f, 0.5f, 0.55f));
            yield return null;
            // 1) chest-high climb
            if (Do(1)) { pc.Teleport(new Vector3(4.3f, Y + 0.05f, 0f), 90f, 8f); yield return new WaitForSeconds(1.2f);
            Shot("climb0"); PlayerCtl.jumpTap = true;
            for (int i = 0; i < 12; i++) { yield return new WaitForSeconds(0.22f); Shot("climb"); }
            Debug.Log($"[ASBO] trav: climb ended at y {pc.transform.position.y - Y:F2} x {pc.transform.position.x:F2}"); }
            // 2) run-up wall climb at a sprint
            if (Do(2)) { pc.Teleport(new Vector3(2f, Y + 0.05f, 6f), 90f, 8f); yield return new WaitForSeconds(1.2f);
            PlayerCtl.TestInput = new Vector2(0f, 1f); PlayerCtl.TestSprint = true;
            for (float w = 0; w < 6f && Ahead(14f) > 1.3f; w += Time.deltaTime) yield return null;
            PlayerCtl.jumpTap = true; PlayerCtl.TestInput = null; PlayerCtl.TestSprint = false;
            for (int i = 0; i < 10; i++) { yield return new WaitForSeconds(0.16f); Shot("wall"); }
            yield return new WaitForSeconds(0.5f);
            Debug.Log($"[ASBO] trav: wall climb ended at y {pc.transform.position.y - Y:F2} x {pc.transform.position.x:F2}"); }
            // 3) vault a fence at a run
            if (Do(3)) { pc.Teleport(new Vector3(4f, Y + 0.05f, 12f), 90f, 8f); yield return new WaitForSeconds(1.2f);
            PlayerCtl.TestInput = new Vector2(0f, 1f);
            for (float w = 0; w < 5f && Ahead(10f) > 0.85f; w += Time.deltaTime) yield return null;
            PlayerCtl.jumpTap = true;
            for (int i = 0; i < 7; i++) { yield return new WaitForSeconds(0.1f); Shot("vault"); }
            PlayerCtl.TestInput = null; yield return new WaitForSeconds(0.4f);
            Debug.Log($"[ASBO] trav: vault ended at x {pc.transform.position.x:F2} (fence at 10)"); }
            // 4) run off a 4 m drop: the roll
            if (Do(4)) { pc.Teleport(new Vector3(1f, Y + 4.05f, 18f), 90f, 8f); yield return new WaitForSeconds(1.2f);
            PlayerCtl.TestInput = new Vector2(0f, 1f);
            for (float w = 0; w < 4f && pc.transform.position.y > Y + 3.5f; w += Time.deltaTime) yield return null;
            PlayerCtl.TestInput = null;
            for (int i = 0; i < 18; i++) { yield return new WaitForSeconds(0.12f); Shot("roll"); } }
            // 5) running into someone
            Person npc = null;
            if (Do(5)) { npc = World.MakePerson(new Vector3(9f, Y + 0.05f, 24f), new Color(0.7f, 0.2f, 0.2f), new Color(0.2f, 0.2f, 0.25f), "Barged");
            npc.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            pc.Teleport(new Vector3(2f, Y + 0.05f, 24f), 90f, 8f);
            for (float w = 0; w < 6f && !npc.Ready; w += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(0.6f);
            PlayerCtl.TestInput = new Vector2(0f, 1f);
            for (float w = 0; w < 4f && Ahead(9f) > 1.6f; w += Time.deltaTime) yield return null;
            for (int i = 0; i < 9; i++) { yield return new WaitForSeconds(0.12f); Shot("barge"); }
            PlayerCtl.TestInput = null; yield return new WaitForSeconds(0.6f); }
            // 6) reaching for something on a table, then on the floor
            if (Do(6)) { pc.Teleport(new Vector3(4f, Y + 0.05f, 30f), 90f, 8f); yield return new WaitForSeconds(1.0f);
            pc.ReachFor(pc.transform.position + Vector3.right * 0.6f + Vector3.up * 0.8f);
            for (int i = 0; i < 5; i++) { yield return new WaitForSeconds(0.2f); Shot("take"); }
            yield return new WaitForSeconds(0.8f);
            pc.ReachFor(pc.transform.position + Vector3.right * 0.5f);
            for (int i = 0; i < 4; i++) { yield return new WaitForSeconds(0.2f); Shot("floor"); } }
            Debug.Log("[ASBO] trav test done");
            if (npc != null) Destroy(npc.gameObject);
            cam.targetTexture = null; Destroy(rt); Destroy(cg); Destroy(root.gameObject); Destroy(gameObject);
        }
    }
}
