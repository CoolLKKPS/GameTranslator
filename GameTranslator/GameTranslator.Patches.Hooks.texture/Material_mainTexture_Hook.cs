using GameTranslator.Patches.Utils;
using System;
using System.Reflection;
using UnityEngine;
using XUnity.Common.Extensions;
using XUnity.Common.Harmony;
using XUnity.Common.MonoMod;

namespace GameTranslator.Patches.Hooks.texture
{
    internal static class Material_mainTexture_Hook
    {
        private static bool Prepare(object instance)
        {
            return true;
        }

        private static MethodBase TargetMethod(object instance)
        {
            PropertyInfo propertyInfo = AccessToolsShim.Property(typeof(Material), "mainTexture");
            return (propertyInfo != null) ? propertyInfo.GetSetMethod() : null;
        }

        public static void Prefix(Material __instance, ref Texture value)
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
            Material_mainTexture_Hook._original = detour.GenerateTrampolineEx<Action<Material, Texture>>();
        }

        private static void MM_Detour(Material __instance, ref Texture value)
        {
            Material_mainTexture_Hook.Prefix(__instance, ref value);
            Material_mainTexture_Hook._original(__instance, value);
        }

        private static Action<Material, Texture> _original;
    }
}
