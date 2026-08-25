using GameTranslator.Patches.Utils;
using HarmonyLib;
using TMPro;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(TMP_FontAsset))]
    internal class TMP_FontAssetHook
    {
        [HarmonyPostfix]
        [HarmonyPatch("ReadFontAssetDefinition")]
        [HarmonyWrapSafe]
        public static void TMP_FontAsset_ReadFontAssetDefinition(TMP_FontAsset __instance)
        {
            if (TranslatePlugin.replaceUnsupportedCharacters.Value)
                FontSupportChecker.RegisterFont(__instance);
            if (TranslatePlugin.scaleFallbackEffects.Value)
                TMP_FallbackMaterialHook.RegisterFontMaterial(__instance);
        }
    }
}