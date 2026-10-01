using ColorBlockJam.Levels;
using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    public sealed class BlockFeatureController : MonoBehaviour
    {
        [SerializeField] private BlockMovementMode movementMode;
        private IceLockFeature iceLock;

        public BlockMovementMode MovementMode => movementMode;

        public void Configure(BlockFeatureData data)
        {
            movementMode = data != null ? data.MovementMode : BlockMovementMode.Free;
            if (iceLock == null) iceLock = GetComponent<IceLockFeature>();
        }

        public bool CanBeginDrag()
        {
            return iceLock == null || !iceLock.IsLocked;
        }

        public Vector2 FilterDragTarget(Vector2 current, Vector2 target)
        {
            switch (movementMode)
            {
                case BlockMovementMode.HorizontalOnly:
                    target.y = current.y;
                    break;
                case BlockMovementMode.VerticalOnly:
                    target.x = current.x;
                    break;
            }

            return target;
        }

        public bool AllowsDirection(Vector2Int direction)
        {
            if (movementMode == BlockMovementMode.HorizontalOnly) return direction.x != 0;
            if (movementMode == BlockMovementMode.VerticalOnly) return direction.y != 0;
            return true;
        }
    }
}
