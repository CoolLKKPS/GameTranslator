using GameTranslator.Patches.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;

namespace GameTranslator.Patches.Hooks
{
    public static class TextMeshProHooks
    {
        public static readonly Type[] All = new[] {
            typeof(TeshMeshProUGUIHook),
            typeof(TextMeshProHook),
            typeof(TMP_Text_text_Hook),
            typeof(TMP_Text_SetText_Hook1),
            typeof(TMP_Text_SetText_Hook2),
            typeof(TMP_Text_SetText_Hook3),
            typeof(TMP_Text_SetCharArray_Hook1),
            typeof(TMP_Text_SetCharArray_Hook2),
            typeof(TMP_Text_SetCharArray_Hook3)
        };
    }

    [HarmonyPatch(typeof(TextMeshProUGUI))]
    internal class TeshMeshProUGUIHook
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnEnable")]
        public static void OnEnablePostfix(TextMeshProUGUI __instance)
        {
            try
            {
                TextTranslate.Instance.Hook_TextChanged(__instance);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError($"Error in TeshMeshProUGUIHook.OnEnable: {ex.Message}");
            }
        }

        public static bool ShouldTranslate(string text)
        {
            return text.Length >= TranslateConfig.text.shouldTranslateMinLength - 4 && text.Any(new Func<char, bool>(char.IsLetter));
        }

        public static Dictionary<string, string> keys = new Dictionary<string, string>();
    }

    [HarmonyPatch(typeof(TextMeshPro))]
    internal class TextMeshProHook
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnEnable")]
        public static void OnEnablePostfix(TextMeshPro __instance)
        {
            try
            {
                TextTranslate.Instance.Hook_TextChanged(__instance);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError($"Error in TextMeshProHook.OnEnable: {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class TMP_Text_text_Hook
    {
        static bool Prepare()
        {
            return typeof(TMP_Text) != null;
        }

        static MethodBase TargetMethod()
        {
            var property = typeof(TMP_Text).GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            return property?.GetSetMethod();
        }

        [HarmonyPostfix]
        static void Postfix(TMP_Text __instance)
        {
            try
            {
                TextTranslate.Instance.Hook_TextChanged(__instance);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError($"Error in TMP_Text_text_Hook: {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class TMP_Text_SetText_Hook1
    {
        static bool Prepare()
        {
            return typeof(TMP_Text) != null;
        }

        static MethodBase TargetMethod()
        {
            return typeof(TMP_Text).GetMethod("SetText", new[] { typeof(string) });
        }

        [HarmonyPostfix]
        static void Postfix(TMP_Text __instance)
        {
            try
            {
                TextTranslate.Instance.Hook_TextChanged(__instance);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError($"Error in TMP_Text_SetText_Hook1: {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class TMP_Text_SetText_Hook2
    {
        static bool Prepare()
        {
            return typeof(TMP_Text) != null;
        }

        static MethodBase TargetMethod()
        {
            return typeof(TMP_Text).GetMethod("SetText", new[] { typeof(string), typeof(bool) });
        }

        [HarmonyPostfix]
        static void Postfix(TMP_Text __instance)
        {
            try
            {
                TextTranslate.Instance.Hook_TextChanged(__instance);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError($"Error in TMP_Text_SetText_Hook2: {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class TMP_Text_SetText_Hook3
    {
        static bool Prepare()
        {
            return typeof(TMP_Text) != null;
        }

        static MethodBase TargetMethod()
        {
            return typeof(TMP_Text).GetMethod("SetText", new[] { typeof(string), typeof(float), typeof(float), typeof(float) });
        }

        [HarmonyPostfix]
        static void Postfix(TMP_Text __instance)
        {
            try
            {
                TextTranslate.Instance.Hook_TextChanged(__instance);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError($"Error in TMP_Text_SetText_Hook3: {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class TMP_Text_SetCharArray_Hook1
    {
        static bool Prepare()
        {
            return typeof(TMP_Text) != null;
        }

        static MethodBase TargetMethod()
        {
            return typeof(TMP_Text).GetMethod("SetCharArray", new[] { typeof(char[]) });
        }

        [HarmonyPostfix]
        static void Postfix(TMP_Text __instance)
        {
            try
            {
                TextTranslate.Instance.Hook_TextChanged(__instance);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError($"Error in TMP_Text_SetCharArray_Hook1: {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class TMP_Text_SetCharArray_Hook2
    {
        static bool Prepare()
        {
            return typeof(TMP_Text) != null;
        }

        static MethodBase TargetMethod()
        {
            return typeof(TMP_Text).GetMethod("SetCharArray", new[] { typeof(char[]), typeof(int), typeof(int) });
        }

        [HarmonyPostfix]
        static void Postfix(TMP_Text __instance)
        {
            try
            {
                TextTranslate.Instance.Hook_TextChanged(__instance);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError($"Error in TMP_Text_SetCharArray_Hook2: {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class TMP_Text_SetCharArray_Hook3
    {
        static bool Prepare()
        {
            return typeof(TMP_Text) != null;
        }

        static MethodBase TargetMethod()
        {
            return typeof(TMP_Text).GetMethod("SetCharArray", new[] { typeof(int[]), typeof(int), typeof(int) });
        }

        [HarmonyPostfix]
        static void Postfix(TMP_Text __instance)
        {
            try
            {
                TextTranslate.Instance.Hook_TextChanged(__instance);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError($"Error in TMP_Text_SetCharArray_Hook3: {ex.Message}");
            }
        }
    }
}
