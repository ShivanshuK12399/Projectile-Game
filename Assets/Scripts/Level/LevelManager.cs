using UnityEngine;
using ProjectileGame.Core;
using ProjectileGame.Launcher;
using ProjectileGame.Projectile;
using ProjectileGame.Surface;

namespace ProjectileGame.Level
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private ProjectileLauncher launcher;
        [SerializeField] private ProjectileController projectile;
        [SerializeField] private GameObject level1Root;
        [SerializeField] private GameObject level2Root;

        [Header("State")]
        [SerializeField] private int currentLevel = 1;

        public int CurrentLevel => currentLevel;
        public GameObject Level1Root => level1Root;
        public GameObject Level2Root => level2Root;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            LoadLevel(1);
        }

        public void LoadLevel(int levelNumber)
        {
            currentLevel = levelNumber;

            if (level1Root != null) level1Root.SetActive(currentLevel == 1);
            if (level2Root != null) level2Root.SetActive(currentLevel == 2);

            // Re-bake current surface
            CurvedSurface activeSurface = (currentLevel == 1) ? 
                level1Root?.GetComponentInChildren<CurvedSurface>() : 
                level2Root?.GetComponentInChildren<CurvedSurface>();

            if (activeSurface != null)
            {
                activeSurface.BakeCurve();
            }

            RestartCurrentLevel();
            Debug.Log($"[LevelManager] Loaded Level {currentLevel}");
        }

        public void NextLevel()
        {
            int next = (currentLevel == 1) ? 2 : 1;
            LoadLevel(next);
        }

        public void RestartCurrentLevel()
        {
            if (projectile != null)
            {
                projectile.ResetProjectile();
            }

            if (launcher != null)
            {
                launcher.CancelAim();
            }

            GoalTrigger[] goals = Object.FindObjectsByType<GoalTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var g in goals)
            {
                g.ResetTrigger();
            }

            GameManager gm = GameManager.Instance ?? Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.SetState(GameState.Ready);
            }
        }
    }
}

