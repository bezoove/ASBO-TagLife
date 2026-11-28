using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Ground-aware feet (Animator IK pass): each foot is raycast onto whatever is underneath — kerbs, steps, slopes —
    /// keeping the animation's own lift, the sole is turned to the surface, and the pelvis drops so the lower foot
    /// can reach. Sits on the Animator's GameObject (OnAnimatorIK is only sent there).
    /// Feet also stay planted: while the animation has a foot down (low, and not moving, in the clip or in the world),
    /// it is pinned to the spot it landed on, so a kick's standing foot or a fighter shuffling into range doesn't skate
    /// along the floor. It lets go when the foot lifts, or if the body has been carried too far from it to reach.
    /// </summary>
    public class FootGround : MonoBehaviour
    {
        public float rayUp = 0.55f, rayDown = 0.75f, maxDrop = 0.32f;
        /// <summary>Sat down (in a car): leave the feet where the pose puts them.</summary>
        public bool off;
        /// <summary>In the air (jumping, falling): the feet and hips are left to the animation, not pulled back to the ground
        /// below (which held the body down while only the legs moved).</summary>
        public bool airborne;
        /// <summary>Head looks at this world point with this weight (crouched: the capture has the head down at the floor; this keeps it up, looking where you're going).</summary>
        public float lookW, lowerBy, leanFwd; public Vector3 lookAt; float lookNow, lowerNow, leanNow;
        /// <summary>Sat in a car's seat: where the body, feet and hands go (world space; set every frame by whoever sat us down).</summary>
        public bool sat; public Vector3 satBody, satFootL, satFootR, satHandL, satHandR; public Quaternion satRot = Quaternion.identity;
        void Goal(AvatarIKGoal g, Vector3 p, float w, float rw) { anim.SetIKPositionWeight(g, w); anim.SetIKPosition(g, p); anim.SetIKRotationWeight(g, rw); if (rw > 0f) anim.SetIKRotation(g, satRot * Quaternion.Euler(-25f, 0f, 0f)); }
        public float lift = 0.025f; // stand a touch taller than the clips: straighter legs, less of a crouch
        float rootFix;
        const int Mask = ~((1 << 2) | (1 << 31) | (1 << 28) | (1 << 29)); // not cars or ragdolls
        Animator anim; float pelvis, lOff, rOff, lW, rW;
        struct Pin { public bool on, seen; public Vector3 at, lastLocal, lastWorld; public float w; }
        Pin lPin, rPin;
        const float PinReach = 0.22f; // how far the body can move off a planted foot before it has to step
        float lastYaw; float turnRate;

        /// <summary>The foot target with the pin applied: held where it landed while the animation has it planted.</summary>
        Vector3 Pinned(ref Pin k, Vector3 raw, Vector3 target, float bottom, float dt)
        {
            var root = anim.transform; Vector3 up = root.up;
            Vector3 local = root.InverseTransformPoint(raw);
            float h = Vector3.Dot(raw - root.position, up) - bottom;
            float vLocal = k.seen ? (local - k.lastLocal).WithY(0).magnitude / dt : 9f, vWorld = k.seen ? (raw - k.lastWorld).WithY(0).magnitude / dt : 9f;
            k.lastLocal = local; k.lastWorld = raw; k.seen = true;
            // planted = low, and held still by the animation itself (a kick's standing foot, a fighting stance); walking and
            // running feet travel back under the body, so they're left to the animation (pinning those made legs pop)
            bool down = h < 0.04f && vLocal < 0.22f && turnRate < 120f;
            if (down && !k.on) { k.on = true; k.at = target; }
            if (k.on && (!down || (k.at - target).WithY(0).magnitude > PinReach)) k.on = false;
            k.w = Mathf.MoveTowards(k.w, k.on ? 1f : 0f, dt / (k.on ? 0.08f : 0.18f));
            if (k.w <= 0f) return target;
            Vector3 at = k.at; at.y = target.y; // keep the ground fit's height
            return Vector3.Lerp(target, at, k.w);
        }

        public void Bind(Animator a) { anim = a; }
        public float Pelvis => pelvis; public float LeftOff => lOff; public float RightOff => rOff;

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

        /// <summary>
        /// Stand the body on the real ground: the navmesh (or the controller's skin) can sit a few centimetres off the
        /// visible ground, which made the feet get pushed up and the knees bend. Measured under the person every frame.
        /// </summary>
        void Update()
        {
            if (anim == null || !anim.enabled || transform.parent == null) return;
            var root = transform.parent;
            float want = 0f;
            if (!airborne && Physics.Raycast(root.position + Vector3.up * 0.5f, Vector3.down, out var hit, 1.1f, Mask, QueryTriggerInteraction.Ignore)) want = Mathf.Clamp(hit.point.y - root.position.y, -0.25f, 0.25f);
            rootFix = Mathf.Lerp(rootFix, want, 1f - Mathf.Exp(-12f * Time.deltaTime));
            var lp = transform.localPosition; lp.y = rootFix; transform.localPosition = lp;
        }

        /// <summary>Tip a spine bone forward by deg (about the body's own left-right axis).</summary>
        void Lean(HumanBodyBones b, float deg)
        {
            var t = anim.GetBoneTransform(b); if (t == null || t.parent == null) return;
            anim.SetBoneLocalRotation(b, Quaternion.Inverse(t.parent.rotation) * (Quaternion.AngleAxis(deg, anim.transform.right) * t.rotation));
        }
        void OnAnimatorIK(int layer)
        {
            if (sat && anim != null && anim.isHuman && layer == 0)
            {
                // sat in a car: hips down on the seat, feet out on the pedals, hands on the wheel
                anim.bodyPosition = satBody;
                Goal(AvatarIKGoal.LeftFoot, satFootL, 1f, 0.6f); Goal(AvatarIKGoal.RightFoot, satFootR, 1f, 0.6f);
                Goal(AvatarIKGoal.LeftHand, satHandL, 0.9f, 0f); Goal(AvatarIKGoal.RightHand, satHandR, 0.9f, 0f);
                return;
            }
            if (off || anim == null || !anim.enabled || !anim.isHuman || layer != 0) return;
            lookNow = Mathf.MoveTowards(lookNow, lookW, 3f * Time.deltaTime);
            if (lookNow > 0.01f) { anim.SetLookAtWeight(lookNow, 0.25f, 0.9f, 1f, 0.6f); anim.SetLookAtPosition(lookAt); }
            leanNow = Mathf.MoveTowards(leanNow, leanFwd, 45f * Time.deltaTime);
            if (Mathf.Abs(leanNow) > 0.1f) { Lean(HumanBodyBones.Spine, leanNow * 0.5f); Lean(HumanBodyBones.Chest, leanNow * 0.5f); }
            if (airborne) { lW = rW = 0f; lOff = rOff = 0f; pelvis = 0f; lPin.on = rPin.on = false; lPin.w = rPin.w = 0f; return; }
            float k = 1f - Mathf.Exp(-16f * Time.deltaTime);
            bool lok = Probe(AvatarIKGoal.LeftFoot, anim.leftFeetBottomHeight, out var lp, out var lr, out var lo);
            bool rok = Probe(AvatarIKGoal.RightFoot, anim.rightFeetBottomHeight, out var rp, out var rr, out var ro);
            lW = Mathf.Lerp(lW, lok ? 1f : 0f, k); rW = Mathf.Lerp(rW, rok ? 1f : 0f, k);
            lOff = Mathf.Lerp(lOff, lok ? lo : 0f, k); rOff = Mathf.Lerp(rOff, rok ? ro : 0f, k);
            // drop the hips by the lower foot's shortfall so it can reach (never raise them: the controller already stands on the higher ground)
            float want = Mathf.Clamp(Mathf.Min(0f, Mathf.Min(lok ? lo : 0f, rok ? ro : 0f)), -maxDrop, 0f);
            pelvis = Mathf.Lerp(pelvis, want, k);
            Vector3 up = anim.transform.up;
            lowerNow = Mathf.MoveTowards(lowerNow, lowerBy, 0.5f * Time.deltaTime);
            anim.bodyPosition += up * (pelvis + lift * Mathf.Min(lW, rW) - lowerNow);
            // feet go to the surface; the hip drop is already in the IK goals' frame, so add it back
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, lW); anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, lW * 0.7f);
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float yawNow = anim.transform.eulerAngles.y; turnRate = Mathf.Lerp(turnRate, Mathf.Abs(Mathf.DeltaAngle(lastYaw, yawNow)) / dt, 0.3f); lastYaw = yawNow;
            Vector3 lt = Pinned(ref lPin, lp, lp + up * lOff, anim.leftFeetBottomHeight, dt), rt = Pinned(ref rPin, rp, rp + up * rOff, anim.rightFeetBottomHeight, dt);
            anim.SetIKPosition(AvatarIKGoal.LeftFoot, lt); anim.SetIKRotation(AvatarIKGoal.LeftFoot, lr);
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, rW); anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, rW * 0.7f);
            anim.SetIKPosition(AvatarIKGoal.RightFoot, rt); anim.SetIKRotation(AvatarIKGoal.RightFoot, rr);
        }
    }
}
