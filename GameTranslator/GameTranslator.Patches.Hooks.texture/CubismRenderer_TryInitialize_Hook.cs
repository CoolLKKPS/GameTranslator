using GameTranslator.Patches.Utils;
using System;
using System.Reflection;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Harmony;
using XUnity.Common.MonoMod;

namespace GameTranslator.Patches.Hooks.texture
{
    internal static class CubismRenderer_TryInitialize_Hook
    {
        private static bool Prepare(object instance)
        {
            return UnityTypes.CubismRenderer != null;
        }

        private static MethodBase TargetMethod(object instance)
        {
            return AccessToolsShim.Method(UnityTypes.CubismRenderer.ClrType, "TryInitialize", Array.Empty<Type>());
        }

        public static void Prefix(Component __instance)
        {
            Texture2D texture2D = null;
            TextureTranslate.Instance.Hook_ImageChangedOnComponent(__instance, ref texture2D, true, true);
        }

        private static void MM_Init(object detour)
        {
            CubismRenderer_TryInitialize_Hook._original = detour.GenerateTrampolineEx<Action<Component>>();
        }

        private static void MM_Detour(Component __instance)
        {
            CubismRenderer_TryInitialize_Hook.Prefix(__instance);
            CubismRenderer_TryInitialize_Hook._original(__instance);
        }

        private static Action<Component> _original;
    }
}
