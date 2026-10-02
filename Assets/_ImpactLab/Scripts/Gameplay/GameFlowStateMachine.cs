using System;

namespace ImpactLab.Gameplay
{
    public sealed class GameFlowStateMachine
    {
        public GameFlowState CurrentState { get; private set; } = GameFlowState.Aiming;

        public event Action<GameFlowState, GameFlowState> StateChanged;

        public bool TryTransition(GameFlowState nextState)
        {
            if (nextState == CurrentState || !IsTransitionAllowed(nextState))
            {
                return false;
            }

            GameFlowState previousState = CurrentState;
            CurrentState = nextState;
            StateChanged?.Invoke(previousState, CurrentState);
            return true;
        }

        public void Reset()
        {
            if (CurrentState == GameFlowState.Aiming)
            {
                return;
            }

            GameFlowState previousState = CurrentState;
            CurrentState = GameFlowState.Aiming;
            StateChanged?.Invoke(previousState, CurrentState);
        }

        private bool IsTransitionAllowed(GameFlowState nextState)
        {
            switch (CurrentState)
            {
                case GameFlowState.Aiming:
                    return nextState == GameFlowState.ProjectileInFlight;

                case GameFlowState.ProjectileInFlight:
                    return nextState == GameFlowState.ResolvingPhysics
                        || nextState == GameFlowState.Won;

                case GameFlowState.ResolvingPhysics:
                    return nextState == GameFlowState.Aiming
                        || nextState == GameFlowState.Won
                        || nextState == GameFlowState.Failed;

                case GameFlowState.Won:
                case GameFlowState.Failed:
                default:
                    return false;
            }
        }
    }
}
