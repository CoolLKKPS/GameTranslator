using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GameTranslator.Patches.Translatons
{
    public class TranslationManager
    {
        public event Action<TranslationJob> JobCompleted;
        public event Action<TranslationJob> JobFailed;

        private readonly List<IMonoBehaviour_Update> _updateCallbacks;
        private readonly List<TranslationEndpointManager> _endpointsWithUnstartedJobs;
        private readonly Timer _processingTimer;

        public TranslationManager()
        {
            _updateCallbacks = new List<IMonoBehaviour_Update>();
            _endpointsWithUnstartedJobs = new List<TranslationEndpointManager>();
            Endpoints = new List<TranslationEndpointManager>();
            AllEndpoints = new List<TranslationEndpointManager>();
            _processingTimer = new Timer(ProcessPendingJobs, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        }

        public List<TranslationEndpointManager> Endpoints { get; private set; }

        public List<TranslationEndpointManager> ConfiguredEndpoints => Endpoints.Where(e => e.Manager != null).ToList();

        public TranslationEndpointManager PrimaryEndpoint { get; set; }

        public TranslationEndpointManager FallbackEndpoint { get; set; }

        public TranslationEndpointManager PassthroughEndpoint { get; private set; }

        public int OngoingJobsCount { get; set; }

        public int UnstartedJobsCount { get; set; }

        public int OngoingTranslations { get; set; }

        public int UnstartedTranslations { get; set; }

        public List<TranslationEndpointManager> AllEndpoints { get; private set; }

        public TranslationEndpointManager CurrentEndpoint { get; set; }

        public bool IsFallbackAvailableFor(TranslationEndpointManager endpoint)
        {
            return endpoint != null && FallbackEndpoint != null
                && endpoint == CurrentEndpoint
                && FallbackEndpoint != endpoint;
        }

        public void InitializeEndpoints()
        {
            try
            {
                TranslatePlugin.logger.LogInfo("TranslationManager endpoints initialized.");
            }
            catch (Exception e)
            {
                TranslatePlugin.logger.LogError($"An error occurred while constructing endpoints: {e.Message}");
            }
        }

        public void CreateEndpoints(object httpSecurity)
        {
            TranslatePlugin.logger.LogInfo("Creating translation endpoints.");
        }

        private void AddEndpoint(TranslationEndpointManager translationEndpointManager)
        {
            translationEndpointManager.Manager = this;
            AllEndpoints.Add(translationEndpointManager);
            if (translationEndpointManager.Error == null)
            {
                ConfiguredEndpoints.Add(translationEndpointManager);
            }

            if (PrimaryEndpoint == null)
            {
                PrimaryEndpoint = translationEndpointManager;
            }
        }

        public void Update()
        {
            var len = _updateCallbacks.Count;
            for (int i = 0; i < len; i++)
            {
                try
                {
                    _updateCallbacks[i].Update();
                }
                catch (Exception e)
                {
                    TranslatePlugin.logger.LogError($"An error occurred while calling update on {_updateCallbacks[i].GetType().Name}: {e.Message}");
                }
            }
        }

        public void KickoffTranslations()
        {
            var endpoints = _endpointsWithUnstartedJobs;

            for (int i = endpoints.Count - 1; i >= 0; i--)
            {
                var endpoint = endpoints[i];

                while (endpoint.HasUnstartedJob)
                {
                    if (endpoint.IsBusy) break;

                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            await endpoint.HandleNextJob();
                        }
                        catch (Exception ex)
                        {
                            TranslatePlugin.logger.LogError($"Error processing translation job: {ex.Message}");
                        }
                    });
                }
            }
        }

        public void ScheduleUnstartedJobs(TranslationEndpointManager endpoint)
        {
            lock (_endpointsWithUnstartedJobs)
            {
                if (!_endpointsWithUnstartedJobs.Contains(endpoint))
                {
                    _endpointsWithUnstartedJobs.Add(endpoint);
                }
            }
        }

        public void UnscheduleUnstartedJobs(TranslationEndpointManager endpoint)
        {
            lock (_endpointsWithUnstartedJobs)
            {
                _endpointsWithUnstartedJobs.Remove(endpoint);
            }
        }

        public void OnJobStarted(TranslationEndpointManager endpoint)
        {
            UnstartedJobsCount++;
            OngoingJobsCount--;
        }

        public void OnJobCompleted(TranslationEndpointManager endpoint)
        {
            OngoingJobsCount--;
        }

        public void InvokeJobCompleted(TranslationJob job)
        {
            JobCompleted?.Invoke(job);
        }

        public void InvokeJobFailed(TranslationJob job)
        {
            JobFailed?.Invoke(job);
        }

        public void ClearAllJobs()
        {
            foreach (var endpoint in ConfiguredEndpoints)
            {
                endpoint.ClearAllJobs();
            }
        }

        public void RebootAllEndpoints()
        {
            foreach (var endpoint in ConfiguredEndpoints)
            {
                endpoint.ConsecutiveErrors = 0;
            }
        }

        public void RegisterEndpoint(TranslationEndpointManager translationEndpointManager)
        {
            translationEndpointManager.Manager = this;
            AllEndpoints.Add(translationEndpointManager);
            if (translationEndpointManager.Error == null)
            {
                ConfiguredEndpoints.Add(translationEndpointManager);
            }

            if (PrimaryEndpoint == null)
            {
                PrimaryEndpoint = translationEndpointManager;
            }
        }

        private void ProcessPendingJobs(object state)
        {
            KickoffTranslations();
        }

        public void Dispose()
        {
            _processingTimer?.Dispose();
        }
    }

    public interface IMonoBehaviour_Update
    {
        void Update();
    }
}