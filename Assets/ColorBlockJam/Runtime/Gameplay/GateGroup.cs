using System.Collections.Generic;
using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GateGroup : MonoBehaviour
    {
        [SerializeField] private List<Vector2Int> cells = new List<Vector2Int>();
        [SerializeField] private Vector2Int exitDirection;
        [SerializeField] private ColorIdentity colorIdentity;

        private int minX;
        private int maxX;
        private int minY;
        private int maxY;

        public IReadOnlyList<Vector2Int> Cells => cells;
        public Vector2Int ExitDirection => exitDirection;
        public int Width => cells.Count;

        public void Configure(IEnumerable<Vector2Int> gateCells, Vector2Int direction)
        {
            cells.Clear();
            cells.AddRange(gateCells);
            exitDirection = direction;
            if (colorIdentity == null) colorIdentity = GetComponent<ColorIdentity>();
            CacheBounds();
        }

        public bool CanAccept(GridMovableBlock block, Vector2Int blockDelta, Vector2Int movementDirection)
        {
            if (block == null || cells.Count == 0 || movementDirection != exitDirection) return false;

            if (colorIdentity == null || !colorIdentity.Matches(block.ColorIdentity)) return false;

            int blockMinX = int.MaxValue;
            int blockMaxX = int.MinValue;
            int blockMinY = int.MaxValue;
            int blockMaxY = int.MinValue;
            foreach (Vector2Int source in block.Cells)
            {
                Vector2Int cell = source + blockDelta;
                blockMinX = Mathf.Min(blockMinX, cell.x);
                blockMaxX = Mathf.Max(blockMaxX, cell.x);
                blockMinY = Mathf.Min(blockMinY, cell.y);
                blockMaxY = Mathf.Max(blockMaxY, cell.y);
            }

            if (exitDirection == Vector2Int.left || exitDirection == Vector2Int.right)
            {
                int gateX = exitDirection == Vector2Int.left ? blockMinX - 1 : blockMaxX + 1;
                int requiredWidth = blockMaxY - blockMinY + 1;
                return requiredWidth == cells.Count && this.minX == gateX && this.maxX == gateX &&
                       this.minY == blockMinY && this.maxY == blockMaxY;
            }

            int gateY = exitDirection == Vector2Int.down ? blockMinY - 1 : blockMaxY + 1;
            int horizontalWidth = blockMaxX - blockMinX + 1;
            return horizontalWidth == cells.Count && this.minY == gateY && this.maxY == gateY &&
                   this.minX == blockMinX && this.maxX == blockMaxX;
        }

        private void Awake()
        {
            if (colorIdentity == null) colorIdentity = GetComponent<ColorIdentity>();
            CacheBounds();
        }

        private void CacheBounds()
        {
            minX = minY = int.MaxValue;
            maxX = maxY = int.MinValue;
            foreach (Vector2Int cell in cells)
            {
                minX = Mathf.Min(minX, cell.x);
                maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxY = Mathf.Max(maxY, cell.y);
            }
        }
    }
}
