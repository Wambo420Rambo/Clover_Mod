using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CloverMod
{
    public static class SymbolChanceCheat
    {
        // Sets the spawnChance for specific symbols based on a dictionary of symbol names and chances
        public static void SetSymbolChances(MethodInfo symbolDataFindMethod, Dictionary<string, float> symbolChances)
        {
            foreach (var symbol in symbolChances)
            {
                SetUserSymbolChance(symbolDataFindMethod, symbol.Key, symbol.Value);
            }
        }

        // Sets the spawnChance for a specific symbol kind
        public static void SetSymbolSpawnChance(MethodInfo symbolDataFindMethod, SymbolScript.Kind kind, float newChance)
        {
            // Find the SymbolData for the given kind
            symbolDataFindMethod = typeof(GameplayData).GetMethod("_SymbolDataFind", BindingFlags.NonPublic | BindingFlags.Static);
            if (symbolDataFindMethod == null)
            {
                Debug.LogError("SymbolChanceCheat: Could not find _SymbolDataFind method.");
                return;
            }
            var symbolData = symbolDataFindMethod.Invoke(null, new object[] { kind });
            if (symbolData == null)
            {
                Debug.LogError($"SymbolChanceCheat: No SymbolData found for kind {kind}.");
                return;
            }
            // Set the spawnChance field
            var spawnChanceField = symbolData.GetType().GetField("spawnChance", BindingFlags.Public | BindingFlags.Instance);
            if (spawnChanceField == null)
            {
                Debug.LogError("SymbolChanceCheat: Could not find spawnChance field.");
                return;
            }
            spawnChanceField.SetValue(symbolData, newChance);
        }

        // Sets the spawnChance for a specific symbol kind based on a user-defined value
        public static void SetUserSymbolChance(MethodInfo symbolDataFindMethod, string symbolName, float newChance)
        {
            if (!System.Enum.TryParse<SymbolScript.Kind>(symbolName, true, out var kind))
            {
                Debug.LogError($"SymbolChanceCheat: Invalid symbol name '{symbolName}'.");
                return;
            }
            SetSymbolSpawnChance(symbolDataFindMethod, kind, newChance);
        }
    }
}