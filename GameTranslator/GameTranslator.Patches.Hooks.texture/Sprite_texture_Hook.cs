using GameTranslator.Patches.Utils;
using System;
using System.Reflection;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Harmony;
using XUnity.Common.MonoMod;

namespace GameTranslator.Patches.Hooks.texture
{
    internal static class Sprite_texture_Hook
    {
        private static bool Prepare(object instance)
        {
            return UnityTypes.Sprite != null;
        }

        private static MethodBase TargetMethod(object instance)
        {
            TypeContainer sprite = UnityTypes.Sprite;
            PropertyInfo propertyInfo = AccessToolsShim.Property((sprite != null) ? sprite.ClrType : null, "texture");
            return (propertyInfo != null) ? propertyInfo.GetGetMethod() : null;
        }

        private static void Postfix(ref Texture2D __result)
        {
            TextureTranslate.Instance.Hook_ImageChanged(ref __result, true);
        }

        private static void MM_Init(object detour)
        {
            Sprite_texture_Hook._original = detour.GenerateTrampolineEx<Func<Sprite, Texture2D>>();
        }

        private static Texture2D MM_Detour(Sprite __instance)
        {
            Texture2D texture2D = Sprite_texture_Hook._original(__instance);
            Sprite_texture_Hook.Postfix(ref texture2D);
            return texture2D;
        }

        private static Func<Sprite, Texture2D> _original;
    }
}
