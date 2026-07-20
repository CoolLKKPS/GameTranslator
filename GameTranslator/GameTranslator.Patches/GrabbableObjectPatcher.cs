using BepInEx;
using HarmonyLib;
using System.Collections.Concurrent;

namespace GameTranslator.Patches
{
    [HarmonyPatch(typeof(GrabbableObject))]
    internal class GrabbableObjectPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void start(ref GrabbableObject __instance)
        {
            if (__instance.itemProperties != null && TranslatePlugin.shouldTranslateItems.Value && !__instance.itemProperties.itemName.IsNullOrWhiteSpace())
            {
                string itemName = __instance.itemProperties.itemName;
                if (!GrabbableObjectPatcher.originToTranslated.TryGetValue(itemName, out string translated))
                {
                    translated = TranslateConfig.replaceByMap(itemName, TranslateConfig.items);
                    GrabbableObjectPatcher.originToTranslated[itemName] = translated;
                }
                __instance.itemProperties.itemName = translated;
                ScanNodeProperties componentInChildren = __instance.GetComponentInChildren<ScanNodeProperties>();
                if (componentInChildren != null && componentInChildren.headerText != null)
                {
                    string headerText = componentInChildren.headerText;
                    if (!GrabbableObjectPatcher.originToTranslated.TryGetValue(headerText, out string translatedHeader))
                    {
                        translatedHeader = TranslateConfig.replaceByMap(headerText, TranslateConfig.items);
                        GrabbableObjectPatcher.originToTranslated[headerText] = translatedHeader;
                    }
                    componentInChildren.headerText = translatedHeader;
                }
            }
        }

        private static ConcurrentDictionary<string, string> originToTranslated = new ConcurrentDictionary<string, string>();

        public static void ClearCache()
        {
            originToTranslated.Clear();
        }
    }
}
