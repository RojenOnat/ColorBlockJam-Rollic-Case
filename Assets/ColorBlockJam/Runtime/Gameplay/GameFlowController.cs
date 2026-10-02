using System;
using System.Collections;
using ColorBlockJam.Configuration;
using ColorBlockJam.Economy;
using ColorBlockJam.Levels;
using ColorBlockJam.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Gameplay
{
    /// <summary>Connects player navigation and HUD presentation to the level lifecycle.</summary>
    [DisallowMultipleComponent]
    public sealed class GameFlowController : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private Camera sceneCamera;

        [Header("Screen Roots")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gameplayHud;
        [SerializeField] private GameObject endGamePanel;
        [SerializeField] private GameObject successContent;
        [SerializeField] private GameObject failContent;

        [Header("HUD Views")]
        [SerializeField] private HudTimerView timerView;
        [SerializeField] private HudLevelView levelView;
        [SerializeField] private HudGoldView goldView;
        [SerializeField] private LevelRewardView rewardView;
        [SerializeField] private LevelPathView levelPathView;

        [Header("Navigation Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button hudRestartButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button failHomeButton;
        [SerializeField] private Button successHomeButton;
        [SerializeField] private Button nextLevelButton;

        private LevelSessionController currentSession;
        private LevelDefinition currentLevel;
        private int currentLevelIndex;
        private Coroutine pendingSuccessPresentation;

        public LevelSessionController CurrentSession => currentSession;
        public bool IsPaused => currentSession != null && currentSession.IsPaused;
        public event Action<bool> PauseChanged;

        private void Awake()
        {
            BindButtons();
            if (levelManager != null)
            {
                levelManager.LevelStarted += HandleLevelStarted;
                levelManager.LevelUnloaded += HandleLevelUnloaded;
            }
            if (sceneCamera != null) sceneCamera.gameObject.SetActive(false);
        }

        private void Start()
        {
            currentLevelIndex = levelManager != null ? levelManager.CurrentLevelIndex : 0;
            levelPathView?.Refresh(currentLevelIndex);
            ShowMainMenu();
        }

        private void OnDestroy()
        {
            UnbindButtons();
            UnbindSession();
            if (levelManager != null)
            {
                levelManager.LevelStarted -= HandleLevelStarted;
                levelManager.LevelUnloaded -= HandleLevelUnloaded;
            }
        }

        public void StartCurrentLevel() => levelManager?.StartCurrentLevel();
        public void RestartCurrentLevel() => levelManager?.RestartCurrentLevel();
        public void LoadNextLevel() => levelManager?.LoadNextLevel();
        public void PauseCurrentLevel() => currentSession?.Pause();
        public void ResumeCurrentLevel() => currentSession?.Resume();

        public void ShowMainMenu()
        {
            CancelPendingSuccessPresentation();
            UnbindSession();
            levelManager?.UnloadCurrentLevel();
            currentLevelIndex = levelManager != null ? levelManager.CurrentLevelIndex : currentLevelIndex;
            levelPathView?.Refresh(currentLevelIndex);
            goldView?.Refresh();
            SetScreenState(showMenu: true, showHud: false, showEndGame: false);
        }

        private void HandleLevelStarted(LevelDefinition level, int levelIndex, LevelSessionController session)
        {
            CancelPendingSuccessPresentation();
            UnbindSession();
            currentLevel = level;
            currentLevelIndex = levelIndex;
            currentSession = session;
            currentSession.TimeChanged += UpdateTimerView;
            currentSession.Completed += HandleLevelCompleted;
            currentSession.Failed += HandleLevelFailed;
            currentSession.PauseChanged += HandlePauseChanged;

            levelPathView?.Refresh(currentLevelIndex);
            levelView?.SetLevelNumber(currentLevelIndex + 1);
            goldView?.Refresh();
            rewardView?.SetReward(currentLevel.RewardGold);
            SetScreenState(showMenu: false, showHud: true, showEndGame: false);
        }

        private void HandleLevelUnloaded()
        {
            CancelPendingSuccessPresentation();
            UnbindSession();
        }

        private void HandleLevelCompleted()
        {
            LevelProgress.UnlockThrough(currentLevelIndex + 1);
            GoldWallet.Add(currentLevel != null ? currentLevel.RewardGold : 0);
            goldView?.Refresh();
            pendingSuccessPresentation = StartCoroutine(ShowSuccessAfterDelay());
        }

        private void HandleLevelFailed() => ShowEndGame(success: false);

        private IEnumerator ShowSuccessAfterDelay()
        {
            yield return new WaitForSeconds(GameTuning.Current.SuccessPanelDelay);
            pendingSuccessPresentation = null;
            if (currentSession != null) ShowEndGame(success: true);
        }

        private void ShowEndGame(bool success)
        {
            if (successContent != null) successContent.SetActive(success);
            if (failContent != null) failContent.SetActive(!success);
            SetScreenState(showMenu: false, showHud: true, showEndGame: true);
        }

        private void UpdateTimerView(int seconds) => timerView?.SetRemainingSeconds(seconds);

        private void UnbindSession()
        {
            if (currentSession == null) return;
            currentSession.TimeChanged -= UpdateTimerView;
            currentSession.Completed -= HandleLevelCompleted;
            currentSession.Failed -= HandleLevelFailed;
            currentSession.PauseChanged -= HandlePauseChanged;
            currentSession = null;
            currentLevel = null;
        }

        private void HandlePauseChanged(bool paused) => PauseChanged?.Invoke(paused);

        private void SetScreenState(bool showMenu, bool showHud, bool showEndGame)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(showMenu);
            if (gameplayHud != null) gameplayHud.SetActive(showHud);
            if (endGamePanel != null) endGamePanel.SetActive(showEndGame);
        }

        private void BindButtons()
        {
            if (playButton != null) playButton.onClick.AddListener(StartCurrentLevel);
            if (hudRestartButton != null) hudRestartButton.onClick.AddListener(RestartCurrentLevel);
            if (retryButton != null) retryButton.onClick.AddListener(RestartCurrentLevel);
            if (failHomeButton != null) failHomeButton.onClick.AddListener(ShowMainMenu);
            if (successHomeButton != null) successHomeButton.onClick.AddListener(ShowMainMenu);
            if (nextLevelButton != null) nextLevelButton.onClick.AddListener(LoadNextLevel);
        }

        private void UnbindButtons()
        {
            if (playButton != null) playButton.onClick.RemoveListener(StartCurrentLevel);
            if (hudRestartButton != null) hudRestartButton.onClick.RemoveListener(RestartCurrentLevel);
            if (retryButton != null) retryButton.onClick.RemoveListener(RestartCurrentLevel);
            if (failHomeButton != null) failHomeButton.onClick.RemoveListener(ShowMainMenu);
            if (successHomeButton != null) successHomeButton.onClick.RemoveListener(ShowMainMenu);
            if (nextLevelButton != null) nextLevelButton.onClick.RemoveListener(LoadNextLevel);
        }

        private void CancelPendingSuccessPresentation()
        {
            if (pendingSuccessPresentation == null) return;
            StopCoroutine(pendingSuccessPresentation);
            pendingSuccessPresentation = null;
        }
    }
}
