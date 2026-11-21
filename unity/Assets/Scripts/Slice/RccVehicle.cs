using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>Lets the game's people react to the RCC car like any other vehicle.</summary>
    public class RccVehicle : MonoBehaviour, IVehicle
    {
        Rigidbody rb;
        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            // RCC Lite's exhaust smoke throws every frame under URP in this Unity version: switch it off for now
            foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true)) if (mb != null && mb.GetType().Name == "RCCP_Exhaust") { mb.enabled = false; mb.gameObject.SetActive(false); }
            // ...and so do its skidmarks (NullReference inside RCCP_Skidmarks.AddSkidMark): no skid marks until that's sorted
            foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null || mb.GetType().Name != "RCCP_WheelCollider") continue;
                var t = mb.GetType(); const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                var f = t.GetField("drawSkid", F); if (f != null && f.FieldType == typeof(bool)) { f.SetValue(mb, false); continue; }
                var pr = t.GetProperty("drawSkid", F); if (pr != null && pr.CanWrite) pr.SetValue(mb, false);
            }
        }
        public float Speed => rb != null ? rb.linearVelocity.magnitude : 0f;
        public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
        public RCCP_CarController Car => GetComponent<RCCP_CarController>();
    }

}
