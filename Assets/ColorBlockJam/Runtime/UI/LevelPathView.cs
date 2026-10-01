using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.UI
{
    /// <summary>Displays the selected level and the next entries in the home-screen level path.</summary>
    [DisallowMultipleComponent]
    public sealed class LevelPathView : MonoBehaviour
    {
        [SerializeField] private Text currentLevelLabel;
        [SerializeField] private Text[] upcomingLevelLabels;

        public void Refresh(int currentLevelIndex)
        {
            int displayedLevel = Mathf.Max(0, currentLevelIndex) + 1;
            SetLabel(currentLevelLabel, displayedLevel);

            for (int index = 0; index < upcomingLevelLabels.Length; index++)
                SetLabel(upcomingLevelLabels[index], displayedLevel + index + 1);
        }

        private static void SetLabel(Text label, int levelNumber)
        {
            if (label != null) label.text = levelNumber.ToString();
        }
    }
}
