using GameTranslator.Patches.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using XUnity.Common.Constants;
using XUnity.Common.Utilities;

namespace GameTranslator.Patches.Translatons.Manipulator
{
    internal class DefaultTextComponentManipulator : ITextComponentManipulator
    {
        public DefaultTextComponentManipulator(Type type)
        {
            this._type = type;
            if (type.GetProperty(DefaultTextComponentManipulator.TextPropertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) != null)
            {
                this._property = type.CachedProperty(DefaultTextComponentManipulator.TextPropertyName);
                return;
            }
            this._property = type.CachedProperty("Text");
        }

        public string GetText(object ui)
        {
            CachedProperty property = this._property;
            return (string)((property != null) ? property.Get(ui) : null);
        }

        public void SetText(object ui, string text)
        {
            try
            {
                Type type = this._type;
                if (UnityTypes.TextWindow != null && UnityTypes.TextMeshPro != null && UnityTypes.TextMeshPro.ClrType.IsAssignableFrom(type))
                {
                    if (IsTextWindowTextMesh(ui))
                    {
                        BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                        if (new StackTrace().GetFrames().Any((StackFrame x) => x.GetMethod().DeclaringType == UnityTypes.TextWindow.ClrType))
                        {
                            if (TranslatePlugin.enableTypingTranslation.Value)
                            {
                                string partial = GetPartialTypingTranslation(ui, text);
                                if (partial != null)
                                {
                                    type.CachedProperty(DefaultTextComponentManipulator.TextPropertyName)?.Set(ui, partial);
                                }
                            }
                            else
                            {
                                _cachedTextWindow.GetType().GetField("curText", flags).SetValue(_cachedTextWindow, text);
                            }
                        }
                        else
                        {
                            type.CachedProperty(DefaultTextComponentManipulator.TextPropertyName)?.Set(ui, text);
                            object previousCurText = _cachedTextWindow.GetType().GetField("curText", flags).GetValue(_cachedTextWindow);
                            _cachedTextWindow.GetType().GetField("curText", flags).SetValue(_cachedTextWindow, text);
                            _cachedTextWindow.GetType().GetMethod("FinishTyping", flags).Invoke(_cachedTextWindow, null);
                            _cachedTextWindow.GetType().GetField("curText", flags).SetValue(_cachedTextWindow, previousCurText);
                            object keyword = _cachedTextWindow.GetType().GetField("Keyword", flags).GetValue(_cachedTextWindow);
                            keyword.GetType().GetMethod("UpdateTextMesh", flags).Invoke(keyword, new object[] { _cachedTextWindowTextMesh, true });
                        }
                        return;
                    }
                }
                CachedProperty property = this._property;
                if (property != null)
                {
                    property.Set(ui, text);
                }
                CachedProperty cachedProperty2 = type.CachedProperty("maxVisibleCharacters");
                if (cachedProperty2 != null && cachedProperty2.PropertyType == typeof(int))
                {
                    int num = (int)cachedProperty2.Get(ui);
                    if (0 < num && num < 99999)
                    {
                        cachedProperty2.Set(ui, 99999);
                    }
                }
                if (UnityTypes.TextExpansion_Methods.SetMessageType != null && UnityTypes.TextExpansion_Methods.SkipTypeWriter != null && UnityTypes.TextExpansion.ClrType.IsAssignableFrom(type))
                {
                    UnityTypes.TextExpansion_Methods.SetMessageType.Invoke(ui, 1);
                    UnityTypes.TextExpansion_Methods.SkipTypeWriter.Invoke(ui);
                }
            }
            catch (System.IndexOutOfRangeException ex)
            {
                TranslatePlugin.logger.LogError($"IndexOutOfRangeException in DefaultTextComponentManipulator.SetText: {ex.Message}");
            }
            catch (System.NullReferenceException)
            {
            }
            catch (System.Exception ex)
            {
                TranslatePlugin.logger.LogError($"Exception in DefaultTextComponentManipulator.SetText: {ex.Message}");
            }
        }

        internal static bool IsTextWindowTextMesh(object ui)
        {
            if (UnityTypes.TextWindow == null || UnityTypes.TextMeshPro == null)
                return false;
            var type = ui.GetType();
            if (!UnityTypes.TextMeshPro.ClrType.IsAssignableFrom(type))
                return false;
            if (_textWindowTextMeshCached &&
                (_cachedTextWindowTextMesh == null ||
                 (_cachedTextWindowTextMesh is UnityEngine.Object uObj1 && !uObj1) ||
                 (_cachedTextWindow is UnityEngine.Object uObj2 && !uObj2)))
            {
                _textWindowTextMeshCached = false;
                _cachedTextWindowTextMesh = null;
                _cachedTextWindow = null;
            }
            if (!_textWindowTextMeshCached)
            {
                var textWindow = UnityEngine.Object.FindObjectOfType(UnityTypes.TextWindow.ClrType);
                if (textWindow == null)
                    return false;
                var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                var field = textWindow.GetType().GetField("TextMesh", flags);
                if (field == null)
                    return false;
                _cachedTextWindow = textWindow;
                _cachedTextWindowTextMesh = field.GetValue(textWindow);
                _textWindowTextMeshCached = true;
            }
            return _cachedTextWindowTextMesh != null && object.Equals(_cachedTextWindowTextMesh, ui);
        }

        internal static bool IsPartialTypingText(string text)
        {
            if (_cachedTextWindow == null || UnityTypes.TextWindow == null)
                return false;
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var curText = _cachedTextWindow.GetType().GetField("curText", flags)?.GetValue(_cachedTextWindow) as string;
            return !string.IsNullOrEmpty(curText) && text.Length < curText.Length;
        }

        internal static void HandleTextWindowText(object ui, ref string value)
        {
            if (IsPartialTypingText(value))
            {
                if (TranslatePlugin.enableTypingTranslation.Value)
                {
                    string partial = GetPartialTypingTranslation(ui, value);
                    if (partial != null)
                        value = partial;
                }
                return;
            }
            string text = value;
            var info = ui.GetOrCreateTextTranslationInfo();
            bool shouldSync = text.Length <= TranslatePlugin.syncTranslationThreshold.Value || !TranslatePlugin.enableAsyncDuringTyping.Value;
            string translated = null;
            if (shouldSync)
                translated = TextTranslate.Instance.TranslateImmediate(ui, text, info, TranslateConfig.normalText, TranslateConfig.normal, false);      // ignoreComponentState = false, for now
            if (string.IsNullOrEmpty(translated) && TranslatePlugin.enableAsyncDuringTyping.Value)
                translated = TextTranslate.Instance.TranslateOrQueue(ui, text, info, TranslateConfig.normalText, TranslateConfig.normal, false);        // ignoreComponentState = false, for now
            if (!string.IsNullOrEmpty(translated) && !translated.Equals(text))
            {
                TextTranslate.Instance.SetTranslatedText(ui, translated, text, info);
            }
        }

        internal static string GetPartialTypingTranslation(object ui, string currentPartialText)
        {
            if (_cachedTextWindow == null || UnityTypes.TextWindow == null)
                return null;
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            string fullText = _cachedTextWindow.GetType().GetField("curText", flags)?.GetValue(_cachedTextWindow) as string;
            if (string.IsNullOrEmpty(fullText) || string.IsNullOrEmpty(currentPartialText))
                return null;

            if (!_typingCache.TryGetValue(ui, out var cached) || cached.original != fullText)
            {
                var info = ui.GetOrCreateTextTranslationInfo();
                bool savedMustIgnore = info.MustIgnore;
                info.MustIgnore = false;
                try
                {
                    bool shouldSync = fullText.Length <= TranslatePlugin.syncTranslationThreshold.Value || !TranslatePlugin.enableAsyncDuringTyping.Value;
                    string t = null;
                    if (shouldSync)
                        t = TextTranslate.Instance.TranslateImmediate(ui, fullText, info, TranslateConfig.normalText, TranslateConfig.normal, false);
                    if (string.IsNullOrEmpty(t) && TranslatePlugin.enableAsyncDuringTyping.Value)
                        t = TextTranslate.Instance.TranslateOrQueue(ui, fullText, info, TranslateConfig.normalText, TranslateConfig.normal, false);
                    bool isAsync = fullText.Length > TranslatePlugin.syncTranslationThreshold.Value;
                    if (isAsync && t == null)
                        return null;
                    if (string.IsNullOrEmpty(t) || t == fullText)
                        return null;
                    cached = (fullText, t);
                    _typingCache[ui] = cached;
                }
                finally
                {
                    info.MustIgnore = savedMustIgnore;
                }
            }

            float progress = (float)currentPartialText.Length / fullText.Length;
            int len = (int)System.Math.Ceiling(cached.translated.Length * progress);
            if (len > cached.translated.Length)
                len = cached.translated.Length;
            if (len <= 0)
                len = 1;
            return cached.translated.Substring(0, len);
        }

        internal static void ClearCache()
        {
            _cachedTextWindowTextMesh = null;
            _cachedTextWindow = null;
            _textWindowTextMeshCached = false;
            _typingCache.Clear();
        }

        private static readonly string TextPropertyName = "text";

        private readonly Type _type;

        private readonly CachedProperty _property;

        private static object _cachedTextWindowTextMesh;
        private static object _cachedTextWindow;
        private static bool _textWindowTextMeshCached;
        private static Dictionary<object, (string original, string translated)> _typingCache = new Dictionary<object, (string, string)>();
    }
}
