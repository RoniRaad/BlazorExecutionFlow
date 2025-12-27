using System.Text.Json.Serialization;

namespace BlazorWorkflow.Models.DTOs;

public sealed class Port
{
    [JsonPropertyName("connections")]
    public List<PortConnection> Connections { get; init; } = new();
}
