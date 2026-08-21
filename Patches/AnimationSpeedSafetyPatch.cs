using System;
using HarmonyLib;

namespace CloverMod.Patches
{
    [HarmonyPatch(typeof(PowerupTriggerAnimController))]
    internal static class AnimationSpeedSafetyPatch
    {
        internal const int MaximumActiveAnimationSpeed = 4;

        private static int requestedTransitionSpeed = 1;
        private static bool requestedSpeedInitialized;
        private static bool safetyLimitApplied;

        internal static int GetRequestedSpeed()
        {
            Panik.Data.SettingsData settings = Panik.Data.SettingsData.inst;
            if (!requestedSpeedInitialized)
            {
                requestedTransitionSpeed = settings?.transitionSpeed ?? 1;
                requestedSpeedInitialized = true;
            }
            else if (!safetyLimitApplied && settings != null)
            {
                requestedTransitionSpeed = settings.transitionSpeed;
            }

            return requestedTransitionSpeed;
        }

        internal static void SetRequestedSpeed(int speed)
        {
            requestedTransitionSpeed = speed;
            requestedSpeedInitialized = true;

            Panik.Data.SettingsData settings = Panik.Data.SettingsData.inst;
            if (settings == null)
            {
                return;
            }

            safetyLimitApplied = PowerupTriggerAnimController.HasAnimations();
            settings.transitionSpeed = safetyLimitApplied
                ? Math.Min(requestedTransitionSpeed, MaximumActiveAnimationSpeed)
                : requestedTransitionSpeed;
        }

        internal static void RestoreRequestedSpeed()
        {
            Panik.Data.SettingsData settings = Panik.Data.SettingsData.inst;
            if (settings != null && requestedSpeedInitialized)
            {
                settings.transitionSpeed = requestedTransitionSpeed;
            }

            safetyLimitApplied = false;
            requestedSpeedInitialized = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PowerupTriggerAnimController.AddAnimation))]
        private static void AfterAnimationQueued()
        {
            RefreshLimit();
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        private static void BeforeAnimationUpdate()
        {
            RefreshLimit();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        private static void AfterAnimationUpdate()
        {
            RefreshLimit();
        }

        private static void RefreshLimit()
        {
            Panik.Data.SettingsData settings = Panik.Data.SettingsData.inst;
            if (settings == null)
            {
                return;
            }

            bool animationActive = PowerupTriggerAnimController.HasAnimations();
            if (animationActive)
            {
                if (!requestedSpeedInitialized || !safetyLimitApplied)
                {
                    requestedTransitionSpeed = settings.transitionSpeed;
                    requestedSpeedInitialized = true;
                }

                settings.transitionSpeed = Math.Min(requestedTransitionSpeed, MaximumActiveAnimationSpeed);
                safetyLimitApplied = true;
                return;
            }

            if (safetyLimitApplied)
            {
                settings.transitionSpeed = requestedTransitionSpeed;
                safetyLimitApplied = false;
            }
            else
            {
                requestedTransitionSpeed = settings.transitionSpeed;
                requestedSpeedInitialized = true;
            }
        }
    }
}
