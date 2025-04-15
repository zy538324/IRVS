using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuGet.Versioning;

namespace VulnScannerLib
{
    public class VulnScanner
    {
        private List<CveEntry> _cveDatabase;
        private string _cvePath;

        public VulnScanner(string cveJsonPath = "data/cve.json")
        {
            _cvePath = cveJsonPath;
            LoadCveDatabase();
        }

        private void LoadCveDatabase()
        {
            if (!File.Exists(_cvePath))
                throw new FileNotFoundException($"CVE database not found at {_cvePath}");
            var json = File.ReadAllText(_cvePath);
            _cveDatabase = JsonConvert.DeserializeObject<List<CveEntry>>(json) ?? new List<CveEntry>();
        }

        public IEnumerable<CveEntry> ScanSoftware(IEnumerable<SoftwareInventoryItem> softwareInventory)
        {
            var vulns = new List<CveEntry>();
            foreach (var sw in softwareInventory)
            {
                vulns.AddRange(_cveDatabase.Where(cve =>
                    (cve.Product.Equals(sw.Product, StringComparison.OrdinalIgnoreCase) ||
                     (!string.IsNullOrEmpty(cve.Cpe) && sw.Cpe == cve.Cpe))
                    && VersionRangeMatch(sw.Version, cve.AffectedVersions)));
            }
            return vulns;
        }

        // Multi-threaded software scan for large inventories
        public IEnumerable<CveEntry> ScanSoftwareParallel(IEnumerable<SoftwareInventoryItem> softwareInventory, int maxDegreeOfParallelism = 8)
        {
            var vulns = new List<CveEntry>();
            var inventoryList = softwareInventory.ToList();
            System.Threading.Tasks.Parallel.ForEach(inventoryList, new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, sw =>
            {
                var matches = _cveDatabase.Where(cve =>
                    (cve.Product.Equals(sw.Product, StringComparison.OrdinalIgnoreCase) ||
                     (!string.IsNullOrEmpty(cve.Cpe) && sw.Cpe == cve.Cpe))
                    && VersionRangeMatch(sw.Version, cve.AffectedVersions)).ToList();
                lock (vulns) vulns.AddRange(matches);
            });
            return vulns;
        }

        private bool VersionRangeMatch(string version, List<string> affectedVersions)
        {
            // Use NuGet.Versioning for semantic version range support
            if (string.IsNullOrEmpty(version) || affectedVersions == null) return false;
            foreach (var range in affectedVersions)
            {
                try
                {
                    if (VersionRange.TryParse(range, out var verRange) && SemanticVersion.TryParse(version, out var semVer))
                    {
                        if (verRange.Satisfies(semVer))
                            return true;
                    }
                    else if (version.StartsWith(range)) // fallback simple match
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        // Configuration scanning stub
        public IEnumerable<ConfigFinding> ScanConfiguration(IEnumerable<ConfigCheck> configChecks)
        {
            var findings = new List<ConfigFinding>();
            foreach (var check in configChecks)
            {
                try
                {
                    string output = RunConfigCommand(check.Command);
                    bool isVuln = !output.Trim().Equals(check.ExpectedValue.Trim(), StringComparison.OrdinalIgnoreCase);
                    findings.Add(new ConfigFinding { Check = check, IsVulnerable = isVuln, Details = output });
                }
                catch (Exception ex)
                {
                    findings.Add(new ConfigFinding { Check = check, IsVulnerable = false, Details = $"Error: {ex.Message}" });
                }
            }
            return findings;
        }

        private string RunConfigCommand(string command)
        {
            // Simple shell/PowerShell runner
            if (string.IsNullOrWhiteSpace(command)) return string.Empty;
            if (command.Trim().StartsWith("powershell", StringComparison.OrdinalIgnoreCase))
            {
                // Windows PowerShell
                var psi = new System.Diagnostics.ProcessStartInfo("powershell", $"-Command \"{command.Substring(10).Trim()}\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    return process.StandardOutput.ReadToEnd();
                }
            }
            else
            {
                // Bash or shell
                var psi = new System.Diagnostics.ProcessStartInfo("/bin/bash", $"-c \"{command}\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    return process.StandardOutput.ReadToEnd();
                }
            }
        }

        // Output normalization for Linux/macOS (example for installed software)
        public IEnumerable<string> GetNormalizedInstalledSoftware()
        {
            var raw = GetInstalledSoftware();
            if (raw == null) return new List<string>();
            var normalized = new List<string>();
            foreach (var line in raw)
            {
                // Example: dpkg -l output normalization
                if (line.StartsWith("ii "))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                        normalized.Add($"{parts[1]} {parts[2]}");
                }
                // Add more normalization rules as needed
            }
            return normalized.Count > 0 ? normalized : raw;
        }

        // Network/port scanning stub
        public async Task<IEnumerable<PortScanResult>> ScanNetworkAsync(string targetHost, IEnumerable<int> ports, int timeoutMs = 1000)
        {
            var results = new List<PortScanResult>();
            var tasks = ports.Select(async port =>
            {
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    try
                    {
                        var connectTask = client.ConnectAsync(targetHost, port);
                        var completed = await Task.WhenAny(connectTask, Task.Delay(timeoutMs));
                        bool isOpen = completed == connectTask && client.Connected;
                        return new PortScanResult { Port = port, IsOpen = isOpen, Service = null };
                    }
                    catch { return new PortScanResult { Port = port, IsOpen = false, Service = null }; }
                }
            });
            results.AddRange(await Task.WhenAll(tasks));
            return results;
        }

        // Authenticated scan stubs
        public bool TestSshConnection(string host, string username, string privateKeyPath)
        {
            // TODO: Implement SSH connection test (e.g., using SSH.NET)
            return false;
        }
        public bool TestWinRmConnection(string host, string username, string password)
        {
            // TODO: Implement WinRM connection test
            return false;
        }

        // Plugin/script support stub
        public IEnumerable<PluginFinding> RunPlugins(IEnumerable<string> pluginPaths)
        {
            var findings = new List<PluginFinding>();
            foreach (var path in pluginPaths)
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        findings.Add(new PluginFinding { PluginName = path, Result = "File not found", Severity = "Error" });
                        continue;
                    }
                    var ext = Path.GetExtension(path).ToLower();
                    string output = string.Empty;
                    if (ext == ".ps1")
                        output = RunConfigCommand($"powershell {path}");
                    else if (ext == ".sh")
                        output = RunConfigCommand($"bash {path}");
                    else
                        output = File.ReadAllText(path);
                    findings.Add(new PluginFinding { PluginName = path, Result = output, Severity = "Info" });
                }
                catch (Exception ex)
                {
                    findings.Add(new PluginFinding { PluginName = path, Result = $"Error: {ex.Message}", Severity = "Error" });
                }
            }
            return findings;
        }

        // External vulnerability feed update stub
        public bool UpdateCveDatabaseFromFeed(string feedUrl)
        {
            // TODO: Download and update CVE JSON from external feed
            return false;
        }

        // Enhanced reporting
        public IEnumerable<VulnReportGroup> GenerateVulnerabilityReport(IEnumerable<CveEntry> vulns)
        {
            return vulns.GroupBy(v => v.Severity)
                .Select(g => new VulnReportGroup
                {
                    Severity = g.Key,
                    Count = g.Count(),
                    TopCVEs = g.OrderByDescending(c => c.CvssScore).Take(5).ToList()
                });
        }

        // Compliance policy template support stub
        public IEnumerable<ConfigCheck> LoadComplianceTemplate(string templateName)
        {
            // TODO: Load compliance checks from a template file (YAML/JSON)
            return new List<ConfigCheck>();
        }
    }

    public class CveEntry
    {
        public string CveId { get; set; }
        public string Product { get; set; }
        public List<string> AffectedVersions { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public string Cpe { get; set; } // Common Platform Enumeration
        public double CvssScore { get; set; }
    }

    public class SoftwareInventoryItem
    {
        public string Product { get; set; }
        public string Version { get; set; }
        public string Cpe { get; set; } // Common Platform Enumeration
    }

    public class ConfigCheck
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Command { get; set; } // e.g., PowerShell, Bash, etc.
        public string ExpectedValue { get; set; }
    }

    public class ConfigFinding
    {
        public ConfigCheck Check { get; set; }
        public bool IsVulnerable { get; set; }
        public string Details { get; set; }
    }

    public class PortScanResult
    {
        public int Port { get; set; }
        public bool IsOpen { get; set; }
        public string Service { get; set; }
    }

    public class PluginFinding
    {
        public string PluginName { get; set; }
        public string Result { get; set; }
        public string Severity { get; set; }
    }

    public class VulnReportGroup
    {
        public string Severity { get; set; }
        public int Count { get; set; }
        public List<CveEntry> TopCVEs { get; set; }
    }
}
