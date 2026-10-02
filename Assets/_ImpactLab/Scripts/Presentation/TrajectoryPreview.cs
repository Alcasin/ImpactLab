using ImpactLab.Physics;
using UnityEngine;

namespace ImpactLab.Presentation
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        [SerializeField, Min(2)] private int sampleCount = 30;
        [SerializeField, Min(0.001f)] private float timeStep = 0.05f;

        private LineRenderer lineRenderer;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            Hide();
        }

        public void Show(Vector3 startPosition, Vector3 initialVelocity, Vector3 gravity)
        {
            if (sampleCount < 2 || timeStep <= 0f || float.IsNaN(timeStep) || float.IsInfinity(timeStep))
            {
                Debug.LogError("TrajectoryPreview requires at least two samples and a finite positive time step.", this);
                Hide();
                return;
            }

            lineRenderer.positionCount = sampleCount;
            for (int i = 0; i < sampleCount; i++)
            {
                lineRenderer.SetPosition(i,
                    LaunchMath.CalculateTrajectoryPoint(startPosition, initialVelocity, gravity, i * timeStep));
            }
            lineRenderer.enabled = true;
        }

        public void Hide()
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        private void OnDisable()
        {
            Hide();
        }
    }
}
