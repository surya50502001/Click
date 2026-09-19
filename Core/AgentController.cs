using System;
using System.Linq;
using System.Threading.Tasks;
using HeyClicky.ComputerControl;
using HeyClicky.Vision;

namespace HeyClicky.Core
{
    public class AgentController
    {
        private readonly IVisionProvider _visionProvider;
        
        public event Action<AgentState, string> OnStateChanged;
        public event Action<int, int> OnTargetIdentified;

        public AgentController(IVisionProvider visionProvider)
        {
            _visionProvider = visionProvider;
        }

        public async Task ExecuteCommandAsync(string command)
        {
            try
            {
                // Simple parser for V1: "open [app]"
                if (command.ToLower().StartsWith("open "))
                {
                    string appName = command.Substring(5).Trim();
                    await ExecuteOpenActionAsync(appName);
                }
                else
                {
                    UpdateState(AgentState.Error, "Unsupported command. Try 'open [app]'");
                }
            }
            catch (Exception ex)
            {
                // Force close the start menu so the user isn't stuck typing in it
                KeyboardController.PressKey(NativeMethods.VK_ESCAPE);
                UpdateState(AgentState.Error, ex.Message);
            }
        }

        private async Task ExecuteOpenActionAsync(string appName)
        {
            // 1. OBSERVE (Initial trigger)
            UpdateState(AgentState.Acting, "Pressing Windows Key...");
            KeyboardController.PressWindowsKey();
            await Task.Delay(500); // Wait for start menu animation

            UpdateState(AgentState.Acting, $"Typing '{appName}'...");
            KeyboardController.TypeText(appName);
            await Task.Delay(1000); // Wait for search results to populate

            // 2. REASON (Capture and analyze)
            UpdateState(AgentState.Observing, "Capturing screen...");
            byte[] screenshot = ScreenCapture.CaptureScreen();

            UpdateState(AgentState.Reasoning, "Analyzing screen...");
            string instruction = $@"Find the Windows Start Menu search result for the application '{appName}'. 
Respond with JSON strictly matching this schema: 
{{
  ""elements"": [
    {{
      ""name"": ""{appName}"",
      ""type"": ""application"",
      ""x"": 0,
      ""y"": 0,
      ""width"": 0,
      ""height"": 0,
      ""confidence"": 0.0
    }}
  ]
}}
IMPORTANT: You must return the X and Y coordinates scaled between 0 and 1000! (0,0 is top-left, 1000,1000 is bottom-right). 
Point precisely to the center of the actual application icon/text on the left-hand list, NOT the giant web preview panel.";
            
            var analysis = await _visionProvider.AnalyzeScreenAsync(screenshot, instruction);
            var target = analysis?.Elements?.OrderByDescending(e => e.Confidence).FirstOrDefault();

            if (target != null && target.Confidence > 0.5)
            {
                // Convert normalized 0-1000 coordinates back to actual screen pixels
                int screenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
                
                int realX = (int)((target.X / 1000.0) * screenWidth);
                int realY = (int)((target.Y / 1000.0) * screenHeight);

                // 3. ACT (Move and Click)
                UpdateState(AgentState.Acting, $"Moving to {realX}, {realY}");
                OnTargetIdentified?.Invoke(realX, realY);
                
                await Task.Delay(500); // Small pause for the UI indicator to show
                
                MouseController.MoveTo(realX, realY, durationMs: 400); // Smooth move
                await Task.Delay(100);
                
                UpdateState(AgentState.Acting, "Clicking target...");
                MouseController.LeftClick();

                // 4. VERIFY (Skipping complex verification for V1, assuming success after click)
                UpdateState(AgentState.Done, $"Successfully clicked {appName}");
            }
            else
            {
                // Ensure we close the Start Menu so the user's keyboard isn't trapped
                KeyboardController.PressKey(NativeMethods.VK_ESCAPE);
                UpdateState(AgentState.Error, "Could not find the target on screen.");
            }
        }

        private void UpdateState(AgentState state, string message)
        {
            OnStateChanged?.Invoke(state, message);
        }
    }
}
