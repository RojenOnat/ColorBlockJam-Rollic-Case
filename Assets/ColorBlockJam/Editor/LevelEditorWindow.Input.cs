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
        private void HandleBoardInput(Rect boardRect, float cellSize)
        {
            Event current = Event.current;

            if (current.type == EventType.MouseDown && current.button == 1 && activeTool == Tool.Gate &&
                boardRect.Contains(current.mousePosition))
            {
                GateDefinition gate = FindGateAt(current.mousePosition, boardRect, cellSize);
                if (gate != null) RemoveGate(gate);
                current.Use();
                return;
            }

            if (current.button != 0) return;

            if (current.type == EventType.MouseDown)
            {
                // The canvas is drawn before the inspector. Do not consume clicks that
                // belong to the side panels, otherwise their buttons never receive them.
                if (!boardRect.Contains(current.mousePosition)) return;

                if (activeTool == Tool.Gate && boardRect.Contains(current.mousePosition))
                {
                    ReplaceWallWithGate(GetCellAt(current.mousePosition, boardRect, cellSize));
                    current.Use();
                    return;
                }

                BlockDefinition hitBlock = FindBlockAt(current.mousePosition, boardRect, cellSize);
                WallDefinition hitWall = FindWallAt(current.mousePosition, boardRect, cellSize);
                GateDefinition hitGate = FindGateAt(current.mousePosition, boardRect, cellSize);

                if (activeTool == Tool.Eraser)
                {
                    if (hitBlock != null) RemoveBlock(hitBlock);
                    else if (hitWall != null) RemoveWall(hitWall);
                    else if (hitGate != null) RemoveGate(hitGate);
                    current.Use();
                    return;
                }

                if (activeTool == Tool.Select)
                {
                    selectedBlock = hitBlock;
                    selectedWall = hitBlock == null && hitWall != null ? hitWall : null;
                    selectedGate = hitBlock == null && hitWall == null ? hitGate : null;
                    draggingBlock = selectedBlock != null;
                    Repaint();
                    current.Use();
                    return;
                }

                if (activeTool == Tool.Feature)
                {
                    selectedBlock = hitBlock;
                    selectedWall = null;
                    selectedGate = null;
                    draggingBlock = false;
                    current.Use();
                    return;
                }

                if (activeTool == Tool.Wall && boardRect.Contains(current.mousePosition))
                {
                    ToggleWall(GetCellAt(current.mousePosition, boardRect, cellSize));
                    current.Use();
                    return;
                }

                if (activeTool == Tool.Block && boardRect.Contains(current.mousePosition))
                {
                    ToggleBlock(GetCellAt(current.mousePosition, boardRect, cellSize));
                    current.Use();
                }
            }
            else if (current.type == EventType.MouseDrag && draggingBlock && selectedBlock != null)
            {
                Vector2Int cell = GetCellAt(current.mousePosition, boardRect, cellSize);
                MoveBlock(selectedBlock, cell);
                current.Use();
            }
            else if (current.type == EventType.MouseUp)
            {
                draggingBlock = false;
            }
        }

    }
}
