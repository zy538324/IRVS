using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

namespace Sysguard.RemoteDesktop
{
    /// <summary>
    /// Delegate for receiving notifications from the native RemoteDesktopServer
    /// </summary>
    /// <param name="messageType">Type of message</param>
    /// <param name="messageContent">JSON-formatted message content</param>
    public delegate void RemoteDesktopServerCallback(string messageType, string messageContent);

    /// <summary>
    /// Wrapper for the native C++ RemoteDesktopServer
    /// </summary>
    public class RemoteDesktopManager : IDisposable
    {
        // P/Invoke declarations for the native C++ functions
        [DllImport("RemoteDesktopServer", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CreateRemoteDesktopServer();

        [DllImport("RemoteDesktopServer", CallingConvention = CallingConvention.Cdecl)]
        private static extern void DestroyRemoteDesktopServer(IntPtr server);

        [DllImport("RemoteDesktopServer", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool StartServer(IntPtr server, int port);

        [DllImport("RemoteDesktopServer", CallingConvention = CallingConvention.Cdecl)]
        private static extern void StopServer(IntPtr server);

        [DllImport("RemoteDesktopServer", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool IsServerRunning(IntPtr server);

        [DllImport("RemoteDesktopServer", CallingConvention = CallingConvention.Cdecl)]
        private static extern void RegisterCallback(IntPtr server, ManagedCallbackDelegate callback);

        [DllImport("RemoteDesktopServer", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ExecuteServerCommand(IntPtr server, string command, StringBuilder response, int responseSize);

        [DllImport("RemoteDesktopServer", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetServerInformation(IntPtr server);

        [DllImport("RemoteDesktopServer", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool StartIPCServer(IntPtr server, int port);

        [DllImport("RemoteDesktopServer", CallingConvention = CallingConvention.Cdecl)]
        private static extern void StopIPCServer(IntPtr server);

        [DllImport("RemoteDesktopServer", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetAgentIdentifier(IntPtr server, string agentId);

        // Delegate definition for native callback
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ManagedCallbackDelegate(string messageType, string messageContent);

        // Private members
        private IntPtr _nativeServer;
        private readonly ManagedCallbackDelegate _callbackDelegate;
        private event RemoteDesktopServerCallback _callback;
        private bool _disposed = false;
        private const int MaxResponseSize = 8192;

        /// <summary>
        /// Gets a value indicating whether the server is running.
        /// </summary>
        public bool IsRunning => _nativeServer != IntPtr.Zero && IsServerRunning(_nativeServer);

        /// <summary>
        /// Event raised when the server sends a notification
        /// </summary>
        public event RemoteDesktopServerCallback OnNotification
        {
            add
            {
                _callback += value;
                
                // If this is the first subscriber, register the native callback
                if (_nativeServer != IntPtr.Zero && _callback != null && _callback.GetInvocationList().Length == 1)
                {
                    RegisterCallback(_nativeServer, _callbackDelegate);
                }
            }
            remove
            {
                _callback -= value;
            }
        }

        /// <summary>
        /// Creates a new RemoteDesktopManager
        /// </summary>
        public RemoteDesktopManager()
        {
            _nativeServer = CreateRemoteDesktopServer();
            
            // Keep a reference to the delegate to prevent garbage collection
            _callbackDelegate = NativeCallback;
        }

        /// <summary>
        /// Starts the remote desktop server
        /// </summary>
        /// <param name="port">The port to listen on</param>
        /// <returns>True if the server was started successfully</returns>
        public bool Start(int port = 8900)
        {
            EnsureNotDisposed();
            if (_callback != null)
            {
                RegisterCallback(_nativeServer, _callbackDelegate);
            }
            return StartServer(_nativeServer, port);
        }

        /// <summary>
        /// Stops the remote desktop server
        /// </summary>
        public void Stop()
        {
            EnsureNotDisposed();
            StopServer(_nativeServer);
        }

        /// <summary>
        /// Starts the IPC server for local process communication
        /// </summary>
        /// <param name="port">The port to listen on</param>
        /// <returns>True if the IPC server was started successfully</returns>
        public bool StartIPC(int port = 8901)
        {
            EnsureNotDisposed();
            return StartIPCServer(_nativeServer, port);
        }

        /// <summary>
        /// Stops the IPC server
        /// </summary>
        public void StopIPC()
        {
            EnsureNotDisposed();
            StopIPCServer(_nativeServer);
        }

        /// <summary>
        /// Sets the agent identifier
        /// </summary>
        /// <param name="agentId">The agent identifier</param>
        public void SetAgentId(string agentId)
        {
            EnsureNotDisposed();
            SetAgentIdentifier(_nativeServer, agentId);
        }

        /// <summary>
        /// Gets information about the server
        /// </summary>
        /// <returns>A dictionary containing server information</returns>
        public Dictionary<string, object> GetServerInfo()
        {
            EnsureNotDisposed();
            
            string json = ExecuteCommand("status");
            if (string.IsNullOrEmpty(json))
            {
                return new Dictionary<string, object>();
            }
            
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            }
            catch (Exception)
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Executes a command on the server
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>The response from the server</returns>
        public string ExecuteCommand(string command)
        {
            EnsureNotDisposed();
            
            StringBuilder response = new StringBuilder(MaxResponseSize);
            if (ExecuteServerCommand(_nativeServer, command, response, MaxResponseSize))
            {
                return response.ToString();
            }
            
            return string.Empty;
        }

        /// <summary>
        /// Gets a list of active sessions
        /// </summary>
        /// <returns>A list of session identifiers</returns>
        public List<string> GetSessions()
        {
            string json = ExecuteCommand("list_sessions");
            try
            {
                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (result != null && result.ContainsKey("sessions"))
                {
                    var sessions = new List<string>();
                    foreach (var session in result["sessions"].EnumerateArray())
                    {
                        string id = session.GetProperty("id").GetString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            sessions.Add(id);
                        }
                    }
                    return sessions;
                }
            }
            catch (Exception)
            {
                // Ignore parsing errors
            }
            
            return new List<string>();
        }

        /// <summary>
        /// Disconnects a session
        /// </summary>
        /// <param name="sessionId">The session identifier to disconnect</param>
        /// <returns>True if the session was disconnected</returns>
        public bool DisconnectSession(string sessionId)
        {
            string json = ExecuteCommand($"disconnect_session:{sessionId}");
            try
            {
                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                return result != null && 
                       result.ContainsKey("success") && 
                       result["success"].GetBoolean();
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Sets the server configuration from a JSON string
        /// </summary>
        /// <param name="jsonConfig">The JSON configuration</param>
        /// <returns>True if the configuration was applied successfully</returns>
        public bool SetConfiguration(string jsonConfig)
        {
            string json = ExecuteCommand($"set_config:{jsonConfig}");
            try
            {
                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                return result != null && 
                       result.ContainsKey("success") && 
                       result["success"].GetBoolean();
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the current server configuration as a JSON string
        /// </summary>
        /// <returns>The server configuration as JSON</returns>
        public string GetConfiguration()
        {
            return ExecuteCommand("get_config");
        }

        /// <summary>
        /// Callback function that will be called from the native code
        /// </summary>
        private void NativeCallback(string messageType, string messageContent)
        {
            // Invoke the event on a thread pool thread to avoid blocking the native code
            Task.Run(() => _callback?.Invoke(messageType, messageContent));
        }

        /// <summary>
        /// Throws an exception if the object has been disposed
        /// </summary>
        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(RemoteDesktopManager));
            }
        }

        /// <summary>
        /// Disposes the RemoteDesktopManager
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the RemoteDesktopManager
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }

                // Clean up unmanaged resources
                if (_nativeServer != IntPtr.Zero)
                {
                    StopServer(_nativeServer);
                    StopIPCServer(_nativeServer);
                    DestroyRemoteDesktopServer(_nativeServer);
                    _nativeServer = IntPtr.Zero;
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~RemoteDesktopManager()
        {
            Dispose(false);
        }
    }
}