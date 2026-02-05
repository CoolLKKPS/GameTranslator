using HarmonyLib;
using System;
using System.Reflection;

namespace GameTranslator.Patches.InteractiveTerminalAPI
{
    internal class InteractiveTerminalAPIPatch
    {
        private static bool _isInteractiveTerminalAPIAvailable = false;
        private static Assembly _interactiveTerminalAPIAssembly = null;
        private static readonly Harmony _harmony = new Harmony("GameTranslator.InteractiveTerminalAPI");
        private static readonly FieldInfo _terminalModifyingText = typeof(Terminal).GetField("modifyingText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        public static void Initialize()
        {
            try
            {
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

                PatchTerminalApplicationMethods();
                PatchTextElementMethods();
                PatchScreenMethods();
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError("Failed to register InteractiveTerminalAPI translation handlers: " + ex.Message);
            }
        }

        private static void PatchTextElementMethods()
        {
            Type textElementType = _interactiveTerminalAPIAssembly.GetType("InteractiveTerminalAPI.UI.TextElement");
            if (textElementType != null)
            {
                MethodInfo getTextMethod = textElementType.GetMethod("GetText", BindingFlags.Public | BindingFlags.Instance);
                if (getTextMethod != null)
                {
                    _harmony.Patch(getTextMethod, prefix: new HarmonyMethod(typeof(InteractiveTerminalAPIPatch).GetMethod("GetTextPrefix", BindingFlags.NonPublic | BindingFlags.Static)));
                    TranslatePlugin.logger.LogInfo("Patched TextElement.GetText method");
                }
            }
        }

        private static void PatchScreenMethods()
        {
            Type iScreenType = _interactiveTerminalAPIAssembly.GetType("InteractiveTerminalAPI.UI.Screen.IScreen");
            if (iScreenType != null)
            {
                Type[] types = _interactiveTerminalAPIAssembly.GetTypes();
                foreach (Type type in types)
                {
                    if (iScreenType.IsAssignableFrom(type) && !type.IsInterface)
                    {
                        MethodInfo getTextMethod = type.GetMethod("GetText", BindingFlags.Public | BindingFlags.Instance);
                        if (getTextMethod != null && getTextMethod.DeclaringType == type)
                        {
                            try
                            {
                                _harmony.Patch(getTextMethod, prefix: new HarmonyMethod(typeof(InteractiveTerminalAPIPatch).GetMethod("IScreenGetTextPrefix", BindingFlags.NonPublic | BindingFlags.Static)));
                                TranslatePlugin.logger.LogInfo($"Patched {type.Name}.GetText method");
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
        }

        private static void PatchTerminalApplicationMethods()
        {
            Type terminalApplicationType = _interactiveTerminalAPIAssembly.GetType("InteractiveTerminalAPI.UI.Application.TerminalApplication");
            if (terminalApplicationType != null)
            {
                MethodInfo updateTextMethod = terminalApplicationType.GetMethod("UpdateText", BindingFlags.Public | BindingFlags.Instance);
                if (updateTextMethod != null)
                {
                    _harmony.Patch(updateTextMethod, postfix: new HarmonyMethod(typeof(InteractiveTerminalAPIPatch).GetMethod("UpdateTextPostfix", BindingFlags.NonPublic | BindingFlags.Static)));
                    TranslatePlugin.logger.LogInfo("Patched TerminalApplication.UpdateText method");
                }
            }
        }

        private static void GetTextPrefix(ref string __result)
        {
            if (!string.IsNullOrEmpty(__result) && TranslatePlugin.shouldTranslateInteractiveTerminalAPI.Value)
            {
                __result = TranslateInteractiveText(__result);
            }
        }

        private static void IScreenGetTextPrefix(ref string __result)
        {
            if (!string.IsNullOrEmpty(__result) && TranslatePlugin.shouldTranslateInteractiveTerminalAPI.Value)
            {
                __result = TranslateInteractiveText(__result);
            }
        }

        private static void UpdateTextPostfix(object __instance)
        {
            try
            {
                if (__instance == null || !TranslatePlugin.shouldTranslateInteractiveTerminalAPI.Value)
                    return;

                Type instanceType = __instance.GetType();
                FieldInfo terminalField = instanceType.GetField("terminal", BindingFlags.NonPublic | BindingFlags.Instance);
                if (terminalField != null && _terminalModifyingText != null)
                {
                    object terminal = terminalField.GetValue(__instance);
                    if (terminal is Terminal term && term.screenText != null)
                    {
                        bool isModifyingText = (bool)_terminalModifyingText.GetValue(term);
                        if (!isModifyingText)
                        {
                            string currentText = term.screenText.text;
                            if (!string.IsNullOrEmpty(currentText))
                            {
                                string translatedText = TranslateInteractiveText(currentText);
                                if (translatedText != currentText)
                                {
                                    _terminalModifyingText.SetValue(term, true);
                                    term.screenText.text = translatedText;
                                    term.currentText = translatedText;
                                    term.screenText.interactable = false;
                                    term.screenText.DeactivateInputField();
                                    _terminalModifyingText.SetValue(term, false);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError("Error in UpdateTextPostfix: " + ex.Message);
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
                    return TranslateConfig.replaceByMap(text, TranslateConfig.interactiveTerminalAPI);
                }
                return text;
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError("Error translating InteractiveTerminalAPI text: " + ex.Message);
                return text;
            }
        }

        public static string TranslateText(string text)
        {
            return TranslateInteractiveText(text);
        }
    }
}