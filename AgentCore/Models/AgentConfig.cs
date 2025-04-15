using System;

namespace AgentCore
{
    /// <summary>
    /// Configuration for the agent core
    /// </summary>
    public class AgentConfig
    {
        /// <summary>
        /// Unique identifier for this agent
        /// </summary>
        public string AgentId { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// URL to the central server
        /// </summary>
        public string ServerUrl { get; set; } = "https://localhost:5001";
        
        /// <summary>
        /// How often to send health checks, in minutes
        /// </summary>
        public int HealthCheckIntervalMinutes { get; set; } = 5;
        
        /// <summary>
        /// How often to send metrics, in seconds
        /// </summary>
        public int MetricsIntervalSeconds { get; set; } = 60;
        
        /// <summary>
        /// Whether vulnerability scanning is enabled
        /// </summary>
        public bool EnableVulnerabilityScans { get; set; } = true;
        
        /// <summary>
        /// How often to run vulnerability scans, in hours
        /// </summary>
        public int VulnerabilityScanIntervalHours { get; set; } = 24;
        
        /// <summary>
        /// Whether antivirus scanning is enabled
        /// </summary>
        public bool EnableAntivirusScans { get; set; } = true;
        
        /// <summary>
        /// How often to run antivirus scans, in hours
        /// </summary>
        public int AntivirusScanIntervalHours { get; set; } = 24;
        
        /// <summary>
        /// Whether patch management is enabled
        /// </summary>
        public bool EnablePatchManagement { get; set; } = true;
        
        /// <summary>
        /// How often to check for patches, in hours
        /// </summary>
        public int PatchCheckIntervalHours { get; set; } = 24;
        
        /// <summary>
        /// Whether remote shell functionality is enabled
        /// </summary>
        public bool EnableRemoteShell { get; set; } = true;
        
        /// <summary>
        /// Whether automatic patching for critical updates is enabled
        /// </summary>
        public bool EnableAutoPatch { get; set; } = false;
    }

    /// <summary>
    /// Configuration for the system monitor module
    /// </summary>
    public class SystemMonitorConfig
    {
        /// <summary>
        /// Monitoring interval in seconds
        /// </summary>
        public int MonitoringIntervalSeconds { get; set; } = 30;
        
        /// <summary>
        /// Whether to collect detailed process information
        /// </summary>
        public bool CollectProcessDetails { get; set; } = true;
        
        /// <summary>
        /// Whether to monitor network activity
        /// </summary>
        public bool MonitorNetwork { get; set; } = true;
        
        /// <summary>
        /// Whether to monitor disk I/O
        /// </summary>
        public bool MonitorDiskIO { get; set; } = true;
    }

    /// <summary>
    /// Configuration for the vulnerability scanner module
    /// </summary>
    public class VulnScannerConfig
    {
        /// <summary>
        /// Path to CVE database file or URL
        /// </summary>
        public string CVEDatabasePath { get; set; } = "Data/cve-database.json";
        
        /// <summary>
        /// Whether to scan network ports
        /// </summary>
        public bool ScanPorts { get; set; } = true;
        
        /// <summary>
        /// Whether to scan for configuration vulnerabilities
        /// </summary>
        public bool ScanConfigurations { get; set; } = true;
        
        /// <summary>
        /// Port range start for port scanning
        /// </summary>
        public int PortScanRangeStart { get; set; } = 1;
        
        /// <summary>
        /// Port range end for port scanning
        /// </summary>
        public int PortScanRangeEnd { get; set; } = 1024;
    }

    /// <summary>
    /// Configuration for the antivirus scanner module
    /// </summary>
    public class AVScannerConfig
    {
        /// <summary>
        /// Path to virus signature database file or URL
        /// </summary>
        public string SignatureDatabasePath { get; set; } = "Data/virus-signatures.db";
        
        /// <summary>
        /// Whether to use heuristic scanning
        /// </summary>
        public bool UseHeuristics { get; set; } = true;
        
        /// <summary>
        /// Whether to use cloud-based scanning
        /// </summary>
        public bool UseCloudScanning { get; set; } = false;
        
        /// <summary>
        /// Whether to scan archives (zip, rar, etc.)
        /// </summary>
        public bool ScanArchives { get; set; } = true;
        
        /// <summary>
        /// Maximum size of files to scan, in MB
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 100;
        
        /// <summary>
        /// Paths to exclude from scanning
        /// </summary>
        public string[] ExcludePaths { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Configuration for the patch manager module
    /// </summary>
    public class PatchManagerConfig
    {
        /// <summary>
        /// Whether to check for Windows updates
        /// </summary>
        public bool CheckWindowsUpdates { get; set; } = true;
        
        /// <summary>
        /// Whether to check for third-party software updates
        /// </summary>
        public bool CheckThirdPartyUpdates { get; set; } = true;
        
        /// <summary>
        /// Whether to install security updates automatically
        /// </summary>
        public bool AutoInstallSecurityUpdates { get; set; } = false;
        
        /// <summary>
        /// Whether to reboot automatically after installing updates that require it
        /// </summary>
        public bool AutoReboot { get; set; } = false;
        
        /// <summary>
        /// Reboot timeout in minutes, after which a forced reboot will occur
        /// </summary>
        public int RebootTimeoutMinutes { get; set; } = 30;
    }

    /// <summary>
    /// Configuration for the remote shell module
    /// </summary>
    public class RemoteShellConfig
    {
        /// <summary>
        /// Whether to require user consent for remote shell sessions
        /// </summary>
        public bool RequireUserConsent { get; set; } = true;
        
        /// <summary>
        /// Whether to allow PowerShell execution
        /// </summary>
        public bool AllowPowerShell { get; set; } = true;
        
        /// <summary>
        /// Whether to allow CMD execution
        /// </summary>
        public bool AllowCmd { get; set; } = true;
        
        /// <summary>
        /// Whether to allow Bash execution
        /// </summary>
        public bool AllowBash { get; set; } = true;
        
        /// <summary>
        /// Maximum idle time in minutes before disconnecting
        /// </summary>
        public int MaxIdleTimeMinutes { get; set; } = 10;
    }

    /// <summary>
    /// Agent health status
    /// </summary>
    public enum AgentStatus
    {
        Starting,
        Running,
        Stopping,
        Error,
        Maintenance
    }

    /// <summary>
    /// Agent health data to be sent to the server
    /// </summary>
    public class AgentHealthData
    {
        /// <summary>
        /// Agent unique identifier
        /// </summary>
        public string AgentId { get; set; }
        
        /// <summary>
        /// Timestamp of the health check
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Current agent status
        /// </summary>
        public AgentStatus Status { get; set; }
        
        /// <summary>
        /// Status of individual modules (true = running)
        /// </summary>
        public Dictionary<string, bool> ModuleStatuses { get; set; }
    }
}