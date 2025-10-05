using System.Reflection;

namespace CloverMod
{
    public static class RedButtonCheat
    {
        public static void SetRedButtonActivationsMultiplier(System.Type gameplayDataType, int value)
        {
            var gameplayDataInstance = GameplayData.Instance;
            if (gameplayDataInstance != null)
            {
                var field = gameplayDataType.GetField("_redButtonActivationsMultiplier", BindingFlags.NonPublic | BindingFlags.Instance);
                field?.SetValue(gameplayDataInstance, value);
            }
        }
    }
}
