using System;
using System.Threading;
using System.Threading.Tasks;
using MediaArchiver;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ConsoleApp1
{
    public class TimerArchiveService : IHostedService
    {
        private readonly App _archiveServiceApp;
        private readonly ILogger _logger;
        private readonly TimeSpan _serviceInterval;
        private Timer _timer;
        private bool _isRunning;


        public TimerArchiveService(App archiveServiceApp, Serilog.ILogger logger, TimeSpan serviceInterval)
        {
            _archiveServiceApp = archiveServiceApp;
            _logger = logger;
            _serviceInterval = serviceInterval;
            _isRunning = false;
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Adding timer to restart every x minutes...");
            // timer repeats call to RemoveScheduledAccounts every 24 hours.
            _timer = new Timer(
                RunMediaArchiver,
                null,
                TimeSpan.Zero,
                _serviceInterval
            );
            return Task.CompletedTask;
        }

        private async void RunMediaArchiver(object? state)
        {
            if (!_isRunning)
            {
                _logger.Information("Starting App...");
                _isRunning = true;
                await _archiveServiceApp.Run();
                _isRunning = false;
                _logger.Information("App is done...");
            }

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}