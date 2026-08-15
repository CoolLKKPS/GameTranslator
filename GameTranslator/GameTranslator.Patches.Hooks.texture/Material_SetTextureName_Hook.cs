using GameTranslator.Patches.Utils;
using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using XUnity.Common.Extensions;

namespace GameTranslator.Patches.Hooks.texture
{
    [HarmonyPatch(typeof(Material))]
    internal class Material_SetTextureName_Hook
    {
        [HarmonyPrefix]
        [HarmonyPatch("SetTexture", new Type[] { typeof(string), typeof(Texture) })]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SetTextureByName(Material __instance, string name, ref Texture value)
        {
            if (!TextureEnhancement.CanProcess || TextureEnhancement.IsProcessing)
            {
                return;
            }
            if (value.TryCastTo(out Texture2D texture2D))
            {
                TextureTranslate.Instance.Hook_ImageChangedOnComponent(__instance, ref texture2D, true, false);
                value = texture2D;
            }
        }
    }
}
