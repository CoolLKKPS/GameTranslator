using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GameTranslator.Patches.Translatons
{
    internal class TranslationManager
    {
        public event Action<TranslationJob> JobCompleted;
        public event Action<TranslationJob> JobFailed;

        private readonly List<TranslationEndpointManager> _endpointsWithUnstartedJobs;
        private readonly Timer _processingTimer;

        public TranslationManager()
        {
            _endpointsWithUnstartedJobs = new List<TranslationEndpointManager>();
            Endpoints = new List<TranslationEndpointManager>();
            _processingTimer = new Timer(ProcessPendingJobs, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        }

        public List<TranslationEndpointManager> Endpoints { get; private set; }

        public List<TranslationEndpointManager> ConfiguredEndpoints => Endpoints.Where(e => e.Manager != null).ToList();

        public TranslationEndpointManager PrimaryEndpoint { get; set; }

        // Still using for other purposes
        public void Dispose()
        {
            _processingTimer?.Dispose();
        }

        public void KickoffTranslations()
        {
            List<TranslationEndpointManager> endpoints;
            lock (_endpointsWithUnstartedJobs)
            {
                endpoints = _endpointsWithUnstartedJobs.ToList();
            }

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

        public void RegisterEndpoint(TranslationEndpointManager translationEndpointManager)
        {
            translationEndpointManager.Manager = this;

            if (PrimaryEndpoint == null)
            {
                PrimaryEndpoint = translationEndpointManager;
            }
        }

        // This can be simplified but i will keep it
        private void ProcessPendingJobs(object state)
        {
            KickoffTranslations();
        }
    }
}