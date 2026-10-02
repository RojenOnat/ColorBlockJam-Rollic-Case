using ColorBlockJam.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.UI
{
    /// <summary>Connects the pause overlay to the active level session.</summary>
    [DisallowMultipleComponent]
    public sealed class PausePanelController : MonoBehaviour
    {
        [SerializeField] private LevelManager levelManager;
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
            if (levelManager != null) levelManager.PauseChanged += HandlePauseChanged;
            HandlePauseChanged(false);
        }

        private void OnDestroy()
        {
            if (pauseButton != null) pauseButton.onClick.RemoveListener(Pause);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
            if (homeButton != null) homeButton.onClick.RemoveListener(Home);
            if (levelManager != null) levelManager.PauseChanged -= HandlePauseChanged;
        }

        public void Configure(LevelManager manager, GameObject targetPanel, Button targetPauseButton,
            Button targetResumeButton, Button targetRestartButton, Button targetHomeButton)
        {
            levelManager = manager;
            panel = targetPanel;
            pauseButton = targetPauseButton;
            resumeButton = targetResumeButton;
            restartButton = targetRestartButton;
            homeButton = targetHomeButton;
        }

        private void Pause() => levelManager?.PauseCurrentLevel();
        private void Resume() => levelManager?.ResumeCurrentLevel();

        private void Restart()
        {
            HandlePauseChanged(false);
            levelManager?.RestartCurrentLevel();
        }

        private void Home()
        {
            HandlePauseChanged(false);
            levelManager?.ShowMainMenu();
        }

        private void HandlePauseChanged(bool paused)
        {
            if (panel != null) panel.SetActive(paused);
        }
    }
}
