using System;
using System.Globalization;
using UnityEngine;

namespace ColorBlockJam.Economy
{
    /// <summary>Single persistent source of truth for the player's soft-currency balance.</summary>
    public static class GoldWallet
    {
        // v2 intentionally starts clean: v1 was populated by the temporary 9.50k placeholder.
        private const string GoldKey = "ColorBlockJam.Gold.v2";
        private const int DefaultGold = 0;

        public static event Action<int> BalanceChanged;

        public static int Balance => Mathf.Max(0, PlayerPrefs.GetInt(GoldKey, DefaultGold));

        public static int Add(int amount)
        {
            if (amount <= 0) return Balance;

            int updatedBalance = (int)Math.Min((long)Balance + amount, int.MaxValue);
            PlayerPrefs.SetInt(GoldKey, updatedBalance);
            PlayerPrefs.Save();
            BalanceChanged?.Invoke(updatedBalance);
            return updatedBalance;
        }

        public static string Format(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (amount < 1000) return amount.ToString(CultureInfo.InvariantCulture);
            if (amount < 1000000)
                return (amount / 1000f).ToString("0.##", CultureInfo.InvariantCulture) + "k";

            return (amount / 1000000f).ToString("0.##", CultureInfo.InvariantCulture) + "m";
        }
    }
}
