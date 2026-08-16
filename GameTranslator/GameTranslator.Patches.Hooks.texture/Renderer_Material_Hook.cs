using GameTranslator.Patches.Utils;
using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameTranslator.Patches.Hooks.texture
{
    [HarmonyPatch(typeof(Renderer))]
    internal class Renderer_Material_Hook
    {
        [HarmonyPrefix]
        [HarmonyPatch("material", MethodType.Setter)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Material(Renderer __instance, Material value)
        {
            Handle(__instance, value);
        }

        [HarmonyPrefix]
        [HarmonyPatch("sharedMaterial", MethodType.Setter)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SharedMaterial(Renderer __instance, Material value)
        {
            Handle(__instance, value);
        }

        private static void Handle(Renderer renderer, Material material)
        {
            if (!TextureEnhancement.CanProcess || TextureEnhancement.IsProcessing)
            {
                return;
            }
            if (renderer is null or not (MeshRenderer or SkinnedMeshRenderer))
            {
                return;
            }
            TextureEnhancement.ProcessMaterialTextures(material);
        }
    }
}
