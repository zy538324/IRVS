#include "RemoteDesktopServer.h"

// C-style exports implementation for C# P/Invoke
extern "C" {

RemoteDesktopServer* CreateRemoteDesktopServer() {
    return new RemoteDesktopServer();
}

void DestroyRemoteDesktopServer(RemoteDesktopServer* server) {
    if (server) {
        delete server;
    }
}

bool StartServer(RemoteDesktopServer* server, int port) {
    if (server) {
        return server->Start(port);
    }
    return false;
}

void StopServer(RemoteDesktopServer* server) {
    if (server) {
        server->Stop();
    }
}

bool IsServerRunning(RemoteDesktopServer* server) {
    if (server) {
        return server->IsRunning();
    }
    return false;
}

void RegisterCallback(RemoteDesktopServer* server, ManagedCallback callback) {
    if (server) {
        server->RegisterManagedCallback(callback);
    }
}

bool ExecuteServerCommand(RemoteDesktopServer* server, const char* command, char* response, int responseSize) {
    if (server && command && response && responseSize > 0) {
        return server->ExecuteCommand(command, response, responseSize);
    }
    return false;
}

const char* GetServerInformation(RemoteDesktopServer* server) {
    if (server) {
        return server->GetServerInfo();
    }
    return nullptr;
}

bool StartIPCServer(RemoteDesktopServer* server, int port) {
    if (server) {
        return server->StartIPCServer(port);
    }
    return false;
}

void StopIPCServer(RemoteDesktopServer* server) {
    if (server) {
        server->StopIPCServer();
    }
}

void SetAgentIdentifier(RemoteDesktopServer* server, const char* agentId) {
    if (server && agentId) {
        server->SetAgentId(agentId);
    }
}

} // extern "C"