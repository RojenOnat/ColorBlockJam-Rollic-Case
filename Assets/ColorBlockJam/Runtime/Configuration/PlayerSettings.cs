using System;
using UnityEngine;

namespace ColorBlockJam.Configuration
{
    /// <summary>Persistent player-facing settings shared by the home and gameplay screens.</summary>
    public static class PlayerSettings
    {
        private const string SoundKey = "ColorBlockJam.Settings.Sound";
        private const string MusicKey = "ColorBlockJam.Settings.Music";
        private const string VibrationKey = "ColorBlockJam.Settings.Vibration";

        public static event Action Changed;

        public static bool SoundEnabled
        {
            get => Read(SoundKey);
            set => Write(SoundKey, value);
        }

        public static bool MusicEnabled
        {
            get => Read(MusicKey);
            set => Write(MusicKey, value);
        }

        public static bool VibrationEnabled
        {
            get => Read(VibrationKey);
            set => Write(VibrationKey, value);
        }

        private static bool Read(string key) => PlayerPrefs.GetInt(key, 1) != 0;

        private static void Write(string key, bool value)
        {
            int serialized = value ? 1 : 0;
            if (PlayerPrefs.GetInt(key, 1) == serialized) return;

            PlayerPrefs.SetInt(key, serialized);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
