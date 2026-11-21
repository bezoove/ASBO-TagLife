#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Import rules: characters (Assets/Resources/Characters) become Humanoids with their own avatar;
    /// CMU mocap FBX (Assets/Art/Anim) become Humanoid clips cut/looped from clips.json. Then the shared
    /// Animator Controller (Assets/Resources/Anim/Humanoid.controller) is (re)built with Mecanim blend trees.
    /// </summary>
    public class ASBOImport : AssetPostprocessor
    {
        const string AnimDir = "Assets/Art/Anim";
        const string ControllerPath = "Assets/Resources/Anim/Humanoid.controller";

        [System.Serializable] public class Clip { public string file, name; public int first, last; public bool loop, inPlace; public float speed; }
        [System.Serializable] public class ClipList { public Clip[] clips; }

        static ClipList Load()
        {
            var p = AnimDir + "/clips.json";
            return File.Exists(p) ? JsonUtility.FromJson<ClipList>(File.ReadAllText(p)) : new ClipList { clips = new Clip[0] };
        }

        void OnPreprocessModel()
        {
            var mi = (ModelImporter)assetImporter;
            if (assetPath.Contains("/Resources/Characters/"))
            {
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.importAnimation = false;
                mi.isReadable = true;            // needed to cut limbs off (BakeMesh)
                mi.importBlendShapes = false;
                mi.importCameras = false; mi.importLights = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                mi.meshCompression = ModelImporterMeshCompression.Low;
                mi.optimizeGameObjects = false;
            }
            else if (assetPath.StartsWith(AnimDir))
            {
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.importAnimation = true;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.animationCompression = ModelImporterAnimationCompression.Optimal;
                mi.resampleCurves = true;
            }
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(AnimDir)) return;
            var mi = (ModelImporter)assetImporter;
            var file = Path.GetFileNameWithoutExtension(assetPath);
            var defs = Load().clips.Where(c => c.file == file).ToArray();
            if (defs.Length == 0 || mi.defaultClipAnimations.Length == 0) return;
            var take = mi.defaultClipAnimations[0];
            var list = new List<ModelImporterClipAnimation>();
            foreach (var d in defs)
            {
                var c = new ModelImporterClipAnimation
                {
                    name = d.name, takeName = take.takeName,
                    firstFrame = d.first, lastFrame = d.last,
                    loopTime = d.loop, loopPose = d.loop,
                    lockRootRotation = true, keepOriginalOrientation = false,          // root rotation baked, based on body orientation
                    lockRootHeightY = true, keepOriginalPositionY = false, heightFromFeet = true, // feet on the ground
                    lockRootPositionXZ = d.inPlace, keepOriginalPositionXZ = false,
                    cycleOffset = 0f, mirror = false,
                };
                list.Add(c);
            }
            mi.clipAnimations = list.ToArray();
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Any(p => p.StartsWith(AnimDir) && (p.EndsWith(".fbx") || p.EndsWith(".json"))))
                EditorApplication.delayCall += BuildController;
        }

        internal static AnimationClip ClipNamed(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { AnimDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (a is AnimationClip c && c.name == name && !c.name.StartsWith("__preview__")) return c;
            }
            return null;
        }

        [MenuItem("ASBO/Rebuild Animator Controller")]
        public static void BuildController()
        {
            var defs = Load().clips;
            if (defs.Length == 0) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
            if (File.Exists(ControllerPath)) AssetDatabase.DeleteAsset(ControllerPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Talk", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Wave", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Kneel", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Variant", AnimatorControllerParameterType.Float);
            var sm = ac.layers[0].stateMachine;

            // locomotion: 1D blend tree on Speed, thresholds = each clip's real root speed so feet don't slide
            var loco = ac.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D; tree.blendParameter = "Speed"; tree.useAutomaticThresholds = false;
            foreach (var n in new[] { "Idle", "WalkSlow", "Walk", "WalkBrisk", "Jog", "Run" })
            {
                var d = defs.FirstOrDefault(c => c.name == n); var clip = ClipNamed(n);
                if (d != null && clip != null) tree.AddChild(clip, d.speed);
            }
            sm.defaultState = loco;

            AnimatorState Add(string clip, Vector3 pos)
            {
                var c = ClipNamed(clip); if (c == null) return null;
                var s = sm.AddState(clip, pos); s.motion = c; return s;
            }
            void Bool(AnimatorState s, string param)
            {
                if (s == null) return;
                var tin = loco.AddTransition(s); tin.AddCondition(AnimatorConditionMode.If, 0, param); tin.hasExitTime = false; tin.duration = 0.35f;
                var tout = s.AddTransition(loco); tout.AddCondition(AnimatorConditionMode.IfNot, 0, param); tout.hasExitTime = false; tout.duration = 0.35f;
            }
            Bool(Add("Talk", new Vector3(300, 0)), "Talk");
            Bool(Add("Wave", new Vector3(300, 80)), "Wave");
            Bool(Add("Kneel", new Vector3(300, 160)), "Kneel");
            var jump = Add("Jump", new Vector3(300, -100));
            if (jump != null)
            {
                var tj = sm.AddAnyStateTransition(jump); tj.AddCondition(AnimatorConditionMode.If, 0, "Jump"); tj.duration = 0.1f; tj.canTransitionToSelf = false;
                var tb = jump.AddTransition(loco); tb.hasExitTime = true; tb.exitTime = 0.85f; tb.duration = 0.2f;
            }
            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            Debug.Log("[ASBO] Humanoid animator controller built with " + tree.children.Length + " locomotion clips.");
        }
    }
}
#endif
