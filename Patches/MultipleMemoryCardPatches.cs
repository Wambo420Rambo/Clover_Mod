using System;
using System.Collections;
using System.Numerics;
using System.Reflection;
using CloverMod.Core;
using HarmonyLib;
using Panik;
using UnityEngine;

namespace CloverMod.Patches
{
    [HarmonyPatch(typeof(GameplayMaster), "Start")]
    internal static class MultipleMemoryCardsRunStartPatch
    {
        private static void Postfix()
        {
            MultipleMemoryCards.BeginRun();
        }
    }

    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.RunModifier_SetCurrent))]
    internal static class MultipleMemoryCardsCurrentCardPatch
    {
        private static void Postfix(RunModifierScript.Identifier identifier, bool setByPlayer)
        {
            MultipleMemoryCards.CurrentCardChanged(identifier, setByPlayer);
        }
    }

    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.SeedIsVerifiable))]
    internal static class MultipleMemoryCardsSeedVerificationPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (MultipleMemoryCards.HasAdditionalActiveCards)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.InterestRateGet))]
    internal static class MultipleMemoryCardsInterestRatePatch
    {
        private static void Postfix(ref float __result)
        {
            if (!MultipleMemoryCards.NeedsManualEffect(RunModifierScript.Identifier.interestsGrow))
            {
                return;
            }

            int debtIndex = GameplayData.DebtIndexGet().CastToInt();
            float bonus = Mathf.Min(2 * debtIndex, 16) * PowerupScript.EvilDealBonusMultiplier_Float();
            __result = Mathf.Clamp(__result + bonus, 0f, 100f);
        }
    }

    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData._GetDebtRoundDeadline_NextIncrement))]
    internal static class MultipleMemoryCardsDeadlineIncrementPatch
    {
        private static bool Prefix(ref int __result)
        {
            if (MultipleMemoryCards.IsActive(RunModifierScript.Identifier.oneRoundPerDeadline))
            {
                __result = 1;
                return false;
            }

            if (MultipleMemoryCards.IsActive(RunModifierScript.Identifier.smallRoundsMoreRounds))
            {
                __result = 7;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.DebtGetExt))]
    internal static class MultipleMemoryCardsDebtPatch
    {
        private static void Postfix(ref BigInteger __result)
        {
            if (MultipleMemoryCards.NeedsManualEffect(RunModifierScript.Identifier.bigDebt))
            {
                __result *= 2;
            }

            if (MultipleMemoryCards.NeedsManualEffect(RunModifierScript.Identifier.extraPacks))
            {
                __result /= 2;
            }
        }
    }

    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.SixSixSix_ChanceGet))]
    internal static class MultipleMemoryCardsSixChancePatch
    {
        private static readonly FieldInfo MaximumChanceField =
            AccessTools.Field(typeof(GameplayData), "_666ChanceMaxAbsolute");

        private static void Postfix(bool considerMaximum, ref float __result)
        {
            if (MultipleMemoryCards.NeedsManualEffect(RunModifierScript.Identifier._666BigBetDouble_SmallBetNoone) && GameplayMaster.GetGamePhase() == GameplayMaster.GamePhase.gambling)
            {
                __result *= GameplayData.LastBet_IsSmallGet() ? 0.5f : 2f;
            }

            if (MultipleMemoryCards.NeedsManualEffect(RunModifierScript.Identifier._666DoubleChances_JackpotRecovers))
            {
                __result *= 2f;
            }

            if (considerMaximum && GameplayData.Instance != null && MaximumChanceField != null)
            {
                float maximum = (float)MaximumChanceField.GetValue(GameplayData.Instance);
                __result = Mathf.Min(__result, maximum);
            }
        }
    }

    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.RedButtonActivationsMultiplierGet))]
    internal static class MultipleMemoryCardsRedButtonMultiplierPatch
    {
        private static void Postfix(ref int __result)
        {
            if (MultipleMemoryCards.NeedsManualEffect(RunModifierScript.Identifier.redButtonOverload))
            {
                __result++;
            }
        }
    }

    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.PhonePickMultiplierGet))]
    internal static class MultipleMemoryCardsPhoneMultiplierPatch
    {
        private static void Postfix(ref int __result)
        {
            if (MultipleMemoryCards.NeedsManualEffect(RunModifierScript.Identifier.phoneEnhancer))
            {
                __result++;
            }
        }
    }

    [HarmonyPatch(typeof(PowerupScript), nameof(PowerupScript.IsBanned))]
    internal static class MultipleMemoryCardsSmallItemPoolPatch
    {
        private static void Postfix(PowerupScript.Archetype archetype, ref bool __result)
        {
            if (__result || !MultipleMemoryCards.IsActive(RunModifierScript.Identifier.smallItemPool))
            {
                return;
            }

            int value = (int)archetype;
            int goldenSymbols = (int)PowerupScript.Archetype.goldenSymbols;
            __result = archetype == PowerupScript.Archetype.symbolInstants || (value >= goldenSymbols && value <= goldenSymbols + 2);
        }
    }

    [HarmonyPatch(typeof(AbilityScript), nameof(AbilityScript.CanBePicked))]
    internal static class MultipleMemoryCardsStoreAbilityPatch
    {
        private static bool Prefix(AbilityScript.Identifier ___identifier, ref bool __result)
        {
            if (!MultipleMemoryCards.IsActive(RunModifierScript.Identifier.allCharmsStoreModded))
            {
                return true;
            }

            if (___identifier != AbilityScript.Identifier.evilGeneric_ShinyObjects &&
                ___identifier != AbilityScript.Identifier.holyGeneric_ModifyStoreCharms_Make1Free)
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(StoreCapsuleScript), "PowerupDiscountGet")]
    internal static class MultipleMemoryCardsStoreDiscountPatch
    {
        private static void Postfix(ref long __result)
        {
            if (MultipleMemoryCards.NeedsManualEffect(RunModifierScript.Identifier.lessSpaceMoreDiscount))
            {
                __result++;
            }
        }
    }

    [HarmonyPatch(typeof(StoreCapsuleScript), nameof(StoreCapsuleScript.IsEnabled))]
    internal static class MultipleMemoryCardsSmallerStorePatch
    {
        private static void Postfix(int ___id, bool ___isRefreshButton, ref bool __result)
        {
            if (MultipleMemoryCards.IsActive(RunModifierScript.Identifier.smallerStore) &&
                ___id == 3 &&
                !___isRefreshButton)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(StoreCapsuleScript), "Update")]
    internal static class MultipleMemoryCardsSmallerStoreVisibilityPatch
    {
        private static void Postfix(StoreCapsuleScript __instance, int ___id, GameObject ___woodenPlanksHolder)
        {
            if (___id != 3 || ___woodenPlanksHolder == null)
            {
                return;
            }

            bool shouldBeBlocked = MultipleMemoryCards.IsActive(RunModifierScript.Identifier.smallerStore);
            if (___woodenPlanksHolder.activeSelf == shouldBeBlocked)
            {
                return;
            }

            ___woodenPlanksHolder.SetActive(shouldBeBlocked);
            if (shouldBeBlocked && StoreCapsuleScript.storePowerups != null &&
                ___id < StoreCapsuleScript.storePowerups.Length)
            {
                StoreCapsuleScript.storePowerups[___id] = null;
            }

            __instance.RefreshCostText();
            PowerupScript.RefreshPlacementAll();
        }
    }

    [HarmonyPatch(typeof(RunModifierScript), nameof(RunModifierScript.TriggerAnimation_IfEquipped))]
    internal static class MultipleMemoryCardsAnimationPatch
    {
        private static bool Prefix(RunModifierScript.Identifier desiredRunModifier)
        {
            if (!MultipleMemoryCards.NeedsManualEffect(desiredRunModifier))
            {
                return true;
            }

            RunModifierScript.TriggerAnimation(desiredRunModifier);
            return false;
        }
    }

    [HarmonyPatch(typeof(RunModifierScript), nameof(RunModifierScript.MFunc_LivingOnTheEdge_DontAllowDeposit))]
    internal static class MultipleMemoryCardsLivingOnTheEdgePatch
    {
        private static void Postfix(ref bool __result)
        {
            if (!__result &&
                OwnsFusionDlc() &&
                MultipleMemoryCards.IsActive(RunModifierScript.Identifier.Fusion_LivingOnTheEdge) &&
                GameplayData.RoundsLeftToDeadline() > 0)
            {
                __result = true;
            }
        }

        private static bool OwnsFusionDlc()
        {
            return PlatformAPI.instance != null && PlatformAPI.instance.OwnsDlc1_UnholyFusion();
        }
    }

    [HarmonyPatch(typeof(RunModifierScript), nameof(RunModifierScript.MFunc_Consequences_CostGet))]
    internal static class MultipleMemoryCardsConsequencesPatch
    {
        private static void Postfix(ref long __result)
        {
            if (__result < 0L &&
                PlatformAPI.instance != null &&
                PlatformAPI.instance.OwnsDlc1_UnholyFusion() &&
                MultipleMemoryCards.IsActive(RunModifierScript.Identifier.Fusion_Consequences))
            {
                __result = GameplayData.RunModSpecific_FusionConsequences_Price;
            }
        }
    }

    [HarmonyPatch(typeof(RunModifierScript), nameof(RunModifierScript.MFunc_HerdMentality_CopyModChance))]
    internal static class MultipleMemoryCardsHerdMentalityPatch
    {
        private static void Postfix(ref float __result)
        {
            if (__result <= 0f &&
                PlatformAPI.instance != null &&
                PlatformAPI.instance.OwnsDlc1_UnholyFusion() &&
                MultipleMemoryCards.IsActive(RunModifierScript.Identifier.Fusion_HerdMentality))
            {
                __result = 0.05f;
            }
        }
    }

    [HarmonyPatch(typeof(RunModifierScript), nameof(RunModifierScript.MFunc_StuckInRoutine_IsActive))]
    internal static class MultipleMemoryCardsStuckInRoutinePatch
    {
        private static void Postfix(ref bool __result)
        {
            __result = __result || (PlatformAPI.instance != null && PlatformAPI.instance.OwnsDlc1_UnholyFusion() && MultipleMemoryCards.IsActive(RunModifierScript.Identifier.Fusion_StuckInARoutine));
        }
    }

    [HarmonyPatch(typeof(RunModifierScript), nameof(RunModifierScript.MFunc_BrightOutsideDarkInside_IsActive))]
    internal static class MultipleMemoryCardsBrightDarkPatch
    {
        private static void Postfix(ref bool __result)
        {
            __result = __result || (PlatformAPI.instance != null && PlatformAPI.instance.OwnsDlc1_UnholyFusion() && MultipleMemoryCards.IsActive(RunModifierScript.Identifier.Fusion_BrightOutsideDarkInside));
        }
    }

    internal static class MultipleMemoryCardsCutsceneContext
    {
        internal static int Depth { get; set; }
    }

    [HarmonyPatch(typeof(GameplayMaster), "CutscenePhaseBehaviour")]
    internal static class MultipleMemoryCardsCutscenePatch
    {
        private static void Prefix()
        {
            MultipleMemoryCardsCutsceneContext.Depth++;
        }

        private static Exception Finalizer(Exception __exception)
        {
            MultipleMemoryCardsCutsceneContext.Depth = Math.Max(0, MultipleMemoryCardsCutsceneContext.Depth - 1);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.InterestEarnedGrow))]
    internal static class MultipleMemoryCardsRoundInterestPatch
    {
        private static bool Prefix()
        {
            return MultipleMemoryCardsCutsceneContext.Depth <= 0 ||
                   !MultipleMemoryCards.NeedsManualEffect(RunModifierScript.Identifier.interestsGrow);
        }
    }

    internal static class MultipleMemoryCardsDebtClearContext
    {
        internal static int Depth { get; set; }

        internal static bool ApplyingInterest { get; set; }
    }

    [HarmonyPatch]
    internal static class MultipleMemoryCardsDebtClearPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(ATMScript), "DebtClearCoroutine"));
        }

        private static void Prefix()
        {
            MultipleMemoryCardsDebtClearContext.Depth++;
        }

        private static Exception Finalizer(Exception __exception)
        {
            MultipleMemoryCardsDebtClearContext.Depth = Math.Max(0, MultipleMemoryCardsDebtClearContext.Depth - 1);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.InterestEarnedGrow_Manual))]
    internal static class MultipleMemoryCardsDeadlineInterestPatch
    {
        private static void Postfix()
        {
            if (MultipleMemoryCardsDebtClearContext.Depth <= 0 || MultipleMemoryCardsDebtClearContext.ApplyingInterest || !MultipleMemoryCards.NeedsManualEffect(RunModifierScript.Identifier.interestsGrow))
            {
                return;
            }

            MultipleMemoryCardsDebtClearContext.ApplyingInterest = true;
            try
            {
                GameplayData.InterestEarnedGrow();
                RunModifierScript.TriggerAnimation_IfEquipped(RunModifierScript.Identifier.interestsGrow);
            }
            finally
            {
                MultipleMemoryCardsDebtClearContext.ApplyingInterest = false;
            }
        }
    }

    internal static class MultipleMemoryCardsDebtNextContext
    {
        internal static int Depth { get; set; }
    }

    [HarmonyPatch(typeof(GameplayMaster), nameof(GameplayMaster.FCall_DebtNext))]
    internal static class MultipleMemoryCardsDebtNextPatch
    {
        private static void Prefix()
        {
            MultipleMemoryCardsDebtNextContext.Depth++;
        }

        private static void Postfix()
        {
            ApplyDrawerTableModification();
            ApplyDrawerGamble();
            ApplyPhoneEnhancer();
            ApplySmallerStoreBonus();
            ApplyCharmRecycling();
        }

        private static Exception Finalizer(Exception __exception)
        {
            MultipleMemoryCardsDebtNextContext.Depth = Math.Max(0, MultipleMemoryCardsDebtNextContext.Depth - 1);
            return __exception;
        }

        private static void ApplyDrawerTableModification()
        {
            RunModifierScript.Identifier card = RunModifierScript.Identifier.drawerTableModifications;
            if (!MultipleMemoryCards.NeedsManualEffect(card))
            {
                return;
            }

            bool changed = false;
            PowerupScript tableCharm = AbilityScript.GetRandomCharmToModify_OnTableOrSlot();
            if (tableCharm != null)
            {
                changed = true;
                GameplayData.Powerup_Modifier_Set(tableCharm.identifier, PowerupScript.Modifier.devious, true);
            }

            PowerupScript drawerCharm = AbilityScript.GetRandomCharmToModify_InDrawers();
            if (drawerCharm != null)
            {
                changed = true;
                drawerCharm.ModifierReEvaluate(false, true);
            }

            if (changed)
            {
                RunModifierScript.TriggerAnimation_IfEquipped(card);
            }
        }

        private static void ApplyDrawerGamble()
        {
            RunModifierScript.Identifier card = RunModifierScript.Identifier.drawerModGamble;
            if (!MultipleMemoryCards.NeedsManualEffect(card))
            {
                return;
            }

            bool discard = R.Rng_RunMod.FlipCoin;
            RunModifierScript.TriggerAnimation_IfEquipped(card);
            for (int index = 0; index < PowerupScript.array_InDrawer.Length; index++)
            {
                PowerupScript charm = PowerupScript.array_InDrawer[index];
                if (charm == null)
                {
                    continue;
                }

                if (discard)
                {
                    PowerupScript.ThrowAway(charm.identifier, false);
                }
                else
                {
                    charm.ModifierReEvaluate(false, true);
                }
            }
        }

        private static void ApplyPhoneEnhancer()
        {
            RunModifierScript.Identifier card = RunModifierScript.Identifier.phoneEnhancer;
            if (!MultipleMemoryCards.NeedsManualEffect(card))
            {
                return;
            }

            for (int index = 0; index < StoreCapsuleScript.storePowerups.Length; index++)
            {
                StoreCapsuleScript.storePowerups[index] = null;
            }

            StoreCapsuleScript.RefreshCostTextAll();
            RunModifierScript.TriggerAnimation_IfEquipped(card);
        }

        private static void ApplySmallerStoreBonus()
        {
            RunModifierScript.Identifier card = RunModifierScript.Identifier.smallerStore;
            if (MultipleMemoryCards.NeedsManualEffect(card))
            {
                RunModifierScript.MFunc_SmallerStoreRestocksBonus();
                RunModifierScript.TriggerAnimation_IfEquipped(card);
            }
        }

        private static void ApplyCharmRecycling()
        {
            RunModifierScript.Identifier card = RunModifierScript.Identifier.charmsRecycling;
            if (!MultipleMemoryCards.NeedsManualEffect(card) ||
                PowerupScript.list_EquippedNormal.Count <= 5)
            {
                return;
            }

            PowerupScript selected = null;
            int attempts = 100;
            while (selected == null && attempts-- > 0)
            {
                int index = R.Rng_RunMod.Range(0, PowerupScript.list_EquippedNormal.Count);
                selected = PowerupScript.list_EquippedNormal[index];
            }

            if (selected != null)
            {
                RunModifierScript.TriggerAnimation_IfEquipped(card);
                PowerupScript.ThrowAway(selected.identifier, false);
                GameplayData.StoreFreeRestocksSet(GameplayData.StoreFreeRestocksGet() + 2L);
            }
        }
    }

    [HarmonyPatch(typeof(RedButtonScript), nameof(RedButtonScript.RestoreCharges))]
    internal static class MultipleMemoryCardsRedButtonRechargePatch
    {
        private static bool Prefix()
        {
            bool protectedContext = MultipleMemoryCardsCutsceneContext.Depth > 0 ||
                                    MultipleMemoryCardsDebtNextContext.Depth > 0;
            return !protectedContext || !MultipleMemoryCards.NeedsManualEffect(RunModifierScript.Identifier.redButtonOverload);
        }
    }

    [HarmonyPatch(typeof(GameplayMaster), "_GotoGambling")]
    internal static class MultipleMemoryCardsGuaranteedSixPatch
    {
        private static void Postfix()
        {
            RunModifierScript.Identifier card = RunModifierScript.Identifier._666LastRoundGuaranteed;
            if (!MultipleMemoryCards.NeedsManualEffect(card) || GameplayData.RoundsLeftToDeadline() != 0 || GameplayData.DebtIndexGet() < GameplayData.SixSixSix_GetMinimumDebtIndex() || GameplayData.SpinsLeftGet() <= 0)
            {
                return;
            }

            GameplayData.SixSixSix_BookedSpinSet(R.Rng_RunMod.Range(0, GameplayData.SpinsLeftGet()));
            RunModifierScript.TriggerAnimation_IfEquipped(card);
        }
    }

    [HarmonyPatch(typeof(StoreCapsuleScript), nameof(StoreCapsuleScript.Restock))]
    internal static class MultipleMemoryCardsStoreRestockPatch
    {
        private static void Prefix(out MultipleMemoryCards.CurrentCardOverride __state)
        {
            __state = MultipleMemoryCards.OverrideCurrentCard(RunModifierScript.Identifier.allCharmsStoreModded);
        }

        private static void Postfix()
        {
            if (!MultipleMemoryCards.IsActive(RunModifierScript.Identifier.smallerStore) ||
                StoreCapsuleScript.storePowerups == null ||
                StoreCapsuleScript.storePowerups.Length <= 3)
            {
                return;
            }

            StoreCapsuleScript.storePowerups[3] = null;
            StoreCapsuleScript.RefreshCostTextAll();
        }

        private static Exception Finalizer(Exception __exception, MultipleMemoryCards.CurrentCardOverride __state)
        {
            MultipleMemoryCards.RestoreCurrentCard(__state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(PhoneUiScript), "DefinePhoneStuff")]
    internal static class MultipleMemoryCardsViciousCirclePatch
    {
        private static void Prefix(out MultipleMemoryCards.CurrentCardOverride __state)
        {
            __state = MultipleMemoryCards.OverrideCurrentCard(RunModifierScript.Identifier.Fusion_ViciousCicle);
        }

        private static Exception Finalizer(Exception __exception, MultipleMemoryCards.CurrentCardOverride __state)
        {
            MultipleMemoryCards.RestoreCurrentCard(__state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(GameplayMaster), nameof(GameplayMaster.FCall_SlotMachineTurnOnTry))]
    internal static class MultipleMemoryCardsOneRoundFailsafePatch
    {
        private static void Prefix(out MultipleMemoryCards.CurrentCardOverride __state)
        {
            __state = MultipleMemoryCards.OverrideCurrentCard(RunModifierScript.Identifier.oneRoundPerDeadline);
        }

        private static Exception Finalizer(Exception __exception, MultipleMemoryCards.CurrentCardOverride __state)
        {
            MultipleMemoryCards.RestoreCurrentCard(__state);
            return __exception;
        }
    }

    [HarmonyPatch]
    internal static class MultipleMemoryCardsJackpotRecoveryPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(SlotMachineScript), "PatternsCompute_Coroutine"));
        }

        private static void Prefix(out MultipleMemoryCards.CurrentCardOverride __state)
        {
            __state = MultipleMemoryCards.OverrideCurrentCard(
                RunModifierScript.Identifier._666DoubleChances_JackpotRecovers);
        }

        private static Exception Finalizer(Exception __exception, MultipleMemoryCards.CurrentCardOverride __state)
        {
            MultipleMemoryCards.RestoreCurrentCard(__state);
            return __exception;
        }
    }

    [HarmonyPatch]
    internal static class MultipleMemoryCardsDangerousExperimentsPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(SurgeryMachine), "SurgeryInProgress"));
        }

        private static void Prefix(out MultipleMemoryCards.CurrentCardOverride __state)
        {
            __state = MultipleMemoryCards.OverrideCurrentCard(RunModifierScript.Identifier.Fusion_DangerousExperiments);
        }

        private static Exception Finalizer(Exception __exception, MultipleMemoryCards.CurrentCardOverride __state)
        {
            MultipleMemoryCards.RestoreCurrentCard(__state);
            return __exception;
        }
    }
}
