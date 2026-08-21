using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using CloverMod.Configuration;
using CloverMod.Patches;
using UnityEngine;

namespace CloverMod.Core
{
    internal sealed class QualityOfLifeController : IDisposable
    {
        private static readonly PowerupScript.Identifier[] SkeletonParts =
        {
            PowerupScript.Identifier.Skeleton_Arm1,
            PowerupScript.Identifier.Skeleton_Arm2,
            PowerupScript.Identifier.Skeleton_Leg1,
            PowerupScript.Identifier.Skeleton_Leg2,
        };

        private readonly ModConfig config;
        private readonly ManualLogSource log;

        private bool introSkipRequested;
        private bool corpseCompletionAttempted;
        private bool phaseProfileActive;
        private float originalGameSpeed = 1f;
        private int originalTransitionSpeed = 1;
        private float discardBoostRemaining;

        internal QualityOfLifeController(ModConfig config, ManualLogSource log)
        {
            this.config = config;
            this.log = log;
            Instance = this;
        }

        internal static QualityOfLifeController Instance { get; private set; }

        internal void Update()
        {
            UpdateIntroSkip();
            UpdateCorpseCompletion();
            UpdatePhaseSpeeds();
        }

        internal void ResetRunState()
        {
            corpseCompletionAttempted = false;
            discardBoostRemaining = 0f;
        }

        internal void NotifyCharmDiscard()
        {
            if (config.UsePhaseSpeedProfiles.Value)
            {
                discardBoostRemaining = 0.5f;
            }
        }

        public void Dispose()
        {
            RestoreOriginalSpeeds();
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        private void UpdateIntroSkip()
        {
            if (!config.AutoSkipIntro.Value)
            {
                introSkipRequested = false;
                return;
            }

            if (Panik.Level.CurrentScene != 1)
            {
                introSkipRequested = false;
                return;
            }

            if (introSkipRequested)
            {
                return;
            }

            introSkipRequested = true;
            log.LogInfo("QoL: skipping the intro scene.");
            Panik.Level.GoTo(2, true);
        }

        private void UpdateCorpseCompletion()
        {
            if (!config.AutoCompleteCorpse.Value || corpseCompletionAttempted ||
                GameplayMaster.GetGamePhase() != GameplayMaster.GamePhase.preparation)
            {
                return;
            }

            corpseCompletionAttempted = true;
            try
            {
                int availableSlots = Enumerable.Range(0, 4)
                    .Count(PowerupScript.IsDrawerAvailable);
                List<PowerupScript.Identifier> missingParts = SkeletonParts
                    .Where(part => !PowerupScript.IsInDrawer_Quick(part) &&
                                   !PowerupScript.IsEquipped_Quick(part))
                    .Take(availableSlots)
                    .ToList();

                int added = missingParts.Count(PowerupScript.PutInDrawer_Quick);
                log.LogInfo($"QoL: added {added} missing corpse piece(s) to available drawers.");
            }
            catch (Exception exception)
            {
                log.LogWarning($"QoL corpse completion failed safely: {exception.Message}");
            }
        }

        private void UpdatePhaseSpeeds()
        {
            if (!config.UsePhaseSpeedProfiles.Value)
            {
                RestoreOriginalSpeeds();
                return;
            }

            if (!phaseProfileActive)
            {
                originalGameSpeed = Time.timeScale > 0f ? Time.timeScale : 1f;
                originalTransitionSpeed = AnimationSpeedSafetyPatch.GetRequestedSpeed();
                phaseProfileActive = true;
            }

            GameplayMaster.GamePhase phase = GameplayMaster.GetGamePhase();
            if (discardBoostRemaining > 0f && phase == GameplayMaster.GamePhase.preparation)
            {
                discardBoostRemaining -= Time.unscaledDeltaTime;
                ApplySpeeds(config.CharmDiscardSpeed.Value, config.CharmDiscardSpeed.Value);
                return;
            }

            discardBoostRemaining = 0f;
            if (phase == GameplayMaster.GamePhase.cutscene)
            {
                ApplySpeeds(config.CutsceneGameSpeed.Value, config.NormalPhaseSpeed.Value);
                return;
            }

            if (phase == GameplayMaster.GamePhase.gambling)
            {
                int animationSpeed = GameplayData.SpinsWithAtLeast1Jackpot_Get() > 0
                    ? config.JackpotAnimationSpeed.Value
                    : config.GamblingAnimationSpeed.Value;
                ApplySpeeds(config.NormalPhaseSpeed.Value, animationSpeed);
                return;
            }

            ApplySpeeds(config.NormalPhaseSpeed.Value, config.NormalPhaseSpeed.Value);
        }

        private void ApplySpeeds(int gameSpeed, int animationSpeed)
        {
            if (Math.Abs(Time.timeScale - gameSpeed) > 0.001f)
            {
                Time.timeScale = gameSpeed;
            }

            if (AnimationSpeedSafetyPatch.GetRequestedSpeed() != animationSpeed)
            {
                AnimationSpeedSafetyPatch.SetRequestedSpeed(animationSpeed);
            }
        }

        private void RestoreOriginalSpeeds()
        {
            if (!phaseProfileActive)
            {
                return;
            }

            Time.timeScale = originalGameSpeed;
            AnimationSpeedSafetyPatch.SetRequestedSpeed(originalTransitionSpeed);
            phaseProfileActive = false;
            discardBoostRemaining = 0f;
        }
    }
}
