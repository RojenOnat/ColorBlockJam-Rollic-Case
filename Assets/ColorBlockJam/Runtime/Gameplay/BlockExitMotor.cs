using System;
using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BlockExitMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 4f;

        private Vector3 direction;
        private float remainingDistance;
        private Action completed;
        private bool isPlaying;

        public bool IsPlaying => isPlaying;

        public void Play(Vector2Int gridDirection, float distance, Action onCompleted)
        {
            direction = new Vector3(gridDirection.x, 0f, gridDirection.y);
            remainingDistance = Mathf.Max(0f, distance);
            completed = onCompleted;
            isPlaying = true;
        }

        private void Update()
        {
            if (!isPlaying) return;

            float movement = Mathf.Min(speed * Time.deltaTime, remainingDistance);
            transform.localPosition += direction * movement;
            remainingDistance -= movement;
            if (remainingDistance > 0f) return;

            isPlaying = false;
            Action callback = completed;
            completed = null;
            callback?.Invoke();
        }
    }
}
