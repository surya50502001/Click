using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HeyClicky.Agent
{
    public class AgentDecision
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }
        
        [JsonPropertyName("arguments")]
        public Dictionary<string, object> Arguments { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; }
    }
}
