using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Sound in the world. Anything loud (a bottle smashing, a brick hitting a car, a gun) calls Emit. Everyone within
    /// earshot turns to look. If the noise was the player's and they can see him they know it was him; if nobody saw,
    /// it's just a mystery noise: they wander over to have a look, which is how you pull people away from a door.
    /// </summary>
    public static class NoiseBus
    {
        /// <summary>How far a person can see the player (m) and how wide their view is (degrees each side).</summary>
        const float SeeRange = 24f, SeeHalfAngle = 100f;

        /// <param name="loud">Hearing range in metres (a can is about 8, a smashed bottle 25, a car alarm 45).</param>
        /// <param name="source">Who made it (the player, if they threw it), or null for an anonymous noise.</param>
        public static void Emit(Vector3 pos, float loud, Transform source)
        {
            if (loud < 2f) return;
            var player = Object.FindFirstObjectByType<PlayerCtl>();
            float l2 = loud * loud;
            for (int i = 0; i < Person.All.Count; i++)
            {
                var p = Person.All[i];
                if (p == null || p.isPlayer || p.dead) continue;
                Vector3 d = pos - p.transform.position; d.y = 0f;
                if (d.sqrMagnitude > l2) continue;
                if (player != null && source == player.transform && CanSee(p, player.transform)) p.Noticed(player.transform);
                else p.Investigate(pos);
            }
        }

        /// <summary>Line of sight: close enough, in front of them, nothing solid in between.</summary>
        public static bool CanSee(Person p, Transform target)
        {
            Vector3 eye = p.HeadPos, to = target.position + Vector3.up * 1.2f;
            Vector3 d = to - eye; float dist = d.magnitude;
            if (dist > SeeRange) return false;
            Vector3 flat = d; flat.y = 0f;
            if (flat.sqrMagnitude > 0.25f && Vector3.Angle(p.transform.forward, flat) > SeeHalfAngle) return false;
            return !Physics.Raycast(eye, d / dist, dist - 0.3f, 1 << 0, QueryTriggerInteraction.Ignore);
        }
    }
}
