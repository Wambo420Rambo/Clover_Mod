using System.Reflection;
using UnityEngine;

namespace CloverMod
{
    public static class AnimationSpeedCheat
    {
        public static void AddAnimationSpeed(int value)
        {
            var settingsDataType = typeof(Panik.Data.SettingsData);

            // Get the static 'inst' field to access the SettingsData instance
            var instField = settingsDataType.GetField("inst", BindingFlags.Public | BindingFlags.Static);
            if (instField == null)
            {
                Debug.LogError("Could not find inst field in Panik.Data.SettingsData!");
                return;
            }

            // Get the SettingsData instance
            var settingsInstance = instField.GetValue(null);
            if (settingsInstance == null)
            {
                Debug.LogError("SettingsData instance (inst) is null!");
                return;
            }

            // Get the transitionSpeed instance field
            var transitionSpeedField = settingsDataType.GetField("transitionSpeed", BindingFlags.Public | BindingFlags.Instance);
            if (transitionSpeedField != null)
            {
                transitionSpeedField.SetValue(settingsInstance, value);
                Debug.Log($"Set transitionSpeed to {value}");
            }
            else
            {
                Debug.LogWarning("Could not find transitionSpeed field!");
            }
        }
    }
}