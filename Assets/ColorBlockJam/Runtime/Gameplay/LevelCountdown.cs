using System;
using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    /// <summary>
    /// Runtime-only countdown for one instantiated level. It is destroyed with the level.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelCountdown : MonoBehaviour
    {
        private float remainingSeconds;
        private bool isRunning;
        private bool hasExpired;
        private bool isPaused;

        public int DisplaySeconds => Mathf.CeilToInt(remainingSeconds);
        public bool IsRunning => isRunning;
        public bool IsPaused => isPaused;

        public event Action<int> TimeChanged;
        public event Action Expired;

        public void StartCountdown(int seconds)
        {
            remainingSeconds = Mathf.Max(0, seconds);
            isRunning = remainingSeconds > 0f;
            hasExpired = false;
            isPaused = false;
            NotifyTimeChanged();

            if (!isRunning)
                RaiseExpired();
        }

        public void StopCountdown()
        {
            isRunning = false;
            isPaused = false;
        }

        public void SetPaused(bool paused)
        {
            if (!isRunning) return;
            isPaused = paused;
        }

        private void Update()
        {
            if (!isRunning || isPaused) return;

            int displayedBeforeTick = DisplaySeconds;
            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
            if (DisplaySeconds != displayedBeforeTick)
                NotifyTimeChanged();

            if (remainingSeconds <= 0f)
            {
                isRunning = false;
                RaiseExpired();
            }
        }

        private void NotifyTimeChanged() => TimeChanged?.Invoke(DisplaySeconds);

        private void RaiseExpired()
        {
            if (hasExpired) return;
            hasExpired = true;
            Expired?.Invoke();
        }
    }
}
