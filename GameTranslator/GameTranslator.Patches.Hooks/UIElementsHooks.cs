using GameTranslator.Patches.Utils;
using HarmonyLib;
using UnityEngine.UIElements;
#if IL2CPP
using Il2CppInterop.Runtime.InteropTypes;
using XUnity.Common.Utilities;
#endif

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(TextElement))]
    internal class TextElement_text_Hook
    {
        [HarmonyPrefix]
        [HarmonyPatch("text", MethodType.Setter)]
#if MANAGED
        public static void Change(TextElement __instance, ref string value)
#else
        public static void Change(Il2CppObjectBase __instance, ref string value)
#endif
        {
#if IL2CPP
            var inst = Il2CppUtilities.CreateProxyComponentWithDerivedType(__instance.Pointer, typeof(TextElement));
            TextTranslate.Instance.OnTranslateIncomingText(inst, ref value);
#else
            TextTranslate.Instance.OnTranslateIncomingText(__instance, ref value);
#endif
        }
    }
}
