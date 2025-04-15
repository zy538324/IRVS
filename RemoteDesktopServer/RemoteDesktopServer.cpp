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

// Cross-platform headers
#ifdef _WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#include <windows.h>
#pragma comment(lib, "ws2_32.lib")
#else
#include <sys/socket.h>
#include <netinet/in.h>
#include <unistd.h>
#include <fcntl.h>
#include <arpa/inet.h>
#endif

// Platform-specific screen capture and input simulation
#ifdef _WIN32
#include <gdiplus.h>
#pragma comment(lib, "gdiplus.lib")
#elif __APPLE__
// macOS headers would go here
#else
// Linux headers would go here
#include <X11/Xlib.h>
#include <X11/Xutil.h>
#endif

// Encryption stub (would use OpenSSL or similar)
#include <cstring>
#include <random>

// Configuration
const int DEFAULT_PORT = 8080;
const int MAX_CLIENTS = 10;
const int SCREEN_UPDATE_INTERVAL_MS = 50;
const int INACTIVITY_TIMEOUT_SECONDS = 300;
const std::string SERVER_VERSION = "1.0.0";

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

// Simple logging with levels
enum class LogLevel { DEBUG, INFO, WARNING, ERROR };

void LogMessage(LogLevel level, const std::string& message) {
    std::string levelStr;
    switch (level) {
        case LogLevel::DEBUG: levelStr = "DEBUG"; break;
        case LogLevel::INFO: levelStr = "INFO"; break;
        case LogLevel::WARNING: levelStr = "WARNING"; break;
        case LogLevel::ERROR: levelStr = "ERROR"; break;
    }
    std::cout << "[" << levelStr << "] " << message << std::endl;
    // In a real implementation, would log to file with timestamp
}

// Simple encryption manager (stub)
class EncryptionManager {
public:
    EncryptionManager() {
        std::random_device rd;
        std::mt19937 gen(rd());
        std::uniform_int_distribution<> dis(0, 255);
        for (int i = 0; i < 32; ++i) {
            _key[i] = static_cast<uint8_t>(dis(gen));
        }
        LogMessage(LogLevel::INFO, "Encryption manager initialized with new key");
    }
    
    std::vector<uint8_t> Encrypt(const std::vector<uint8_t>& data) {
        std::vector<uint8_t> encrypted(data.size());
        for (size_t i = 0; i < data.size(); ++i) {
            encrypted[i] = data[i] ^ _key[i % 32];
        }
        return encrypted;
    }
    
    std::vector<uint8_t> Decrypt(const std::vector<uint8_t>& data) {
        return Encrypt(data); // XOR is symmetric
    }
    
    bool NegotiateKey(int clientSocket) {
        // In a real implementation, would perform key exchange (e.g., Diffie-Hellman)
        LogMessage(LogLevel::INFO, "Key negotiation completed");
        return true;
    }
    
private:
    uint8_t _key[32]; // Simple encryption key
};

// Authentication manager
class AuthenticationManager {
public:
    bool Authenticate(const std::string& username, const std::string& password) {
        // In a real implementation, would validate against secure credential store
        _lastAuthTime = std::chrono::system_clock::now();
        LogMessage(LogLevel::INFO, "Authentication successful for user: " + username);
        return true;
    }
    
    bool ValidateSession(const std::string& sessionId) {
        // Check if session is still valid
        if (_sessions.find(sessionId) == _sessions.end()) return false;
        auto now = std::chrono::system_clock::now();
        auto elapsed = std::chrono::duration_cast<std::chrono::seconds>(
            now - _lastAuthTime).count();
        return elapsed < INACTIVITY_TIMEOUT_SECONDS;
    }
    
    std::string CreateSession(const std::string& username) {
        std::string sessionId = GenerateSessionId();
        _sessions[sessionId] = username;
        LogMessage(LogLevel::INFO, "Created session for user: " + username);
        return sessionId;
    }
    
    bool RevokeSession(const std::string& sessionId) {
        auto it = _sessions.find(sessionId);
        if (it == _sessions.end()) return false;
        LogMessage(LogLevel::INFO, "Revoked session for user: " + it->second);
        _sessions.erase(it);
        return true;
    }
    
private:
    std::map<std::string, std::string> _sessions;
    std::chrono::system_clock::time_point _lastAuthTime;
    
    std::string GenerateSessionId() {
        // Generate a random session ID
        std::random_device rd;
        std::mt19937 gen(rd());
        std::uniform_int_distribution<> dis(0, 15);
        const char* hex = "0123456789abcdef";
        std::string sessionId;
        for (int i = 0; i < 32; ++i) {
            sessionId += hex[dis(gen)];
        }
        return sessionId;
    }
};

// Cross-platform screen capture
class ScreenCapture {
public:
    ScreenCapture() {
#ifdef _WIN32
        // Initialize GDI+ for Windows
        Gdiplus::GdiplusStartupInput gdiplusStartupInput;
        ULONG_PTR gdiplusToken;
        Gdiplus::GdiplusStartup(&gdiplusToken, &gdiplusStartupInput, nullptr);
#elif __APPLE__
        // Initialize macOS screen capture
#else
        // Initialize X11 for Linux
        _display = XOpenDisplay(nullptr);
        _root = DefaultRootWindow(_display);
#endif
        LogMessage(LogLevel::INFO, "Screen capture initialized");
    }
    
    ~ScreenCapture() {
#ifdef _WIN32
        // Cleanup GDI+
#elif __APPLE__
        // Cleanup macOS resources
#else
        // Cleanup X11
        if (_display) XCloseDisplay(_display);
#endif
    }
    
    std::vector<uint8_t> CaptureScreen() {
        std::vector<uint8_t> screenData;
#ifdef _WIN32
        // Windows screen capture
        HDC hdcScreen = GetDC(NULL);
        HDC hdcMem = CreateCompatibleDC(hdcScreen);
        
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);
        
        HBITMAP hbmScreen = CreateCompatibleBitmap(hdcScreen, width, height);
        SelectObject(hdcMem, hbmScreen);
        BitBlt(hdcMem, 0, 0, width, height, hdcScreen, 0, 0, SRCCOPY);
        
        BITMAP bmpScreen;
        GetObject(hbmScreen, sizeof(BITMAP), &bmpScreen);
        
        BITMAPINFOHEADER bi;
        bi.biSize = sizeof(BITMAPINFOHEADER);
        bi.biWidth = width;
        bi.biHeight = -height;  // Negative for top-down
        bi.biPlanes = 1;
        bi.biBitCount = 32;
        bi.biCompression = BI_RGB;
        bi.biSizeImage = 0;
        bi.biXPelsPerMeter = 0;
        bi.biYPelsPerMeter = 0;
        bi.biClrUsed = 0;
        bi.biClrImportant = 0;
        
        int bmpSize = ((width * bi.biBitCount + 31) / 32) * 4 * height;
        screenData.resize(bmpSize);
        
        GetDIBits(hdcMem, hbmScreen, 0, height, screenData.data(), 
                 (BITMAPINFO*)&bi, DIB_RGB_COLORS);
        
        DeleteObject(hbmScreen);
        DeleteDC(hdcMem);
        ReleaseDC(NULL, hdcScreen);
#elif __APPLE__
        // macOS screen capture (stub)
        screenData.resize(1024); // Placeholder
#else
        // Linux X11 screen capture
        if (_display) {
            XWindowAttributes attr;
            XGetWindowAttributes(_display, _root, &attr);
            
            XImage *img = XGetImage(_display, _root, 0, 0, attr.width, attr.height, AllPlanes, ZPixmap);
            if (img) {
                int bpp = img->bits_per_pixel / 8;
                screenData.resize(attr.width * attr.height * bpp);
                memcpy(screenData.data(), img->data, screenData.size());
                XDestroyImage(img);
            }
        }
#endif
        LogMessage(LogLevel::DEBUG, "Screen captured: " + std::to_string(screenData.size()) + " bytes");
        return screenData;
    }
    
private:
#ifdef _WIN32
    // Windows specific members
#elif __APPLE__
    // macOS specific members
#else
    Display* _display = nullptr;
    Window _root;
#endif
};

// Cross-platform input handler
class InputHandler {
public:
    enum class InputType { MOUSE_MOVE, MOUSE_DOWN, MOUSE_UP, KEY_DOWN, KEY_UP };
    
    struct InputEvent {
        InputType type;
        int x;
        int y;
        int data; // Button or key code
    };
    
    bool ProcessInput(const InputEvent& event) {
#ifdef _WIN32
        // Windows input simulation
        INPUT input = {0};
        switch (event.type) {
            case InputType::MOUSE_MOVE:
                input.type = INPUT_MOUSE;
                input.mi.dx = event.x * 65536 / GetSystemMetrics(SM_CXSCREEN);
                input.mi.dy = event.y * 65536 / GetSystemMetrics(SM_CYSCREEN);
                input.mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE;
                SendInput(1, &input, sizeof(INPUT));
                break;
            case InputType::MOUSE_DOWN:
                input.type = INPUT_MOUSE;
                input.mi.dwFlags = (event.data == 0) ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_RIGHTDOWN;
                SendInput(1, &input, sizeof(INPUT));
                break;
            case InputType::MOUSE_UP:
                input.type = INPUT_MOUSE;
                input.mi.dwFlags = (event.data == 0) ? MOUSEEVENTF_LEFTUP : MOUSEEVENTF_RIGHTUP;
                SendInput(1, &input, sizeof(INPUT));
                break;
            case InputType::KEY_DOWN:
                input.type = INPUT_KEYBOARD;
                input.ki.wVk = event.data;
                input.ki.dwFlags = 0;
                SendInput(1, &input, sizeof(INPUT));
                break;
            case InputType::KEY_UP:
                input.type = INPUT_KEYBOARD;
                input.ki.wVk = event.data;
                input.ki.dwFlags = KEYEVENTF_KEYUP;
                SendInput(1, &input, sizeof(INPUT));
                break;
        }
#elif __APPLE__
        // macOS input simulation (stub)
#else
        // Linux input simulation (stub)
#endif
        LogMessage(LogLevel::DEBUG, "Processed input event of type: " + std::to_string(static_cast<int>(event.type)));
        return true;
    }
};

// Network manager for socket operations
class NetworkManager {
public:
    NetworkManager() : _running(false), _port(DEFAULT_PORT) {
#ifdef _WIN32
        WSADATA wsaData;
        WSAStartup(MAKEWORD(2, 2), &wsaData);
#endif
    }
    
    ~NetworkManager() {
        Stop();
#ifdef _WIN32
        WSACleanup();
#endif
    }
    
    bool Start(int port = DEFAULT_PORT) {
        _port = port;
        _running = true;
        
        // Create socket
#ifdef _WIN32
        _serverSocket = socket(AF_INET, SOCK_STREAM, 0);
        if (_serverSocket == INVALID_SOCKET) {
            LogMessage(LogLevel::ERROR, "Failed to create socket");
            return false;
        }
#else
        _serverSocket = socket(AF_INET, SOCK_STREAM, 0);
        if (_serverSocket < 0) {
            LogMessage(LogLevel::ERROR, "Failed to create socket");
            return false;
        }
#endif

        // Bind socket
        sockaddr_in serverAddr;
        serverAddr.sin_family = AF_INET;
        serverAddr.sin_addr.s_addr = INADDR_ANY;
        serverAddr.sin_port = htons(_port);
        
#ifdef _WIN32
        if (bind(_serverSocket, (SOCKADDR*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
            LogMessage(LogLevel::ERROR, "Bind failed");
            closesocket(_serverSocket);
            return false;
        }
#else
        if (bind(_serverSocket, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) < 0) {
            LogMessage(LogLevel::ERROR, "Bind failed");
            close(_serverSocket);
            return false;
        }
#endif

        // Listen for connections
#ifdef _WIN32
        if (listen(_serverSocket, SOMAXCONN) == SOCKET_ERROR) {
            LogMessage(LogLevel::ERROR, "Listen failed");
            closesocket(_serverSocket);
            return false;
        }
#else
        if (listen(_serverSocket, SOMAXCONN) < 0) {
            LogMessage(LogLevel::ERROR, "Listen failed");
            close(_serverSocket);
            return false;
        }
#endif

        LogMessage(LogLevel::INFO, "Server started on port " + std::to_string(_port));
        
        // Accept connections in a separate thread
        _acceptThread = std::thread(&NetworkManager::AcceptConnections, this);
        
        return true;
    }
    
    void Stop() {
        _running = false;
        
        if (_acceptThread.joinable()) {
            _acceptThread.join();
        }
        
#ifdef _WIN32
        closesocket(_serverSocket);
#else
        close(_serverSocket);
#endif

        LogMessage(LogLevel::INFO, "Server stopped");
    }
    
    bool SendData(int clientSocket, const std::vector<uint8_t>& data) {
        // In a real implementation, would handle partial sends
        int bytesSent = send(clientSocket, 
                           reinterpret_cast<const char*>(data.data()), 
                           static_cast<int>(data.size()), 0);
#ifdef _WIN32
        if (bytesSent == SOCKET_ERROR) {
            LogMessage(LogLevel::ERROR, "Send failed");
            return false;
        }
#else
        if (bytesSent < 0) {
            LogMessage(LogLevel::ERROR, "Send failed");
            return false;
        }
#endif
        return true;
    }
    
    std::vector<uint8_t> ReceiveData(int clientSocket) {
        std::vector<uint8_t> buffer(4096);
        int bytesRead = recv(clientSocket, 
                           reinterpret_cast<char*>(buffer.data()), 
                           static_cast<int>(buffer.size()), 0);
        
#ifdef _WIN32
        if (bytesRead == SOCKET_ERROR || bytesRead == 0) {
            return std::vector<uint8_t>();
        }
#else
        if (bytesRead <= 0) {
            return std::vector<uint8_t>();
        }
#endif
        
        buffer.resize(bytesRead);
        return buffer;
    }
    
    void SetConnectionCallback(std::function<void(int)> callback) {
        _connectionCallback = callback;
    }
    
private:
    void AcceptConnections() {
        while (_running) {
            sockaddr_in clientAddr;
#ifdef _WIN32
            int clientAddrSize = sizeof(clientAddr);
            SOCKET clientSocket = accept(_serverSocket, (SOCKADDR*)&clientAddr, &clientAddrSize);
            if (clientSocket == INVALID_SOCKET) {
                LogMessage(LogLevel::ERROR, "Accept failed");
                continue;
            }
#else
            socklen_t clientAddrSize = sizeof(clientAddr);
            int clientSocket = accept(_serverSocket, (struct sockaddr*)&clientAddr, &clientAddrSize);
            if (clientSocket < 0) {
                LogMessage(LogLevel::ERROR, "Accept failed");
                continue;
            }
#endif
            
            // Log connection
            char clientIP[INET_ADDRSTRLEN];
            inet_ntop(AF_INET, &(clientAddr.sin_addr), clientIP, INET_ADDRSTRLEN);
            LogMessage(LogLevel::INFO, "New connection from: " + std::string(clientIP));
            
            // Notify callback
            if (_connectionCallback) {
                _connectionCallback(clientSocket);
            }
        }
    }
    
#ifdef _WIN32
    SOCKET _serverSocket;
#else
    int _serverSocket;
#endif
    std::atomic<bool> _running;
    int _port;
    std::thread _acceptThread;
    std::function<void(int)> _connectionCallback;
};

// File transfer functionality
class FileTransfer {
public:
    enum class TransferDirection { UPLOAD, DOWNLOAD };
    
    struct FileTransferRequest {
        TransferDirection direction;
        std::string sourcePath;
        std::string destinationPath;
        uint64_t fileSize;
    };
    
    FileTransfer(NetworkManager& networkManager) : _networkManager(networkManager) {
        LogMessage(LogLevel::INFO, "File transfer module initialized");
    }
    
    bool StartFileTransfer(int clientSocket, const FileTransferRequest& request) {
        if (request.direction == TransferDirection::UPLOAD) {
            return ReceiveFile(clientSocket, request);
        } else {
            return SendFile(clientSocket, request);
        }
    }
    
    bool SendFile(int clientSocket, const FileTransferRequest& request) {
        LogMessage(LogLevel::INFO, "Sending file: " + request.sourcePath + " to client");
        
        // Check if source file exists and is accessible
        FILE* file = fopen(request.sourcePath.c_str(), "rb");
        if (!file) {
            LogMessage(LogLevel::ERROR, "Failed to open file for sending: " + request.sourcePath);
            return false;
        }
        
        // Get file size
        fseek(file, 0, SEEK_END);
        long fileSize = ftell(file);
        fseek(file, 0, SEEK_SET);
        
        // Send file size
        std::vector<uint8_t> sizeData(sizeof(fileSize));
        memcpy(sizeData.data(), &fileSize, sizeof(fileSize));
        if (!_networkManager.SendData(clientSocket, sizeData)) {
            fclose(file);
            return false;
        }
        
        // Send file content
        constexpr size_t bufferSize = 8192;
        std::vector<uint8_t> buffer(bufferSize);
        size_t bytesRead;
        
        while ((bytesRead = fread(buffer.data(), 1, bufferSize, file)) > 0) {
            buffer.resize(bytesRead);
            if (!_networkManager.SendData(clientSocket, buffer)) {
                fclose(file);
                return false;
            }
            buffer.resize(bufferSize);
        }
        
        fclose(file);
        LogMessage(LogLevel::INFO, "File sent successfully: " + request.sourcePath);
        return true;
    }
    
    bool ReceiveFile(int clientSocket, const FileTransferRequest& request) {
        LogMessage(LogLevel::INFO, "Receiving file: " + request.destinationPath + " from client");
        
        // Open destination file
        FILE* file = fopen(request.destinationPath.c_str(), "wb");
        if (!file) {
            LogMessage(LogLevel::ERROR, "Failed to open file for writing: " + request.destinationPath);
            return false;
        }
        
        // Receive file size
        auto sizeData = _networkManager.ReceiveData(clientSocket);
        if (sizeData.size() != sizeof(uint64_t)) {
            fclose(file);
            return false;
        }
        
        uint64_t fileSize;
        memcpy(&fileSize, sizeData.data(), sizeof(fileSize));
        
        // Receive file content
        uint64_t totalReceived = 0;
        while (totalReceived < fileSize) {
            auto data = _networkManager.ReceiveData(clientSocket);
            if (data.empty()) {
                fclose(file);
                return false;
            }
            
            fwrite(data.data(), 1, data.size(), file);
            totalReceived += data.size();
        }
        
        fclose(file);
        LogMessage(LogLevel::INFO, "File received successfully: " + request.destinationPath);
        return true;
    }
    
private:
    NetworkManager& _networkManager;
};

// Remote clipboard functionality
class RemoteClipboard {
public:
    RemoteClipboard(NetworkManager& networkManager) : _networkManager(networkManager) {
        LogMessage(LogLevel::INFO, "Remote clipboard module initialized");
    }
    
    bool SendClipboardData(int clientSocket, const std::string& data) {
        LogMessage(LogLevel::INFO, "Sending clipboard data to client");
        
        std::vector<uint8_t> clipboardData(data.begin(), data.end());
        return _networkManager.SendData(clientSocket, clipboardData);
    }
    
    std::string ReceiveClipboardData(int clientSocket) {
        LogMessage(LogLevel::INFO, "Receiving clipboard data from client");
        
        auto data = _networkManager.ReceiveData(clientSocket);
        return std::string(data.begin(), data.end());
    }
    
    void SetClipboardText(const std::string& text) {
#ifdef _WIN32
        // Windows implementation
        if (OpenClipboard(NULL)) {
            EmptyClipboard();
            HGLOBAL hg = GlobalAlloc(GMEM_MOVEABLE, text.size() + 1);
            if (hg) {
                char* ptr = static_cast<char*>(GlobalLock(hg));
                memcpy(ptr, text.c_str(), text.size() + 1);
                GlobalUnlock(hg);
                SetClipboardData(CF_TEXT, hg);
            }
            CloseClipboard();
        }
#elif __APPLE__
        // macOS implementation (stub)
        LogMessage(LogLevel::WARNING, "SetClipboardText not implemented for macOS");
#else
        // Linux implementation (stub)
        LogMessage(LogLevel::WARNING, "SetClipboardText not implemented for Linux");
#endif
        LogMessage(LogLevel::INFO, "Clipboard text set");
    }
    
    std::string GetClipboardText() {
        std::string result;
#ifdef _WIN32
        // Windows implementation
        if (OpenClipboard(NULL)) {
            HANDLE hData = GetClipboardData(CF_TEXT);
            if (hData) {
                char* pszText = static_cast<char*>(GlobalLock(hData));
                if (pszText) {
                    result = pszText;
                    GlobalUnlock(hData);
                }
            }
            CloseClipboard();
        }
#elif __APPLE__
        // macOS implementation (stub)
        LogMessage(LogLevel::WARNING, "GetClipboardText not implemented for macOS");
#else
        // Linux implementation (stub)
        LogMessage(LogLevel::WARNING, "GetClipboardText not implemented for Linux");
#endif
        LogMessage(LogLevel::INFO, "Clipboard text retrieved");
        return result;
    }
    
private:
    NetworkManager& _networkManager;
};

// Chat functionality
class ChatManager {
public:
    struct ChatMessage {
        std::string sender;
        std::string content;
        std::chrono::system_clock::time_point timestamp;
    };
    
    ChatManager(NetworkManager& networkManager) : _networkManager(networkManager) {
        LogMessage(LogLevel::INFO, "Chat manager initialized");
    }
    
    bool SendMessage(int clientSocket, const std::string& sender, const std::string& message) {
        LogMessage(LogLevel::INFO, "Sending chat message from: " + sender);
        
        // Format: [sender]|[timestamp]|[message]
        auto now = std::chrono::system_clock::now();
        auto timestamp = std::chrono::system_clock::to_time_t(now);
        std::string formattedMessage = sender + "|" + std::to_string(timestamp) + "|" + message;
        
        std::vector<uint8_t> chatData(formattedMessage.begin(), formattedMessage.end());
        bool result = _networkManager.SendData(clientSocket, chatData);
        
        // Store in history
        if (result) {
            _messageHistory.push_back({sender, message, now});
            if (_messageHistory.size() > MAX_CHAT_HISTORY) {
                _messageHistory.pop_front();
            }
        }
        
        return result;
    }
    
    ChatMessage ReceiveMessage(int clientSocket) {
        auto data = _networkManager.ReceiveData(clientSocket);
        std::string messageStr(data.begin(), data.end());
        
        // Parse message format: [sender]|[timestamp]|[message]
        size_t pos1 = messageStr.find('|');
        size_t pos2 = messageStr.find('|', pos1 + 1);
        
        if (pos1 != std::string::npos && pos2 != std::string::npos) {
            std::string sender = messageStr.substr(0, pos1);
            std::string timestampStr = messageStr.substr(pos1 + 1, pos2 - pos1 - 1);
            std::string content = messageStr.substr(pos2 + 1);
            
            auto timestamp = std::chrono::system_clock::from_time_t(std::stoll(timestampStr));
            ChatMessage message = {sender, content, timestamp};
            
            // Store in history
            _messageHistory.push_back(message);
            if (_messageHistory.size() > MAX_CHAT_HISTORY) {
                _messageHistory.pop_front();
            }
            
            LogMessage(LogLevel::INFO, "Received chat message from: " + sender);
            return message;
        }
        
        return {"system", "Invalid message format", std::chrono::system_clock::now()};
    }
    
    const std::list<ChatMessage>& GetMessageHistory() const {
        return _messageHistory;
    }
    
private:
    static const size_t MAX_CHAT_HISTORY = 100;
    NetworkManager& _networkManager;
    std::list<ChatMessage> _messageHistory;
};

// Session recording functionality
class SessionRecorder {
public:
    SessionRecorder() : _recording(false) {
        LogMessage(LogLevel::INFO, "Session recorder initialized");
    }
    
    void StartRecording(const std::string& filename) {
        if (_recording) return;
        
        _filename = filename;
        _recordFile.open(filename, std::ios::binary);
        if (!_recordFile) {
            LogMessage(LogLevel::ERROR, "Failed to open recording file: " + filename);
            return;
        }
        
        _recording = true;
        _startTime = std::chrono::system_clock::now();
        LogMessage(LogLevel::INFO, "Session recording started: " + filename);
    }
    
    void StopRecording() {
        if (!_recording) return;
        
        _recordFile.close();
        _recording = false;
        LogMessage(LogLevel::INFO, "Session recording stopped: " + _filename);
    }
    
    void RecordFrame(const std::vector<uint8_t>& frameData) {
        if (!_recording || !_recordFile) return;
        
        // Record timestamp
        auto now = std::chrono::system_clock::now();
        auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(now - _startTime).count();
        
        // Format: [timestamp(8 bytes)][size(4 bytes)][data]
        _recordFile.write(reinterpret_cast<char*>(&elapsed), sizeof(elapsed));
        
        uint32_t size = static_cast<uint32_t>(frameData.size());
        _recordFile.write(reinterpret_cast<char*>(&size), sizeof(size));
        
        _recordFile.write(reinterpret_cast<const char*>(frameData.data()), frameData.size());
    }
    
    void RecordEvent(const InputHandler::InputEvent& event) {
        if (!_recording || !_recordFile) return;
        
        // Record timestamp
        auto now = std::chrono::system_clock::now();
        auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(now - _startTime).count();
        
        // Format for event: [timestamp(8 bytes)][event_type(1 byte)][event_data(12 bytes)]
        _recordFile.write(reinterpret_cast<char*>(&elapsed), sizeof(elapsed));
        
        uint8_t eventType = static_cast<uint8_t>(event.type);
        _recordFile.write(reinterpret_cast<char*>(&eventType), sizeof(eventType));
        
        // Write x, y, data
        _recordFile.write(reinterpret_cast<const char*>(&event.x), sizeof(event.x));
        _recordFile.write(reinterpret_cast<const char*>(&event.y), sizeof(event.y));
        _recordFile.write(reinterpret_cast<const char*>(&event.data), sizeof(event.data));
    }
    
private:
    bool _recording;
    std::string _filename;
    std::ofstream _recordFile;
    std::chrono::system_clock::time_point _startTime;
};

// UI Theme and Appearance manager
class UiManager {
public:
    enum class Theme { LIGHT, DARK, SYSTEM, CUSTOM };
    
    struct UiColors {
        uint32_t background;
        uint32_t foreground;
        uint32_t accent;
        uint32_t highlight;
    };
    
    UiManager() : _currentTheme(Theme::SYSTEM) {
        // Default light theme
        _lightTheme = {0xFFFFFF, 0x000000, 0x007ACC, 0xE6F3FF};
        
        // Default dark theme
        _darkTheme = {0x1E1E1E, 0xFFFFFF, 0x007ACC, 0x3F3F3F};
        
        // Set current colors based on system preference
        _currentColors = IsSystemDarkMode() ? _darkTheme : _lightTheme;
        
        LogMessage(LogLevel::INFO, "UI manager initialized");
    }
    
    void SetTheme(Theme theme) {
        _currentTheme = theme;
        switch (theme) {
            case Theme::LIGHT:
                _currentColors = _lightTheme;
                break;
            case Theme::DARK:
                _currentColors = _darkTheme;
                break;
            case Theme::SYSTEM:
                _currentColors = IsSystemDarkMode() ? _darkTheme : _lightTheme;
                break;
            case Theme::CUSTOM:
                // Keep current custom colors
                break;
        }
        LogMessage(LogLevel::INFO, "Theme changed to: " + std::to_string(static_cast<int>(theme)));
    }
    
    void SetCustomColors(const UiColors& colors) {
        _customTheme = colors;
        if (_currentTheme == Theme::CUSTOM) {
            _currentColors = colors;
        }
        LogMessage(LogLevel::INFO, "Custom theme colors set");
    }
    
    const UiColors& GetCurrentColors() const {
        return _currentColors;
    }
    
private:
    bool IsSystemDarkMode() const {
#ifdef _WIN32
        // Windows 10+ dark mode detection
        HKEY hKey;
        DWORD value = 0;
        DWORD size = sizeof(value);
        if (RegOpenKeyEx(HKEY_CURRENT_USER, "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", 0, KEY_READ, &hKey) == ERROR_SUCCESS) {
            RegQueryValueEx(hKey, "AppsUseLightTheme", NULL, NULL, reinterpret_cast<LPBYTE>(&value), &size);
            RegCloseKey(hKey);
            return value == 0; // 0 = dark mode
        }
        return false;
#elif __APPLE__
        // macOS dark mode detection would go here
        return false;
#else
        // Linux dark mode detection would go here
        return false;
#endif
    }
    
    Theme _currentTheme;
    UiColors _currentColors;
    UiColors _lightTheme;
    UiColors _darkTheme;
    UiColors _customTheme;
};

// Multi-monitor support
class MultiMonitorManager {
public:
    struct Monitor {
        int id;
        int x;
        int y;
        int width;
        int height;
        bool isPrimary;
    };
    
    MultiMonitorManager() {
        RefreshMonitors();
        LogMessage(LogLevel::INFO, "Multi-monitor manager initialized");
    }
    
    void RefreshMonitors() {
        _monitors.clear();
        
#ifdef _WIN32
        // Windows implementation
        EnumDisplayMonitors(NULL, NULL, 
            [](HMONITOR hMonitor, HDC hdcMonitor, LPRECT lprcMonitor, LPARAM dwData) -> BOOL {
                auto monitors = reinterpret_cast<std::vector<Monitor>*>(dwData);
                MONITORINFO mi;
                mi.cbSize = sizeof(MONITORINFO);
                if (GetMonitorInfo(hMonitor, &mi)) {
                    Monitor monitor;
                    monitor.id = static_cast<int>(monitors->size());
                    monitor.x = mi.rcMonitor.left;
                    monitor.y = mi.rcMonitor.top;
                    monitor.width = mi.rcMonitor.right - mi.rcMonitor.left;
                    monitor.height = mi.rcMonitor.bottom - mi.rcMonitor.top;
                    monitor.isPrimary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0;
                    monitors->push_back(monitor);
                }
                return TRUE;
            }, 
            reinterpret_cast<LPARAM>(&_monitors));
#elif __APPLE__
        // macOS implementation would go here
        _monitors.push_back({0, 0, 0, 1440, 900, true}); // Example
#else
        // Linux X11 implementation
        if (Display* display = XOpenDisplay(nullptr)) {
            int screen = XDefaultScreen(display);
            _monitors.push_back({
                0, 0, 0, 
                XDisplayWidth(display, screen),
                XDisplayHeight(display, screen),
                true
            });
            XCloseDisplay(display);
        }
#endif
        
        LogMessage(LogLevel::INFO, "Found " + std::to_string(_monitors.size()) + " monitors");
    }
    
    const std::vector<Monitor>& GetMonitors() const {
        return _monitors;
    }
    
    const Monitor* GetPrimaryMonitor() const {
        for (const auto& monitor : _monitors) {
            if (monitor.isPrimary) return &monitor;
        }
        return _monitors.empty() ? nullptr : &_monitors[0];
    }
    
    const Monitor* GetMonitorById(int id) const {
        for (const auto& monitor : _monitors) {
            if (monitor.id == id) return &monitor;
        }
        return nullptr;
    }
    
private:
    std::vector<Monitor> _monitors;
};

// Session handler
class Session {
public:
    Session(int clientSocket) 
        : _clientSocket(clientSocket), _running(false), 
          _fileTransfer(_networkManager), _remoteClipboard(_networkManager),
          _chatManager(_networkManager) {
        LogMessage(LogLevel::INFO, "New session created");
    }
    
    ~Session() {
        Stop();
    }
    
    bool Start() {
        if (_running) return false;
        
        _running = true;
        _screenThread = std::thread(&Session::ScreenCaptureLoop, this);
        _inputThread = std::thread(&Session::InputProcessingLoop, this);
        
        LogMessage(LogLevel::INFO, "Session started");
        return true;
    }
    
    void Stop() {
        if (!_running) return;
        
        _running = false;
        
        if (_screenThread.joinable()) {
            _screenThread.join();
        }
        
        if (_inputThread.joinable()) {
            _inputThread.join();
        }
        
#ifdef _WIN32
        closesocket(_clientSocket);
#else
        close(_clientSocket);
#endif
        
        LogMessage(LogLevel::INFO, "Session stopped");
    }
    
    bool Authenticate(const std::string& username, const std::string& password) {
        if (_authManager.Authenticate(username, password)) {
            _sessionId = _authManager.CreateSession(username);
            LogMessage(LogLevel::INFO, "Client authenticated");
            return true;
        }
        return false;
    }
    
    bool TransferFile(const FileTransfer::FileTransferRequest& request) {
        return _fileTransfer.StartFileTransfer(_clientSocket, request);
    }
    
    bool SyncClipboard(const std::string& text) {
        _remoteClipboard.SetClipboardText(text);
        return _remoteClipboard.SendClipboardData(_clientSocket, text);
    }
    
    std::string GetRemoteClipboard() {
        return _remoteClipboard.ReceiveClipboardData(_clientSocket);
    }
    
    bool StartRecording(const std::string& filename) {
        _sessionRecorder.StartRecording(filename);
        return true;
    }
    
    void StopRecording() {
        _sessionRecorder.StopRecording();
    }
    
    bool SendChatMessage(const std::string& sender, const std::string& message) {
        return _chatManager.SendMessage(_clientSocket, sender, message);
    }
    
    ChatManager::ChatMessage ReceiveChatMessage() {
        return _chatManager.ReceiveMessage(_clientSocket);
    }
    
    void SetTheme(UiManager::Theme theme) {
        _uiManager.SetTheme(theme);
    }
    
    void SetCustomColors(const UiManager::UiColors& colors) {
        _uiManager.SetCustomColors(colors);
    }
    
    const UiManager::UiColors& GetThemeColors() const {
        return _uiManager.GetCurrentColors();
    }
    
    const std::vector<MultiMonitorManager::Monitor>& GetMonitors() {
        _multiMonitorManager.RefreshMonitors();
        return _multiMonitorManager.GetMonitors();
    }
    
    std::vector<uint8_t> CaptureMonitor(int monitorId) {
        const auto* monitor = _multiMonitorManager.GetMonitorById(monitorId);
        if (!monitor) return std::vector<uint8_t>();
        
        // In a real implementation, would capture the specific monitor
        return _screenCapture.CaptureScreen();
    }
    
private:
    void ScreenCaptureLoop() {
        while (_running) {
            // Capture screen
            auto screenData = _screenCapture.CaptureScreen();
            
            // Record frame if recording
            _sessionRecorder.RecordFrame(screenData);
            
            // Compress screen data (stub)
            // In a real implementation, would use a proper compression library
            
            // Encrypt screen data
            auto encryptedData = _encryptionManager.Encrypt(screenData);
            
            // Send to client
            _networkManager.SendData(_clientSocket, encryptedData);
            
            // Sleep to avoid excessive CPU usage
            std::this_thread::sleep_for(std::chrono::milliseconds(SCREEN_UPDATE_INTERVAL_MS));
        }
    }
    
    void InputProcessingLoop() {
        while (_running) {
            // Receive input events from client
            auto encryptedData = _networkManager.ReceiveData(_clientSocket);
            if (encryptedData.empty()) {
                // Connection closed
                _running = false;
                break;
            }
            
            // Decrypt data
            auto data = _encryptionManager.Decrypt(encryptedData);
            
            // Process input event
            // In a real implementation, would parse the event data properly
            if (data.size() >= sizeof(InputHandler::InputEvent)) {
                InputHandler::InputEvent event;
                memcpy(&event, data.data(), sizeof(event));
                _sessionRecorder.RecordEvent(event);
                _inputHandler.ProcessInput(event);
            }
        }
    }
    
    int _clientSocket;
    std::atomic<bool> _running;
    std::string _sessionId;
    std::thread _screenThread;
    std::thread _inputThread;
    
    AuthenticationManager _authManager;
    ScreenCapture _screenCapture;
    InputHandler _inputHandler;
    NetworkManager _networkManager;
    EncryptionManager _encryptionManager;
    FileTransfer _fileTransfer;
    RemoteClipboard _remoteClipboard;
    SessionRecorder _sessionRecorder;
    ChatManager _chatManager;
    UiManager _uiManager;
    MultiMonitorManager _multiMonitorManager;
};

// Main RemoteDesktopServer class
class RemoteDesktopServer {
public:
    RemoteDesktopServer() : _running(false) {
        _networkManager.SetConnectionCallback(
            [this](int clientSocket) { this->HandleNewConnection(clientSocket); }
        );
    }
    
    bool Start(int port = DEFAULT_PORT) {
        if (_running) return false;
        
        LogMessage(LogLevel::INFO, "Remote Desktop Server v" + SERVER_VERSION + " starting...");
        
        if (!_networkManager.Start(port)) {
            LogMessage(LogLevel::ERROR, "Failed to start network manager");
            return false;
        }
        
        _running = true;
        
        LogMessage(LogLevel::INFO, "Remote Desktop Server started");
        return true;
    }
    
    void Stop() {
        if (!_running) return;
        
        _running = false;
        _networkManager.Stop();
        
        // Stop all sessions
        for (auto& session : _sessions) {
            session.second->Stop();
        }
        _sessions.clear();
        
        LogMessage(LogLevel::INFO, "Remote Desktop Server stopped");
    }
    
    void HandleNewConnection(int clientSocket) {
        LogMessage(LogLevel::INFO, "Handling new connection");
        
        // Create new session
        auto session = std::make_shared<Session>(clientSocket);
        _sessions[clientSocket] = session;
        
        // Start session
        session->Start();
    }
    
    // NAT traversal helper
    void SetupNatTraversal(const std::string& stunServer) {
        // In a real implementation, would use a STUN/TURN library
        LogMessage(LogLevel::INFO, "Setting up NAT traversal with STUN server: " + stunServer);
    }
    
    // User consent handling
    bool RequestUserConsent() {
        // In a real implementation, would show a UI dialog to the local user
        LogMessage(LogLevel::INFO, "User consent requested");
        return true;
    }
    
    // Admin override
    void OverrideUserConsent(const std::string& adminId) {
        LogMessage(LogLevel::WARNING, "Admin override by: " + adminId);
    }
    
    // Implement the IPC methods for RemoteDesktopServer
    void RegisterWithAgentCore(const std::string& agentId) {
        _agentId = agentId;
        LogMessage(LogLevel::INFO, "Registering RemoteDesktopServer with AgentCore, ID: " + agentId);
        
        // In a real implementation, would use the IPC framework to register with AgentCore
        try {
            // Access IPC framework if available
            #ifdef ENABLE_IPC
            using namespace Sysguard::IPC;
            if (g_ipcBroker) {
                g_ipcBroker->RegisterModule("RemoteDesktopServer");
                
                // Register message handler
                g_ipcBroker->RegisterHandler("RemoteDesktopServer", [this](const Message& message) {
                    if (message.type == MessageType::COMMAND) {
                        this->ProcessCommandFromAgentCore(message.payload);
                        
                        // Send response
                        auto response = Message::CreateResponse(message, "Command processed");
                        g_ipcBroker->SendMessage(response);
                    }
                });
                
                // Send registration message
                Message regMsg;
                regMsg.sourceModule = "RemoteDesktopServer";
                regMsg.targetModule = "AgentCore";
                regMsg.type = MessageType::REGISTER;
                regMsg.payload = "{ \"id\": \"" + _agentId + "\", \"features\": [\"remote-desktop\", \"file-transfer\", \"chat\"] }";
                g_ipcBroker->SendMessage(regMsg);
            }
            #endif
        } catch (const std::exception& e) {
            LogMessage(LogLevel::ERROR, "Error registering with AgentCore: " + std::string(e.what()));
        }
    }

    void SendStatusToAgentCore() {
        LogMessage(LogLevel::INFO, "Sending status update to AgentCore");
        
        // Build status JSON
        std::string statusJson = "{ ";
        statusJson += "\"id\": \"" + _agentId + "\", ";
        statusJson += "\"status\": \"online\", ";
        statusJson += "\"activeSessions\": " + std::to_string(_sessions.size()) + ", ";
        statusJson += "\"version\": \"" + SERVER_VERSION + "\" ";
        statusJson += "}";
        
        // In a real implementation, would use the IPC framework to send status
        try {
            #ifdef ENABLE_IPC
            using namespace Sysguard::IPC;
            if (g_ipcBroker) {
                auto statusMsg = Message::CreateStatus("RemoteDesktopServer", statusJson);
                g_ipcBroker->SendMessage(statusMsg);
            }
            #endif
        } catch (const std::exception& e) {
            LogMessage(LogLevel::ERROR, "Error sending status to AgentCore: " + std::string(e.what()));
        }
    }

    void ProcessCommandFromAgentCore(const std::string& command) {
        LogMessage(LogLevel::INFO, "Processing command from AgentCore: " + command);
        
        // Simple command parser
        if (command.find("shutdown") != std::string::npos) {
            Stop();
        } else if (command.find("status") != std::string::npos) {
            SendStatusToAgentCore();
        } else if (command.find("disconnect_all") != std::string::npos) {
            // Disconnect all sessions
            for (auto& session : _sessions) {
                session.second->Stop();
            }
            _sessions.clear();
        } else {
            LogMessage(LogLevel::WARNING, "Unknown command: " + command);
        }
    }
    
    // C# interoperability methods implementation
    void RegisterManagedCallback(ManagedCallback callback) {
        std::lock_guard<std::mutex> lock(_callbackMutex);
        _managedCallback = callback;
        LogMessage(LogLevel::INFO, "Managed callback registered");
        
        // Report current status via callback
        if (_managedCallback) {
            std::string status = "{ \"running\": " + std::string(_running ? "true" : "false") + 
                               ", \"port\": " + std::to_string(_port) + 
                               ", \"sessions\": " + std::to_string(GetSessionCount()) + " }";
            _managedCallback("status", status.c_str());
        }
    }

    bool ExecuteCommand(const char* command, char* response, int responseSize) {
        if (!command || !response || responseSize <= 0) {
            LogMessage(LogLevel::ERROR, "Invalid parameters for ExecuteCommand");
            return false;
        }
        
        std::string cmd(command);
        std::string result;
        
        LogMessage(LogLevel::INFO, "Executing command from managed code: " + cmd);
        
        // Simple command parsing
        if (cmd == "status") {
            result = "{ \"running\": " + std::string(_running ? "true" : "false") + 
                    ", \"port\": " + std::to_string(_port) + 
                    ", \"sessions\": " + std::to_string(GetSessionCount()) + " }";
        }
        else if (cmd == "start") {
            if (_running) {
                result = "{ \"success\": false, \"message\": \"Server already running\" }";
            } else {
                bool success = Start(_port);
                result = "{ \"success\": " + std::string(success ? "true" : "false") + 
                        ", \"message\": \"" + (success ? "Server started" : "Failed to start server") + "\" }";
            }
        }
        else if (cmd == "stop") {
            if (!_running) {
                result = "{ \"success\": false, \"message\": \"Server not running\" }";
            } else {
                Stop();
                result = "{ \"success\": true, \"message\": \"Server stopped\" }";
            }
        }
        else if (cmd.compare(0, 9, "setport: ") == 0) {
            // Extract port number
            try {
                int newPort = std::stoi(cmd.substr(9));
                if (newPort > 0 && newPort < 65536) {
                    _port = newPort;
                    result = "{ \"success\": true, \"message\": \"Port set to " + std::to_string(_port) + "\" }";
                } else {
                    result = "{ \"success\": false, \"message\": \"Invalid port number\" }";
                }
            } catch (const std::exception&) {
                result = "{ \"success\": false, \"message\": \"Invalid port format\" }";
            }
        }
        else if (cmd == "list_sessions") {
            std::lock_guard<std::mutex> lock(_sessionsMutex);
            result = "{ \"sessions\": [";
            bool first = true;
            for (const auto& pair : _sessions) {
                if (!first) result += ",";
                result += "{ \"id\": \"" + pair.first + "\" }";
                first = false;
            }
            result += "] }";
        }
        else if (cmd.compare(0, 16, "disconnect_session:") == 0) {
            std::string sessionId = cmd.substr(16);
            bool success = RemoveSession(sessionId);
            result = "{ \"success\": " + std::string(success ? "true" : "false") + 
                    ", \"message\": \"" + (success ? "Session disconnected" : "Session not found") + "\" }";
        }
        else {
            result = "{ \"success\": false, \"message\": \"Unknown command\" }";
        }
        
        // Copy result to response buffer
        strncpy(response, result.c_str(), responseSize - 1);
        response[responseSize - 1] = '\0'; // Ensure null termination
        
        return true;
    }

    const char* GetServerInfo() {
        UpdateServerInfoCache();
        return _serverInfoCache.c_str();
    }

    void UpdateServerInfoCache() const {
        std::lock_guard<std::mutex> lock(_serverInfoMutex);
        
        _serverInfoCache = "{ ";
        _serverInfoCache += "\"version\":\"" + std::string(SERVER_VERSION) + "\",";
        _serverInfoCache += "\"running\":" + std::string(_running ? "true" : "false") + ",";
        _serverInfoCache += "\"port\":" + std::to_string(_port) + ",";
        _serverInfoCache += "\"sessionCount\":" + std::to_string(GetSessionCount());
        
        // Add feature flags
        _serverInfoCache += ",\"features\":{";
        _serverInfoCache += "\"fileTransfer\":true,";
        _serverInfoCache += "\"chat\":true,";
        _serverInfoCache += "\"sessionRecording\":true,";
        _serverInfoCache += "\"multiMonitor\":true,";
        _serverInfoCache += "\"remoteClipboard\":true,";
        _serverInfoCache += "\"theming\":true";
        _serverInfoCache += "}";
        
        _serverInfoCache += "}";
    }

    bool StartIPCServer(int ipcPort) {
        if (_ipcRunning) {
            LogMessage(LogLevel::WARNING, "IPC server already running");
            return true;
        }
        
        _ipcPort = ipcPort;
        _ipcRunning = true;
        
        LogMessage(LogLevel::INFO, "Starting IPC server on port " + std::to_string(_ipcPort));
        
        _ipcThread = std::thread(&RemoteDesktopServer::IPCServerThread, this);
        return true;
    }

    void StopIPCServer() {
        if (!_ipcRunning) {
            return;
        }
        
        _ipcRunning = false;
        
        if (_ipcThread.joinable()) {
            _ipcThread.join();
        }
        
        LogMessage(LogLevel::INFO, "IPC server stopped");
    }

    void IPCServerThread() {
        LogMessage(LogLevel::INFO, "IPC server thread started");
        
#ifdef _WIN32
        // Named pipe implementation for Windows
        std::string pipeName = "\\\\.\\pipe\\SysguardRemoteDesktopServer";
        
        while (_ipcRunning) {
            HANDLE pipe = CreateNamedPipeA(
                pipeName.c_str(),
                PIPE_ACCESS_DUPLEX,
                PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
                PIPE_UNLIMITED_INSTANCES,
                4096,
                4096,
                0,
                NULL
            );
            
            if (pipe == INVALID_HANDLE_VALUE) {
                LogMessage(LogLevel::ERROR, "Failed to create named pipe: " + std::to_string(GetLastError()));
                std::this_thread::sleep_for(std::chrono::seconds(1));
                continue;
            }
            
            LogMessage(LogLevel::DEBUG, "Waiting for client connection on named pipe");
            
            // Wait for client connection
            BOOL connected = ConnectNamedPipe(pipe, NULL) ? TRUE : (GetLastError() == ERROR_PIPE_CONNECTED);
            
            if (connected) {
                LogMessage(LogLevel::INFO, "Client connected to named pipe");
                
                // Handle client request in a separate thread to continue accepting
                std::thread([this, pipe]() {
                    char buffer[4096];
                    DWORD bytesRead;
                    BOOL success = ReadFile(pipe, buffer, sizeof(buffer) - 1, &bytesRead, NULL);
                    
                    if (success && bytesRead > 0) {
                        buffer[bytesRead] = '\0';
                        
                        char response[4096];
                        ExecuteCommand(buffer, response, sizeof(response));
                        
                        DWORD bytesWritten;
                        WriteFile(pipe, response, strlen(response), &bytesWritten, NULL);
                    }
                    
                    FlushFileBuffers(pipe);
                    DisconnectNamedPipe(pipe);
                    CloseHandle(pipe);
                }).detach();
            } else {
                CloseHandle(pipe);
            }
        }
#else
        // TCP socket implementation for Unix-based systems
        int serverSocket = socket(AF_INET, SOCK_STREAM, 0);
        if (serverSocket < 0) {
            LogMessage(LogLevel::ERROR, "Failed to create IPC server socket");
            return;
        }
        
        // Allow socket reuse
        int opt = 1;
        setsockopt(serverSocket, SOL_SOCKET, SO_REUSEADDR, &opt, sizeof(opt));
        
        struct sockaddr_in serverAddr;
        serverAddr.sin_family = AF_INET;
        serverAddr.sin_addr.s_addr = INADDR_ANY;
        serverAddr.sin_port = htons(_ipcPort);
        
        if (bind(serverSocket, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) < 0) {
            LogMessage(LogLevel::ERROR, "Failed to bind IPC server socket");
            close(serverSocket);
            return;
        }
        
        if (listen(serverSocket, 5) < 0) {
            LogMessage(LogLevel::ERROR, "Failed to listen on IPC server socket");
            close(serverSocket);
            return;
        }
        
        LogMessage(LogLevel::INFO, "IPC server listening on port " + std::to_string(_ipcPort));
        
        // Set non-blocking mode for accept
        int flags = fcntl(serverSocket, F_GETFL, 0);
        fcntl(serverSocket, F_SETFL, flags | O_NONBLOCK);
        
        while (_ipcRunning) {
            struct sockaddr_in clientAddr;
            socklen_t clientAddrLen = sizeof(clientAddr);
            int clientSocket = accept(serverSocket, (struct sockaddr*)&clientAddr, &clientAddrLen);
            
            if (clientSocket < 0) {
                if (errno != EAGAIN && errno != EWOULDBLOCK) {
                    LogMessage(LogLevel::WARNING, "Error accepting IPC connection: " + std::string(strerror(errno)));
                }
                std::this_thread::sleep_for(std::chrono::milliseconds(100));
                continue;
            }
            
            LogMessage(LogLevel::INFO, "IPC client connected");
            
            // Handle client in a separate thread
            std::thread([this, clientSocket]() {
                char buffer[4096];
                ssize_t bytesRead = recv(clientSocket, buffer, sizeof(buffer) - 1, 0);
                
                if (bytesRead > 0) {
                    buffer[bytesRead] = '\0';
                    
                    char response[4096];
                    ExecuteCommand(buffer, response, sizeof(response));
                    
                    send(clientSocket, response, strlen(response), 0);
                }
                
                close(clientSocket);
            }).detach();
        }
        
        close(serverSocket);
#endif
        
        LogMessage(LogLevel::INFO, "IPC server thread exited");
    }

    // JSON configuration methods
    bool LoadConfigFromJson(const std::string& jsonConfig) {
        LogMessage(LogLevel::INFO, "Loading configuration from JSON");
        
        // In a real implementation, would use a JSON parser library
        // Here's a simple parsing example assuming well-formed JSON
        
        // Example: { "port": 8900, "featureFlags": { "chat": true, ... } }
        
        // Parse port
        size_t portPos = jsonConfig.find("\"port\":");
        if (portPos != std::string::npos) {
            size_t valueStart = jsonConfig.find_first_of("0123456789", portPos + 6);
            size_t valueEnd = jsonConfig.find_first_not_of("0123456789", valueStart);
            
            if (valueStart != std::string::npos && valueEnd != std::string::npos) {
                std::string portStr = jsonConfig.substr(valueStart, valueEnd - valueStart);
                try {
                    _port = std::stoi(portStr);
                    LogMessage(LogLevel::INFO, "Set port to " + std::to_string(_port));
                } catch (const std::exception&) {
                    LogMessage(LogLevel::ERROR, "Invalid port in JSON config");
                }
            }
        }
        
        // In a real implementation, would parse more configuration options
        
        return true;
    }

    std::string SaveConfigToJson() const {
        std::string config = "{\n";
        config += "  \"port\": " + std::to_string(_port) + ",\n";
        config += "  \"agentId\": \"" + _agentId + "\",\n";
        config += "  \"featureFlags\": {\n";
        config += "    \"chat\": true,\n";
        config += "    \"sessionRecording\": true,\n";
        config += "    \"multiMonitor\": true,\n";
        config += "    \"fileTransfer\": true,\n";
        config += "    \"remoteClipboard\": true,\n";
        config += "    \"theming\": true\n";
        config += "  }\n";
        config += "}";
        
        return config;
    }

    // Implement SerializableMessage methods
    std::vector<uint8_t> SerializableMessage::Serialize() const {
        // Format: [type:1][source_len:2][target_len:2][content_len:4][timestamp:8][source][target][content]
        std::vector<uint8_t> result;
        
        // Reserve space to avoid reallocations
        size_t size = 1 + 2 + 2 + 4 + 8 + source.size() + target.size() + content.size();
        result.reserve(size);
        
        // Message type
        result.push_back(static_cast<uint8_t>(type));
        
        // Source length (2 bytes)
        uint16_t sourceLen = static_cast<uint16_t>(source.size());
        result.push_back(static_cast<uint8_t>(sourceLen & 0xFF));
        result.push_back(static_cast<uint8_t>((sourceLen >> 8) & 0xFF));
        
        // Target length (2 bytes)
        uint16_t targetLen = static_cast<uint16_t>(target.size());
        result.push_back(static_cast<uint8_t>(targetLen & 0xFF));
        result.push_back(static_cast<uint8_t>((targetLen >> 8) & 0xFF));
        
        // Content length (4 bytes)
        uint32_t contentLen = static_cast<uint32_t>(content.size());
        result.push_back(static_cast<uint8_t>(contentLen & 0xFF));
        result.push_back(static_cast<uint8_t>((contentLen >> 8) & 0xFF));
        result.push_back(static_cast<uint8_t>((contentLen >> 16) & 0xFF));
        result.push_back(static_cast<uint8_t>((contentLen >> 24) & 0xFF));
        
        // Timestamp (8 bytes)
        for (int i = 0; i < 8; ++i) {
            result.push_back(static_cast<uint8_t>((timestamp >> (i * 8)) & 0xFF));
        }
        
        // Source
        result.insert(result.end(), source.begin(), source.end());
        
        // Target
        result.insert(result.end(), target.begin(), target.end());
        
        // Content
        result.insert(result.end(), content.begin(), content.end());
        
        return result;
    }

    SerializableMessage SerializableMessage::Deserialize(const std::vector<uint8_t>& data) {
        SerializableMessage msg;
        
        if (data.size() < 17) { // Minimum size: type(1) + source_len(2) + target_len(2) + content_len(4) + timestamp(8)
            return msg; // Invalid message
        }
        
        // Message type
        msg.type = static_cast<MessageType>(data[0]);
        
        // Source length
        uint16_t sourceLen = static_cast<uint16_t>(data[1]) | (static_cast<uint16_t>(data[2]) << 8);
        
        // Target length
        uint16_t targetLen = static_cast<uint16_t>(data[3]) | (static_cast<uint16_t>(data[4]) << 8);
        
        // Content length
        uint32_t contentLen = static_cast<uint32_t>(data[5]) | 
                            (static_cast<uint32_t>(data[6]) << 8) | 
                            (static_cast<uint32_t>(data[7]) << 16) | 
                            (static_cast<uint32_t>(data[8]) << 24);
        
        // Timestamp
        msg.timestamp = 0;
        for (int i = 0; i < 8; ++i) {
            msg.timestamp |= (static_cast<uint64_t>(data[9 + i]) << (i * 8));
        }
        
        // Validate total size
        size_t expectedSize = 17 + sourceLen + targetLen + contentLen;
        if (data.size() < expectedSize) {
            return SerializableMessage(); // Invalid message
        }
        
        // Source
        msg.source.assign(reinterpret_cast<const char*>(&data[17]), sourceLen);
        
        // Target
        msg.target.assign(reinterpret_cast<const char*>(&data[17 + sourceLen]), targetLen);
        
        // Content
        msg.content.assign(reinterpret_cast<const char*>(&data[17 + sourceLen + targetLen]), contentLen);
        
        return msg;
    }

private:
    std::atomic<bool> _running;
    NetworkManager _networkManager;
    std::map<int, std::shared_ptr<Session>> _sessions;
    std::string _agentId;
    int _port = DEFAULT_PORT;
    std::mutex _sessionsMutex;
    std::mutex _callbackMutex;
    std::mutex _serverInfoMutex;
    std::string _serverInfoCache;
    ManagedCallback _managedCallback;
    bool _ipcRunning = false;
    int _ipcPort = 0;
    std::thread _ipcThread;
};

// Main function
int main() {
    RemoteDesktopServer server;
    if (server.Start(DEFAULT_PORT)) {
        LogMessage(LogLevel::INFO, "Press Enter to stop the server...");
        std::cin.get();
        server.Stop();
    }
    return 0;
}