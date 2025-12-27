using System.Text.Json.Serialization;

namespace BlazorWorkflow.Models.DTOs;

public sealed record DfPort
{
    [JsonPropertyName("connections")] public List<DfConnectionRef> Connections { get; init; } = new();
}
