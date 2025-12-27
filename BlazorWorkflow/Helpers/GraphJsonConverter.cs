using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorWorkflow.Models.NodeV2;

namespace BlazorWorkflow.Helpers
{
    /// <summary>
    /// Custom JSON converter for the Graph class.
    /// Uses FlowSerializer to properly handle node connections.
    /// </summary>
    public sealed class GraphJsonConverter : JsonConverter<Graph>
    {
        public override Graph? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            // Get the raw JSON as a string
            var json = root.GetRawText();

            // Use FlowSerializer to deserialize nodes with connections
            var nodes = FlowSerializer.DeserializeFlow(json);

            // Create a new Graph
            var graph = new Graph();
            graph.Nodes.Clear();

            // Add all nodes to the graph
            foreach (var node in nodes)
            {
                graph.Nodes[node.DrawflowNodeId] = node;
            }

            return graph;
        }

        public override void Write(Utf8JsonWriter writer, Graph value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            // Use FlowSerializer to serialize nodes with connections
            var json = FlowSerializer.SerializeFlow(value.Nodes.Values);

            // Write the serialized JSON directly
            using var doc = JsonDocument.Parse(json);
            doc.WriteTo(writer);
        }
    }
}
