using System;
using ColorBlockJam.Configuration;
using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BlockExitMotor : MonoBehaviour
    {
        private Transform collapsePivot;
        private Vector3 initialPivotScale;
        private float totalCollapseDistance;
        private float remainingCollapseDistance;
        private bool collapseAlongX;
        private Action completed;
        private bool isPlaying;

        public bool IsPlaying => isPlaying;

        public void Play(Transform pivot, Vector2Int gridDirection, float collapseDistance, Action onCompleted)
        {
            collapsePivot = pivot;
            totalCollapseDistance = Mathf.Max(0f, collapseDistance);
            remainingCollapseDistance = totalCollapseDistance;
            collapseAlongX = gridDirection.x != 0;
            initialPivotScale = collapsePivot != null ? collapsePivot.localScale : Vector3.one;
            completed = onCompleted;
            isPlaying = true;
        }

        private void Update()
        {
            if (!isPlaying) return;

            remainingCollapseDistance = Mathf.Max(0f,
                remainingCollapseDistance - GameTuning.Current.ExitCollapseSpeed * Time.deltaTime);
            float collapseProgress = totalCollapseDistance <= 0f ? 1f :
                1f - (remainingCollapseDistance / totalCollapseDistance);

            if (collapsePivot != null)
            {
                Vector3 scale = initialPivotScale;
                if (collapseAlongX) scale.x = initialPivotScale.x * (1f - collapseProgress);
                else scale.z = initialPivotScale.z * (1f - collapseProgress);
                collapsePivot.localScale = scale;
            }

            if (remainingCollapseDistance > 0f) return;

            isPlaying = false;
            Action callback = completed;
            completed = null;
            callback?.Invoke();
        }
    }
}
