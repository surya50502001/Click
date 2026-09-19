using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HeyClicky.Vision
{
    public class ScreenAnalysis
    {
        [JsonPropertyName("elements")]
        public List<ScreenElement> Elements { get; set; } = new List<ScreenElement>();
    }

    public class ScreenElement
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }
}
