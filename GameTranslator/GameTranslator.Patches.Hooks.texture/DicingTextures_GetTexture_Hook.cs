using GameTranslator.Patches.Utils;
using System;
using System.Reflection;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Harmony;
using XUnity.Common.MonoMod;

namespace GameTranslator.Patches.Hooks.texture
{
    internal static class DicingTextures_GetTexture_Hook
    {
        private static bool Prepare(object instance)
        {
            return UnityTypes.DicingTextures != null;
        }

        private static MethodBase TargetMethod(object instance)
        {
            TypeContainer dicingTextures = UnityTypes.DicingTextures;
            return AccessToolsShim.Method((dicingTextures != null) ? dicingTextures.ClrType : null, "GetTexture", new Type[] { typeof(string) });
        }

        public static void Postfix(object __instance, ref Texture2D __result)
        {
            TextureTranslate.Instance.Hook_ImageChanged(ref __result, false);
        }

        private static void MM_Init(object detour)
        {
            DicingTextures_GetTexture_Hook._original = detour.GenerateTrampolineEx<Func<object, string, Texture2D>>();
        }

        private static Texture2D MM_Detour(object __instance, string arg1)
        {
            Texture2D texture2D = DicingTextures_GetTexture_Hook._original(__instance, arg1);
            DicingTextures_GetTexture_Hook.Postfix(__instance, ref texture2D);
            return texture2D;
        }

        private static Func<object, string, Texture2D> _original;
    }
}
