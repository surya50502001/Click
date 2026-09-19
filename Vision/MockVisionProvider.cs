using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeyClicky.Vision
{
    public class MockVisionProvider : IVisionProvider
    {
        public Task<ScreenAnalysis> AnalyzeScreenAsync(byte[] screenshot, string instruction)
        {
            // Simulate AI delay
            Task.Delay(2000).Wait();

            // Return a hardcoded coordinate to demonstrate the mouse movement.
            // In a real scenario, the Vision AI would detect the "Blender" result in the start menu.
            // These coordinates are an estimate for a Windows 10/11 Start Menu search result (left-center).
            return Task.FromResult(new ScreenAnalysis
            {
                Elements = new List<ScreenElement>
                {
                    new ScreenElement
                    {
                        Name = "Blender",
                        Type = "application",
                        X = 250, // Hardcoded estimate 
                        Y = 300, // Hardcoded estimate
                        Width = 250,
                        Height = 60,
                        Confidence = 0.99
                    }
                }
            });
        }
    }
}
