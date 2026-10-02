using ColorBlockJam.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.UI
{
    public enum SettingKind
    {
        Sound,
        Music,
        Vibration
    }

    /// <summary>Displays and changes one persistent on/off setting.</summary>
    [DisallowMultipleComponent]
    public sealed class SettingsToggleView : MonoBehaviour
    {
        [SerializeField] private SettingKind setting;
        [SerializeField] private Button button;
        [SerializeField] private Image stateImage;
        [SerializeField] private Sprite enabledSprite;
        [SerializeField] private Sprite disabledSprite;
        [SerializeField] private Color enabledColor = Color.white;
        [SerializeField] private Color disabledColor = new Color(0.45f, 0.48f, 0.55f, 1f);

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(Toggle);
        }

        private void OnEnable()
        {
            PlayerSettings.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => PlayerSettings.Changed -= Refresh;

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(Toggle);
        }

        public void Configure(SettingKind kind, Button targetButton, Image targetImage, Sprite onSprite,
            Sprite offSprite)
        {
            setting = kind;
            button = targetButton;
            stateImage = targetImage;
            enabledSprite = onSprite;
            disabledSprite = offSprite;
        }

        private void Toggle()
        {
            switch (setting)
            {
                case SettingKind.Sound:
                    PlayerSettings.SoundEnabled = !PlayerSettings.SoundEnabled;
                    break;
                case SettingKind.Music:
                    PlayerSettings.MusicEnabled = !PlayerSettings.MusicEnabled;
                    break;
                case SettingKind.Vibration:
                    PlayerSettings.VibrationEnabled = !PlayerSettings.VibrationEnabled;
                    break;
            }
        }

        private void Refresh()
        {
            bool enabled = IsEnabled();
            if (stateImage == null) return;

            stateImage.sprite = enabled || disabledSprite == null ? enabledSprite : disabledSprite;
            stateImage.color = enabled ? enabledColor : disabledColor;
        }

        private bool IsEnabled()
        {
            switch (setting)
            {
                case SettingKind.Sound: return PlayerSettings.SoundEnabled;
                case SettingKind.Music: return PlayerSettings.MusicEnabled;
                default: return PlayerSettings.VibrationEnabled;
            }
        }
    }
}
