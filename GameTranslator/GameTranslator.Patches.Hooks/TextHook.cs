using GameTranslator.Patches.Utils;
using HarmonyLib;
using System;
using UnityEngine.UI;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(Text))]
    internal class TextHook
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnEnable")]
        public static void Change(Text __instance)
        {
            try
            {
                TextTranslate.Instance.OnComponentTextChanged(__instance);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError($"Error in TextHook.OnEnable: {ex.Message}");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("text", MethodType.Setter)]
        public static void Change(Text __instance, ref string value)
        {
            TextTranslate.Instance.OnTranslateIncomingText(__instance, ref value);
        }
    }
}
