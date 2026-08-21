using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CloverMod.Patches
{
    internal static class SlotMachineAutoPatch
    {
        private const float AutoSpinPause = 0.2f;

        private static readonly FieldInfo CoinVisualizersField =
            AccessTools.Field(typeof(SlotMachineScript), "coinsVisualizers");
        private static readonly MethodInfo StopWinTextMethod =
            AccessTools.Method(typeof(SlotMachineScript), "SpinWinText_StopIfAny");

        private static bool lastAutoEnabled;
        private static float autoSpinDelay;

        private static bool AutoEnabled => Plugin.Settings?.AutoSlotMode.Value == true;

        internal static void StopAutoMode()
        {
            lastAutoEnabled = false;
            autoSpinDelay = 0f;
        }

        [HarmonyPatch(typeof(SlotMachineScript), "Update")]
        private static class SlotUpdatePatch
        {
            [HarmonyPrefix]
            private static void BeforeUpdate(SlotMachineScript __instance)
            {
                if (!AutoEnabled)
                {
                    StopAutoMode();
                    return;
                }

                if (!lastAutoEnabled)
                {
                    lastAutoEnabled = true;
                    autoSpinDelay = 0f;
                    Plugin.Log?.LogInfo("Auto slot mode is ready to start spins.");
                }

                if (!Panik.Tick.IsGameRunning || Time.timeScale <= 0f)
                {
                    return;
                }

                SlotMachineScript.State state = SlotMachineScript.StateGet();
                if (state == SlotMachineScript.State.spinning)
                {
                    autoSpinDelay = AutoSpinPause;
                    return;
                }

                if (state != SlotMachineScript.State.idle || GameplayData.SpinsLeftGet() <= 0)
                {
                    return;
                }

                autoSpinDelay -= Panik.Tick.Time;
                if (autoSpinDelay > 0f || GameplayMaster.instance == null)
                {
                    return;
                }

                autoSpinDelay = AutoSpinPause;
                ClearPreviousWinPresentation(__instance);
                GameplayMaster.instance.FCall_SlotSpinTry(true);
            }
        }

        private static void ClearPreviousWinPresentation(SlotMachineScript slotMachine)
        {
            if (slotMachine == null)
            {
                return;
            }

            StopWinTextMethod?.Invoke(slotMachine, null);
            if (CoinVisualizersField?.GetValue(slotMachine) is CoinVisualizerScript[] visualizers)
            {
                CoinVisualizerScript.HideAll(visualizers);
            }

            if (CameraController.GetPositionKind() == CameraController.PositionKind.SlotCoinPlate_Fixed)
            {
                CameraController.SetPosition(
                    CameraController.PositionKind.Slot_Fixed,
                    true,
                    1f);
            }
        }
    }
}
