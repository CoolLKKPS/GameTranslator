using GameTranslator.Patches.Utils;
using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using XUnity.Common.Logging;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(Texture2D))]
    internal class Texture2DHook
    {
        [HarmonyPostfix]
        [HarmonyPatch("LoadRawTextureData", new Type[] { typeof(byte[]) })]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void LoadRawTextureData(Texture2D __instance, byte[] __0)
        {
            try
            {
                if (TextureTranslate.ImageHooksEnabled && TranslatePlugin.changeTexture.Value && __instance != null)
                {
                    var format = (int)__instance.format;
                    if (format != 1 && format != 9 && format != 63)
                    {
                        TextureTranslate.Instance.Hook_ImageChanged(ref __instance, false);
                    }
                }
            }
            catch (Exception ex)
            {
                XuaLogger.AutoTranslator.Error(ex, "An error occurred in Texture2D.LoadRawTextureData hook.");
            }
        }
    }
}
