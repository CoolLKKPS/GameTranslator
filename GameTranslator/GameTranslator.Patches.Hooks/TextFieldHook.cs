using HarmonyLib;
using UnityEngine.UIElements;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(TextField))]
    internal class TextFieldHook
    {
    }
}
