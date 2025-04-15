#include "IPCFramework.h"
#include <iostream>
#include <chrono>
#include <algorithm>

namespace Sysguard {
namespace IPC {

// Initialize the global broker instance
std::shared_ptr<IPCBroker> g_ipcBroker;

// IPCBroker implementation
IPCBroker::IPCBroker() : _running(false) {
}

IPCBroker::~IPCBroker() {
    Stop();
}

bool IPCBroker::Start() {
    if (_running) {
        return true; // Already running
    }
    
    _running = true;
    
    // Start message processing thread
    _processingThread = std::thread(&IPCBroker::ProcessMessages, this);
    
    std::cout << "[IPC] Broker started" << std::endl;
    return true;
}

void IPCBroker::Stop() {
    if (!_running) {
        return; // Already stopped
    }
    
    _running = false;
    
    // Wake up processing thread
    _queueCondition.notify_all();
    
    // Wait for processing thread to exit
    if (_processingThread.joinable()) {
        _processingThread.join();
    }
    
    // Clear resources
    {
        std::lock_guard<std::mutex> lock(_queueMutex);
        std::queue<Message> empty;
        std::swap(_messageQueue, empty);
    }
    
    {
        std::lock_guard<std::mutex> lock(_handlersMutex);
        _handlers.clear();
    }
    
    {
        std::lock_guard<std::mutex> lock(_modulesMutex);
        _registeredModules.clear();
    }
    
    std::cout << "[IPC] Broker stopped" << std::endl;
}

bool IPCBroker::RegisterModule(const std::string& moduleName) {
    std::lock_guard<std::mutex> lock(_modulesMutex);
    
    // Check if module is already registered
    if (std::find(_registeredModules.begin(), _registeredModules.end(), moduleName) != _registeredModules.end()) {
        std::cout << "[IPC] Module '" << moduleName << "' is already registered" << std::endl;
        return false;
    }
    
    // Register module
    _registeredModules.push_back(moduleName);
    std::cout << "[IPC] Module '" << moduleName << "' registered" << std::endl;
    
    return true;
}

bool IPCBroker::UnregisterModule(const std::string& moduleName) {
    std::lock_guard<std::mutex> lock(_modulesMutex);
    
    // Find and remove module
    auto it = std::find(_registeredModules.begin(), _registeredModules.end(), moduleName);
    if (it != _registeredModules.end()) {
        _registeredModules.erase(it);
        std::cout << "[IPC] Module '" << moduleName << "' unregistered" << std::endl;
        
        // Also remove any registered handlers
        {
            std::lock_guard<std::mutex> handlersLock(_handlersMutex);
            _handlers.erase(moduleName);
        }
        
        return true;
    }
    
    std::cout << "[IPC] Module '" << moduleName << "' not found for unregistration" << std::endl;
    return false;
}

bool IPCBroker::IsModuleRegistered(const std::string& moduleName) const {
    std::lock_guard<std::mutex> lock(_modulesMutex);
    return std::find(_registeredModules.begin(), _registeredModules.end(), moduleName) != _registeredModules.end();
}

bool IPCBroker::SendMessage(const Message& message) {
    if (!_running) {
        std::cerr << "[IPC] Cannot send message, broker is not running" << std::endl;
        return false;
    }
    
    // Validate message
    if (message.sourceModule.empty()) {
        std::cerr << "[IPC] Message has empty source module" << std::endl;
        return false;
    }
    
    // Queue message
    {
        std::lock_guard<std::mutex> lock(_queueMutex);
        _messageQueue.push(message);
    }
    
    // Notify processing thread
    _queueCondition.notify_one();
    
    return true;
}

bool IPCBroker::RegisterHandler(const std::string& moduleName, const MessageHandler& handler) {
    if (!handler) {
        std::cerr << "[IPC] Cannot register null handler for module '" << moduleName << "'" << std::endl;
        return false;
    }
    
    std::lock_guard<std::mutex> lock(_handlersMutex);
    _handlers[moduleName].push_back(handler);
    
    std::cout << "[IPC] Registered handler for module '" << moduleName << "'" << std::endl;
    return true;
}

void IPCBroker::ProcessMessages() {
    while (_running) {
        Message message;
        bool hasMessage = false;
        
        // Get next message from queue
        {
            std::unique_lock<std::mutex> lock(_queueMutex);
            
            if (_messageQueue.empty()) {
                // Wait for new messages or stop signal
                _queueCondition.wait_for(lock, std::chrono::milliseconds(100), 
                    [this]() { return !_running || !_messageQueue.empty(); });
                
                if (!_running && _messageQueue.empty()) {
                    break; // Exit if stopped and queue is empty
                }
                
                if (_messageQueue.empty()) {
                    continue; // No messages yet
                }
            }
            
            message = _messageQueue.front();
            _messageQueue.pop();
            hasMessage = true;
        }
        
        // Process message
        if (hasMessage) {
            std::vector<std::string> targetModules;
            
            // Determine target modules
            if (message.targetModule.empty()) {
                // Broadcast to all modules
                std::lock_guard<std::mutex> lock(_modulesMutex);
                targetModules = _registeredModules;
                
                // Remove source module from broadcast targets
                targetModules.erase(
                    std::remove(targetModules.begin(), targetModules.end(), message.sourceModule),
                    targetModules.end()
                );
            } else {
                // Send to specific module
                targetModules.push_back(message.targetModule);
            }
            
            // Call handlers for target modules
            std::lock_guard<std::mutex> lock(_handlersMutex);
            
            for (const auto& targetModule : targetModules) {
                auto it = _handlers.find(targetModule);
                if (it != _handlers.end()) {
                    for (const auto& handler : it->second) {
                        try {
                            handler(message);
                        } catch (const std::exception& e) {
                            std::cerr << "[IPC] Exception in handler for module '" << targetModule 
                                     << "': " << e.what() << std::endl;
                        }
                    }
                }
            }
        }
    }
}

// Initialize the IPC framework
std::shared_ptr<IPCBroker> InitializeIPC() {
    // Create and start broker if not already initialized
    if (!g_ipcBroker) {
        g_ipcBroker = std::make_shared<IPCBroker>();
        g_ipcBroker->Start();
    }
    
    return g_ipcBroker;
}

} // namespace IPC
} // namespace Sysguard