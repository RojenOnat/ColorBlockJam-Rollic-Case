using System;
using ColorBlockJam.Board;
using ColorBlockJam.Levels;
using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    /// <summary>Owns level selection and the create/destroy lifecycle of one runtime level.</summary>
    [DisallowMultipleComponent]
    public sealed class LevelManager : MonoBehaviour
    {
        [Header("Level Data")]
        [SerializeField] private LevelCatalog levelCatalog;
        [SerializeField] private BoardVisualSettings visualSettings;
        [SerializeField] private GameObject levelRuntimePrefab;

        private LevelRuntimeFactory runtimeFactory;
        private LevelRuntimeContext currentRuntime;
        private LevelSessionController currentSession;
        private LevelDefinition currentLevel;
        private int currentLevelIndex;

        public LevelDefinition CurrentLevel => currentLevel;
        public int CurrentLevelIndex => currentLevelIndex;
        public LevelSessionController CurrentSession => currentSession;
        public bool HasActiveLevel => currentRuntime != null && currentRuntime.Root != null;

        public event Action<LevelDefinition, int, LevelSessionController> LevelStarted;
        public event Action LevelUnloaded;

        private void Awake()
        {
            runtimeFactory = new LevelRuntimeFactory(levelRuntimePrefab, visualSettings);
            currentLevelIndex = LevelProgress.CurrentLevelIndex;

            // Keep gameplay presentation consistent across supported mobile devices.
            Application.targetFrameRate = 60;
        }

        private void OnDestroy() => UnloadCurrentLevel();

        public bool StartCurrentLevel() => StartLevel(currentLevelIndex);

        public bool StartLevel(int levelIndex)
        {
            int progressionIndex = Mathf.Max(0, levelIndex);
            if (!TryGetLevel(progressionIndex, out LevelDefinition level)) return false;

            UnloadCurrentLevel();
            if (!runtimeFactory.TryCreate(level, progressionIndex + 1, out LevelRuntimeContext runtime)) return false;

            currentLevelIndex = progressionIndex;
            currentLevel = level;
            currentRuntime = runtime;
            LevelProgress.CurrentLevelIndex = currentLevelIndex;

            currentSession = runtime.Root.AddComponent<LevelSessionController>();
            currentSession.Initialize(level, runtime);
            LevelStarted?.Invoke(level, currentLevelIndex, currentSession);
            currentSession.Begin();
            return true;
        }

        public bool RestartCurrentLevel() => StartLevel(currentLevelIndex);

        public bool LoadNextLevel()
        {
            if (levelCatalog == null || levelCatalog.Count == 0) return false;
            return StartLevel(currentLevelIndex + 1);
        }

        public void SaveFollowingLevelSelection()
        {
            if (levelCatalog == null || levelCatalog.Count == 0) return;
            LevelProgress.CurrentLevelIndex = currentLevelIndex + 1;
        }

        public void UnloadCurrentLevel()
        {
            if (currentRuntime == null) return;

            LevelRuntimeContext runtimeToUnload = currentRuntime;
            LevelSessionController sessionToShutdown = currentSession;
            currentRuntime = null;
            currentSession = null;
            currentLevel = null;

            if (sessionToShutdown != null) sessionToShutdown.Shutdown();
            LevelUnloaded?.Invoke();

            GameObject root = runtimeToUnload.Root;
            if (root != null)
            {
                root.SetActive(false);
                Destroy(root);
            }

            currentLevelIndex = LevelProgress.CurrentLevelIndex;
        }

        private bool TryGetLevel(int progressionIndex, out LevelDefinition level)
        {
            level = null;
            if (levelCatalog == null || visualSettings == null || levelRuntimePrefab == null)
            {
                Debug.LogError("LevelManager needs a LevelCatalog, Board Visual Settings, and Level Runtime Prefab.", this);
                return false;
            }

            int dataIndex = levelCatalog.WrapIndex(progressionIndex);
            if (levelCatalog.TryGet(dataIndex, out level)) return true;

            Debug.LogError($"No level exists at catalog index {dataIndex}.", this);
            return false;
        }
    }
}
