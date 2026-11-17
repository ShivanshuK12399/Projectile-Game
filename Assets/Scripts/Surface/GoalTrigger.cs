using UnityEngine;
using ProjectileGame.Core;
using ProjectileGame.Projectile;

namespace ProjectileGame.Surface
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class GoalTrigger : MonoBehaviour
    {
        [SerializeField] private ParticleSystem successParticles;
        [SerializeField] private float restSpeedThreshold = 0.35f;
        [SerializeField] private float restDurationRequired = 0.25f;

        private ProjectileController currentProjectile;
        private float restTimer = 0f;
        private float timeInGoal = 0f;
        private bool hasTriggered = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            ProjectileController proj = other.GetComponent<ProjectileController>();
            if (proj != null)
            {
                currentProjectile = proj;
                restTimer = 0f;
                timeInGoal = 0f;
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (hasTriggered) return;
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.Launched &&
                GameManager.Instance.CurrentState != GameState.FollowingSurface) return;

            ProjectileController proj = other.GetComponent<ProjectileController>();
            if (proj == null) return;

            currentProjectile = proj;
            timeInGoal += Time.deltaTime;

            // Check if ball has settled and is at rest inside the basket
            float speed = proj.Velocity.magnitude;
            if (speed <= restSpeedThreshold)
            {
                restTimer += Time.deltaTime;
                if (restTimer >= restDurationRequired)
                {
                    TriggerWin();
                }
            }
            else
            {
                restTimer = 0f;
                // Safety: if the ball is contained inside the basket for over 1.2s, confirm victory
                if (timeInGoal >= 1.2f)
                {
                    TriggerWin();
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<ProjectileController>() == currentProjectile)
            {
                currentProjectile = null;
                restTimer = 0f;
                timeInGoal = 0f;
            }
        }

        private void TriggerWin()
        {
            if (hasTriggered) return;
            hasTriggered = true;

            if (successParticles != null)
            {
                successParticles.Play();
            }

            GameManager.Instance.TriggerSuccess("Ball rested in the target!");
        }

        public void ResetTrigger()
        {
            hasTriggered = false;
            currentProjectile = null;
            restTimer = 0f;
            timeInGoal = 0f;
        }
    }
}

