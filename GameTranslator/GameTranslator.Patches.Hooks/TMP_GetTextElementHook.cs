using GameTranslator.Patches.Utils;
using HarmonyLib;
using TMPro;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(TMP_Text), "GetTextElement")]
    internal class TMP_GetTextElementHook
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        public static void Postfix(uint unicode, ref TMP_TextElement __result)
        {
            if (__result == null)
                FontDynamicLoader.TryAddCharacterOnDemand(unicode);
        }
    }
}
