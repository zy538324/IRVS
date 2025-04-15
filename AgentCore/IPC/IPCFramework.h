#pragma once

#include <string>
#include <memory>
#include <functional>
#include <map>
#include <mutex>
#include <vector>
#include <queue>
#include <thread>
#include <condition_variable>
#include <atomic>

namespace Sysguard {
namespace IPC {

// Forward declaration
class IPCBroker;

// Global broker instance
extern std::shared_ptr<IPCBroker> g_ipcBroker;

// Message types for IPC communication
enum class MessageType {
    REGISTER,       // Register a module with the IPC system
    UNREGISTER,     // Unregister a module
    COMMAND,        // Send a command to a module
    RESPONSE,       // Response to a command
    STATUS,         // Status update from a module
    ALERT,          // Alert notification
    DATA            // Generic data transfer
};

// Message structure for IPC communication
struct Message {
    std::string id;             // Unique message ID
    std::string sourceModule;   // Source module name
    std::string targetModule;   // Target module name (empty for broadcast)
    MessageType type;           // Message type
    std::string payload;        // Message payload (typically JSON)
    std::string correlationId;  // ID for correlating requests/responses
    
    // Helper methods for creating common message types
    static Message CreateCommand(const std::string& source, const std::string& target, const std::string& command) {
        Message msg;
        msg.id = GenerateUniqueId();
        msg.sourceModule = source;
        msg.targetModule = target;
        msg.type = MessageType::COMMAND;
        msg.payload = command;
        return msg;
    }
    
    static Message CreateResponse(const Message& request, const std::string& response) {
        Message msg;
        msg.id = GenerateUniqueId();
        msg.sourceModule = request.targetModule;
        msg.targetModule = request.sourceModule;
        msg.type = MessageType::RESPONSE;
        msg.payload = response;
        msg.correlationId = request.id;
        return msg;
    }
    
    static Message CreateStatus(const std::string& source, const std::string& status) {
        Message msg;
        msg.id = GenerateUniqueId();
        msg.sourceModule = source;
        msg.type = MessageType::STATUS;
        msg.payload = status;
        return msg;
    }
    
    static Message CreateAlert(const std::string& source, const std::string& alert) {
        Message msg;
        msg.id = GenerateUniqueId();
        msg.sourceModule = source;
        msg.type = MessageType::ALERT;
        msg.payload = alert;
        return msg;
    }
    
private:
    static std::string GenerateUniqueId() {
        static std::atomic<unsigned long> counter(0);
        return std::to_string(++counter);
    }
};

// Message handler function type
using MessageHandler = std::function<void(const Message&)>;

// Main IPC broker class
class IPCBroker : public std::enable_shared_from_this<IPCBroker> {
public:
    IPCBroker();
    ~IPCBroker();
    
    // Start/stop the broker
    bool Start();
    void Stop();
    
    // Module registration
    bool RegisterModule(const std::string& moduleName);
    bool UnregisterModule(const std::string& moduleName);
    bool IsModuleRegistered(const std::string& moduleName) const;
    
    // Message handling
    bool SendMessage(const Message& message);
    bool RegisterHandler(const std::string& moduleName, const MessageHandler& handler);
    
private:
    std::atomic<bool> _running;
    std::thread _processingThread;
    std::queue<Message> _messageQueue;
    std::mutex _queueMutex;
    std::condition_variable _queueCondition;
    std::map<std::string, std::vector<MessageHandler>> _handlers;
    std::mutex _handlersMutex;
    std::vector<std::string> _registeredModules;
    std::mutex _modulesMutex;
    
    void ProcessMessages();
};

// Initialize the IPC framework
std::shared_ptr<IPCBroker> InitializeIPC();

} // namespace IPC
} // namespace Sysguard