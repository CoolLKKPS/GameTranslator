using GameTranslator.Patches.Utils;
using System;
using System.Reflection;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Extensions;
using XUnity.Common.Harmony;
using XUnity.Common.MonoMod;

namespace GameTranslator.Patches.Hooks.texture
{
    internal static class RawImage_texture_Hook
    {
        private static bool Prepare(object instance)
        {
            return UnityTypes.RawImage != null;
        }

        private static MethodBase TargetMethod(object instance)
        {
            TypeContainer rawImage = UnityTypes.RawImage;
            PropertyInfo propertyInfo = AccessToolsShim.Property((rawImage != null) ? rawImage.ClrType : null, "texture");
            return (propertyInfo != null) ? propertyInfo.GetSetMethod() : null;
        }

        public static void Prefix(Component __instance, ref Texture value)
        {
            Texture2D texture2D;
            bool flag = value.TryCastTo(out texture2D);
            if (flag)
            {
                TextureTranslate.Instance.Hook_ImageChangedOnComponent(__instance, ref texture2D, true, false);
                value = texture2D;
            }
        }

        private static void MM_Init(object detour)
        {
            RawImage_texture_Hook._original = detour.GenerateTrampolineEx<Action<Component, Texture>>();
        }

        private static void MM_Detour(Component __instance, Texture value)
        {
            RawImage_texture_Hook.Prefix(__instance, ref value);
            RawImage_texture_Hook._original(__instance, value);
        }

        private static Action<Component, Texture> _original;
    }
}
