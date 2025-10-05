using System.Reflection;

namespace CloverMod
{
    public static class InterestRateCheat
    {
        public static void SetMaxInterestRate(MethodInfo interestRateAdd, float rate)
        {
            float max = 100f; // 100 is hard cap for interest rate
            if(rate > max)
            {
                rate = max;
            }
            object[] parameters = new object[] { rate };
            interestRateAdd.Invoke(null, parameters);
        }
    }
}
