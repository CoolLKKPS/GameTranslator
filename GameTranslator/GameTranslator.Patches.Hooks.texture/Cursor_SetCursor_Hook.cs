using GameTranslator.Patches.Utils;
using System;
using System.Reflection;
using UnityEngine;
using XUnity.Common.Harmony;
using XUnity.Common.MonoMod;

namespace GameTranslator.Patches.Hooks.texture
{
    internal static class Cursor_SetCursor_Hook
    {
        private static bool Prepare(object instance)
        {
            return true;
        }

        private static MethodBase TargetMethod(object instance)
        {
            return AccessToolsShim.Method(typeof(Cursor), "SetCursor", new Type[]
            {
                typeof(Texture2D),
                typeof(Vector2),
                typeof(CursorMode)
            });
        }

        public static void Prefix(ref Texture2D texture)
        {
            TextureTranslate.Instance.Hook_ImageChanged(ref texture, true);
        }

        private static void MM_Init(object detour)
        {
            Cursor_SetCursor_Hook._original = detour.GenerateTrampolineEx<Action<Texture2D, Vector2, CursorMode>>();
        }

        private static void MM_Detour(Texture2D texture, Vector2 arg2, CursorMode arg3)
        {
            Cursor_SetCursor_Hook.Prefix(ref texture);
            Cursor_SetCursor_Hook._original(texture, arg2, arg3);
        }

        private static Action<Texture2D, Vector2, CursorMode> _original;
    }
}
