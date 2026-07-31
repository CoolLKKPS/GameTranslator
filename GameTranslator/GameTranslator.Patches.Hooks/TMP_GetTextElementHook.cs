using GameTranslator.Patches.Utils;
using HarmonyLib;
using System.Reflection;
using TMPro;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch]
    internal class TMP_GetTextElementHook
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(TMP_Text), "GetTextElement");
        }

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        public static void Postfix(uint unicode, ref TMP_TextElement __result)
        {
            if (__result == null)
                FontDynamicLoader.TryAddCharacterOnDemand(unicode);
        }
    }
}
