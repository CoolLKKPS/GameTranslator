using GameTranslator.Patches.Utils;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameTranslator.Patches.Translatons
{
    internal class TranslationEndpointManager(int maxConcurrency = 3, int maxRetries = 3, float translationDelay = 1.0f)
    {
        private readonly ConcurrentDictionary<string, TranslationJob> _unstartedJobs = new();
        private readonly ConcurrentDictionary<string, TranslationJob> _ongoingJobs = new();
        private readonly ConcurrentDictionary<string, byte> _failedTranslations = new();
        private readonly SemaphoreSlim _concurrencyLimiter = new(maxConcurrency, maxConcurrency);
        private readonly int _maxConcurrency = maxConcurrency;
        private readonly int _maxRetries = maxRetries;
        private readonly float _translationDelay = translationDelay;

        public bool IsBusy => _ongoingJobs.Count >= _maxConcurrency;

        public bool HasUnstartedJob => !_unstartedJobs.IsEmpty;

        public TranslationManager Manager { get; set; }

        public TranslationJob EnqueueTranslation(
            object ui,
            string key,
            object translationInfo,
            NormalTextTranslator normalText,
            TranslateConfig.TranslateConfigFile config,
            bool isTranslatable,
            bool allowFallback = true)
        {
            var jobKey = BuildKey(key, config, TranslationScopeHelper.GetScope(ui));

            if (_unstartedJobs.TryGetValue(jobKey, out var existingUnstartedJob))
            {
                existingUnstartedJob.Associate(ui, translationInfo, normalText, config);
                return null;
            }

            if (_ongoingJobs.TryGetValue(jobKey, out var existingOngoingJob))
            {
                existingOngoingJob.Associate(ui, translationInfo, normalText, config);
                return null;
            }

            var newJob = new TranslationJob(ui, key, true, isTranslatable)
            {
                Scope = TranslationScopeHelper.GetScope(ui)
            };
            newJob.Associate(ui, translationInfo, normalText, config);

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
            if (!_unstartedJobs.TryRemove(jobKey, out var job)) return;

            _ongoingJobs.TryAdd(jobKey, job);

            try
            {
                await _concurrencyLimiter.WaitAsync();
                await ProcessTranslationJob(job, jobKey);
            }
            finally
            {
                _concurrencyLimiter.Release();
                _ongoingJobs.TryRemove(jobKey, out _);

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
                if (!CanTranslate(job.OriginalText, job.Scope))
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
                    Manager?.InvokeJobCompleted(job);
                }
                else
                {
                    job.State = TranslationJobState.Succeeded;
                    job.TranslatedText = null;
                    Manager?.InvokeJobCompleted(job);
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
                    RegisterTranslationFailure(job.OriginalText, job.Scope);
                    Manager?.InvokeJobFailed(job);
                }
            }
        }

        private bool CanTranslate(string untranslatedText, int scope = -1)
        {
            if (_failedTranslations.TryGetValue($"{scope}:{untranslatedText}", out var count))
            {
                return count < 3;
            }

            if (string.IsNullOrEmpty(untranslatedText))
            {
                return false;
            }
            return true;
        }

        private void RegisterTranslationFailure(string untranslatedText, int scope = -1)
        {
            var failureKey = $"{scope}:{untranslatedText}";
            _failedTranslations.AddOrUpdate(failureKey, 1, (k, value) => (byte)(value + 1));
            try { TranslatePlugin.logger.LogWarning($"Translation failure registered for text: '{NormalTextTranslator.GetTextSnippet(untranslatedText, 50)}' (scope={scope}, Total failures: {_failedTranslations[failureKey]})"); }
            catch (IndexOutOfRangeException) { }
        }

        internal static string TranslateText(string text, NormalTextTranslator normalText, TranslateConfig.TranslateConfigFile config, int scope = -1)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string translatedText = text;

            try
            {
                if (normalText != null && TranslatePlugin.shouldTranslateNormalText.Value && normalText.IsTranslatable(text, false, scope))
                {
                    translatedText = normalText.TryTranslate(translatedText, scope);
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
            }
            catch (Exception ex)
            {
                try { TranslatePlugin.logger.LogError($"Translation error for text '{NormalTextTranslator.GetTextSnippet(text, 50)}': {ex.Message}"); }
                catch (IndexOutOfRangeException) { }
                return text;
            }

            return translatedText;
        }

        internal static string BuildKey(string text, TranslateConfig.TranslateConfigFile config, int scope = -1)
        {
            return $"{config?.ConfigFileName ?? "global"}:{scope}:{text}";
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

        // This can be simplified but i will keep it
        public void ClearEndpointManagerCaches()
        {
            _failedTranslations.Clear();
        }
    }
}