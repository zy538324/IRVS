using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace RemoteShellServer
{
    /// <summary>
    /// Provides system information for agent registration
    /// </summary>
    public static class SystemInfoProvider
    {
        /// <summary>
        /// Collects basic system information
        /// </summary>
        /// <returns>JSON string with system information</returns>
        public static string GetSystemInfo()
        {
            var systemInfo = new Dictionary<string, object>
            {
                { "Hostname", Dns.GetHostName() },
                { "OSVersion", RuntimeInformation.OSDescription },
                { "OSArchitecture", RuntimeInformation.OSArchitecture.ToString() },
                { "ProcessArchitecture", RuntimeInformation.ProcessArchitecture.ToString() },
                { "MachineName", Environment.MachineName },
                { "UserName", Environment.UserName },
                { "UserDomainName", Environment.UserDomainName },
                { "ProcessorCount", Environment.ProcessorCount },
                { "SystemDirectory", Environment.SystemDirectory },
                { "IPAddresses", GetLocalIPAddresses() },
                { "NetBIOSName", GetNetBIOSName() },
                { "LastBootTime", GetLastBootTime() }
            };

            try
            {
                // Include memory information
                var memoryInfo = GetMemoryInfo();
                if (memoryInfo != null)
                {
                    systemInfo["TotalMemory"] = memoryInfo.TotalPhysicalMemory;
                    systemInfo["AvailableMemory"] = memoryInfo.AvailablePhysicalMemory;
                }
            }
            catch (Exception ex)
            {
                systemInfo["MemoryError"] = ex.Message;
            }

            try
            {
                // Include disk information
                systemInfo["DiskInfo"] = GetDiskInfo();
            }
            catch (Exception ex)
            {
                systemInfo["DiskError"] = ex.Message;
            }

            return JsonConvert.SerializeObject(systemInfo);
        }

        /// <summary>
        /// Gets local IP addresses
        /// </summary>
        private static List<string> GetLocalIPAddresses()
        {
            var addresses = new List<string>();

            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(i => i.OperationalStatus == OperationalStatus.Up &&
                           (i.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                            i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211));

                foreach (var ni in networkInterfaces)
                {
                    var properties = ni.GetIPProperties();
                    var ipv4Addresses = properties.UnicastAddresses
                        .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Select(a => a.Address.ToString());

                    addresses.AddRange(ipv4Addresses);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting network interfaces: {ex.Message}");
            }

            return addresses;
        }

        /// <summary>
        /// Gets the NetBIOS name of the machine
        /// </summary>
        private static string GetNetBIOSName()
        {
            try
            {
                return Environment.MachineName;
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Gets memory information
        /// </summary>
        private static dynamic GetMemoryInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    using var computerSystem = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                    using var memoryStatus = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");

                    var totalMemory = computerSystem.Get().Cast<ManagementObject>().First();
                    var availableMemory = memoryStatus.Get().Cast<ManagementObject>().First();

                    return new 
                    { 
                        TotalPhysicalMemory = Convert.ToUInt64(totalMemory["TotalPhysicalMemory"]),
                        AvailablePhysicalMemory = Convert.ToUInt64(availableMemory["FreePhysicalMemory"]) * 1024 // Convert KB to bytes
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting memory info: {ex.Message}");
                    return null;
                }
            }
            else
            {
                return new { TotalPhysicalMemory = "Unknown", AvailablePhysicalMemory = "Unknown" };
            }
        }

        /// <summary>
        /// Gets disk information
        /// </summary>
        private static List<object> GetDiskInfo()
        {
            var disks = new List<object>();

            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                disks.Add(new
                {
                    Name = drive.Name,
                    Label = string.IsNullOrEmpty(drive.VolumeLabel) ? "Local Disk" : drive.VolumeLabel,
                    DriveType = drive.DriveType.ToString(),
                    FileSystem = drive.DriveFormat,
                    TotalSize = drive.TotalSize,
                    AvailableSpace = drive.AvailableFreeSpace
                });
            }

            return disks;
        }

        /// <summary>
        /// Gets the last boot time of the system
        /// </summary>
        private static string GetLastBootTime()
        {
            try
            {
                using var uptime = new PerformanceCounter("System", "System Up Time");
                uptime.NextValue(); // First call always returns 0
                var uptimeSeconds = uptime.NextValue();
                var currentTime = DateTime.Now;
                var lastBootTime = currentTime.AddSeconds(-uptimeSeconds);
                
                return lastBootTime.ToString("o");
            }
            catch (Exception)
            {
                return "Unknown";
            }
        }
    }
}