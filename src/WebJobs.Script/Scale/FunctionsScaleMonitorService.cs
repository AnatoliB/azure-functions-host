// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Script.Scale
{
    /// <summary>
    /// Service responsible for taking periodic trigger metrics samples and persisting them.
    /// </summary>
    public class FunctionsScaleMonitorService : IHostedService, IDisposable
    {
        private readonly IPrimaryHostStateProvider _primaryHostStateProvider;
        private readonly ITriggerScaleMonitorProvider _scaleMonitorProvider;
        private readonly ITriggerMetricsRepository _metricsRepository;
        private readonly IEnvironment _environment;
        private readonly ILogger _logger;
        private readonly Timer _timer;
        private readonly TimeSpan _interval;

        public FunctionsScaleMonitorService(ITriggerScaleMonitorProvider scaleMonitorProvider, ITriggerMetricsRepository metricsRepository, IPrimaryHostStateProvider primaryHostStateProvider, IEnvironment environment, ILoggerFactory loggerFactory)
        {
            _scaleMonitorProvider = scaleMonitorProvider;
            _metricsRepository = metricsRepository;
            _primaryHostStateProvider = primaryHostStateProvider;
            _environment = environment;
            _logger = loggerFactory.CreateLogger<FunctionsScaleMonitorService>();

            // TODO: make interval configurable via options
            _interval = TimeSpan.FromSeconds(10);
            _timer = new Timer(OnTimer, null, Timeout.Infinite, Timeout.Infinite);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // start the timer by setting the due time
            _timer.Change((int)_interval.TotalMilliseconds, Timeout.Infinite);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // stop the timer
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            return Task.CompletedTask;
        }

        private async void OnTimer(object state)
        {
            try
            {
                if (_primaryHostStateProvider.IsPrimary)
                {
                    var providers = _scaleMonitorProvider.GetMonitors();
                    await _metricsRepository.SampleAsync(providers);
                }

                var timer = _timer;
                if (timer != null)
                {
                    try
                    {
                        _timer.Change((int)_interval.TotalMilliseconds, Timeout.Infinite);
                    }
                    catch (ObjectDisposedException)
                    {
                        // might race with dispose
                    }
                }
            }
            catch (Exception exc) when (!exc.IsFatal())
            {
                _logger.LogError(exc, "Failed to collect/persist metrics sample.");
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
