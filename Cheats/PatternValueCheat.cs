using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CloverMod
{
    public static class PatternValueCheat
    {
        public static void SetPatternMultiplier(MethodInfo PatternValueAdd, Dictionary<string, double> symbolChances)
        {
            foreach (var symbol in symbolChances)
            {
                SetSymbolSpawnChance(PatternValueAdd,symbol.Key, symbol.Value);
            }

        }

        public static void SetSymbolSpawnChance(MethodInfo PatternValueAdd, string symbolName, double newChance)
        {
            if (!Enum.TryParse<PatternScript.Kind>(symbolName, true, out var kind))
            {
                Debug.LogError($"SymbolChanceCheat: Invalid symbol name '{symbolName}'.");
                return;
            }
            PatternValueAdd.Invoke(null, new object[] { kind, newChance });
        }


        public static void SetPatternMultiplierMath(MethodInfo PatternValueAdd, Dictionary<string, double> symbolChances)
        {
            foreach (var symbol in symbolChances)
            {
                SetSymbolSpawnChanceMath(PatternValueAdd, symbol.Key, symbol.Value);
            }

        }

        public static void SetSymbolSpawnChanceMath(MethodInfo PatternValueAdd, string symbolName, double newChance)
        {
            if (!Enum.TryParse<PatternScript.Kind>(symbolName, true, out var kind))
            {
                Debug.LogError($"SymbolChanceCheat: Invalid symbol name '{symbolName}'.");
                return;
            }
            PatternValueAdd.Invoke(null, new object[] { kind, double.Parse("1" + new String('0', (int)newChance))});
        }
    }
}
