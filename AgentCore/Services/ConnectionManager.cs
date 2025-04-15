using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SystemMonitor;
using VulnScanner;
using AVScanner;
using PatchManager;

namespace AgentCore
{
    /// <summary>
    /// Interface for the connection manager
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Connect to the central server
        /// </summary>
        Task<bool> ConnectAsync(string serverUrl, string agentId);
        
        /// <summary>
        /// Disconnect from the central server
        /// </summary>
        Task DisconnectAsync();
        
        /// <summary>
        /// Send health check data to the server
        /// </summary>
        Task SendHealthCheckAsync(AgentHealthData healthData);
        
        /// <summary>
        /// Send system metrics to the server
        /// </summary>
        Task SendMetricsAsync(string agentId, object metrics);
        
        /// <summary>
        /// Send vulnerability scan results to the server
        /// </summary>
        Task SendVulnerabilityScanResultAsync(string agentId, object scanResult);
        
        /// <summary>
        /// Send antivirus scan results to the server
        /// </summary>
        Task SendAntivirusScanResultAsync(string agentId, object scanResult);
        
        /// <summary>
        /// Send patch check results to the server
        /// </summary>
        Task SendPatchCheckResultAsync(string agentId, object patchResult);
        
        /// <summary>
        /// Send patch installation results to the server
        /// </summary>
        Task SendPatchInstallResultAsync(string agentId, object installResult);
        
        /// <summary>
        /// Returns true if connected to the server
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// Event raised when new commands are received from the server
        /// </summary>
        event EventHandler<CommandEventArgs> CommandReceived;
    }

    /// <summary>
    /// Implements communication with the central server
    /// </summary>
    public class ConnectionManager : IConnectionManager, IDisposable
    {
        private readonly ILogger<ConnectionManager> _logger;
        private readonly HttpClient _httpClient;
        private HubConnection _hubConnection;
        private string _serverUrl;
        private string _agentId;
        private bool _isConnected = false;
        private bool _disposed = false;

        public event EventHandler<CommandEventArgs> CommandReceived;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public ConnectionManager(ILogger<ConnectionManager> logger)
        {
            _logger = logger;
            
            // Configure HttpClient
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SysGuard-Agent");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }
        
        /// <summary>
        /// Connect to the central server
        /// </summary>
        public async Task<bool> ConnectAsync(string serverUrl, string agentId)
        {
            _serverUrl = serverUrl;
            _agentId = agentId;
            
            try
            {
                // Configure the SignalR hub connection
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{_serverUrl}/hubs/agent")
                    .WithAutomaticReconnect()
                    .Build();
                
                // Register handlers for SignalR messages
                RegisterHubHandlers();
                
                // Start the hub connection
                await _hubConnection.StartAsync();
                _isConnected = true;
                
                // Register this agent with the hub
                await _hubConnection.InvokeAsync("RegisterAgent", _agentId, Environment.MachineName);
                
                _logger.LogInformation("Connected to server at {ServerUrl}", _serverUrl);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to server at {ServerUrl}", _serverUrl);
                _isConnected = false;
                return false;
            }
        }
        
        /// <summary>
        /// Register handlers for SignalR messages
        /// </summary>
        private void RegisterHubHandlers()
        {
            // Handle incoming commands from the server
            _hubConnection.On<string>("ExecuteCommand", (commandJson) => 
            {
                try
                {
                    var command = JsonSerializer.Deserialize<AgentCommand>(commandJson);
                    _logger.LogInformation("Received command: {CommandType}", command.CommandType);
                    
                    // Raise event to notify subscribers
                    CommandReceived?.Invoke(this, new CommandEventArgs(command));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling command from server");
                }
                
                return Task.CompletedTask;
            });

            // Handle connection status changes
            _hubConnection.Reconnecting += error =>
            {
                _isConnected = false;
                _logger.LogWarning("Connection lost: {ErrorMessage}. Attempting to reconnect...", error?.Message);
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                _isConnected = true;
                _logger.LogInformation("Connection reestablished with ID: {ConnectionId}", connectionId);
                return _hubConnection.InvokeAsync("RegisterAgent", _agentId, Environment.MachineName);
            };

            _hubConnection.Closed += error =>
            {
                _isConnected = false;
                _logger.LogWarning("Connection closed: {ErrorMessage}", error?.Message);
                return Task.CompletedTask;
            };
        }
        
        /// <summary>
        /// Disconnect from the central server
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                try
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disconnecting from hub");
                }
                finally
                {
                    _isConnected = false;
                }
            }
        }
        
        /// <summary>
        /// Send health check data to the server
        /// </summary>
        public async Task SendHealthCheckAsync(AgentHealthData healthData)
        {
            if (!_isConnected)
            {
                _logger.LogWarning("Not connected to server. Cannot send health check.");
                return;
            }
            
            try
            {
                await _hubConnection.InvokeAsync("SubmitHealthCheck", JsonSerializer.Serialize(healthData));
                _logger.LogDebug("Health check sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send health check");
            }
        }
        
        /// <summary>
        /// Send system metrics to the server
        /// </summary>
        public async Task SendMetricsAsync(string agentId, object metrics)
        {
            if (!_isConnected)
            {
                _logger.LogWarning("Not connected to server. Cannot send metrics.");
                return;
            }
            
            try
            {
                var payload = new 
                {
                    AgentId = agentId,
                    Timestamp = DateTime.UtcNow,
                    Metrics = metrics
                };
                
                await _hubConnection.InvokeAsync("SubmitMetrics", JsonSerializer.Serialize(payload));
                _logger.LogDebug("Metrics sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send metrics");
            }
        }
        
        /// <summary>
        /// Send vulnerability scan results to the server
        /// </summary>
        public async Task SendVulnerabilityScanResultAsync(string agentId, object scanResult)
        {
            await SendResultAsync("SubmitVulnScanResult", agentId, scanResult, "vulnerability scan results");
        }
        
        /// <summary>
        /// Send antivirus scan results to the server
        /// </summary>
        public async Task SendAntivirusScanResultAsync(string agentId, object scanResult)
        {
            await SendResultAsync("SubmitAVScanResult", agentId, scanResult, "antivirus scan results");
        }
        
        /// <summary>
        /// Send patch check results to the server
        /// </summary>
        public async Task SendPatchCheckResultAsync(string agentId, object patchResult)
        {
            await SendResultAsync("SubmitPatchCheckResult", agentId, patchResult, "patch check results");
        }
        
        /// <summary>
        /// Send patch installation results to the server
        /// </summary>
        public async Task SendPatchInstallResultAsync(string agentId, object installResult)
        {
            await SendResultAsync("SubmitPatchInstallResult", agentId, installResult, "patch installation results");
        }
        
        /// <summary>
        /// Generic method to send results to the server
        /// </summary>
        private async Task SendResultAsync(string method, string agentId, object result, string resultType)
        {
            if (!_isConnected)
            {
                _logger.LogWarning("Not connected to server. Cannot send {ResultType}.", resultType);
                return;
            }
            
            try
            {
                var payload = new 
                {
                    AgentId = agentId,
                    Timestamp = DateTime.UtcNow,
                    Result = result
                };
                
                await _hubConnection.InvokeAsync(method, JsonSerializer.Serialize(payload));
                _logger.LogDebug("{ResultType} sent successfully", resultType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send {ResultType}", resultType);
            }
        }
        
        /// <summary>
        /// Returns true if connected to the server
        /// </summary>
        public bool IsConnected => _isConnected;
        
        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            
            if (disposing)
            {
                DisconnectAsync().Wait();
                _httpClient.Dispose();
            }
            
            _disposed = true;
        }
    }

    /// <summary>
    /// Event args for command received events
    /// </summary>
    public class CommandEventArgs : EventArgs
    {
        public AgentCommand Command { get; }
        
        public CommandEventArgs(AgentCommand command)
        {
            Command = command;
        }
    }

    /// <summary>
    /// Command sent from server to agent
    /// </summary>
    public class AgentCommand
    {
        /// <summary>
        /// Type of command
        /// </summary>
        public string CommandType { get; set; }
        
        /// <summary>
        /// Command parameters as JSON
        /// </summary>
        public string Parameters { get; set; }
        
        /// <summary>
        /// Command ID for correlation
        /// </summary>
        public string CommandId { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// When the command was created
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}