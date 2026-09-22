using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Tools;

namespace HeyClicky.Agent
{
    public class GeminiAgentModel : IAgentModel
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly ToolRegistry _toolRegistry;

        // Verified active models in the environment with independent quotas
        private static readonly string[] CandidateModels = new[]
        {
            "gemini-3.5-flash-lite",
            "gemini-3.6-flash",
            "gemini-flash-latest",
            "gemini-3.7-flash",
            "gemini-3.5-flash"
        };

        public GeminiAgentModel(string apiKey, ToolRegistry toolRegistry)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _toolRegistry = toolRegistry;
        }

        public async Task<AgentDecision> GetNextActionAsync(AgentContext context, byte[] currentScreenshot, CancellationToken cancellationToken)
        {
            string systemPrompt = BuildSystemPrompt(context);

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = systemPrompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = Convert.ToBase64String(currentScreenshot)
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    temperature = 0.1,
                    maxOutputTokens = 300
                }
            };

            string jsonPayload = JsonSerializer.Serialize(payload);

            Exception lastException = null!;

            // Iterate through candidate models and backoff if overloaded (503 / 429 high demand)
            foreach (var modelName in CandidateModels)
            {
                if (cancellationToken.IsCancellationRequested) break;

                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={_apiKey}";
                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        var response = await _httpClient.PostAsync(url, content, cancellationToken);
                        string responseString = await response.Content.ReadAsStringAsync(cancellationToken);

                        if (response.IsSuccessStatusCode)
                        {
                            return ParseResponse(responseString);
                        }

                        // Check for high demand / overloaded / rate limit errors
                        bool isOverloaded = (int)response.StatusCode == 429 || 
                                           (int)response.StatusCode == 503 ||
                                           responseString.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase) ||
                                           responseString.Contains("overloaded", StringComparison.OrdinalIgnoreCase) ||
                                           responseString.Contains("demand", StringComparison.OrdinalIgnoreCase) ||
                                           responseString.Contains("quota", StringComparison.OrdinalIgnoreCase);

                        if (isOverloaded)
                        {
                            lastException = new Exception($"Model '{modelName}' reached free-tier rate limit. Switching to alternative model...");
                            break; // Switch to the next available model immediately!
                        }
                        else
                        {
                            // Other non-retriable error
                            throw new Exception($"Gemini API error ({response.StatusCode}): {responseString}");
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex) when (ex.Message.Contains("rate limit") || ex.Message.Contains("overloaded"))
                    {
                        lastException = ex;
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        break; // Try next model
                    }
                }
            }

            throw lastException ?? new Exception("All candidate models reached temporary quota limits. Please wait 30 seconds and retry.");
        }

        private string BuildSystemPrompt(AgentContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are HeyClicky, an autonomous computer-use agent. Your goal is to fulfill the user's objective.");
            sb.AppendLine($"USER GOAL: {context.OriginalGoal}");
            sb.AppendLine();
            sb.AppendLine("CURRENT OBSERVATIONS:");
            sb.AppendLine($"Foreground Window: {HeyClicky.ComputerControl.WindowController.GetForegroundWindowText()}");
            sb.AppendLine($"Visible Windows: {context.VisibleWindows}");
            if (!string.IsNullOrEmpty(context.LastError))
                sb.AppendLine($"LAST ERROR: {context.LastError}");
            sb.AppendLine();
            sb.AppendLine("ACTION HISTORY:");
            foreach (var act in context.ActionHistory)
            {
                sb.AppendLine($"- {act}");
            }
            sb.AppendLine();
            sb.AppendLine("AVAILABLE TOOLS:");
            foreach (var tool in _toolRegistry.GetAllTools())
            {
                sb.AppendLine($"- {tool.Name}: {tool.Description}");
                sb.AppendLine($"  Arguments schema: {tool.SchemaJson}");
            }
            sb.AppendLine();
            sb.AppendLine("INSTRUCTIONS & BEST PRACTICES:");
            sb.AppendLine("1. Look at the screenshot and current observations.");
            sb.AppendLine("2. BROWSER NAVIGATION & SEARCH:");
            sb.AppendLine("   - To search or open a web page, you can use 'launch_app' with the direct URL (e.g. 'https://www.google.com/search?q=flowers' or 'https://youtube.com').");
            sb.AppendLine("   - Or if Chrome is already open, use 'hotkey' with 'CTRL+L' to focus the address bar, then 'type_text' with 'press_enter: true'.");
            sb.AppendLine("3. Consider previous steps and errors. If a strategy fails, try an alternative tool.");
            sb.AppendLine("4. If the goal is met, output the 'done' action.");
            sb.AppendLine("5. Respond strictly in JSON format matching this structure:");
            sb.AppendLine("{ \"action\": \"tool_name\", \"arguments\": { \"arg1\": \"val1\" }, \"reason\": \"Why you chose this action\" }");
            sb.AppendLine("IMPORTANT: For mouse clicks, coordinates must be normalized between 0 and 1000 (0,0 top-left, 1000,1000 bottom-right).");
            
            return sb.ToString();
        }

        private AgentDecision ParseResponse(string responseString)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(responseString))
                {
                    var textResult = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text").GetString();

                    if (!string.IsNullOrWhiteSpace(textResult))
                    {
                        textResult = textResult.Trim();
                        if (textResult.StartsWith("```json")) textResult = textResult.Substring(7);
                        if (textResult.StartsWith("```")) textResult = textResult.Substring(3);
                        if (textResult.EndsWith("```")) textResult = textResult.Substring(0, textResult.Length - 3);
                        textResult = textResult.Trim();

                        return JsonSerializer.Deserialize<AgentDecision>(textResult);
                    }
                }
            }
            catch (Exception ex)
            {
                return new AgentDecision
                {
                    Action = "fail",
                    Reason = $"Failed to parse model response: {ex.Message}",
                    Arguments = new System.Collections.Generic.Dictionary<string, object>()
                };
            }
            
            return new AgentDecision { Action = "fail", Reason = "Empty response" };
        }
    }
}
