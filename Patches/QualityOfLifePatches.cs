using System.Reflection;
using CloverMod.Core;
using HarmonyLib;

namespace CloverMod.Patches
{
    internal static class QualityOfLifePatches
    {
        [HarmonyPatch(typeof(GameplayMaster), "Start")]
        private static class GameplayStartPatch
        {
            [HarmonyPostfix]
            private static void AfterGameplayStart()
            {
                QualityOfLifeController.Instance?.ResetRunState();
            }
        }

        [HarmonyPatch(typeof(PowerupScript), "ThrowAway")]
        private static class CharmDiscardPatch
        {
            [HarmonyPostfix]
            private static void AfterCharmDiscard()
            {
                QualityOfLifeController.Instance?.NotifyCharmDiscard();
            }
        }

        [HarmonyPatch(typeof(CardsPackScript), "Animator_PackPunch")]
        private static class PackPunchPatch
        {
            [HarmonyPrefix]
            private static bool BeforePackPunch()
            {
                return Plugin.Settings?.SkipMemoryPackPunch.Value != true;
            }
        }

        [HarmonyPatch(typeof(CardScript), "Update")]
        private static class AutoFlipCardPatch
        {
            [HarmonyPostfix]
            private static void AfterCardUpdate(CardScript __instance)
            {
                if (Plugin.Settings?.AutoFlipMemoryPackCards.Value == true &&
                    MemoryPackDealUI.IsDealRunnning() &&
                    __instance.IsFaceDown())
                {
                    __instance.FlipRequest();
                }
            }
        }

        [HarmonyPatch(typeof(Panik.Controls), nameof(Panik.Controls.ActionButton_PressedGet),
            new[] { typeof(int), typeof(Panik.Controls.InputAction), typeof(bool) })]
        private static class MemoryPackContinuePatch
        {
            [HarmonyPostfix]
            private static void AfterActionRead(Panik.Controls.InputAction action, ref bool __result)
            {
                if (Plugin.Settings?.FastMemoryPackFlow.Value == true &&
                    MemoryPackDealUI.IsDealRunnning() &&
                    action == Panik.Controls.InputAction.menuSelect)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch]
        private static class MemoryPackDealFastForwardPatch
        {
            private static FieldInfo timerField;
            private static FieldInfo depositTimerField;
            private static FieldInfo scaleField;

            private static MethodBase TargetMethod()
            {
                MethodInfo coroutineMethod = AccessTools.Method(typeof(MemoryPackDealUI), "DealCoroutine");
                return AccessTools.EnumeratorMoveNext(coroutineMethod);
            }

            [HarmonyPrefix]
            private static void BeforeMoveNext(object __instance)
            {
                if (Plugin.Settings?.FastMemoryPackFlow.Value != true || __instance == null)
                {
                    return;
                }

                if (timerField == null)
                {
                    timerField = AccessTools.Field(__instance.GetType(), "<timer>5__2");
                    depositTimerField = AccessTools.Field(__instance.GetType(), "<depositDelayTimer>5__9");
                    scaleField = AccessTools.Field(__instance.GetType(), "<scale>5__11");
                }

                timerField?.SetValue(__instance, -1f);
                depositTimerField?.SetValue(__instance, -1f);
                scaleField?.SetValue(__instance, 1f);
            }
        }
    }
}
