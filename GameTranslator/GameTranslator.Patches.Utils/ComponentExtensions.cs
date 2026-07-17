using UnityEngine;

namespace GameTranslator.Patches.Utils
{
    internal static class ComponentExtensions
    {
        private static bool _guiContentCheckFailed = false;

        public static bool SupportsStabilization(this object ui)
        {
            if (ui == null) return false;
            return _guiContentCheckFailed || !IsGUIContentSafe(ui);
        }

        private static bool IsGUIContentSafe(object ui)
        {
            try
            {
                return ui is GUIContent;
            }
            catch
            {
                _guiContentCheckFailed = true;
            }
            return false;
        }

        /*
        public static bool IsSpammingComponent(this object ui)
        {
            return ui == null
                || (!_guiContentCheckFailed && IsGUIContentSafe(ui));
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

        private static bool SetTextOnGUIContentUnsafe(object ui, string text)
        {
            if (ui is GUIContent gui)
            {
                gui.text = text;
                return true;
            }
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
        */
    }
}