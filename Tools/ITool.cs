using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Tools
{
    public interface ITool
    {
        string Name { get; }
        string Description { get; }
        string SchemaJson { get; }
        
        Task<string> ExecuteAsync(JsonElement arguments, CancellationToken token);
    }
}
