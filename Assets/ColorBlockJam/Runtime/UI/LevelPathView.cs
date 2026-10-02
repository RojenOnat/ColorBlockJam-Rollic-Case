using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.UI
{
    /// <summary>Displays the selected level and the next entries in the home-screen level path.</summary>
    [DisallowMultipleComponent]
    public sealed class LevelPathView : MonoBehaviour
    {
        private static readonly Color RailBlue = new Color(0.19f, 0.47f, 0.95f, 1f);
        private static readonly Color RailGold = new Color(1f, 0.72f, 0.08f, 1f);

        [SerializeField] private Text currentLevelLabel;
        [SerializeField] private Text[] upcomingLevelLabels;

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
            Transform railTransform = transform.Find("PathLine");
            if (railTransform == null) return;

            railTransform.SetAsFirstSibling();
            Image railImage = railTransform.GetComponent<Image>();
            if (railImage == null) return;

            railImage.color = RailBlue;
            railImage.raycastTarget = false;
            ConfigureRailBorder(railTransform, railImage, "Left Gold Border", -42f);
            ConfigureRailBorder(railTransform, railImage, "Right Gold Border", 42f);
        }

        private static void ConfigureRailBorder(Transform railTransform, Image railImage, string borderName, float positionX)
        {
            Transform borderTransform = railTransform.Find(borderName);
            if (borderTransform == null)
            {
                var borderObject = new GameObject(borderName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                borderTransform = borderObject.transform;
                borderTransform.SetParent(railTransform, false);
            }

            var rectTransform = (RectTransform)borderTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(positionX, 0f);
            rectTransform.sizeDelta = new Vector2(12f, 0f);

            Image borderImage = borderTransform.GetComponent<Image>();
            borderImage.sprite = railImage.sprite;
            borderImage.type = Image.Type.Simple;
            borderImage.color = RailGold;
            borderImage.raycastTarget = false;
        }
    }
}
