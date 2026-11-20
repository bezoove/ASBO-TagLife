using UnityEngine;
using UnityEngine.InputSystem;

namespace ASBO.Slice
{
    /// <summary>Simple drivable car: rigid body with four wheel colliders.</summary>
    public class Car : MonoBehaviour, IVehicle
    {
        public bool occupied;
        public Rigidbody rb;
        WheelCollider[] wc = new WheelCollider[4];
        Transform[] wv = new Transform[4];
        public Transform seatPoint, exitPoint;
        public float Speed => rb != null ? rb.linearVelocity.magnitude : 0f;
        public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
        Light[] head = new Light[2];

        public static Car Create(Transform parent, Vector3 pos, float yaw, Color body)
        {
            var g = new GameObject("Car");
            g.transform.SetParent(parent, false); g.transform.position = pos; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var car = g.AddComponent<Car>();
            car.Build(body);
            return car;
        }

        void Build(Color bodyColor)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 1300f; rb.centerOfMass = new Vector3(0, -0.15f, 0.1f); rb.linearDamping = 0.02f; rb.angularDamping = 0.3f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // collision shape
            var bc = gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 0.78f, 0); bc.size = new Vector3(1.78f, 0.72f, 4.4f);
            var cc2 = gameObject.AddComponent<BoxCollider>(); cc2.center = new Vector3(0, 1.25f, -0.15f); cc2.size = new Vector3(1.5f, 0.42f, 2.0f);
            // lofted saloon body (visual root dropped to sit on the wheels)
            var vis = new GameObject("Visual").transform; vis.SetParent(transform, false); vis.localPosition = new Vector3(0, -0.06f, 0);
            var wheelVis = CarBody.Build(vis, CarBody.Saloon, bodyColor);
            foreach (int s in new[] { -1, 1 })
            {
                var l = new GameObject("Head").AddComponent<Light>(); l.transform.SetParent(transform, false);
                l.transform.localPosition = new Vector3(s * 0.65f, 0.65f, 2.25f); l.type = LightType.Spot; l.range = 28f; l.spotAngle = 55f; l.intensity = 6f;
                head[(s + 1) / 2] = l;
            }
            float[] xs = { -0.88f, 0.88f, -0.88f, 0.88f }, zs = { 1.35f, 1.35f, -1.35f, -1.35f };
            for (int i = 0; i < 4; i++)
            {
                var w = new GameObject("WC" + i); w.transform.SetParent(transform, false); w.transform.localPosition = new Vector3(xs[i], 0.5f, zs[i]);
                var c = w.AddComponent<WheelCollider>();
                c.radius = 0.31f; c.suspensionDistance = 0.22f; c.mass = 20f;
                c.suspensionSpring = new JointSpring { spring = 38000f, damper = 4800f, targetPosition = 0.5f };
                var f = c.forwardFriction; f.stiffness = 2.2f; c.forwardFriction = f;
                var sf = c.sidewaysFriction; sf.stiffness = 2.4f; c.sidewaysFriction = sf;
                wc[i] = c;
                wv[i] = wheelVis[i];
            }
            gameObject.AddComponent<EngineAudio>();
            seatPoint = new GameObject("Seat").transform; seatPoint.SetParent(transform, false); seatPoint.localPosition = new Vector3(-0.4f, 0.9f, 0.1f);
            exitPoint = new GameObject("Exit").transform; exitPoint.SetParent(transform, false); exitPoint.localPosition = new Vector3(-1.9f, 0.2f, 0.1f);
        }

        void FixedUpdate()
        {
            float thr = 0, steer = 0; bool brake = false;
            if (occupied && Keyboard.current != null)
            {
                var k = Keyboard.current;
                thr = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);
                steer = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
                brake = k.spaceKey.isPressed;
            }
            float fwd = Vector3.Dot(rb.linearVelocity, transform.forward);
            float maxSteer = Mathf.Lerp(30f, 8f, Mathf.Clamp01(Speed / 28f));
            for (int i = 0; i < 4; i++)
            {
                if (i < 2) wc[i].steerAngle = Mathf.Lerp(wc[i].steerAngle, steer * maxSteer, 8f * Time.fixedDeltaTime);
                float torque = 0, br = 0;
                if (thr != 0f)
                {
                    bool against = fwd * thr < -0.5f;
                    if (against) br = 4000f; else if (Speed < 34f) torque = thr * (thr > 0 ? 1900f : 1000f);
                }
                if (brake) br = 5500f;
                if (!occupied) br = 1800f;
                if (i >= 2) wc[i].motorTorque = torque; else wc[i].motorTorque = torque * 0.15f;
                wc[i].brakeTorque = br;
            }
            // keep the car tracking where it points: damp sideways slide a little (arcade stability)
            Vector3 lv = transform.InverseTransformDirection(rb.linearVelocity);
            rb.AddForce(-transform.right * lv.x * rb.mass * 1.5f);
            rb.AddTorque(-transform.up * rb.angularVelocity.y * rb.mass * 0.6f * (thr == 0 && steer == 0 ? 1f : 0.15f));
            // downforce and anti-roll help
            rb.AddForce(-transform.up * Speed * 12f);
        }

        void Update()
        {
            for (int i = 0; i < 4; i++)
            {
                wc[i].GetWorldPose(out var p, out var q);
                wv[i].position = p - transform.up * 0.0f; wv[i].rotation = q * Quaternion.Euler(0, 0, 90f);
            }
        }

        public void LightsOn(bool on) { foreach (var l in head) l.enabled = on; }
    }
}
