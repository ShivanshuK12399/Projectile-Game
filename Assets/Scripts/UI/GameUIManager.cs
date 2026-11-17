using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectileGame.Core;
using ProjectileGame.Launcher;
using ProjectileGame.Level;

namespace ProjectileGame.UI
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("Top HUD")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private Slider powerSlider;

        [Header("Status Panels")]
        [SerializeField] private GameObject successPanel;
        [SerializeField] private TextMeshProUGUI successMessageText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button successRetryButton;

        [SerializeField] private GameObject failurePanel;
        [SerializeField] private TextMeshProUGUI failureMessageText;
        [SerializeField] private Button failureRetryButton;

        [Header("Controls")]
        [SerializeField] private Button hudRetryButton;

        [Header("References")]
        [SerializeField] private ProjectileLauncher launcher;

        private void Start()
        {
            if (hudRetryButton != null) hudRetryButton.onClick.AddListener(OnRetryClicked);
            if (successRetryButton != null) successRetryButton.onClick.AddListener(OnRetryClicked);
            if (failureRetryButton != null) failureRetryButton.onClick.AddListener(OnRetryClicked);
            if (nextLevelButton != null) nextLevelButton.onClick.AddListener(OnNextLevelClicked);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }

            HidePanels();
            UpdateLevelDisplay();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void Update()
        {
            if (launcher != null && powerSlider != null)
            {
                powerSlider.value = launcher.CurrentPowerRatio;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Ready:
                    HidePanels();
                    if (instructionText != null)
                        instructionText.text = "Drag projectile backward to aim and launch!";
                    UpdateLevelDisplay();
                    break;

                case GameState.Aiming:
                    if (instructionText != null)
                        instructionText.text = "Aiming... Release to launch towards the curve!";
                    break;

                case GameState.Launched:
                    if (instructionText != null)
                        instructionText.text = "Flying towards curved ramp...";
                    break;

                case GameState.FollowingSurface:
                    if (instructionText != null)
                        instructionText.text = "Smoothly traversing curved surface!";
                    break;

                case GameState.Success:
                    ShowSuccess();
                    break;

                case GameState.Failure:
                    ShowFailure();
                    break;
            }
        }

        private void HidePanels()
        {
            if (successPanel != null) successPanel.SetActive(false);
            if (failurePanel != null) failurePanel.SetActive(false);
        }

        private void ShowSuccess()
        {
            if (successPanel != null) successPanel.SetActive(true);
            if (successMessageText != null)
            {
                successMessageText.text = "Shot Successful!";
            }
        }

        private void ShowFailure()
        {
            if (failurePanel != null) failurePanel.SetActive(true);
            if (failureMessageText != null)
            {
                failureMessageText.text = "Shot Failed!";
            }
        }

        private void OnRetryClicked()
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RestartCurrentLevel();
            }
        }

        private void OnNextLevelClicked()
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.NextLevel();
            }
        }

        public void UpdateLevelDisplay()
        {
            if (levelText != null && LevelManager.Instance != null)
            {
                levelText.text = $"Level: {LevelManager.Instance.CurrentLevel}";
            }
        }
    }
}

