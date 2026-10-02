using System;
using System.Collections.Generic;
using ImpactLab.Gameplay;
using NUnit.Framework;

namespace ImpactLab.Tests.EditMode
{
    public sealed class ShotCounterTests
    {
        [Test]
        public void PositiveMaximumInitializesPublicState()
        {
            var counter = new ShotCounter(3);
            Assert.That(counter.MaximumShots, Is.EqualTo(3));
            Assert.That(counter.RemainingShots, Is.EqualTo(3));
            Assert.That(counter.HasShotsRemaining, Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonpositiveMaximumIsRejected(int maximum)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShotCounter(maximum));
        }

        [Test]
        public void ConsumptionReachesZeroAndNeverGoesNegative()
        {
            var counter = new ShotCounter(3);
            for (int remaining = 2; remaining >= 0; remaining--)
            {
                Assert.That(counter.TryConsume(), Is.True);
                Assert.That(counter.RemainingShots, Is.EqualTo(remaining));
                Assert.That(counter.HasShotsRemaining, Is.EqualTo(remaining > 0));
            }
            Assert.That(counter.TryConsume(), Is.False);
            Assert.That(counter.TryConsume(), Is.False);
            Assert.That(counter.RemainingShots, Is.Zero);
            Assert.That(counter.MaximumShots, Is.EqualTo(3));
        }

        [Test]
        public void ResetRestoresMaximumAndAvailability()
        {
            var counter = new ShotCounter(1);
            counter.TryConsume();
            counter.Reset();
            Assert.That(counter.RemainingShots, Is.EqualTo(1));
            Assert.That(counter.HasShotsRemaining, Is.True);
            Assert.That(counter.TryConsume(), Is.True);
        }

        [Test]
        public void EventsReportOnlyActualChangesOnce()
        {
            var counter = new ShotCounter(2);
            var values = new List<int>();
            counter.RemainingShotsChanged += values.Add;
            counter.Reset();
            Assert.That(values, Is.Empty);
            counter.TryConsume();
            Assert.That(values, Is.EqualTo(new[] { 1 }));
            counter.TryConsume();
            counter.TryConsume();
            Assert.That(values, Is.EqualTo(new[] { 1, 0 }));
            counter.Reset();
            counter.Reset();
            Assert.That(values, Is.EqualTo(new[] { 1, 0, 2 }));
        }
    }
}
