using ColorBlockJam.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.UI
{
    /// <summary>Renders the configured reward on the successful end-game panel.</summary>
    [DisallowMultipleComponent]
    public sealed class LevelRewardView : MonoBehaviour
    {
        [SerializeField] private Text amountLabel;

        public void Bind(Text label) => amountLabel = label;

        public void SetReward(int amount)
        {
            if (amountLabel != null) amountLabel.text = GoldWallet.Format(amount);
        }
    }
}
