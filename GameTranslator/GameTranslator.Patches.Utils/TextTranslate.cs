using BepInEx;
using BepInEx.Logging;
using GameTranslator.Patches.Translatons;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTranslator.Patches.Utils
{
    internal class TextTranslate
    {
        private static readonly Dictionary<string, DateTime> _debugOutputCache = new Dictionary<string, DateTime>();
        private static readonly TimeSpan _debugOutputInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan _cacheCleanupInterval = TimeSpan.FromMinutes(5);
        private static DateTime _lastCleanupTime = DateTime.Now;

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
        internal void Hook_TextChanged(object ui)
        {
            if (TranslatePlugin.enableTerminalPatch != null && TranslatePlugin.enableTerminalPatch.Value)
            {
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
                                return;
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            TextTranslationInfo orCreateTextTranslationInfo = ui.GetOrCreateTextTranslationInfo();
            bool flag = this.DiscoverComponent(ui, orCreateTextTranslationInfo);
            if (TranslatePlugin.shouldTranslateSpecialText.Value || TranslatePlugin.shouldTranslateNormalText.Value)
            {
                string currentText = ui.GetText(orCreateTextTranslationInfo);
                string translatedText = this.TranslateOrQueue(ui, currentText, orCreateTextTranslationInfo, TranslateConfig.normalText, TranslateConfig.text, flag);
                if (!string.IsNullOrEmpty(translatedText) && !translatedText.Equals(currentText) && IsUIObjectValid(ui))
                {
                    this.SetText(ui, translatedText, true, currentText, orCreateTextTranslationInfo);
                }
            }
        }

        internal void Hook_TextChanged(object ui, ref string value)
        {
            if (TranslatePlugin.enableTerminalPatch != null && TranslatePlugin.enableTerminalPatch.Value)
            {
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
                                return;
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            TextTranslationInfo orCreateTextTranslationInfo = ui.GetOrCreateTextTranslationInfo();
            bool flag = this.DiscoverComponent(ui, orCreateTextTranslationInfo);
            if (TranslatePlugin.shouldTranslateSpecialText.Value || TranslatePlugin.shouldTranslateNormalText.Value)
            {
                string translatedText = this.TranslateOrQueue(ui, value, orCreateTextTranslationInfo, TranslateConfig.normalText, TranslateConfig.text, flag);
                if (!string.IsNullOrEmpty(translatedText) && !translatedText.Equals(value) && IsUIObjectValid(ui))
                {
                    value = translatedText;
                }
            }
        }

        public string TranslateOrQueue(object ui, string text, TextTranslationInfo info, NormalTextTranslator normalText, TranslateConfig.TranslateConfigFile config, bool ignoreComponentState)
        {
            if (info != null && (info.IsCurrentlySettingText || info.MustIgnore))
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
                if (!info.OriginalText.Equals(text) || info.ChangeTime != TextTranslate.ChangeTime)
                {
                    info.Reset(text);
                }
                else if (info.OriginalText.Equals(text) || info.TranslatedText.Equals(text))
                {
                    return info.TranslatedText;
                }
            }

            string cachedTranslation = GameTranslator.Patches.Translatons.AsyncTranslationManager.Instance.GetCachedTranslation(text, config);
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

            if (normalText == null || normalText.IsTranslatable(text, false))
            {
                if (text.Length <= TranslatePlugin.syncTranslationThreshold.Value)
                {
                    var translatedText = TranslateImmediate(ui, text, info, normalText, config, ignoreComponentState);
                    if (translatedText != null)
                    {
                        GameTranslator.Patches.Translatons.AsyncTranslationManager.Instance.GetCachedTranslation(text, config);
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
            if (info != null && (info.IsCurrentlySettingText || info.MustIgnore))
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
                if (!info.OriginalText.Equals(text) || info.ChangeTime != TextTranslate.ChangeTime)
                {
                    info.Reset(text);
                }
                else if (info.OriginalText.Equals(text) || info.TranslatedText.Equals(text))
                {
                    return info.TranslatedText;
                }
            }

            string text3 = null;
            if ((normalText == null || normalText.IsTranslatable(text, false)) && (ignoreComponentState || ui.IsComponentActive()))
            {
                if (normalText != null && TranslatePlugin.shouldTranslateNormalText.Value)
                {
                    if (TranslatePlugin.showAvailableText.Value && ShouldOutputDebug($"available:{text}"))
                    {
                        TranslatePlugin.logger.LogInfo($"[Debug] Found available text: '{text}'");
                    }
                    text3 = normalText.TryTranslate(text);
                }
                if (text3 != null)
                {
                    text3 = TranslateConfig.replaceByMap(text3, config);
                    this.SetTranslatedText(ui, text3, text, info);
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

        private bool IsUIObjectValid(object ui)
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
