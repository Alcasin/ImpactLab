using System;
using UnityEngine;

namespace ImpactLab.Physics
{
    public static class LaunchMath
    {
        public static Vector3 ClampDragOffset(Vector3 dragOffset, float maximumDragDistance)
        {
            ValidatePositive(maximumDragDistance, nameof(maximumDragDistance));
            return Vector3.ClampMagnitude(dragOffset, maximumDragDistance);
        }

        public static float CalculateNormalizedPower(Vector3 dragOffset, float maximumDragDistance)
        {
            ValidatePositive(maximumDragDistance, nameof(maximumDragDistance));
            return Mathf.Clamp01(dragOffset.magnitude / maximumDragDistance);
        }

        public static Vector3 CalculateLaunchVelocity(
            Vector3 dragOffset, float maximumDragDistance, float maximumLaunchSpeed)
        {
            ValidatePositive(maximumLaunchSpeed, nameof(maximumLaunchSpeed));
            Vector3 clampedDrag = ClampDragOffset(dragOffset, maximumDragDistance);
            return -clampedDrag / maximumDragDistance * maximumLaunchSpeed;
        }

        public static Vector3 CalculateTrajectoryPoint(
            Vector3 startPosition, Vector3 initialVelocity, Vector3 gravity, float time)
        {
            return startPosition + initialVelocity * time + 0.5f * gravity * time * time;
        }

        private static void ValidatePositive(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Must be finite and greater than zero.");
            }
        }
    }
}
