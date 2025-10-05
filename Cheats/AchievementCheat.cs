using static Panik.PlatformAPI;
using System;

namespace CloverMod
{
    public static class AchievementCheat
    {
        public static void UnlockAllAchievements()
        {
            AchievementFullGame[] allAchievements = (AchievementFullGame[])Enum.GetValues(typeof(AchievementFullGame));
            foreach (var achievement in allAchievements)
            {
                PlatformAPI_Steam.AchievementUnlock_FullGame(achievement);
            }
        }
    }
}
