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
        public static void KnockTest() { new GameObject("KnockTest").AddComponent<KnockTestRun>(); }
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
        public float secs = 16f; public bool kickFinish = true;
        System.Collections.IEnumerator Start()
        {
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
            float t0 = Time.time, nextShot = 0f, nextPunch = 0.6f; int n = 0, throws = 0;
            while (Time.time - t0 < secs)
            {
                float t = Time.time - t0;
                Vector3 mid = (pc.transform.position + foe.transform.position) * 0.5f;
                if (fp.state != Person.State.Walking) mid = (pc.transform.position + fp.BodyPos) * 0.5f;
                Vector3 side = Vector3.Cross(Vector3.up, (foe.transform.position - pc.transform.position).WithY(0).normalized); if (side.sqrMagnitude < 0.1f) side = Vector3.right;
                cg.transform.position = Vector3.Lerp(cg.transform.position == Vector3.zero ? mid + side * 4f + Vector3.up * 1.4f : cg.transform.position, mid + side * 4f + Vector3.up * 1.4f, n == 0 ? 1f : 0.1f);
                cg.transform.LookAt(mid + Vector3.up * 1.0f);
                if (t >= nextPunch && fp.state == Person.State.Walking)
                {
                    nextPunch = t + 0.55f;
                    Vector3 d = (foe.transform.position - pc.transform.position).WithY(0);
                    if (d.magnitude > 1.15f) { pc.transform.position += d.normalized * Mathf.Min(0.4f, d.magnitude - 1f); }
                    if (kickFinish && throws == 4) me.Attack(Fighter.Move.Kick); else me.PunchButton();
                    throws++;
                    Debug.Log($"[ASBO] brawltest t={t:F1} throw {throws} foeHP={ff.health:F0} myHP={me.health:F0} foe={fp.state}");
                }
                if (t >= nextShot) { ScreenCapture.CaptureScreenshot(dir + "/b_" + (n++).ToString("000") + ".png"); nextShot = t + 0.1f; }
                yield return null;
            }
            Debug.Log($"[ASBO] brawltest end: foe={fp.state} hurt={fp.hurt} foeHP={ff.health:F0} myHP={me.health:F0} frames={n}");
            Destroy(cg); World.HideHud = false; Destroy(gameObject);
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
}
