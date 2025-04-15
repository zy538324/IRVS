using System;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteShellServer
{
    /// <summary>
    /// Main service class for the Remote Shell functionality
    /// </summary>
    public class RemoteShellService : IDisposable
    {
        private RemoteShellConnection _connection;
        private readonly string _hubUrl;
        private readonly string _agentId;
        private bool _isRunning = false;
        private Timer _heartbeatTimer;
        private readonly object _lock = new object();
        private bool _disposed = false;

        /// <summary>
        /// Event raised when the connection status changes
        /// </summary>
        public event EventHandler<bool> ConnectionStatusChanged;

        /// <summary>
        /// Creates a new instance of the RemoteShellService
        /// </summary>
        /// <param name="hubUrl">The URL of the SignalR hub</param>
        /// <param name="agentId">The unique identifier for this agent</param>
        public RemoteShellService(string hubUrl, string agentId)
        {
            _hubUrl = hubUrl;
            _agentId = agentId;
        }

        /// <summary>
        /// Starts the Remote Shell service
        /// </summary>
        public async Task<bool> StartAsync()
        {
            if (_isRunning)
                return true;

            try
            {
                lock (_lock)
                {
                    if (_isRunning)
                        return true;

                    _connection = new RemoteShellConnection(_hubUrl, _agentId);
                    _connection.ConnectionStatusChanged += OnConnectionStatusChanged;
                }

                var connected = await _connection.ConnectAsync();
                if (connected)
                {
                    _isRunning = true;
                    StartHeartbeat();
                    Console.WriteLine("Remote Shell Service started successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to connect to Remote Shell hub.");
                }

                return connected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting Remote Shell Service: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stops the Remote Shell service
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning)
                return;

            try
            {
                lock (_lock)
                {
                    if (!_isRunning)
                        return;

                    _isRunning = false;
                    StopHeartbeat();
                }

                if (_connection != null)
                {
                    await _connection.DisconnectAsync();
                    _connection.ConnectionStatusChanged -= OnConnectionStatusChanged;
                    _connection.Dispose();
                    _connection = null;
                }

                Console.WriteLine("Remote Shell Service stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping Remote Shell Service: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles connection status changes
        /// </summary>
        private void OnConnectionStatusChanged(object sender, bool isConnected)
        {
            ConnectionStatusChanged?.Invoke(this, isConnected);
        }

        /// <summary>
        /// Starts the heartbeat timer to keep the connection alive
        /// </summary>
        private void StartHeartbeat()
        {
            _heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Stops the heartbeat timer
        /// </summary>
        private void StopHeartbeat()
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        /// <summary>
        /// Sends a heartbeat to the server
        /// </summary>
        private void SendHeartbeat(object state)
        {
            try
            {
                // In a real implementation, we would send a heartbeat to the server
                // using the hub connection
                Console.WriteLine("Heartbeat sent.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending heartbeat: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns whether the service is currently running
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Returns whether the service is currently connected to the hub
        /// </summary>
        public bool IsConnected => _connection?.IsConnected ?? false;

        /// <summary>
        /// Disposes the service resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                StopAsync().Wait();
                _heartbeatTimer?.Dispose();
            }

            _disposed = true;
        }
    }
}