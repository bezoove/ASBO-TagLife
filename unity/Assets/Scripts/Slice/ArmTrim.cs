using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Brings the upper arms in towards the body after the Animator has posed the character. The mocap was recorded
    /// on a slimmer skeleton, so retargeted onto UMA bodies the arms swing out wide; this trims them back.
    /// Skips frames where the Animator did not write the pose (culled), so the offset never accumulates.
    /// </summary>
    public class ArmTrim : MonoBehaviour
    {
        public float maxTrim = 18f, minOut = 9f; // trim up to maxTrim degrees, never closer to the body than minOut
        Animator anim; Transform lu, ru, ll, rl; Quaternion lastL, lastR; bool set;

        public void Bind(Animator a)
        {
            anim = a;
            lu = a.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            ru = a.GetBoneTransform(HumanBodyBones.RightUpperArm);
            ll = a.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            rl = a.GetBoneTransform(HumanBodyBones.RightLowerArm);
        }

        /// <summary>How far the upper arm is out from hanging straight down, seen from behind (degrees, + = away from the body).</summary>
        float Out(Transform upper, Transform lower, Vector3 fwd, Vector3 up, float side)
        {
            Vector3 d = Vector3.ProjectOnPlane(lower.position - upper.position, fwd);
            return side * Vector3.SignedAngle(-up, d, fwd);
        }

        void LateUpdate()
        {
            if (anim == null || !anim.enabled || lu == null || ru == null || ll == null || rl == null || maxTrim <= 0f) return;
            if (set && lu.localRotation == lastL && ru.localRotation == lastR) return;
            Vector3 fwd = anim.transform.forward, up = anim.transform.up;
            // looking along +forward a positive angle turns counter-clockwise: down -> right, so the left arm comes down with +
            float l = Mathf.Clamp(Out(lu, ll, fwd, up, -1f) - minOut, 0f, maxTrim);
            float r = Mathf.Clamp(Out(ru, rl, fwd, up, 1f) - minOut, 0f, maxTrim);
            lu.rotation = Quaternion.AngleAxis(l, fwd) * lu.rotation;
            ru.rotation = Quaternion.AngleAxis(-r, fwd) * ru.rotation;
            lastL = lu.localRotation; lastR = ru.localRotation; set = true;
        }
    }
}
