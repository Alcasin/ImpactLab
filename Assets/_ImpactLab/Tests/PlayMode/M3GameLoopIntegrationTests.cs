#if UNITY_EDITOR
using System.Collections;
using ImpactLab.Gameplay;
using ImpactLab.Physics;
using ImpactLab.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ImpactLab.Tests.PlayMode
{
    // SerializedObject configures fixtures; activation invokes the real Unity lifecycle.
    public sealed class M3GameLoopIntegrationTests
    {
        private GameObject root;
        private Rigidbody body;
        private Collider projectileCollider;
        private Transform anchor;
        private LineRenderer line;
        private GameFlowController flow;
        private ProjectileLauncher launcher;
        private ShotResolver resolver;
        private LevelAttemptController attempt;
        private BreakableObject target;
        private TargetObjective objective;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("M3TestFixture");
            root.SetActive(false);
            anchor = Child("Anchor").transform;
            anchor.position = new Vector3(-1.5f, 10f, 0f);
            var projectile = Child("Projectile");
            body = projectile.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            projectileCollider = projectile.AddComponent<SphereCollider>();
            var camera = Child("Camera").AddComponent<Camera>();
            line = Child("Preview").AddComponent<LineRenderer>();
            var preview = line.gameObject.AddComponent<TrajectoryPreview>();
            var flowObject = Child("GameFlow");
            flow = flowObject.AddComponent<GameFlowController>();
            launcher = flowObject.AddComponent<ProjectileLauncher>();
            var serialized = new SerializedObject(launcher);
            SetReference(serialized, "gameplayCamera", camera);
            SetReference(serialized, "gameFlow", flow);
            SetReference(serialized, "launchAnchor", anchor);
            SetReference(serialized, "projectileBody", body);
            SetReference(serialized, "projectileCollider", projectileCollider);
            SetReference(serialized, "trajectoryPreview", preview);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            resolver = flowObject.AddComponent<ShotResolver>();
            serialized = new SerializedObject(resolver);
            var bodies = serialized.FindProperty("monitoredBodies");
            bodies.arraySize = 1;
            bodies.GetArrayElementAtIndex(0).objectReferenceValue = body;
            serialized.FindProperty("minimumObservationTime").floatValue = 0.05f;
            serialized.FindProperty("settleHoldDuration").floatValue = 0.05f;
            serialized.FindProperty("maximumResolutionTime").floatValue = 0.3f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var targetObject = Child("Target");
            targetObject.transform.position = new Vector3(50f, 10f, 0f);
            var intact = new GameObject("IntactVisual");
            intact.transform.SetParent(targetObject.transform, false);
            var fractured = new GameObject("FracturedRoot");
            fractured.transform.SetParent(targetObject.transform, false);
            var collider = targetObject.AddComponent<BoxCollider>();
            target = targetObject.AddComponent<BreakableObject>();
            serialized = new SerializedObject(target);
            SetReference(serialized, "intactVisualRoot", intact);
            SetReference(serialized, "fracturedRoot", fractured);
            SetReference(serialized, "intactCollider", collider);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            objective = targetObject.AddComponent<TargetObjective>();
            serialized = new SerializedObject(objective);
            SetReference(serialized, "target", target);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            attempt = flowObject.AddComponent<LevelAttemptController>();
            serialized = new SerializedObject(attempt);
            SetReference(serialized, "gameFlow", flow);
            SetReference(serialized, "projectileLauncher", launcher);
            SetReference(serialized, "shotResolver", resolver);
            SetReference(serialized, "targetObjective", objective);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
        }

        private GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            return child;
        }

        private static void SetReference(SerializedObject serialized, string name, Object value)
        {
            serialized.FindProperty(name).objectReferenceValue = value;
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                root.SetActive(false);
                Object.DestroyImmediate(root);
            }
        }

        private IEnumerator WaitForResolution()
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while (resolver.IsResolving && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(resolver.IsResolving, Is.False, "Resolution did not complete within the test deadline.");
        }

        [UnityTest]
        public IEnumerator KinematicBodySettlesAndEmitsExactlyOnce()
        {
            int count = 0;
            resolver.Resolved += () => count++;
            resolver.BeginResolution();
            Assert.That(resolver.IsResolving, Is.True);
            yield return WaitForResolution();
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SleepingDynamicBodySettlesBeforeTimeout()
        {
            var serialized = new SerializedObject(resolver);
            serialized.FindProperty("maximumResolutionTime").floatValue = 10f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            body.isKinematic = false;
            body.Sleep();
            Assert.That(body.IsSleeping(), Is.True);
            int count = 0;
            resolver.Resolved += () => count++;
            resolver.BeginResolution();
            yield return WaitForResolution();
            Assert.That(count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CancellationEmitsNothing()
        {
            int count = 0;
            resolver.Resolved += () => count++;
            resolver.BeginResolution();
            resolver.CancelResolution();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(resolver.IsResolving, Is.False);
            Assert.That(count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator MovingBodyResolvesBySafetyTimeout()
        {
            var serialized = new SerializedObject(resolver);
            serialized.FindProperty("minimumObservationTime").floatValue = 10f;
            serialized.FindProperty("minimumWorldY").floatValue = -100f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            body.isKinematic = false;
            body.linearVelocity = Vector3.right * 10f;
            int count = 0;
            resolver.Resolved += () => count++;
            resolver.BeginResolution();
            yield return WaitForResolution();
            Assert.That(body.linearVelocity.magnitude, Is.GreaterThan(0.15f));
            Assert.That(count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DynamicBodyBelowBoundsResolvesWithoutWaitingForSettle()
        {
            var serialized = new SerializedObject(resolver);
            serialized.FindProperty("minimumObservationTime").floatValue = 10f;
            serialized.FindProperty("maximumResolutionTime").floatValue = 10f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            body.isKinematic = false;
            body.position = new Vector3(0f, -6f, 0f);
            resolver.BeginResolution();
            yield return WaitForResolution();
        }

        [Test]
        public void OnlyLaunchTransitionConsumesOneShot()
        {
            int count = 0;
            int reported = -1;
            attempt.RemainingShotsChanged += remaining => { count++; reported = remaining; };
            Assert.That(attempt.RemainingShots, Is.EqualTo(3));
            Assert.That(flow.RequestTransition(GameFlowState.Aiming), Is.False);
            Assert.That(attempt.RemainingShots, Is.EqualTo(3));
            Assert.That(flow.RequestTransition(GameFlowState.ProjectileInFlight), Is.True);
            Assert.That(flow.RequestTransition(GameFlowState.ProjectileInFlight), Is.False);
            Assert.That(attempt.RemainingShots, Is.EqualTo(2));
            Assert.That(resolver.IsResolving, Is.True);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(reported, Is.EqualTo(2));
            Assert.That(attempt.RetryLevel(), Is.False);
        }

        [UnityTest]
        public IEnumerator ResolvedMissResetsProjectileAndReturnsToAiming()
        {
            body.isKinematic = false;
            body.constraints = RigidbodyConstraints.FreezeRotationX;
            body.position = new Vector3(3f, 10f, 0f);
            body.linearVelocity = Vector3.right * 3f;
            body.angularVelocity = Vector3.up;
            projectileCollider.enabled = false;
            line.enabled = true;
            flow.RequestTransition(GameFlowState.ProjectileInFlight);
            yield return WaitForResolution();
            Assert.That(flow.CurrentState, Is.EqualTo(GameFlowState.Aiming));
            Assert.That(attempt.RemainingShots, Is.EqualTo(2));
            Assert.That(body.position, Is.EqualTo(anchor.position));
            Assert.That(body.isKinematic, Is.True);
            Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(body.angularVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezePositionZ));
            Assert.That(projectileCollider.enabled, Is.True);
            Assert.That(line.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator FinalMissFailsAndLaterResolutionCannotChangeTerminalState()
        {
            for (int shot = 0; shot < 3; shot++)
            {
                Assert.That(flow.RequestTransition(GameFlowState.ProjectileInFlight), Is.True);
                yield return WaitForResolution();
                Assert.That(attempt.RemainingShots, Is.EqualTo(2 - shot));
                Assert.That(flow.CurrentState, Is.EqualTo(shot < 2 ? GameFlowState.Aiming : GameFlowState.Failed));
            }
            resolver.BeginResolution();
            yield return WaitForResolution();
            Assert.That(flow.CurrentState, Is.EqualTo(GameFlowState.Failed));
            Assert.That(attempt.RemainingShots, Is.Zero);
        }

        [UnityTest]
        public IEnumerator TargetCompletionDuringFlightWinsWithoutAnotherShot()
        {
            flow.RequestTransition(GameFlowState.ProjectileInFlight);
            Assert.That(target.TryBreak(6f, target.transform.position), Is.True);
            Assert.That(objective.IsComplete, Is.True);
            Assert.That(flow.CurrentState, Is.EqualTo(GameFlowState.Won));
            Assert.That(attempt.RemainingShots, Is.EqualTo(2));
            Assert.That(resolver.IsResolving, Is.False);
            resolver.BeginResolution();
            yield return WaitForResolution();
            Assert.That(flow.CurrentState, Is.EqualTo(GameFlowState.Won));
            Assert.That(attempt.RemainingShots, Is.EqualTo(2));
        }

        [Test]
        public void UnexpectedLaunchWithoutShotsFailsInsteadOfStalling()
        {
            // Exercise the defensive path through the existing public flow reset API.
            for (int shot = 0; shot < 3; shot++)
            {
                flow.RequestTransition(GameFlowState.ProjectileInFlight);
                flow.ResetFlow();
            }
            Assert.That(attempt.RemainingShots, Is.Zero);
            flow.RequestTransition(GameFlowState.ProjectileInFlight);
            Assert.That(flow.CurrentState, Is.EqualTo(GameFlowState.Failed));
            Assert.That(resolver.IsResolving, Is.False);
            Assert.That(attempt.RemainingShots, Is.Zero);
        }

        [UnityTest]
        public IEnumerator TargetCompletionAtResolutionBoundaryWinsBeforeReset()
        {
            flow.StateChanged += (previous, current) =>
            {
                if (current == GameFlowState.ResolvingPhysics)
                {
                    target.TryBreak(6f, target.transform.position);
                }
            };
            body.position = new Vector3(5f, 10f, 0f);
            flow.RequestTransition(GameFlowState.ProjectileInFlight);
            yield return WaitForResolution();
            Assert.That(flow.CurrentState, Is.EqualTo(GameFlowState.Won));
            Assert.That(attempt.RemainingShots, Is.EqualTo(2));
            Assert.That(body.position, Is.Not.EqualTo(anchor.position));
        }

        [Test]
        public void DisableUnsubscribesAndReenableDoesNotDuplicateSubscriptions()
        {
            attempt.enabled = false;
            flow.RequestTransition(GameFlowState.ProjectileInFlight);
            Assert.That(attempt.RemainingShots, Is.EqualTo(3));
            Assert.That(resolver.IsResolving, Is.False);
            flow.ResetFlow();
            attempt.enabled = true;
            flow.RequestTransition(GameFlowState.ProjectileInFlight);
            Assert.That(attempt.RemainingShots, Is.EqualTo(2));
            Assert.That(resolver.IsResolving, Is.True);
        }
    }
}
#endif
