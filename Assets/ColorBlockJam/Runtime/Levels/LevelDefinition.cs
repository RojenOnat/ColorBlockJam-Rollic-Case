using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorBlockJam.Levels
{
    public enum BlockColor
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple,
        Orange
    }

    public enum BlockShape
    {
        Single,
        Horizontal2,
        Horizontal3,
        Square2,
        L3,
        T4
    }

    public enum BlockMovementMode
    {
        Free,
        HorizontalOnly,
        VerticalOnly
    }

    [Serializable]
    public sealed class BlockFeatureData
    {
        [SerializeField] private BlockMovementMode movementMode;
        [SerializeField] private bool iceLockEnabled;
        [SerializeField, Min(1)] private int iceUnlockAfter = 1;

        public BlockMovementMode MovementMode => movementMode;
        public bool IceLockEnabled => iceLockEnabled;
        public int IceUnlockAfter => iceUnlockAfter;

        public void SetMovementMode(BlockMovementMode value) => movementMode = value;
        public void SetIceLock(bool enabled, int unlockAfter)
        {
            iceLockEnabled = enabled;
            iceUnlockAfter = Mathf.Max(1, unlockAfter);
        }
    }

    [Serializable]
    public sealed class LevelCameraSettings
    {
        [SerializeField] private Vector3 position = new Vector3(0f, 7.5f, -9f);
        [SerializeField] private Vector3 rotation = new Vector3(42f, 0f, 0f);
        [SerializeField, Range(20f, 100f)] private float fieldOfView = 60f;

        public Vector3 Position => position;
        public Vector3 Rotation => rotation;
        public float FieldOfView => fieldOfView;

        public void Set(Vector3 newPosition, Vector3 newRotation, float newFieldOfView)
        {
            position = newPosition;
            rotation = newRotation;
            fieldOfView = Mathf.Clamp(newFieldOfView, 20f, 100f);
        }
    }

    public enum BoardEdge
    {
        Top,
        Right,
        Bottom,
        Left
    }

    [Serializable]
    public sealed class BlockDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private BlockColor color;
        [SerializeField] private BlockShape shape;
        [SerializeField] private Vector2Int position;
        [SerializeField] private string groupId;
        [SerializeField, Range(0, 3)] private int quarterTurns;
        [SerializeField] private BlockFeatureData features = new BlockFeatureData();

        public string Id => id;
        public BlockColor Color => color;
        public BlockShape Shape => shape;
        public Vector2Int Position => position;
        public string GroupId => groupId;
        public int QuarterTurns => quarterTurns;
        public BlockFeatureData Features
        {
            get
            {
                if (features == null) features = new BlockFeatureData();
                return features;
            }
        }

        public BlockDefinition(string id, BlockColor color, BlockShape shape, Vector2Int position)
        {
            this.id = id;
            this.color = color;
            this.shape = shape;
            this.position = position;
            groupId = id;
            quarterTurns = 0;
        }

        public void SetPosition(Vector2Int value) => position = value;
        public void SetColor(BlockColor value) => color = value;
        public void SetShape(BlockShape value) => shape = value;
        public void SetGroupId(string value) => groupId = value;
        public void SetMovementMode(BlockMovementMode value) => Features.SetMovementMode(value);
        public void RotateClockwise() => quarterTurns = (quarterTurns + 1) % 4;
    }

    [Serializable]
    public sealed class DoorDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private BlockColor color;
        [SerializeField] private BoardEdge edge;
        [SerializeField, Min(0)] private int edgePosition;
        [SerializeField, Min(1)] private int width = 1;
        [SerializeField] private bool gridPlaced;
        [SerializeField] private Vector2Int position;
        [SerializeField, Range(0, 3)] private int quarterTurns;

        public string Id => id;
        public BlockColor Color => color;
        public BoardEdge Edge => edge;
        public int EdgePosition => edgePosition;
        public int Width => width;
        public bool GridPlaced => gridPlaced;
        public Vector2Int Position => position;
        public int QuarterTurns => quarterTurns;

        public DoorDefinition(string id, BlockColor color, BoardEdge edge, int edgePosition)
        {
            this.id = id;
            this.color = color;
            this.edge = edge;
            this.edgePosition = edgePosition;
            width = 1;
        }

        public DoorDefinition(string id, BlockColor color, Vector2Int position, int quarterTurns)
        {
            this.id = id;
            this.color = color;
            this.position = position;
            this.quarterTurns = Mathf.Clamp(quarterTurns, 0, 3);
            gridPlaced = true;
            width = 1;
        }

        public void SetColor(BlockColor value) => color = value;
        public void SetWidth(int value) => width = Mathf.Max(1, value);
    }

    [Serializable]
    public sealed class WallDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private Vector2Int position;
        [SerializeField, Range(0, 3)] private int quarterTurns;

        public string Id => id;
        public Vector2Int Position => position;
        public int QuarterTurns => quarterTurns;

        public WallDefinition(string id, Vector2Int position)
        {
            this.id = id;
            this.position = position;
        }

        public void SetPosition(Vector2Int value) => position = value;
        public void RotateClockwise() => quarterTurns = (quarterTurns + 1) % 4;
    }

    [CreateAssetMenu(fileName = "Level_01", menuName = "Color Block Jam/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public const int MinimumBoardSize = 3;
        public const int MaximumBoardSize = 12;

        [SerializeField] private string displayName = "New Level";
        [SerializeField, Range(MinimumBoardSize, MaximumBoardSize)] private int boardWidth = 6;
        [SerializeField, Range(MinimumBoardSize, MaximumBoardSize)] private int boardHeight = 8;
        [SerializeField, Min(1)] private int timerSeconds = 60;
        [SerializeField] private LevelCameraSettings cameraSettings = new LevelCameraSettings();
        [SerializeField] private List<BlockDefinition> blocks = new List<BlockDefinition>();
        [SerializeField] private List<DoorDefinition> doors = new List<DoorDefinition>();
        [SerializeField] private List<WallDefinition> walls = new List<WallDefinition>();

        public string DisplayName => displayName;
        public int BoardWidth => boardWidth;
        public int BoardHeight => boardHeight;
        public int TimerSeconds => timerSeconds;
        public LevelCameraSettings CameraSettings
        {
            get
            {
                if (cameraSettings == null) cameraSettings = new LevelCameraSettings();
                return cameraSettings;
            }
        }
        public IReadOnlyList<BlockDefinition> Blocks => blocks;
        public IReadOnlyList<DoorDefinition> Doors => doors;
        public IReadOnlyList<WallDefinition> Walls => walls;

        public void SetDisplayName(string value) => displayName = string.IsNullOrWhiteSpace(value) ? name : value.Trim();

        public void SetBoardSize(int width, int height)
        {
            boardWidth = Mathf.Clamp(width, MinimumBoardSize, MaximumBoardSize);
            boardHeight = Mathf.Clamp(height, MinimumBoardSize, MaximumBoardSize);
        }

        public void SetTimer(int seconds) => timerSeconds = Mathf.Max(1, seconds);
        public void SetCameraSettings(Vector3 position, Vector3 rotation, float fieldOfView) =>
            CameraSettings.Set(position, rotation, fieldOfView);
        public void AddBlock(BlockDefinition block) => blocks.Add(block);
        public void RemoveBlock(BlockDefinition block) => blocks.Remove(block);
        public void AddDoor(DoorDefinition door) => doors.Add(door);
        public void RemoveDoor(DoorDefinition door) => doors.Remove(door);
        public void AddWall(WallDefinition wall) => walls.Add(wall);
        public void RemoveWall(WallDefinition wall) => walls.Remove(wall);

        public bool EnsureBlockGroupIds()
        {
            var unassigned = new HashSet<BlockDefinition>();
            foreach (BlockDefinition block in blocks)
                if (string.IsNullOrWhiteSpace(block.GroupId)) unassigned.Add(block);

            bool changed = false;
            Vector2Int[] directions =
                { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
            while (unassigned.Count > 0)
            {
                BlockDefinition seed = null;
                foreach (BlockDefinition candidate in unassigned)
                {
                    seed = candidate;
                    break;
                }

                string legacyGroupId = $"Group_{seed.Id}";
                var pending = new Queue<BlockDefinition>();
                pending.Enqueue(seed);
                unassigned.Remove(seed);
                while (pending.Count > 0)
                {
                    BlockDefinition current = pending.Dequeue();
                    current.SetGroupId(legacyGroupId);
                    changed = true;
                    foreach (Vector2Int direction in directions)
                    {
                        Vector2Int neighbourPosition = current.Position + direction;
                        BlockDefinition neighbour = null;
                        foreach (BlockDefinition candidate in unassigned)
                        {
                            if (candidate.Color != seed.Color || candidate.Position != neighbourPosition) continue;
                            neighbour = candidate;
                            break;
                        }

                        if (neighbour == null) continue;
                        unassigned.Remove(neighbour);
                        pending.Enqueue(neighbour);
                    }
                }
            }

            return changed;
        }
    }
}
