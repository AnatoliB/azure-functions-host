// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Scale;

namespace Microsoft.Azure.WebJobs.Script.Scale
{
    /// <summary>
    /// Interface defining methods for reading/writing metrics to a persistent store.
    /// </summary>
    public interface ITriggerMetricsRepository
    {
        /// <summary>
        /// Take a new metrics sample for each monitor and persist.
        /// </summary>
        /// <param name="monitors">The current collection of monitors.</param>
        /// <returns>A task.</returns>
        Task SampleAsync(IEnumerable<ITriggerScaleMonitor> monitors);

        /// <summary>
        /// Read all the metrics.
        /// </summary>
        /// <param name="monitors">The current collection of monitors.</param>
        /// <returns>Map of metrics per monitor.</returns>
        Task<IDictionary<ITriggerScaleMonitor, IList<TriggerMetrics>>> ReadAsync(IEnumerable<ITriggerScaleMonitor> monitors);
    }
}
