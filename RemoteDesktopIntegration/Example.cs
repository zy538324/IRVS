using System;
using System.Threading;
using Sysguard.RemoteDesktop;

namespace Sysguard.Examples
{
    class RemoteDesktopExample
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Remote Desktop Server Example");
            
            // Create and initialize the RemoteDesktopManager
            using (var rdpManager = new RemoteDesktopManager())
            {
                // Set a unique agent ID
                rdpManager.SetAgentId(Guid.NewGuid().ToString());
                
                // Register for notifications
                rdpManager.OnNotification += (msgType, content) => {
                    Console.WriteLine($"Notification: {msgType} - {content}");
                };
                
                // Start the server
                int port = 8900;
                Console.WriteLine($"Starting Remote Desktop Server on port {port}...");
                if (rdpManager.Start(port))
                {
                    Console.WriteLine("Server started successfully.");
                    
                    // Start the IPC server for local process communication
                    rdpManager.StartIPC(8901);
                    
                    // Get and display server information
                    var serverInfo = rdpManager.GetServerInfo();
                    Console.WriteLine("Server Information:");
                    foreach (var kvp in serverInfo)
                    {
                        Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                    }
                    
                    Console.WriteLine("Press Enter to stop the server...");
                    Console.ReadLine();
                    
                    // List any active sessions
                    var sessions = rdpManager.GetSessions();
                    Console.WriteLine($"Active Sessions: {sessions.Count}");
                    foreach (var session in sessions)
                    {
                        Console.WriteLine($"  Session ID: {session}");
                        
                        // Option to disconnect sessions
                        Console.Write($"  Disconnect this session? (y/n): ");
                        if (Console.ReadLine().ToLower() == "y")
                        {
                            rdpManager.DisconnectSession(session);
                            Console.WriteLine("  Session disconnected.");
                        }
                    }
                    
                    // Stop the server
                    rdpManager.Stop();
                    rdpManager.StopIPC();
                    Console.WriteLine("Server stopped.");
                }
                else
                {
                    Console.WriteLine("Failed to start the server.");
                }
            }
            
            Console.WriteLine("Example completed. Press Enter to exit...");
            Console.ReadLine();
        }
    }
}