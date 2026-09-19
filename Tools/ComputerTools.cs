using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.ComputerControl;
using System.Diagnostics;

namespace HeyClicky.Tools
{
    public class MouseClickTool : ITool
    {
        public string Name => "mouse_click";
        public string Description => "Moves the mouse to normalized coordinates (0-1000) and clicks.";
        public string SchemaJson => "{ \"x\": \"number (0-1000)\", \"y\": \"number (0-1000)\", \"type\": \"left|right|double\" }";

        public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken token)
        {
            try
            {
                double xNorm = arguments.GetProperty("x").GetDouble();
                double yNorm = arguments.GetProperty("y").GetDouble();
                string type = arguments.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : "left";

                int screenWidth = System.Windows.Forms.SystemInformation.VirtualScreen.Width;
                int screenHeight = System.Windows.Forms.SystemInformation.VirtualScreen.Height;
                int leftOffset = System.Windows.Forms.SystemInformation.VirtualScreen.Left;
                int topOffset = System.Windows.Forms.SystemInformation.VirtualScreen.Top;
                
                int realX = leftOffset + (int)((xNorm / 1000.0) * screenWidth);
                int realY = topOffset + (int)((yNorm / 1000.0) * screenHeight);

                MouseController.MoveTo(realX, realY, durationMs: 300);
                Task.Delay(100).Wait(); // Small wait before click

                if (type == "right") MouseController.RightClick();
                else if (type == "double") MouseController.DoubleClick();
                else MouseController.LeftClick();

                return Task.FromResult($"Clicked {type} at {realX}, {realY}.");
            }
            catch (System.Exception ex)
            {
                return Task.FromResult($"Error: {ex.Message}");
            }
        }
    }

    public class MouseDragTool : ITool
    {
        public string Name => "mouse_drag";
        public string Description => "Drags the mouse from start coordinates to end coordinates with the left button held down (useful for dragging files, sliders, or drawing lines).";
        public string SchemaJson => "{ \"start_x\": \"number (0-1000)\", \"start_y\": \"number (0-1000)\", \"end_x\": \"number (0-1000)\", \"end_y\": \"number (0-1000)\" }";

        public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken token)
        {
            try
            {
                double sxNorm = arguments.GetProperty("start_x").GetDouble();
                double syNorm = arguments.GetProperty("start_y").GetDouble();
                double exNorm = arguments.GetProperty("end_x").GetDouble();
                double eyNorm = arguments.GetProperty("end_y").GetDouble();

                int screenWidth = System.Windows.Forms.SystemInformation.VirtualScreen.Width;
                int screenHeight = System.Windows.Forms.SystemInformation.VirtualScreen.Height;
                int leftOffset = System.Windows.Forms.SystemInformation.VirtualScreen.Left;
                int topOffset = System.Windows.Forms.SystemInformation.VirtualScreen.Top;

                int startX = leftOffset + (int)((sxNorm / 1000.0) * screenWidth);
                int startY = topOffset + (int)((syNorm / 1000.0) * screenHeight);
                int endX = leftOffset + (int)((exNorm / 1000.0) * screenWidth);
                int endY = topOffset + (int)((eyNorm / 1000.0) * screenHeight);

                MouseController.Drag(startX, startY, endX, endY);
                return Task.FromResult($"Dragged from ({startX}, {startY}) to ({endX}, {endY})");
            }
            catch (System.Exception ex)
            {
                return Task.FromResult($"Error: {ex.Message}");
            }
        }
    }

    public class DrawCircleTool : ITool
    {
        public string Name => "draw_circle";
        public string Description => "Draws a circle on the current canvas or screen at the specified center coordinates (0-1000) with a given radius.";
        public string SchemaJson => "{ \"center_x\": \"number (0-1000)\", \"center_y\": \"number (0-1000)\", \"radius\": \"number (e.g. 50-150)\" }";

        public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken token)
        {
            try
            {
                double cxNorm = arguments.GetProperty("center_x").GetDouble();
                double cyNorm = arguments.GetProperty("center_y").GetDouble();
                int radius = arguments.TryGetProperty("radius", out var rProp) ? rProp.GetInt32() : 75;

                int screenWidth = System.Windows.Forms.SystemInformation.VirtualScreen.Width;
                int screenHeight = System.Windows.Forms.SystemInformation.VirtualScreen.Height;
                int leftOffset = System.Windows.Forms.SystemInformation.VirtualScreen.Left;
                int topOffset = System.Windows.Forms.SystemInformation.VirtualScreen.Top;

                int centerX = leftOffset + (int)((cxNorm / 1000.0) * screenWidth);
                int centerY = topOffset + (int)((cyNorm / 1000.0) * screenHeight);

                MouseController.DrawCircle(centerX, centerY, radius);
                return Task.FromResult($"Drew circle centered at ({centerX}, {centerY}) with radius {radius}");
            }
            catch (System.Exception ex)
            {
                return Task.FromResult($"Error: {ex.Message}");
            }
        }
    }

    public class TypeTextTool : ITool
    {
        public string Name => "type_text";
        public string Description => "Types the specified text on the keyboard.";
        public string SchemaJson => "{ \"text\": \"string\", \"press_enter\": \"boolean\" }";

        public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken token)
        {
            try
            {
                string text = arguments.GetProperty("text").GetString();
                bool pressEnter = arguments.TryGetProperty("press_enter", out var pe) && pe.GetBoolean();

                KeyboardController.TypeText(text);
                if (pressEnter)
                {
                    Task.Delay(100).Wait();
                    KeyboardController.PressEnter();
                }

                return Task.FromResult($"Typed text. Enter pressed: {pressEnter}");
            }
            catch (System.Exception ex)
            {
                return Task.FromResult($"Error: {ex.Message}");
            }
        }
    }

    public class PressKeyTool : ITool
    {
        public string Name => "press_key";
        public string Description => "Presses a special key (e.g. WIN, ESC, ENTER, BACKSPACE).";
        public string SchemaJson => "{ \"key\": \"string (WIN|ESC|ENTER|BACKSPACE)\" }";

        public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken token)
        {
            try
            {
                string key = arguments.GetProperty("key").GetString().ToUpper();
                switch (key)
                {
                    case "WIN": KeyboardController.PressWindowsKey(); break;
                    case "ESC": KeyboardController.PressKey(NativeMethods.VK_ESCAPE); break;
                    case "ENTER": KeyboardController.PressEnter(); break;
                    case "BACKSPACE": KeyboardController.PressKey(NativeMethods.VK_BACK); break;
                    default: return Task.FromResult($"Error: Unknown key {key}");
                }
                return Task.FromResult($"Pressed {key}");
            }
            catch (System.Exception ex)
            {
                return Task.FromResult($"Error: {ex.Message}");
            }
        }
    }

    public class FocusWindowTool : ITool
    {
        public string Name => "focus_window";
        public string Description => "Brings a window to the foreground using its partial title. Use this instead of Vision clicking if the app is already running.";
        public string SchemaJson => "{ \"title\": \"string (partial window title)\" }";

        public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken token)
        {
            try
            {
                string title = arguments.GetProperty("title").GetString();
                bool success = WindowController.FocusWindow(title);
                return Task.FromResult(success ? $"Focused window matching '{title}'" : $"Error: Could not find window matching '{title}'");
            }
            catch (System.Exception ex)
            {
                return Task.FromResult($"Error: {ex.Message}");
            }
        }
    }

    public class LaunchAppTool : ITool
    {
        public string Name => "launch_app";
        public string Description => "Directly launches a process or URL (e.g., 'chrome', 'notepad', 'https://youtube.com'). Faster than clicking through Start Menu.";
        public string SchemaJson => "{ \"target\": \"string (process name or URL)\" }";

        public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken token)
        {
            try
            {
                string target = arguments.GetProperty("target").GetString();
                Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
                return Task.FromResult($"Launched {target}");
            }
            catch (System.Exception ex)
            {
                return Task.FromResult($"Error: Failed to launch {ex.Message}");
            }
        }
    }

    public class WaitTool : ITool
    {
        public string Name => "wait";
        public string Description => "Waits for a specified number of milliseconds (e.g. for a page to load).";
        public string SchemaJson => "{ \"milliseconds\": \"number\" }";

        public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken token)
        {
            int ms = 1000;
            if (arguments.TryGetProperty("milliseconds", out var msProp))
            {
                ms = msProp.GetInt32();
            }
            await Task.Delay(ms, token);
            return $"Waited {ms}ms";
        }
    }

    public class HotkeyTool : ITool
    {
        public string Name => "hotkey";
        public string Description => "Simulates a keyboard shortcut (e.g. 'CTRL+L' to focus browser address bar, 'CTRL+T' for new tab, 'CTRL+A' for select all).";
        public string SchemaJson => "{ \"keys\": \"string (e.g. CTRL+L, CTRL+T, CTRL+A)\" }";

        public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken token)
        {
            try
            {
                string keys = arguments.GetProperty("keys").GetString().ToUpper();
                if (keys == "CTRL+L") KeyboardController.SendHotkey(NativeMethods.VK_CONTROL, NativeMethods.VK_KEY_L);
                else if (keys == "CTRL+T") KeyboardController.SendHotkey(NativeMethods.VK_CONTROL, NativeMethods.VK_KEY_T);
                else if (keys == "CTRL+A") KeyboardController.SendHotkey(NativeMethods.VK_CONTROL, NativeMethods.VK_KEY_A);
                else if (keys == "CTRL+V") KeyboardController.SendHotkey(NativeMethods.VK_CONTROL, NativeMethods.VK_KEY_V);
                else return Task.FromResult($"Error: Unsupported hotkey '{keys}'");

                return Task.FromResult($"Sent hotkey {keys}");
            }
            catch (System.Exception ex)
            {
                return Task.FromResult($"Error: {ex.Message}");
            }
        }
    }
    public class DoneTool : ITool
    {
        public string Name => "done";
        public string Description => "Marks the goal as successfully completed.";
        public string SchemaJson => "{ \"reason\": \"string (explain why it is complete)\" }";

        public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken token)
        {
            return Task.FromResult("Done.");
        }
    }

    public class FailTool : ITool
    {
        public string Name => "fail";
        public string Description => "Marks the goal as failed or impossible.";
        public string SchemaJson => "{ \"reason\": \"string (explain why it failed)\" }";

        public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken token)
        {
            return Task.FromResult("Failed.");
        }
    }
}
