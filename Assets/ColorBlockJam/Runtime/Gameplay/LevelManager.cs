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
        public bool HasActiveLevel => currentRuntime != null;

        public event Action<LevelDefinition, int, LevelSessionController> LevelStarted;
        public event Action LevelUnloaded;

        private void Awake()
        {
            runtimeFactory = new LevelRuntimeFactory(levelRuntimePrefab, visualSettings);
            currentLevelIndex = levelCatalog != null
                ? levelCatalog.ClampIndex(LevelProgress.CurrentLevelIndex)
                : 0;
        }

        private void OnDestroy() => UnloadCurrentLevel();

        public bool StartCurrentLevel() => StartLevel(currentLevelIndex);

        public bool StartLevel(int levelIndex)
        {
            if (!TryGetLevel(levelIndex, out int resolvedIndex, out LevelDefinition level)) return false;

            UnloadCurrentLevel();
            if (!runtimeFactory.TryCreate(level, resolvedIndex + 1, out LevelRuntimeContext runtime)) return false;

            currentLevelIndex = resolvedIndex;
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
            return StartLevel(Mathf.Min(currentLevelIndex + 1, levelCatalog.Count - 1));
        }

        public void UnloadCurrentLevel()
        {
            if (currentRuntime == null) return;

            currentSession?.Shutdown();
            LevelUnloaded?.Invoke();
            currentRuntime.Root.SetActive(false);
            Destroy(currentRuntime.Root);
            currentRuntime = null;
            currentSession = null;
            currentLevel = null;
        }

        private bool TryGetLevel(int requestedIndex, out int resolvedIndex, out LevelDefinition level)
        {
            resolvedIndex = 0;
            level = null;
            if (levelCatalog == null || visualSettings == null || levelRuntimePrefab == null)
            {
                Debug.LogError("LevelManager needs a LevelCatalog, Board Visual Settings, and Level Runtime Prefab.", this);
                return false;
            }

            resolvedIndex = levelCatalog.ClampIndex(requestedIndex);
            if (levelCatalog.TryGet(resolvedIndex, out level)) return true;

            Debug.LogError($"No level exists at catalog index {resolvedIndex}.", this);
            return false;
        }
    }
}
