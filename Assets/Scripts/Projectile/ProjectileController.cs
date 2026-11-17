using System.Collections;
using UnityEngine;
using ProjectileGame.Core;
using ProjectileGame.Feedback;

namespace ProjectileGame.Projectile
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class ProjectileController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private CircleCollider2D col;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private MotionFeedback feedback;

        [Header("Starting Settings")]
        [SerializeField] private Vector3 spawnPosition;
        [SerializeField] private float gravityScale = 1.6f;

        [SerializeField] private Vector3 initialScale = new Vector3(3.2f, 3.2f, 1f);
        [SerializeField] private Color initialColor = new Color(1f, 0.85f, 0.2f, 1f);
        private Coroutine followPathCoroutine;

        public float GravityScale => gravityScale;
        public Rigidbody2D Rigidbody
        {
            get
            {
                if (rb == null) rb = GetComponent<Rigidbody2D>();
                return rb;
            }
        }
        public Vector2 Velocity => Rigidbody != null ? Rigidbody.linearVelocity : Vector2.zero;

        private Vector2 preCollisionVelocity;
        public Vector2 PreCollisionVelocity => preCollisionVelocity.sqrMagnitude > 0.05f ? preCollisionVelocity : Velocity;
        public MotionFeedback Feedback => feedback != null ? feedback : (feedback = GetComponent<MotionFeedback>());

        private void FixedUpdate()
        {
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                preCollisionVelocity = rb.linearVelocity;
            }
        }

        private void Awake()
        {
            EnsureComponents();
            if (transform.localScale.x > 0.01f)
            {
                initialScale = transform.localScale;
            }
            if (spriteRenderer != null) initialColor = spriteRenderer.color;

            spawnPosition = transform.position;
            ResetProjectile();
        }

        private void EnsureComponents()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (col == null) col = GetComponent<CircleCollider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
            if (feedback == null) feedback = GetComponent<MotionFeedback>();

            if (rb != null)
            {
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            // Check out of bounds
            if (GameManager.Instance.CurrentState == GameState.Launched ||
                GameManager.Instance.CurrentState == GameState.FollowingSurface)
            {
                if (transform.position.y < GameManager.Instance.LowerBoundaryY)
                {
                    OnOutOfBounds();
                }
            }
        }

        public void Launch(Vector2 initialVelocity)
        {
            if (rb == null) return;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = gravityScale;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = initialVelocity;

            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                trailRenderer.emitting = true;
            }

            if (feedback != null)
            {
                feedback.PlayLaunchFeedback(initialVelocity.normalized);
            }
        }

        public void SetVisualDragPosition(Vector2 dragPos)
        {
            // Kept for backward compatibility: keeps position static at spawnPosition
            transform.position = spawnPosition;
        }

        public void SetVisualAimCharge(Vector2 launchDirection, float chargeRatio)
        {
            // Keep projectile strictly static at its spawn position on the pedestal
            transform.position = spawnPosition;

            // Subtle in-place squash & stretch tension along launch direction
            chargeRatio = Mathf.Clamp01(chargeRatio);
            if (launchDirection.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(launchDirection.y, launchDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            float stretch = 1.0f + 0.18f * chargeRatio;
            float squash = 1.0f - 0.14f * chargeRatio;
            transform.localScale = new Vector3(initialScale.x * stretch, initialScale.y * squash, initialScale.z);

            // Subtle charge glow tint
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(initialColor, new Color(1f, 0.95f, 0.55f, 1f), chargeRatio * 0.7f);
            }
        }

        public void ResetVisualAimCharge()
        {
            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = initialScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = initialColor;
            }
        }

        public void ResetProjectile()
        {
            if (followPathCoroutine != null)
            {
                StopCoroutine(followPathCoroutine);
                followPathCoroutine = null;
            }

            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = initialScale;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = initialColor;
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
                trailRenderer.Clear();
            }

            if (col != null)
            {
                col.enabled = true;
            }
        }

        public void StartFollowingPath(Vector2[] pathPoints, float speed, System.Action onComplete)
        {
            if (followPathCoroutine != null)
            {
                StopCoroutine(followPathCoroutine);
            }

            followPathCoroutine = StartCoroutine(FollowPathRoutine(pathPoints, speed, onComplete));
        }

        private IEnumerator FollowPathRoutine(Vector2[] pathPoints, float speed, System.Action onComplete)
        {
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }

            if (feedback != null)
            {
                feedback.PlaySlideFeedback();
            }

            for (int i = 0; i < pathPoints.Length; i++)
            {
                Vector3 target = pathPoints[i];
                while (Vector2.Distance(transform.position, target) > 0.05f)
                {
                    // Check if cancelled/reset
                    if (GameManager.Instance.CurrentState != GameState.FollowingSurface)
                    {
                        yield break;
                    }

                    transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                    yield return null;
                }
            }

            followPathCoroutine = null;
            onComplete?.Invoke();
        }

        private void OnOutOfBounds()
        {
            if (feedback != null)
            {
                feedback.PlayFailureFeedback();
            }
            GameManager.Instance.TriggerFailure("Projectile dropped out of bounds!");
        }

        public void SetSpawnPosition(Vector3 newSpawn)
        {
            spawnPosition = newSpawn;
            ResetProjectile();
        }

        public void OnValidImpact()
        {
            if (feedback != null)
            {
                feedback.PlayImpactFeedback();
            }
        }

        public void OnInvalidImpact(string reason)
        {
            if (feedback != null)
            {
                feedback.PlayFailureFeedback();
            }
            GameManager.Instance.TriggerFailure(reason);
        }
    }
}

