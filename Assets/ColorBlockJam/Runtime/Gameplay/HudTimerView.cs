using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Gameplay
{
    /// <summary>Formats the countdown value for the persistent gameplay HUD.</summary>
    [DisallowMultipleComponent]
    public sealed class HudTimerView : MonoBehaviour
    {
        [SerializeField] private Text valueLabel;

        public void SetRemainingSeconds(int seconds)
        {
            if (valueLabel == null) return;
            seconds = Mathf.Max(0, seconds);
            valueLabel.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }
}
