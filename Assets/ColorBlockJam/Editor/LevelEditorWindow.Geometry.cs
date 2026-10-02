using System;
using System.Collections.Generic;
using System.IO;
using ColorBlockJam.Board;
using ColorBlockJam.Gameplay;
using ColorBlockJam.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ColorBlockJam.Editor
{
    public sealed partial class LevelEditorWindow
    {
        private BlockDefinition FindBlockAt(Vector2 mousePosition, Rect boardRect, float cellSize)
        {
            if (!boardRect.Contains(mousePosition)) return null;
            Vector2Int cell = GetCellAt(mousePosition, boardRect, cellSize);
            foreach (BlockDefinition block in selectedLevel.Blocks)
            {
                foreach (Vector2Int occupied in BlockShapeLibrary.GetBoardCells(block))
                {
                    if (occupied == cell) return block;
                }
            }
            return null;
        }

        private WallDefinition FindWallAt(Vector2 mousePosition, Rect boardRect, float cellSize)
        {
            if (!boardRect.Contains(mousePosition)) return null;
            Vector2Int cell = GetCellAt(mousePosition, boardRect, cellSize);
            foreach (WallDefinition wall in selectedLevel.Walls)
                if (wall.Position == cell) return wall;
            return null;
        }

        private GateDefinition FindGateAt(Vector2 mousePosition, Rect boardRect, float cellSize)
        {
            foreach (GateDefinition gate in selectedLevel.Gates)
            {
                Rect gateRect = gate.GridPlaced
                    ? GetCellRect(boardRect, cellSize, gate.Position)
                    : GetGateRect(boardRect, cellSize, gate.Edge, gate.EdgePosition, gate.Width);
                if (gateRect.Contains(mousePosition))
                    return gate;
            }
            return null;
        }

        private bool TryGetEdgeHit(Vector2 mousePosition, Rect boardRect, float cellSize,
            out BoardEdge edge, out int edgePosition)
        {
            Rect top = new Rect(boardRect.x, boardRect.y - GateBand, boardRect.width, GateBand);
            Rect bottom = new Rect(boardRect.x, boardRect.yMax, boardRect.width, GateBand);
            Rect left = new Rect(boardRect.x - GateBand, boardRect.y, GateBand, boardRect.height);
            Rect right = new Rect(boardRect.xMax, boardRect.y, GateBand, boardRect.height);

            if (top.Contains(mousePosition))
            {
                edge = BoardEdge.Top;
                edgePosition = Mathf.Clamp(Mathf.FloorToInt((mousePosition.x - boardRect.x) / cellSize), 0, selectedLevel.BoardWidth - 1);
                return true;
            }
            if (bottom.Contains(mousePosition))
            {
                edge = BoardEdge.Bottom;
                edgePosition = Mathf.Clamp(Mathf.FloorToInt((mousePosition.x - boardRect.x) / cellSize), 0, selectedLevel.BoardWidth - 1);
                return true;
            }
            if (left.Contains(mousePosition))
            {
                edge = BoardEdge.Left;
                edgePosition = Mathf.Clamp(selectedLevel.BoardHeight - 1 - Mathf.FloorToInt((mousePosition.y - boardRect.y) / cellSize), 0, selectedLevel.BoardHeight - 1);
                return true;
            }
            if (right.Contains(mousePosition))
            {
                edge = BoardEdge.Right;
                edgePosition = Mathf.Clamp(selectedLevel.BoardHeight - 1 - Mathf.FloorToInt((mousePosition.y - boardRect.y) / cellSize), 0, selectedLevel.BoardHeight - 1);
                return true;
            }

            edge = default;
            edgePosition = 0;
            return false;
        }

        private Rect GetGateRect(Rect boardRect, float cellSize, BoardEdge edge, int edgePosition, int width)
        {
            switch (edge)
            {
                case BoardEdge.Top:
                    return new Rect(boardRect.x + edgePosition * cellSize + 2f, boardRect.y - GateBand,
                        width * cellSize - 4f, GateBand - 2f);
                case BoardEdge.Bottom:
                    return new Rect(boardRect.x + edgePosition * cellSize + 2f, boardRect.yMax + 2f,
                        width * cellSize - 4f, GateBand - 2f);
                case BoardEdge.Left:
                    return new Rect(boardRect.x - GateBand, boardRect.yMax - (edgePosition + width) * cellSize + 2f,
                        GateBand - 2f, width * cellSize - 4f);
                default:
                    return new Rect(boardRect.xMax + 2f, boardRect.yMax - (edgePosition + width) * cellSize + 2f,
                        GateBand - 2f, width * cellSize - 4f);
            }
        }

        private Vector2Int GetCellAt(Vector2 mousePosition, Rect boardRect, float cellSize)
        {
            int x = Mathf.FloorToInt((mousePosition.x - boardRect.x) / cellSize);
            int screenY = Mathf.FloorToInt((mousePosition.y - boardRect.y) / cellSize);
            int y = selectedLevel.BoardHeight - 1 - screenY;
            return new Vector2Int(x, y);
        }

        private Rect GetCellRect(Rect boardRect, float cellSize, Vector2Int cell)
        {
            return new Rect(boardRect.x + cell.x * cellSize,
                boardRect.y + (selectedLevel.BoardHeight - 1 - cell.y) * cellSize,
                cellSize, cellSize);
        }

        private bool IsInsideBoard(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 &&
                   cell.x < selectedLevel.BoardWidth && cell.y < selectedLevel.BoardHeight;
        }

        private string CreateId(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

        private string GetNextLevelAssetPath()
        {
            int number = 1;
            string path;
            do
            {
                path = $"{LevelsFolder}/Level_{number:000}.asset";
                number++;
            } while (AssetDatabase.LoadAssetAtPath<LevelDefinition>(path) != null);

            return path;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static Rect Shrink(Rect rect, float amount)
        {
            return new Rect(rect.x + amount, rect.y + amount,
                Mathf.Max(0f, rect.width - amount * 2f), Mathf.Max(0f, rect.height - amount * 2f));
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static Color GetColor(BlockColor color)
        {
            switch (color)
            {
                case BlockColor.Red: return new Color(0.93f, 0.22f, 0.28f);
                case BlockColor.Blue: return new Color(0.18f, 0.48f, 0.95f);
                case BlockColor.Green: return new Color(0.24f, 0.76f, 0.36f);
                case BlockColor.Yellow: return new Color(1f, 0.78f, 0.12f);
                case BlockColor.Purple: return new Color(0.64f, 0.31f, 0.88f);
                default: return new Color(1f, 0.46f, 0.12f);
            }
        }

        private static GUIStyle CenteredLabelStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 0.84f) }
            };
        }

        private static GUIStyle CenteredMiniLabelStyle()
        {
            return new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.62f, 0.62f, 0.67f) }
            };
        }
    }
}
