using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

namespace RemoteShellServer
{
    /// <summary>
    /// Handles the connection between the agent and central server for remote shell operations
    /// </summary>
    public class RemoteShellConnection : IDisposable
    {
        private readonly HubConnection _hubConnection;
        private readonly string _agentId;
        private readonly RemoteShellExecutor _shellExecutor;
        private bool _isConnected = false;
        private CancellationTokenSource _connectionCts;

        /// <summary>
        /// Event raised when the connection status changes
        /// </summary>
        public event EventHandler<bool> ConnectionStatusChanged;

        /// <summary>
        /// Creates a new RemoteShellConnection
        /// </summary>
        /// <param name="hubUrl">The URL of the SignalR hub</param>
        /// <param name="agentId">The unique identifier for this agent</param>
        public RemoteShellConnection(string hubUrl, string agentId)
        {
            _agentId = agentId;
            _shellExecutor = new RemoteShellExecutor();

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            // Register handlers for incoming SignalR messages
            _hubConnection.On<string, string>("ExecuteCommand", HandleExecuteCommand);
            _hubConnection.On<string>("TerminateSession", HandleTerminateSession);

            // Handle reconnection events
            _hubConnection.Reconnecting += error =>
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, _isConnected);
                Console.WriteLine($"Connection lost: {error.Message}. Attempting to reconnect...");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                _isConnected = true;
                ConnectionStatusChanged?.Invoke(this, _isConnected);
                Console.WriteLine($"Connection reestablished. Connected with ID: {connectionId}");
                return RegisterAgent();
            };

            _hubConnection.Closed += error =>
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, _isConnected);
                Console.WriteLine($"Connection closed: {error?.Message}");
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Starts the connection to the SignalR hub
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                _connectionCts = new CancellationTokenSource();
                await _hubConnection.StartAsync(_connectionCts.Token);
                _isConnected = true;
                ConnectionStatusChanged?.Invoke(this, _isConnected);

                // Register this agent with the hub
                await RegisterAgent();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to hub: {ex.Message}");
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, _isConnected);
                return false;
            }
        }

        /// <summary>
        /// Registers this agent with the central server
        /// </summary>
        private async Task RegisterAgent()
        {
            try
            {
                var systemInfo = SystemInfoProvider.GetSystemInfo();
                await _hubConnection.InvokeAsync("RegisterAgent", _agentId, systemInfo);
                Console.WriteLine($"Agent registered with ID: {_agentId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering agent: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the ExecuteCommand message from the hub
        /// </summary>
        private async Task HandleExecuteCommand(string sessionId, string commandJson)
        {
            try
            {
                var command = JsonConvert.DeserializeObject<ShellCommand>(commandJson);
                Console.WriteLine($"Received command for execution: {command.CommandText} (Type: {command.ShellType})");

                // Execute the command and send the result back
                var result = await _shellExecutor.ExecuteCommandAsync(command.ShellType, command.CommandText);
                await SendCommandResult(sessionId, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command: {ex.Message}");
                await SendCommandResult(sessionId, $"Error executing command: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the TerminateSession message from the hub
        /// </summary>
        private Task HandleTerminateSession(string sessionId)
        {
            Console.WriteLine($"Terminating session: {sessionId}");
            _shellExecutor.TerminateSession(sessionId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Sends the command result back to the hub
        /// </summary>
        private async Task SendCommandResult(string sessionId, string result)
        {
            try
            {
                await _hubConnection.InvokeAsync("CommandResult", _agentId, sessionId, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending command result: {ex.Message}");
            }
        }

        /// <summary>
        /// Disconnects from the hub
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_isConnected)
            {
                try
                {
                    _connectionCts?.Cancel();
                    await _hubConnection.StopAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disconnecting: {ex.Message}");
                }
                finally
                {
                    _isConnected = false;
                    ConnectionStatusChanged?.Invoke(this, _isConnected);
                }
            }
        }

        /// <summary>
        /// Disposes the connection
        /// </summary>
        public void Dispose()
        {
            DisconnectAsync().Wait();
            _connectionCts?.Dispose();
            _shellExecutor?.Dispose();
        }

        /// <summary>
        /// Gets the connection status
        /// </summary>
        public bool IsConnected => _isConnected;
    }
}