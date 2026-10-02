using System;
using ImpactLab.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImpactLab.Gameplay
{
    public sealed class LevelAttemptController : MonoBehaviour
    {
        [SerializeField] private GameFlowController gameFlow;
        [SerializeField] private ProjectileLauncher projectileLauncher;
        [SerializeField] private ShotResolver shotResolver;
        [SerializeField] private TargetObjective targetObjective;
        [SerializeField, Min(1)] private int maximumShots = 3;

        private ShotCounter shotCounter;
        public int RemainingShots => shotCounter != null ? shotCounter.RemainingShots : 0;
        public event Action<int> RemainingShotsChanged;

        private void Awake()
        {
            if (maximumShots > 0)
            {
                shotCounter = new ShotCounter(maximumShots);
            }
        }

        private void OnEnable()
        {
            if (shotCounter == null || gameFlow == null || projectileLauncher == null
                || shotResolver == null || targetObjective == null)
            {
                Debug.LogError("LevelAttemptController requires all references and positive maximum shots.", this);
                enabled = false;
                return;
            }
            gameFlow.StateChanged += OnStateChanged;
            shotResolver.Resolved += OnShotResolved;
            targetObjective.Completed += OnTargetCompleted;
            shotCounter.RemainingShotsChanged += OnRemainingShotsChanged;
        }

        private void OnDisable()
        {
            if (gameFlow != null) gameFlow.StateChanged -= OnStateChanged;
            if (shotResolver != null)
            {
                shotResolver.Resolved -= OnShotResolved;
                shotResolver.CancelResolution();
            }
            if (targetObjective != null) targetObjective.Completed -= OnTargetCompleted;
            if (shotCounter != null) shotCounter.RemainingShotsChanged -= OnRemainingShotsChanged;
        }

        private void OnRemainingShotsChanged(int remaining)
        {
            RemainingShotsChanged?.Invoke(remaining);
        }

        private void OnStateChanged(GameFlowState previous, GameFlowState current)
        {
            if (current == GameFlowState.Won || current == GameFlowState.Failed)
            {
                shotResolver.CancelResolution();
                return;
            }
            if (previous != GameFlowState.Aiming || current != GameFlowState.ProjectileInFlight)
            {
                return;
            }
            if (!shotCounter.TryConsume())
            {
                shotResolver.CancelResolution();
                gameFlow.RequestTransition(GameFlowState.ResolvingPhysics);
                gameFlow.RequestTransition(targetObjective.IsComplete ? GameFlowState.Won : GameFlowState.Failed);
                return;
            }
            // Public counter listeners can trigger target completion synchronously.
            if (gameFlow.CurrentState != GameFlowState.ProjectileInFlight)
            {
                return;
            }
            if (targetObjective.IsComplete)
            {
                OnTargetCompleted(targetObjective);
                return;
            }
            shotResolver.BeginResolution();
            if (!shotResolver.IsResolving)
            {
                // Invalid/disabled resolver must not strand an in-flight attempt.
                gameFlow.RequestTransition(GameFlowState.ResolvingPhysics);
                gameFlow.RequestTransition(GameFlowState.Failed);
            }
        }

        private void OnTargetCompleted(TargetObjective objective)
        {
            GameFlowState state = gameFlow.CurrentState;
            if (state == GameFlowState.ProjectileInFlight || state == GameFlowState.ResolvingPhysics)
            {
                shotResolver.CancelResolution();
                gameFlow.RequestTransition(GameFlowState.Won);
            }
        }

        private void OnShotResolved()
        {
            if (gameFlow.CurrentState != GameFlowState.ProjectileInFlight
                || !gameFlow.RequestTransition(GameFlowState.ResolvingPhysics)
                || gameFlow.CurrentState != GameFlowState.ResolvingPhysics)
            {
                return;
            }
            if (targetObjective.IsComplete)
            {
                gameFlow.RequestTransition(GameFlowState.Won);
            }
            else if (shotCounter.HasShotsRemaining)
            {
                projectileLauncher.ResetForAiming();
                gameFlow.RequestTransition(GameFlowState.Aiming);
            }
            else
            {
                gameFlow.RequestTransition(GameFlowState.Failed);
            }
        }

        public bool RetryLevel()
        {
            if (gameFlow == null || (gameFlow.CurrentState != GameFlowState.Won
                && gameFlow.CurrentState != GameFlowState.Failed))
            {
                return false;
            }
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex < 0)
            {
                Debug.LogError("RetryLevel requires the active scene to be in the build scene list.", this);
                return false;
            }
            SceneManager.LoadScene(scene.buildIndex);
            return true;
        }
    }
}
