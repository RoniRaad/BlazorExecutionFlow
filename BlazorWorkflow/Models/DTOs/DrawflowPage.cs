using System.Text.Json.Serialization;

namespace BlazorWorkflow.Models.DTOs;

public sealed class DrawflowPage
{
    [JsonPropertyName("data")]
    public Dictionary<string, DrawflowNode> Data { get; init; } = new();
}
