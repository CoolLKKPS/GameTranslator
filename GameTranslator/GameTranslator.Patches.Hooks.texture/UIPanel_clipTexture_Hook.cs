using GameTranslator.Patches.Utils;
using System;
using System.Reflection;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Harmony;
using XUnity.Common.MonoMod;

namespace GameTranslator.Patches.Hooks.texture
{
    internal static class UIPanel_clipTexture_Hook
    {
        private static bool Prepare(object instance)
        {
            return UnityTypes.UIPanel != null;
        }

        private static MethodBase TargetMethod(object instance)
        {
            PropertyInfo propertyInfo = AccessToolsShim.Property(UnityTypes.UIPanel.ClrType, "clipTexture");
            return (propertyInfo != null) ? propertyInfo.GetSetMethod() : null;
        }

        public static void Postfix(object __instance)
        {
            Texture2D texture2D = null;
            TextureTranslate.Instance.Hook_ImageChangedOnComponent(__instance, ref texture2D, false, false);
        }

        private static void MM_Init(object detour)
        {
            UIPanel_clipTexture_Hook._original = detour.GenerateTrampolineEx<Action<object, object>>();
        }

        private static void MM_Detour(object __instance, object value)
        {
            UIPanel_clipTexture_Hook._original(__instance, value);
            UIPanel_clipTexture_Hook.Postfix(__instance);
        }

        private static Action<object, object> _original;
    }
}
