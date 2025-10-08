using System.Reflection;

namespace CloverMod
{
    public static class CloverTicketCheat
    {
        public static void AddMaxCloverTickets(MethodInfo cloverAddMethod, long clover)
        {
            long maxClover = 9999999999;
            if(clover > maxClover)
            {
                clover = maxClover;
            }
            object[] parameters = new object[] { clover, true };
            cloverAddMethod.Invoke(null, parameters);
        }
    }
}
