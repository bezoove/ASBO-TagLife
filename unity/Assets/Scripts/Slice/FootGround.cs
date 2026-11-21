using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Ground-aware feet (Animator IK pass): each foot is raycast onto whatever is underneath — kerbs, steps, slopes —
    /// keeping the animation's own lift, the sole is turned to the surface, and the pelvis drops so the lower foot
    /// can reach. Sits on the Animator's GameObject (OnAnimatorIK is only sent there).
    /// </summary>
    public class FootGround : MonoBehaviour
    {
        public float rayUp = 0.55f, rayDown = 0.75f, maxDrop = 0.32f;
        const int Mask = ~((1 << 2) | (1 << 31) | (1 << 28) | (1 << 29)); // not cars or ragdolls
        Animator anim; float pelvis, lOff, rOff, lW, rW;

        public void Bind(Animator a) { anim = a; }

        bool Probe(AvatarIKGoal goal, float bottom, out Vector3 pos, out Quaternion rot, out float off)
        {
            Transform root = anim.transform; Vector3 up = root.up;
            pos = anim.GetIKPosition(goal); rot = anim.GetIKRotation(goal); off = 0f;
            float lift = Mathf.Max(0f, Vector3.Dot(pos - root.position, up) - bottom); // how high the animation raises this foot
            if (!Physics.Raycast(pos + up * rayUp, -up, out var hit, rayUp + rayDown, Mask, QueryTriggerInteraction.Ignore)) return false;
            if (Vector3.Angle(hit.normal, up) > 50f) return false;
            Vector3 target = hit.point + up * (bottom + lift);
            off = Vector3.Dot(target - pos, up);
            rot = Quaternion.FromToRotation(up, Vector3.Slerp(up, hit.normal, 0.8f)) * rot;
            return true;
        }

        void OnAnimatorIK(int layer)
        {
            if (anim == null || !anim.enabled || !anim.isHuman) return;
            float k = 1f - Mathf.Exp(-16f * Time.deltaTime);
            bool lok = Probe(AvatarIKGoal.LeftFoot, anim.leftFeetBottomHeight, out var lp, out var lr, out var lo);
            bool rok = Probe(AvatarIKGoal.RightFoot, anim.rightFeetBottomHeight, out var rp, out var rr, out var ro);
            lW = Mathf.Lerp(lW, lok ? 1f : 0f, k); rW = Mathf.Lerp(rW, rok ? 1f : 0f, k);
            lOff = Mathf.Lerp(lOff, lok ? lo : 0f, k); rOff = Mathf.Lerp(rOff, rok ? ro : 0f, k);
            // drop the hips by the lower foot's shortfall so it can reach (never raise them: the controller already stands on the higher ground)
            float want = Mathf.Clamp(Mathf.Min(0f, Mathf.Min(lok ? lo : 0f, rok ? ro : 0f)), -maxDrop, 0f);
            pelvis = Mathf.Lerp(pelvis, want, k);
            Vector3 up = anim.transform.up;
            anim.bodyPosition += up * pelvis;
            // feet go to the surface; the hip drop is already in the IK goals' frame, so add it back
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, lW); anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, lW * 0.7f);
            anim.SetIKPosition(AvatarIKGoal.LeftFoot, lp + up * lOff); anim.SetIKRotation(AvatarIKGoal.LeftFoot, lr);
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, rW); anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, rW * 0.7f);
            anim.SetIKPosition(AvatarIKGoal.RightFoot, rp + up * rOff); anim.SetIKRotation(AvatarIKGoal.RightFoot, rr);
        }
    }
}
