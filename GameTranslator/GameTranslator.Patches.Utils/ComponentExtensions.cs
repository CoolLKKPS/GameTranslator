using UnityEngine;

namespace GameTranslator.Patches.Utils
{
    internal static class ComponentExtensions
    {
        private static bool _guiContentCheckFailed = false;

        public static bool SupportsStabilization(this object ui)
        {
            if (ui == null) return false;
            return _guiContentCheckFailed || !IsGUIContentSafe(ui);
        }

        private static bool IsGUIContentSafe(object ui)
        {
            try
            {
                return ui is GUIContent;
            }
            catch
            {
                _guiContentCheckFailed = true;
            }
            return false;
        }
    }
}