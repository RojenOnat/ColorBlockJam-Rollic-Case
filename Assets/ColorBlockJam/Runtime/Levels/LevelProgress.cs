using UnityEngine;

namespace ColorBlockJam.Levels
{
    /// <summary>Single persistence boundary for player level progress.</summary>
    public static class LevelProgress
    {
        private const string CurrentLevelKey = "ColorBlockJam.SelectedLevel";
        private const string HighestUnlockedLevelKey = "ColorBlockJam.HighestUnlockedLevel";

        public static int CurrentLevelIndex
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(CurrentLevelKey, 0));
            set
            {
                PlayerPrefs.SetInt(CurrentLevelKey, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }

        public static int HighestUnlockedLevelIndex => Mathf.Max(0, PlayerPrefs.GetInt(HighestUnlockedLevelKey, 0));

        public static void UnlockThrough(int levelIndex)
        {
            int target = Mathf.Max(0, levelIndex);
            if (target <= HighestUnlockedLevelIndex) return;
            PlayerPrefs.SetInt(HighestUnlockedLevelKey, target);
            PlayerPrefs.Save();
        }
    }
}
