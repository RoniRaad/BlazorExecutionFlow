using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models;
using BlazorExecutionFlow.Models.NodeV2;
using Microsoft.Extensions.Logging;

namespace BlazorExecutionFlow.Services
{
    /// <summary>
    /// File-based implementation of IWorkflowService.
    /// Stores workflows as JSON files in a specified directory.
    /// </summary>
    public class FileBasedWorkflowService : IWorkflowService
    {
        private readonly string _storageDirectory;
        private readonly ILogger<FileBasedWorkflowService>? _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly object _lock = new();

        public FileBasedWorkflowService(string storageDirectory, ILogger<FileBasedWorkflowService>? logger = null)
        {
            _storageDirectory = storageDirectory;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                Converters = { new MethodInfoJsonConverter(), new GraphJsonConverter() }
            };

            // Ensure storage directory exists
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
                _logger?.LogInformation("Created workflow storage directory: {Directory}", _storageDirectory);
            }

            // Seed sample workflows if directory is empty
            if (!GetAllWorkflows().Any())
            {
                SeedSampleWorkflows();
            }
        }

        public List<WorkflowInfo> GetAllWorkflows()
        {
            lock (_lock)
            {
                try
                {
                    var workflows = new List<WorkflowInfo>();
                    var files = Directory.GetFiles(_storageDirectory, "*.json");

                    foreach (var file in files)
                    {
                        try
                        {
                            var json = File.ReadAllText(file);
                            var workflow = JsonSerializer.Deserialize<WorkflowInfo>(json, _jsonOptions);
                            if (workflow != null)
                            {
                                PopulateInputData(workflow);
                                workflows.Add(workflow);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Failed to deserialize workflow from file: {File}", file);
                        }
                    }

                    return workflows;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to get all workflows");
                    return new List<WorkflowInfo>();
                }
            }
        }

        public WorkflowInfo? GetWorkflow(string id)
        {
            lock (_lock)
            {
                try
                {
                    var filePath = GetWorkflowFilePath(id);
                    if (!File.Exists(filePath))
                    {
                        return null;
                    }

                    var json = File.ReadAllText(filePath);
                    var workflow = JsonSerializer.Deserialize<WorkflowInfo>(json, _jsonOptions);
                    if (workflow != null)
                    {
                        PopulateInputData(workflow);
                    }

                    return workflow;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to get workflow: {Id}", id);
                    return null;
                }
            }
        }

        public void AddWorkflow(WorkflowInfo workflow)
        {
            lock (_lock)
            {
                try
                {
                    var filePath = GetWorkflowFilePath(workflow.Id);
                    var json = JsonSerializer.Serialize(workflow, _jsonOptions);
                    File.WriteAllText(filePath, json);
                    _logger?.LogInformation("Added workflow: {Id} - {Name}", workflow.Id, workflow.Name);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to add workflow: {Id}", workflow.Id);
                    throw;
                }
            }
        }

        public void UpdateWorkflow(WorkflowInfo workflow)
        {
            lock (_lock)
            {
                try
                {
                    var filePath = GetWorkflowFilePath(workflow.Id);
                    var json = JsonSerializer.Serialize(workflow, _jsonOptions);
                    File.WriteAllText(filePath, json);
                    _logger?.LogInformation("Updated workflow: {Id} - {Name}", workflow.Id, workflow.Name);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to update workflow: {Id}", workflow.Id);
                    throw;
                }
            }
        }

        public void DeleteWorkflow(string id)
        {
            lock (_lock)
            {
                try
                {
                    var filePath = GetWorkflowFilePath(id);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        _logger?.LogInformation("Deleted workflow: {Id}", id);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to delete workflow: {Id}", id);
                    throw;
                }
            }
        }

        private void PopulateInputData(WorkflowInfo workflow)
        {
            foreach (var nodeKvp in workflow.FlowGraph.Nodes)
            {
                var node = nodeKvp.Value;
                if (node.IsWorkflowNode)
                {
                    var externalWorkflow = GetWorkflow(node.ParentWorkflowId!);
                    var discoveredInputs = WorkflowInputDiscovery.DiscoverInputs(externalWorkflow?.FlowGraph ?? new());
                    var newInputMap = new List<PathMapEntry>();
                    foreach (var input in discoveredInputs)
                    {
                        var currentMap = node.NodeInputToMethodInputMap.FirstOrDefault(x => x.To == input);
                        if (currentMap == null)
                        {
                            newInputMap.Add(new PathMapEntry() { To = input });
                        }
                        else
                        {
                            newInputMap.Add(currentMap);
                        }
                    }

                    // We replace it so that if the inputs on the workflow are changed stale input maps are removed.
                    node.NodeInputToMethodInputMap = newInputMap;
                }
            }
        }

        private string GetWorkflowFilePath(string id)
        {
            // Sanitize ID to ensure it's a valid filename
            var safeId = string.Join("_", id.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_storageDirectory, $"{safeId}.json");
        }

        private void SeedSampleWorkflows()
        {
            _logger?.LogInformation("Seeding sample workflows");

            var workflow1 = new WorkflowInfo
            {
                Id = "sample-1",
                Name = "Example: Data Processing Pipeline",
                Description = "Example workflow that fetches data from an API, processes it, and stores results",
                CreatedAt = DateTime.Now.AddDays(-10),
                ModifiedAt = DateTime.Now.AddDays(-2),
                Inputs = new Dictionary<string, string>
                {
                    ["apiUrl"] = "https://api.example.com/data",
                    ["maxRetries"] = "3",
                    ["timeout"] = "30000"
                },
                PreviousExecutions = new List<WorkflowExecution>
                {
                    new WorkflowExecution
                    {
                        Id = "exec-1",
                        ExecutedAt = DateTime.Now.AddHours(-2),
                        Success = true,
                        Duration = TimeSpan.FromSeconds(12.3),
                        Output = new Dictionary<string, object>
                        {
                            ["recordsProcessed"] = 1523,
                            ["status"] = "success"
                        }
                    },
                    new WorkflowExecution
                    {
                        Id = "exec-2",
                        ExecutedAt = DateTime.Now.AddHours(-5),
                        Success = false,
                        Duration = TimeSpan.FromSeconds(3.1),
                        ErrorMessage = "Connection timeout: Unable to reach API endpoint",
                        Output = new Dictionary<string, object>()
                    }
                },
                FlowGraph = new()
            };

            var workflow2 = new WorkflowInfo
            {
                Id = "sample-2",
                Name = "Example: Email Campaign Automation",
                Description = "Example workflow that sends personalized emails to subscribers based on their preferences",
                CreatedAt = DateTime.Now.AddDays(-5),
                ModifiedAt = DateTime.Now.AddDays(-1),
                Inputs = new Dictionary<string, string>
                {
                    ["campaignId"] = "camp-2024-01",
                    ["batchSize"] = "100"
                },
                PreviousExecutions = new List<WorkflowExecution>
                {
                    new WorkflowExecution
                    {
                        Id = "exec-3",
                        ExecutedAt = DateTime.Now.AddMinutes(-30),
                        Success = true,
                        Duration = TimeSpan.FromMinutes(2.5),
                        Output = new Dictionary<string, object>
                        {
                            ["emailsSent"] = 450,
                            ["bounces"] = 3,
                            ["opens"] = 127
                        }
                    }
                },
                FlowGraph = new()
            };

            var workflow3 = new WorkflowInfo
            {
                Id = "sample-3",
                Name = "Example: Report Generator",
                Description = "Example workflow that generates weekly analytics reports and uploads to cloud storage",
                CreatedAt = DateTime.Now.AddDays(-15),
                ModifiedAt = DateTime.Now.AddDays(-15),
                Inputs = new Dictionary<string, string>
                {
                    ["reportType"] = "weekly",
                    ["includeCharts"] = "true"
                },
                FlowGraph = new()
            };

            AddWorkflow(workflow1);
            AddWorkflow(workflow2);
            AddWorkflow(workflow3);
        }
    }
}
