using System;
using ColorBlockJam.Board;
using ColorBlockJam.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LevelManager : MonoBehaviour
    {
        [Header("Level Data")]
        [SerializeField] private LevelCatalog levelCatalog;
        [SerializeField] private BoardVisualSettings visualSettings;
        [SerializeField] private GameObject levelRuntimePrefab;

        [Header("Screen Roots")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gameplayHud;
        [SerializeField] private GameObject endGamePanel;
        [SerializeField] private Button playButton;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private HudTimerView timerView;

        private GameObject currentLevelInstance;
        private LevelCountdown currentCountdown;
        private LevelDefinition currentLevel;
        private int currentLevelIndex;

        public LevelDefinition CurrentLevel => currentLevel;
        public int CurrentLevelIndex => currentLevelIndex;
        public event Action<LevelDefinition, int> LevelLoaded;

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(StartCurrentLevel);
            if (sceneCamera != null) sceneCamera.gameObject.SetActive(false);
        }

        private void Start()
        {
            currentLevelIndex = levelCatalog != null
                ? levelCatalog.ClampIndex(LevelProgress.CurrentLevelIndex)
                : 0;
            ShowMainMenu();
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(StartCurrentLevel);
            UnbindCountdown();
        }

        public void StartCurrentLevel()
        {
            StartLevel(currentLevelIndex);
        }

        public bool StartLevel(int levelIndex)
        {
            if (levelCatalog == null || visualSettings == null || levelRuntimePrefab == null ||
                !levelCatalog.TryGet(levelCatalog.ClampIndex(levelIndex), out LevelDefinition level))
            {
                Debug.LogError("LevelManager needs a LevelCatalog, Board Visual Settings, and Level Runtime Prefab.", this);
                return false;
            }

            currentLevelIndex = levelCatalog.ClampIndex(levelIndex);
            currentLevel = level;
            LevelProgress.CurrentLevelIndex = currentLevelIndex;

            SetScreenState(showMenu: false, showHud: true, showEndGame: false);
            DestroyCurrentLevel();

            currentLevelInstance = Instantiate(levelRuntimePrefab);
            currentLevelInstance.name = $"RuntimeLevel_{currentLevelIndex + 1:000}";
            BoardPreviewGenerator boardBuilder = currentLevelInstance.GetComponentInChildren<BoardPreviewGenerator>();
            if (boardBuilder == null)
            {
                Debug.LogError("Level Runtime Prefab needs a BoardPreviewGenerator on its BoardRoot.", currentLevelInstance);
                DestroyCurrentLevel();
                return false;
            }
            boardBuilder.Configure(currentLevel, visualSettings);
            boardBuilder.Rebuild();
            currentCountdown = currentLevelInstance.GetComponentInChildren<LevelCountdown>();
            if (currentCountdown == null)
            {
                Debug.LogError("Level Runtime Prefab needs a LevelCountdown component on its root.", currentLevelInstance);
                DestroyCurrentLevel();
                return false;
            }

            currentCountdown.TimeChanged += UpdateTimerView;
            currentCountdown.Expired += FailCurrentLevel;
            currentCountdown.StartCountdown(currentLevel.TimerSeconds);

            LevelLoaded?.Invoke(currentLevel, currentLevelIndex);
            return true;
        }

        public void RestartCurrentLevel()
        {
            StartLevel(currentLevelIndex);
        }

        public void CompleteCurrentLevel()
        {
            StopActiveCountdown();
            LevelProgress.UnlockThrough(currentLevelIndex + 1);
            SetScreenState(showMenu: false, showHud: true, showEndGame: true);
        }

        public void LoadNextLevel()
        {
            if (levelCatalog == null || levelCatalog.Count == 0) return;
            StartLevel(Mathf.Min(currentLevelIndex + 1, levelCatalog.Count - 1));
        }

        public void FailCurrentLevel()
        {
            StopActiveCountdown();
            SetScreenState(showMenu: false, showHud: true, showEndGame: true);
        }

        public void ShowMainMenu()
        {
            StopActiveCountdown();
            DestroyCurrentLevel();
            SetScreenState(showMenu: true, showHud: false, showEndGame: false);
        }

        private void DestroyCurrentLevel()
        {
            StopActiveCountdown();
            UnbindCountdown();
            if (currentLevelInstance == null) return;
            currentLevelInstance.SetActive(false);
            Destroy(currentLevelInstance);
            currentLevelInstance = null;
        }

        private void StopActiveCountdown()
        {
            if (currentCountdown != null) currentCountdown.StopCountdown();
        }

        private void UnbindCountdown()
        {
            if (currentCountdown == null) return;
            currentCountdown.TimeChanged -= UpdateTimerView;
            currentCountdown.Expired -= FailCurrentLevel;
            currentCountdown = null;
        }

        private void UpdateTimerView(int seconds)
        {
            if (timerView != null) timerView.SetRemainingSeconds(seconds);
        }

        private void SetScreenState(bool showMenu, bool showHud, bool showEndGame)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(showMenu);
            if (gameplayHud != null) gameplayHud.SetActive(showHud);
            if (endGamePanel != null) endGamePanel.SetActive(showEndGame);
        }
    }
}
