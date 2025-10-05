using System;
using System.Numerics;
using System.Reflection;
using UnityEngine;

namespace CloverMod
{
    public static class CoinCheat
    {
        public static void AddMaxCoins(MethodInfo coinsAddMethod, int coins) { 
            
            
            coinsAddMethod.Invoke(null, new object[] { (BigInteger)coins, true });
        }

        public static void AddMaxCoinsMath(MethodInfo coinsAddMethod, int coins)
        {
            BigInteger baseValue = BigInteger.Parse("1" + new string('0', coins));
            coinsAddMethod.Invoke(null, new object[] { baseValue, true });
        }
    }
}
