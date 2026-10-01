using System.Collections.Generic;
using ColorBlockJam.Board;
using ColorBlockJam.Levels;
using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    /// <summary>
    /// A block-local feature that unlocks after a configured number of other blocks leave the board.
    /// It listens to BoardGridState so future completion-driven features can use the same event.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IceLockFeature : MonoBehaviour
    {
        [SerializeField] private bool isLocked;
        [SerializeField, Min(0)] private int remainingExits;

        private readonly Dictionary<Renderer, Material[]> originalMaterials =
            new Dictionary<Renderer, Material[]>();
        private BoardGridState board;
        private BoardVisualSettings visuals;
        private BlockColor blockColor;
        private TextMesh counter;

        public bool IsLocked => isLocked;
        public int RemainingExits => remainingExits;

        public void Configure(BoardGridState boardState, BlockFeatureData data, BoardVisualSettings settings,
            BlockColor color)
        {
            board = boardState;
            visuals = settings;
            blockColor = color;
            remainingExits = data != null && data.IceLockEnabled ? Mathf.Max(1, data.IceUnlockAfter) : 0;
            isLocked = remainingExits > 0;

            if (!isLocked) return;
            CaptureOriginalMaterials();
            ApplyIceAppearance();
            CreateCounter();
            board.BlockExited -= HandleBlockExited;
            board.BlockExited += HandleBlockExited;
        }

        private void OnDestroy()
        {
            if (board != null) board.BlockExited -= HandleBlockExited;
        }

        private void HandleBlockExited(GridMovableBlock exitedBlock)
        {
            if (!isLocked) return;

            remainingExits = Mathf.Max(0, remainingExits - 1);
            UpdateCounter();
            if (remainingExits == 0) Unlock();
        }

        private void CaptureOriginalMaterials()
        {
            originalMaterials.Clear();
            foreach (Renderer renderer in GetBlockRenderers())
                originalMaterials[renderer] = renderer.sharedMaterials;
        }

        private void ApplyIceAppearance()
        {
            foreach (Renderer renderer in GetBlockRenderers())
            {
                if (visuals.IceMaterial != null)
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++) materials[i] = visuals.IceMaterial;
                    renderer.sharedMaterials = materials;
                }

                var properties = new MaterialPropertyBlock();
                if (visuals.IceTexture != null)
                {
                    properties.SetTexture("_BaseMap", visuals.IceTexture);
                    properties.SetTexture("_MainTex", visuals.IceTexture);
                }
                renderer.SetPropertyBlock(properties);
            }
        }

        private void CreateCounter()
        {
            var counterObject = new GameObject("IceCounter");
            counterObject.transform.SetParent(transform, false);
            counterObject.transform.localPosition = visuals.IceCounterOffset;
            counterObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            counter = counterObject.AddComponent<TextMesh>();
            counter.anchor = TextAnchor.MiddleCenter;
            counter.alignment = TextAlignment.Center;
            counter.characterSize = visuals.IceCounterFontSize / 42f;
            counter.color = visuals.IceCounterColor;
            if (visuals.IceCounterFont != null) counter.font = visuals.IceCounterFont;
            UpdateCounter();
        }

        private void UpdateCounter()
        {
            if (counter != null) counter.text = remainingExits.ToString();
        }

        private void Unlock()
        {
            isLocked = false;
            if (board != null) board.BlockExited -= HandleBlockExited;

            foreach (KeyValuePair<Renderer, Material[]> pair in originalMaterials)
            {
                if (pair.Key == null) continue;
                pair.Key.sharedMaterials = pair.Value;
                ApplyBlockColor(pair.Key, blockColor);
            }

            if (counter != null) Destroy(counter.gameObject);
        }

        private IEnumerable<Renderer> GetBlockRenderers()
        {
            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Block_", System.StringComparison.Ordinal)) continue;
                foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>(true)) yield return renderer;
            }
        }

        private static void ApplyBlockColor(Renderer renderer, BlockColor color)
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

            var properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", value);
            properties.SetColor("_Color", value);
            renderer.SetPropertyBlock(properties);
        }
    }
}
