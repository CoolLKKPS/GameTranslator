using GameTranslator.Patches.Utils;
using HarmonyLib;
using System;
using System.Text;
using TMPro;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(TMP_Text))]
    internal class TMP_TextHook
    {
        [HarmonyPrefix]
        [HarmonyPatch("SetTextInternal")]
        public static void SetTextInternal(ref TMP_Text __instance, ref string sourceText)
        {
            TextTranslate.Instance.Hook_TextChanged(__instance, ref sourceText);
            ReplaceUnsupportedCharacters(ref sourceText, __instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch("SetText", new Type[]
        {
            typeof(string),
            typeof(bool)
        })]
        public static void SetText(ref TMP_Text __instance, ref string sourceText, bool syncTextInputBox)
        {
            TextTranslate.Instance.Hook_TextChanged(__instance, ref sourceText);
            ReplaceUnsupportedCharacters(ref sourceText, __instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch("SetText", new Type[]
        {
            typeof(string),
            typeof(float),
            typeof(float),
            typeof(float),
            typeof(float),
            typeof(float),
            typeof(float),
            typeof(float),
            typeof(float)
        })]
        public static void SetText(ref TMP_Text __instance, ref string sourceText, float arg0, float arg1, float arg2, float arg3, float arg4, float arg5, float arg6, float arg7)
        {
            TextTranslate.Instance.Hook_TextChanged(__instance, ref sourceText);
            ReplaceUnsupportedCharacters(ref sourceText, __instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch("SetText", new Type[]
        {
            typeof(StringBuilder),
            typeof(int),
            typeof(int)
        })]
        public static void SetText(ref TMP_Text __instance, ref StringBuilder sourceText, int start, int length)
        {
            string text = sourceText.ToString();
            TextTranslate.Instance.Hook_TextChanged(__instance, ref text);
            ReplaceUnsupportedCharacters(ref text, __instance);
            sourceText = new StringBuilder(text);
        }

        [HarmonyPrefix]
        [HarmonyPatch("text", MethodType.Setter)]
        public static void Change(ref TMP_Text __instance, ref string value)
        {
            TextTranslate.Instance.Hook_TextChanged(__instance, ref value);
            ReplaceUnsupportedCharacters(ref value, __instance);
        }

        private static void ReplaceUnsupportedCharacters(ref string text, TMP_Text textComponent)
        {
            if (string.IsNullOrEmpty(text) || !TranslatePlugin.replaceUnsupportedCharacters.Value)
                return;

            string replacedText = FontSupportChecker.ReplaceUnsupportedCharacters(text);
            if (replacedText != text)
            {
                text = replacedText;
            }
        }
    }
}
