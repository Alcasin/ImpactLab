using System;
using UnityEngine;

namespace ImpactLab.Gameplay
{
    public sealed class GameFlowController : MonoBehaviour
    {
        private readonly GameFlowStateMachine stateMachine = new GameFlowStateMachine();

        public GameFlowState CurrentState => stateMachine.CurrentState;

        public event Action<GameFlowState, GameFlowState> StateChanged
        {
            add => stateMachine.StateChanged += value;
            remove => stateMachine.StateChanged -= value;
        }

        public bool RequestTransition(GameFlowState nextState)
        {
            return stateMachine.TryTransition(nextState);
        }

        public void ResetFlow()
        {
            stateMachine.Reset();
        }
    }
}
