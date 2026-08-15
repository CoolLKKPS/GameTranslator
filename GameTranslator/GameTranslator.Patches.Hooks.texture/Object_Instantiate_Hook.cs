using GameTranslator.Patches.Utils;
using HarmonyLib;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameTranslator.Patches.Hooks.texture
{
    [HarmonyPatch(typeof(Object))]
    internal class Object_Instantiate_Hook
    {
        private static readonly Dictionary<Object, bool> OriginalHasRenderers = [];

        private static void Handle(Object data, Object __result)
        {
            if (!TextureEnhancement.CanProcess || TextureEnhancement.IsProcessing || __result == null)
            {
                return;
            }
            if (data != null && !HasRenderers(data))
            {
                return;
            }
            TextureEnhancement.ProcessObject(__result);
        }

        private static bool HasRenderers(Object data)
        {
            if (!OriginalHasRenderers.TryGetValue(data, out var hasRenderers))
            {
                hasRenderers = TextureEnhancement.ContainsRenderer(data);
                OriginalHasRenderers[data] = hasRenderers;
            }
            return hasRenderers;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Internal_CloneSingle")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CloneSingle(Object data, Object __result)
        {
            Handle(data, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Internal_CloneSingleWithScene")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CloneSingleWithScene(Object data, Object __result)
        {
            Handle(data, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Internal_CloneSingleWithParams")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CloneSingleWithParams(Object data, Object __result)
        {
            Handle(data, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Internal_CloneSingleWithParent")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CloneSingleWithParent(Object data, Object __result)
        {
            Handle(data, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Internal_InstantiateSingle")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void InstantiateSingle(Object data, Object __result)
        {
            Handle(data, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Internal_InstantiateSingleWithParent")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void InstantiateSingleWithParent(Object data, Object __result)
        {
            Handle(data, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Internal_InstantiateSingleWithParams")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void InstantiateSingleWithParams(Object data, Object __result)
        {
            Handle(data, __result);
        }
    }
}
