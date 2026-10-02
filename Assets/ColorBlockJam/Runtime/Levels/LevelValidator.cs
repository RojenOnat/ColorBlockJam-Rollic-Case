using System.Collections.Generic;
using UnityEngine;

namespace ColorBlockJam.Levels
{
    public enum ValidationSeverity
    {
        Warning,
        Error
    }

    public readonly struct ValidationIssue
    {
        public ValidationSeverity Severity { get; }
        public string Message { get; }

        public ValidationIssue(ValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }
    }

    public static class LevelValidator
    {
        public static List<ValidationIssue> Validate(LevelDefinition level)
        {
            var issues = new List<ValidationIssue>();
            if (level == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, "No level is selected."));
                return issues;
            }

            if (level.TimerSeconds <= 0)
                issues.Add(new ValidationIssue(ValidationSeverity.Error, "Timer must be greater than zero."));
            var occupiedCells = new Dictionary<Vector2Int, string>();
            var blockColors = new HashSet<BlockColor>();
            var gateColors = new HashSet<BlockColor>();
            var ids = new HashSet<string>();
            var boundaryCells = new HashSet<Vector2Int>();

            foreach (BlockDefinition block in level.Blocks)
            {
                blockColors.Add(block.Color);
                CheckId(block.Id, "block", ids, issues);

                foreach (Vector2Int cell in BlockShapeLibrary.GetBoardCells(block))
                {
                    if (cell.x < 0 || cell.y < 0 || cell.x >= level.BoardWidth || cell.y >= level.BoardHeight)
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error,
                            $"Block {block.Id} extends outside the board at ({cell.x}, {cell.y})."));
                        continue;
                    }

                    if (occupiedCells.TryGetValue(cell, out string otherId))
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error,
                            $"Blocks {otherId} and {block.Id} overlap at ({cell.x}, {cell.y})."));
                    }
                    else
                    {
                        occupiedCells.Add(cell, block.Id);
                    }
                }
            }

            foreach (WallDefinition wall in level.Walls)
            {
                CheckId(wall.Id, "wall", ids, issues);
                if (!IsInside(level, wall.Position))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error,
                        $"Wall {wall.Id} is outside the board at ({wall.Position.x}, {wall.Position.y})."));
                    continue;
                }

                if (!boundaryCells.Add(wall.Position))
                    issues.Add(new ValidationIssue(ValidationSeverity.Error,
                        $"More than one boundary object occupies ({wall.Position.x}, {wall.Position.y})."));
            }

            foreach (GateDefinition gate in level.Gates)
            {
                gateColors.Add(gate.Color);
                CheckId(gate.Id, "gate", ids, issues);
                if (gate.GridPlaced)
                {
                    if (!IsInside(level, gate.Position))
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error,
                            $"Gate {gate.Id} is outside the board at ({gate.Position.x}, {gate.Position.y})."));
                    }
                    else if (!boundaryCells.Add(gate.Position))
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error,
                            $"More than one boundary object occupies ({gate.Position.x}, {gate.Position.y})."));
                    }
                    continue;
                }

                int edgeLength = gate.Edge == BoardEdge.Top || gate.Edge == BoardEdge.Bottom
                    ? level.BoardWidth
                    : level.BoardHeight;
                if (gate.EdgePosition < 0 || gate.EdgePosition + gate.Width > edgeLength)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error,
                        $"Gate {gate.Id} does not fit on the {gate.Edge} edge."));
                }
            }

            foreach (GateDefinition gate in level.Gates)
            {
                if (!gate.GridPlaced || !IsInside(level, gate.Position)) continue;
                bool horizontal = boundaryCells.Contains(gate.Position + Vector2Int.left) &&
                                  boundaryCells.Contains(gate.Position + Vector2Int.right);
                bool vertical = boundaryCells.Contains(gate.Position + Vector2Int.down) &&
                                boundaryCells.Contains(gate.Position + Vector2Int.up);
                if (horizontal == vertical)
                    issues.Add(new ValidationIssue(ValidationSeverity.Error,
                        $"Gate {gate.Id} must be placed on a straight boundary segment."));
            }

            foreach (BlockColor color in blockColors)
            {
                if (!gateColors.Contains(color))
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, $"{color} blocks have no matching gate."));
            }

            foreach (BlockColor color in gateColors)
            {
                if (!blockColors.Contains(color))
                    issues.Add(new ValidationIssue(ValidationSeverity.Warning, $"The {color} gate has no matching block."));
            }

            return issues;
        }

        private static void CheckId(string id, string kind, HashSet<string> ids, List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, $"A {kind} has no id."));
            }
            else if (!ids.Add(id))
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, $"The id '{id}' is used more than once."));
            }
        }

        private static bool IsInside(LevelDefinition level, Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < level.BoardWidth && cell.y < level.BoardHeight;
        }
    }
}
