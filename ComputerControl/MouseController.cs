using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.ComputerControl
{
    public class MouseController
    {
        public static void MoveTo(int x, int y, int durationMs = 300)
        {
            if (durationMs <= 0)
            {
                NativeMethods.SetCursorPos(x, y);
                return;
            }

            NativeMethods.POINT startPos;
            NativeMethods.GetCursorPos(out startPos);

            int steps = durationMs / 10;
            if (steps == 0) steps = 1;

            double dx = (x - startPos.x) / (double)steps;
            double dy = (y - startPos.y) / (double)steps;

            for (int i = 1; i <= steps; i++)
            {
                int nextX = (int)(startPos.x + (dx * i));
                int nextY = (int)(startPos.y + (dy * i));
                NativeMethods.SetCursorPos(nextX, nextY);
                Thread.Sleep(10);
            }

            // Ensure exact final position
            NativeMethods.SetCursorPos(x, y);
        }

        public static void LeftClick()
        {
            SendMouseEvent(NativeMethods.MouseEventFlags.LEFTDOWN);
            Thread.Sleep(50);
            SendMouseEvent(NativeMethods.MouseEventFlags.LEFTUP);
        }

        public static void RightClick()
        {
            SendMouseEvent(NativeMethods.MouseEventFlags.RIGHTDOWN);
            Thread.Sleep(50);
            SendMouseEvent(NativeMethods.MouseEventFlags.RIGHTUP);
        }

        public static void DoubleClick()
        {
            LeftClick();
            Thread.Sleep(50);
            LeftClick();
        }

        private static void SendMouseEvent(NativeMethods.MouseEventFlags flags)
        {
            NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[1];
            inputs[0].type = NativeMethods.INPUT_MOUSE;
            inputs[0].u.mi.dwFlags = (uint)flags;

            NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }
    }
}
