using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Assuming these namespaces exist
using SystemMonitor;
using VulnScanner;
using AVScanner;
using PatchManager;
using RemoteShellServer;

namespace AgentCore
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("SysGuard Agent Core - Starting up...");
            
            try
            {
                // Create and configure the host
                var host = CreateHostBuilder(args).Build();
                
                // Start the agent services
                var agent = host.Services.GetRequiredService<Agent>();
                
                // Register ctrl+c to gracefully shut down
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    agent.Shutdown();
                };
                
                // Run the agent
                await agent.StartAsync();
                
                // Wait for shutdown signal
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical error in Agent: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Register core agent services
                    services.AddSingleton<Agent>();
                    
                    // Register module configurations from appsettings.json
                    var configuration = hostContext.Configuration;
                    services.Configure<SystemMonitorConfig>(configuration.GetSection("SystemMonitor"));
                    services.Configure<VulnScannerConfig>(configuration.GetSection("VulnScanner"));
                    services.Configure<AVScannerConfig>(configuration.GetSection("AVScanner"));
                    services.Configure<PatchManagerConfig>(configuration.GetSection("PatchManager"));
                    services.Configure<RemoteShellConfig>(configuration.GetSection("RemoteShell"));
                    services.Configure<AgentConfig>(configuration.GetSection("Agent"));
                    
                    // Register individual module services
                    ConfigureSystemMonitor(services, configuration);
                    ConfigureVulnScanner(services, configuration);
                    ConfigureAVScanner(services, configuration);
                    ConfigurePatchManager(services, configuration);
                    ConfigureRemoteShell(services, configuration);
                    
                    // Register internal agent services
                    services.AddSingleton<IConnectionManager, ConnectionManager>();
                    services.AddSingleton<IScheduler, Scheduler>();
                    services.AddHostedService<AgentBackgroundService>();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddFile(hostingContext.Configuration.GetSection("Logging"));
                });

        private static void ConfigureSystemMonitor(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ISystemMonitorService, SystemMonitorService>();
            // Add additional SystemMonitor dependencies
        }
        
        private static void ConfigureVulnScanner(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IVulnScannerService, VulnScannerService>();
            // Add additional VulnScanner dependencies
        }
        
        private static void ConfigureAVScanner(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IAVScannerService, AVScannerService>();
            // Add additional AVScanner dependencies
        }
        
        private static void ConfigurePatchManager(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IPatchManagerService, PatchManagerService>();
            // Add additional PatchManager dependencies
        }
        
        private static void ConfigureRemoteShell(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<RemoteShellService>();
            // Add additional RemoteShell dependencies
        }
    }
}
