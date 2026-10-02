using ColorBlockJam.Board;
using ColorBlockJam.Levels;
using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    /// <summary>Builds a complete runtime level from the shared level definition.</summary>
    public sealed class LevelRuntimeFactory
    {
        private readonly GameObject runtimePrefab;
        private readonly BoardVisualSettings visualSettings;

        public LevelRuntimeFactory(GameObject runtimePrefab, BoardVisualSettings visualSettings)
        {
            this.runtimePrefab = runtimePrefab;
            this.visualSettings = visualSettings;
        }

        public bool TryCreate(LevelDefinition level, int levelNumber, out LevelRuntimeContext context)
        {
            context = null;
            if (runtimePrefab == null || visualSettings == null || level == null) return false;

            GameObject root = Object.Instantiate(runtimePrefab);
            root.name = $"RuntimeLevel_{levelNumber:000}";

            ApplyPresentation(root, level);
            BoardPreviewGenerator boardBuilder = root.GetComponentInChildren<BoardPreviewGenerator>();
            if (boardBuilder == null)
            {
                Debug.LogError("Level Runtime Prefab needs a BoardPreviewGenerator on its BoardRoot.", root);
                Object.Destroy(root);
                return false;
            }

            boardBuilder.Configure(level, visualSettings);
            boardBuilder.Rebuild();

            BoardGridState board = root.GetComponentInChildren<BoardGridState>();
            GameplayInputController input = root.GetComponentInChildren<GameplayInputController>(true);
            LevelCountdown countdown = root.GetComponentInChildren<LevelCountdown>(true);
            if (board == null || input == null || countdown == null)
            {
                Debug.LogError(
                    "Level Runtime Prefab must provide BoardGridState, GameplayInputController, and LevelCountdown.",
                    root);
                Object.Destroy(root);
                return false;
            }

            input.Configure(board);
            context = new LevelRuntimeContext(root, board, input, countdown);
            return true;
        }

        private static void ApplyPresentation(GameObject root, LevelDefinition level)
        {
            Camera levelCamera = root.GetComponentInChildren<Camera>(true);
            if (!LevelCameraSettingsApplicator.Apply(levelCamera, level.CameraSettings))
                Debug.LogError("Level Runtime Prefab needs a Camera.", root);

            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (light.type != LightType.Directional) continue;
                LevelLightingSettingsApplicator.Apply(light, level.LightingSettings);
                return;
            }

            Debug.LogError("Level Runtime Prefab needs a directional Light.", root);
        }
    }
}
