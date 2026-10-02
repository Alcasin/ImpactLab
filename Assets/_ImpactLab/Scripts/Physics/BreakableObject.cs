using System;
using UnityEngine;

namespace ImpactLab.Physics
{
    public sealed class BreakableObject : MonoBehaviour
    {
        [SerializeField] private GameObject intactVisualRoot;
        [SerializeField] private GameObject fracturedRoot;
        [SerializeField] private Collider intactCollider;
        [SerializeField, Min(0.01f)] private float breakImpulseThreshold = 6f;
        [SerializeField, Min(0f)] private float fragmentImpulseScale = 0.25f;

        private Rigidbody[] fragments;
        private bool initialized;

        public bool IsBroken { get; private set; }
        public event Action<BreakableObject> Broken;

        private void Awake()
        {
            Initialize();
        }

        private bool Initialize()
        {
            if (initialized)
            {
                return true;
            }

            if (intactVisualRoot == null || fracturedRoot == null || intactCollider == null
                || intactVisualRoot == fracturedRoot || intactVisualRoot == gameObject
                || fracturedRoot == gameObject
                || !intactVisualRoot.transform.IsChildOf(transform)
                || !fracturedRoot.transform.IsChildOf(transform)
                || intactVisualRoot.transform.IsChildOf(fracturedRoot.transform)
                || fracturedRoot.transform.IsChildOf(intactVisualRoot.transform)
                || !intactCollider.transform.IsChildOf(transform)
                || intactCollider.transform.IsChildOf(fracturedRoot.transform)
                || !IsFinite(breakImpulseThreshold) || breakImpulseThreshold <= 0f
                || !IsFinite(fragmentImpulseScale) || fragmentImpulseScale < 0f)
            {
                Debug.LogError("BreakableObject needs separate visual/fracture descendants, an intact collider "
                    + "outside the fractured root, a finite positive threshold, and a finite nonnegative impulse scale.", this);
                return false;
            }

            fragments = fracturedRoot.GetComponentsInChildren<Rigidbody>(true);
            foreach (Rigidbody fragment in fragments)
            {
                fragment.isKinematic = true;
            }
            fracturedRoot.SetActive(false);
            intactVisualRoot.SetActive(true);
            intactCollider.enabled = true;
            initialized = true;
            return true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsBroken)
            {
                return;
            }

            float impulse = collision.impulse.magnitude;
            if (impulse < breakImpulseThreshold)
            {
                return;
            }
            Vector3 impactPoint = collision.contactCount > 0
                ? collision.GetContact(0).point : transform.position;
            TryBreak(impulse, impactPoint);
        }

        public bool TryBreak(float impactImpulseMagnitude, Vector3 impactPoint)
        {
            if (IsBroken)
            {
                return false;
            }
            if (!IsFinite(impactImpulseMagnitude) || impactImpulseMagnitude < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(impactImpulseMagnitude), "Must be finite and nonnegative.");
            }
            if (!IsFinite(impactPoint.x) || !IsFinite(impactPoint.y) || !IsFinite(impactPoint.z))
            {
                throw new ArgumentException("Impact point must be finite.", nameof(impactPoint));
            }
            if (!Initialize() || impactImpulseMagnitude < breakImpulseThreshold)
            {
                return false;
            }

            IsBroken = true;
            intactCollider.enabled = false;
            intactVisualRoot.SetActive(false);
            fracturedRoot.SetActive(true);

            float impulsePerFragment = fragments.Length > 0
                ? impactImpulseMagnitude * fragmentImpulseScale / fragments.Length : 0f;
            foreach (Rigidbody fragment in fragments)
            {
                if (fragment == null)
                {
                    continue;
                }
                fragment.isKinematic = false;
                fragment.linearVelocity = Vector3.zero;
                fragment.angularVelocity = Vector3.zero;
                Vector3 direction = fragment.worldCenterOfMass - impactPoint;
                direction = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.up;
                fragment.AddForce(direction * impulsePerFragment, ForceMode.Impulse);
            }
            Broken?.Invoke(this);
            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
