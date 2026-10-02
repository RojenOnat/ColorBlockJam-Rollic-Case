using System.Collections.Generic;
using ColorBlockJam.Gameplay;
using ColorBlockJam.Levels;
using UnityEngine;

namespace ColorBlockJam.Board
{
    public sealed partial class BoardPreviewGenerator
    {
        private void BuildBlocks(Transform root, BoardGridState board)
        {
            Transform blockRoot = CreateGroup("Blocks", root);
            GameObject prefab = visualSettings.BlockPrefab;
            if (prefab == null) return;

            var remaining = new HashSet<BlockDefinition>(level.Blocks);
            int groupIndex = 0;
            while (remaining.Count > 0)
            {
                BlockDefinition seed = null;
                foreach (BlockDefinition candidate in remaining)
                {
                    seed = candidate;
                    break;
                }

                HashSet<BlockDefinition> component = CollectConnectedBlock(seed, remaining);
                Vector3 groupCenter = GetBlockGroupCenter(component);
                Transform group = CreateGroup($"BlockGroup_{seed.Color}_{groupIndex++}", blockRoot);
                group.localPosition = groupCenter;
                group.gameObject.layer = prefab.layer;
                group.gameObject.AddComponent<ColorIdentity>().Configure(seed.Color);

                HashSet<BlockDefinition> innerCornerCells =
                    BuildInnerCorners(group, prefab, component, groupCenter);
                foreach (BlockDefinition block in component)
                {
                    if (innerCornerCells.Contains(block)) continue;
                    BlockVisualPart partType = BlockTopology.GetVisualPart(level, block);

                    float angle = BlockTopology.GetVisualRotation(level, block);
                    Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
                    Vector3 position = CellCenter(block.Position.x, block.Position.y) - groupCenter +
                                       rotation * visualSettings.BlockOffset;
                    GameObject instance = Create(prefab, position, Quaternion.identity, group,
                        $"Block_{block.Color}_{partType}_{block.Position.x}_{block.Position.y}");
                    SetBlockRotation(instance.transform, rotation);
                    Transform activePart = SetActiveBlockPart(instance, partType);
                    CenterBlockPartOnCell(instance.transform, activePart);
                    ApplyMaterial(instance, visualSettings.BlockMaterial);
                    ClearMaterialOverrides(instance);
                    ApplyColor(instance, block.Color);
                }

                var occupiedCells = new List<Vector2Int>();
                foreach (BlockDefinition block in component) occupiedCells.Add(block.Position);
                GridMovableBlock movableBlock = group.gameObject.AddComponent<GridMovableBlock>();
                IceLockFeature iceLock = group.gameObject.AddComponent<IceLockFeature>();
                iceLock.Configure(board, seed.Features, visualSettings, seed.Color);
                movableBlock.Configure(board, occupiedCells, seed.Features);
                CreateDirectionFeatureVisual(group, component, seed.Features.MovementMode);

            }
        }

        private void CreateDirectionFeatureVisual(Transform blockGroup, HashSet<BlockDefinition> component,
            BlockMovementMode movementMode)
        {
            if (movementMode == BlockMovementMode.Free || visualSettings.DirectionArrowPrefab == null) return;

            bool horizontal = movementMode == BlockMovementMode.HorizontalOnly;
            // A physical modular block occupies a 2 x 2 editor-grid area.
            // The arrow variants represent physical block units, not individual grid cells.
            int span = Mathf.CeilToInt(GetBlockSpan(component, horizontal) / 2f);
            Quaternion rotation = horizontal ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
            GameObject arrow = Create(visualSettings.DirectionArrowPrefab, Vector3.up * -0.25f, rotation,
                blockGroup, $"DirectionFeature_{movementMode}");
            SetDirectionArrowVariant(arrow.transform, span);
        }

        private static void ClearMaterialOverrides(GameObject instance)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.SetPropertyBlock(new MaterialPropertyBlock());
        }

        private static int GetBlockSpan(IEnumerable<BlockDefinition> component, bool horizontal)
        {
            int minimum = int.MaxValue;
            int maximum = int.MinValue;
            foreach (BlockDefinition block in component)
            {
                int value = horizontal ? block.Position.x : block.Position.y;
                minimum = Mathf.Min(minimum, value);
                maximum = Mathf.Max(maximum, value);
            }

            return maximum - minimum + 1;
        }

        private static void SetDirectionArrowVariant(Transform arrow, int span)
        {
            Transform variants = arrow.Find("Visual");
            if (variants == null) variants = arrow;
            if (variants.childCount == 0) return;

            int activeIndex = Mathf.Clamp(span - 1, 0, variants.childCount - 1);
            for (int i = 0; i < variants.childCount; i++)
                variants.GetChild(i).gameObject.SetActive(i == activeIndex);
        }

        private HashSet<BlockDefinition> CollectConnectedBlock(BlockDefinition seed,
            HashSet<BlockDefinition> remaining)
        {
            var component = new HashSet<BlockDefinition>();
            var pending = new Queue<BlockDefinition>();
            remaining.Remove(seed);
            component.Add(seed);
            pending.Enqueue(seed);

            Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
            while (pending.Count > 0)
            {
                BlockDefinition current = pending.Dequeue();
                foreach (Vector2Int direction in directions)
                {
                    BlockDefinition neighbour = BlockTopology.GetBlockAt(level, current.Position + direction);
                    if (neighbour == null || neighbour.GroupId != seed.GroupId || !remaining.Remove(neighbour))
                        continue;
                    component.Add(neighbour);
                    pending.Enqueue(neighbour);
                }
            }
            return component;
        }

        private Vector3 GetBlockGroupCenter(HashSet<BlockDefinition> component)
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            foreach (BlockDefinition block in component)
            {
                minX = Mathf.Min(minX, block.Position.x);
                maxX = Mathf.Max(maxX, block.Position.x);
                minY = Mathf.Min(minY, block.Position.y);
                maxY = Mathf.Max(maxY, block.Position.y);
            }

            return (CellCenter(minX, minY) + CellCenter(maxX, maxY)) * 0.5f;
        }

        private HashSet<BlockDefinition> BuildInnerCorners(Transform blockRoot, GameObject prefab,
            HashSet<BlockDefinition> component, Vector3 groupCenter)
        {
            var consumed = new HashSet<BlockDefinition>();
            for (int y = 0; y < level.BoardHeight - 1; y++)
            {
                for (int x = 0; x < level.BoardWidth - 1; x++)
                {
                    BlockDefinition bottomLeft = BlockTopology.GetBlockAt(level, new Vector2Int(x, y));
                    BlockDefinition bottomRight = BlockTopology.GetBlockAt(level, new Vector2Int(x + 1, y));
                    BlockDefinition topLeft = BlockTopology.GetBlockAt(level, new Vector2Int(x, y + 1));
                    BlockDefinition topRight = BlockTopology.GetBlockAt(level, new Vector2Int(x + 1, y + 1));

                    BlockDefinition anchor;
                    BlockDefinition first;
                    BlockDefinition second;
                    BlockDefinition third;
                    float angle;

                    if (topRight == null && HaveSameGroup(bottomLeft, bottomRight, topLeft) &&
                        HasMatchingBlock(topLeft.Position + Vector2Int.up, topLeft) &&
                        HasMatchingBlock(bottomRight.Position + Vector2Int.right, bottomRight))
                    {
                        anchor = topLeft;
                        first = bottomLeft;
                        second = bottomRight;
                        third = topLeft;
                        angle = 0f;
                    }
                    else if (bottomRight == null && HaveSameGroup(bottomLeft, topLeft, topRight) &&
                             HasMatchingBlock(topRight.Position + Vector2Int.right, topRight) &&
                             HasMatchingBlock(bottomLeft.Position + Vector2Int.down, bottomLeft))
                    {
                        anchor = topRight;
                        first = bottomLeft;
                        second = topLeft;
                        third = topRight;
                        angle = 90f;
                    }
                    else if (bottomLeft == null && HaveSameGroup(bottomRight, topLeft, topRight) &&
                             HasMatchingBlock(bottomRight.Position + Vector2Int.down, bottomRight) &&
                             HasMatchingBlock(topLeft.Position + Vector2Int.left, topLeft))
                    {
                        anchor = bottomRight;
                        first = bottomRight;
                        second = topLeft;
                        third = topRight;
                        angle = 180f;
                    }
                    else if (topLeft == null && HaveSameGroup(bottomLeft, bottomRight, topRight) &&
                             HasMatchingBlock(bottomLeft.Position + Vector2Int.left, bottomLeft) &&
                             HasMatchingBlock(topRight.Position + Vector2Int.up, topRight))
                    {
                        anchor = bottomLeft;
                        first = bottomLeft;
                        second = bottomRight;
                        third = topRight;
                        angle = 270f;
                    }
                    else
                    {
                        continue;
                    }

                    if (!component.Contains(first) || !component.Contains(second) || !component.Contains(third) ||
                        consumed.Contains(first) || consumed.Contains(second) || consumed.Contains(third))
                        continue;

                    Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
                    Vector3 position = CellCenter(anchor.Position.x, anchor.Position.y) - groupCenter +
                                       rotation * visualSettings.BlockOffset;
                    GameObject instance = Create(prefab, position, Quaternion.identity, blockRoot,
                        $"Block_{anchor.Color}_InnerCorner_{anchor.Position.x}_{anchor.Position.y}");
                    SetBlockRotation(instance.transform, rotation);
                    SetActiveBlockPart(instance, BlockVisualPart.InnerCorner);
                    ApplyMaterial(instance, visualSettings.BlockMaterial);
                    ClearMaterialOverrides(instance);
                    ApplyColor(instance, anchor.Color);

                    consumed.Add(first);
                    consumed.Add(second);
                    consumed.Add(third);
                }
            }
            return consumed;
        }

        private static bool HaveSameGroup(BlockDefinition first, BlockDefinition second, BlockDefinition third)
        {
            return first != null && second != null && third != null &&
                   first.GroupId == second.GroupId && second.GroupId == third.GroupId;
        }

        private static void SetBlockRotation(Transform blockRoot, Quaternion rotation)
        {
            Transform rotationRoot = blockRoot.childCount > 0
                ? blockRoot.GetChild(0)
                : blockRoot;
            rotationRoot.localRotation = rotation;
        }

        private static Transform SetActiveBlockPart(GameObject instance, BlockVisualPart activePart)
        {
            // The prefab root is also the corner mesh variant. Its name is part of the visual contract,
            // just like the named child variants below, so it must participate in variant selection.
            string activeName = activePart == BlockVisualPart.Corner
                ? "Block_Corner"
                : activePart == BlockVisualPart.Middle
                    ? "Block_Center"
                    : activePart == BlockVisualPart.InnerCorner
                        ? "ModularBlock_InnerCorner"
                        : "Block_Edge";

            Transform activeTransform = null;
            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Block_Corner" || child.name == "Block_Edge" ||
                    child.name == "Block_Center" || child.name == "ModularBlock_InnerCorner")
                {
                    bool active = child.name == activeName;
                    child.gameObject.SetActive(active);
                    if (active) activeTransform = child;
                }
            }
            return activeTransform;
        }

        private static void CenterBlockPartOnCell(Transform blockRoot, Transform activePart)
        {
            if (activePart == null) return;

            Renderer[] renderers = activePart.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            Vector3 correction = blockRoot.position - bounds.center;
            correction.y = 0f;
            activePart.position += correction;
        }

        private bool HasMatchingBlock(Vector2Int coordinate, BlockDefinition source)
        {
            return BlockTopology.IsSameGroupAt(level, source, coordinate);
        }

        private static void ApplyColor(GameObject instance, BlockColor color, float brightness = 1f)
        {
            Color value;
            switch (color)
            {
                case BlockColor.Red: value = new Color(0.93f, 0.22f, 0.28f); break;
                case BlockColor.Blue: value = new Color(0.18f, 0.48f, 0.95f); break;
                case BlockColor.Green: value = new Color(0.24f, 0.76f, 0.36f); break;
                case BlockColor.Yellow: value = new Color(1f, 0.78f, 0.12f); break;
                case BlockColor.Purple: value = new Color(0.64f, 0.31f, 0.88f); break;
                default: value = new Color(1f, 0.46f, 0.12f); break;
            }

            value = new Color(value.r * brightness, value.g * brightness, value.b * brightness, value.a);

            var properties = new MaterialPropertyBlock();
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", value);
                properties.SetColor("_Color", value);
                renderer.SetPropertyBlock(properties);
            }
        }

        private static void ApplyMaterial(GameObject instance, Material material)
        {
            if (material == null) return;

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
            }
        }

        private void RestoreGeneratedBlockColors()
        {
            Transform generatedRoot = transform.Find(GeneratedRootName);
            if (generatedRoot == null) return;

            Transform blocks = generatedRoot.Find("Blocks");
            if (blocks == null) return;

            foreach (Transform group in blocks)
            {
                foreach (BlockColor color in System.Enum.GetValues(typeof(BlockColor)))
                {
                    if (!group.name.StartsWith($"BlockGroup_{color}_", System.StringComparison.Ordinal))
                        continue;

                    if (group.GetComponent<IceLockFeature>() == null ||
                        !group.GetComponent<IceLockFeature>().IsLocked)
                        ApplyColor(group.gameObject, color);
                    RestoreDirectionFeatureMaterialColor(group);
                    break;
                }
            }

            Transform gates = generatedRoot.Find("Gates");
            if (gates == null) return;

            foreach (Transform gate in gates)
            {
                foreach (BlockColor color in System.Enum.GetValues(typeof(BlockColor)))
                {
                    if (!gate.name.StartsWith($"GateGroup_{color}_", System.StringComparison.Ordinal)) continue;
                    ApplyColor(gate.gameObject, color);
                    Transform arrow = gate.Find("GateArrow");
                    if (arrow != null) ApplyColor(arrow.gameObject, color, 0.85f);
                    break;
                }
            }
        }

        private static void RestoreDirectionFeatureMaterialColor(Transform blockGroup)
        {
            foreach (Transform child in blockGroup)
            {
                if (!child.name.StartsWith("DirectionFeature_", System.StringComparison.Ordinal)) continue;
                foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>(true))
                    renderer.SetPropertyBlock(new MaterialPropertyBlock());
            }
        }

    }
}
