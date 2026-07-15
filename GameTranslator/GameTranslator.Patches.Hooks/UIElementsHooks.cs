using GameTranslator.Patches.Utils;
using HarmonyLib;
using UnityEngine.UIElements;

namespace GameTranslator.Patches.Hooks
{
    internal static class UIElementsHooks
    {
        public static readonly System.Type[] All = new[] {
            typeof(TextElement_text_Hook),
        };
    }

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
