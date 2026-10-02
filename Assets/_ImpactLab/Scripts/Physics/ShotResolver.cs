using System;
using UnityEngine;

namespace ImpactLab.Physics
{
    public sealed class ShotResolver : MonoBehaviour
    {
        [SerializeField] private Rigidbody[] monitoredBodies = new Rigidbody[0];
        [SerializeField, Min(0f)] private float minimumObservationTime = 0.25f;
        [SerializeField, Min(0f)] private float settleLinearSpeed = 0.15f;
        [SerializeField, Min(0f)] private float settleAngularSpeed = 0.5f;
        [SerializeField, Min(0f)] private float settleHoldDuration = 0.3f;
        [SerializeField, Min(0.01f)] private float maximumResolutionTime = 4f;
        [SerializeField] private float minimumWorldY = -5f;

        private float elapsed;
        private float settledFor;
        public bool IsResolving { get; private set; }
        public event Action Resolved;

        public void BeginResolution()
        {
            CancelResolution();
            if (!isActiveAndEnabled || !HasValidConfiguration())
            {
                Debug.LogError("ShotResolver requires an active component, monitored bodies, finite "
                    + "nonnegative settle tuning, and a positive timeout.", this);
                return;
            }
            IsResolving = true;
        }

        public void CancelResolution()
        {
            IsResolving = false;
            elapsed = 0f;
            settledFor = 0f;
        }

        private bool HasValidConfiguration()
        {
            if (monitoredBodies == null || monitoredBodies.Length == 0
                || !IsNonnegativeFinite(minimumObservationTime)
                || !IsNonnegativeFinite(settleLinearSpeed)
                || !IsNonnegativeFinite(settleAngularSpeed)
                || !IsNonnegativeFinite(settleHoldDuration)
                || !IsNonnegativeFinite(maximumResolutionTime) || maximumResolutionTime <= 0f
                || float.IsNaN(minimumWorldY) || float.IsInfinity(minimumWorldY))
            {
                return false;
            }
            foreach (Rigidbody body in monitoredBodies)
            {
                if (body != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsNonnegativeFinite(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void Update()
        {
            if (!IsResolving)
            {
                return;
            }
            float previousElapsed = elapsed;
            elapsed += Time.deltaTime;
            bool allSettled = true;
            foreach (Rigidbody body in monitoredBodies)
            {
                if (body == null || !body.gameObject.activeInHierarchy)
                {
                    continue;
                }
                if (!body.isKinematic && body.position.y < minimumWorldY)
                {
                    Resolve();
                    return;
                }
                if (!body.isKinematic && !body.IsSleeping()
                    && (body.linearVelocity.sqrMagnitude > settleLinearSpeed * settleLinearSpeed
                        || body.angularVelocity.sqrMagnitude > settleAngularSpeed * settleAngularSpeed))
                {
                    allSettled = false;
                }
            }

            // The hold starts after observation, not during the initial launch window.
            settledFor = allSettled && elapsed >= minimumObservationTime
                ? settledFor + elapsed - Mathf.Max(previousElapsed, minimumObservationTime) : 0f;
            if (elapsed >= maximumResolutionTime
                || (allSettled && elapsed >= minimumObservationTime && settledFor >= settleHoldDuration))
            {
                Resolve();
            }
        }

        private void Resolve()
        {
            IsResolving = false;
            Resolved?.Invoke();
        }

        private void OnDisable()
        {
            CancelResolution();
        }
    }
}
