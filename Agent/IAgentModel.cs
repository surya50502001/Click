using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Agent
{
    public interface IAgentModel
    {
        Task<AgentDecision> GetNextActionAsync(AgentContext context, byte[] currentScreenshot, CancellationToken cancellationToken);
    }
}
