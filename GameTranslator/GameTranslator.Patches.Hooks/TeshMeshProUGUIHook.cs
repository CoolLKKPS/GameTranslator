using GameTranslator.Patches.Utils;
using HarmonyLib;
using System;
using TMPro;
#if IL2CPP
using Il2CppInterop.Runtime.InteropTypes;
using XUnity.Common.Utilities;
#endif

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(TextMeshProUGUI))]
    internal class TeshMeshProUGUIHook
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnEnable")]
#if MANAGED
        public static void Change(TextMeshProUGUI __instance)
#else
        public static void Change(Il2CppObjectBase __instance)
#endif
        {
            try
            {
#if IL2CPP
                var inst = Il2CppUtilities.CreateProxyComponentWithDerivedType(__instance.Pointer, typeof(TextMeshProUGUI));
                TextTranslate.Instance.OnComponentTextChanged(inst);
#else
                TextTranslate.Instance.OnComponentTextChanged(__instance);
#endif
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError($"Error in TeshMeshProUGUIHook.OnEnable: {ex.Message}");
            }
        }
    }
}
