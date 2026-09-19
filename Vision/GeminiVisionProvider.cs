using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HeyClicky.Vision
{
    public class GeminiVisionProvider : IVisionProvider
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GeminiVisionProvider(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        public async Task<ScreenAnalysis> AnalyzeScreenAsync(byte[] screenshot, string instruction)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new Exception("API Key not configured.");
            }

            string base64Image = Convert.ToBase64String(screenshot);
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-pro:generateContent?key={_apiKey}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = instruction + "\nRespond strictly in JSON matching the requested schema." },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/png",
                                    data = base64Image
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json"
                }
            };

            string jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Vision API failed: {error}");
            }

            string responseString = await response.Content.ReadAsStringAsync();
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
                        // Gemini sometimes wraps JSON in markdown blocks like ```json ... ```
                        textResult = textResult.Trim();
                        if (textResult.StartsWith("```json")) textResult = textResult.Substring(7);
                        if (textResult.StartsWith("```")) textResult = textResult.Substring(3);
                        if (textResult.EndsWith("```")) textResult = textResult.Substring(0, textResult.Length - 3);
                        textResult = textResult.Trim();

                        return JsonSerializer.Deserialize<ScreenAnalysis>(textResult);
                    }
                }
            }
            catch (Exception)
            {
                // If Gemini returns plain text (e.g. "I cannot find Blender") instead of JSON, 
                // we catch the error and safely return an empty result so the app doesn't crash.
                return new ScreenAnalysis();
            }

            return new ScreenAnalysis();
        }
    }
}
