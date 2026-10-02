using ImpactLab.Gameplay;
using NUnit.Framework;

namespace ImpactLab.Tests.EditMode
{
    public sealed class GameFlowStateMachineTests
    {
        [Test]
        public void InitialStateIsAiming()
        {
            var stateMachine = new GameFlowStateMachine();

            Assert.That(stateMachine.CurrentState, Is.EqualTo(GameFlowState.Aiming));
        }

        [Test]
        public void AimingToProjectileInFlightIsValid()
        {
            var stateMachine = new GameFlowStateMachine();

            bool transitioned = stateMachine.TryTransition(GameFlowState.ProjectileInFlight);

            Assert.That(transitioned, Is.True);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(GameFlowState.ProjectileInFlight));
        }

        [Test]
        public void AimingToWonIsInvalid()
        {
            var stateMachine = new GameFlowStateMachine();

            bool transitioned = stateMachine.TryTransition(GameFlowState.Won);

            Assert.That(transitioned, Is.False);
        }

        [Test]
        public void ProjectileInFlightToResolvingPhysicsIsValid()
        {
            var stateMachine = CreateProjectileInFlightStateMachine();

            bool transitioned = stateMachine.TryTransition(GameFlowState.ResolvingPhysics);

            Assert.That(transitioned, Is.True);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(GameFlowState.ResolvingPhysics));
        }

        [Test]
        public void ProjectileInFlightToWonIsValid()
        {
            var stateMachine = CreateProjectileInFlightStateMachine();

            bool transitioned = stateMachine.TryTransition(GameFlowState.Won);

            Assert.That(transitioned, Is.True);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(GameFlowState.Won));
        }

        [Test]
        public void ResolvingPhysicsToAimingIsValid()
        {
            var stateMachine = CreateResolvingPhysicsStateMachine();

            bool transitioned = stateMachine.TryTransition(GameFlowState.Aiming);

            Assert.That(transitioned, Is.True);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(GameFlowState.Aiming));
        }

        [Test]
        public void ResolvingPhysicsToWonIsValid()
        {
            var stateMachine = CreateResolvingPhysicsStateMachine();

            bool transitioned = stateMachine.TryTransition(GameFlowState.Won);

            Assert.That(transitioned, Is.True);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(GameFlowState.Won));
        }

        [Test]
        public void ResolvingPhysicsToFailedIsValid()
        {
            var stateMachine = CreateResolvingPhysicsStateMachine();

            bool transitioned = stateMachine.TryTransition(GameFlowState.Failed);

            Assert.That(transitioned, Is.True);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(GameFlowState.Failed));
        }

        [Test]
        public void WonRejectsNormalOutgoingTransitions()
        {
            var stateMachine = CreateProjectileInFlightStateMachine();
            stateMachine.TryTransition(GameFlowState.Won);

            Assert.That(stateMachine.TryTransition(GameFlowState.Aiming), Is.False);
            Assert.That(stateMachine.TryTransition(GameFlowState.ProjectileInFlight), Is.False);
            Assert.That(stateMachine.TryTransition(GameFlowState.ResolvingPhysics), Is.False);
            Assert.That(stateMachine.TryTransition(GameFlowState.Failed), Is.False);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(GameFlowState.Won));
        }

        [Test]
        public void FailedRejectsNormalOutgoingTransitions()
        {
            var stateMachine = CreateResolvingPhysicsStateMachine();
            stateMachine.TryTransition(GameFlowState.Failed);

            Assert.That(stateMachine.TryTransition(GameFlowState.Aiming), Is.False);
            Assert.That(stateMachine.TryTransition(GameFlowState.ProjectileInFlight), Is.False);
            Assert.That(stateMachine.TryTransition(GameFlowState.ResolvingPhysics), Is.False);
            Assert.That(stateMachine.TryTransition(GameFlowState.Won), Is.False);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(GameFlowState.Failed));
        }

        [Test]
        public void ResetReturnsWonToAiming()
        {
            var stateMachine = CreateProjectileInFlightStateMachine();
            stateMachine.TryTransition(GameFlowState.Won);

            stateMachine.Reset();

            Assert.That(stateMachine.CurrentState, Is.EqualTo(GameFlowState.Aiming));
        }

        [Test]
        public void ResetReturnsFailedToAiming()
        {
            var stateMachine = CreateResolvingPhysicsStateMachine();
            stateMachine.TryTransition(GameFlowState.Failed);

            stateMachine.Reset();

            Assert.That(stateMachine.CurrentState, Is.EqualTo(GameFlowState.Aiming));
        }

        [Test]
        public void InvalidTransitionDoesNotChangeCurrentState()
        {
            var stateMachine = new GameFlowStateMachine();

            stateMachine.TryTransition(GameFlowState.Failed);

            Assert.That(stateMachine.CurrentState, Is.EqualTo(GameFlowState.Aiming));
        }

        [Test]
        public void InvalidTransitionDoesNotEmitStateChanged()
        {
            var stateMachine = new GameFlowStateMachine();
            int eventCount = 0;
            stateMachine.StateChanged += (_, _) => eventCount++;

            stateMachine.TryTransition(GameFlowState.Won);

            Assert.That(eventCount, Is.Zero);
        }

        [Test]
        public void ValidTransitionEmitsStateChangedOnceWithPreviousAndNewStates()
        {
            var stateMachine = new GameFlowStateMachine();
            int eventCount = 0;
            GameFlowState? observedPreviousState = null;
            GameFlowState? observedNewState = null;
            stateMachine.StateChanged += (previousState, newState) =>
            {
                eventCount++;
                observedPreviousState = previousState;
                observedNewState = newState;
            };

            stateMachine.TryTransition(GameFlowState.ProjectileInFlight);

            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(observedPreviousState, Is.EqualTo(GameFlowState.Aiming));
            Assert.That(observedNewState, Is.EqualTo(GameFlowState.ProjectileInFlight));
        }

        [Test]
        public void RequestingCurrentStateAgainIsNotAMeaningfulTransition()
        {
            var stateMachine = new GameFlowStateMachine();
            int eventCount = 0;
            stateMachine.StateChanged += (_, _) => eventCount++;

            bool transitioned = stateMachine.TryTransition(GameFlowState.Aiming);

            Assert.That(transitioned, Is.False);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(GameFlowState.Aiming));
            Assert.That(eventCount, Is.Zero);
        }

        private static GameFlowStateMachine CreateProjectileInFlightStateMachine()
        {
            var stateMachine = new GameFlowStateMachine();
            stateMachine.TryTransition(GameFlowState.ProjectileInFlight);
            return stateMachine;
        }

        private static GameFlowStateMachine CreateResolvingPhysicsStateMachine()
        {
            GameFlowStateMachine stateMachine = CreateProjectileInFlightStateMachine();
            stateMachine.TryTransition(GameFlowState.ResolvingPhysics);
            return stateMachine;
        }
    }
}
