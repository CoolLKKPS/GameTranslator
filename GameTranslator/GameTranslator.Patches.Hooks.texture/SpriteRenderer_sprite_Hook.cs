using GameTranslator.Patches.Utils;
using System;
using System.Reflection;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Harmony;
using XUnity.Common.MonoMod;

namespace GameTranslator.Patches.Hooks.texture
{
    internal static class SpriteRenderer_sprite_Hook
    {
        private static bool Prepare(object instance)
        {
            return UnityTypes.SpriteRenderer != null;
        }

        private static MethodBase TargetMethod(object instance)
        {
            TypeContainer spriteRenderer = UnityTypes.SpriteRenderer;
            PropertyInfo propertyInfo = AccessToolsShim.Property((spriteRenderer != null) ? spriteRenderer.ClrType : null, "sprite");
            return (propertyInfo != null) ? propertyInfo.GetSetMethod() : null;
        }

        public static void Prefix(SpriteRenderer __instance, ref Sprite value)
        {
            bool imageHooksEnabled = TextureTranslate.ImageHooksEnabled;
            TextureTranslate.ImageHooksEnabled = false;
            Texture2D texture;
            try
            {
                texture = value.texture;
            }
            finally
            {
                TextureTranslate.ImageHooksEnabled = imageHooksEnabled;
            }
            TextureTranslate.Instance.Hook_ImageChangedOnComponent(__instance, ref value, ref texture, true, false);
        }

        private static void MM_Init(object detour)
        {
            SpriteRenderer_sprite_Hook._original = detour.GenerateTrampolineEx<Action<SpriteRenderer, Sprite>>();
        }

        private static void MM_Detour(SpriteRenderer __instance, Sprite sprite)
        {
            SpriteRenderer_sprite_Hook.Prefix(__instance, ref sprite);
            SpriteRenderer_sprite_Hook._original(__instance, sprite);
        }

        private static Action<SpriteRenderer, Sprite> _original;
    }
}
