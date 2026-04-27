using System;
using System.Collections.Generic;

namespace EduLibrary.MinimapSystem
{
    public static class FastTravelManager
    {
        public static HashSet<string> UnlockedPOIs = new HashSet<string>();
        public static event Action<string, string> OnPOIUnlocked;

        public static void UnlockPOI(string elementID, string displayName)
        {
            if (UnlockedPOIs.Add(elementID))
            {
                OnPOIUnlocked?.Invoke(elementID, displayName);
            }
        }

        public static bool IsUnlocked(string elementID)
        {
            return UnlockedPOIs.Contains(elementID);
        }
    }
}