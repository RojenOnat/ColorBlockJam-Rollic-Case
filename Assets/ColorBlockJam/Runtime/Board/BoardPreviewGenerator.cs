using System.Collections.Generic;
using ColorBlockJam.Gameplay;
using ColorBlockJam.Levels;
using UnityEngine;

namespace ColorBlockJam.Board
{
    /// <summary>
    /// Composes the runtime board from authored level data. Gate, block, and wall construction live in focused
    /// partial files so each visual system can evolve independently while sharing one serialized component.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed partial class BoardPreviewGenerator : MonoBehaviour
    {
        public const string GeneratedRootName = "Generated Board";

        [SerializeField] private LevelDefinition level;
        [SerializeField] private BoardVisualSettings visualSettings;

        public LevelDefinition Level => level;
        public BoardVisualSettings VisualSettings => visualSettings;

        private void OnEnable()
        {
            RestoreGeneratedBlockColors();
        }

        public void Configure(LevelDefinition levelDefinition, BoardVisualSettings settings)
        {
            level = levelDefinition;
            visualSettings = settings;
        }

        [ContextMenu("Rebuild Board")]
        public void Rebuild()
        {
            ClearGenerated();
            if (level == null || visualSettings == null) return;
            level.EnsureBlockGroupIds();

            Transform generatedRoot = new GameObject(GeneratedRootName).transform;
            generatedRoot.SetParent(transform, false);

            HashSet<Vector2Int> enclosedCells = BoardTopology.FindEnclosedCells(level);
            BoardGridState board = generatedRoot.gameObject.AddComponent<BoardGridState>();
            board.Configure(level.BoardWidth, level.BoardHeight, visualSettings.CellSize, enclosedCells);

            BuildTiles(generatedRoot, enclosedCells);
            BuildBlocks(generatedRoot, board);
            BuildPlacedWalls(generatedRoot);
            BuildGates(generatedRoot, enclosedCells, board);
        }

        [ContextMenu("Clear Generated Board")]
        public void ClearGenerated()
        {
            Transform existing = transform.Find(GeneratedRootName);
            if (existing == null) return;

            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        private void BuildTiles(Transform root, HashSet<Vector2Int> enclosedCells)
        {
            GameObject prefab = visualSettings.TilePrefab;
            if (prefab == null) return;

            Transform tileRoot = CreateGroup("Tiles", root);
            foreach (Vector2Int cell in enclosedCells)
            {
                Vector3 position = CellCenter(cell.x, cell.y) + visualSettings.TileOffset;
                Create(prefab, position, visualSettings.TileRotation, tileRoot, $"Tile_{cell.x}_{cell.y}");
            }
        }

        private Vector3 CellCenter(int x, int y)
        {
            float xPosition = (x - (level.BoardWidth - 1) * 0.5f) * visualSettings.CellSize;
            float zPosition = (y - (level.BoardHeight - 1) * 0.5f) * visualSettings.CellSize;
            return new Vector3(xPosition, 0f, zPosition);
        }

        private Vector3 EdgeSlotCenter(BoardEdge edge, int start, int width)
        {
            float cellSize = visualSettings.CellSize;
            float halfWidth = level.BoardWidth * cellSize * 0.5f;
            float halfHeight = level.BoardHeight * cellSize * 0.5f;
            float center = (start + (width - 1) * 0.5f);

            switch (edge)
            {
                case BoardEdge.Top:
                    return new Vector3((center - (level.BoardWidth - 1) * 0.5f) * cellSize, 0f, halfHeight);
                case BoardEdge.Right:
                    return new Vector3(halfWidth, 0f, (center - (level.BoardHeight - 1) * 0.5f) * cellSize);
                case BoardEdge.Bottom:
                    return new Vector3((center - (level.BoardWidth - 1) * 0.5f) * cellSize, 0f, -halfHeight);
                default:
                    return new Vector3(-halfWidth, 0f, (center - (level.BoardHeight - 1) * 0.5f) * cellSize);
            }
        }

        private int GetEdgeLength(BoardEdge edge)
        {
            return edge == BoardEdge.Top || edge == BoardEdge.Bottom
                ? level.BoardWidth
                : level.BoardHeight;
        }

        private static Quaternion EdgeRotation(BoardEdge edge)
        {
            switch (edge)
            {
                case BoardEdge.Right: return Quaternion.Euler(0f, 90f, 0f);
                case BoardEdge.Bottom: return Quaternion.Euler(0f, 180f, 0f);
                case BoardEdge.Left: return Quaternion.Euler(0f, 270f, 0f);
                default: return Quaternion.identity;
            }
        }

        private static Transform CreateGroup(string groupName, Transform parent)
        {
            Transform group = new GameObject(groupName).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static GameObject Create(GameObject prefab, Vector3 localPosition, Quaternion localRotation,
            Transform parent, string objectName)
        {
            GameObject instance = Instantiate(prefab, parent);
            instance.name = objectName;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            return instance;
        }

    }
}
