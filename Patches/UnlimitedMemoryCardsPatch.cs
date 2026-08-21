using HarmonyLib;

namespace CloverMod.Patches
{
    [HarmonyPatch(typeof(Panik.Data.GameData), nameof(Panik.Data.GameData.RunModifier_OwnedCount_Set))]
    internal static class UnlimitedMemoryCardsPatch
    {
        internal static bool Bypass { get; set; }

        private static void Prefix(
            Panik.Data.GameData __instance,
            RunModifierScript.Identifier identifier,
            ref int n)
        {
            if (Bypass || Plugin.Settings == null || !Plugin.Settings.UnlimitedMemoryCards.Value)
            {
                return;
            }

            int numeric = (int)identifier;
            if (numeric <= (int)RunModifierScript.Identifier.defaultModifier ||
                numeric >= (int)RunModifierScript.Identifier.count)
            {
                return;
            }

            int current = __instance.RunModifier_OwnedCount_Get(identifier);
            if (n < current)
            {
                n = current;
            }
        }
    }
}
