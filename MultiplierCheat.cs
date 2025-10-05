using System;
using System.Numerics;
using System.Reflection;
using UnityEngine;

namespace CloverMod
{
    public static class MultiplierCheat
    {
        public static void AddMaxPatternMultiplier(MethodInfo allPatternMultiplierAdd, int value)
        {
            allPatternMultiplierAdd?.Invoke(null, new object[] { (BigInteger)value });
        }

        public static void AddMaxPatternMultiplierMath(MethodInfo allPatternMultiplierAdd, int value)
        {
            BigInteger baseValue = BigInteger.Parse("1" + new string('0', value));
            allPatternMultiplierAdd?.Invoke(null, new object[] { baseValue });
        }
        public static void AddMaxSymbolMultiplier(MethodInfo allSymbolsMultiplierAdd, int value)
        {
            allSymbolsMultiplierAdd?.Invoke(null, new object[] { (BigInteger)value });
        }

        public static void AddMaxSymbolMultiplierMath(MethodInfo allSymbolsMultiplierAdd, int value)
        {
            BigInteger baseValue = BigInteger.Parse("1" + new string('0', value));
            allSymbolsMultiplierAdd?.Invoke(null, new object[] { baseValue });
        }
    }
}