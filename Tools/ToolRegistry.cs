using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Tools
{
    public class ToolRegistry
    {
        private readonly Dictionary<string, ITool> _tools = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase);

        public void RegisterTool(ITool tool)
        {
            _tools[tool.Name] = tool;
        }

        public ITool GetTool(string name)
        {
            return _tools.TryGetValue(name, out var tool) ? tool : null;
        }

        public IEnumerable<ITool> GetAllTools()
        {
            return _tools.Values;
        }

        public async Task<string> ExecuteToolAsync(string name, JsonElement arguments, CancellationToken token)
        {
            var tool = GetTool(name);
            if (tool == null)
            {
                return $"Error: Tool '{name}' not found.";
            }

            try
            {
                return await tool.ExecuteAsync(arguments, token);
            }
            catch (Exception ex)
            {
                return $"Error executing {name}: {ex.Message}";
            }
        }
    }
}
