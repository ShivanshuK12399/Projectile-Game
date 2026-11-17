using System.Collections.Generic;
using UnityEngine;
using ProjectileGame.Core;
using ProjectileGame.Projectile;

namespace ProjectileGame.Surface
{
    [RequireComponent(typeof(EdgeCollider2D), typeof(LineRenderer))]
    public class CurvedSurface : MonoBehaviour
    {
        [Header("Validation Thresholds")]
        [SerializeField] private float minImpactSpeed = 1.0f;
        [SerializeField] private float maxImpactSpeed = 25.0f;
        [Tooltip("Acceptable angle range (degrees) between velocity and surface tangent at impact")]
        [SerializeField] private float maxAngleDeviation = 75.0f;

        [Header("Curve Configuration")]
        [SerializeField] private Transform[] controlPoints;
        [SerializeField] private int curveResolution = 30;
        [SerializeField] private float slideSpeed = 7.0f;

        [Header("Components")]
        [SerializeField] private EdgeCollider2D edgeCollider;
        [SerializeField] private LineRenderer lineRenderer;

        private Vector2[] bakedCurvePoints;

        private void Awake()
        {
            if (edgeCollider == null) edgeCollider = GetComponent<EdgeCollider2D>();
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();

            BakeCurve();
        }

        public void BakeCurve()
        {
            if (controlPoints == null || controlPoints.Length < 3) return;

            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= curveResolution; i++)
            {
                float t = i / (float)curveResolution;
                Vector2 pt = EvaluateBezier(t);
                points.Add(pt);
            }

            bakedCurvePoints = points.ToArray();

            // Set EdgeCollider points (in local space)
            if (edgeCollider != null)
            {
                Vector2[] localPoints = new Vector2[bakedCurvePoints.Length];
                for (int i = 0; i < bakedCurvePoints.Length; i++)
                {
                    localPoints[i] = transform.InverseTransformPoint(bakedCurvePoints[i]);
                }
                edgeCollider.points = localPoints;
                edgeCollider.edgeRadius = 0.05f;
            }

            // Set LineRenderer points (in world space)
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = bakedCurvePoints.Length;
                for (int i = 0; i < bakedCurvePoints.Length; i++)
                {
                    lineRenderer.SetPosition(i, bakedCurvePoints[i]);
                }
            }
        }

        private Vector2 EvaluateBezier(float t)
        {
            if (controlPoints.Length == 3)
            {
                // Quadratic
                Vector2 p0 = controlPoints[0].position;
                Vector2 p1 = controlPoints[1].position;
                Vector2 p2 = controlPoints[2].position;
                return (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * p1 + t * t * p2;
            }
            else if (controlPoints.Length >= 4)
            {
                // Cubic
                Vector2 p0 = controlPoints[0].position;
                Vector2 p1 = controlPoints[1].position;
                Vector2 p2 = controlPoints[2].position;
                Vector2 p3 = controlPoints[3].position;
                float u = 1 - t;
                return (u * u * u) * p0 + 3 * (u * u) * t * p1 + 3 * u * (t * t) * p2 + (t * t * t) * p3;
            }

            return controlPoints[0].position;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Launched)
                return;

            ProjectileController proj = collision.gameObject.GetComponent<ProjectileController>();
            if (proj == null) return;

            // Use pre-collision flight velocity or relative velocity for physical accuracy
            Vector2 incomingVelocity = proj.PreCollisionVelocity;
            if (incomingVelocity.sqrMagnitude < 0.2f)
            {
                incomingVelocity = collision.relativeVelocity;
            }
            if (incomingVelocity.sqrMagnitude < 0.1f)
            {
                incomingVelocity = proj.Velocity;
            }
            float impactSpeed = incomingVelocity.magnitude;

            ContactPoint2D contact = collision.GetContact(0);
            Vector2 tangent = GetTangentAtPoint(contact.point);

            // Acute angle deviation between velocity vector and curve tangent line
            float rawAngle = Vector2.Angle(incomingVelocity, tangent);
            float angleDeviation = Mathf.Min(rawAngle, 180f - rawAngle);

            bool speedValid = impactSpeed >= minImpactSpeed && impactSpeed <= maxImpactSpeed;
            bool angleValid = angleDeviation <= maxAngleDeviation;

            if (speedValid && angleValid)
            {
                // Successful validated impact: trigger impact feedback and set state
                proj.OnValidImpact();
                GameManager.Instance.SetState(GameState.FollowingSurface);

                // Zero velocity interference: Unity's built-in Rigidbody2D and PhysicsMaterial2D
                // naturally simulate the realistic physical bounce, friction rolling torque, and gravity!
            }
            else
            {
                // Imperfect shot - let it bounce naturally with physics!
                Debug.Log($"[CurvedSurface] Shot hit curve (Speed: {impactSpeed:F1}, AngleDeviation: {angleDeviation:F1}°). Bouncing naturally...");
                if (proj.Feedback != null)
                {
                    proj.Feedback.PlayImpactFeedback();
                }
            }
        }

        private Vector2 GetDownwardTangent(Vector2 point)
        {
            Vector2 tangent = GetTangentAtPoint(point);
            if (tangent.y > 0)
            {
                tangent = -tangent;
            }
            return tangent.normalized;
        }

        private Vector2 GetTangentAtPoint(Vector2 point)
        {
            if (bakedCurvePoints == null || bakedCurvePoints.Length < 2) return Vector2.right;

            int closestIdx = GetClosestPointIndex(point);
            int nextIdx = Mathf.Clamp(closestIdx + 1, 0, bakedCurvePoints.Length - 1);
            int prevIdx = Mathf.Clamp(closestIdx - 1, 0, bakedCurvePoints.Length - 1);

            if (nextIdx != prevIdx)
            {
                return (bakedCurvePoints[nextIdx] - bakedCurvePoints[prevIdx]).normalized;
            }
            return (bakedCurvePoints[1] - bakedCurvePoints[0]).normalized;
        }

        private int GetClosestPointIndex(Vector2 point)
        {
            if (bakedCurvePoints == null) return 0;
            int bestIdx = 0;
            float bestDistSq = float.MaxValue;

            for (int i = 0; i < bakedCurvePoints.Length; i++)
            {
                float d = (bakedCurvePoints[i] - point).sqrMagnitude;
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    bestIdx = i;
                }
            }
            return bestIdx;
        }

        private Vector2[] GetSubPath(int startIndex)
        {
            int count = bakedCurvePoints.Length - startIndex;
            if (count <= 0) return new Vector2[] { bakedCurvePoints[bakedCurvePoints.Length - 1] };

            Vector2[] sub = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                sub[i] = bakedCurvePoints[startIndex + i];
            }
            return sub;
        }

        public void ConfigureValidation(float minSpeed, float maxSpeed, float maxAngle)
        {
            minImpactSpeed = minSpeed;
            maxImpactSpeed = maxSpeed;
            maxAngleDeviation = maxAngle;
        }

        public void SetControlPoints(Transform[] points)
        {
            controlPoints = points;
            BakeCurve();
        }
    }
}

