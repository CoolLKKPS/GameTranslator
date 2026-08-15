using GameTranslator.Patches.Utils;
using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameTranslator.Patches.Hooks.texture
{
    [HarmonyPatch(typeof(Renderer))]
    internal class Renderer_Materials_Hook
    {
        [HarmonyPrefix]
        [HarmonyPatch("materials", MethodType.Setter)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Materials(Renderer __instance, Material[] value)
        {
            if (!TextureEnhancement.CanProcess || TextureEnhancement.IsProcessing)
            {
                return;
            }
            TextureEnhancement.ProcessMaterials(value);
        }

        [HarmonyPrefix]
        [HarmonyPatch("sharedMaterials", MethodType.Setter)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SharedMaterials(Renderer __instance, Material[] value)
        {
            if (!TextureEnhancement.CanProcess || TextureEnhancement.IsProcessing)
            {
                return;
            }
            TextureEnhancement.ProcessMaterials(value);
        }
    }
}
