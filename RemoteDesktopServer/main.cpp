#include "RemoteDesktopServer.h"
#ifdef ENABLE_IPC
#include "IPC/IPCFramework.h"
#endif
#include <iostream>
#include <string>
#include <memory>
#include <signal.h>
#include <thread>
#include <chrono>

// Global variables
std::shared_ptr<RemoteDesktopServer> g_server;
bool g_running = true;

#ifdef ENABLE_IPC
// IPC broker instance for cross-module communication
std::shared_ptr<Sysguard::IPC::IPCBroker> Sysguard::IPC::g_ipcBroker;
#endif

// Signal handler for graceful shutdown
void SignalHandler(int signal) {
    std::cout << "Received signal: " << signal << std::endl;
    g_running = false;
}

// Parse command line arguments
struct CommandLineArgs {
    int port = DEFAULT_PORT;
    bool headless = false;
    bool enableIPC = false;
    std::string logLevel = "info";
    std::string agentId = "";
};

CommandLineArgs ParseCommandLine(int argc, char* argv[]) {
    CommandLineArgs args;
    
    for (int i = 1; i < argc; ++i) {
        std::string arg = argv[i];
        
        if (arg == "--port" || arg == "-p") {
            if (i + 1 < argc) {
                args.port = std::stoi(argv[++i]);
            }
        } else if (arg == "--headless") {
            args.headless = true;
        } else if (arg == "--enable-ipc") {
            args.enableIPC = true;
        } else if (arg == "--log-level" || arg == "-l") {
            if (i + 1 < argc) {
                args.logLevel = argv[++i];
            }
        } else if (arg == "--agent-id") {
            if (i + 1 < argc) {
                args.agentId = argv[++i];
            }
        } else if (arg == "--help" || arg == "-h") {
            std::cout << "Usage: RemoteDesktopServer [options]\n"
                      << "Options:\n"
                      << "  --port, -p PORT       Set server port (default: " << DEFAULT_PORT << ")\n"
                      << "  --headless            Run in headless mode without UI\n"
                      << "  --enable-ipc          Enable IPC communication with AgentCore\n"
                      << "  --log-level, -l LEVEL Set logging level (debug|info|warning|error)\n"
                      << "  --agent-id ID         Set agent identifier\n"
                      << "  --help, -h            Show this help message\n";
            exit(0);
        }
    }
    
    return args;
}

// Configure logging based on command line arguments
void ConfigureLogging(const std::string& level) {
    LogLevel logLevel = LogLevel::INFO;
    
    if (level == "debug") {
        logLevel = LogLevel::DEBUG;
    } else if (level == "info") {
        logLevel = LogLevel::INFO;
    } else if (level == "warning") {
        logLevel = LogLevel::WARNING;
    } else if (level == "error") {
        logLevel = LogLevel::ERROR;
    }
    
    // In a real implementation, would set global log level
    LogMessage(logLevel, "Logging configured at " + level + " level");
}

int main(int argc, char* argv[]) {
    // Parse command line arguments
    CommandLineArgs args = ParseCommandLine(argc, argv);
    
    // Configure logging
    ConfigureLogging(args.logLevel);
    
    // Set up signal handlers for graceful shutdown
    signal(SIGINT, SignalHandler);
    signal(SIGTERM, SignalHandler);
    
    LogMessage(LogLevel::INFO, "RemoteDesktopServer v" + std::string(SERVER_VERSION) + " starting...");
    
#ifdef ENABLE_IPC
    if (args.enableIPC) {
        LogMessage(LogLevel::INFO, "Initializing IPC framework...");
        try {
            // Initialize IPC framework
            Sysguard::IPC::g_ipcBroker = Sysguard::IPC::InitializeIPC();
            LogMessage(LogLevel::INFO, "IPC framework initialized");
        } catch (const std::exception& e) {
            LogMessage(LogLevel::ERROR, "Failed to initialize IPC: " + std::string(e.what()));
        }
    }
#endif
    
    // Create and start the server
    g_server = std::make_shared<RemoteDesktopServer>();
    if (!g_server->Start(args.port)) {
        LogMessage(LogLevel::ERROR, "Failed to start RemoteDesktopServer");
        return 1;
    }
    
#ifdef ENABLE_IPC
    // Register with AgentCore if IPC is enabled
    if (args.enableIPC && !args.agentId.empty()) {
        g_server->RegisterWithAgentCore(args.agentId);
        
        // Send initial status update
        g_server->SendStatusToAgentCore();
        
        // Set up periodic status updates
        std::thread statusThread([&]() {
            while (g_running) {
                std::this_thread::sleep_for(std::chrono::seconds(60));
                if (g_running) {
                    g_server->SendStatusToAgentCore();
                }
            }
        });
        statusThread.detach();
    }
#endif
    
    LogMessage(LogLevel::INFO, "RemoteDesktopServer running on port " + std::to_string(args.port));
    LogMessage(LogLevel::INFO, "Press Ctrl+C to exit");
    
    // Main event loop
    while (g_running) {
        std::this_thread::sleep_for(std::chrono::milliseconds(100));
    }
    
    LogMessage(LogLevel::INFO, "Shutting down RemoteDesktopServer...");
    g_server->Stop();
    
#ifdef ENABLE_IPC
    // Shut down IPC framework
    if (args.enableIPC && Sysguard::IPC::g_ipcBroker) {
        Sysguard::IPC::g_ipcBroker->Stop();
        Sysguard::IPC::g_ipcBroker.reset();
    }
#endif
    
    LogMessage(LogLevel::INFO, "RemoteDesktopServer terminated");
    return 0;
}