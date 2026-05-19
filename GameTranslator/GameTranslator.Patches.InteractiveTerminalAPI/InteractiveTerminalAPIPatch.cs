using HarmonyLib;
using System;
using System.Reflection;

namespace GameTranslator.Patches.InteractiveTerminalAPI
{
    internal class InteractiveTerminalAPIPatch
    {
        private static bool _isInteractiveTerminalAPIAvailable = false;
        private static Assembly _interactiveTerminalAPIAssembly = null;
        private static Harmony _harmony = null;

        public static void Initialize(Harmony harmony)
        {
            try
            {
                _harmony = harmony;
                _isInteractiveTerminalAPIAvailable = IsInteractiveTerminalAPIAvailable();

                if (_isInteractiveTerminalAPIAvailable)
                {
                    TranslatePlugin.logger.LogInfo("InteractiveTerminalAPI translation module initialized");
                    RegisterTranslationHandlers();
                }
                else
                {
                    TranslatePlugin.logger.LogInfo("InteractiveTerminalAPI not found, translation module disabled");
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError("Failed to initialize InteractiveTerminalAPI: " + ex.Message);
                _isInteractiveTerminalAPIAvailable = false;
            }
        }

        private static bool IsInteractiveTerminalAPIAvailable()
        {
            try
            {
                _interactiveTerminalAPIAssembly = Assembly.Load("InteractiveTerminalAPI");
                return _interactiveTerminalAPIAssembly != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void RegisterTranslationHandlers()
        {
            try
            {
                if (!_isInteractiveTerminalAPIAvailable || _interactiveTerminalAPIAssembly == null)
                    return;

                PatchGetTextMethods();
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError("Failed to register InteractiveTerminalAPI translation handlers: " + ex.Message);
            }
        }

        private static void PatchGetTextMethods()
        {
            string[] typeNames = new string[]
            {
                // "InteractiveTerminalAPI.UI.TextElement",
                // "InteractiveTerminalAPI.UI.Cursor.CursorElement",
                "InteractiveTerminalAPI.UI.Page.PageElement",
                "InteractiveTerminalAPI.UI.Screen.BoxedScreen"
            };
            foreach (string typeName in typeNames)
            {
                Type type = _interactiveTerminalAPIAssembly.GetType(typeName);
                if (type != null)
                {
                    MethodInfo getTextMethod = type.GetMethod("GetText", BindingFlags.Public | BindingFlags.Instance);
                    if (getTextMethod != null)
                    {
                        try
                        {
                            _harmony.Patch(getTextMethod, postfix: new HarmonyMethod(typeof(InteractiveTerminalAPIPatch).GetMethod("GetTextPostfix", BindingFlags.NonPublic | BindingFlags.Static)));
                            TranslatePlugin.logger.LogInfo($"Patched {type.Name}.GetText method");
                        }
                        catch (Exception ex)
                        {
                            TranslatePlugin.logger.LogWarning($"Failed to patch {type.Name}.GetText: {ex}");
                        }
                    }
                }
            }
        }

        private static void GetTextPostfix(ref string __result)
        {
            if (!string.IsNullOrEmpty(__result) && TranslatePlugin.shouldTranslateInteractiveTerminalAPI.Value)
            {
                __result = TranslateInteractiveText(__result);
            }
        }

        public static string TranslateInteractiveText(string text)
        {
            if (string.IsNullOrEmpty(text) || !TranslatePlugin.shouldTranslateInteractiveTerminalAPI.Value)
                return text;
            try
            {
                if (TranslatePlugin.showAvailableText.Value &&
                    GameTranslator.Patches.Utils.TextTranslate.ShouldOutputDebug($"InteractiveTerminalAPI:{text}"))
                {
                    TranslatePlugin.LogInfo($"[Debug] InteractiveTerminalAPI available text: '{text}'");
                }

                if (TranslateConfig.interactiveTerminalAPI != null && TranslateConfig.interactiveTerminalAPI.shouldTranslate)
                {
                    string translated = TranslateConfig.replaceByMap(text, TranslateConfig.interactiveTerminalAPI);
                    if (TranslatePlugin.showAvailableText.Value && TranslatePlugin.showOtherDebug.Value &&
                        GameTranslator.Patches.Utils.TextTranslate.ShouldOutputDebug($"InteractiveTerminalAPI_translated:{translated}"))
                    {
                        TranslatePlugin.LogInfo($"[Debug] InteractiveTerminalAPI translated: '{translated}'");
                    }
                    return translated;
                }
                return text;
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError("Error translating InteractiveTerminalAPI text: " + ex.Message);
                return text;
            }
        }
    }
}