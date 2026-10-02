using System.Collections.Generic;
using UnityEngine;

namespace ColorBlockJam.Levels
{
    /// <summary>Shared boundary rules used by both the visual editor and runtime board builder.</summary>
    public static class BoardTopology
    {
        private static readonly Vector2Int[] CardinalDirections =
            { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };

        public static bool IsInside(LevelDefinition level, Vector2Int cell) =>
            level != null && cell.x >= 0 && cell.y >= 0 &&
            cell.x < level.BoardWidth && cell.y < level.BoardHeight;

        public static bool IsWall(LevelDefinition level, Vector2Int cell)
        {
            if (level == null) return false;
            foreach (WallDefinition wall in level.Walls)
                if (wall.Position == cell) return true;
            return false;
        }

        public static bool IsGate(LevelDefinition level, Vector2Int cell)
        {
            if (level == null) return false;
            foreach (GateDefinition gate in level.Gates)
                if (gate.GridPlaced && gate.Position == cell) return true;
            return false;
        }

        public static bool IsBoundary(LevelDefinition level, Vector2Int cell) =>
            IsWall(level, cell) || IsGate(level, cell);

        public static bool IsCorner(LevelDefinition level, Vector2Int cell)
        {
            if (!IsWall(level, cell)) return false;
            bool left = IsBoundary(level, cell + Vector2Int.left);
            bool right = IsBoundary(level, cell + Vector2Int.right);
            bool bottom = IsBoundary(level, cell + Vector2Int.down);
            bool top = IsBoundary(level, cell + Vector2Int.up);
            int neighbours = (left ? 1 : 0) + (right ? 1 : 0) + (bottom ? 1 : 0) + (top ? 1 : 0);
            return neighbours == 2 && (left || right) && (bottom || top);
        }

        public static int GetAutomaticRotation(LevelDefinition level, Vector2Int cell)
        {
            bool left = IsBoundary(level, cell + Vector2Int.left);
            bool right = IsBoundary(level, cell + Vector2Int.right);
            bool bottom = IsBoundary(level, cell + Vector2Int.down);
            bool top = IsBoundary(level, cell + Vector2Int.up);

            if (!IsCorner(level, cell)) return left || right ? 0 : 90;
            if (right && bottom) return 0;
            if (left && bottom) return 90;
            if (left && top) return 180;
            if (right && top) return 270;
            return 0;
        }

        public static HashSet<Vector2Int> FindEnclosedCells(LevelDefinition level)
        {
            var enclosed = new HashSet<Vector2Int>();
            if (level == null) return enclosed;

            var exterior = new HashSet<Vector2Int>();
            var pending = new Queue<Vector2Int>();
            for (int x = 0; x < level.BoardWidth; x++)
            {
                AddExterior(level, new Vector2Int(x, 0), exterior, pending);
                AddExterior(level, new Vector2Int(x, level.BoardHeight - 1), exterior, pending);
            }

            for (int y = 0; y < level.BoardHeight; y++)
            {
                AddExterior(level, new Vector2Int(0, y), exterior, pending);
                AddExterior(level, new Vector2Int(level.BoardWidth - 1, y), exterior, pending);
            }

            while (pending.Count > 0)
            {
                Vector2Int current = pending.Dequeue();
                foreach (Vector2Int direction in CardinalDirections)
                    AddExterior(level, current + direction, exterior, pending);
            }

            for (int y = 0; y < level.BoardHeight; y++)
            for (int x = 0; x < level.BoardWidth; x++)
            {
                var cell = new Vector2Int(x, y);
                if (!IsBoundary(level, cell) && !exterior.Contains(cell)) enclosed.Add(cell);
            }

            return enclosed;
        }

        private static void AddExterior(LevelDefinition level, Vector2Int cell, HashSet<Vector2Int> exterior,
            Queue<Vector2Int> pending)
        {
            if (!IsInside(level, cell) || IsBoundary(level, cell) || !exterior.Add(cell)) return;
            pending.Enqueue(cell);
        }
    }
}
