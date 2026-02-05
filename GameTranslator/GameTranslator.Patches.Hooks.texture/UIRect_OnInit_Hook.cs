using GameTranslator.Patches.Utils;
using System;
using System.Reflection;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Harmony;
using XUnity.Common.MonoMod;

namespace GameTranslator.Patches.Hooks.texture
{
    internal static class UIRect_OnInit_Hook
    {
        private static bool Prepare(object instance)
        {
            return UnityTypes.UIRect != null;
        }

        private static MethodBase TargetMethod(object instance)
        {
            TypeContainer uirect = UnityTypes.UIRect;
            return AccessToolsShim.Method((uirect != null) ? uirect.ClrType : null, "OnInit", Array.Empty<Type>());
        }

        public static void Postfix(object __instance)
        {
            Texture2D texture2D = null;
            TextureTranslate.Instance.Hook_ImageChangedOnComponent(__instance, ref texture2D, false, true);
        }

        private static void MM_Init(object detour)
        {
            UIRect_OnInit_Hook._original = detour.GenerateTrampolineEx<Action<object>>();
        }

        private static void MM_Detour(object __instance)
        {
            UIRect_OnInit_Hook._original(__instance);
            UIRect_OnInit_Hook.Postfix(__instance);
        }

        private static Action<object> _original;
    }
}
