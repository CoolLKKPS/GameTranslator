using GameTranslator.Patches.Utils;
using HarmonyLib;
using UnityEngine;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(TextMesh))]
    internal class TextMeshHook
    {
        [HarmonyPrefix]
        [HarmonyPatch("text", MethodType.Setter)]
        public static void Change(ref TextMesh __instance, ref string value)
        {
            TextTranslate.Instance.Hook_TextChanged(__instance, ref value);
        }
    }
}
