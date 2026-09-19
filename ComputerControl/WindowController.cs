using System;
using System.Collections.Generic;
using System.Text;

namespace HeyClicky.ComputerControl
{
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; }
    }

    public class WindowController
    {
        public static List<WindowInfo> GetVisibleWindows()
        {
            var windows = new List<WindowInfo>();

            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (NativeMethods.IsWindowVisible(hWnd))
                {
                    int length = NativeMethods.GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        var builder = new StringBuilder(length + 1);
                        NativeMethods.GetWindowText(hWnd, builder, builder.Capacity);
                        string title = builder.ToString();
                        
                        // Ignore some basic system overlays
                        if (title != "Program Manager" && title != "HeyClicky" && title != "HeyClicky Overlay")
                        {
                            windows.Add(new WindowInfo { Handle = hWnd, Title = title });
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        public static bool FocusWindow(string partialTitle)
        {
            var windows = GetVisibleWindows();
            foreach (var w in windows)
            {
                if (w.Title.Contains(partialTitle, StringComparison.OrdinalIgnoreCase))
                {
                    if (NativeMethods.IsIconic(w.Handle))
                    {
                        NativeMethods.ShowWindow(w.Handle, NativeMethods.SW_RESTORE);
                    }
                    else
                    {
                        NativeMethods.ShowWindow(w.Handle, NativeMethods.SW_SHOW);
                    }
                    NativeMethods.SetForegroundWindow(w.Handle);
                    return true;
                }
            }
            return false;
        }

        public static string GetForegroundWindowText()
        {
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd != IntPtr.Zero)
            {
                int length = NativeMethods.GetWindowTextLength(hwnd);
                if (length > 0)
                {
                    var builder = new StringBuilder(length + 1);
                    NativeMethods.GetWindowText(hwnd, builder, builder.Capacity);
                    return builder.ToString();
                }
            }
            return string.Empty;
        }
    }
}
