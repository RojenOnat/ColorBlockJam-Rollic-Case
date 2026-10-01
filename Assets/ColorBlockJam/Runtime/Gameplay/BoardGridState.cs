using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    public sealed class BoardGridState : MonoBehaviour
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private List<Vector2Int> playableCells = new List<Vector2Int>();

        private readonly Dictionary<Vector2Int, GridMovableBlock> occupants =
            new Dictionary<Vector2Int, GridMovableBlock>();
        private readonly HashSet<Vector2Int> playable = new HashSet<Vector2Int>();
        private readonly List<GateGroup> gates = new List<GateGroup>();
        private readonly HashSet<GridMovableBlock> blocks = new HashSet<GridMovableBlock>();

        public float CellSize => cellSize;
        public int RemainingBlockCount => blocks.Count;
        public event Action<int> RemainingBlockCountChanged;
        // Feature systems listen to this single gameplay event instead of talking to one another.
        public event Action<GridMovableBlock> BlockExited;

        private void Awake()
        {
            RebuildRuntimeState();
        }

        public void Configure(int boardWidth, int boardHeight, float size, IEnumerable<Vector2Int> cells)
        {
            width = boardWidth;
            height = boardHeight;
            cellSize = Mathf.Max(0.01f, size);
            playableCells.Clear();
            playableCells.AddRange(cells);
            RebuildRuntimeState();
        }

        public void RebuildRuntimeState()
        {
            playable.Clear();
            foreach (Vector2Int cell in playableCells) playable.Add(cell);

            occupants.Clear();
            blocks.Clear();
            foreach (GridMovableBlock block in GetComponentsInChildren<GridMovableBlock>(true))
                Register(block);

            gates.Clear();
            gates.AddRange(GetComponentsInChildren<GateGroup>(true));
        }

        public void Register(GridMovableBlock block)
        {
            if (block == null) return;
            blocks.Add(block);
            foreach (Vector2Int cell in block.Cells) occupants[cell] = block;
        }

        public void Register(GateGroup gate)
        {
            if (gate != null && !gates.Contains(gate)) gates.Add(gate);
        }

        public void ReleaseCells(GridMovableBlock block)
        {
            if (block == null) return;
            foreach (Vector2Int cell in block.Cells)
                if (occupants.TryGetValue(cell, out GridMovableBlock occupant) && occupant == block)
                    occupants.Remove(cell);
        }

        public void NotifyBlockCompleted(GridMovableBlock block)
        {
            if (block == null || !blocks.Remove(block)) return;
            BlockExited?.Invoke(block);
            RemainingBlockCountChanged?.Invoke(blocks.Count);
        }

        public bool TryGetExitGate(GridMovableBlock block, Vector2Int blockDelta,
            Vector2Int movementDirection, out GateGroup gate)
        {
            foreach (GateGroup candidate in gates)
            {
                if (!candidate.CanAccept(block, blockDelta, movementDirection)) continue;
                gate = candidate;
                return true;
            }

            gate = null;
            return false;
        }

        public bool CanOccupy(GridMovableBlock movingBlock, IReadOnlyList<Vector2Int> cells)
        {
            foreach (Vector2Int cell in cells)
            {
                if (!playable.Contains(cell)) return false;
                if (occupants.TryGetValue(cell, out GridMovableBlock occupant) && occupant != movingBlock)
                    return false;
            }
            return true;
        }

        public bool CanOccupyAtOffset(GridMovableBlock movingBlock, IReadOnlyList<Vector2Int> sourceCells,
            Vector2 offsetInCells)
        {
            const float epsilon = 0.0001f;
            foreach (Vector2Int sourceCell in sourceCells)
            {
                float centerX = sourceCell.x + offsetInCells.x;
                float centerY = sourceCell.y + offsetInCells.y;
                int minX = Mathf.CeilToInt(centerX - 1f + epsilon);
                int maxX = Mathf.FloorToInt(centerX + 1f - epsilon);
                int minY = Mathf.CeilToInt(centerY - 1f + epsilon);
                int maxY = Mathf.FloorToInt(centerY + 1f - epsilon);

                for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!playable.Contains(cell)) return false;
                    if (occupants.TryGetValue(cell, out GridMovableBlock occupant) && occupant != movingBlock)
                        return false;
                }
            }
            return true;
        }

        public void Move(GridMovableBlock block, IReadOnlyList<Vector2Int> oldCells,
            IReadOnlyList<Vector2Int> newCells)
        {
            foreach (Vector2Int cell in oldCells)
                if (occupants.TryGetValue(cell, out GridMovableBlock occupant) && occupant == block)
                    occupants.Remove(cell);

            foreach (Vector2Int cell in newCells) occupants[cell] = block;
        }
    }
}
