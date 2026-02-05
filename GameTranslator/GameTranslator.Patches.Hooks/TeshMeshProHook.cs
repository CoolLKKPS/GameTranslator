using GameTranslator.Patches.Utils;
using HarmonyLib;
using TMPro;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(TextMeshPro))]
    internal class TeshMeshProHook
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnEnable")]
        public static void Change(ref TextMeshPro __instance)
        {
            TextTranslate.Instance.Hook_TextChanged(__instance);
        }
    }
}
