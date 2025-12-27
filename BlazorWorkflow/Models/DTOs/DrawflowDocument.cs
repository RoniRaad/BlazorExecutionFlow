using System.Text.Json.Serialization;

namespace BlazorWorkflow.Models.DTOs;

public sealed class DrawflowDocument
{
    [JsonPropertyName("drawflow")]
    public Dictionary<string, DrawflowPage> Pages { get; init; } = new();

    public DrawflowPage? GetPage(string name) =>
        Pages.TryGetValue(name, out var page) ? page : null;
}
