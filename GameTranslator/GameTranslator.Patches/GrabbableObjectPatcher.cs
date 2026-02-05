using BepInEx;
using HarmonyLib;
using System.Collections.Generic;

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
                if (!GrabbableObjectPatcher.translatedItems.Contains(__instance.itemProperties.itemName))
                {
                    GrabbableObjectPatcher.originToTranslated[__instance.itemProperties.itemName] = TranslateConfig.replaceByMap(__instance.itemProperties.itemName, TranslateConfig.items);
                    GrabbableObjectPatcher.translatedItems.Add(GrabbableObjectPatcher.originToTranslated[__instance.itemProperties.itemName]);
                    __instance.itemProperties.itemName = GrabbableObjectPatcher.originToTranslated[__instance.itemProperties.itemName];
                }
                if (GrabbableObjectPatcher.originToTranslated.ContainsKey(__instance.itemProperties.itemName))
                {
                    __instance.itemProperties.itemName = GrabbableObjectPatcher.originToTranslated[__instance.itemProperties.itemName];
                }
                ScanNodeProperties componentInChildren = __instance.GetComponentInChildren<ScanNodeProperties>();
                if (componentInChildren != null && componentInChildren.headerText != null)
                {
                    if (!GrabbableObjectPatcher.translatedItems.Contains(componentInChildren.headerText))
                    {
                        GrabbableObjectPatcher.originToTranslated[componentInChildren.headerText] = TranslateConfig.replaceByMap(componentInChildren.headerText, TranslateConfig.items);
                        GrabbableObjectPatcher.translatedItems.Add(GrabbableObjectPatcher.originToTranslated[componentInChildren.headerText]);
                        componentInChildren.headerText = GrabbableObjectPatcher.originToTranslated[componentInChildren.headerText];
                    }
                    if (GrabbableObjectPatcher.originToTranslated.ContainsKey(componentInChildren.headerText))
                    {
                        componentInChildren.headerText = GrabbableObjectPatcher.originToTranslated[componentInChildren.headerText];
                    }
                }
            }
        }

        public static Dictionary<string, string> originToTranslated = new Dictionary<string, string>();

        public static HashSet<string> translatedItems = new HashSet<string>();
    }
}
