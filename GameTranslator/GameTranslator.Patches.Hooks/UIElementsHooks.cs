using GameTranslator.Patches.Utils;
using HarmonyLib;
using UnityEngine.UIElements;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(TextElement))]
    internal class TextElement_text_Hook
    {
        [HarmonyPrefix]
        [HarmonyPatch("text", MethodType.Setter)]
        public static void Change(ref TextElement __instance, ref string value)
        {
            TextTranslate.Instance.OnTranslateIncomingText(__instance, ref value);
        }
    }
}
