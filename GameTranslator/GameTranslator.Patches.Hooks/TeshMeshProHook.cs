using GameTranslator.Patches.Utils;
using HarmonyLib;
using System;
using TMPro;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(TextMeshPro))]
    internal class TeshMeshProHook
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnEnable")]
        public static void Change(TextMeshPro __instance)
        {
            try
            {
                TextTranslate.Instance.OnComponentTextChanged(__instance);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError($"Error in TeshMeshProHook.OnEnable: {ex.Message}");
            }
        }
    }
}
