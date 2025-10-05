using System;
using System.Collections.Generic;
using System.Reflection;
using static PowerupScript;
using UnityEngine;

namespace CloverMod
{
    public static class CharmUnlockCheat
    {
        // List of all possible Identifier enum values except last 2 (non-charms)
        private static List<Identifier> lockedList = new List<Identifier>((Identifier[])Enum.GetValues(typeof(Identifier)));

        static CharmUnlockCheat()
        {
            // Remove last 2 entries (non-charms)
            if (lockedList.Count >= 2)
            {
                lockedList.RemoveAt(lockedList.Count - 1);
                lockedList.RemoveAt(lockedList.Count - 1);
            }
        }

        public static void UnlockAllLockedCharms(object gameplayDataInstance)
        {
            // Print all identifiers to the Unity console
            foreach (var powerup in lockedList)
            {
                Debug.Log($"CharmUnlockCheat: Identifier found: {powerup}");
            }

            // Get the Unlock static method from PowerupScript
            var unlockMethod = typeof(PowerupScript).GetMethod("Unlock", BindingFlags.Public | BindingFlags.Static);

            if (unlockMethod != null)
            {
                foreach (var powerup in lockedList)
                {
                    unlockMethod.Invoke(null, new object[] { powerup });
                }
            }
        }
    }
}