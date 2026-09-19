using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace HeyClicky.ComputerControl
{
    public class KeyboardController
    {
        public static void PressKey(ushort key)
        {
            SendKeyEvent(key, false);
            Thread.Sleep(50);
            SendKeyEvent(key, true);
        }

        public static void TypeText(string text)
        {
            foreach (char c in text)
            {
                NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[2];
                
                inputs[0].type = NativeMethods.INPUT_KEYBOARD;
                inputs[0].u.ki.wScan = (ushort)c;
                inputs[0].u.ki.dwFlags = (uint)NativeMethods.KeyEventFlags.UNICODE;

                inputs[1].type = NativeMethods.INPUT_KEYBOARD;
                inputs[1].u.ki.wScan = (ushort)c;
                inputs[1].u.ki.dwFlags = (uint)(NativeMethods.KeyEventFlags.UNICODE | NativeMethods.KeyEventFlags.KEYUP);

                NativeMethods.SendInput(2, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
                Thread.Sleep(20);
            }
        }

        public static void PressWindowsKey()
        {
            PressKey(NativeMethods.VK_LWIN);
        }

        public static void PressEnter()
        {
            PressKey(NativeMethods.VK_RETURN);
        }

        private static void SendKeyEvent(ushort key, bool keyUp)
        {
            NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[1];
            inputs[0].type = NativeMethods.INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = key;
            inputs[0].u.ki.dwFlags = keyUp ? (uint)NativeMethods.KeyEventFlags.KEYUP : 0;

            NativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }
    }
}
