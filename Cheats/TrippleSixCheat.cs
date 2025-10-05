using System.Reflection;
using UnityEngine;

namespace CloverMod
{
    public static class TrippleSixCheat
    {
        public static void Set666Chance(System.Type gameplayDataType, float value)
        {
            var instanceProperty = gameplayDataType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            var gameplayDataInstance = instanceProperty.GetValue(null);

            if (gameplayDataInstance != null)
            {
                // Get the max chance
                var trippleSixChanceMax = gameplayDataType.GetField("_666ChanceMaxAbsolute", BindingFlags.NonPublic | BindingFlags.Instance);
                float maxChance = trippleSixChanceMax != null ? (float)trippleSixChanceMax.GetValue(gameplayDataInstance) : 0.1f;

                // Clamp value to max chance and ensure it's non-negative
                value = Mathf.Clamp(value, 0f, maxChance);

                var trippleSixChance = gameplayDataType.GetField("_666Chance", BindingFlags.NonPublic | BindingFlags.Instance);
                trippleSixChance?.SetValue(gameplayDataInstance, value);
            }
        }

        public static void SetMax666Chance(System.Type gameplayDataType, float value)
        {
            var instanceProperty = gameplayDataType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            var gameplayDataInstance = instanceProperty.GetValue(null);

            if (gameplayDataInstance != null)
            {
                value = Mathf.Clamp(value, 0f, 1f);

                var trippleSixChanceMax = gameplayDataType.GetField("_666ChanceMaxAbsolute", BindingFlags.NonPublic | BindingFlags.Instance);
                trippleSixChanceMax?.SetValue(gameplayDataInstance, value);
            }
        }
    }
}
