using System.Collections.Generic;
using UnityEngine;

namespace ProjectileGame.Launcher
{
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPreview : MonoBehaviour
    {
        [Header("Visualization")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private int maxSteps = 45;
        [SerializeField] private float timeStep = 0.04f;
        [SerializeField] private LayerMask collisionLayers;

        private void Awake()
        {
            EnsureLineRenderer();
            if (lineRenderer != null) lineRenderer.enabled = false;
        }

        private void EnsureLineRenderer()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }
        }

        public void ShowTrajectory(Vector2 startPosition, Vector2 launchVelocity, float gravityScale)
        {
            EnsureLineRenderer();
            if (lineRenderer == null) return;

            lineRenderer.enabled = true;
            Vector2 gravity = Physics2D.gravity * gravityScale;

            List<Vector3> points = new List<Vector3>();
            Vector2 currentPosition = startPosition;
            Vector2 currentVelocity = launchVelocity;

            // Offset Z slightly forward so it is always rendered on top of surfaces
            points.Add(new Vector3(currentPosition.x, currentPosition.y, -0.1f));

            for (int i = 0; i < maxSteps; i++)
            {
                Vector2 nextPosition = currentPosition + currentVelocity * timeStep + 0.5f * gravity * (timeStep * timeStep);
                currentVelocity += gravity * timeStep;

                // Check collision ray along this trajectory segment (ignore launcher area)
                Vector2 direction = nextPosition - currentPosition;
                float distance = direction.magnitude;

                if (distance > 0.001f && collisionLayers.value != 0)
                {
                    RaycastHit2D hit = Physics2D.CircleCast(currentPosition, 0.15f, direction.normalized, distance, collisionLayers);
                    if (hit.collider != null && !hit.collider.isTrigger && !hit.collider.CompareTag("Player"))
                    {
                        points.Add(new Vector3(hit.point.x, hit.point.y, -0.1f));
                        break;
                    }
                }

                points.Add(new Vector3(nextPosition.x, nextPosition.y, -0.1f));
                currentPosition = nextPosition;

                // Stop if below bottom play area
                if (currentPosition.y < -7f) break;
            }

            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
        }

        public void HideTrajectory()
        {
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
                lineRenderer.enabled = false;
            }
        }
    }
}

