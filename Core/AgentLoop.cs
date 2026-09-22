using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agent;
using HeyClicky.ComputerControl;
using HeyClicky.Tools;

namespace HeyClicky.Core
{
    public class AgentLoop
    {
        private readonly IAgentModel _model;
        private readonly ToolRegistry _toolRegistry;
        
        public event Action<AgentState, string> OnStateChanged;
        public event Action<int, int> OnTargetIdentified; // X,Y

        public AgentLoop(IAgentModel model, ToolRegistry toolRegistry)
        {
            _model = model;
            _toolRegistry = toolRegistry;
        }

        public async Task RunAsync(string originalGoal, CancellationToken token)
        {
            var context = new AgentContext
            {
                OriginalGoal = originalGoal,
                IsFinished = false,
                IsSuccess = false
            };

            UpdateState(AgentState.Idle, "Starting autonomous agent...");

            while (!context.IsFinished && !token.IsCancellationRequested && context.StepCount < 15)
            {
                context.StepCount++;
                
                // 1. OBSERVE
                UpdateState(AgentState.Observing, "Observing system state...");
                var visibleWindows = WindowController.GetVisibleWindows();
                context.VisibleWindows = string.Join(", ", visibleWindows.Select(w => w.Title));
                byte[] screenshot = ScreenCapture.CaptureScreen();

                // 2. REASON & PLAN
                UpdateState(AgentState.Reasoning, "Planning next action...");
                AgentDecision decision = null;
                try
                {
                    decision = await _model.GetNextActionAsync(context, screenshot, token);
                }
                catch (OperationCanceledException)
                {
                    break; // Instant break on user stop!
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested) break;
                    context.LastError = $"Model API Error: {ex.Message}";
                    context.RetryCount++;
                    if (context.RetryCount > 3) break;
                    try { await Task.Delay(2000, token); } catch { break; }
                    continue;
                }

                // Log the decision
                context.ActionHistory.Add($"Step {context.StepCount} Planned: {decision.Action} (Reason: {decision.Reason})");
                UpdateState(AgentState.Acting, $"Action: {decision.Action}");

                // Visual target marker if it's a mouse click
                if (decision.Action == "mouse_click" && decision.Arguments != null)
                {
                    if (decision.Arguments.TryGetValue("x", out var xObj) && decision.Arguments.TryGetValue("y", out var yObj))
                    {
                        try
                        {
                            // Parse JsonElement if it comes back as such
                            double xNorm = double.Parse(xObj.ToString());
                            double yNorm = double.Parse(yObj.ToString());

                            int screenWidth = System.Windows.Forms.SystemInformation.VirtualScreen.Width;
                            int screenHeight = System.Windows.Forms.SystemInformation.VirtualScreen.Height;
                            int leftOffset = System.Windows.Forms.SystemInformation.VirtualScreen.Left;
                            int topOffset = System.Windows.Forms.SystemInformation.VirtualScreen.Top;
                            
                            int realX = leftOffset + (int)((xNorm / 1000.0) * screenWidth);
                            int realY = topOffset + (int)((yNorm / 1000.0) * screenHeight);

                            OnTargetIdentified?.Invoke(realX, realY);
                        }
                        catch { } // Ignore parse errors for UI marker
                    }
                }

                // 3. ACT
                string result = string.Empty;
                if (decision.Action == "done")
                {
                    context.IsFinished = true;
                    context.IsSuccess = true;
                    result = "Task marked complete by agent.";
                }
                else if (decision.Action == "fail")
                {
                    context.IsFinished = true;
                    context.IsSuccess = false;
                    result = $"Task marked failed by agent: {decision.Reason}";
                }
                else
                {
                    // Execute tool
                    JsonElement argsElement = default;
                    if (decision.Arguments != null)
                    {
                        string argsJson = JsonSerializer.Serialize(decision.Arguments);
                        argsElement = JsonSerializer.Deserialize<JsonElement>(argsJson);
                    }
                    
                    result = await _toolRegistry.ExecuteToolAsync(decision.Action, argsElement, token);
                }

                // 4. VERIFY / LOG RESULT
                context.LastError = result.StartsWith("Error") ? result : "";
                context.ActionHistory.Add($"Step {context.StepCount} Result: {result}");
                
                await Task.Delay(200, token); // Fast pause for UI animations to settle
            }

            if (token.IsCancellationRequested)
            {
                UpdateState(AgentState.Error, "Task was cancelled by user.");
            }
            else if (context.IsSuccess)
            {
                UpdateState(AgentState.Done, "Task finished successfully.");
            }
            else
            {
                UpdateState(AgentState.Error, $"Task failed or timed out. Last state: {context.LastError}");
            }
        }

        private void UpdateState(AgentState state, string message)
        {
            OnStateChanged?.Invoke(state, message);
        }
    }
}
