using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

#if WINDOWS
using System.ServiceProcess;
using System.Net.NetworkInformation;
using System.Management;
using Microsoft.VisualBasic.Devices;
using System.Security.Principal;
using Microsoft.Win32;
#endif

namespace SystemMonitorLib
{
    /// <summary>
    /// Provides cross-platform system monitoring utilities for Windows, Linux, and macOS.
    /// </summary>
    public class SystemMonitor
    {
        // Platform detection
        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        private static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        // Simple cache for expensive shell commands (resource usage)
        private readonly Dictionary<string, (DateTime, string)> _shellCache = new();
        private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Runs a shell command safely and returns the output. Handles errors and logs exceptions.
        /// </summary>
        private string RunShell(string cmd)
        {
            try
            {
                if (_shellCache.TryGetValue(cmd, out var cache) && (DateTime.Now - cache.Item1) < _cacheDuration)
                    return cache.Item2;
                var psi = new ProcessStartInfo
                {
                    FileName = IsMacOS ? "/bin/zsh" : "/bin/bash",
                    Arguments = $"-c \"{cmd.Replace("\"", "\\\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (!string.IsNullOrWhiteSpace(error))
                        LogException(new Exception($"Shell error: {error.Trim()} (cmd: {cmd})"));
                    _shellCache[cmd] = (DateTime.Now, output);
                    return output;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets CPU usage percentage (cross-platform, async).
        /// </summary>
        public async Task<float> GetCpuUsageAsync(int intervalMs = 1000)
        {
            if (IsWindows)
            {
#if WINDOWS
                using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                {
                    cpuCounter.NextValue();
                    await Task.Delay(intervalMs);
                    return cpuCounter.NextValue();
                }
#else
                return 0f;
#endif
            }
            else if (IsLinux)
            {
                return await GetCpuUsageLinuxAsync(intervalMs);
            }
            else if (IsMacOS)
            {
                return await GetCpuUsageMacAsync(intervalMs);
            }
            return 0f;
        }

        private async Task<float> GetCpuUsageLinuxAsync(int intervalMs)
        {
            try
            {
                var cpu1 = File.ReadAllLines("/proc/stat").FirstOrDefault(l => l.StartsWith("cpu "));
                await Task.Delay(intervalMs);
                var cpu2 = File.ReadAllLines("/proc/stat").FirstOrDefault(l => l.StartsWith("cpu "));
                if (cpu1 == null || cpu2 == null) return 0f;
                var vals1 = cpu1.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(ulong.Parse).ToArray();
                var vals2 = cpu2.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(ulong.Parse).ToArray();
                ulong idle1 = vals1[3], idle2 = vals2[3];
                ulong total1 = vals1.Sum(), total2 = vals2.Sum();
                ulong totalDiff = total2 - total1;
                ulong idleDiff = idle2 - idle1;
                return totalDiff == 0 ? 0 : (float)(100.0 - (idleDiff * 100.0 / totalDiff));
            }
            catch (Exception ex) { LogException(ex); return 0f; }
        }

        private async Task<float> GetCpuUsageMacAsync(int intervalMs)
        {
            try
            {
                // Use 'top -l 2' and parse the last line
                await Task.Delay(intervalMs);
                var output = RunShell("top -l 2 | grep 'CPU usage' | tail -1");
                var parts = output.Split(new[] { ' ', '%', ':' }, StringSplitOptions.RemoveEmptyEntries);
                var userIdx = Array.IndexOf(parts, "user");
                var sysIdx = Array.IndexOf(parts, "sys");
                float user = userIdx > 0 ? float.Parse(parts[userIdx - 1]) : 0;
                float sys = sysIdx > 0 ? float.Parse(parts[sysIdx - 1]) : 0;
                return user + sys;
            }
            catch (Exception ex) { LogException(ex); return 0f; }
        }

        /// <summary>
        /// Gets RAM usage percentage (cross-platform, async).
        /// </summary>
        public async Task<float> GetRamUsageAsync()
        {
            if (IsWindows)
            {
#if WINDOWS
                var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                ulong total = computerInfo.TotalPhysicalMemory;
                ulong available = computerInfo.AvailablePhysicalMemory;
                return (float)(100.0 - ((double)available / total * 100.0));
#else
                return 0f;
#endif
            }
            else if (IsLinux)
            {
                try
                {
                    var lines = File.ReadAllLines("/proc/meminfo");
                    ulong total = ulong.Parse(lines.First(l => l.StartsWith("MemTotal")).Split(':')[1].Trim().Split(' ')[0]);
                    ulong free = ulong.Parse(lines.First(l => l.StartsWith("MemAvailable")).Split(':')[1].Trim().Split(' ')[0]);
                    return (float)(100.0 - ((double)free / total * 100.0));
                }
                catch (Exception ex) { LogException(ex); return 0f; }
            }
            else if (IsMacOS)
            {
                try
                {
                    var output = RunShell("vm_stat");
                    var lines = output.Split('\n');
                    ulong pageSize = 4096;
                    ulong free = 0, active = 0, inactive = 0, speculative = 0, wired = 0;
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("Pages free")) free = ulong.Parse(line.Split(':')[1].Trim().Split(' ')[0]);
                        if (line.StartsWith("Pages active")) active = ulong.Parse(line.Split(':')[1].Trim().Split(' ')[0]);
                        if (line.StartsWith("Pages inactive")) inactive = ulong.Parse(line.Split(':')[1].Trim().Split(' ')[0]);
                        if (line.StartsWith("Pages speculative")) speculative = ulong.Parse(line.Split(':')[1].Trim().Split(' ')[0]);
                        if (line.StartsWith("Pages wired down")) wired = ulong.Parse(line.Split(':')[1].Trim().Split(' ')[0]);
                    }
                    ulong total = free + active + inactive + speculative + wired;
                    ulong used = active + inactive + speculative + wired;
                    return (float)(100.0 * used / total);
                }
                catch (Exception ex) { LogException(ex); return 0f; }
            }
            return 0f;
        }

        /// <summary>
        /// Gets disk usage percentage for a given drive (cross-platform).
        /// </summary>
        public float GetDiskUsage(string drive = null)
        {
#if WINDOWS
            drive ??= "C";
            var driveInfo = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name.StartsWith(drive, StringComparison.OrdinalIgnoreCase));
            if (driveInfo == null) return 0f;
            double used = driveInfo.TotalSize - driveInfo.TotalFreeSpace;
            return (float)(used / driveInfo.TotalSize * 100.0);
#else
            drive ??= "/";
            var di = new DriveInfo(drive);
            if (!di.IsReady) return 0f;
            double used = di.TotalSize - di.TotalFreeSpace;
            return (float)(used / di.TotalSize * 100.0);
#endif
        }

        /// <summary>
        /// Gets all disks usage (cross-platform).
        /// </summary>
        public IEnumerable<string> GetAllDisksUsage()
        {
#if WINDOWS
            return DriveInfo.GetDrives().Where(d => d.IsReady)
                .Select(d =>
                {
                    double used = d.TotalSize - d.TotalFreeSpace;
                    float percent = (float)(used / d.TotalSize * 100.0);
                    return $"{d.Name}: {percent:F2}% used ({FormatBytes(used)} of {FormatBytes(d.TotalSize)})";
                });
#else
            var output = RunShell("df -h --output=source,pcent,used,size,target | tail -n +2");
            return output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l));
#endif
        }

        /// <summary>
        /// Gets running processes (cross-platform).
        /// </summary>
        public IEnumerable<string> GetRunningProcesses()
        {
#if WINDOWS
            return Process.GetProcesses().Select(p => $"{p.ProcessName} (PID: {p.Id})");
#else
            var output = RunShell("ps -eo pid,comm");
            return output.Split('\n').Skip(1).Where(l => !string.IsNullOrWhiteSpace(l));
#endif
        }

        /// <summary>
        /// Gets running processes with command line (cross-platform).
        /// </summary>
        public IEnumerable<string> GetRunningProcessesWithCommandLine()
        {
#if WINDOWS
            var result = new List<string>();
            var searcher = new ManagementObjectSearcher("SELECT ProcessId, Name, CommandLine FROM Win32_Process");
            foreach (var obj in searcher.Get())
                result.Add($"{obj["Name"]} (PID: {obj["ProcessId"]}) - {obj["CommandLine"]}");
            return result;
#else
            var output = RunShell("ps -eo pid,comm,args");
            return output.Split('\n').Skip(1).Where(l => !string.IsNullOrWhiteSpace(l));
#endif
        }

        /// <summary>
        /// Gets top processes by CPU usage (cross-platform).
        /// </summary>
        public IEnumerable<string> GetTopProcessesByCpu(int top = 5)
        {
#if WINDOWS
            // Windows implementation as before
            var processList = Process.GetProcesses();
            var cpuCounters = processList.Select(p =>
            {
                try
                {
                    return new { Process = p, Counter = new PerformanceCounter("Process", "% Processor Time", p.ProcessName, true) };
                }
                catch
                {
                    return null;
                }
            }).Where(x => x != null).ToList();

            System.Threading.Thread.Sleep(500);

            var usage = cpuCounters.Select(x =>
            {
                try
                {
                    return new { x.Process, Cpu = x.Counter.NextValue() / Environment.ProcessorCount };
                }
                catch
                {
                    return new { x.Process, Cpu = 0f };
                }
            }).OrderByDescending(x => x.Cpu).Take(top);

            foreach (var x in cpuCounters) x.Counter.Dispose();

            return usage.Select(x => $"{x.Process.ProcessName} (PID: {x.Process.Id}) - {x.Cpu:F2}% CPU");
#else
            var output = RunShell($"ps -eo pid,comm,%cpu --sort=-%cpu | head -n {top + 1}");
            return output.Split('\n').Skip(1).Where(l => !string.IsNullOrWhiteSpace(l));
#endif
        }

        /// <summary>
        /// Gets top processes by memory usage (cross-platform).
        /// </summary>
        public IEnumerable<string> GetTopProcessesByMemory(int top = 5)
        {
#if WINDOWS
            return Process.GetProcesses()
                .OrderByDescending(p => p.WorkingSet64)
                .Take(top)
                .Select(p => $"{p.ProcessName} (PID: {p.Id}) - {FormatBytes(p.WorkingSet64)} RAM");
#else
            var output = RunShell($"ps -eo pid,comm,%mem --sort=-%mem | head -n {top + 1}");
            return output.Split('\n').Skip(1).Where(l => !string.IsNullOrWhiteSpace(l));
#endif
        }

        /// <summary>
        /// Gets running services (cross-platform).
        /// </summary>
        public IEnumerable<string> GetRunningServices()
        {
            if (IsWindows)
            {
#if WINDOWS
                return ServiceController.GetServices()
                    .Where(s => s.Status == ServiceControllerStatus.Running)
                    .Select(s => $"{s.ServiceName} ({s.DisplayName})");
#else
                return new[] { "Not implemented on this platform." };
#endif
            }
            else if (IsLinux)
            {
                try { return RunShell("systemctl list-units --type=service --state=running").Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)); }
                catch (Exception ex) { LogException(ex); return new[] { "Error retrieving running services." }; }
            }
            else if (IsMacOS)
            {
                try { return RunShell("launchctl list").Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)); }
                catch (Exception ex) { LogException(ex); return new[] { "Error retrieving running services." }; }
            }
            return new[] { "Not implemented on this platform." };
        }

        /// <summary>
        /// Gets stopped services (cross-platform).
        /// </summary>
        public IEnumerable<string> GetStoppedServices()
        {
            if (IsWindows)
            {
#if WINDOWS
                return ServiceController.GetServices()
                    .Where(s => s.Status == ServiceControllerStatus.Stopped)
                    .Select(s => $"{s.ServiceName} ({s.DisplayName})");
#else
                return new[] { "Not implemented on this platform." };
#endif
            }
            else if (IsLinux)
            {
                try { return RunShell("systemctl list-units --type=service --state=exited").Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)); }
                catch (Exception ex) { LogException(ex); return new[] { "Error retrieving stopped services." }; }
            }
            else if (IsMacOS)
            {
                try { return RunShell("launchctl list").Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)); }
                catch (Exception ex) { LogException(ex); return new[] { "Error retrieving stopped services." }; }
            }
            return new[] { "Not implemented on this platform." };
        }

        /// <summary>
        /// Gets services by startup type (Windows only).
        /// </summary>
        public IEnumerable<string> GetServicesByStartupType(string startupType = "Automatic")
        {
            if (IsWindows)
            {
#if WINDOWS
                var services = new List<string>();
                var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Service WHERE StartMode='{startupType}'");
                foreach (var obj in searcher.Get())
                    services.Add($"{obj["Name"]} ({obj["DisplayName"]}) - {obj["State"]}");
                return services;
#else
                return new[] { "Not implemented on this platform." };
#endif
            }
            return new[] { "Not implemented on this platform." };
        }

        /// <summary>
        /// Gets logged in users (cross-platform).
        /// </summary>
        public IEnumerable<string> GetLoggedInUsers()
        {
#if WINDOWS
            var users = new HashSet<string>();
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LoggedOnUser");
            foreach (var obj in searcher.Get())
            {
                var user = (ManagementObject)obj["Antecedent"];
                string[] parts = user["Name"].ToString().Split('=');
                if (parts.Length > 1)
                    users.Add(parts[1].Trim('"'));
            }
            return users;
#else
            var output = RunShell("who");
            return output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l));
#endif
        }

        /// <summary>
        /// Gets system uptime (cross-platform).
        /// </summary>
        public TimeSpan GetSystemUptime()
        {
#if WINDOWS
            using (var uptime = new PerformanceCounter("System", "System Up Time"))
            {
                uptime.NextValue();
                return TimeSpan.FromSeconds(uptime.NextValue());
            }
#else
            try
            {
                var uptime = File.ReadAllText("/proc/uptime").Split(' ')[0];
                return TimeSpan.FromSeconds(double.Parse(uptime));
            }
            catch { return TimeSpan.Zero; }
#endif
        }

        /// <summary>
        /// Gets network adapters (cross-platform).
        /// </summary>
        public IEnumerable<string> GetNetworkAdapters()
        {
#if WINDOWS
            return NetworkInterface.GetAllNetworkInterfaces()
                .Select(nic => $"{nic.Name} ({nic.NetworkInterfaceType}) - {nic.OperationalStatus}");
#else
            var output = RunShell("ip link show");
            return output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l));
#endif
        }

        /// <summary>
        /// Gets network usage (cross-platform).
        /// </summary>
        public IEnumerable<string> GetNetworkUsage()
        {
#if WINDOWS
            var usages = new List<string>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up))
            {
                var stats = nic.GetIPv4Statistics();
                usages.Add($"{nic.Name}: Sent {FormatBytes(stats.BytesSent)}, Received {FormatBytes(stats.BytesReceived)}");
            }
            return usages;
#else
            var output = RunShell("cat /proc/net/dev");
            return output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l));
#endif
        }

        /// <summary>
        /// Gets active TCP connections (cross-platform).
        /// </summary>
        public IEnumerable<string> GetActiveTcpConnections()
        {
#if WINDOWS
            return IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpConnections()
                .Select(tc => $"{tc.LocalEndPoint} <-> {tc.RemoteEndPoint} ({tc.State})");
#else
            var output = RunShell("ss -t");
            return output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l));
#endif
        }

        /// <summary>
        /// Gets event logs (Windows only).
        /// </summary>
        public IEnumerable<string> GetEventLogs(int maxEntries = 10, string logName = "System")
        {
#if WINDOWS
            var logs = new List<string>();
            EventLog eventLog = new EventLog(logName);
            foreach (EventLogEntry entry in eventLog.Entries.Cast<EventLogEntry>().Reverse().Take(maxEntries))
                logs.Add($"[{entry.TimeGenerated}] {entry.EntryType}: {entry.Source} - {entry.Message}");
            return logs;
#else
            // Not available on Linux/macOS in the same way; stub
            return new[] { "Event log not available on this platform." };
#endif
        }

        /// <summary>
        /// Gets hardware info (cross-platform).
        /// </summary>
        public Dictionary<string, string> GetHardwareInfo()
        {
            var info = new Dictionary<string, string>();
#if WINDOWS
            // ... Windows implementation as before ...
#else
            // Example for Linux: CPU model, RAM, Disk, etc.
            info["CPU"] = RunShell("lscpu | grep 'Model name'").Split(':').LastOrDefault()?.Trim();
            info["RAM"] = RunShell("grep MemTotal /proc/meminfo").Split(':').LastOrDefault()?.Trim();
            info["Disk"] = RunShell("lsblk -d -o name,size").Replace("\n", "; ");
            info["Network"] = RunShell("ip link show").Replace("\n", "; ");
            return info;
#endif
        }

        /// <summary>
        /// Gets OS version (cross-platform).
        /// </summary>
        public string GetOsVersion()
        {
            return Environment.OSVersion.ToString();
        }

        /// <summary>
        /// Gets machine name (cross-platform).
        /// </summary>
        public string GetMachineName()
        {
            return Environment.MachineName;
        }

        /// <summary>
        /// Gets current user (cross-platform).
        /// </summary>
        public string GetCurrentUser()
        {
            if (IsWindows)
            {
#if WINDOWS
                return WindowsIdentity.GetCurrent().Name;
#else
                return Environment.UserName;
#endif
            }
            return Environment.UserName;
        }

        /// <summary>
        /// Gets installed software (cross-platform).
        /// </summary>
        public IEnumerable<string> GetInstalledSoftware()
        {
#if WINDOWS
            var software = new List<string>();
            using (var searcher = new ManagementObjectSearcher(@"SELECT * FROM Win32_Product"))
            {
                foreach (var obj in searcher.Get())
                    software.Add($"{obj["Name"]} {obj["Version"]}");
            }
            return software;
#else
            // Linux: dpkg or rpm, macOS: brew or system_profiler
            var output = RunShell("which dpkg && dpkg -l || which rpm && rpm -qa || echo 'Not implemented'");
            return output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l));
#endif
        }

        /// <summary>
        /// Gets startup programs (cross-platform).
        /// </summary>
        public IEnumerable<string> GetStartupPrograms()
        {
#if WINDOWS
            var startup = new List<string>();
            string key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            using (var rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(key))
            {
                if (rk != null)
                {
                    foreach (var name in rk.GetValueNames())
                        startup.Add($"{name}: {rk.GetValue(name)}");
                }
            }
            return startup;
#else
            // Linux: ~/.config/autostart, /etc/rc.local, systemd user services
            var output = RunShell("ls ~/.config/autostart 2>/dev/null; cat /etc/rc.local 2>/dev/null");
            return output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l));
#endif
        }

        /// <summary>
        /// Gets scheduled tasks (cross-platform).
        /// </summary>
        public IEnumerable<string> GetScheduledTasks()
        {
#if WINDOWS
            var tasks = new List<string>();
            using (var searcher = new ManagementObjectSearcher(@"SELECT * FROM Win32_ScheduledJob"))
            {
                foreach (var obj in searcher.Get())
                    tasks.Add($"{obj["Name"]} - {obj["Command"]}");
            }
            return tasks;
#else
            // Linux: cron jobs
            var output = RunShell("crontab -l 2>/dev/null");
            return output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l));
#endif
        }

        /// <summary>
        /// Gets Windows update status (Windows only).
        /// </summary>
        public string GetWindowsUpdateStatus()
        {
#if WINDOWS
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_QuickFixEngineering"))
            {
                var updates = searcher.Get().Cast<ManagementObject>().Select(mo => mo["HotFixID"]?.ToString());
                return $"Installed Updates: {string.Join(", ", updates)}";
            }
#else
            // Linux: apt or yum history, macOS: softwareupdate
            var output = RunShell("which apt && apt list --installed || which yum && yum list installed || echo 'Not implemented'");
            return output;
#endif
        }

        private string FormatBytes(double bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes = bytes / 1024;
            }
            return $"{bytes:0.##} {sizes[order]}";
        }

        private void LogException(Exception ex)
        {
            try
            {
                File.AppendAllText("SystemMonitorErrors.log", $"{DateTime.Now}: {ex}\n");
            }
            catch { }
        }
    }
}