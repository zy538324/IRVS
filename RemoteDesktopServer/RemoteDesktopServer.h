#pragma once

#include <iostream>
#include <string>
#include <thread>
#include <chrono>
#include <vector>
#include <map>
#include <mutex>
#include <atomic>
#include <functional>
#include <memory>
#include <condition_variable>
#include <queue>
#include <list>
#include <fstream>

// Include version header
#include "version.h"

#define SERVER_VERSION REMOTE_DESKTOP_SERVER_VERSION
#define DEFAULT_PORT 8900

// Cross-platform headers
#ifdef _WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#include <windows.h>
#pragma comment(lib, "ws2_32.lib")

// C++/CLI support for C# interop when compiled with /clr
#ifdef _MANAGED
#include <msclr/marshal.h>
#include <msclr/marshal_cppstd.h>
#endif
#else
#include <sys/socket.h>
#include <netinet/in.h>
#include <unistd.h>
#include <fcntl.h>
#include <arpa/inet.h>
#endif

// Forward declarations
class Session;
class ScreenCapture;
class InputHandler;
class NetworkManager;
class AuthenticationManager;
class EncryptionManager;
class FileTransfer;
class RemoteClipboard;
class ChatManager;
class SessionRecorder;
class UiManager;
class MultiMonitorManager;

// Serializable message for inter-process/network communication
struct SerializableMessage {
    enum class MessageType {
        COMMAND,
        RESPONSE,
        STATUS,
        ERROR
    };

    MessageType type;
    std::string source;
    std::string target;
    std::string content;
    uint64_t timestamp;
    
    std::vector<uint8_t> Serialize() const;
    static SerializableMessage Deserialize(const std::vector<uint8_t>& data);
};

// Logging enums and helpers
enum class LogLevel {
    DEBUG,
    INFO,
    WARNING,
    ERROR
};

// Simple logging function
inline void LogMessage(LogLevel level, const std::string& message) {
    const char* levelStr = "";
    switch (level) {
        case LogLevel::DEBUG: levelStr = "DEBUG"; break;
        case LogLevel::INFO: levelStr = "INFO"; break;
        case LogLevel::WARNING: levelStr = "WARNING"; break;
        case LogLevel::ERROR: levelStr = "ERROR"; break;
    }
    
    // In a real implementation, would use a proper logger
    std::string timestamp = "2023-12-01 12:00:00"; // Would be replaced with actual timestamp
    printf("[%s] [%s] %s\n", timestamp.c_str(), levelStr, message.c_str());
}

// C# interop callback function type for callbacks from C++ to C#
typedef void (*ManagedCallback)(const char* eventName, const char* jsonData);

// Main RemoteDesktopServer class
#ifdef _WIN32
// Export class for C++/C# interop
#ifdef REMOTEDESKTOPSERVER_EXPORTS
#define RDS_API __declspec(dllexport)
#else
#define RDS_API __declspec(dllimport)
#endif
#else
#define RDS_API
#endif

class RDS_API RemoteDesktopServer {
public:
    RemoteDesktopServer();
    ~RemoteDesktopServer();
    
    // Server lifecycle methods
    bool Start(int port = DEFAULT_PORT);
    void Stop();
    bool IsRunning() const { return _running; }
    int GetPort() const { return _port; }
    
    // Session management
    bool AddSession(const std::shared_ptr<Session>& session);
    bool RemoveSession(const std::string& id);
    std::shared_ptr<Session> GetSession(const std::string& id);
    size_t GetSessionCount() const { 
        std::lock_guard<std::mutex> lock(_sessionsMutex);
        return _sessions.size(); 
    }
    
    // IPC methods for cross-module communication
    void RegisterWithAgentCore(const std::string& agentId);
    void SendStatusToAgentCore();
    void ProcessCommandFromAgentCore(const std::string& command);
    
    // C# interoperability methods
    void RegisterManagedCallback(ManagedCallback callback);
    bool ExecuteCommand(const char* command, char* response, int responseSize);
    const char* GetServerInfo();
    
    // Named pipe or TCP-based communication for .NET clients
    bool StartIPCServer(int ipcPort = 8901);
    void StopIPCServer();
    
    // JSON configuration for cross-language compatibility
    bool LoadConfigFromJson(const std::string& jsonConfig);
    std::string SaveConfigToJson() const;
    
private:
    bool _running;
    int _port;
    std::map<std::string, std::shared_ptr<Session>> _sessions;
    mutable std::mutex _sessionsMutex;
    std::string _agentId;
    
    // Server implementation details
    bool InitializeSocket();
    void AcceptConnections();
    void CleanupSockets();
    
    // Socket internals (platform-specific)
#ifdef _WIN32
    unsigned long long _serverSocket;  // SOCKET on Windows
    void* _wsaData;  // WSAData*
#else
    int _serverSocket;
#endif

    // IPC server for C# communication
    std::atomic<bool> _ipcRunning;
    int _ipcPort;
    std::thread _ipcThread;
    void IPCServerThread();
    
    // Callback to C# code
    ManagedCallback _managedCallback;
    std::mutex _callbackMutex;
    
    // Server information cache for interop
    mutable std::string _serverInfoCache;
    mutable std::mutex _serverInfoMutex;
    void UpdateServerInfoCache() const;

    // Logging helper (in a real implementation, this would use a logging framework)
    void LogMessage(LogLevel level, const std::string& message) const {
        // Simple implementation for demonstration purposes
        const char* levelStr;
        switch (level) {
            case LogLevel::DEBUG: levelStr = "DEBUG"; break;
            case LogLevel::INFO: levelStr = "INFO"; break;
            case LogLevel::WARNING: levelStr = "WARNING"; break;
            case LogLevel::ERROR: levelStr = "ERROR"; break;
            default: levelStr = "UNKNOWN"; break;
        }
        
        std::string logMessage = "[RemoteDesktopServer][";
        logMessage += levelStr;
        logMessage += "] ";
        logMessage += message;
        
        // In a real implementation, would use a proper logging system
        std::cout << logMessage << std::endl;
        
        // Also send to C# callback if registered
        if (_managedCallback) {
            std::lock_guard<std::mutex> lock(_callbackMutex);
            _managedCallback("log", (std::string("{\"level\":\"") + levelStr + 
                "\",\"message\":\"" + message + "\"}").c_str());
        }
    }
};