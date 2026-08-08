using GameTranslator.Patches.Utils;
using HarmonyLib;
using TMPro;
#if IL2CPP
using Il2CppInterop.Runtime.InteropTypes;
using XUnity.Common.Utilities;
#endif

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(TextMeshPro))]
    internal class TeshMeshProHook
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnEnable")]
#if MANAGED
        public static void Change(TextMeshPro __instance)
#else
        public static void Change(Il2CppObjectBase __instance)
#endif
        {
#if IL2CPP
            var inst = Il2CppUtilities.CreateProxyComponentWithDerivedType(__instance.Pointer, typeof(TextMeshPro));
            TextTranslate.Instance.OnComponentTextChanged(inst);
#else
            TextTranslate.Instance.OnComponentTextChanged(__instance);
#endif
        }
    }
}
