using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CloverMod
{
    public static class AddWinsToCardsCheat
    {
        public static void addCardWins(int value)
        {
            var gameDataInstance = Panik.Data.GameData.inst;

            if (gameDataInstance == null)
            {
                Debug.LogError("GameData instance is null!");
                return;
            }

            var allIdentifiers = (RunModifierScript.Identifier[])Enum.GetValues(typeof(RunModifierScript.Identifier));

            MethodInfo method = gameDataInstance.GetType().GetMethod("RunModifier_WonTimes_Set", BindingFlags.Public | BindingFlags.Instance);

            if (method == null)
            {
                Debug.LogError("Method RunModifier_WonTimes_Set not found!");
                return;
            }

            foreach (var card in allIdentifiers.Take(allIdentifiers.Length - 2))
            {
                method.Invoke(gameDataInstance, new object[] { card, value });
                Debug.Log($"Added 20 wins to {card}");
            }
        }
    }

}
