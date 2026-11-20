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
