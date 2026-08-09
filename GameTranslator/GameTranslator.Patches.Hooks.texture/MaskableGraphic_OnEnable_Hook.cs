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
    internal static class MaskableGraphic_OnEnable_Hook
    {
        private static bool Prepare(object instance)
        {
            return UnityTypes.MaskableGraphic != null;
        }

        private static MethodBase TargetMethod(object instance)
        {
            TypeContainer maskableGraphic = UnityTypes.MaskableGraphic;
            return AccessToolsShim.Method((maskableGraphic != null) ? maskableGraphic.ClrType : null, "OnEnable", Array.Empty<Type>());
        }

        public static void Postfix(Component __instance)
        {
            Type unityType = __instance.GetUnityType();
            bool flag = (UnityTypes.Image != null && UnityTypes.Image.IsAssignableFrom(unityType)) || (UnityTypes.RawImage != null && UnityTypes.RawImage.IsAssignableFrom(unityType));
            if (flag)
            {
                Texture2D texture2D = null;
                TextureTranslate.Instance.Hook_ImageChangedOnComponent(__instance, ref texture2D, false, true);
            }
        }

        private static void MM_Init(object detour)
        {
            MaskableGraphic_OnEnable_Hook._original = detour.GenerateTrampolineEx<Action<Component>>();
        }

        private static void MM_Detour(Component __instance)
        {
            MaskableGraphic_OnEnable_Hook._original(__instance);
            MaskableGraphic_OnEnable_Hook.Postfix(__instance);
        }

        private static Action<Component> _original;
    }
}
