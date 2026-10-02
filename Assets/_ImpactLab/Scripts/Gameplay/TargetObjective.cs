using System;
using ImpactLab.Physics;
using UnityEngine;

namespace ImpactLab.Gameplay
{
    public sealed class TargetObjective : MonoBehaviour
    {
        [SerializeField] private BreakableObject target;
        private BreakableObject observedTarget;

        public bool IsComplete { get; private set; }
        public event Action<TargetObjective> Completed;

        private void OnEnable()
        {
            if (target == null)
            {
                Debug.LogError("TargetObjective requires a BreakableObject target.", this);
                return;
            }
            observedTarget = target;
            observedTarget.Broken += OnTargetBroken;
            if (observedTarget.IsBroken)
            {
                OnTargetBroken(observedTarget);
            }
        }

        private void OnDisable()
        {
            if (observedTarget != null)
            {
                observedTarget.Broken -= OnTargetBroken;
                observedTarget = null;
            }
        }

        private void OnTargetBroken(BreakableObject brokenTarget)
        {
            if (IsComplete)
            {
                return;
            }
            IsComplete = true;
            Completed?.Invoke(this);
        }
    }
}
