using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ColorBlockJam.Gameplay
{
    public sealed class GameplayInputController : MonoBehaviour
    {
        [SerializeField] private Camera inputCamera;
        [SerializeField] private LayerMask clickableLayerMask = ~0;
        [SerializeField, Min(1f)] private float rayDistance = 1000f;
        [SerializeField, Min(0f)] private float liftHeight = 0.5f;
        [SerializeField] private GameObject selectedObject;

        private readonly List<Vector2Int> candidateCells = new List<Vector2Int>();
        private GridMovableBlock selectedBlock;
        private Vector3 dragStartPointerWorld;
        private Vector3 dragStartLocalPosition;
        private Vector2 acceptedOffset;
        private bool inputEnabled = true;

        public GameObject SelectedObject => selectedObject;
        public bool IsInputEnabled => inputEnabled;

        private void Awake()
        {
            if (inputCamera == null) inputCamera = Camera.main;
        }

        private void Update()
        {
            if (!inputEnabled || inputCamera == null) return;

            if (TryGetPointerDown(out Vector2 screenPosition)) BeginDrag(screenPosition);
            if (selectedBlock != null && TryGetPointerHeld(out screenPosition)) Drag(screenPosition);
            if (selectedBlock != null && TryGetPointerUp()) EndDrag();
        }

        public void SetInputEnabled(bool enabled)
        {
            if (inputEnabled == enabled) return;
            inputEnabled = enabled;
            if (!inputEnabled) CancelActiveDrag();
        }

        private void CancelActiveDrag()
        {
            if (selectedBlock == null) return;

            if (selectedBlock.State == BlockMovementState.Dragging)
            {
                selectedBlock.transform.localPosition = dragStartLocalPosition;
                selectedBlock.CancelDrag();
            }

            selectedBlock = null;
            selectedObject = null;
        }

        private void BeginDrag(Vector2 screenPosition)
        {
            selectedBlock = null;

            Ray ray = inputCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, clickableLayerMask,
                    QueryTriggerInteraction.Collide))
            {
                selectedBlock = hit.collider.GetComponentInParent<GridMovableBlock>();
                selectedObject = selectedBlock != null ? selectedBlock.gameObject : hit.collider.gameObject;
                Debug.Log($"Clicked Object: {selectedObject.name}", selectedObject);

                if (selectedBlock == null || !TryPointerOnBoard(screenPosition, out dragStartPointerWorld))
                {
                    selectedBlock = null;
                    return;
                }

                if (!selectedBlock.BeginDrag())
                {
                    selectedBlock = null;
                    selectedObject = null;
                    return;
                }

                dragStartLocalPosition = selectedBlock.transform.localPosition;
                acceptedOffset = Vector2.zero;
                Vector3 lifted = dragStartLocalPosition;
                lifted.y += liftHeight;
                selectedBlock.transform.localPosition = lifted;
            }
            else
            {
                selectedObject = null;
                Debug.Log("Clicked Object: null", this);
            }
        }

        private void Drag(Vector2 screenPosition)
        {
            if (!TryPointerOnBoard(screenPosition, out Vector3 pointerWorld)) return;

            Vector3 worldDelta = pointerWorld - dragStartPointerWorld;
            Vector3 localDelta = selectedBlock.Board.transform.InverseTransformVector(worldDelta);
            float cellSize = selectedBlock.Board.CellSize;
            var requestedOffset = new Vector2(localDelta.x / cellSize, localDelta.z / cellSize);
            if (TryBeginExit(requestedOffset, cellSize)) return;

            acceptedOffset = selectedBlock.ResolveDragOffset(acceptedOffset, requestedOffset);
            Vector3 position = dragStartLocalPosition +
                               new Vector3(acceptedOffset.x * cellSize, liftHeight,
                                   acceptedOffset.y * cellSize);
            selectedBlock.transform.localPosition = position;
        }

        private bool TryBeginExit(Vector2 requestedOffset, float cellSize)
        {
            Vector2 push = requestedOffset - acceptedOffset;
            if (push.sqrMagnitude < 0.01f) return false;

            Vector2Int direction;
            if (Mathf.Abs(push.x) >= Mathf.Abs(push.y))
                direction = push.x < 0f ? Vector2Int.left : Vector2Int.right;
            else
                direction = push.y < 0f ? Vector2Int.down : Vector2Int.up;

            var alignedDelta = new Vector2Int(Mathf.RoundToInt(acceptedOffset.x),
                Mathf.RoundToInt(acceptedOffset.y));
            Vector2 alignedOffset = alignedDelta;
            if ((acceptedOffset - alignedOffset).sqrMagnitude > 0.01f) return false;
            Vector3 alignedPosition = dragStartLocalPosition +
                                      new Vector3(alignedDelta.x * cellSize, 0f,
                                          alignedDelta.y * cellSize);
            if (!selectedBlock.TryBeginExit(alignedDelta, direction, alignedPosition)) return false;
            selectedObject = null;
            selectedBlock = null;
            return true;
        }

        private void EndDrag()
        {
            float cellSize = selectedBlock.Board.CellSize;
            var snappedDelta = new Vector2Int(Mathf.RoundToInt(acceptedOffset.x),
                Mathf.RoundToInt(acceptedOffset.y));

            if (selectedBlock.TryGetShiftedCells(snappedDelta, candidateCells))
            {
                selectedBlock.transform.localPosition = dragStartLocalPosition +
                                                        new Vector3(snappedDelta.x * cellSize, 0f,
                                                            snappedDelta.y * cellSize);
                selectedBlock.CommitMove(snappedDelta, candidateCells);
            }
            else
            {
                selectedBlock.transform.localPosition = dragStartLocalPosition;
                selectedBlock.CancelDrag();
            }
            selectedBlock = null;
            selectedObject = null;
        }

        private bool TryPointerOnBoard(Vector2 screenPosition, out Vector3 worldPosition)
        {
            Transform boardTransform = selectedBlock.Board.transform;
            var plane = new Plane(boardTransform.up, boardTransform.position);
            Ray ray = inputCamera.ScreenPointToRay(screenPosition);
            if (plane.Raycast(ray, out float distance))
            {
                worldPosition = ray.GetPoint(distance);
                return true;
            }

            worldPosition = default;
            return false;
        }

        private static bool TryGetPointerDown(out Vector2 position)
        {
#if ENABLE_INPUT_SYSTEM
            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                position = default;
                return false;
            }

            position = pointer.position.ReadValue();
            return pointer.press.wasPressedThisFrame;
#else
            position = Input.mousePosition;
            return Input.GetMouseButtonDown(0);
#endif
        }

        private static bool TryGetPointerHeld(out Vector2 position)
        {
#if ENABLE_INPUT_SYSTEM
            Pointer pointer = Pointer.current;
            position = pointer != null ? pointer.position.ReadValue() : default;
            return pointer != null && pointer.press.isPressed;
#else
            position = Input.mousePosition;
            return Input.GetMouseButton(0);
#endif
        }

        private static bool TryGetPointerUp()
        {
#if ENABLE_INPUT_SYSTEM
            Pointer pointer = Pointer.current;
            return pointer != null && pointer.press.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(0);
#endif
        }
    }
}
