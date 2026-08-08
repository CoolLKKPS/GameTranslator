using GameTranslator.Patches.Utils;
using HarmonyLib;
using UnityEngine.UI;
#if IL2CPP
using Il2CppInterop.Runtime.InteropTypes;
using XUnity.Common.Utilities;
#endif

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(Text))]
    internal class TextHook
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnEnable")]
#if MANAGED
        public static void Change(Text __instance)
#else
        public static void Change(Il2CppObjectBase __instance)
#endif
        {
#if IL2CPP
            var inst = Il2CppUtilities.CreateProxyComponentWithDerivedType(__instance.Pointer, typeof(Text));
            TextTranslate.Instance.OnComponentTextChanged(inst);
#else
            TextTranslate.Instance.OnComponentTextChanged(__instance);
#endif
        }

        [HarmonyPrefix]
        [HarmonyPatch("text", MethodType.Setter)]
#if MANAGED
        public static void Change(Text __instance, ref string value)
#else
        public static void Change(Il2CppObjectBase __instance, ref string value)
#endif
        {
#if IL2CPP
            var inst = Il2CppUtilities.CreateProxyComponentWithDerivedType(__instance.Pointer, typeof(Text));
            TextTranslate.Instance.OnTranslateIncomingText(inst, ref value);
#else
            TextTranslate.Instance.OnTranslateIncomingText(__instance, ref value);
#endif
        }
    }
}
