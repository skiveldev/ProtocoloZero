using System.Collections.Generic;
using UnityEngine;

namespace ProtocoloZero.AI
{
    /// <summary>
    /// Pure patrol/detect/chase/return decision helpers for the Phase 06 zombie.
    /// No NavMesh, no GameObject mutation: everything here is deterministic and
    /// unit-testable in EditMode. <see cref="SimpleZombieAI"/> owns the agent.
    /// Distance is measured horizontally (XZ plane): floor height differences
    /// must not change detection, and the zombie and player both walk on a
    /// roughly flat warehouse floor, so vertical separation carries no signal.
    /// </summary>
    public static class ZombieDecision
    {
        public const float MinDetectRange = 0.5f;
        public const float MinDecisionInterval = 0.05f;

        /// <summary>
        /// Horizontal (XZ) distance in meters between two world positions.
        /// </summary>
        public static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// True when the player enters the detect radius (inclusive boundary).
        /// </summary>
        public static bool ShouldStartChase(float distance, float detectRange)
        {
            return distance <= detectRange;
        }

        /// <summary>
        /// True when the player leaves the loss radius (inclusive boundary).
        /// Hysteresis requires lossRange >= detectRange (see ValidateRanges),
        /// so a target flickering on the detect edge cannot flap states.
        /// </summary>
        public static bool ShouldLoseTarget(float distance, float loseRange)
        {
            return distance >= loseRange;
        }

        /// <summary>
        /// Enforces the actual behavior behind the inspector names:
        /// detect stays positive and loss never drops below detect.
        /// </summary>
        public static void ValidateRanges(ref float detectRange, ref float loseRange)
        {
            if (detectRange < MinDetectRange)
                detectRange = MinDetectRange;
            if (loseRange < detectRange)
                loseRange = detectRange;
        }

        /// <summary>
        /// Next patrol index, looping. Returns 0 when the list is empty so
        /// callers degrade to "stay" instead of throwing.
        /// </summary>
        public static int NextWaypointIndex(int current, int count)
        {
            if (count <= 0)
                return 0;
            return (current + 1) % count;
        }

        /// <summary>
        /// Index of the closest waypoint to a position, or -1 when empty.
        /// </summary>
        public static int NearestWaypointIndex(Vector3 position, IList<Vector3> points)
        {
            if (points == null || points.Count == 0)
                return -1;
            int best = 0;
            float bestDistance = HorizontalDistance(position, points[0]);
            for (int i = 1; i < points.Count; i++)
            {
                float candidate = HorizontalDistance(position, points[i]);
                if (candidate < bestDistance)
                {
                    bestDistance = candidate;
                    best = i;
                }
            }
            return best;
        }
    }
}
