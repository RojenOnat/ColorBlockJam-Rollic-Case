using ColorBlockJam.Levels;
using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    /// <summary>Applies designer-authored directional-light data to runtime levels and editor previews.</summary>
    public static class LevelLightingSettingsApplicator
    {
        public static bool Apply(Light directionalLight, LevelLightingSettings settings)
        {
            if (directionalLight == null || settings == null) return false;

            directionalLight.transform.localRotation = Quaternion.Euler(settings.DirectionalLightRotation);
            return true;
        }
    }
}
