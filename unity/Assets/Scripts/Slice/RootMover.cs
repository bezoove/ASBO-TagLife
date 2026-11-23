using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Sits beside the player's Animator. While on, the playing clip's own motion (its rise and its travel, which the clip
    /// leaves out of the pose) moves the player, scaled per axis so one climbing capture fits walls of different heights
    /// and depths. Off, nothing moves (the usual: the controller script moves the body).
    /// </summary>
    public class RootMover : MonoBehaviour
    {
        public Transform target; public Vector3 scale = Vector3.one; public bool on;
        Animator a;
        void OnAnimatorMove()
        {
            if (!on || target == null) return;
            if (a == null) a = GetComponent<Animator>();
            Vector3 l = target.InverseTransformDirection(a.deltaPosition);
            l.x *= scale.x; l.y *= scale.y; l.z *= scale.z;
            target.position += target.TransformDirection(l);
        }
    }
}
