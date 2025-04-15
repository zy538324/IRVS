using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteShellServer
{
    /// <summary>
    /// Handles execution of commands in different shell environments
    /// </summary>
    public class RemoteShellExecutor : IDisposable
    {
        private readonly ConcurrentDictionary<string, Process> _activeSessions = new ConcurrentDictionary<string, Process>();
        private bool _disposed = false;

        /// <summary>
        /// Executes a command in the specified shell type
        /// </summary>
        /// <param name="shellType">Type of shell (PowerShell, CMD, Bash)</param>
        /// <param name="command">Command to execute</param>
        /// <returns>The output of the command execution</returns>
        public async Task<string> ExecuteCommandAsync(ShellType shellType, string command)
        {
            // Generate a unique session ID if we want to maintain state between commands
            var sessionId = Guid.NewGuid().ToString();
            
            try
            {
                using var process = CreateProcessForShell(shellType);
                _activeSessions.TryAdd(sessionId, process);
                
                // Start the process
                process.Start();
                
                // Write the command to the process
                await process.StandardInput.WriteLineAsync(command);
                await process.StandardInput.FlushAsync();
                
                // For non-persistent sessions, close the input stream to signal we're done
                if (!IsPersistentShell(shellType))
                {
                    process.StandardInput.Close();
                }

                // Read the output
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                // Wait for the process to exit if it's not a persistent shell
                if (!IsPersistentShell(shellType))
                {
                    await Task.Run(() => process.WaitForExit(5000));
                }
                
                // Remove the session
                _activeSessions.TryRemove(sessionId, out _);
                
                // Combine output and error
                return string.IsNullOrEmpty(error) ? output : output + Environment.NewLine + "ERROR: " + error;
            }
            catch (Exception ex)
            {
                _activeSessions.TryRemove(sessionId, out _);
                return $"Error executing command: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Creates a process for the specified shell type
        /// </summary>
        private Process CreateProcessForShell(ShellType shellType)
        {
            var processStartInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            
            switch (shellType)
            {
                case ShellType.PowerShell:
                    processStartInfo.FileName = "powershell.exe";
                    processStartInfo.Arguments = "-NoProfile -ExecutionPolicy Bypass -Command -";
                    break;
                case ShellType.Cmd:
                    processStartInfo.FileName = "cmd.exe";
                    processStartInfo.Arguments = "/c";
                    break;
                case ShellType.Bash:
                    // Check if WSL or Git Bash is available
                    if (File.Exists("/bin/bash") || File.Exists("C:\\Program Files\\Git\\bin\\bash.exe"))
                    {
                        processStartInfo.FileName = File.Exists("/bin/bash") ? "/bin/bash" : "C:\\Program Files\\Git\\bin\\bash.exe";
                    }
                    else if (File.Exists("C:\\Windows\\System32\\wsl.exe"))
                    {
                        processStartInfo.FileName = "wsl.exe";
                    }
                    else
                    {
                        throw new NotSupportedException("Bash shell is not supported on this system");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shellType), shellType, "Unsupported shell type");
            }
            
            return new Process { StartInfo = processStartInfo };
        }
        
        /// <summary>
        /// Determines if the shell type supports persistent sessions
        /// </summary>
        private bool IsPersistentShell(ShellType shellType)
        {
            // For this implementation, we're treating all shells as non-persistent
            // In a more complete implementation, we might maintain shell state between commands
            return false;
        }
        
        /// <summary>
        /// Terminates a specific shell session
        /// </summary>
        public void TerminateSession(string sessionId)
        {
            if (_activeSessions.TryRemove(sessionId, out var process))
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                    process.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error terminating session {sessionId}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Disposes all active sessions
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
                foreach (var session in _activeSessions)
                {
                    try
                    {
                        if (!session.Value.HasExited)
                        {
                            session.Value.Kill();
                        }
                        session.Value.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error disposing session {session.Key}: {ex.Message}");
                    }
                }
                
                _activeSessions.Clear();
            }
            
            _disposed = true;
        }
    }
}