using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgentCore
{
    /// <summary>
    /// Background service that keeps the agent running
    /// </summary>
    public class AgentBackgroundService : BackgroundService
    {
        private readonly ILogger<AgentBackgroundService> _logger;
        private readonly Agent _agent;

        /// <summary>
        /// Constructor
        /// </summary>
        public AgentBackgroundService(ILogger<AgentBackgroundService> logger, Agent agent)
        {
            _logger = logger;
            _agent = agent;
        }

        /// <summary>
        /// Entry point for the background service
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Agent background service starting");
                
                // Register for cancellation
                stoppingToken.Register(() => 
                {
                    _logger.LogInformation("Agent background service stopping");
                    _ = _agent.ShutdownAsync();
                });
                
                // Start the agent
                await _agent.StartAsync();
                
                // Keep running until cancellation is requested
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(5000, stoppingToken); // Check every 5 seconds
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in agent background service");
                throw;
            }
        }
        
        /// <summary>
        /// Called when the service is stopping
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping agent background service");
            
            try
            {
                // Shutdown the agent
                await _agent.ShutdownAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shutting down agent");
            }
            
            await base.StopAsync(cancellationToken);
        }
    }
}