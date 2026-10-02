using UnityEngine;

namespace ColorBlockJam.Configuration
{
    /// <summary>Designer-authored gameplay feel and presentation values shared by every level.</summary>
    [CreateAssetMenu(fileName = "GameTuning", menuName = "Color Block Jam/Game Tuning")]
    public sealed class GameTuning : ScriptableObject
    {
        private const string ResourcePath = "ColorBlockJam/GameTuning";
        private static GameTuning current;

        [Header("Block Drag")]
        [SerializeField, Range(0f, 2f)] private float dragLiftHeight = 0.5f;
        [SerializeField, Range(0.25f, 2f)] private float dragSensitivity = 1f;

        [Header("Block Exit")]
        [SerializeField, Range(1f, 30f)] private float exitCollapseSpeed = 12.5f;

        [Header("UI Flow")]
        [SerializeField, Range(0f, 3f)] private float successPanelDelay = 1f;

        public float DragLiftHeight => dragLiftHeight;
        public float DragSensitivity => dragSensitivity;
        public float ExitCollapseSpeed => exitCollapseSpeed;
        public float SuccessPanelDelay => successPanelDelay;

        public static GameTuning Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<GameTuning>(ResourcePath);
                if (current != null) return current;

                Debug.LogError($"Missing GameTuning asset at Resources/{ResourcePath}. Using temporary defaults.");
                current = CreateInstance<GameTuning>();
                return current;
            }
        }
    }
}
