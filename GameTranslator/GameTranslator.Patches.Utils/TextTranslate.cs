using BepInEx;
using BepInEx.Logging;
using GameTranslator.Patches.Translatons;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XUnity.Common.Constants;

namespace GameTranslator.Patches.Utils
{
    internal class TextTranslate
    {
        private static readonly Dictionary<string, DateTime> _debugOutputCache = new Dictionary<string, DateTime>();
        private static readonly TimeSpan _debugOutputInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan _cacheCleanupInterval = TimeSpan.FromMinutes(5);
        private static DateTime _lastCleanupTime = DateTime.Now;
        internal static HashSet<object> _translatingFromFinishTyping = new HashSet<object>();
        private static object _cachedTextWindowTextMesh;
        private static object _cachedTextWindow;
        private static bool _textWindowTextMeshCached;
        private static Dictionary<object, (string original, string translated)> _typingCache = new Dictionary<object, (string, string)>();
        internal static bool EnableTypingTranslation = false;

        public static void ClearCache()
        {
            _cachedTextWindowTextMesh = null;
            _cachedTextWindow = null;
            _textWindowTextMeshCached = false;
            _translatingFromFinishTyping.Clear();
            _typingCache.Clear();
        }

        public static bool ShouldOutputDebug(string text)
        {
            if (!TranslatePlugin.showOtherDebug.Value && !TranslatePlugin.showAvailableText.Value)
            {
                return true;
            }

            var now = DateTime.Now;

            if (now - _lastCleanupTime > _cacheCleanupInterval)
            {
                CleanupDebugCache();
                _lastCleanupTime = now;
            }

            if (_debugOutputCache.TryGetValue(text, out var lastOutputTime))
            {
                if (now - lastOutputTime < _debugOutputInterval)
                {
                    return false;
                }
            }
            _debugOutputCache[text] = now;
            return true;
        }

        private static void CleanupDebugCache()
        {
            if (!TranslatePlugin.showOtherDebug.Value && !TranslatePlugin.showAvailableText.Value)
            {
                _debugOutputCache.Clear();
                return;
            }

            var now = DateTime.Now;
            var keysToRemove = new List<string>();

            foreach (var entry in _debugOutputCache)
            {
                if (now - entry.Value > _cacheCleanupInterval)
                {
                    keysToRemove.Add(entry.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _debugOutputCache.Remove(key);
            }

            if (TranslatePlugin.showOtherDebug.Value && keysToRemove.Count > 0)
            {
                TranslatePlugin.logger.LogInfo($"[Debug] Cleaned up {keysToRemove.Count} old debug cache entries");
            }
        }

        private static bool IsTerminalIgnoredUI(object ui)
        {
            if (TranslatePlugin.enableTerminalPatch == null || !TranslatePlugin.enableTerminalPatch.Value)
                return false;
            try
            {
                var terminalPatchType = Type.GetType("GameTranslator.Patches.TerminalPatch, GameTranslator");
                if (terminalPatchType != null)
                {
                    var igField = terminalPatchType.GetField("ig", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (igField != null)
                    {
                        var igValue = igField.GetValue(null) as System.Collections.Generic.HashSet<object>;
                        if (igValue != null && igValue.Contains(ui))
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        private static bool IsTextWindowTextMesh(object ui)
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
                var textWindow = global::UnityEngine.Object.FindObjectOfType(UnityTypes.TextWindow.ClrType);
                if (textWindow == null)
                    return false;
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                var field = textWindow.GetType().GetField("TextMesh", flags);
                if (field == null)
                    return false;
                _cachedTextWindow = textWindow;
                _cachedTextWindowTextMesh = field.GetValue(textWindow);
                _textWindowTextMeshCached = true;
            }
            return _cachedTextWindowTextMesh != null && object.Equals(_cachedTextWindowTextMesh, ui);
        }

        private static bool IsCallFromTextWindow()
        {
            return new System.Diagnostics.StackTrace().GetFrames().Any(
                x => x.GetMethod().DeclaringType == UnityTypes.TextWindow.ClrType);
        }

        private static string GetPartialTypingTranslation(object ui, string currentPartialText)
        {
            if (UnityTypes.TextWindow == null || _cachedTextWindow == null)
                return null;
            var textWindow = _cachedTextWindow;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            string fullText = textWindow.GetType().GetField("curText", flags)?.GetValue(textWindow) as string;
            if (string.IsNullOrEmpty(fullText) || string.IsNullOrEmpty(currentPartialText))
                return null;

            if (!_typingCache.TryGetValue(ui, out var cached) || cached.original != fullText)
            {
                var info = ui.GetOrCreateTextTranslationInfo();
                string t = Instance.TranslateImmediate(ui, fullText, info, TranslateConfig.normalText, TranslateConfig.text, false);
                if (string.IsNullOrEmpty(t))
                    t = Instance.TranslateOrQueue(ui, fullText, info, TranslateConfig.normalText, TranslateConfig.text, false);
                if (string.IsNullOrEmpty(t))
                    return null;
                cached = (fullText, t);
                _typingCache[ui] = cached;
            }

            float progress = (float)currentPartialText.Length / fullText.Length;
            int len = (int)System.Math.Ceiling(cached.translated.Length * progress);
            if (len > cached.translated.Length)
                len = cached.translated.Length;
            if (len <= 0)
                len = 1;
            return cached.translated.Substring(0, len);
        }

        internal static void ClearTypingCacheForUI(object ui)
        {
            _typingCache.Remove(ui);
        }

        private bool TryTranslateChangedText(object ui, ref string text, out string translated, out TextTranslationInfo info,
            NormalTextTranslator normalText = null, TranslateConfig.TranslateConfigFile config = null)
        {
            normalText = normalText ?? TranslateConfig.normalText;
            config = config ?? TranslateConfig.text;
            translated = null;
            info = null;
            if (IsTerminalIgnoredUI(ui))
                return false;
            if (IsTextWindowTextMesh(ui) && IsCallFromTextWindow())
            {
                if (!_translatingFromFinishTyping.Contains(ui))
                {
                    if (EnableTypingTranslation)
                    {
                        translated = GetPartialTypingTranslation(ui, text);
                        if (translated != null)
                            return true;
                    }
                    return false;
                }
            }
            info = ui.GetOrCreateTextTranslationInfo();
            bool componentState = this.DiscoverComponent(ui, info);
            if (!TranslatePlugin.shouldTranslateSpecialText.Value && !TranslatePlugin.shouldTranslateNormalText.Value)
                return false;
            if (text == null)
                text = ui.GetText(info);
            translated = this.TranslateOrQueue(ui, text, info, normalText, config, componentState);
            return !string.IsNullOrEmpty(translated) && !translated.Equals(text) && IsUIObjectValid(ui);
        }

        internal void OnComponentTextChanged(object ui, NormalTextTranslator normalText = null, TranslateConfig.TranslateConfigFile config = null)
        {
            normalText = normalText ?? TranslateConfig.normalText;
            config = config ?? TranslateConfig.text;
            string currentText = null;
            if (TryTranslateChangedText(ui, ref currentText, out var translated, out var info, normalText, config))
            {
                this.SetText(ui, translated, true, currentText, info);
            }
        }

        internal void OnTranslateIncomingText(object ui, ref string value)
        {
            string text = value;
            if (TryTranslateChangedText(ui, ref text, out var translated, out _))
            {
                value = translated;
            }
        }

        public string TranslateOrQueue(object ui, string text, TextTranslationInfo info, NormalTextTranslator normalText, TranslateConfig.TranslateConfigFile config, bool ignoreComponentState)
        {
            if (info != null && (info.IsCurrentlySettingText || info.MustIgnore || info.ShouldIgnore))
            {
                return null;
            }

            text = text ?? ui.GetText(info);
            if (text.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (info != null && info.IsTranslated)
            {
                if (info.OriginalText.Equals(text) || info.TranslatedText.Equals(text))
                {
                    if (info.ChangeTime != TextTranslate.ChangeTime)
                    {
                        info.Reset(text);
                    }
                    else
                    {
                        return info.TranslatedText;
                    }
                }
                else
                {
                    info.Reset(text);
                }
            }

            string cachedTranslation = GameTranslator.Patches.Translatons.AsyncTranslationManager.Instance.GetCachedTranslation(text, config, TranslationScopeHelper.GetScope(ui));
            if (cachedTranslation != null)
            {
                if (TranslatePlugin.showOtherDebug.Value && ShouldOutputDebug($"cached:{text}"))
                {
                    TranslatePlugin.logger.LogInfo($"[Debug] Cached translation found for: '{text}' -> '{cachedTranslation}'");
                }
                if (info != null)
                {
                    info.OriginalText = text;
                    info.SetTranslatedText(cachedTranslation);
                }

                if (!IsUIObjectValid(ui))
                {
                    return null;
                }

                try
                {
                    if (info != null)
                    {
                        info.IsCurrentlySettingText = true;
                    }

                    ui.SetText(cachedTranslation, info);
                }
                catch (System.NullReferenceException)
                {
                }
                catch (System.IndexOutOfRangeException ex)
                {
                    TranslatePlugin.logger.LogError($"IndexOutOfRangeException in cached translation: {ex.Message}");
                }
                catch (System.Exception ex)
                {
                    TranslatePlugin.logger.LogError($"Exception in cached translation: {ex.Message}");
                }
                finally
                {
                    if (info != null)
                    {
                        info.IsCurrentlySettingText = false;
                    }
                }

                return cachedTranslation;
            }

            if (normalText == null || normalText.IsTranslatable(text, false, TranslationScopeHelper.GetScope(ui)))
            {
                if (text.Length <= TranslatePlugin.syncTranslationThreshold.Value)
                {
                    var translatedText = TranslateImmediate(ui, text, info, normalText, config, ignoreComponentState);
                    if (translatedText != null)
                    {
                        return translatedText;
                    }
                }
                else
                {
                    if (TranslatePlugin.showAvailableText.Value && ShouldOutputDebug($"queued:{text}"))
                    {
                        TranslatePlugin.logger.LogInfo($"[Debug] Queued available text: '{text}'");
                    }
                    GameTranslator.Patches.Translatons.AsyncTranslationManager.Instance.QueueTranslation(ui, text, info, normalText, config, ignoreComponentState);

                    if (info != null && info.IsTranslated && info.TranslatedText != null)
                    {
                        return info.TranslatedText;
                    }
                }
            }

            return null;
        }

        public string TranslateImmediate(object ui, string text, TextTranslationInfo info, NormalTextTranslator normalText, TranslateConfig.TranslateConfigFile config, bool ignoreComponentState)
        {
            if (info != null && (info.IsCurrentlySettingText || info.MustIgnore || info.ShouldIgnore))
            {
                return null;
            }

            text = text ?? ui.GetText(info);
            if (text.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (info != null && info.IsTranslated)
            {
                if (info.OriginalText.Equals(text) || info.TranslatedText.Equals(text))
                {
                    if (info.ChangeTime != TextTranslate.ChangeTime)
                    {
                        info.Reset(text);
                    }
                    else
                    {
                        return info.TranslatedText;
                    }
                }
                else
                {
                    info.Reset(text);
                }
            }

            string text3 = null;
            bool fallbackUsed = false;
            if ((normalText == null || normalText.IsTranslatable(text, false, TranslationScopeHelper.GetScope(ui))) && (ignoreComponentState || ui.IsComponentActive()))
            {
                if (normalText != null && TranslatePlugin.shouldTranslateNormalText.Value)
                {
                    if (TranslatePlugin.showAvailableText.Value && ShouldOutputDebug($"available:{text}"))
                    {
                        TranslatePlugin.logger.LogInfo($"[Debug] Found available text: '{text}'");
                    }
                    int scope = TranslationScopeHelper.GetScope(ui);
                    text3 = normalText.TryTranslate(text, scope);
                }
                if (text3 == null && config.shouldTranslate && config.normal.Count > 0 && (object.ReferenceEquals(config, TranslateConfig.text) || object.ReferenceEquals(config, TranslateConfig.hud)))
                {
                    text3 = text;
                    fallbackUsed = true;
                }
                if (text3 != null)
                {
                    var configTranslator = TranslateConfig.GetModuleTranslator(config);
                    bool sameSource = configTranslator != null && object.ReferenceEquals(normalText, configTranslator);
                    if ((fallbackUsed || !sameSource) && !normalText.IsScopedTranslation(text, TranslationScopeHelper.GetScope(ui)) && config.shouldTranslate && config.normal.Count > 0)
                    {
                        StringBuffer buffer = new StringBuffer(text3);
                        foreach (KeyValuePair<string, string> kv in config._normalOrdered)
                        {
                            buffer.ReplaceFull(kv.Key, kv.Value);
                        }
                        text3 = buffer.ToString();
                    }
                    if (info != null)
                    {
                        info.OriginalText = text;
                        info.SetTranslatedText(text3);
                    }
                }
            }
            return text3;
        }

        internal void SetTranslatedText(object ui, string translatedText, string originalText, TextTranslationInfo info)
        {
            if (info != null)
            {
                info.OriginalText = originalText;
                info.SetTranslatedText(translatedText);
            }

            if (!IsUIObjectValid(ui)) return;

            try
            {
                if (info != null)
                {
                    info.IsCurrentlySettingText = true;
                }

                ui.SetText(translatedText, info);
            }
            catch (System.NullReferenceException)
            {
            }
            catch (System.IndexOutOfRangeException ex)
            {
                TranslatePlugin.logger.LogError($"IndexOutOfRangeException in SetTranslatedText: {ex.Message}");
            }
            catch (System.Exception ex)
            {
                TranslatePlugin.logger.LogError($"Exception in SetTranslatedText: {ex.Message}");
            }
            finally
            {
                if (info != null)
                {
                    info.IsCurrentlySettingText = false;
                }
            }
        }

        private void SetText(object ui, string text, bool isTranslated, string originalText, TextTranslationInfo info)
        {
            if (info == null || !info.IsCurrentlySettingText)
            {
                if (!IsUIObjectValid(ui)) return;

                try
                {
                    if (info != null)
                    {
                        info.IsCurrentlySettingText = true;
                    }

                    ui.SetText(text, info);
                }
                catch (System.NullReferenceException)
                {
                }
                catch (System.IndexOutOfRangeException ex)
                {
                    TranslatePlugin.logger.LogError($"IndexOutOfRangeException in SetText: {ex.Message}");
                }
                catch (System.Exception ex)
                {
                    TranslatePlugin.logger.LogError($"Exception in SetText: {ex.Message}");
                }
                finally
                {
                    if (info != null)
                    {
                        info.IsCurrentlySettingText = false;
                    }
                }
            }
        }

        internal static bool IsUIObjectValid(object ui)
        {
            if (ui == null) return false;

            try
            {
                if (ui is Component component && component)
                {
                    var go = component.gameObject;
                    if (go)
                    {
                        if (component is Behaviour be)
                        {
                            return go.activeInHierarchy && be.enabled;
                        }
                        else
                        {
                            return go.activeInHierarchy;
                        }
                    }
                }

                return ui != null;
            }
            catch
            {
                return false;
            }
        }

        public bool DiscoverComponent(object ui, TextTranslationInfo info)
        {
            if (info != null && TranslatePlugin.changeFont.Value)
            {
                try
                {
                    bool flag = ui.IsComponentActive();
                    if (TranslatePlugin.fallbackFontTextMeshPro.Value != null && flag)
                    {
                        info.ChangeFont(ui);
                        return true;
                    }
                    return flag;
                }
                catch (Exception ex)
                {
                    ManualLogSource logger = TranslatePlugin.logger;
                    string text = "An error occurred while processing the UI.";
                    string newLine = Environment.NewLine;
                    Exception ex2 = ex;
                    logger.LogWarning(text + newLine + ((ex2 != null) ? ex2.ToString() : null));
                }
                return false;
            }
            return true;
        }

        public static TextTranslate Instance = new TextTranslate();

        public static long ChangeTime = 0L;

    }
}
