using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models;
using Microsoft.Extensions.Logging;

namespace BlazorExecutionFlow.Repositories
{
    /// <summary>
    /// File-based implementation of IWorkflowRepository.
    /// Stores workflows as JSON files in a specified directory.
    /// </summary>
    public class FileBasedWorkflowRepository : IWorkflowRepository
    {
        private readonly string _storageDirectory;
        private readonly ILogger<FileBasedWorkflowRepository>? _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly object _lock = new();
        private readonly ConcurrentDictionary<string, WorkflowInfo> _cache = new();
        private bool _cacheLoaded = false;

        public FileBasedWorkflowRepository(string storageDirectory, ILogger<FileBasedWorkflowRepository>? logger = null)
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
        }

        public List<WorkflowInfo> GetAll()
        {
            lock (_lock)
            {
                EnsureCacheLoaded();
                return _cache.Values.ToList();
            }
        }

        public WorkflowInfo? GetById(string id)
        {
            lock (_lock)
            {
                EnsureCacheLoaded();
                return _cache.TryGetValue(id, out var workflow) ? workflow : null;
            }
        }

        public void Add(WorkflowInfo workflow)
        {
            lock (_lock)
            {
                try
                {
                    EnsureCacheLoaded();

                    // Add to cache first
                    _cache[workflow.Id] = workflow;

                    // Then persist to disk
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

        public void Update(WorkflowInfo workflow)
        {
            lock (_lock)
            {
                try
                {
                    EnsureCacheLoaded();

                    // Update cache first
                    _cache[workflow.Id] = workflow;

                    // Then persist to disk
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

        public void Delete(string id)
        {
            lock (_lock)
            {
                try
                {
                    EnsureCacheLoaded();

                    // Remove from cache first
                    _cache.TryRemove(id, out _);

                    // Then delete from disk
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

        private void EnsureCacheLoaded()
        {
            if (_cacheLoaded)
                return;

            try
            {
                _cache.Clear();
                var files = Directory.GetFiles(_storageDirectory, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var workflow = JsonSerializer.Deserialize<WorkflowInfo>(json, _jsonOptions);
                        if (workflow != null)
                        {
                            _cache[workflow.Id] = workflow;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to deserialize workflow from file: {File}", file);
                    }
                }

                _cacheLoaded = true;
                _logger?.LogInformation("Loaded {Count} workflows into cache", _cache.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load workflows into cache");
            }
        }

        private string GetWorkflowFilePath(string id)
        {
            // Sanitize ID to ensure it's a valid filename
            var safeId = string.Join("_", id.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_storageDirectory, $"{safeId}.json");
        }
    }
}
