#if CORECLR
using GameTranslator.Patches.Utils;
using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameTranslator.Patches.Hooks.texture
{
    [HarmonyPatch(typeof(AsyncOperation))]
    internal class AsyncInstantiate_Hook
    {
        [HarmonyPostfix]
        [HarmonyPatch("InvokeCompletionEvent")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void InvokeCompletionEvent(AsyncOperation __instance)
        {
            if (!TextureEnhancement.CanProcess || TextureEnhancement.IsProcessing)
            {
                return;
            }
            if (__instance is not AsyncInstantiateOperation operation)
            {
                return;
            }
            var results = operation.Result;
            if (results == null)
            {
                return;
            }
            for (var i = 0; i < results.Length; i++)
            {
                if (results[i] != null)
                {
                    TextureEnhancement.ProcessObject(results[i]);
                }
            }
        }
    }
}
#endif
