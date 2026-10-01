using UnityEngine;

namespace ColorBlockJam.Board
{
    [CreateAssetMenu(fileName = "BoardVisualSettings", menuName = "Color Block Jam/Board Visual Settings")]
    public sealed class BoardVisualSettings : ScriptableObject
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private GameObject straightWallPrefab;
        [SerializeField] private GameObject cornerWallPrefab;
        [SerializeField] private GameObject gatePrefab;
        [SerializeField] private GameObject doorArrowPrefab;
        [SerializeField] private GameObject blockPrefab;

        [Header("Feature Prefabs")]
        [SerializeField] private GameObject directionArrowPrefab;

        [Header("Ice Feature Visuals")]
        [SerializeField] private Texture2D iceTexture;
        [SerializeField] private Material iceMaterial;
        [SerializeField] private Font iceCounterFont;
        [SerializeField, Min(1f)] private float iceCounterFontSize = 42f;
        [SerializeField] private Color iceCounterColor = Color.white;
        [SerializeField] private Vector3 iceCounterOffset = Vector3.up;

        [Header("Materials")]
        [SerializeField] private Material blockMaterial;
        [SerializeField] private Material gateMaterial;
        [SerializeField] private Material wallMaterial;

        [Header("Layout")]
        [SerializeField, Min(0.01f)] private float cellSize = 1f;
        [SerializeField] private Vector3 tileOffset;
        [SerializeField] private Vector3 wallOffset;
        [SerializeField] private Vector3 cornerOffset;
        [SerializeField] private Vector3 gateOffset;
        [SerializeField] private Vector3 doorArrowOffset = Vector3.up * 2f;
        [SerializeField] private float doorArrowVisualZ;
        [SerializeField] private Vector3 blockOffset;

        [Header("Prefab Base Rotations")]
        [Tooltip("Rotation when the straight wall or gate is on the top edge.")]
        [SerializeField] private Vector3 straightWallBaseEuler;
        [SerializeField] private Vector3 gateBaseEuler;
        [SerializeField] private Vector3 doorArrowBaseEuler;
        [Tooltip("Rotation when the corner is at the top-left of the board.")]
        [SerializeField] private Vector3 cornerBaseEuler;
        [SerializeField] private Vector3 tileEuler;
        [SerializeField] private Vector3 blockBaseEuler;

        public GameObject TilePrefab => tilePrefab;
        public GameObject StraightWallPrefab => straightWallPrefab;
        public GameObject CornerWallPrefab => cornerWallPrefab;
        public GameObject GatePrefab => gatePrefab;
        public GameObject DoorArrowPrefab => doorArrowPrefab;
        public GameObject BlockPrefab => blockPrefab;
        public GameObject DirectionArrowPrefab => directionArrowPrefab;
        public Texture2D IceTexture => iceTexture;
        public Material IceMaterial => iceMaterial;
        public Font IceCounterFont => iceCounterFont;
        public float IceCounterFontSize => iceCounterFontSize;
        public Color IceCounterColor => iceCounterColor;
        public Vector3 IceCounterOffset => iceCounterOffset;
        public Material BlockMaterial => blockMaterial;
        public Material GateMaterial => gateMaterial;
        public Material WallMaterial => wallMaterial;
        public float CellSize => cellSize;
        public Vector3 TileOffset => tileOffset;
        public Vector3 WallOffset => wallOffset;
        public Vector3 CornerOffset => cornerOffset;
        public Vector3 GateOffset => gateOffset;
        public Vector3 DoorArrowOffset => doorArrowOffset;
        public float DoorArrowVisualZ => doorArrowVisualZ;
        public Vector3 BlockOffset => blockOffset;
        public Quaternion TileRotation => Quaternion.Euler(tileEuler);
        public Quaternion StraightWallBaseRotation => Quaternion.Euler(straightWallBaseEuler);
        public Quaternion GateBaseRotation => Quaternion.Euler(gateBaseEuler);
        public Quaternion DoorArrowBaseRotation => Quaternion.Euler(doorArrowBaseEuler);
        public Quaternion CornerBaseRotation => Quaternion.Euler(cornerBaseEuler);
        public Quaternion BlockBaseRotation => Quaternion.Euler(blockBaseEuler);

        public void ConfigurePrefabs(GameObject tile, GameObject straightWall, GameObject cornerWall, GameObject gate,
            GameObject block = null, GameObject doorArrow = null)
        {
            if (tilePrefab == null) tilePrefab = tile;
            if (straightWallPrefab == null) straightWallPrefab = straightWall;
            if (cornerWallPrefab == null) cornerWallPrefab = cornerWall;
            if (gatePrefab == null) gatePrefab = gate;
            if (blockPrefab == null) blockPrefab = block;
            if (doorArrowPrefab == null) doorArrowPrefab = doorArrow;
        }

        public void ConfigureMaterials(Material block, Material gate, Material wall)
        {
            if (blockMaterial == null) blockMaterial = block;
            if (gateMaterial == null) gateMaterial = gate;
            if (wallMaterial == null) wallMaterial = wall;
        }

        public void ConfigureDirectionArrowPrefab(GameObject prefab)
        {
            if (directionArrowPrefab == null) directionArrowPrefab = prefab;
        }

        public bool HasRequiredPrefabs(out string message)
        {
            if (tilePrefab == null) { message = "Assign a Tile Prefab."; return false; }
            if (straightWallPrefab == null) { message = "Assign a Straight Wall Prefab."; return false; }
            if (cornerWallPrefab == null) { message = "Assign a Corner Wall Prefab."; return false; }
            if (gatePrefab == null) { message = "Assign a Gate Prefab."; return false; }
            if (doorArrowPrefab == null) { message = "Assign a Door Arrow Prefab."; return false; }
            if (blockPrefab == null) { message = "Assign a Block Prefab."; return false; }
            if (blockMaterial == null) { message = "Assign a Block Material."; return false; }
            if (gateMaterial == null) { message = "Assign a Gate Material."; return false; }
            if (wallMaterial == null) { message = "Assign a Wall Material."; return false; }
            message = string.Empty;
            return true;
        }
    }
}
