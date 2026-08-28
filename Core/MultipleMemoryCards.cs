using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Panik;

namespace CloverMod.Core
{
    internal static class MultipleMemoryCards
    {
        private static readonly HashSet<RunModifierScript.Identifier> configuredCards =
            new HashSet<RunModifierScript.Identifier>();

        private static readonly HashSet<RunModifierScript.Identifier> activeRunCards =
            new HashSet<RunModifierScript.Identifier>();

        private static readonly HashSet<RunModifierScript.Identifier> appliedSetupCards =
            new HashSet<RunModifierScript.Identifier>();

        private static readonly FieldInfo CurrentCardField =
            AccessTools.Field(typeof(GameplayData), "runModifierPicked");

        private static RunModifierScript.Identifier primaryCard = RunModifierScript.Identifier.defaultModifier;

        internal static bool Enabled => Plugin.Settings?.MultipleMemoryCardsEnabled.Value == true;

        internal static IReadOnlyCollection<RunModifierScript.Identifier> ConfiguredCards => configuredCards;

        internal static IReadOnlyCollection<RunModifierScript.Identifier> ActiveRunCards => activeRunCards;

        internal static RunModifierScript.Identifier PrimaryCard => primaryCard;

        internal static bool HasAdditionalActiveCards => Enabled && activeRunCards.Count > 0;

        internal static bool IsValidCard(RunModifierScript.Identifier identifier)
        {
            int value = (int)identifier;
            return value > (int)RunModifierScript.Identifier.defaultModifier &&
                   value < (int)RunModifierScript.Identifier.count;
        }

        internal static bool AddConfigured(RunModifierScript.Identifier identifier)
        {
            if (!IsValidCard(identifier))
            {
                return false;
            }

            bool added = configuredCards.Add(identifier);
            if (added)
            {
                SaveConfigured();
                ActivateForCurrentRun(identifier);
            }

            return added;
        }

        internal static bool RemoveConfigured(RunModifierScript.Identifier identifier)
        {
            bool removed = configuredCards.Remove(identifier);
            if (removed)
            {
                activeRunCards.Remove(identifier);
                SaveConfigured();
            }

            return removed;
        }

        internal static void ClearConfigured()
        {
            if (configuredCards.Count == 0)
            {
                return;
            }

            configuredCards.Clear();
            activeRunCards.Clear();
            SaveConfigured();
        }

        internal static bool IsConfigured(RunModifierScript.Identifier identifier)
        {
            return configuredCards.Contains(identifier);
        }

        internal static void SelectAllConfigured()
        {
            foreach (RunModifierScript.Identifier identifier in Enum.GetValues(typeof(RunModifierScript.Identifier)))
            {
                if (IsValidCard(identifier))
                {
                    configuredCards.Add(identifier);
                }
            }

            SaveConfigured();
            RefreshActiveCards();
            ApplyPendingSetupEffects();
        }

        internal static void BeginRun()
        {
            activeRunCards.Clear();
            appliedSetupCards.Clear();
            primaryCard = GetCurrentCard();
            RefreshActiveCards();
        }

        internal static void CurrentCardChanged(RunModifierScript.Identifier identifier, bool applySetupEffects)
        {
            primaryCard = identifier;
            RefreshActiveCards();
            if (applySetupEffects)
            {
                ApplyPendingSetupEffects();
            }
        }

        internal static void EnabledChanged()
        {
            if (!Enabled)
            {
                activeRunCards.Clear();
                return;
            }

            RefreshActiveCards();
            ApplyPendingSetupEffects();
        }

        internal static bool IsActive(RunModifierScript.Identifier identifier)
        {
            return primaryCard == identifier ||
                   GetCurrentCard() == identifier ||
                   (Enabled && activeRunCards.Contains(identifier));
        }

        internal static bool IsAdditionalActive(RunModifierScript.Identifier identifier)
        {
            return Enabled &&
                   primaryCard != identifier &&
                   activeRunCards.Contains(identifier);
        }

        internal static bool NeedsManualEffect(RunModifierScript.Identifier identifier)
        {
            return IsActive(identifier) && GetCurrentCard() != identifier;
        }

        internal static CurrentCardOverride OverrideCurrentCard(RunModifierScript.Identifier identifier)
        {
            if (!IsAdditionalActive(identifier) ||
                !CanUseCard(identifier) ||
                GameplayData.Instance == null ||
                CurrentCardField == null)
            {
                return new CurrentCardOverride(false, GetCurrentCard());
            }

            RunModifierScript.Identifier previous = GetCurrentCard();
            CurrentCardField.SetValue(GameplayData.Instance, identifier);
            return new CurrentCardOverride(true, previous);
        }

        internal static void RestoreCurrentCard(CurrentCardOverride state)
        {
            if (state.Changed && GameplayData.Instance != null && CurrentCardField != null)
            {
                CurrentCardField.SetValue(GameplayData.Instance, state.Previous);
            }
        }

        internal static void Initialize()
        {
            string configuredText = Plugin.Settings?.AdditionalMemoryCards.Value;
            LoadConfigured(configuredText);
        }

        internal static void LoadConfigured(string configuredText)
        {
            configuredCards.Clear();

            if (string.IsNullOrWhiteSpace(configuredText))
            {
                return;
            }

            string[] entries = configuredText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string entry in entries)
            {
                string cardName = entry.Trim();
                if (!Enum.TryParse(cardName, true, out RunModifierScript.Identifier identifier))
                {
                    Plugin.Log?.LogWarning($"Unknown memory card in configuration: {cardName}");
                    continue;
                }

                if (!IsValidCard(identifier))
                {
                    Plugin.Log?.LogWarning($"Invalid memory card in configuration: {cardName}");
                    continue;
                }

                configuredCards.Add(identifier);
            }

            Plugin.Log?.LogInfo($"Loaded {configuredCards.Count} additional memory cards.");
        }

        private static void RefreshActiveCards()
        {
            activeRunCards.Clear();
            if (!Enabled)
            {
                return;
            }

            foreach (RunModifierScript.Identifier identifier in configuredCards)
            {
                if (IsValidCard(identifier) && identifier != primaryCard)
                {
                    activeRunCards.Add(identifier);
                }
            }
        }

        private static string SerializeConfigured()
        {
            List<string> cardNames = new List<string>();
            foreach (RunModifierScript.Identifier identifier in configuredCards)
            {
                cardNames.Add(identifier.ToString());
            }

            cardNames.Sort(StringComparer.Ordinal);
            return string.Join(",", cardNames);
        }

        private static void SaveConfigured()
        {
            if (Plugin.Settings != null)
            {
                Plugin.Settings.AdditionalMemoryCards.Value = SerializeConfigured();
            }
        }

        private static void ActivateForCurrentRun(RunModifierScript.Identifier identifier)
        {
            if (!Enabled || GameplayMaster.instance == null || identifier == primaryCard)
            {
                return;
            }

            activeRunCards.Add(identifier);
            ApplySetupEffect(identifier);
        }

        private static void ApplyPendingSetupEffects()
        {
            if (!Enabled || GameplayMaster.instance == null)
            {
                return;
            }

            List<RunModifierScript.Identifier> orderedCards = new List<RunModifierScript.Identifier>(activeRunCards);
            orderedCards.Sort((left, right) => ((int)left).CompareTo((int)right));

            foreach (RunModifierScript.Identifier identifier in orderedCards)
            {
                ApplySetupEffect(identifier);
            }
        }

        private static void ApplySetupEffect(RunModifierScript.Identifier identifier)
        {
            if (!CanUseCard(identifier) || !appliedSetupCards.Add(identifier))
            {
                return;
            }

            try
            {
                RunModifierScript.OnRunModifierSet(identifier);
                Plugin.Log?.LogInfo($"Activated additional memory card: {identifier}");
            }
            catch (Exception exception)
            {
                appliedSetupCards.Remove(identifier);
                Plugin.Log?.LogError($"Could not apply setup effect for memory card {identifier}: {exception}");
            }
        }

        private static bool CanUseCard(RunModifierScript.Identifier identifier)
        {
            if ((int)identifier < (int)RunModifierScript.Identifier.Fusion_ViciousCicle)
            {
                return true;
            }

            return PlatformAPI.instance != null &&
                   PlatformAPI.instance.OwnsDlc1_UnholyFusion();
        }

        private static RunModifierScript.Identifier GetCurrentCard()
        {
            return GameplayData.Instance == null
                ? RunModifierScript.Identifier.defaultModifier
                : GameplayData.RunModifier_GetCurrent();
        }

        internal readonly struct CurrentCardOverride
        {
            internal CurrentCardOverride(bool changed, RunModifierScript.Identifier previous)
            {
                Changed = changed;
                Previous = previous;
            }

            internal bool Changed { get; }

            internal RunModifierScript.Identifier Previous { get; }
        }
    }
}
