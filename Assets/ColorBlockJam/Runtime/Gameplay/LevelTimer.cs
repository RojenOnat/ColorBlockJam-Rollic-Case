using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Gameplay
{
    /// <summary>
    /// Displays and counts down the active level's duration.
    /// The timer owns no game-state decisions; listeners decide what expiry means.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelTimer : MonoBehaviour
    {
        [SerializeField] private Text valueLabel;

        private float remainingSeconds;
        private bool isRunning;
        private bool hasExpired;

        public float RemainingSeconds => remainingSeconds;
        public bool IsRunning => isRunning;
        public event Action Expired;

        public void StartCountdown(int seconds)
        {
            remainingSeconds = Mathf.Max(0, seconds);
            hasExpired = false;
            isRunning = remainingSeconds > 0f;
            RefreshLabel();

            if (!isRunning)
                RaiseExpired();
        }

        public void StopCountdown()
        {
            isRunning = false;
        }

        private void Update()
        {
            if (!isRunning) return;

            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
            RefreshLabel();

            if (remainingSeconds <= 0f)
            {
                isRunning = false;
                RaiseExpired();
            }
        }

        private void RaiseExpired()
        {
            if (hasExpired) return;
            hasExpired = true;
            Expired?.Invoke();
        }

        private void RefreshLabel()
        {
            if (valueLabel == null) return;

            int wholeSeconds = Mathf.CeilToInt(remainingSeconds);
            int minutes = wholeSeconds / 60;
            int seconds = wholeSeconds % 60;
            valueLabel.text = $"{minutes:00}:{seconds:00}";
        }
    }
}
