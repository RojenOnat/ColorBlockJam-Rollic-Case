using ColorBlockJam.Levels;
using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ColorIdentity : MonoBehaviour
    {
        [SerializeField] private BlockColor color;

        public BlockColor Color => color;

        public void Configure(BlockColor value)
        {
            color = value;
        }

        public bool Matches(ColorIdentity other)
        {
            return other != null && color == other.color;
        }
    }
}
