using UnityEngine;

namespace ColorBlockJam.Levels
{
    public enum BlockVisualPart
    {
        Edge,
        Corner,
        Middle,
        InnerCorner
    }

    /// <summary>Shared visual topology rules for cells belonging to one authored block group.</summary>
    public static class BlockTopology
    {
        public static BlockVisualPart GetVisualPart(LevelDefinition level, BlockDefinition block)
        {
            GetNeighbours(level, block, out bool left, out bool right, out bool bottom, out bool top);
            int count = (left ? 1 : 0) + (right ? 1 : 0) + (bottom ? 1 : 0) + (top ? 1 : 0);
            if (count == 4) return BlockVisualPart.Middle;
            if (count == 2 && (left || right) && (bottom || top)) return BlockVisualPart.Corner;
            return BlockVisualPart.Edge;
        }

        public static int GetVisualRotation(LevelDefinition level, BlockDefinition block)
        {
            GetNeighbours(level, block, out bool left, out bool right, out bool bottom, out bool top);
            if (GetVisualPart(level, block) == BlockVisualPart.Corner)
            {
                if (right && bottom) return 0;
                if (left && bottom) return 90;
                if (left && top) return 180;
                if (right && top) return 270;
            }

            if (!top) return 0;
            if (!right) return 90;
            if (!bottom) return 180;
            if (!left) return 270;
            return 0;
        }

        public static BlockDefinition GetBlockAt(LevelDefinition level, Vector2Int cell)
        {
            if (level == null) return null;
            foreach (BlockDefinition block in level.Blocks)
            foreach (Vector2Int occupiedCell in BlockShapeLibrary.GetBoardCells(block))
                if (occupiedCell == cell) return block;
            return null;
        }

        public static bool IsSameGroupAt(LevelDefinition level, BlockDefinition source, Vector2Int cell)
        {
            BlockDefinition neighbour = GetBlockAt(level, cell);
            return neighbour != null && source != null && neighbour.GroupId == source.GroupId;
        }

        private static void GetNeighbours(LevelDefinition level, BlockDefinition block, out bool left,
            out bool right, out bool bottom, out bool top)
        {
            Vector2Int cell = block.Position;
            left = IsSameGroupAt(level, block, cell + Vector2Int.left);
            right = IsSameGroupAt(level, block, cell + Vector2Int.right);
            bottom = IsSameGroupAt(level, block, cell + Vector2Int.down);
            top = IsSameGroupAt(level, block, cell + Vector2Int.up);
        }
    }
}
