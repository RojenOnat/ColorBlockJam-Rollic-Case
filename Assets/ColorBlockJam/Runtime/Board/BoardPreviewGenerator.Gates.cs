using System.Collections.Generic;
using ColorBlockJam.Gameplay;
using ColorBlockJam.Levels;
using UnityEngine;

namespace ColorBlockJam.Board
{
    public sealed partial class BoardPreviewGenerator
    {
        private void BuildGates(Transform root, HashSet<Vector2Int> enclosedCells, BoardGridState board)
        {
            if (visualSettings.GatePrefab == null) return;
            Transform gateRoot = CreateGroup("Gates", root);

            var remainingGridGates = new HashSet<GateDefinition>();
            foreach (GateDefinition gate in level.Gates)
            {
                if (gate.GridPlaced) remainingGridGates.Add(gate);
            }

            int groupIndex = 0;
            while (remainingGridGates.Count > 0)
            {
                GateDefinition seed = null;
                foreach (GateDefinition candidate in remainingGridGates)
                {
                    seed = candidate;
                    break;
                }

                HashSet<GateDefinition> component = CollectConnectedGates(seed, remainingGridGates);
                Vector3 groupCenter = GetGateGroupCenter(component);
                Transform group = CreateGroup($"GateGroup_{seed.Color}_{groupIndex++}", gateRoot);
                group.localPosition = groupCenter;
                group.gameObject.AddComponent<ColorIdentity>().Configure(seed.Color);
                GateGroup gateGroup = group.gameObject.AddComponent<GateGroup>();
                var gateCells = new List<Vector2Int>();
                foreach (GateDefinition gate in component) gateCells.Add(gate.Position);
                gateGroup.Configure(gateCells, GetGateExitDirection(component, enclosedCells));
                board.Register(gateGroup);

                foreach (GateDefinition gate in component)
                {
                    Quaternion gridRotation = Quaternion.Euler(0f, gate.QuarterTurns * 90f, 0f) *
                                              visualSettings.GateBaseRotation;
                    Vector3 gridPosition = CellCenter(gate.Position.x, gate.Position.y) - groupCenter +
                                           gridRotation * visualSettings.GateOffset;
                    Create(visualSettings.GatePrefab, gridPosition, gridRotation, group,
                        $"GatePart_{gate.Position.x}_{gate.Position.y}");
                }

                ApplyMaterial(group.gameObject, visualSettings.GateMaterial);
                ApplyColor(group.gameObject, seed.Color);
                CreateGateArrow(group, seed.Color, gateGroup.ExitDirection);
            }

            foreach (GateDefinition gate in level.Gates)
            {
                if (gate.GridPlaced) continue;
                int edgeLength = GetEdgeLength(gate.Edge);
                int gateWidth = Mathf.Clamp(gate.Width, 1, edgeLength);
                int start = Mathf.Clamp(gate.EdgePosition, 0, Mathf.Max(0, edgeLength - gateWidth));
                Vector3 gatePosition = EdgeSlotCenter(gate.Edge, start, gateWidth);
                Quaternion rotation = EdgeRotation(gate.Edge) * visualSettings.GateBaseRotation;
                Transform group = CreateGroup($"GateGroup_{gate.Color}_{groupIndex++}", gateRoot);
                group.localPosition = gatePosition;
                group.gameObject.AddComponent<ColorIdentity>().Configure(gate.Color);
                GateGroup gateGroup = group.gameObject.AddComponent<GateGroup>();
                gateGroup.Configure(System.Array.Empty<Vector2Int>(), EdgeDirection(gate.Edge));
                board.Register(gateGroup);
                Create(visualSettings.GatePrefab, rotation * visualSettings.GateOffset,
                    rotation, group, $"GatePart_{gate.Edge}_{start}");
                ApplyMaterial(group.gameObject, visualSettings.GateMaterial);
                ApplyColor(group.gameObject, gate.Color);
                CreateGateArrow(group, gate.Color, gateGroup.ExitDirection);
            }
        }

        private void CreateGateArrow(Transform gateGroup, BlockColor color, Vector2Int exitDirection)
        {
            if (visualSettings.GateArrowPrefab == null || exitDirection == Vector2Int.zero) return;

            Quaternion rotation = DirectionRotation(exitDirection) * visualSettings.GateArrowBaseRotation;
            Vector3 arrowPosition = visualSettings.GateArrowOffset;
            arrowPosition.y = 2f;
            GameObject arrow = Create(visualSettings.GateArrowPrefab, arrowPosition,
                rotation, gateGroup, "GateArrow");
            if (arrow.transform.childCount > 0)
            {
                Transform visual = arrow.transform.GetChild(0);
                Vector3 visualPosition = visual.localPosition;
                visualPosition.z = 0f;
                visual.localPosition = visualPosition;
            }
            ApplyMaterial(arrow, visualSettings.GateMaterial);
            ApplyColor(arrow, color, 0.85f);
        }

        private static Quaternion DirectionRotation(Vector2Int direction)
        {
            if (direction == Vector2Int.right) return Quaternion.Euler(0f, 90f, 0f);
            if (direction == Vector2Int.down) return Quaternion.Euler(0f, 180f, 0f);
            if (direction == Vector2Int.left) return Quaternion.Euler(0f, 270f, 0f);
            return Quaternion.identity;
        }

        private HashSet<GateDefinition> CollectConnectedGates(GateDefinition seed,
            HashSet<GateDefinition> remaining)
        {
            var component = new HashSet<GateDefinition>();
            var pending = new Queue<GateDefinition>();
            remaining.Remove(seed);
            component.Add(seed);
            pending.Enqueue(seed);

            Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
            while (pending.Count > 0)
            {
                GateDefinition current = pending.Dequeue();
                foreach (Vector2Int direction in directions)
                {
                    GateDefinition neighbour = GetGridGateAt(current.Position + direction);
                    if (neighbour == null || neighbour.Color != seed.Color || !remaining.Remove(neighbour))
                        continue;
                    component.Add(neighbour);
                    pending.Enqueue(neighbour);
                }
            }
            return component;
        }

        private GateDefinition GetGridGateAt(Vector2Int coordinate)
        {
            foreach (GateDefinition gate in level.Gates)
                if (gate.GridPlaced && gate.Position == coordinate) return gate;
            return null;
        }

        private Vector3 GetGateGroupCenter(HashSet<GateDefinition> component)
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            foreach (GateDefinition gate in component)
            {
                minX = Mathf.Min(minX, gate.Position.x);
                maxX = Mathf.Max(maxX, gate.Position.x);
                minY = Mathf.Min(minY, gate.Position.y);
                maxY = Mathf.Max(maxY, gate.Position.y);
            }
            return (CellCenter(minX, minY) + CellCenter(maxX, maxY)) * 0.5f;
        }

        private static Vector2Int GetGateExitDirection(HashSet<GateDefinition> component,
            HashSet<Vector2Int> enclosedCells)
        {
            Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
            Vector2Int inward = Vector2Int.zero;
            int bestScore = 0;
            foreach (Vector2Int direction in directions)
            {
                int score = 0;
                foreach (GateDefinition gate in component)
                    if (enclosedCells.Contains(gate.Position + direction)) score++;
                if (score <= bestScore) continue;
                bestScore = score;
                inward = direction;
            }
            return -inward;
        }

        private static Vector2Int EdgeDirection(BoardEdge edge)
        {
            switch (edge)
            {
                case BoardEdge.Right: return Vector2Int.right;
                case BoardEdge.Bottom: return Vector2Int.down;
                case BoardEdge.Left: return Vector2Int.left;
                default: return Vector2Int.up;
            }
        }

    }
}
