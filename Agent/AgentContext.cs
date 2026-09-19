using System.Collections.Generic;

namespace HeyClicky.Agent
{
    public class AgentContext
    {
        public string OriginalGoal { get; set; }
        public string CurrentObjective { get; set; }
        
        public string LastObservation { get; set; }
        public string VisibleWindows { get; set; }
        
        public List<string> ActionHistory { get; set; } = new List<string>();
        
        public int StepCount { get; set; }
        public int RetryCount { get; set; }
        
        public string LastError { get; set; }
        public bool IsFinished { get; set; }
        public bool IsSuccess { get; set; }
    }
}
