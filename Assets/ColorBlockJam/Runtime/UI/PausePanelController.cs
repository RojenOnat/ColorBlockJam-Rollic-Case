using ColorBlockJam.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.UI
{
    /// <summary>Connects the pause overlay to the active level session.</summary>
    [DisallowMultipleComponent]
    public sealed class PausePanelController : MonoBehaviour
    {
        [SerializeField] private GameFlowController gameFlow;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;

        private void Awake()
        {
            if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (homeButton != null) homeButton.onClick.AddListener(Home);
            if (gameFlow != null) gameFlow.PauseChanged += HandlePauseChanged;
            HandlePauseChanged(false);
        }

        private void OnDestroy()
        {
            if (pauseButton != null) pauseButton.onClick.RemoveListener(Pause);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
            if (homeButton != null) homeButton.onClick.RemoveListener(Home);
            if (gameFlow != null) gameFlow.PauseChanged -= HandlePauseChanged;
        }

        public void Configure(GameFlowController flow, GameObject targetPanel, Button targetPauseButton,
            Button targetResumeButton, Button targetRestartButton, Button targetHomeButton)
        {
            gameFlow = flow;
            panel = targetPanel;
            pauseButton = targetPauseButton;
            resumeButton = targetResumeButton;
            restartButton = targetRestartButton;
            homeButton = targetHomeButton;
        }

        private void Pause()
        {
            gameFlow?.PauseCurrentLevel();
            HandlePauseChanged(gameFlow != null && gameFlow.IsPaused);
        }

        private void Resume()
        {
            gameFlow?.ResumeCurrentLevel();
            HandlePauseChanged(false);
        }

        private void Restart()
        {
            HandlePauseChanged(false);
            gameFlow?.RestartCurrentLevel();
        }

        private void Home()
        {
            HandlePauseChanged(false);
            gameFlow?.ShowMainMenu();
        }

        private void HandlePauseChanged(bool paused)
        {
            if (panel != null) panel.SetActive(paused);
        }
    }
}
