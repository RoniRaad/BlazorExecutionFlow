using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BlazorExecutionFlow.Flow.Attributes;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models;
using BlazorExecutionFlow.Models.NodeV2;
using BlazorExecutionFlow.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorExecutionFlow.Flow.BaseNodes
{
    /// <summary>
    /// Workflow-related nodes that allow workflows to be composed from other workflows.
    /// </summary>
    public static class WorkflowNodes
    {
        /// <summary>
        /// Executes another workflow as a nested sub-workflow.
        /// This allows workflows to be composed and reused as nodes within other workflows.
        /// </summary>
        /// <param name="context">Node context providing access to parent execution context</param>
        /// <param name="serviceProvider">Service provider for dependency injection</param>
        /// <param name="workflowId">The ID of the workflow to execute</param>
        /// <param name="inputs">Input parameters to pass to the workflow (as JSON object)</param>
        /// <returns>The output from the executed workflow</returns>
        [BlazorFlowNodeMethod(NodeType.Function, "Workflow")]
        public static async Task<JsonObject> ExecuteWorkflow(
            NodeContext context,
            IServiceProvider serviceProvider,
            [BlazorFlowInputField] string workflowId,
            JsonObject? inputs = null)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                throw new ArgumentException("Workflow ID cannot be empty", nameof(workflowId));

            // Get the workflow service from DI
            var workflowService = serviceProvider.GetService<IWorkflowService>();
            if (workflowService == null)
            {
                throw new InvalidOperationException(
                    "IWorkflowService is not registered in the DI container. " +
                    "Call AddBlazorExecutionFlow() in Program.cs to register the required services.");
            }

            // Get the workflow
            var workflow = workflowService.GetWorkflow(workflowId);
            if (workflow == null)
            {
                throw new ArgumentException($"Workflow with ID '{workflowId}' not found", nameof(workflowId));
            }

            // Create execution context with the provided inputs
            var executionContext = new GraphExecutionContext();

            // Copy environment variables from parent context if available
            if (context.CurrentNode.SharedExecutionContext != null)
            {
                executionContext.EnvironmentVariables = context.CurrentNode.SharedExecutionContext.EnvironmentVariables;
            }

            // Convert inputs JsonObject to Dictionary<string, string>
            if (inputs != null)
            {
                var inputDict = new Dictionary<string, string>();
                foreach (var kvp in inputs)
                {
                    if (kvp.Value != null)
                    {
                        // Try to get the string value
                        if (kvp.Value is JsonValue jsonValue)
                        {
                            inputDict[kvp.Key] = jsonValue.ToString();
                        }
                        else
                        {
                            inputDict[kvp.Key] = kvp.Value.ToJsonString();
                        }
                    }
                }
                executionContext.Parameters = inputDict.ToFrozenDictionary();
            }

            // Execute the workflow
            await workflow.FlowGraph.Run(executionContext);

            // Collect outputs from all nodes marked as workflow outputs
            var outputs = new JsonObject();

            foreach (var node in workflow.FlowGraph.Nodes.Values)
            {
                if (node.Result != null)
                {
                    // Check if this node has outputs marked as workflow outputs
                    foreach (var outputMapping in node.MethodOutputToNodeOutputMap)
                    {
                        // Check if this output is marked as a workflow output
                        // (In a more advanced implementation, we'd check node metadata)
                        if (outputMapping.To != null)
                        {
                            var outputValue = node.Result.GetByPath($"output.{outputMapping.To}");
                            if (outputValue != null)
                            {
                                outputs[outputMapping.To] = outputValue.DeepClone();
                            }
                        }
                    }
                }
            }

            return outputs;
        }

        /// <summary>
        /// Gets a workflow input parameter value.
        /// This is used to access parameters passed into the workflow.
        /// </summary>
        /// <param name="context">The node context providing access to workflow parameters</param>
        /// <param name="parameterName">The name of the parameter to retrieve</param>
        /// <param name="defaultValue">The default value if the parameter is not found</param>
        /// <returns>The parameter value or default if not found</returns>
        [BlazorFlowNodeMethod(NodeType.Function, "Workflow")]
        public static string GetWorkflowParameter(
            NodeContext context,
            [BlazorFlowInputField] string parameterName,
            [BlazorFlowInputField] string defaultValue = "")
        {
            if (context.CurrentNode.SharedExecutionContext?.Parameters.TryGetValue(parameterName, out var value) == true)
            {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Marks a value as a workflow output.
        /// This allows values to be exposed as outputs when the workflow is used as a nested workflow.
        /// </summary>
        /// <param name="outputName">The name for this output</param>
        /// <param name="value">The value to output</param>
        /// <returns>The same value (pass-through)</returns>
        [BlazorFlowNodeMethod(NodeType.Function, "Workflow")]
        public static T SetWorkflowOutput<T>(
            [BlazorFlowInputField] string outputName,
            T value)
        {
            // The actual output collection is handled by the ExecuteWorkflow node
            // This is just a marker/pass-through node
            return value;
        }
    }
}
