using System;
using System.Collections;
using ColorBlockJam.Board;
using ColorBlockJam.Configuration;
using ColorBlockJam.Economy;
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
        [SerializeField] private HudLevelView levelView;
        [SerializeField] private HudGoldView goldView;
        [SerializeField] private LevelRewardView rewardView;
        [SerializeField] private LevelPathView levelPathView;

        [Header("Navigation Buttons")]
        [SerializeField] private Button hudRestartButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button failHomeButton;
        [SerializeField] private Button successHomeButton;
        [SerializeField] private Button nextLevelButton;

        private GameObject currentLevelInstance;
        private LevelRuntimeContext currentRuntime;
        private LevelRuntimeFactory runtimeFactory;
        private LevelCountdown currentCountdown;
        private BoardGridState currentBoard;
        private GameplayInputController currentInput;
        private LevelDefinition currentLevel;
        private int currentLevelIndex;
        private bool hasLevelResolved;
        private bool isAwaitingExitResolution;
        private bool isPaused;
        private Coroutine pendingSuccessPresentation;
        private Coroutine pendingTimeoutResolution;

        public LevelDefinition CurrentLevel => currentLevel;
        public int CurrentLevelIndex => currentLevelIndex;
        public bool IsLevelActive => currentRuntime != null && !hasLevelResolved;
        public bool IsPaused => isPaused;
        public event Action<LevelDefinition, int> LevelLoaded;
        public event Action<bool> PauseChanged;

        private void Awake()
        {
            runtimeFactory = new LevelRuntimeFactory(levelRuntimePrefab, visualSettings);
            if (playButton != null) playButton.onClick.AddListener(StartCurrentLevel);
            if (hudRestartButton != null) hudRestartButton.onClick.AddListener(RestartCurrentLevel);
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
            Time.timeScale = 1f;
            if (playButton != null) playButton.onClick.RemoveListener(StartCurrentLevel);
            if (hudRestartButton != null) hudRestartButton.onClick.RemoveListener(RestartCurrentLevel);
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
            CancelPendingSuccessPresentation();
            currentLevel = level;
            LevelProgress.CurrentLevelIndex = currentLevelIndex;
            levelPathView?.Refresh(currentLevelIndex);
            levelView?.SetLevelNumber(currentLevelIndex + 1);
            goldView?.Refresh();
            rewardView?.SetReward(currentLevel.RewardGold);
            hasLevelResolved = false;
            isAwaitingExitResolution = false;
            SetPaused(false);

            SetScreenState(showMenu: false, showHud: true, showEndGame: false);
            DestroyCurrentLevel();

            if (!runtimeFactory.TryCreate(currentLevel, currentLevelIndex + 1, out currentRuntime)) return false;

            currentLevelInstance = currentRuntime.Root;
            currentBoard = currentRuntime.Board;
            currentInput = currentRuntime.Input;
            currentCountdown = currentRuntime.Countdown;
            currentBoard.RemainingBlockCountChanged += HandleRemainingBlockCountChanged;
            currentInput.SetInputEnabled(true);

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
            CancelPendingTimeoutResolution();
            StopActiveCountdown();
            SetGameplayInputEnabled(false);
            LevelProgress.UnlockThrough(currentLevelIndex + 1);
            GoldWallet.Add(currentLevel != null ? currentLevel.RewardGold : 0);
            goldView?.Refresh();
            pendingSuccessPresentation = StartCoroutine(ShowSuccessAfterDelay());
        }

        public void LoadNextLevel()
        {
            if (levelCatalog == null || levelCatalog.Count == 0) return;
            StartLevel(Mathf.Min(currentLevelIndex + 1, levelCatalog.Count - 1));
        }

        public void FailCurrentLevel()
        {
            if (hasLevelResolved) return;
            CancelPendingSuccessPresentation();
            CancelPendingTimeoutResolution();
            hasLevelResolved = true;
            StopActiveCountdown();
            SetGameplayInputEnabled(false);
            ShowEndGame(success: false);
        }

        public void ShowMainMenu()
        {
            CancelPendingSuccessPresentation();
            CancelPendingTimeoutResolution();
            StopActiveCountdown();
            SetGameplayInputEnabled(false);
            DestroyCurrentLevel();
            levelPathView?.Refresh(currentLevelIndex);
            SetScreenState(showMenu: true, showHud: false, showEndGame: false);
        }

        public void PauseCurrentLevel()
        {
            if (!IsLevelActive || isPaused) return;
            SetPaused(true);
        }

        public void ResumeCurrentLevel()
        {
            if (!IsLevelActive || !isPaused) return;
            SetPaused(false);
        }

        private void DestroyCurrentLevel()
        {
            CancelPendingSuccessPresentation();
            CancelPendingTimeoutResolution();
            StopActiveCountdown();
            UnbindCountdown();
            UnbindBoard();
            currentInput = null;
            currentRuntime = null;
            if (isPaused)
            {
                isPaused = false;
                Time.timeScale = 1f;
                PauseChanged?.Invoke(false);
            }
            if (currentLevelInstance == null) return;
            currentLevelInstance.SetActive(false);
            Destroy(currentLevelInstance);
            currentLevelInstance = null;
        }

        private void SetPaused(bool paused)
        {
            isPaused = paused && IsLevelActive;
            Time.timeScale = isPaused ? 0f : 1f;
            currentCountdown?.SetPaused(isPaused);
            SetGameplayInputEnabled(!isPaused && IsLevelActive);
            PauseChanged?.Invoke(isPaused);
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
            if (hasLevelResolved || pendingTimeoutResolution != null) return;

            // Let an input release already queued for this frame begin its valid gate exit.
            pendingTimeoutResolution = StartCoroutine(ResolveTimerExpiryAtEndOfFrame());
        }

        private IEnumerator ResolveTimerExpiryAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            pendingTimeoutResolution = null;
            if (hasLevelResolved) yield break;

            if (currentBoard != null && currentBoard.RemainingBlockCount == 0)
            {
                CompleteCurrentLevel();
                yield break;
            }

            SetGameplayInputEnabled(false);
            if (currentBoard == null || !currentBoard.HasBlocksExiting)
            {
                FailCurrentLevel();
                yield break;
            }

            isAwaitingExitResolution = true;
        }

        private void SetGameplayInputEnabled(bool enabled)
        {
            if (currentInput != null) currentInput.SetInputEnabled(enabled);
        }

        private IEnumerator ShowSuccessAfterDelay()
        {
            yield return new WaitForSeconds(GameTuning.Current.SuccessPanelDelay);
            pendingSuccessPresentation = null;
            if (hasLevelResolved && currentLevelInstance != null) ShowEndGame(success: true);
        }

        private void CancelPendingSuccessPresentation()
        {
            if (pendingSuccessPresentation == null) return;
            StopCoroutine(pendingSuccessPresentation);
            pendingSuccessPresentation = null;
        }

        private void CancelPendingTimeoutResolution()
        {
            if (pendingTimeoutResolution == null) return;
            StopCoroutine(pendingTimeoutResolution);
            pendingTimeoutResolution = null;
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
