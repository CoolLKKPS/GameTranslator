using GameTranslator.Patches.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameTranslator.Patches.Translatons
{
    public class TranslationEndpointManager
    {
        private readonly ConcurrentDictionary<string, TranslationJob> _unstartedJobs;
        private readonly ConcurrentDictionary<string, TranslationJob> _ongoingJobs;
        private readonly ConcurrentDictionary<string, byte> _failedTranslations;
        private readonly ConcurrentDictionary<string, string> _translationCache;
        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly int _maxConcurrency;
        private readonly int _maxRetries;
        private readonly float _translationDelay;
        private int _availableBatchOperations = 5;

        public TranslationEndpointManager(int maxConcurrency = 3, int maxRetries = 3, float translationDelay = 1.0f)
        {
            _unstartedJobs = new ConcurrentDictionary<string, TranslationJob>();
            _ongoingJobs = new ConcurrentDictionary<string, TranslationJob>();
            _failedTranslations = new ConcurrentDictionary<string, byte>();
            _translationCache = new ConcurrentDictionary<string, string>();
            _concurrencyLimiter = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            _maxConcurrency = maxConcurrency;
            _maxRetries = maxRetries;
            _translationDelay = translationDelay;
        }

        public bool IsBusy => _ongoingJobs.Count >= _maxConcurrency;

        public bool HasUnstartedJob => !_unstartedJobs.IsEmpty;

        public int OngoingJobsCount => _ongoingJobs.Count;

        public int UnstartedJobsCount => _unstartedJobs.Count;

        public int AvailableBatchOperations
        {
            get => _availableBatchOperations;
            set => _availableBatchOperations = value;
        }

        public TranslationManager Manager { get; set; }

        public Exception Error { get; set; }

        public int ConsecutiveErrors { get; set; }

        public bool TryGetTranslation(string key, out string value)
        {
            return _translationCache.TryGetValue(key, out value);
        }

        public void AddTranslationToCache(string key, string value)
        {
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                _translationCache.TryAdd(key, value);
            }
        }

        public TranslationJob EnqueueTranslation(
            object ui,
            string key,
            object translationInfo,
            NormalTextTranslator normalText,
            TranslateConfig.TranslateConfigFile config,
            bool isTranslatable,
            bool allowFallback = true)
        {
            var jobKey = GetJobKey(key, config);

            if (_unstartedJobs.TryGetValue(jobKey, out var existingUnstartedJob))
            {
                existingUnstartedJob.Associate(key, ui, translationInfo, normalText, config, true, allowFallback);
                return null;
            }

            if (_ongoingJobs.TryGetValue(jobKey, out var existingOngoingJob))
            {
                existingOngoingJob.Associate(key, ui, translationInfo, normalText, config, true, allowFallback);
                return null;
            }

            var newJob = new TranslationJob(ui, key, true, isTranslatable);
            newJob.Scope = TranslationScopeHelper.GetScope(ui);
            newJob.Associate(key, ui, translationInfo, normalText, config, true, allowFallback);

            if (_unstartedJobs.TryAdd(jobKey, newJob))
            {
                Manager?.ScheduleUnstartedJobs(this);
                return newJob;
            }

            return null;
        }

        public async Task HandleNextJob()
        {
            if (_unstartedJobs.IsEmpty) return;

            var kvp = _unstartedJobs.FirstOrDefault();
            if (kvp.Value == null) return;

            var jobKey = kvp.Key;
            var job = kvp.Value;

            if (!_unstartedJobs.TryRemove(jobKey, out job)) return;

            _ongoingJobs.TryAdd(jobKey, job);
            Manager?.OnJobStarted(this);

            try
            {
                await _concurrencyLimiter.WaitAsync();
                await ProcessTranslationJob(job, jobKey);
            }
            finally
            {
                _concurrencyLimiter.Release();
                _ongoingJobs.TryRemove(jobKey, out _);
                Manager?.OnJobCompleted(this);

                if (_unstartedJobs.IsEmpty)
                {
                    Manager?.UnscheduleUnstartedJobs(this);
                }
            }
        }

        private async Task ProcessTranslationJob(TranslationJob job, string jobKey)
        {
            try
            {
                if (!CanTranslate(job.OriginalText))
                {
                    job.State = TranslationJobState.Failed;
                    job.ErrorMessage = "Translation failed due to too many previous failures.";
                    Manager?.InvokeJobFailed(job);
                    return;
                }

                var translatedText = await Task.Run(() => TranslateText(job.OriginalText, job.NormalText, job.Config, job.Scope));

                if (!string.IsNullOrEmpty(translatedText) && !translatedText.Equals(job.OriginalText))
                {
                    job.TranslatedText = translatedText;
                    job.State = TranslationJobState.Succeeded;
                    var cacheKey = GetCacheKey(job.OriginalText, job.Config);
                    AddTranslationToCache(cacheKey, translatedText);
                    Manager?.InvokeJobCompleted(job);
                }
                else
                {
                    if (string.IsNullOrEmpty(translatedText) || translatedText.Equals(job.OriginalText))
                    {
                        job.State = TranslationJobState.Succeeded;
                        job.TranslatedText = null;
                        Manager?.InvokeJobCompleted(job);
                    }
                    else
                    {
                        if (job.RetryCount < _maxRetries)
                        {
                            job.RetryCount++;
                            _unstartedJobs.TryAdd(jobKey, job);
                            await Task.Delay(TimeSpan.FromSeconds(_translationDelay));
                            Manager?.ScheduleUnstartedJobs(this);
                        }
                        else
                        {
                            job.State = TranslationJobState.Failed;
                            job.ErrorMessage = "Max retries exceeded.";
                            RegisterTranslationFailure(job.OriginalText);
                            Manager?.InvokeJobFailed(job);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError($"Translation job failed: {ex.Message}");

                if (job.RetryCount < _maxRetries)
                {
                    job.RetryCount++;
                    _unstartedJobs.TryAdd(jobKey, job);
                    await Task.Delay(TimeSpan.FromSeconds(_translationDelay));
                    Manager?.ScheduleUnstartedJobs(this);
                }
                else
                {
                    job.State = TranslationJobState.Failed;
                    job.ErrorMessage = ex.Message;
                    RegisterTranslationFailure(job.OriginalText);
                    Manager?.InvokeJobFailed(job);
                }
            }
        }

        private bool CanTranslate(string untranslatedText)
        {
            if (_failedTranslations.TryGetValue(untranslatedText, out var count))
            {
                return count < 3;
            }

            if (ShouldSkipTranslation(untranslatedText))
            {
                return false;
            }
            return true;
        }

        private bool ShouldSkipTranslation(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;

            return false;
        }

        private void RegisterTranslationFailure(string untranslatedText)
        {
            _failedTranslations.AddOrUpdate(untranslatedText, 1, (key, value) => (byte)(value + 1));
            TranslatePlugin.logger.LogWarning($"Translation failure registered for text: '{untranslatedText}' (Total failures: {_failedTranslations[untranslatedText]})");
        }

        private string TranslateText(string text, NormalTextTranslator normalText, TranslateConfig.TranslateConfigFile config, int scope = -1)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string translatedText = text;

            try
            {
                if (normalText != null && TranslatePlugin.shouldTranslateNormalText.Value && normalText.IsTranslatable(text, false, scope))
                {
                    translatedText = normalText.TryTranslate(translatedText, scope);
                }
                if (!normalText.IsScopedTranslation(text, scope) && config.normal.Count > 0)
                {
                    StringBuffer buffer = new StringBuffer(translatedText);
                    foreach (KeyValuePair<string, string> kv in config.normal.OrderByDescending((KeyValuePair<string, string> kv) => kv.Key.Length))
                    {
                        buffer.ReplaceFull(kv.Key, kv.Value);
                    }
                    translatedText = buffer.ToString();
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError($"Translation error for text '{text}': {ex.Message}");
                return text;
            }

            return translatedText;
        }

        private string GetJobKey(string text, TranslateConfig.TranslateConfigFile config)
        {
            return $"{config?.ConfigFileName ?? "global"}:{text}";
        }

        private string GetCacheKey(string text, TranslateConfig.TranslateConfigFile config)
        {
            return $"{config?.ConfigFileName ?? "global"}:{text}";
        }

        public void ClearAllJobs()
        {
            var unstartedJobs = _unstartedJobs.Values.ToList();
            var ongoingJobs = _ongoingJobs.Values.ToList();

            _unstartedJobs.Clear();
            _ongoingJobs.Clear();

            foreach (var job in unstartedJobs.Concat(ongoingJobs))
            {
                job.State = TranslationJobState.Failed;
                job.ErrorMessage = "Translation failed because all jobs were cleared.";
                Manager?.InvokeJobFailed(job);
            }
        }

        public void ClearTranslationCaches()
        {
            _translationCache.Clear();
            _failedTranslations.Clear();
        }
    }
}