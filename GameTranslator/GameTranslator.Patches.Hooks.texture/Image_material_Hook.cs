using GameTranslator.Patches.Utils;
using System;
using System.Reflection;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Harmony;
using XUnity.Common.MonoMod;

namespace GameTranslator.Patches.Hooks.texture
{
    internal static class Image_material_Hook
    {
        private static bool Prepare(object instance)
        {
            return UnityTypes.Image != null;
        }

        private static MethodBase TargetMethod(object instance)
        {
            TypeContainer image = UnityTypes.Image;
            PropertyInfo propertyInfo = AccessToolsShim.Property((image != null) ? image.ClrType : null, "material");
            return (propertyInfo != null) ? propertyInfo.GetSetMethod() : null;
        }

        public static void Postfix(Component __instance)
        {
            Texture2D texture2D = null;
            TextureTranslate.Instance.Hook_ImageChangedOnComponent(__instance, ref texture2D, false, false);
        }

        private static void MM_Init(object detour)
        {
            Image_material_Hook._original = detour.GenerateTrampolineEx<Action<Component, Material>>();
        }

        private static void MM_Detour(Component __instance, Material value)
        {
            Image_material_Hook._original(__instance, value);
            Image_material_Hook.Postfix(__instance);
        }

        private static Action<Component, Material> _original;
    }
}
