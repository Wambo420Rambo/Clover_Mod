using System.Reflection;
using UnityEngine;

namespace CloverMod
{
    public static class PhoneCheat
    {
        public static void TriggerPhoneTransformation(System.Type gameplayDataType)
        {
            GameplayData gameplayData = GameplayData.Instance;
            gameplayData._phone_pickedUpOnceLastDeadline = false;
            gameplayData._phone_bookSpecialCall = true;
            gameplayData._phoneAlreadyTransformed = false;
            gameplayData._phone_SpecialCalls_Counter = 3;

            var phoneUiScriptType = typeof(PhoneUiScript);
            var phoneUiScriptInstance = Object.FindFirstObjectByType(phoneUiScriptType);

            if (phoneUiScriptInstance != null)
            {
                MethodInfo method = phoneUiScriptType.GetMethod("DefinePhoneStuff", BindingFlags.NonPublic | BindingFlags.Instance);
                method?.Invoke(phoneUiScriptInstance, new object[] { true });
            }
        }
    }
}
