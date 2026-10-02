using System;
using ImpactLab.Physics;
using NUnit.Framework;
using UnityEngine;

namespace ImpactLab.Tests.EditMode
{
    public sealed class LaunchMathTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void DragShorterThanMaximumRemainsUnchanged()
        {
            Vector3 drag = new Vector3(-0.5f, -0.5f, 0f);
            AssertVector(LaunchMath.ClampDragOffset(drag, 2f), drag);
        }

        [Test]
        public void DragLongerThanMaximumIsClamped()
        {
            Vector3 result = LaunchMath.ClampDragOffset(new Vector3(-3f, -4f, 0f), 2f);
            Assert.That(result.magnitude, Is.EqualTo(2f).Within(Tolerance));
            AssertVector(result.normalized, new Vector3(-3f, -4f, 0f).normalized);
        }

        [Test]
        public void ZeroDragGivesZeroPower()
        {
            Assert.That(LaunchMath.CalculateNormalizedPower(Vector3.zero, 2f), Is.Zero);
        }

        [Test]
        public void MaximumDragGivesFullPower()
        {
            Assert.That(LaunchMath.CalculateNormalizedPower(Vector3.left * 2f, 2f), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void DragBeyondMaximumGivesFullPower()
        {
            Assert.That(LaunchMath.CalculateNormalizedPower(Vector3.left * 4f, 2f), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void HalfMaximumDragGivesHalfPower()
        {
            Assert.That(LaunchMath.CalculateNormalizedPower(Vector3.left, 2f), Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void LaunchVelocityPointsOppositeDrag()
        {
            Vector3 drag = new Vector3(-1f, -1f, 0f);
            Vector3 velocity = LaunchMath.CalculateLaunchVelocity(drag, 2f, 12f);
            AssertVector(velocity.normalized, -drag.normalized);
        }

        [Test]
        public void MaximumDragProducesMaximumSpeed()
        {
            Assert.That(LaunchMath.CalculateLaunchVelocity(Vector3.left * 2f, 2f, 12f).magnitude,
                Is.EqualTo(12f).Within(Tolerance));
        }

        [Test]
        public void HalfDragProducesHalfSpeed()
        {
            Assert.That(LaunchMath.CalculateLaunchVelocity(Vector3.left, 2f, 12f).magnitude,
                Is.EqualTo(6f).Within(Tolerance));
        }

        [Test]
        public void ZeroDragProducesZeroVelocity()
        {
            AssertVector(LaunchMath.CalculateLaunchVelocity(Vector3.zero, 2f, 12f), Vector3.zero);
        }

        [Test]
        public void TrajectoryAtZeroTimeEqualsStart()
        {
            Vector3 start = new Vector3(2f, 3f, 4f);
            AssertVector(LaunchMath.CalculateTrajectoryPoint(start, Vector3.right * 12f, Vector3.down * 10f, 0f), start);
        }

        [Test]
        public void TrajectoryAppliesInitialVelocity()
        {
            Vector3 start = new Vector3(2f, 3f, 4f);
            Vector3 velocity = new Vector3(4f, 6f, 0f);
            AssertVector(LaunchMath.CalculateTrajectoryPoint(start, velocity, Vector3.zero, 0.5f),
                new Vector3(4f, 6f, 4f));
        }

        [Test]
        public void TrajectoryAppliesGravityTerm()
        {
            AssertVector(LaunchMath.CalculateTrajectoryPoint(Vector3.zero, new Vector3(2f, 4f, 0f),
                new Vector3(0f, -10f, 0f), 2f), new Vector3(4f, -12f, 0f));
        }

        [Test]
        public void VelocityBeyondMaximumIsClamped()
        {
            Assert.That(LaunchMath.CalculateLaunchVelocity(Vector3.left * 8f, 2f, 12f).magnitude,
                Is.EqualTo(12f).Within(Tolerance));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidMaximumDragDistanceIsRejected(float distance)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LaunchMath.ClampDragOffset(Vector3.left, distance));
            Assert.Throws<ArgumentOutOfRangeException>(() => LaunchMath.CalculateNormalizedPower(Vector3.left, distance));
            Assert.Throws<ArgumentOutOfRangeException>(() => LaunchMath.CalculateLaunchVelocity(Vector3.left, distance, 12f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidMaximumLaunchSpeedIsRejected(float speed)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LaunchMath.CalculateLaunchVelocity(Vector3.left, 2f, speed));
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(Tolerance));
        }
    }
}
