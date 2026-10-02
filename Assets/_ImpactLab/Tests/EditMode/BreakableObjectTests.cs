using System;
using ImpactLab.Physics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ImpactLab.Tests.EditMode
{
    public sealed class BreakableObjectTests
    {
        private BreakableTestRig rig;

        [SetUp]
        public void SetUp()
        {
            rig = new BreakableTestRig();
        }

        [TearDown]
        public void TearDown()
        {
            rig.Dispose();
        }

        [Test]
        public void NewBreakableStartsNotBroken()
        {
            Assert.That(rig.Breakable.IsBroken, Is.False);
        }

        [Test]
        public void ImpactBelowThresholdDoesNotBreak()
        {
            Assert.That(rig.Breakable.TryBreak(5f, Vector3.zero), Is.False);
            Assert.That(rig.Breakable.IsBroken, Is.False);
        }

        [Test]
        public void ImpactEqualToThresholdBreaks()
        {
            Assert.That(rig.Breakable.TryBreak(6f, Vector3.zero), Is.True);
        }

        [Test]
        public void ImpactAboveThresholdBreaks()
        {
            Assert.That(rig.Breakable.TryBreak(7f, Vector3.zero), Is.True);
        }

        [Test]
        public void SuccessfulBreakSetsIsBroken()
        {
            rig.Breakable.TryBreak(6f, Vector3.zero);
            Assert.That(rig.Breakable.IsBroken, Is.True);
        }

        [Test]
        public void SuccessfulBreakDisablesIntactCollider()
        {
            rig.Breakable.TryBreak(6f, Vector3.zero);
            Assert.That(rig.IntactCollider.enabled, Is.False);
        }

        [Test]
        public void SuccessfulBreakDisablesIntactVisual()
        {
            rig.Breakable.TryBreak(6f, Vector3.zero);
            Assert.That(rig.IntactVisual.activeSelf, Is.False);
        }

        [Test]
        public void SuccessfulBreakEnablesFracturedRoot()
        {
            rig.Breakable.TryBreak(6f, Vector3.zero);
            Assert.That(rig.FracturedRoot.activeSelf, Is.True);
        }

        [Test]
        public void FragmentsBecomeNonKinematicAfterBreak()
        {
            rig.Breakable.TryBreak(6f, Vector3.zero);
            foreach (Rigidbody fragment in rig.Fragments)
            {
                Assert.That(fragment.isKinematic, Is.False);
            }
        }

        [Test]
        public void BelowThresholdDoesNotEmitBroken()
        {
            int count = 0;
            rig.Breakable.Broken += _ => count++;
            rig.Breakable.TryBreak(5f, Vector3.zero);
            Assert.That(count, Is.Zero);
        }

        [Test]
        public void SuccessfulBreakEmitsBrokenExactlyOnce()
        {
            int count = 0;
            BreakableObject reported = null;
            rig.Breakable.Broken += source => { count++; reported = source; };
            rig.Breakable.TryBreak(6f, Vector3.zero);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(reported, Is.SameAs(rig.Breakable));
        }

        [Test]
        public void RepeatedBreakReturnsFalse()
        {
            rig.Breakable.TryBreak(6f, Vector3.zero);
            Assert.That(rig.Breakable.TryBreak(12f, Vector3.zero), Is.False);
        }

        [Test]
        public void RepeatedBreakDoesNotEmitBrokenAgain()
        {
            int count = 0;
            rig.Breakable.Broken += _ => count++;
            rig.Breakable.TryBreak(6f, Vector3.zero);
            rig.Breakable.TryBreak(12f, Vector3.zero);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(rig.IntactVisual.activeSelf, Is.False);
            Assert.That(rig.FracturedRoot.activeSelf, Is.True);
        }

        [Test]
        public void NegativeImpulseIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => rig.Breakable.TryBreak(-1f, Vector3.zero));
            Assert.That(rig.Breakable.IsBroken, Is.False);
        }

        [Test]
        public void NaNImpulseIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => rig.Breakable.TryBreak(float.NaN, Vector3.zero));
        }

        [Test]
        public void InfiniteImpulseIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => rig.Breakable.TryBreak(float.PositiveInfinity, Vector3.zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => rig.Breakable.TryBreak(float.NegativeInfinity, Vector3.zero));
        }

        [Test]
        public void FragmentsAreDiscoveredUnderInactiveRoot()
        {
            Assert.That(rig.FracturedRoot.activeSelf, Is.False);
            Rigidbody[] inactiveFragments = rig.FracturedRoot.GetComponentsInChildren<Rigidbody>(true);
            Assert.That(inactiveFragments, Is.EquivalentTo(rig.Fragments));
            foreach (Rigidbody fragment in rig.Fragments)
            {
                Assert.That(fragment.gameObject.activeInHierarchy, Is.False);
            }
            Assert.That(rig.Breakable.TryBreak(6f, Vector3.zero), Is.True);
            foreach (Rigidbody fragment in rig.Fragments)
            {
                Assert.That(fragment.isKinematic, Is.False);
            }
        }

        [Test]
        public void BreakDoesNotAlterUnrelatedGameObjects()
        {
            var unrelated = new GameObject("Unrelated");
            try
            {
                unrelated.transform.position = new Vector3(10f, 20f, 30f);
                BoxCollider collider = unrelated.AddComponent<BoxCollider>();
                Rigidbody body = unrelated.AddComponent<Rigidbody>();
                body.isKinematic = true;
                rig.Breakable.TryBreak(6f, Vector3.zero);
                Assert.That(unrelated.activeSelf, Is.True);
                Assert.That(unrelated.transform.position, Is.EqualTo(new Vector3(10f, 20f, 30f)));
                Assert.That(collider.enabled, Is.True);
                Assert.That(body.isKinematic, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unrelated);
            }
        }

        [Test]
        public void NonFiniteImpactPointIsRejected()
        {
            Assert.Throws<ArgumentException>(() => rig.Breakable.TryBreak(6f, new Vector3(float.NaN, 0f, 0f)));
        }

    }

    internal sealed class BreakableTestRig : IDisposable
    {
        public GameObject Root { get; }
        public BreakableObject Breakable { get; }
        public GameObject IntactVisual { get; }
        public GameObject FracturedRoot { get; }
        public BoxCollider IntactCollider { get; }
        public Rigidbody[] Fragments { get; }

        public BreakableTestRig()
        {
            Root = new GameObject("BreakableTestRoot");
            Root.SetActive(false);
            IntactVisual = new GameObject("IntactVisual");
            IntactVisual.transform.SetParent(Root.transform, false);
            FracturedRoot = new GameObject("FracturedRoot");
            FracturedRoot.transform.SetParent(Root.transform, false);
            Fragments = new Rigidbody[2];
            for (int i = 0; i < Fragments.Length; i++)
            {
                var piece = new GameObject("Piece_" + i);
                piece.transform.SetParent(FracturedRoot.transform, false);
                piece.transform.localPosition = new Vector3(i - 0.5f, 0f, 0f);
                piece.AddComponent<BoxCollider>();
                Fragments[i] = piece.AddComponent<Rigidbody>();
                Fragments[i].isKinematic = true;
            }
            IntactCollider = Root.AddComponent<BoxCollider>();
            Breakable = Root.AddComponent<BreakableObject>();
            var serialized = new SerializedObject(Breakable);
            serialized.FindProperty("intactVisualRoot").objectReferenceValue = IntactVisual;
            serialized.FindProperty("fracturedRoot").objectReferenceValue = FracturedRoot;
            serialized.FindProperty("intactCollider").objectReferenceValue = IntactCollider;
            serialized.FindProperty("breakImpulseThreshold").floatValue = 6f;
            serialized.FindProperty("fragmentImpulseScale").floatValue = 0.25f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // EditMode exercises TryBreak's lazy initialization, not runtime Awake.
            IntactVisual.SetActive(true);
            IntactCollider.enabled = true;
            FracturedRoot.SetActive(false);
            Root.SetActive(true);
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Root);
        }
    }
}
