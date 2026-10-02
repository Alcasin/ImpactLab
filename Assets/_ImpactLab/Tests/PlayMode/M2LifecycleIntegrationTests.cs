#if UNITY_EDITOR
using ImpactLab.Gameplay;
using ImpactLab.Physics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ImpactLab.Tests.PlayMode
{
    // Editor-hosted PlayMode tests use SerializedObject only to configure private scene references.
    public sealed class M2LifecycleIntegrationTests
    {
        private GameObject root;
        private GameObject intactVisual;
        private GameObject fracturedRoot;
        private BoxCollider intactCollider;
        private Rigidbody[] fragments;
        private BreakableObject breakable;
        private GameObject objectiveObject;
        private TargetObjective objective;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("M2LifecycleTestRoot");
            root.SetActive(false);
            intactVisual = new GameObject("IntactVisual");
            intactVisual.transform.SetParent(root.transform, false);
            intactVisual.SetActive(false);
            fracturedRoot = new GameObject("FracturedRoot");
            fracturedRoot.transform.SetParent(root.transform, false);
            fragments = new Rigidbody[2];
            for (int i = 0; i < fragments.Length; i++)
            {
                var piece = new GameObject("Piece_" + i);
                piece.transform.SetParent(fracturedRoot.transform, false);
                piece.transform.localPosition = new Vector3(i - 0.5f, 0f, 0f);
                piece.AddComponent<BoxCollider>();
                fragments[i] = piece.AddComponent<Rigidbody>();
                fragments[i].isKinematic = false;
            }
            intactCollider = root.AddComponent<BoxCollider>();
            intactCollider.enabled = false;
            breakable = root.AddComponent<BreakableObject>();
            var serializedBreakable = new SerializedObject(breakable);
            serializedBreakable.FindProperty("intactVisualRoot").objectReferenceValue = intactVisual;
            serializedBreakable.FindProperty("fracturedRoot").objectReferenceValue = fracturedRoot;
            serializedBreakable.FindProperty("intactCollider").objectReferenceValue = intactCollider;
            serializedBreakable.FindProperty("breakImpulseThreshold").floatValue = 6f;
            serializedBreakable.FindProperty("fragmentImpulseScale").floatValue = 0.25f;
            serializedBreakable.ApplyModifiedPropertiesWithoutUndo();

            objectiveObject = new GameObject("M2ObjectiveTest");
            objectiveObject.SetActive(false);
            objective = objectiveObject.AddComponent<TargetObjective>();
            var serializedObjective = new SerializedObject(objective);
            serializedObjective.FindProperty("target").objectReferenceValue = breakable;
            serializedObjective.ApplyModifiedPropertiesWithoutUndo();

            // In PlayMode these real state changes invoke Awake and OnEnable synchronously.
            root.SetActive(true);
            objectiveObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (objectiveObject != null)
            {
                objectiveObject.SetActive(false);
                Object.DestroyImmediate(objectiveObject);
            }
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InitializationRestoresSafeIntactState()
        {
            // No TryBreak call: the observed initialization must come from Awake.
            Assert.That(breakable.IsBroken, Is.False);
            Assert.That(intactVisual.activeSelf, Is.True);
            Assert.That(intactCollider.enabled, Is.True);
            Assert.That(fracturedRoot.activeSelf, Is.False);
            foreach (Rigidbody fragment in fragments)
            {
                Assert.That(fragment.isKinematic, Is.True);
                Assert.That(fragment.gameObject.activeInHierarchy, Is.False);
            }
        }

        [Test]
        public void ObjectiveObservesTargetAndCompletesExactlyOnce()
        {
            int count = 0;
            TargetObjective reported = null;
            objective.Completed += source => { count++; reported = source; };
            Assert.That(objective.IsComplete, Is.False);

            Assert.That(breakable.TryBreak(5f, Vector3.zero), Is.False);
            Assert.That(objective.IsComplete, Is.False);
            Assert.That(count, Is.Zero);

            Assert.That(breakable.TryBreak(6f, Vector3.zero), Is.True);
            Assert.That(objective.IsComplete, Is.True);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(reported, Is.SameAs(objective));

            Assert.That(breakable.TryBreak(12f, Vector3.zero), Is.False);
            Assert.That(objective.IsComplete, Is.True);
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void DisabledObjectiveUnsubscribesAndCatchesUpOnEnable()
        {
            int count = 0;
            objective.Completed += _ => count++;
            objective.enabled = false;
            Assert.That(breakable.TryBreak(6f, Vector3.zero), Is.True);
            Assert.That(objective.IsComplete, Is.False);
            Assert.That(count, Is.Zero);

            objective.enabled = true;
            Assert.That(objective.IsComplete, Is.True);
            Assert.That(count, Is.EqualTo(1));
            objective.enabled = false;
            objective.enabled = true;
            Assert.That(count, Is.EqualTo(1));
        }
    }
}
#endif
