using System;
using ColorBlockJam.Board;
using ColorBlockJam.Levels;
using ColorBlockJam.UI;
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
        [SerializeField] private GameObject successContent;
        [SerializeField] private GameObject failContent;
        [SerializeField] private Button playButton;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private HudTimerView timerView;
        [SerializeField] private LevelPathView levelPathView;

        [Header("Navigation Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button failHomeButton;
        [SerializeField] private Button successHomeButton;
        [SerializeField] private Button nextLevelButton;

        private GameObject currentLevelInstance;
        private LevelCountdown currentCountdown;
        private BoardGridState currentBoard;
        private GameplayInputController currentInput;
        private LevelDefinition currentLevel;
        private int currentLevelIndex;
        private bool hasLevelResolved;
        private bool isAwaitingExitResolution;

        public LevelDefinition CurrentLevel => currentLevel;
        public int CurrentLevelIndex => currentLevelIndex;
        public event Action<LevelDefinition, int> LevelLoaded;

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(StartCurrentLevel);
            if (retryButton != null) retryButton.onClick.AddListener(RestartCurrentLevel);
            if (failHomeButton != null) failHomeButton.onClick.AddListener(ShowMainMenu);
            if (successHomeButton != null) successHomeButton.onClick.AddListener(ShowMainMenu);
            if (nextLevelButton != null) nextLevelButton.onClick.AddListener(LoadNextLevel);
            if (sceneCamera != null) sceneCamera.gameObject.SetActive(false);
        }

        private void Start()
        {
            currentLevelIndex = levelCatalog != null
                ? levelCatalog.ClampIndex(LevelProgress.CurrentLevelIndex)
                : 0;
            levelPathView?.Refresh(currentLevelIndex);
            ShowMainMenu();
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(StartCurrentLevel);
            if (retryButton != null) retryButton.onClick.RemoveListener(RestartCurrentLevel);
            if (failHomeButton != null) failHomeButton.onClick.RemoveListener(ShowMainMenu);
            if (successHomeButton != null) successHomeButton.onClick.RemoveListener(ShowMainMenu);
            if (nextLevelButton != null) nextLevelButton.onClick.RemoveListener(LoadNextLevel);
            UnbindCountdown();
            UnbindBoard();
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
            levelPathView?.Refresh(currentLevelIndex);
            hasLevelResolved = false;
            isAwaitingExitResolution = false;

            SetScreenState(showMenu: false, showHud: true, showEndGame: false);
            DestroyCurrentLevel();

            currentLevelInstance = Instantiate(levelRuntimePrefab);
            currentLevelInstance.name = $"RuntimeLevel_{currentLevelIndex + 1:000}";
            ApplyCameraSettings(currentLevelInstance, currentLevel.CameraSettings);
            BoardPreviewGenerator boardBuilder = currentLevelInstance.GetComponentInChildren<BoardPreviewGenerator>();
            if (boardBuilder == null)
            {
                Debug.LogError("Level Runtime Prefab needs a BoardPreviewGenerator on its BoardRoot.", currentLevelInstance);
                DestroyCurrentLevel();
                return false;
            }
            boardBuilder.Configure(currentLevel, visualSettings);
            boardBuilder.Rebuild();
            currentBoard = currentLevelInstance.GetComponentInChildren<BoardGridState>();
            if (currentBoard == null)
            {
                Debug.LogError("Level Runtime Prefab could not build a BoardGridState.", currentLevelInstance);
                DestroyCurrentLevel();
                return false;
            }
            currentBoard.RemainingBlockCountChanged += HandleRemainingBlockCountChanged;

            currentInput = currentLevelInstance.GetComponentInChildren<GameplayInputController>(true);
            if (currentInput == null)
            {
                Debug.LogError("Level Runtime Prefab needs a GameplayInputController.", currentLevelInstance);
                DestroyCurrentLevel();
                return false;
            }
            currentInput.SetInputEnabled(true);

            currentCountdown = currentLevelInstance.GetComponentInChildren<LevelCountdown>();
            if (currentCountdown == null)
            {
                Debug.LogError("Level Runtime Prefab needs a LevelCountdown component on its root.", currentLevelInstance);
                DestroyCurrentLevel();
                return false;
            }

            currentCountdown.TimeChanged += UpdateTimerView;
            currentCountdown.Expired += HandleTimerExpired;
            currentCountdown.StartCountdown(currentLevel.TimerSeconds);

            LevelLoaded?.Invoke(currentLevel, currentLevelIndex);
            return true;
        }

        public void RestartCurrentLevel()
        {
            if (levelCatalog == null || levelCatalog.Count == 0) return;
            StartLevel(currentLevelIndex);
        }

        public void CompleteCurrentLevel()
        {
            if (hasLevelResolved) return;
            hasLevelResolved = true;
            StopActiveCountdown();
            SetGameplayInputEnabled(false);
            LevelProgress.UnlockThrough(currentLevelIndex + 1);
            ShowEndGame(success: true);
        }

        public void LoadNextLevel()
        {
            if (levelCatalog == null || levelCatalog.Count == 0) return;
            StartLevel(Mathf.Min(currentLevelIndex + 1, levelCatalog.Count - 1));
        }

        public void FailCurrentLevel()
        {
            if (hasLevelResolved) return;
            hasLevelResolved = true;
            StopActiveCountdown();
            SetGameplayInputEnabled(false);
            ShowEndGame(success: false);
        }

        public void ShowMainMenu()
        {
            StopActiveCountdown();
            SetGameplayInputEnabled(false);
            DestroyCurrentLevel();
            levelPathView?.Refresh(currentLevelIndex);
            SetScreenState(showMenu: true, showHud: false, showEndGame: false);
        }

        private void DestroyCurrentLevel()
        {
            StopActiveCountdown();
            UnbindCountdown();
            UnbindBoard();
            currentInput = null;
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
            currentCountdown.Expired -= HandleTimerExpired;
            currentCountdown = null;
        }

        private void UnbindBoard()
        {
            if (currentBoard == null) return;
            currentBoard.RemainingBlockCountChanged -= HandleRemainingBlockCountChanged;
            currentBoard = null;
        }

        private void HandleRemainingBlockCountChanged(int remainingBlockCount)
        {
            if (remainingBlockCount == 0)
            {
                CompleteCurrentLevel();
                return;
            }

            if (isAwaitingExitResolution && !currentBoard.HasBlocksExiting)
                FailCurrentLevel();
        }

        private void HandleTimerExpired()
        {
            SetGameplayInputEnabled(false);
            if (currentBoard == null || !currentBoard.HasBlocksExiting)
            {
                FailCurrentLevel();
                return;
            }

            isAwaitingExitResolution = true;
        }

        private void SetGameplayInputEnabled(bool enabled)
        {
            if (currentInput != null) currentInput.SetInputEnabled(enabled);
        }

        private static void ApplyCameraSettings(GameObject levelInstance, LevelCameraSettings settings)
        {
            if (levelInstance == null || settings == null) return;

            Camera levelCamera = levelInstance.GetComponentInChildren<Camera>(true);
            if (LevelCameraSettingsApplicator.Apply(levelCamera, settings)) return;
            Debug.LogError("Level Runtime Prefab needs a Camera.", levelInstance);
        }

        private void ShowEndGame(bool success)
        {
            if (successContent != null) successContent.SetActive(success);
            if (failContent != null) failContent.SetActive(!success);
            SetScreenState(showMenu: false, showHud: true, showEndGame: true);
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
