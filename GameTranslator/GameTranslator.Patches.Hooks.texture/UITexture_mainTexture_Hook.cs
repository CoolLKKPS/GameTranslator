using GameTranslator.Patches.Utils;
using System;
using System.Reflection;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Harmony;
using XUnity.Common.MonoMod;

namespace GameTranslator.Patches.Hooks.texture
{
    internal static class UITexture_mainTexture_Hook
    {
        private static bool Prepare(object instance)
        {
            return UnityTypes.UITexture != null;
        }

        private static MethodBase TargetMethod(object instance)
        {
            TypeContainer uitexture = UnityTypes.UITexture;
            PropertyInfo propertyInfo = AccessToolsShim.Property((uitexture != null) ? uitexture.ClrType : null, "mainTexture");
            return (propertyInfo != null) ? propertyInfo.GetSetMethod() : null;
        }

        public static void Postfix(object __instance)
        {
            Texture2D texture2D = null;
            TextureTranslate.Instance.Hook_ImageChangedOnComponent(__instance, ref texture2D, false, false);
        }

        private static void MM_Init(object detour)
        {
            UITexture_mainTexture_Hook._original = detour.GenerateTrampolineEx<Action<object, object>>();
        }

        private static void MM_Detour(object __instance, object value)
        {
            UITexture_mainTexture_Hook._original(__instance, value);
            UITexture_mainTexture_Hook.Postfix(__instance);
        }

        private static Action<object, object> _original;
    }
}
