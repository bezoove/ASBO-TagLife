using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>Takes the animation events some walk clips carry (footstep markers) so they don't go unanswered.</summary>
    public class AnimEventSink : MonoBehaviour
    {
        public void OnFootstep(AnimationEvent e) { }
        public void OnLand(AnimationEvent e) { }
    }
}
