using UnityEngine;

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
}
