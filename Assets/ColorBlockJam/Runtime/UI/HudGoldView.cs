using ColorBlockJam.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.UI
{
    /// <summary>Renders the current GoldWallet balance in the gameplay HUD.</summary>
    [DisallowMultipleComponent]
    public sealed class HudGoldView : MonoBehaviour
    {
        [SerializeField] private Text amountLabel;

        private void OnEnable()
        {
            GoldWallet.BalanceChanged += HandleBalanceChanged;
            Refresh();
        }

        private void OnDisable() => GoldWallet.BalanceChanged -= HandleBalanceChanged;

        public void Refresh() => SetBalance(GoldWallet.Balance);

        public void Bind(Text label)
        {
            amountLabel = label;
            Refresh();
        }

        private void HandleBalanceChanged(int balance) => SetBalance(balance);

        private void SetBalance(int balance)
        {
            if (amountLabel != null) amountLabel.text = GoldWallet.Format(balance);
        }
    }
}
