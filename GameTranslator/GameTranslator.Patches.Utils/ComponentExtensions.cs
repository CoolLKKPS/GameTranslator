using GameTranslator.Patches.Translatons;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameTranslator.Patches.Utils
{
    internal static class ComponentExtensions
    {
        private static readonly string XuaIgnore = "XUAIGNORE";
        private static readonly string XuaIgnoreTree = "XUAIGNORETREE";
        private static bool _guiContentCheckFailed = false;

        public static bool SupportsStabilization(this object ui)
        {
            if (ui == null) return false;
            return _guiContentCheckFailed || !IsGUIContentSafe(ui);
        }

        public static bool IsSpammingComponent(this object ui)
        {
            return ui == null
                || (!_guiContentCheckFailed && IsGUIContentSafe(ui));
        }

        public static bool ShouldIgnoreTextComponent(this object ui)
        {
            if (ui is Component component && component != null)
            {
                var tr = component.transform;

                if (tr.name.Contains(XuaIgnore))
                {
                    return true;
                }

                while (tr.parent != null)
                {
                    tr = tr.parent;

                    if (tr.name.Contains(XuaIgnoreTree))
                    {
                        return true;
                    }
                }

                Component inputField = null;

                if (GetTypeByName("UnityEngine.UI.InputField") != null)
                {
                    inputField = component.gameObject.GetFirstComponentInSelfOrAncestor(GetTypeByName("UnityEngine.UI.InputField"));
                    if (inputField != null)
                    {
                        var placeholderProperty = inputField.GetType().GetProperty("placeholder");
                        if (placeholderProperty != null)
                        {
                            var placeholder = (Component)placeholderProperty.GetValue(inputField);
                            return !ReferenceEquals(placeholder, component);
                        }
                    }
                }

                if (GetTypeByName("TMPro.TMP_InputField") != null)
                {
                    inputField = component.gameObject.GetFirstComponentInSelfOrAncestor(GetTypeByName("TMPro.TMP_InputField"));
                    if (inputField != null)
                    {
                        var placeholderProperty = inputField.GetType().GetProperty("placeholder");
                        if (placeholderProperty != null)
                        {
                            var placeholder = (Component)placeholderProperty.GetValue(inputField);
                            return !ReferenceEquals(placeholder, component);
                        }
                    }
                }
                inputField = component.gameObject.GetFirstComponentInSelfOrAncestor(GetTypeByName("UIInput"));
                return inputField != null;
            }
            return false;
        }

        public static string GetPath(this object ui)
        {
            if (ui is Component component && component != null)
            {
                var path = new StringBuilder();
                var current = component.transform;
                var segments = new List<string>();

                while (current != null)
                {
                    segments.Add(current.name);
                    current = current.parent;
                }

                segments.Reverse();
                foreach (var segment in segments)
                {
                    path.Append("/").Append(segment);
                }

                return path.ToString();
            }

            return "Unknown";
        }

        private static bool IsGUIContentSafe(object ui)
        {
            try
            {
                return IsGUIContentUnsafe(ui);
            }
            catch
            {
                _guiContentCheckFailed = true;
            }
            return false;
        }

        private static bool IsGUIContentUnsafe(object ui) => ui is GUIContent;

        private static bool SetTextOnGUIContentSafe(object ui, string text)
        {
            try
            {
                return SetTextOnGUIContentUnsafe(ui, text);
            }
            catch
            {
                _guiContentCheckFailed = true;
            }
            return false;
        }

        private static bool SetTextOnGUIContentUnsafe(object ui, string text)
        {
            if (ui is GUIContent gui)
            {
                gui.text = text;
                return true;
            }
            return false;
        }

        private static bool TryGetTextFromGUIContentSafe(object ui, out string text)
        {
            try
            {
                return TryGetTextFromGUIContentUnsafe(ui, out text);
            }
            catch
            {
                _guiContentCheckFailed = false;
            }
            text = null;
            return false;
        }

        private static bool TryGetTextFromGUIContentUnsafe(object ui, out string text)
        {
            if (ui is GUIContent gui)
            {
                text = gui.text;
                return true;
            }
            text = null;
            return false;
        }

        private static Type GetTypeByName(string typeName)
        {
            try
            {
                return Type.GetType(typeName);
            }
            catch
            {
                return null;
            }
        }

        private static Component GetFirstComponentInSelfOrAncestor(this GameObject go, Type type)
        {
            if (type == null) return null;

            var current = go;

            while (current != null)
            {
                var foundComponent = current.GetComponent(type);
                if (foundComponent != null)
                {
                    return foundComponent;
                }

                current = current.transform?.parent?.gameObject;
            }

            return null;
        }

        private static string GetTextFromComponent(object ui, TextTranslationInfo info)
        {
            if (ui == null || info?.TextManipulator == null) return null;

            try
            {
                return info.TextManipulator.GetText(ui);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError($"Error in GetTextFromComponent: {ex.Message}");
                return null;
            }
        }

        private static void SetTextOnComponent(object ui, string text, TextTranslationInfo info)
        {
            if (ui == null || info?.TextManipulator == null) return;

            try
            {
                info.TextManipulator.SetText(ui, text);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError($"Error in SetTextOnComponent: {ex.Message}");
            }
        }
    }
}