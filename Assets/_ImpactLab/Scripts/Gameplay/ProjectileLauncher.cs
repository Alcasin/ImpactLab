using ImpactLab.Physics;
using ImpactLab.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImpactLab.Gameplay
{
    public sealed class ProjectileLauncher : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private GameFlowController gameFlow;
        [SerializeField] private Transform launchAnchor;
        [SerializeField] private Rigidbody projectileBody;
        [SerializeField] private Collider projectileCollider;
        [SerializeField] private TrajectoryPreview trajectoryPreview;
        [SerializeField, Min(0.01f)] private float maximumDragDistance = 2f;
        [SerializeField, Min(0.01f)] private float minimumDragDistance = 0.15f;
        [SerializeField, Min(0.01f)] private float maximumLaunchSpeed = 12f;

        private InputAction pointerPress;
        private Pointer aimingPointer;
        private bool isAiming;
        private Vector3 clampedDragOffset;

        private void Awake()
        {
            pointerPress = new InputAction("AimPress", InputActionType.Button, "<Pointer>/press");
        }

        private void OnEnable()
        {
            if (!HasValidConfiguration())
            {
                Debug.LogError("ProjectileLauncher needs all references, a collider on the projectile Rigidbody, "
                    + "finite positive tuning values (minimum <= maximum drag), and unfrozen X/Y translation.", this);
                enabled = false;
                return;
            }

            if (gameFlow.CurrentState == GameFlowState.Aiming)
            {
                PrepareAtAnchor();
            }
            pointerPress.Enable();
        }

        private bool HasValidConfiguration()
        {
            return gameplayCamera != null && gameFlow != null && launchAnchor != null
                && projectileBody != null && projectileCollider != null && trajectoryPreview != null
                && projectileCollider.attachedRigidbody == projectileBody
                && IsPositiveFinite(maximumDragDistance) && IsPositiveFinite(minimumDragDistance)
                && IsPositiveFinite(maximumLaunchSpeed) && minimumDragDistance <= maximumDragDistance
                && (projectileBody.constraints & (RigidbodyConstraints.FreezePositionX
                    | RigidbodyConstraints.FreezePositionY)) == 0;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void Update()
        {
            if (gameFlow.CurrentState != GameFlowState.Aiming)
            {
                if (isAiming)
                {
                    CancelAim();
                }
                return;
            }

            if (!isAiming && pointerPress.WasPressedThisFrame())
            {
                Pointer pointer = pointerPress.activeControl?.device as Pointer;
                if (pointer != null && PressHitsProjectile(pointer.position.ReadValue()))
                {
                    aimingPointer = pointer;
                    isAiming = true;
                }
            }

            if (!isAiming)
            {
                return;
            }

            if (!aimingPointer.added)
            {
                CancelAim();
                return;
            }

            if (!UpdateAim(aimingPointer.position.ReadValue()))
            {
                CancelAim();
                return;
            }

            // Keep the gesture on its initiating pointer, even if another device becomes active.
            if (!aimingPointer.press.isPressed)
            {
                ReleaseAim();
            }
        }

        private bool PressHitsProjectile(Vector2 screenPosition)
        {
            Ray ray = gameplayCamera.ScreenPointToRay(screenPosition);
            return UnityEngine.Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity,
                UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.collider == projectileCollider;
        }

        private bool UpdateAim(Vector2 screenPosition)
        {
            var plane = new Plane(Vector3.forward, launchAnchor.position);
            Ray ray = gameplayCamera.ScreenPointToRay(screenPosition);
            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            Vector3 rawDragOffset = ray.GetPoint(distance) - launchAnchor.position;
            rawDragOffset.z = 0f;
            clampedDragOffset = LaunchMath.ClampDragOffset(rawDragOffset, maximumDragDistance);
            projectileBody.position = launchAnchor.position + clampedDragOffset;
            Vector3 velocity = LaunchMath.CalculateLaunchVelocity(
                clampedDragOffset, maximumDragDistance, maximumLaunchSpeed);
            Vector3 gravity = projectileBody.useGravity ? UnityEngine.Physics.gravity : Vector3.zero;
            gravity.z = 0f; // Z translation is constrained on the real projectile too.
            trajectoryPreview.Show(projectileBody.position, velocity, gravity);
            return true;
        }

        private void ReleaseAim()
        {
            if (clampedDragOffset.magnitude < minimumDragDistance)
            {
                CancelAim();
                return;
            }

            Vector3 velocity = LaunchMath.CalculateLaunchVelocity(
                clampedDragOffset, maximumDragDistance, maximumLaunchSpeed);
            isAiming = false;
            aimingPointer = null;
            trajectoryPreview.Hide();
            projectileBody.constraints |= RigidbodyConstraints.FreezePositionZ;
            projectileBody.isKinematic = false;
            projectileBody.linearVelocity = Vector3.zero;
            projectileBody.angularVelocity = Vector3.zero;
            projectileBody.AddForce(velocity * projectileBody.mass, ForceMode.Impulse);
            gameFlow.RequestTransition(GameFlowState.ProjectileInFlight);
        }

        public void ResetForAiming()
        {
            // Clear even retained kinematic velocities before preparing the next shot.
            projectileBody.isKinematic = false;
            CancelAim();
            projectileCollider.enabled = true;
        }

        private void PrepareAtAnchor()
        {
            // Clear velocity while dynamic; Unity does not support setting it on kinematic bodies.
            if (!projectileBody.isKinematic)
            {
                projectileBody.linearVelocity = Vector3.zero;
                projectileBody.angularVelocity = Vector3.zero;
            }
            projectileBody.constraints |= RigidbodyConstraints.FreezePositionZ;
            projectileBody.isKinematic = true;
            projectileBody.position = launchAnchor.position;
        }

        private void CancelAim()
        {
            isAiming = false;
            aimingPointer = null;
            clampedDragOffset = Vector3.zero;
            PrepareAtAnchor();
            trajectoryPreview.Hide();
        }

        private void OnDisable()
        {
            pointerPress?.Disable();
            if (isAiming)
            {
                CancelAim();
            }
        }

        private void OnDestroy()
        {
            pointerPress?.Dispose();
        }
    }
}
