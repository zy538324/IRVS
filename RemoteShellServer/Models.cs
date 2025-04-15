using System;

namespace RemoteShellServer
{
    /// <summary>
    /// Represents the type of shell to use for command execution
    /// </summary>
    public enum ShellType
    {
        PowerShell,
        Cmd,
        Bash
    }

    /// <summary>
    /// Represents a command to be executed in a shell
    /// </summary>
    public class ShellCommand
    {
        /// <summary>
        /// The type of shell to execute the command in
        /// </summary>
        public ShellType ShellType { get; set; }
        
        /// <summary>
        /// The command text to be executed
        /// </summary>
        public string CommandText { get; set; }
        
        /// <summary>
        /// Indicates if the command requires elevated privileges
        /// </summary>
        public bool RequireElevation { get; set; }
        
        /// <summary>
        /// Timeout in milliseconds for command execution
        /// </summary>
        public int TimeoutMilliseconds { get; set; } = 30000; // Default 30 seconds
    }

    /// <summary>
    /// Represents the result of a shell command execution
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        /// Indicates if the command was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// The output of the command
        /// </summary>
        public string Output { get; set; }
        
        /// <summary>
        /// Any errors that occurred during command execution
        /// </summary>
        public string Error { get; set; }
        
        /// <summary>
        /// The exit code of the process
        /// </summary>
        public int ExitCode { get; set; }
        
        /// <summary>
        /// The time when the command started execution
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// The time when the command completed execution
        /// </summary>
        public DateTime EndTime { get; set; }
        
        /// <summary>
        /// The execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs => (long)(EndTime - StartTime).TotalMilliseconds;
    }
}