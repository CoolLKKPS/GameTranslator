using GameTranslator.Patches.Utils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace GameTranslator.Patches.Translatons
{
    internal class AsyncTranslationManager
    {
        private readonly TranslationManager _translationManager;
        private readonly ConcurrentQueue<Action> _mainThreadActions;
        private readonly HashSet<string> _immediatelyTranslating;
        private float _batchOperationSecondCounter = 0;
        private readonly Dictionary<string, TextStabilizationContext> _stabilizationContexts;
        private bool _textHooksEnabled = true;
        private bool _temporarilyDisabled = false;

        public static AsyncTranslationManager Instance { get; } = new AsyncTranslationManager();

        private AsyncTranslationManager()
        {
            _translationManager = new TranslationManager();
            _mainThreadActions = new ConcurrentQueue<Action>();
            _immediatelyTranslating = new HashSet<string>();
            _stabilizationContexts = new Dictionary<string, TextStabilizationContext>();
            _translationManager.JobCompleted += OnTranslationJobCompleted;
            _translationManager.JobFailed += OnTranslationJobFailed;
            var endpoint = new TranslationEndpointManager(
                maxConcurrency: 3,
                maxRetries: 3,
                translationDelay: 1.0f
            );
            _translationManager.RegisterEndpoint(endpoint);
        }

        public void Start()
        {
        }

        public void Stop()
        {
            _translationManager?.ClearAllJobs();
            _stabilizationContexts.Clear();
        }

        public bool IsTemporarilyDisabled()
        {
            return _temporarilyDisabled;
        }

        public string GetCachedTranslation(string originalText, TranslateConfig.TranslateConfigFile config)
        {
            if (_translationManager?.PrimaryEndpoint == null) return null;
            string cacheKey = GetCacheKey(originalText, config);
            return _translationManager.PrimaryEndpoint.TryGetTranslation(cacheKey, out var translation) ? translation : null;
        }

        public void QueueTranslation(object ui, string originalText, TextTranslationInfo info, NormalTextTranslator normalText, TranslateConfig.TranslateConfigFile config, bool ignoreComponentState)
        {
            if (string.IsNullOrEmpty(originalText) || string.IsNullOrWhiteSpace(originalText)) return;
            if (!_textHooksEnabled || _temporarilyDisabled) return;
            try
            {
                if (info != null && info.IsTranslated)
                {
                    info.Reset(originalText);
                }
                var cachedTranslation = GetCachedTranslation(originalText, config);
                if (cachedTranslation != null)
                {
                    if (IsUIObjectValid(ui))
                    {
                        _mainThreadActions.Enqueue(() => SafeUpdateUI(ui, cachedTranslation, originalText, info, TextTranslate.ChangeTime));
                    }
                    return;
                }
                if (originalText.Length <= TranslatePlugin.syncTranslationThreshold.Value)
                {
                    var translatedText = TranslateText(originalText, normalText, config);
                    if (!string.IsNullOrEmpty(translatedText) && !translatedText.Equals(originalText))
                    {
                        string cacheKey = GetCacheKey(originalText, config);
                        _translationManager.PrimaryEndpoint?.AddTranslationToCache(cacheKey, translatedText);
                        if (IsUIObjectValid(ui))
                        {
                            _mainThreadActions.Enqueue(() => SafeUpdateUI(ui, translatedText, originalText, info, TextTranslate.ChangeTime));
                        }
                    }
                    return;
                }
                if (_translationManager?.PrimaryEndpoint != null)
                {
                    var isTranslatable = normalText == null || normalText.IsTranslatable(originalText, false);
                    if (!_immediatelyTranslating.Contains(originalText))
                    {
                        _immediatelyTranslating.Add(originalText);
                        if (ShouldStabilizeText(ui, originalText))
                        {
                            StartTextStabilization(ui, originalText, info, normalText, config);
                        }
                        else
                        {
                            var job = new TranslationJob(ui, originalText, true, isTranslatable);
                            job.Associate(originalText, ui, info, normalText, config, true, true);
                            _translationManager.PrimaryEndpoint.EnqueueTranslation(
                                ui,
                                originalText,
                                info,
                                normalText,
                                config,
                                isTranslatable,
                                true
                            );
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TranslatePlugin.logger.LogError($"An unexpected error occurred in QueueTranslation: {e.Message}");
            }
        }

        private bool ShouldStabilizeText(object ui, string text)
        {
            if (ui == null) return false;
            if (!ui.SupportsStabilization())
            {
                return false;
            }
            return text.Length > 100;
        }

        private void StartTextStabilization(object ui, string text, TextTranslationInfo info, NormalTextTranslator normalText, TranslateConfig.TranslateConfigFile config)
        {
            var context = new TextStabilizationContext
            {
                UI = ui,
                OriginalText = text,
                Info = info,
                NormalText = normalText,
                Config = config,
                StartTime = Time.realtimeSinceStartup,
                MaxTries = 60,
                CurrentTries = 0,
                Delay = 0.9f
            };
            string key = GetStabilizationKey(ui, text);
            _stabilizationContexts[key] = context;
            if (ui is MonoBehaviour behaviour)
            {
                behaviour.StartCoroutine(WaitForTextStablization(ui, info, context.Delay, context.MaxTries, 0,
                    stabilizedText =>
                    {
                        OnTextStabilized(context, stabilizedText);
                        try
                        {
                            _stabilizationContexts.Remove(GetStabilizationKey(ui, context.OriginalText));
                            _immediatelyTranslating.Remove(context.OriginalText);
                        }
                        catch (Exception ex)
                        {
                            TranslatePlugin.logger.LogError($"Error cleaning up stabilization context: {ex.Message}");
                        }
                    },
                    () =>
                    {
                        OnStabilizationFailed(context);
                        try
                        {
                            _stabilizationContexts.Remove(GetStabilizationKey(ui, context.OriginalText));
                            _immediatelyTranslating.Remove(context.OriginalText);
                        }
                        catch (Exception ex)
                        {
                            TranslatePlugin.logger.LogError($"Error cleaning up stabilization context: {ex.Message}");
                        }
                    }));
            }
        }

        private IEnumerator WaitForTextStablization(object ui, TextTranslationInfo info, float delay, int maxTries, int currentTries, Action<string> onTextStabilized, Action onMaxTriesExceeded)
        {
            yield return null;
            bool succeeded = false;
            while (currentTries < maxTries)
            {
                string beforeText = null;
                string afterText = null;
                try
                {
                    beforeText = GetUIText(ui, info);
                }
                catch (Exception ex)
                {
                    TranslatePlugin.logger.LogError($"Error getting before text during stabilization: {ex.Message}");
                    break;
                }
                float start = Time.realtimeSinceStartup;
                var end = start + delay;
                while (Time.realtimeSinceStartup < end)
                {
                    yield return null;
                }
                try
                {
                    afterText = GetUIText(ui, info);
                }
                catch (Exception ex)
                {
                    TranslatePlugin.logger.LogError($"Error getting after text during stabilization: {ex.Message}");
                    break;
                }
                if (beforeText == afterText)
                {
                    onTextStabilized(afterText);
                    succeeded = true;
                    break;
                }
                currentTries++;
            }
            if (!succeeded)
            {
                onMaxTriesExceeded();
            }
        }

        private void OnTextStabilized(TextStabilizationContext context, string stabilizedText)
        {
            if (context.Info?.IsTranslated == true) return;
            context.Info?.Reset(stabilizedText);
            if (string.IsNullOrWhiteSpace(stabilizedText)) return;
            var cachedTranslation = GetCachedTranslation(stabilizedText, context.Config);
            if (cachedTranslation != null)
            {
                SafeUpdateUI(context.UI, cachedTranslation, stabilizedText, context.Info, context.Info?.ChangeTime ?? TextTranslate.ChangeTime);
                return;
            }
            var isTranslatable = context.NormalText == null || context.NormalText.IsTranslatable(stabilizedText, false);
            var job = new TranslationJob(context.UI, stabilizedText, true, isTranslatable);
            job.Associate(stabilizedText, context.UI, context.Info, context.NormalText, context.Config, true, true);
            _translationManager.PrimaryEndpoint.EnqueueTranslation(
                context.UI,
                stabilizedText,
                context.Info,
                context.NormalText,
                context.Config,
                isTranslatable,
                true
            );
        }

        private void OnStabilizationFailed(TextStabilizationContext context)
        {
            context.Info?.Reset(context.OriginalText);
        }

        public void ProcessMainThreadActions()
        {
            while (_mainThreadActions.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    TranslatePlugin.logger.LogError($"Error in main thread action: {ex.Message}");
                }
            }
        }

        public void Update()
        {
            IncrementBatchOperations();
            _translationManager?.KickoffTranslations();
            ProcessMainThreadActions();
        }

        private void IncrementBatchOperations()
        {
            _batchOperationSecondCounter += Time.deltaTime;

            if (_batchOperationSecondCounter > 1.0f)
            {
                var endpoints = _translationManager.Endpoints;
                foreach (var endpoint in endpoints)
                {
                    if (endpoint.AvailableBatchOperations < 5)
                    {
                        endpoint.AvailableBatchOperations++;
                    }
                }

                _batchOperationSecondCounter = 0;
            }
        }

        private void OnTranslationJobCompleted(TranslationJob job)
        {
            try
            {
                _immediatelyTranslating.Remove(job.OriginalText);
                if (!string.IsNullOrEmpty(job.TranslatedText))
                {
                    foreach (var ui in job.AssociatedUIs)
                    {
                        if (IsUIObjectValid(ui))
                        {
                            _mainThreadActions.Enqueue(() => SafeUpdateUI(ui, job.TranslatedText, job.OriginalText, job.TranslationInfo, job.StartVersion));
                        }
                    }
                    string cacheKey = GetCacheKey(job.OriginalText, job.Config);
                    _translationManager.PrimaryEndpoint?.AddTranslationToCache(cacheKey, job.TranslatedText);
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError($"Error handling translation job completion: {ex.Message}");
            }
        }

        private void OnTranslationJobFailed(TranslationJob job)
        {
            try
            {
                _immediatelyTranslating.Remove(job.OriginalText);

                TranslatePlugin.logger.LogWarning($"Translation failed for '{job.OriginalText}': {job.ErrorMessage}");
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError($"Error handling translation job failure: {ex.Message}");
            }
        }

        private void SafeUpdateUI(object ui, string translatedText, string originalText, object translationInfo, long expectedVersion)
        {
            if (!IsUIObjectValid(ui)) return;

            try
            {
                var info = translationInfo as TextTranslationInfo;
                if (info != null)
                {
                    if (info.IsCurrentlySettingText) return;
                    string currentText = GetUIText(ui, info);
                    if (currentText != originalText && currentText != info.TranslatedText)
                    {
                        return;
                    }
                    if (info.ChangeTime != expectedVersion && currentText != originalText)
                    {
                        return;
                    }
                    info.OriginalText = originalText;
                    info.SetTranslatedText(translatedText);
                }
                TextTranslate.Instance.SetTranslatedText(ui, translatedText, originalText, info as TextTranslationInfo);
            }
            catch (System.NullReferenceException)
            {
            }
            catch (System.Exception ex)
            {
                TranslatePlugin.logger.LogError($"Failed to safely update UI for text '{originalText}': {ex.Message}");
            }
        }

        private string GetUIText(object ui, TextTranslationInfo info)
        {
            try
            {
                if (ui == null) return string.Empty;
                return ui.GetText(info);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError($"Error getting UI text: {ex.Message}");
                return string.Empty;
            }
        }

        private string TranslateText(string text, NormalTextTranslator normalText, TranslateConfig.TranslateConfigFile config)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string translatedText = text;
            try
            {
                if (normalText != null && TranslatePlugin.shouldTranslateNormalText.Value && normalText.IsTranslatable(text, false))
                {
                    translatedText = normalText.TryTranslate(translatedText);
                }
                translatedText = TranslateConfig.replaceByMap(translatedText, config);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError($"Translation error for text '{text}': {ex.Message}");
                return text;
            }
            return translatedText;
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

        public void ClearCache()
        {
            _translationManager?.ClearAllJobs();
            _stabilizationContexts.Clear();
            _immediatelyTranslating.Clear();
            TextTranslate.ChangeTime += 1L;
        }

        private string GetCacheKey(string originalText, TranslateConfig.TranslateConfigFile config)
        {
            return $"{config?.ConfigFileName ?? "global"}:{originalText}";
        }

        private string GetStabilizationKey(object ui, string text)
        {
            return $"{ui.GetHashCode()}:{text}";
        }
    }

    internal class TextStabilizationContext
    {
        public object UI { get; set; }
        public string OriginalText { get; set; }
        public TextTranslationInfo Info { get; set; }
        public NormalTextTranslator NormalText { get; set; }
        public TranslateConfig.TranslateConfigFile Config { get; set; }
        public float StartTime { get; set; }
        public int MaxTries { get; set; }
        public int CurrentTries { get; set; }
        public float Delay { get; set; }
    }
}