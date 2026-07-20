using GameTranslator.Patches.Utils;
using HarmonyLib;
using System;
using TMPro;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(TextMeshProUGUI))]
    internal class TeshMeshProUGUIHook
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnEnable")]
        public static void OnEnablePostfix(TextMeshProUGUI __instance)
        {
            try
            {
                TextTranslate.Instance.OnComponentTextChanged(__instance);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError($"Error in TeshMeshProUGUIHook.OnEnable: {ex.Message}");
            }
        }
    }
}
