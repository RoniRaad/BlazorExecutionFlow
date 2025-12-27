using System.Text.Json.Serialization;

namespace BlazorWorkflow.Models.DTOs;

public sealed record DfModule
{
    [JsonPropertyName("data")] public required Dictionary<string, DfNode> Data { get; init; }
}

