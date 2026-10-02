using System.Collections.Generic;
using ColorBlockJam.Configuration;
using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    public enum BlockMovementState
    {
        Idle,
        Dragging,
        Exiting,
        Completed
    }

    [RequireComponent(typeof(ColorIdentity), typeof(BlockExitMotor), typeof(BlockFeatureController))]
    public sealed class GridMovableBlock : MonoBehaviour
    {
        [SerializeField] private BoardGridState board;
        [SerializeField] private List<Vector2Int> cells = new List<Vector2Int>();
        [SerializeField] private BlockMovementState state;
        [SerializeField] private BlockExitMotor exitMotor;
        [SerializeField] private BlockFeatureController featureController;
        [SerializeField] private IceLockFeature iceLock;

        private const float MovementComparisonEpsilon = 0.0001f;

        private readonly List<Vector2Int> previousCells = new List<Vector2Int>();
        private Transform exitPivot;

        public IReadOnlyList<Vector2Int> Cells => cells;
        public BoardGridState Board => board;
        public BlockMovementState State => state;
        public ColorIdentity ColorIdentity { get; private set; }

        private void Awake()
        {
            if (exitMotor == null) exitMotor = GetComponent<BlockExitMotor>();
            if (featureController == null) featureController = GetComponent<BlockFeatureController>();
            if (iceLock == null) iceLock = GetComponent<IceLockFeature>();
            ColorIdentity = GetComponent<ColorIdentity>();
        }

        public void Configure(BoardGridState boardState, IEnumerable<Vector2Int> occupiedCells,
            ColorBlockJam.Levels.BlockFeatureData featureData)
        {
            board = boardState;
            cells.Clear();
            cells.AddRange(occupiedCells);
            if (exitMotor == null) exitMotor = GetComponent<BlockExitMotor>();
            if (featureController == null) featureController = GetComponent<BlockFeatureController>();
            if (iceLock == null) iceLock = GetComponent<IceLockFeature>();
            featureController.Configure(featureData);
            if (ColorIdentity == null) ColorIdentity = GetComponent<ColorIdentity>();
            board.Register(this);
            state = BlockMovementState.Idle;
        }

        public bool BeginDrag()
        {
            if (state != BlockMovementState.Idle ||
                (iceLock != null && iceLock.IsLocked) ||
                !featureController.CanBeginDrag()) return false;
            state = BlockMovementState.Dragging;
            return true;
        }

        public void CancelDrag()
        {
            if (state == BlockMovementState.Dragging) state = BlockMovementState.Idle;
        }

        private void BeginExit(GateGroup gate, Vector3 alignedLocalPosition)
        {
            if (state != BlockMovementState.Dragging || gate == null) return;

            board.ReleaseCells(this);
            transform.localPosition = alignedLocalPosition;
            state = BlockMovementState.Exiting;
            board.NotifyBlockExitStarted(this);
            float collapseDistance = board.CellSize * GetDepthAlong(gate.ExitDirection);
            float collapseHalfExtent = board.CellSize * GetDepthAlong(gate.ExitDirection) * 0.5f;
            exitPivot = CreateExitPivot(gate.ExitDirection, collapseHalfExtent);
            exitMotor.Play(exitPivot, gate.ExitDirection, collapseDistance, CompleteExit);
        }

        private void CompleteExit()
        {
            state = BlockMovementState.Completed;
            board.NotifyBlockCompleted(this);
            Destroy(exitPivot != null ? exitPivot.gameObject : gameObject);
        }

        private Transform CreateExitPivot(Vector2Int exitDirection, float halfExtent)
        {
            Transform originalParent = transform.parent;
            var pivotObject = new GameObject("ExitPivot");
            Transform pivot = pivotObject.transform;
            pivot.SetParent(originalParent, false);
            pivot.localPosition = transform.localPosition +
                                  new Vector3(exitDirection.x * halfExtent, 0f, exitDirection.y * halfExtent);
            pivot.localRotation = transform.localRotation;
            pivot.localScale = transform.localScale;
            transform.SetParent(pivot, true);
            return pivot;
        }

        private int GetDepthAlong(Vector2Int direction)
        {
            int min = int.MaxValue;
            int max = int.MinValue;
            bool horizontal = direction.x != 0;
            foreach (Vector2Int cell in cells)
            {
                int value = horizontal ? cell.x : cell.y;
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }
            return max - min + 1;
        }

        public bool TryGetShiftedCells(Vector2Int delta, List<Vector2Int> result)
        {
            result.Clear();
            foreach (Vector2Int cell in cells) result.Add(cell + delta);
            return board != null && board.CanOccupy(this, result);
        }

        public bool CanOccupyAtOffset(Vector2 offsetInCells)
        {
            return board != null && board.CanOccupyAtOffset(this, cells, offsetInCells);
        }

        public Vector2 ResolveDragOffset(Vector2 current, Vector2 target)
        {
            target = featureController.FilterDragTarget(current, target);
            Vector2 horizontalFirst = MoveAlongAxis(current, target.x, true);
            horizontalFirst = MoveAlongAxis(horizontalFirst, target.y, false);

            Vector2 assisted = TryAlignForVerticalCorridor(horizontalFirst, target);
            assisted = TryAlignForHorizontalCorridor(assisted, target);
            return assisted;
        }

        private Vector2 TryAlignForVerticalCorridor(Vector2 current, Vector2 target)
        {
            if (Mathf.Abs(target.y - current.y) <= MovementComparisonEpsilon) return current;

            float alignedX = Mathf.Round(target.x);
            if (Mathf.Abs(alignedX - target.x) > GameTuning.Current.CorridorAlignmentDistance) return current;

            Vector2 aligned = MoveAlongAxis(current, alignedX, true);
            if (Mathf.Abs(aligned.x - alignedX) > MovementComparisonEpsilon) return current;

            Vector2 candidate = MoveAlongAxis(aligned, target.y, false);
            return Mathf.Abs(candidate.y - target.y) + MovementComparisonEpsilon <
                   Mathf.Abs(current.y - target.y)
                ? candidate
                : current;
        }

        private Vector2 TryAlignForHorizontalCorridor(Vector2 current, Vector2 target)
        {
            if (Mathf.Abs(target.x - current.x) <= MovementComparisonEpsilon) return current;

            float alignedY = Mathf.Round(target.y);
            if (Mathf.Abs(alignedY - target.y) > GameTuning.Current.CorridorAlignmentDistance) return current;

            Vector2 aligned = MoveAlongAxis(current, alignedY, false);
            if (Mathf.Abs(aligned.y - alignedY) > MovementComparisonEpsilon) return current;

            Vector2 candidate = MoveAlongAxis(aligned, target.x, true);
            return Mathf.Abs(candidate.x - target.x) + MovementComparisonEpsilon <
                   Mathf.Abs(current.x - target.x)
                ? candidate
                : current;
        }

        private Vector2 MoveAlongAxis(Vector2 current, float target, bool horizontal)
        {
            float start = horizontal ? current.x : current.y;
            float distance = target - start;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(distance) / 0.2f));
            Vector2 lastValid = current;

            for (int step = 1; step <= steps; step++)
            {
                Vector2 candidate = current;
                float value = Mathf.Lerp(start, target, step / (float)steps);
                if (horizontal) candidate.x = value;
                else candidate.y = value;

                if (!CanOccupyAtOffset(candidate)) return FindCollisionLimit(lastValid, candidate);
                lastValid = candidate;
            }
            return lastValid;
        }

        private Vector2 FindCollisionLimit(Vector2 valid, Vector2 blocked)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 middle = (valid + blocked) * 0.5f;
                if (CanOccupyAtOffset(middle)) valid = middle;
                else blocked = middle;
            }
            return valid;
        }

        public bool TryBeginExit(Vector2Int blockDelta, Vector2Int movementDirection,
            Vector3 alignedLocalPosition)
        {
            if (state != BlockMovementState.Dragging ||
                !featureController.AllowsDirection(movementDirection) ||
                !board.TryGetExitGate(this, blockDelta, movementDirection, out GateGroup gate))
                return false;

            BeginExit(gate, alignedLocalPosition);
            return true;
        }

        public void CommitMove(Vector2Int delta, IReadOnlyList<Vector2Int> shiftedCells)
        {
            if (delta == Vector2Int.zero)
            {
                state = BlockMovementState.Idle;
                return;
            }

            previousCells.Clear();
            previousCells.AddRange(cells);
            cells.Clear();
            for (int i = 0; i < shiftedCells.Count; i++) cells.Add(shiftedCells[i]);
            board.Move(this, previousCells, cells);
            state = BlockMovementState.Idle;
        }
    }
}
