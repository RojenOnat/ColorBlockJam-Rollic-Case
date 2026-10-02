using ColorBlockJam.Levels;
using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    /// <summary>Applies designer-authored camera data to a level camera in editor previews and runtime.</summary>
    public static class LevelCameraSettingsApplicator
    {
        public static bool Apply(Camera levelCamera, LevelCameraSettings settings)
        {
            if (levelCamera == null || settings == null) return false;

            levelCamera.transform.localPosition = settings.Position;
            levelCamera.transform.localRotation = Quaternion.Euler(settings.Rotation);
            levelCamera.fieldOfView = settings.FieldOfView;
            return true;
        }
    }
}
