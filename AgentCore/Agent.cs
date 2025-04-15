using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SystemMonitor;
using VulnScanner;
using AVScanner;
using PatchManager;
using RemoteShellServer;

namespace AgentCore
{
    public class Agent
    {
        private readonly ILogger<Agent> _logger;
        private readonly ISystemMonitorService _systemMonitorService;
        private readonly IVulnScannerService _vulnScannerService;
        private readonly IAVScannerService _avScannerService;
        private readonly IPatchManagerService _patchManagerService;
        private readonly RemoteShellService _remoteShellService;
        private readonly IConnectionManager _connectionManager;
        private readonly IScheduler _scheduler;
        private readonly AgentConfig _config;
        private bool _isRunning = false;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public Agent(
            ILogger<Agent> logger,
            IOptions<AgentConfig> config,
            ISystemMonitorService systemMonitorService,
            IVulnScannerService vulnScannerService,
            IAVScannerService avScannerService,
            IPatchManagerService patchManagerService,
            RemoteShellService remoteShellService,
            IConnectionManager connectionManager,
            IScheduler scheduler)
        {
            _logger = logger;
            _config = config.Value;
            _systemMonitorService = systemMonitorService;
            _vulnScannerService = vulnScannerService;
            _avScannerService = avScannerService;
            _patchManagerService = patchManagerService;
            _remoteShellService = remoteShellService;
            _connectionManager = connectionManager;
            _scheduler = scheduler;
        }

        /// <summary>
        /// Start the agent and all its components
        /// </summary>
        public async Task StartAsync()
        {
            if (_isRunning)
            {
                _logger.LogWarning("Agent is already running");
                return;
            }

            _logger.LogInformation("Agent starting with ID: {AgentId}", _config.AgentId);
            
            try
            {
                // Initialize the connection to the central server
                await _connectionManager.ConnectAsync(_config.ServerUrl, _config.AgentId);
                
                _logger.LogInformation("Agent connected to server at {ServerUrl}", _config.ServerUrl);
                
                // Start all modules
                await StartModulesAsync();
                
                // Register health check task
                _scheduler.ScheduleRecurringTask("HealthCheck", 
                    TimeSpan.FromMinutes(_config.HealthCheckIntervalMinutes), 
                    SendHealthCheckAsync);
                
                // Register system metrics collection task
                _scheduler.ScheduleRecurringTask("SystemMetrics", 
                    TimeSpan.FromSeconds(_config.MetricsIntervalSeconds), 
                    SendSystemMetricsAsync);

                // Schedule vulnerability scans
                if (_config.EnableVulnerabilityScans)
                {
                    _scheduler.ScheduleRecurringTask("VulnerabilityScan", 
                        TimeSpan.FromHours(_config.VulnerabilityScanIntervalHours), 
                        RunVulnerabilityScanAsync);
                }

                // Schedule AV scans
                if (_config.EnableAntivirusScans)
                {
                    _scheduler.ScheduleRecurringTask("AntivirusScan", 
                        TimeSpan.FromHours(_config.AntivirusScanIntervalHours), 
                        RunAntivirusScanAsync);
                }

                // Schedule patch checks
                if (_config.EnablePatchManagement)
                {
                    _scheduler.ScheduleRecurringTask("PatchCheck", 
                        TimeSpan.FromHours(_config.PatchCheckIntervalHours), 
                        CheckForPatchesAsync);
                }
                
                _isRunning = true;
                _logger.LogInformation("Agent successfully started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start agent");
                throw;
            }
        }
        
        /// <summary>
        /// Start all agent modules
        /// </summary>
        private async Task StartModulesAsync()
        {
            _logger.LogInformation("Starting agent modules...");
            
            var startupTasks = new List<Task>();
            
            // Start monitoring service
            startupTasks.Add(Task.Run(async () => {
                _logger.LogInformation("Starting System Monitor module");
                await _systemMonitorService.StartAsync();
                _logger.LogInformation("System Monitor module started");
            }));
            
            // Start vulnerability scanner if enabled
            if (_config.EnableVulnerabilityScans)
            {
                startupTasks.Add(Task.Run(async () => {
                    _logger.LogInformation("Starting Vulnerability Scanner module");
                    await _vulnScannerService.StartAsync();
                    _logger.LogInformation("Vulnerability Scanner module started");
                }));
            }
            
            // Start antivirus scanner if enabled
            if (_config.EnableAntivirusScans)
            {
                startupTasks.Add(Task.Run(async () => {
                    _logger.LogInformation("Starting Antivirus Scanner module");
                    await _avScannerService.StartAsync();
                    _logger.LogInformation("Antivirus Scanner module started");
                }));
            }
            
            // Start patch manager if enabled
            if (_config.EnablePatchManagement)
            {
                startupTasks.Add(Task.Run(async () => {
                    _logger.LogInformation("Starting Patch Manager module");
                    await _patchManagerService.StartAsync();
                    _logger.LogInformation("Patch Manager module started");
                }));
            }
            
            // Start remote shell service if enabled
            if (_config.EnableRemoteShell)
            {
                startupTasks.Add(Task.Run(async () => {
                    _logger.LogInformation("Starting Remote Shell module");
                    string remoteShellHubUrl = $"{_config.ServerUrl}/hubs/remoteshell";
                    bool success = await _remoteShellService.StartAsync();
                    if (success)
                    {
                        _logger.LogInformation("Remote Shell module started");
                    }
                    else
                    {
                        _logger.LogError("Failed to start Remote Shell module");
                    }
                }));
            }
            
            // Wait for all modules to start
            await Task.WhenAll(startupTasks);
            
            _logger.LogInformation("All agent modules started successfully");
        }
        
        /// <summary>
        /// Send health check to central server
        /// </summary>
        private async Task SendHealthCheckAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Sending health check to server");
                var healthData = new AgentHealthData
                {
                    AgentId = _config.AgentId,
                    Timestamp = DateTime.UtcNow,
                    Status = AgentStatus.Running,
                    ModuleStatuses = new Dictionary<string, bool>
                    {
                        { "SystemMonitor", _systemMonitorService.IsRunning },
                        { "VulnScanner", _config.EnableVulnerabilityScans && _vulnScannerService.IsRunning },
                        { "AVScanner", _config.EnableAntivirusScans && _avScannerService.IsRunning },
                        { "PatchManager", _config.EnablePatchManagement && _patchManagerService.IsRunning },
                        { "RemoteShell", _config.EnableRemoteShell && _remoteShellService.IsRunning }
                    }
                };
                
                await _connectionManager.SendHealthCheckAsync(healthData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send health check");
            }
        }
        
        /// <summary>
        /// Send system metrics to central server
        /// </summary>
        private async Task SendSystemMetricsAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Collecting system metrics");
                var metrics = await _systemMonitorService.GetSystemMetricsAsync();
                
                _logger.LogDebug("Sending system metrics to server");
                await _connectionManager.SendMetricsAsync(_config.AgentId, metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send system metrics");
            }
        }
        
        /// <summary>
        /// Run a vulnerability scan
        /// </summary>
        private async Task RunVulnerabilityScanAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting vulnerability scan");
                var scanResult = await _vulnScannerService.RunScanAsync(cancellationToken);
                
                _logger.LogInformation("Vulnerability scan completed. Found {VulnCount} vulnerabilities", 
                    scanResult.VulnerabilitiesFound);
                
                await _connectionManager.SendVulnerabilityScanResultAsync(_config.AgentId, scanResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run vulnerability scan");
            }
        }
        
        /// <summary>
        /// Run an antivirus scan
        /// </summary>
        private async Task RunAntivirusScanAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting antivirus scan");
                var scanResult = await _avScannerService.RunScanAsync(cancellationToken);
                
                _logger.LogInformation("Antivirus scan completed. Found {ThreatCount} threats", 
                    scanResult.ThreatsFound);
                
                await _connectionManager.SendAntivirusScanResultAsync(_config.AgentId, scanResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run antivirus scan");
            }
        }
        
        /// <summary>
        /// Check for available patches
        /// </summary>
        private async Task CheckForPatchesAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Checking for available patches");
                var patchResult = await _patchManagerService.CheckForPatchesAsync(cancellationToken);
                
                _logger.LogInformation("Patch check completed. Found {PatchCount} available patches", 
                    patchResult.AvailablePatches.Count);
                
                await _connectionManager.SendPatchCheckResultAsync(_config.AgentId, patchResult);
                
                // If auto-patching is enabled, install critical patches
                if (_config.EnableAutoPatch && patchResult.CriticalPatchesAvailable)
                {
                    await InstallCriticalPatchesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check for patches");
            }
        }
        
        /// <summary>
        /// Install critical security patches
        /// </summary>
        private async Task InstallCriticalPatchesAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Installing critical patches");
                var result = await _patchManagerService.InstallCriticalPatchesAsync(cancellationToken);
                
                _logger.LogInformation("Installed {PatchCount} critical patches", result.InstalledPatchCount);
                await _connectionManager.SendPatchInstallResultAsync(_config.AgentId, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to install critical patches");
            }
        }
        
        /// <summary>
        /// Shutdown the agent and all its components
        /// </summary>
        public async Task ShutdownAsync()
        {
            if (!_isRunning)
            {
                return;
            }

            _logger.LogInformation("Shutting down agent");
            
            try
            {
                // Signal cancellation to all tasks
                _cts.Cancel();
                
                // Stop all modules in reverse order
                if (_config.EnableRemoteShell)
                {
                    await _remoteShellService.StopAsync();
                }
                
                if (_config.EnablePatchManagement)
                {
                    await _patchManagerService.StopAsync();
                }
                
                if (_config.EnableAntivirusScans)
                {
                    await _avScannerService.StopAsync();
                }
                
                if (_config.EnableVulnerabilityScans)
                {
                    await _vulnScannerService.StopAsync();
                }
                
                await _systemMonitorService.StopAsync();
                
                // Stop the scheduler
                _scheduler.Stop();
                
                // Disconnect from the server
                await _connectionManager.DisconnectAsync();
                
                _isRunning = false;
                _logger.LogInformation("Agent shutdown completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during agent shutdown");
            }
        }

        /// <summary>
        /// Public method to initiate shutdown (for ctrl+c handler)
        /// </summary>
        public void Shutdown()
        {
            // Fire and forget - will be handled by the host
            _ = ShutdownAsync();
        }
    }
}