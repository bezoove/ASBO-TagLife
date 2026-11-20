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
        }
        public float Speed => rb != null ? rb.linearVelocity.magnitude : 0f;
        public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
        public RCCP_CarController Car => GetComponent<RCCP_CarController>();
    }

}
