using GameTranslator.Patches.Utils;
using System;
using System.Reflection;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Harmony;
using XUnity.Common.MonoMod;

namespace GameTranslator.Patches.Hooks.texture
{
    internal static class UISprite_material_Hook
    {
        private static bool Prepare(object instance)
        {
            return UnityTypes.UISprite != null;
        }

        private static MethodBase TargetMethod(object instance)
        {
            TypeContainer uisprite = UnityTypes.UISprite;
            PropertyInfo propertyInfo = AccessToolsShim.Property((uisprite != null) ? uisprite.ClrType : null, "material");
            return (propertyInfo != null) ? propertyInfo.GetSetMethod() : null;
        }

        public static void Postfix(object __instance)
        {
            Texture2D texture2D = null;
            TextureTranslate.Instance.Hook_ImageChangedOnComponent(__instance, ref texture2D, false, false);
        }

        private static void MM_Init(object detour)
        {
            UISprite_material_Hook._original = detour.GenerateTrampolineEx<Action<object, Material>>();
        }

        private static void MM_Detour(object __instance, Material value)
        {
            UISprite_material_Hook._original(__instance, value);
            UISprite_material_Hook.Postfix(__instance);
        }

        private static Action<object, Material> _original;
    }
}
