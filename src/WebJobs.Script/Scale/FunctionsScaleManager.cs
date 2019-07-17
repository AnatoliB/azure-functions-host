// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Scale;

namespace Microsoft.Azure.WebJobs.Script.Scale
{
    /// <summary>
    /// Layer over core scale provider interfaces, providing higher level services.
    /// </summary>
    public class FunctionsScaleManager
    {
        private readonly ITriggerScaleMonitorProvider _monitorProvider;
        private readonly ITriggerMetricsRepository _metricsRepository;

        public FunctionsScaleManager(ITriggerScaleMonitorProvider monitorProvider, ITriggerMetricsRepository metricsRepository)
        {
            _monitorProvider = monitorProvider;
            _metricsRepository = metricsRepository;
        }

        /// <summary>
        /// Get the current scale status (vote) by querying all active triggers for their
        /// scale status
        /// </summary>
        /// <param name="context">The context to use for the scale decision.</param>
        /// <returns>The scale vote.</returns>
        public async Task<ScaleVote> GetScaleStatusAsync(ScaleStatusContext context)
        {
            // get the collection of current metrics for each monitor
            var monitors = _monitorProvider.GetMonitors();
            var monitorMetrics = await _metricsRepository.ReadAsync(monitors);

            // for each monitor, ask it to return its scale status (vote) based on
            // the metrics and context info (e.g. worker count)
            List<ScaleVote> votes = new List<ScaleVote>();
            foreach (var pair in monitorMetrics)
            {
                var monitor = pair.Key;
                var metrics = pair.Value;

                context.Metrics = metrics;
                var result = monitor.GetScaleStatus(context);

                votes.Add(result.Vote);
            }

            // aggregate all the votes into a single vote
            if (votes.All(p => p == ScaleVote.ScaleOut))
            {
                return ScaleVote.ScaleOut;
            }
            else if (context.WorkerCount > 0 && votes.All(p => p == ScaleVote.ScaleIn))
            {
                return ScaleVote.ScaleIn;
            }

            return ScaleVote.None;
        }
    }
}
