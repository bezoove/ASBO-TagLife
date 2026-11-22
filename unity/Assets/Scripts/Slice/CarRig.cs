using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Real car physics for the cars you drive (Realistic Car Controller): proper suspension, tyres that grip and let
    /// go, weight transfer, gears. Each car is RCC's drivetrain (stripped of its demo body, made by ASBO/Cars/Make RCC
    /// Rig) wearing one of our own bodies: its wheels moved to where our wheels are, our body sat on them, and the
    /// engine, weight and driving style set for that kind of car (CarHandling). Without the rig in the build the
    /// arcade car is used instead.
    /// </summary>
    public static class CarRig
    {
        static GameObject basePrefab; static bool looked, configured;
        const float GamePull = 2.4f;

        public static bool Ready
        {
            get
            {
                if (!looked) { looked = true; basePrefab = Resources.Load<GameObject>("CarRig/RccBase"); if (basePrefab == null) Debug.Log("[ASBO] no RCC rig in the build: arcade cars"); }
                return basePrefab != null;
            }
        }

        /// <summary>RCC's global settings, set once: keep our physics step and frame rate, our touch controls, styles on.</summary>
        static void Configure()
        {
            if (configured) return; configured = true;
            var s = RCCP_Settings.Instance; if (s == null) return;
            s.overrideFixedTimeStep = false; s.overrideFPS = false;  // our timestep and frame cap (phones)
            s.mobileControllerEnabled = false;                        // our own touch buttons feed it
            s.overrideBehavior = true;                                // driving styles apply
            s.useTelemetry = false; s.useInputDebugger = false;
            var sm = RCCP_SceneManager.Instance; if (sm != null) { sm.registerLastVehicleAsPlayer = false; sm.disableUIWhenNoPlayerVehicle = true; }
        }

        /// <summary>An inactive copy of the rig (under an inactive holder) at pos, ready for a body. Call Finish when built.</summary>
        public static GameObject Spawn(Vector3 pos, float yaw, Transform parent)
        {
            if (!Ready) return null;
            Configure();
            var holder = new GameObject("CarRigHolder"); holder.SetActive(false);
            var g = Object.Instantiate(basePrefab, holder.transform);
            g.name = "PlayerCar"; g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            return g;
        }

        public static void Finish(GameObject g, Transform parent)
        {
            var holder = g.transform.parent;
            g.transform.SetParent(parent, true);
            if (holder != null && holder.name == "CarRigHolder") Object.Destroy(holder.gameObject);
            g.SetActive(true);
        }

        struct WheelPair { public RCCP_WheelCollider wc; public Transform model; public bool front, left; }

        /// <summary>
        /// Put our body on the rig: rig wheels moved to our wheel positions (keeping RCC's collider-to-wheel offset), our
        /// wheels hung on RCC's wheel pivots so they spin, steer and ride the springs, our body lined up on them, a body
        /// collider that slides along walls, and the handling for this kind of car.
        /// </summary>
        public static bool Fit(GameObject g, Transform vis, Transform[] wheels, CarBody.Spec spec, CarHandling h)
        {
            var pairs = new List<WheelPair>();
            foreach (var ax in g.GetComponentsInChildren<RCCP_Axle>(true))
            {
                if (ax.leftWheelCollider != null) pairs.Add(new WheelPair { wc = ax.leftWheelCollider, model = ax.leftWheelModel != null ? ax.leftWheelModel : ax.leftWheelCollider.wheelModel, left = true });
                if (ax.rightWheelCollider != null) pairs.Add(new WheelPair { wc = ax.rightWheelCollider, model = ax.rightWheelModel != null ? ax.rightWheelModel : ax.rightWheelCollider.wheelModel, left = false });
            }
            if (pairs.Count < 4 || pairs.Any(p => p.model == null) || wheels == null || wheels.Length < 4) { Debug.LogWarning("[ASBO] RCC rig: wheels not found"); return false; }
            var root = g.transform;
            float maxZ = pairs.Max(p => root.InverseTransformPoint(p.model.position).z), minZ = pairs.Min(p => root.InverseTransformPoint(p.model.position).z);
            for (int i = 0; i < pairs.Count; i++) { var p = pairs[i]; float z = root.InverseTransformPoint(p.model.position).z; p.front = z > (maxZ + minZ) * 0.5f; p.left = root.InverseTransformPoint(p.model.position).x < 0f; pairs[i] = p; }
            var uwc0 = pairs[0].wc.GetComponent<WheelCollider>();
            float protoR = uwc0 != null ? uwc0.radius : 0.33f;
            float protoGround = pairs.Average(p => root.InverseTransformPoint(p.model.position).y) - protoR;
            float protoMid = (maxZ + minZ) * 0.5f;
            // our body: ground level with RCC's, axles centred where RCC's are (so its centre of mass sits right)
            Vector3 fl = root.InverseTransformPoint(wheels[0].position), rl = root.InverseTransformPoint(wheels[2].position);
            float rad = WheelRadius(wheels[0], spec.m.rad);
            float ourMid = (fl.z + rl.z) * 0.5f, ourGround = fl.y - rad;
            vis.localPosition += new Vector3(0f, protoGround - ourGround, protoMid - ourMid);
            // RCC's wheels to ours
            for (int i = 0; i < pairs.Count; i++)
            {
                var p = pairs[i];
                int k = (p.front ? 0 : 2) + (p.left ? 0 : 1); // FL FR RL RR
                var ours = wheels[k];
                Vector3 off = p.wc.transform.position - p.model.position; // RCC's collider sits this far from its wheel
                p.model.position = ours.position;
                p.wc.transform.position = ours.position + off;
                var uwc = p.wc.GetComponent<WheelCollider>(); if (uwc != null) uwc.radius = rad;
                ours.SetParent(p.model, true);
            }
            // body collider: slides along walls rather than sticking to them
            float L = spec.m.L, W = spec.m.W, H = spec.van ? 2.0f : 1.42f;
            var slide = new PhysicsMaterial("CarBody") { dynamicFriction = 0.05f, staticFriction = 0.05f, frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0.05f, bounceCombine = PhysicsMaterialCombine.Minimum };
            var lower = vis.gameObject.AddComponent<BoxCollider>();
            lower.center = new Vector3(0f, 0.3f + 0.36f, 0f); lower.size = new Vector3(W * 0.97f, 0.72f, L * 0.98f); lower.sharedMaterial = slide;
            var upper = new GameObject("Cabin").AddComponent<BoxCollider>(); upper.transform.SetParent(vis, false);
            upper.center = new Vector3(0f, 1.02f + (H - 1.02f) * 0.5f - 0.02f, -L * 0.04f); upper.size = new Vector3(W * 0.86f, H - 1.06f, L * (spec.van ? 0.9f : 0.58f)); upper.sharedMaterial = slide;
            SetLayer(vis, Person.VehicleLayer);
            Tune(g, h);
            root.position += root.up * (0.03f - protoGround); // the rig's origin sits above its wheels: lift it so the tyres are on the ground we were given
            return true;
        }

        /// <summary>How big our wheel really is (half its height), from its meshes; works while the car is still switched off.</summary>
        static float WheelRadius(Transform w, float fallback)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var mf in w.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue; var b = mf.sharedMesh.bounds; var m = mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++) { var c = m.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z)); lo = Mathf.Min(lo, c.y); hi = Mathf.Max(hi, c.y); }
            }
            float r = (hi - lo) * 0.5f;
            return r > 0.18f && r < 0.6f ? r : fallback;
        }

        static void SetLayer(Transform t, int layer) { foreach (var c in t.GetComponentsInChildren<Transform>(true)) c.gameObject.layer = layer; }

        /// <summary>The settings for this kind of car: weight, top speed, pull, downforce; brakes on when left parked.</summary>
        static void Tune(GameObject g, CarHandling h)
        {
            var rb = g.GetComponent<Rigidbody>(); if (rb != null) { rb.mass = h.mass; rb.interpolation = RigidbodyInterpolation.Interpolate; }
            var eng = g.GetComponentInChildren<RCCP_Engine>(true);
            // game pace: a real hatch's 150 Nm takes ten seconds to 60; the old game got there in four, so every engine pulls
            // a good deal harder than the real thing (the order between the cars stays the same)
            if (eng != null) { eng.maximumSpeed = h.topKmh; eng.maximumTorqueAsNM = h.torque * GamePull; eng.engineRevLimiter = true; }
            var aero = g.GetComponentInChildren<RCCP_AeroDynamics>(true);
            if (aero != null) { aero.downForce = h.downforce; aero.autoReset = true; }
            var gb = g.GetComponentInChildren<RCCP_Gearbox>(true);
            if (gb != null) gb.transmissionType = RCCP_Gearbox.TransmissionType.Automatic;
            var inp = g.GetComponentInChildren<RCCP_Input>(true);
            if (inp != null) { inp.autoReverse = true; inp.applyHandBrakeOnDisable = true; }
        }

        /// <summary>Switch RCC's driving style to this car's (only the car you're in runs on it, so it's set as you get in).</summary>
        public static void Style(CarHandling h)
        {
            if (h?.style == null) return;
            foreach (var n in h.style)
            {
                int i = RCCP.GetBehaviorIndexByName(n);
                if (i >= 0) { RCCP.SetBehavior(i); return; }
            }
        }

        /// <summary>The names of RCC's driving styles (for the log).</summary>
        public static string Styles()
        {
            var s = RCCP_Settings.Instance; if (s == null || s.behaviorTypes == null) return "(none)";
            return string.Join(", ", s.behaviorTypes.Select(b => b.behaviorName));
        }
    }
}
