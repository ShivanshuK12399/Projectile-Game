using System.Collections;
using UnityEngine;

namespace ProjectileGame.Feedback
{
    public class MotionFeedback : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color failureColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color slideColor = new Color(0.4f, 1f, 0.6f);

        [SerializeField] private Vector3 initialScale = new Vector3(3.2f, 3.2f, 1f);
        private Coroutine activeFeedbackRoutine;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (transform.localScale.x > 0.01f)
            {
                initialScale = transform.localScale;
            }
            if (spriteRenderer != null) normalColor = spriteRenderer.color;
        }

        public void PlayLaunchFeedback(Vector2 launchDir)
        {
            StopActive();
            activeFeedbackRoutine = StartCoroutine(LaunchStretchRoutine(launchDir));
        }

        private IEnumerator LaunchStretchRoutine(Vector2 launchDir)
        {
            // Squash and stretch along launch direction
            Vector3 stretchScale = new Vector3(initialScale.x * 1.35f, initialScale.y * 0.75f, initialScale.z);
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(stretchScale, initialScale, t);
                yield return null;
            }

            transform.localScale = initialScale;
            activeFeedbackRoutine = null;
        }

        public void PlayImpactFeedback()
        {
            StopActive();
            activeFeedbackRoutine = StartCoroutine(ImpactPulseRoutine());
        }

        private IEnumerator ImpactPulseRoutine()
        {
            Vector3 pulseScale = initialScale * 1.25f;
            float halfDuration = 0.08f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(initialScale, pulseScale, elapsed / halfDuration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(pulseScale, initialScale, elapsed / halfDuration);
                yield return null;
            }

            transform.localScale = initialScale;
            activeFeedbackRoutine = null;
        }

        public void PlaySlideFeedback()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = slideColor;
            }
        }

        public void PlayFailureFeedback()
        {
            StopActive();
            activeFeedbackRoutine = StartCoroutine(FailureRoutine());
        }

        private IEnumerator FailureRoutine()
        {
            if (spriteRenderer != null)
            {
                // Rapid blink red
                for (int i = 0; i < 3; i++)
                {
                    spriteRenderer.color = failureColor;
                    yield return new WaitForSeconds(0.06f);
                    spriteRenderer.color = normalColor;
                    yield return new WaitForSeconds(0.06f);
                }
                spriteRenderer.color = failureColor;
            }

            // Shrink down
            float duration = 0.25f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, elapsed / duration);
                yield return null;
            }

            transform.localScale = Vector3.zero;
            activeFeedbackRoutine = null;
        }

        private void StopActive()
        {
            if (activeFeedbackRoutine != null)
            {
                StopCoroutine(activeFeedbackRoutine);
                activeFeedbackRoutine = null;
            }
        }

        public void ResetFeedback()
        {
            StopActive();
            transform.localScale = initialScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }
    }
}

