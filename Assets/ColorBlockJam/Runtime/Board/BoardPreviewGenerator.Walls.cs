using ColorBlockJam.Gameplay;
using ColorBlockJam.Levels;
using UnityEngine;

namespace ColorBlockJam.Board
{
    public sealed partial class BoardPreviewGenerator
    {
        private void BuildPlacedWalls(Transform root)
        {
            Transform wallRoot = CreateGroup("Walls", root);
            foreach (WallDefinition wall in level.Walls)
            {
                bool corner = IsCornerCell(wall.Position);
                GameObject prefab = corner
                    ? visualSettings.CornerWallPrefab
                    : visualSettings.StraightWallPrefab;
                if (prefab == null) continue;

                float angle = GetAutomaticRotation(wall.Position);
                Quaternion baseRotation = corner
                    ? visualSettings.CornerBaseRotation
                    : visualSettings.StraightWallBaseRotation;
                Vector3 offset = corner ? visualSettings.CornerOffset : visualSettings.WallOffset;
                Quaternion rotation = Quaternion.Euler(0f, angle, 0f) * baseRotation;
                Vector3 position = CellCenter(wall.Position.x, wall.Position.y) + rotation * offset;
                GameObject instance = Create(prefab, position, rotation, wallRoot,
                    $"{(corner ? "Corner" : "Wall")}_{wall.Position.x}_{wall.Position.y}");
                ApplyMaterial(instance, visualSettings.WallMaterial);
            }
        }

        private bool IsCornerCell(Vector2Int coordinate)
            => BoardTopology.IsCorner(level, coordinate);

        private float GetAutomaticRotation(Vector2Int coordinate)
            => BoardTopology.GetAutomaticRotation(level, coordinate);

    }
}
