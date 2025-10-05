using System.Reflection;

namespace CloverMod
{
    public static class SpinsRoundsCheat
    {
        public static void ExtraSpinsAdd(MethodInfo ExtraSpinsAddMethod, int value)
        {
            object[] parameters = new object[] { value };
            ExtraSpinsAddMethod?.Invoke(null, parameters);
        }

        public static void ExtraRoundsAdd(MethodInfo ExtraRoundsAddMethod, int value)
        {
            object[] parameters = new object[] { value };
            ExtraRoundsAddMethod?.Invoke(null, parameters);
        }
    }
}
