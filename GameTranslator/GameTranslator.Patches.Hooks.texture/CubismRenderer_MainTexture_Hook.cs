using GameTranslator.Patches.Utils;
using System;
using System.Reflection;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Harmony;
using XUnity.Common.MonoMod;

namespace GameTranslator.Patches.Hooks.texture
{
    internal static class CubismRenderer_MainTexture_Hook
    {
        private static bool Prepare(object instance)
        {
            return UnityTypes.CubismRenderer != null;
        }

        private static MethodBase TargetMethod(object instance)
        {
            PropertyInfo propertyInfo = AccessToolsShim.Property(UnityTypes.CubismRenderer.ClrType, "MainTexture");
            return (propertyInfo != null) ? propertyInfo.GetSetMethod() : null;
        }

        public static void Prefix(Component __instance, ref Texture2D value)
        {
            TextureTranslate.Instance.Hook_ImageChangedOnComponent(__instance, ref value, true, false);
        }

        private static void MM_Init(object detour)
        {
            CubismRenderer_MainTexture_Hook._original = detour.GenerateTrampolineEx<Action<Component, Texture2D>>();
        }

        private static void MM_Detour(Component __instance, ref Texture2D value)
        {
            CubismRenderer_MainTexture_Hook.Prefix(__instance, ref value);
            CubismRenderer_MainTexture_Hook._original(__instance, value);
        }

        private static Action<Component, Texture2D> _original;
    }
}
