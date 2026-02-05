using GameTranslator.Patches.Utils;
using HarmonyLib;
using TMPro;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(TMP_FontAsset))]
    internal class TMP_FontAssetHook
    {
        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        [HarmonyWrapSafe]
        public static void TMP_FontAsset_Awake(TMP_FontAsset __instance)
        {
            FontSupportChecker.RegisterFont(__instance);
        }
    }
}