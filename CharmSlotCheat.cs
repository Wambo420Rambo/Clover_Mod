using System.Reflection;

namespace CloverMod
{
    public static class CharmSlotCheat
    {
        public static void AddCharmSlots(MethodInfo charmSlotsAddMethod, int slotsToAdd)
        {
            object[] parameters = new object[] { slotsToAdd };
            charmSlotsAddMethod?.Invoke(null, parameters);
        }
    }
}
