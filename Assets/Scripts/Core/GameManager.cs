using System;
using UnityEngine;

namespace ProjectileGame.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Ready;

        public event Action<GameState> OnStateChanged;
        public event Action<int> OnLevelChanged;

        [Header("Game Limits")]
        [SerializeField] private float lowerBoundaryY = -6.0f;
        [SerializeField] private float flightTimeoutSeconds = 12.0f;

        [Header("Framerate Settings")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool disableVSyncForTargetFps = true;

        private float stateTimer = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            ApplyFramerateSettings();
        }

        public void ApplyFramerateSettings()
        {
            if (disableVSyncForTargetFps)
            {
                QualitySettings.vSyncCount = 0;
            }
            Application.targetFrameRate = targetFrameRate;
            Debug.Log($"[GameManager] Target frame rate set to {targetFrameRate} FPS (vSyncCount: {QualitySettings.vSyncCount})");
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplyFramerateSettings();
            }
        }

        private void Update()
        {
            if (CurrentState == GameState.Launched || CurrentState == GameState.FollowingSurface)
            {
                stateTimer += Time.deltaTime;
                if (stateTimer >= flightTimeoutSeconds)
                {
                    TriggerFailure("Shot timed out!");
                }
            }
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            stateTimer = 0f;
            Debug.Log($"[GameManager] State changed to: {newState}");
            OnStateChanged?.Invoke(newState);
        }

        public void TriggerSuccess(string reason = "Goal reached!")
        {
            if (CurrentState == GameState.Success) return;
            Debug.Log($"[GameManager] Success: {reason}");
            SetState(GameState.Success);
        }

        public void TriggerFailure(string reason = "Failed!")
        {
            if (CurrentState == GameState.Failure || CurrentState == GameState.Success) return;
            Debug.Log($"[GameManager] Failure: {reason}");
            SetState(GameState.Failure);
        }

        public bool CanAim()
        {
            return CurrentState == GameState.Ready || CurrentState == GameState.Aiming;
        }

        public float LowerBoundaryY => lowerBoundaryY;
        public int TargetFrameRate => targetFrameRate;
    }
}

