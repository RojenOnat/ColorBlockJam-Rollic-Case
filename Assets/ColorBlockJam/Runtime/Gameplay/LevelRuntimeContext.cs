using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    /// <summary>References owned by one instantiated level attempt.</summary>
    public sealed class LevelRuntimeContext
    {
        public GameObject Root { get; }
        public BoardGridState Board { get; }
        public GameplayInputController Input { get; }
        public LevelCountdown Countdown { get; }

        public LevelRuntimeContext(GameObject root, BoardGridState board, GameplayInputController input,
            LevelCountdown countdown)
        {
            Root = root;
            Board = board;
            Input = input;
            Countdown = countdown;
        }
    }
}
