using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.UI
{
    /// <summary>Displays the selected level and the next entries in the home-screen level path.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class LevelPathView : MonoBehaviour
    {
        private static readonly Color RailBlue = new Color(0.19f, 0.47f, 0.95f, 1f);
        private static readonly Color RailGold = new Color(1f, 0.72f, 0.08f, 1f);

        [SerializeField] private Text currentLevelLabel;
        [SerializeField] private Text[] upcomingLevelLabels;
        [SerializeField] private Image pathRail;
        [SerializeField] private Image leftRailBorder;
        [SerializeField] private Image rightRailBorder;

        private void OnEnable() => ConfigurePathRail();

        public void Refresh(int currentLevelIndex)
        {
            ConfigurePathRail();
            int displayedLevel = Mathf.Max(0, currentLevelIndex) + 1;
            SetLabel(currentLevelLabel, displayedLevel);

            for (int index = 0; index < upcomingLevelLabels.Length; index++)
                SetLabel(upcomingLevelLabels[index], displayedLevel + index + 1);
        }

        private static void SetLabel(Text label, int levelNumber)
        {
            if (label != null) label.text = levelNumber.ToString();
        }

        private void ConfigurePathRail()
        {
            if (pathRail != null)
            {
                pathRail.transform.SetAsFirstSibling();
                pathRail.color = RailBlue;
                pathRail.raycastTarget = false;
            }

            ConfigureRailBorder(leftRailBorder);
            ConfigureRailBorder(rightRailBorder);
        }

        private static void ConfigureRailBorder(Image border)
        {
            if (border == null) return;
            border.color = RailGold;
            border.raycastTarget = false;
        }
    }
}
