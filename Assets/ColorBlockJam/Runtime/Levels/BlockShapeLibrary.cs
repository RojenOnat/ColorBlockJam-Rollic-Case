using System.Collections.Generic;
using UnityEngine;

namespace ColorBlockJam.Levels
{
    public static class BlockShapeLibrary
    {
        private static readonly Vector2Int[] Single = { new Vector2Int(0, 0) };
        private static readonly Vector2Int[] Horizontal2 = { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        private static readonly Vector2Int[] Horizontal3 = { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        private static readonly Vector2Int[] Square2 =
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(0, 1), new Vector2Int(1, 1)
        };
        private static readonly Vector2Int[] L3 =
        {
            new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0)
        };
        private static readonly Vector2Int[] T4 =
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1)
        };

        public static IReadOnlyList<Vector2Int> GetLocalCells(BlockShape shape)
        {
            switch (shape)
            {
                case BlockShape.Horizontal2: return Horizontal2;
                case BlockShape.Horizontal3: return Horizontal3;
                case BlockShape.Square2: return Square2;
                case BlockShape.L3: return L3;
                case BlockShape.T4: return T4;
                default: return Single;
            }
        }

        public static Vector2Int Rotate(Vector2Int cell, int quarterTurns)
        {
            switch ((quarterTurns % 4 + 4) % 4)
            {
                case 1: return new Vector2Int(cell.y, -cell.x);
                case 2: return new Vector2Int(-cell.x, -cell.y);
                case 3: return new Vector2Int(-cell.y, cell.x);
                default: return cell;
            }
        }

        public static IEnumerable<Vector2Int> GetBoardCells(BlockDefinition block)
        {
            IReadOnlyList<Vector2Int> localCells = GetLocalCells(block.Shape);
            for (int i = 0; i < localCells.Count; i++)
            {
                yield return block.Position + Rotate(localCells[i], block.QuarterTurns);
            }
        }
    }
}
