#if UNITY_EDITOR
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ASBO.EditorTools
{
    /// <summary>
    /// Finds moves inside the MoCap Central fight takes by measuring the body, not by eye: for each moment of a take,
    /// where each hand is relative to the head and chest (guard = both fists up by the face; punch = one hand driven
    /// out in front; hit = chest snapped back). Logs a compact table to pick clip ranges from.
    /// </summary>
    public static class ASBOFightCuts
    {
        public static void Scan(int take, float from, float to, float step)
        {
            string[] takes = { "MCU_am_Ready_Fight_01_Knockdown_A", "MCU_af_Ready_Fight_01_Knockdown_B", "MCU_am_Ready_Fight_02_Kickdown_A", "MCU_af_Ready_Fight_02_Kickdown_B" };
            string path = "Assets/MoCapCentral/MC_Sample/Animations/Fight/" + takes[take] + ".FBX";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview") && !c.name.StartsWith("ASBO_")).OrderByDescending(c => c.length).FirstOrDefault();
            if (model == null || clip == null) { Debug.LogWarning("[ASBO] fightcuts: missing " + path); return; }
            var go = Object.Instantiate(model); go.hideFlags = HideFlags.HideAndDontSave;
            var an = go.GetComponent<Animator>(); if (an == null) an = go.AddComponent<Animator>();
            if (an.avatar == null) an.avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if (an.avatar == null && AssetImporter.GetAtPath(path) is ModelImporter mi && mi.sourceAvatar != null) an.avatar = mi.sourceAvatar;
            if (an.avatar == null) { Debug.LogWarning("[ASBO] fightcuts: no avatar for " + path); Object.DestroyImmediate(go); return; }
            var sb = new StringBuilder($"[ASBO] fightcuts take {take} {clip.name} len {clip.length:F2}s fps {clip.frameRate}: t | L fwd,side,up | R fwd,side,up | chest back | hips y\n");
            AnimationMode.StartAnimationMode();
            try
            {
                for (float t = from; t <= Mathf.Min(to, clip.length); t += step)
                {
                    AnimationMode.BeginSampling(); AnimationMode.SampleAnimationClip(go, clip, t); AnimationMode.EndSampling();
                    Transform B(HumanBodyBones b) => an.GetBoneTransform(b);
                    var hips = B(HumanBodyBones.Hips); var head = B(HumanBodyBones.Head); var chest = B(HumanBodyBones.Chest) ?? B(HumanBodyBones.Spine);
                    var lh = B(HumanBodyBones.LeftHand); var rh = B(HumanBodyBones.RightHand);
                    var lu = B(HumanBodyBones.LeftUpperLeg); var ru = B(HumanBodyBones.RightUpperLeg);
                    if (hips == null || head == null || lh == null || rh == null) { sb.Append("no bones\n"); break; }
                    // body frame: right = hip line, forward = up x right
                    Vector3 right = (ru.position - lu.position).WithY(0).normalized, fwd = Vector3.Cross(right, Vector3.up).normalized;
                    string H(Transform h) => $"{Vector3.Dot(h.position - chest.position, fwd):F2},{Vector3.Dot(h.position - chest.position, right):F2},{h.position.y - head.position.y:F2}";
                    float back = Vector3.Dot(chest.position - hips.position, -fwd);
                    sb.Append($"{t:F2} | {H(lh)} | {H(rh)} | {back:F2} | {hips.position.y:F2}\n");
                }
            }
            finally { AnimationMode.StopAnimationMode(); Object.DestroyImmediate(go); }
            Debug.Log(sb.ToString());
        }
        static Vector3 WithY(this Vector3 v, float y) { v.y = y; return v; }
    }
}
#endif
