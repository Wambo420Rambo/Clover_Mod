namespace CloverMod
{
    public static class EndingCounterCheat
    {
        public static void SetGoodEndingCounter(int value)
        {
            var data = Panik.Data.GameData.inst;
            if (data != null)
            {
                data.goodEndingCounter = value;
            }
        }
    }
}
