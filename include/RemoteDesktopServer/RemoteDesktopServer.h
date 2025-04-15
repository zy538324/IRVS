#pragma once

#include <string>
#include <map>
#include <vector>
#include <thread>
#include <mutex>
#include <atomic>
#include <functional>
#include <memory>
#include <cstdint>

#ifdef _WIN32
#include <windows.h>
#else
#include <unistd.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <fcntl.h>
#include <errno.h>
#endif

#define SERVER_VERSION "1.2.0"

// Callback function type for C# integration
// First parameter: message type
// Second parameter: message content (JSON formatted string)
typedef void (*ManagedCallback)(const char*, const char*);

enum class LogLevel {
    DEBUG,
    INFO,
    WARNING,
    ERROR
};

enum class MessageType {
    CONTROL = 0,
    SCREEN_DATA = 1,
    INPUT = 2,
    AUDIO = 3,
    CHAT = 4,
    FILE_TRANSFER = 5,
    CLIPBOARD = 6,
    UNDEFINED = 255
};

// Serializable message class for structured communication
class SerializableMessage {
public:
    MessageType type = MessageType::UNDEFINED;
    std::string source;
    std::string target;
    std::string content;
    uint64_t timestamp = 0;

    // Serialization methods
    std::vector<uint8_t> Serialize() const;
    static SerializableMessage Deserialize(const std::vector<uint8_t>& data);
};

class RemoteDesktopServer {
public:
    RemoteDesktopServer();
    ~RemoteDesktopServer();
    
    // Main server control
    bool Start(int port = 8900);
    void Stop();
    bool IsRunning() const { return _running; }
    
    // Session management
    bool AddSession(const std::string& sessionId);
    bool RemoveSession(const std::string& sessionId);
    size_t GetSessionCount() const;
    
    // Logging
    void LogMessage(LogLevel level, const std::string& message);
    void SetLogCallback(std::function<void(LogLevel, const std::string&)> callback);
    
    // C# interoperability methods
    void RegisterManagedCallback(ManagedCallback callback);
    bool ExecuteCommand(const char* command, char* response, int responseSize);
    const char* GetServerInfo();
    
    // IPC server for local integration
    bool StartIPCServer(int ipcPort = 8901);
    void StopIPCServer();
    
    // Configuration
    bool LoadConfigFromJson(const std::string& jsonConfig);
    std::string SaveConfigToJson() const;
    
    // Set an identifier for this agent
    void SetAgentId(const std::string& agentId) { _agentId = agentId; }
    std::string GetAgentId() const { return _agentId; }
    
private:
    // IPC thread method
    void IPCServerThread();
    
    // Server info cache update
    void UpdateServerInfoCache() const;
    
    // Member variables
    int _port = 8900;
    std::atomic<bool> _running{false};
    std::atomic<bool> _ipcRunning{false};
    int _ipcPort = 8901;
    std::thread _serverThread;
    std::thread _ipcThread;
    std::string _agentId;
    
    // Sessions map with mutex
    std::map<std::string, std::shared_ptr<void>> _sessions;
    mutable std::mutex _sessionsMutex;
    
    // Callback for C# integration
    ManagedCallback _managedCallback = nullptr;
    std::mutex _callbackMutex;
    
    // Logging callback
    std::function<void(LogLevel, const std::string&)> _logCallback;
    
    // Server info cache
    mutable std::string _serverInfoCache;
    mutable std::mutex _serverInfoMutex;
};

// C-style exports for C# P/Invoke
extern "C" {
#ifdef _WIN32
#define EXPORT __declspec(dllexport)
#else
#define EXPORT __attribute__((visibility("default")))
#endif

    EXPORT RemoteDesktopServer* CreateRemoteDesktopServer();
    EXPORT void DestroyRemoteDesktopServer(RemoteDesktopServer* server);
    EXPORT bool StartServer(RemoteDesktopServer* server, int port);
    EXPORT void StopServer(RemoteDesktopServer* server);
    EXPORT bool IsServerRunning(RemoteDesktopServer* server);
    EXPORT void RegisterCallback(RemoteDesktopServer* server, ManagedCallback callback);
    EXPORT bool ExecuteServerCommand(RemoteDesktopServer* server, const char* command, char* response, int responseSize);
    EXPORT const char* GetServerInformation(RemoteDesktopServer* server);
    EXPORT bool StartIPCServer(RemoteDesktopServer* server, int port);
    EXPORT void StopIPCServer(RemoteDesktopServer* server);
    EXPORT void SetAgentIdentifier(RemoteDesktopServer* server, const char* agentId);
}