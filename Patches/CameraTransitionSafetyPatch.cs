using HarmonyLib;

namespace CloverMod.Patches
{
    [HarmonyPatch(typeof(CameraController), "Update")]
    internal static class CameraTransitionSafetyPatch
    {
        private const float CameraPositionLerpFactorLimit = 0.95f;

        [HarmonyPrefix]
        private static void LimitCameraPositionStep(ref float ___lerpSpeedMultiplier)
        {
            float scaledDeltaTime = Panik.Tick.Time;
            if (scaledDeltaTime <= 0f || ___lerpSpeedMultiplier <= 0f)
            {
                return;
            }

            // CameraController updates position with:
            // position += (target - position) * (Tick.Time * 10 * lerpSpeedMultiplier).
            // A factor above 1 overshoots the target and can send the camera outside the room.
            float maximumSafeMultiplier = CameraPositionLerpFactorLimit / (scaledDeltaTime * 10f);
            if (___lerpSpeedMultiplier > maximumSafeMultiplier)
            {
                ___lerpSpeedMultiplier = maximumSafeMultiplier;
            }
        }
    }
}
