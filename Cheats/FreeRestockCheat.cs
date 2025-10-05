using System.Reflection;

namespace CloverMod
{
    public static class FreeRestockCheat
    {
        public static void AddFreeRestock(MethodInfo addFreeRestockMethod, int value)
        {
            object[] parameters = new object[] { value };
            addFreeRestockMethod?.Invoke(null, parameters);
        }
    }
}

