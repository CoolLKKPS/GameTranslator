using GameTranslator.Patches.Utils;
using HarmonyLib;
using UnityEngine.UI;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(Text))]
    internal class TextHook
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnEnable")]
        public static void Change(ref Text __instance)
        {
            TextTranslate.Instance.OnComponentTextChanged(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch("text", MethodType.Setter)]
        public static void Change(ref Text __instance, ref string value)
        {
            TextTranslate.Instance.OnTranslateIncomingText(__instance, ref value);
        }
    }
}
