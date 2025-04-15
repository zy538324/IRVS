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

#define SHELL_SERVER_VERSION "1.0.0"

// Callback function type for C# integration
// First parameter: message type (output, error, exit, etc.)
// Second parameter: message content
typedef void (*ShellManagedCallback)(const char*, const char*);

enum class ShellLogLevel {
    DEBUG,
    INFO,
    WARNING,
    ERROR
};

// Command execution result structure
struct ShellCommandResult {
    int exitCode;
    std::string stdOutput;
    std::string stdError;
    bool timedOut;
    uint64_t executionTimeMs;
};

// Shell session structure
struct ShellSession {
    std::string id;
    std::string user;
    bool isAdmin;
    bool isRunning;
    uint64_t startTime;
    
#ifdef _WIN32
    HANDLE processHandle;
    HANDLE stdInWrite;
    HANDLE stdOutRead;
    HANDLE stdErrRead;
#else
    pid_t pid;
    int stdInWrite;
    int stdOutRead;
    int stdErrRead;
#endif
};

class RemoteShellServer {
public:
    RemoteShellServer();
    ~RemoteShellServer();
    
    // Main server control
    bool Start(int port = 9900);
    void Stop();
    bool IsRunning() const { return _running; }
    
    // Session management
    std::string CreateSession(const std::string& shell = "", const std::string& initialDir = "", 
                            bool runAsAdmin = false);
    bool TerminateSession(const std::string& sessionId);
    bool WriteToSession(const std::string& sessionId, const std::string& input);
    bool ResizeSession(const std::string& sessionId, int cols, int rows);
    std::map<std::string, ShellSession> GetSessions() const;
    
    // Direct command execution (without session)
    ShellCommandResult ExecuteCommand(const std::string& command, 
                                    int timeoutMs = 30000,
                                    const std::string& workingDir = "");
    
    // Logging
    void LogMessage(ShellLogLevel level, const std::string& message);
    void SetLogCallback(std::function<void(ShellLogLevel, const std::string&)> callback);
    
    // C# interoperability methods
    void RegisterManagedCallback(ShellManagedCallback callback);
    bool ExecuteShellCommand(const char* command, char* response, int responseSize);
    const char* GetServerInfo();
    
    // IPC server for local integration
    bool StartIPCServer(int ipcPort = 9901);
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
    
    // Session reader threads
    void SessionOutputReader(const std::string& sessionId);
    void SessionErrorReader(const std::string& sessionId);
    
    // Server info cache update
    void UpdateServerInfoCache() const;
    
    // Member variables
    int _port = 9900;
    std::atomic<bool> _running{false};
    std::atomic<bool> _ipcRunning{false};
    int _ipcPort = 9901;
    std::thread _serverThread;
    std::thread _ipcThread;
    std::string _agentId;
    
    // Sessions map with mutex
    std::map<std::string, ShellSession> _sessions;
    mutable std::mutex _sessionsMutex;
    
    // Reader threads
    std::map<std::string, std::thread> _outputReaders;
    std::map<std::string, std::thread> _errorReaders;
    
    // Callback for C# integration
    ShellManagedCallback _managedCallback = nullptr;
    std::mutex _callbackMutex;
    
    // Logging callback
    std::function<void(ShellLogLevel, const std::string&)> _logCallback;
    
    // Server info cache
    mutable std::string _serverInfoCache;
    mutable std::mutex _serverInfoMutex;
    
    // Maximum output buffer size
    const size_t _maxBufferSize = 1024 * 1024; // 1MB
};

// C-style exports for C# P/Invoke
extern "C" {
#ifdef _WIN32
#define SHELL_EXPORT __declspec(dllexport)
#else
#define SHELL_EXPORT __attribute__((visibility("default")))
#endif

    SHELL_EXPORT RemoteShellServer* CreateRemoteShellServer();
    SHELL_EXPORT void DestroyRemoteShellServer(RemoteShellServer* server);
    SHELL_EXPORT bool StartShellServer(RemoteShellServer* server, int port);
    SHELL_EXPORT void StopShellServer(RemoteShellServer* server);
    SHELL_EXPORT bool IsShellServerRunning(RemoteShellServer* server);
    SHELL_EXPORT void RegisterShellCallback(RemoteShellServer* server, ShellManagedCallback callback);
    SHELL_EXPORT bool ExecuteShellServerCommand(RemoteShellServer* server, const char* command, char* response, int responseSize);
    SHELL_EXPORT const char* GetShellServerInformation(RemoteShellServer* server);
    SHELL_EXPORT bool StartShellIPCServer(RemoteShellServer* server, int port);
    SHELL_EXPORT void StopShellIPCServer(RemoteShellServer* server);
    SHELL_EXPORT void SetShellAgentIdentifier(RemoteShellServer* server, const char* agentId);
    SHELL_EXPORT const char* CreateShellSession(RemoteShellServer* server, const char* shell, const char* initialDir, bool runAsAdmin);
    SHELL_EXPORT bool TerminateShellSession(RemoteShellServer* server, const char* sessionId);
    SHELL_EXPORT bool WriteToShellSession(RemoteShellServer* server, const char* sessionId, const char* input);
    SHELL_EXPORT bool ResizeShellSession(RemoteShellServer* server, const char* sessionId, int cols, int rows);
    SHELL_EXPORT const char* GetShellSessions(RemoteShellServer* server);
    SHELL_EXPORT const char* ExecuteShellCommandDirect(RemoteShellServer* server, const char* command, int timeoutMs, const char* workingDir);
}