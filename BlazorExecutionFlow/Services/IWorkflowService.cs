using BlazorExecutionFlow.Models;

namespace BlazorExecutionFlow.Services
{
    /// <summary>
    /// Service for managing workflows.
    /// Consumers can implement this interface to provide their own storage mechanism.
    /// </summary>
    public interface IWorkflowService
    {
        /// <summary>
        /// Gets all workflows.
        /// </summary>
        List<WorkflowInfo> GetAllWorkflows();

        /// <summary>
        /// Gets a specific workflow by ID.
        /// </summary>
        WorkflowInfo? GetWorkflow(string id);

        /// <summary>
        /// Adds a new workflow.
        /// </summary>
        void AddWorkflow(WorkflowInfo workflow);

        /// <summary>
        /// Updates an existing workflow.
        /// </summary>
        void UpdateWorkflow(WorkflowInfo workflow);

        /// <summary>
        /// Deletes a workflow by ID.
        /// </summary>
        void DeleteWorkflow(string id);
    }
}
