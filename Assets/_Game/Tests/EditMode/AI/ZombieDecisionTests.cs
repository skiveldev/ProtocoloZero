using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProtocoloZero.AI.Tests
{
    /// <summary>
    /// Pure decision coverage for ZombieDecision: horizontal distance,
    /// detect/lose thresholds with inclusive boundaries, hysteresis clamping,
    /// patrol looping/return helpers and null/empty safety. No NavMesh needed.
    /// </summary>
    public class ZombieDecisionTests
    {
        [Test]
        public void HorizontalDistance_IgnoresHeight()
        {
            float distance = ZombieDecision.HorizontalDistance(
                new Vector3(0f, 5f, 0f), new Vector3(3f, -2f, 4f));
            Assert.That(distance, Is.EqualTo(5f).Within(0.0001f),
                "vertical separation must not change detection");
        }

        [Test]
        public void ShouldStartChase_UsesInclusiveBoundary()
        {
            Assert.That(ZombieDecision.ShouldStartChase(8f, 8f), Is.True);
            Assert.That(ZombieDecision.ShouldStartChase(7.9f, 8f), Is.True);
            Assert.That(ZombieDecision.ShouldStartChase(8.1f, 8f), Is.False);
        }

        [Test]
        public void ShouldLoseTarget_UsesInclusiveBoundary()
        {
            Assert.That(ZombieDecision.ShouldLoseTarget(12f, 12f), Is.True);
            Assert.That(ZombieDecision.ShouldLoseTarget(12.1f, 12f), Is.True);
            Assert.That(ZombieDecision.ShouldLoseTarget(11.9f, 12f), Is.False);
        }

        [Test]
        public void Hysteresis_LossNeverBelowDetect()
        {
            float detect = 8f;
            float lose = 12f;
            Assert.That(lose, Is.GreaterThanOrEqualTo(detect),
                "configured loss must stay above detect to avoid state flap");

            float badDetect = 10f;
            float badLose = 6f;
            ZombieDecision.ValidateRanges(ref badDetect, ref badLose);
            Assert.That(badLose, Is.GreaterThanOrEqualTo(badDetect),
                "validation clamps behavior, not just names");
        }

        [Test]
        public void ValidateRanges_ClampsDetectMinimum()
        {
            float detect = 0f;
            float lose = 0f;
            ZombieDecision.ValidateRanges(ref detect, ref lose);
            Assert.That(detect, Is.EqualTo(ZombieDecision.MinDetectRange));
            Assert.That(lose, Is.EqualTo(ZombieDecision.MinDetectRange));
        }

        [Test]
        public void NextWaypointIndex_LoopsInOrder()
        {
            Assert.That(ZombieDecision.NextWaypointIndex(0, 4), Is.EqualTo(1));
            Assert.That(ZombieDecision.NextWaypointIndex(2, 4), Is.EqualTo(3));
            Assert.That(ZombieDecision.NextWaypointIndex(3, 4), Is.EqualTo(0),
                "patrol visits waypoints in order and loops");
        }

        [Test]
        public void NextWaypointIndex_EmptyList_StaysWithoutThrow()
        {
            Assert.That(ZombieDecision.NextWaypointIndex(0, 0), Is.EqualTo(0));
        }

        [Test]
        public void NearestWaypointIndex_PicksClosest()
        {
            var points = new List<Vector3>
            {
                new Vector3(9f, 0f, 9f),
                new Vector3(15f, 0f, 9f),
                new Vector3(15f, 0f, 15f),
                new Vector3(9f, 0f, 15f)
            };
            Assert.That(
                ZombieDecision.NearestWaypointIndex(new Vector3(14f, 0f, 14f), points),
                Is.EqualTo(2));
        }

        [Test]
        public void NearestWaypointIndex_EmptyList_ReturnsMinusOne()
        {
            Assert.That(
                ZombieDecision.NearestWaypointIndex(Vector3.zero, new List<Vector3>()),
                Is.EqualTo(-1));
            Assert.That(
                ZombieDecision.NearestWaypointIndex(Vector3.zero, null),
                Is.EqualTo(-1));
        }

        [Test]
        public void BrainSurvives_NullTarget_NoWaypoints_WithoutNavMesh()
        {
            var owner = new GameObject("BrainProbe");
            try
            {
                SimpleZombieAI brain = owner.AddComponent<SimpleZombieAI>();
                Assert.That(brain.EvaluateDistanceToPlayer(), Is.EqualTo(-1f),
                    "null target reports -1 instead of throwing");
                Assert.DoesNotThrow(() => brain.Tick(0.25f),
                    "tick without NavMesh readiness must not throw");
                Assert.That(brain.IsNavigationReady, Is.False,
                    "must not pretend ready while off the NavMesh");
                Assert.That(brain.CurrentState, Is.EqualTo(SimpleZombieAI.ZombieState.Patrol));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void DefaultConfig_MatchesSpecThresholds()
        {
            var owner = new GameObject("ConfigProbe");
            try
            {
                SimpleZombieAI brain = owner.AddComponent<SimpleZombieAI>();
                Assert.That(brain.DetectRange, Is.EqualTo(8f));
                Assert.That(brain.LoseRange, Is.EqualTo(12f));
                Assert.That(brain.DecisionInterval, Is.EqualTo(0.25f));
                Assert.That(brain.PatrolSpeed, Is.EqualTo(1.5f));
                Assert.That(brain.ChaseSpeed, Is.EqualTo(2.5f));
                Assert.That(brain.WaypointWaitTime, Is.EqualTo(1.5f));
                Assert.That(brain.StoppingDistance, Is.EqualTo(0.8f));
                Assert.That(brain.ChaseSpeed, Is.GreaterThan(brain.PatrolSpeed),
                    "patrol/chase speeds stay configurable and distinct");
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
