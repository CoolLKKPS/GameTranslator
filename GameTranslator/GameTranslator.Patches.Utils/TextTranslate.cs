using BepInEx;
using BepInEx.Logging;
using GameTranslator.Patches.Translatons;
using GameTranslator.Patches.Translatons.Manipulator;
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
            if (!TranslatePlugin.showAvailableText.Value && !TranslatePlugin.showOtherDebug.Value)
            {
                return false;
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
            if (!TranslatePlugin.showAvailableText.Value && !TranslatePlugin.showOtherDebug.Value)
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

        private bool TryTranslateChangedText(object ui, ref string text, out string translated, out TextTranslationInfo info)
        {
            translated = null;
            info = null;
            if (IsTerminalIgnoredUI(ui))
                return false;
            info = ui.GetOrCreateTextTranslationInfo();
            bool componentState = this.DiscoverComponent(ui, info);

            if (!TranslatePlugin.shouldTranslateNormalText.Value)
                return false;

            if (text == null)
                text = ui.GetText(info);
            translated = this.TranslateOrQueue(ui, text, info, TranslateConfig.normalText, TranslateConfig.normal, componentState);
            return !string.IsNullOrEmpty(translated) && !translated.Equals(text) && IsUIObjectValid(ui);
        }

        internal void OnComponentTextChanged(object ui)
        {
            if (DefaultTextComponentManipulator.IsTextWindowTextMesh(ui))
                return;
            string currentText = null;
            if (TryTranslateChangedText(ui, ref currentText, out var translated, out var info))
            {
                this.SetText(ui, translated, info);
            }
        }

        internal void OnTranslateIncomingText(object ui, ref string value)
        {
            if (DefaultTextComponentManipulator.IsTextWindowTextMesh(ui))
            {
                DefaultTextComponentManipulator.HandleTextWindowText(ui, ref value);
                return;
            }
            string text = value;
            if (TryTranslateChangedText(ui, ref text, out var translated, out _))
            {
                value = translated;
            }
        }

        public string TranslateOrQueue(object ui, string text, TextTranslationInfo info, NormalTextTranslator normalText, TranslateConfig.TranslateConfigFile config, bool ignoreComponentState)
        {
            string immediate = GuardAndPrepareText(ui, ref text, info, out bool shouldContinue, ignoreComponentState);
            if (!shouldContinue)
                return immediate;

            string cachedTranslation = normalText?.TryGetCachedTranslation(text, TranslationScopeHelper.GetScope(ui));
            if (cachedTranslation != null)
            {
                if (!TranslatePlugin.showAvailableText.Value && TranslatePlugin.showOtherDebug.Value && ShouldOutputDebug($"cached-result:{text}"))
                {
                    TranslatePlugin.logger.LogInfo($"[Debug] Cached translation found for: '{text}' -> '{cachedTranslation}'");
                }
                else if (TranslatePlugin.showAvailableText.Value && TranslatePlugin.showOtherDebug.Value && ShouldOutputDebug($"cached:{text}"))
                {
                    TranslatePlugin.logger.LogInfo($"[Debug] Cached translation hit for: '{text}'");
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
            string immediate = GuardAndPrepareText(ui, ref text, info, out bool shouldContinue, ignoreComponentState);
            if (!shouldContinue)
                return immediate;

            string result = null;
            int scope = TranslationScopeHelper.GetScope(ui);
            if (normalText == null || normalText.IsTranslatable(text, false, scope))
            {
                if (normalText != null && TranslatePlugin.shouldTranslateNormalText.Value)
                {
                    if (TranslatePlugin.showAvailableText.Value && ShouldOutputDebug($"available:{text}"))
                    {
                        TranslatePlugin.logger.LogInfo($"[Debug] Found available text: '{text}'");
                    }
                    result = normalText.TryTranslate(text, scope);
                }
                if (result != null)
                {
                    if (info != null)
                    {
                        info.OriginalText = text;
                        info.SetTranslatedText(result);
                    }
                }
            }
            return result;
        }

        private static string GuardAndPrepareText(object ui, ref string text, TextTranslationInfo info, out bool shouldContinue, bool ignoreComponentState = false)
        {
            shouldContinue = false;

            if (!ignoreComponentState && !ui.IsComponentActive())
            {
                return null;
            }

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

            shouldContinue = true;
            return null;
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

        private void SetText(object ui, string text, TextTranslationInfo info)
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

                return true;
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
