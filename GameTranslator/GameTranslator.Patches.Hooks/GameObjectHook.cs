using GameTranslator.Patches.Translatons;
using GameTranslator.Patches.Utils;
using HarmonyLib;
using System;
using UnityEngine;
using XUnity.Common.Constants;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(GameObject))]
    internal class GameObjectHook
    {
        [HarmonyPostfix]
        [HarmonyPatch("active", MethodType.Setter)]
        public static void Active(ref GameObject __instance, bool value)
        {
            if (value)
            {
                foreach (Component component in __instance.GetComponentsInChildren(UnityTypes.TextMesh.UnityType))
                {
                    if (component.IsComponentActive())
                    {
                        TextTranslate.Instance.Hook_TextChanged(component);
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("SetActive", new Type[] { typeof(bool) })]
        public static void SetActive(ref GameObject __instance, bool value)
        {
            if (value)
            {
                foreach (Component component in __instance.GetComponentsInChildren(UnityTypes.TextMesh.UnityType))
                {
                    if (component.IsComponentActive())
                    {
                        TextTranslate.Instance.Hook_TextChanged(component);
                    }
                }
            }
        }
    }
}
