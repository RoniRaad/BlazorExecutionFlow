using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Helpers;
namespace BlazorExecutionFlow.Models.NodeV2
{
    public class Graph
    {
        public ConcurrentDictionary<string, Node> Nodes = [];

        public Graph()
        {
            Nodes["0"] = DrawflowHelpers.CreateNodeFromMethod(typeof(CoreNodes).GetMethod("Start"));
            Nodes["0"].DrawflowNodeId = "0";
        }

        public async Task Run(GraphExecutionContext executionContext)
        {
            // Clear results and errors from previous runs
            foreach (var node in Nodes)
            {
                node.Value.Result = null;
                node.Value.HasError = false;
                node.Value.ErrorMessage = null;
                node.Value.SharedExecutionContext = executionContext;
            }

            // Event handlers are already attached during component initialization
            // No need to re-attach them here

            var startNodes = Nodes.Where(x => x.Value.BackingMethod.Name == nameof(CoreNodes.Start));
            var tasks = new List<Task>();
            foreach (var startNode in startNodes)
            {
                tasks.Add(startNode.Value.ExecuteNode());
            }

            await Task.WhenAll(tasks);
        }
    }

    public class GraphExecutionContext
    {
        public FrozenDictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>().ToFrozenDictionary();
        public Dictionary<string, object> Output { get; set; } = [];
    }
}