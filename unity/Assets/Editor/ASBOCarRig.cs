using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Makes the driving rig our cars wear (Resources/CarRig/RccBase): RCC's prototype car with its demo body, wheel
    /// meshes and body colliders taken off, and the extras we don't use switched off (its lights, wheel
    /// blur, body tilt, fuel, recorder, trailer hitch, dashboard, its own damage: our dents come from Realistic Mesh
    /// Deformation). Everything RCC drives with stays: engine, clutch, gearbox, differential, axles, wheel colliders,
    /// stability, aero, input, audio, particles, nitrous. Built from the Asset Store pack, so it stays out of git;
    /// run ASBO/Cars/Make RCC Rig after importing the pack. Also makes the car models readable so they can dent.
    /// </summary>
    public static class ASBOCarRig
    {
        const string Out = "Assets/Resources/CarRig/RccBase.prefab";

        [MenuItem("ASBO/Cars/Make RCC Rig")]
        public static void Make()
        {
            string src = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Realistic Car Controller Pro" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => { var go = AssetDatabase.LoadAssetAtPath<GameObject>(p); return go != null && go.GetComponent<RCCP_CarController>() != null; });
            if (src == null) { Debug.LogWarning("[ASBO] RCC rig: no RCC vehicle prefab found (import Realistic Car Controller Pro Lite)"); return; }
            var root = PrefabUtility.LoadPrefabContents(src);
            try
            {
                int meshes = 0, cols = 0, off = 0;
                foreach (var r in root.GetComponentsInChildren<Renderer>(true)) if (r is MeshRenderer || r is SkinnedMeshRenderer) { Object.DestroyImmediate(r); meshes++; }
                foreach (var f in root.GetComponentsInChildren<MeshFilter>(true)) Object.DestroyImmediate(f);
                foreach (var c in root.GetComponentsInChildren<Collider>(true)) if (!(c is WheelCollider)) { Object.DestroyImmediate(c); cols++; }
                foreach (var lf in root.GetComponentsInChildren<LensFlare>(true)) Object.DestroyImmediate(lf);
                string[] unused = { "RCCP_Lights", "RCCP_WheelBlur", "RCCP_BodyTilt", "RCCP_Customizer", "RCCP_Recorder", "RCCP_FuelTank", "RCCP_Limiter",
                    "RCCP_TrailerAttacher", "RCCP_Visual_Dashboard", "RCCP_Damage", "RCCP_AI", "RCCP_Exterior_Cameras", "RCCP_HoodCamera", "RCCP_WheelCamera", "RCCP_Lod",
                    "RCCP_VehicleUpgrade_CustomizationManager", "RCCP_DetachablePart", "RCCP_LiteSetupHelper" };
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null || !unused.Contains(mb.GetType().Name)) continue;
                    mb.enabled = false; off++;
                    if (mb.gameObject != root && mb.GetComponents<Component>().Length <= 2) mb.gameObject.SetActive(false); // its own child: switch the lot off
                }
                // no lamps on the rig (our bodies carry their own), and nothing left that lights or flares
                foreach (var l in root.GetComponentsInChildren<Light>(true)) l.gameObject.SetActive(false);
                Directory.CreateDirectory(Path.GetDirectoryName(Out));
                PrefabUtility.SaveAsPrefabAsset(root, Out);
                var cc = root.GetComponent<RCCP_CarController>();
                var rb = root.GetComponent<Rigidbody>();
                var eng = root.GetComponentInChildren<RCCP_Engine>(true);
                var axles = root.GetComponentsInChildren<RCCP_Axle>(true);
                var s = RCCP_Settings.Instance;
                Debug.Log($"[ASBO] RCC rig made from {Path.GetFileName(src)}: removed {meshes} meshes, {cols} colliders, switched off {off} extras. mass {rb?.mass} kg, engine {eng?.maximumTorqueAsNM} Nm / {eng?.maximumSpeed} km/h, " +
                    $"axles: {string.Join("; ", axles.Select(a => $"{a.name} power={a.isPower} steer={a.isSteer} lock={a.maxSteerAngle} autoAlign={a.autoAlignWheelColliders}"))}");
                foreach (var a in axles)
                    foreach (var w in new[] { a.leftWheelCollider, a.rightWheelCollider })
                    {
                        if (w == null) continue; var u = w.GetComponent<WheelCollider>();
                        Debug.Log($"[ASBO] RCC wheel {w.name}: collider {root.transform.InverseTransformPoint(w.transform.position)} model {(w.wheelModel != null ? root.transform.InverseTransformPoint(w.wheelModel.position).ToString() : "none")} r={u?.radius} susp={u?.suspensionDistance} center={u?.center} align={w.alignWheels}");
                    }
                if (s != null) Debug.Log($"[ASBO] RCC settings: styles [{string.Join(", ", s.behaviorTypes.Select(b => b.behaviorName))}] selected {s.behaviorSelectedIndex} override={s.overrideBehavior}; fixedStep override={s.overrideFixedTimeStep} {s.fixedTimeStep}; fps override={s.overrideFPS} {s.maxFPS}; setLayers={s.setLayers} ({s.RCCPLayer}/{s.RCCPWheelColliderLayer}); mobile={s.mobileControllerEnabled}");
                var aero = root.GetComponentInChildren<RCCP_AeroDynamics>(true);
                if (aero != null && aero.COM != null) Debug.Log($"[ASBO] RCC COM at {root.transform.InverseTransformPoint(aero.COM.position)}, downforce {aero.downForce}");
                Debug.Log($"[ASBO] RCC nos: {(root.GetComponentInChildren<RCCP_Nos>(true) != null ? "yes" : "no")}, input autoReverse={root.GetComponentInChildren<RCCP_Input>(true)?.autoReverse}");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Readable();
        }

        /// <summary>Car models need their meshes readable for dents.</summary>
        [MenuItem("ASBO/Cars/Make Car Meshes Readable")]
        public static void Readable()
        {
            int n = 0;
            foreach (var dir in new[] { "Assets/Art/Cars", "Assets/Resources/Cars" })
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var f in Directory.GetFiles(dir, "*.fbx"))
                {
                    var mi = AssetImporter.GetAtPath(f.Replace('\\', '/')) as ModelImporter;
                    if (mi == null || mi.isReadable) continue;
                    mi.isReadable = true; mi.SaveAndReimport(); n++;
                }
            }
            Debug.Log($"[ASBO] car meshes made readable: {n}");
        }
    }
}
