using GameTranslator.Patches.Utils;
using HarmonyLib;
using System;
using System.Reflection;
using XUnity.Common.Constants;
using XUnity.Common.Harmony;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch]
    internal static class TextWindow_FinishTyping_Hook
    {
        static bool Prepare()
        {
            return UnityTypes.TextWindow != null;
        }

        static MethodBase TargetMethod()
        {
            return AccessToolsShim.Method(UnityTypes.TextWindow.ClrType, "FinishTyping", new Type[0]);
        }

        [HarmonyPostfix]
        static void Postfix(object __instance)
        {
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var textMesh = __instance.GetType().GetField("TextMesh", flags)?.GetValue(__instance);
            if (textMesh != null)
            {
                TextTranslate._translatingFromFinishTyping = true;
                try
                {
                    TextTranslate.Instance.OnComponentTextChanged(textMesh);
                }
                finally
                {
                    TextTranslate._translatingFromFinishTyping = false;
                }
            }
        }
    }
}
