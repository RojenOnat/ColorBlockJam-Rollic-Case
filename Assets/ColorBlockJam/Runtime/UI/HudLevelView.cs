using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.UI
{
    /// <summary>Renders the active level number in the gameplay HUD.</summary>
    [DisallowMultipleComponent]
    public sealed class HudLevelView : MonoBehaviour
    {
        [SerializeField] private Text levelNumberLabel;

        public void SetLevelNumber(int levelNumber)
        {
            if (levelNumberLabel != null) levelNumberLabel.text = Mathf.Max(1, levelNumber).ToString();
        }
    }
}
