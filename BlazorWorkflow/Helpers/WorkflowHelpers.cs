using System.Collections.Frozen;
using System.Text.Json.Nodes;
using BlazorWorkflow.Flow.Attributes;
using BlazorWorkflow.Models;
using BlazorWorkflow.Models.NodeV2;

namespace BlazorWorkflow.Helpers
{
    public static class WorkflowHelpers
    {
        [BlazorFlowNodeMethod(NodeType.Function, "Workflow")]
        public static async Task<JsonObject> ExecuteWorkflow(WorkflowInfo workflow, 
            Dictionary<string, string> inputParams, Dictionary<string, string> envVariables)
        {
            var context = new GraphExecutionContext()
            {
                Parameters = inputParams.ToFrozenDictionary(),
                EnvironmentVariables = envVariables.ToFrozenDictionary()
            };
            await workflow.FlowGraph.Run(context).ConfigureAwait(false);

            return context.SharedContext;
        }
    }
}
