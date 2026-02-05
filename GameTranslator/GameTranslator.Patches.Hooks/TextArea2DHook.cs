using HarmonyLib;
using UnityEngine;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(Texture2D))]
    internal class TextArea2DHook
    {
    }
}
