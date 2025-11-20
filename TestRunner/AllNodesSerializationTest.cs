using System.Text.Json;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models.NodeV2;

namespace TestRunner
{
    /// <summary>
    /// Comprehensive test that creates ALL available nodes, exports them, and imports them back
    /// to ensure every node type can be serialized and deserialized correctly.
    /// </summary>
    public static class AllNodesSerializationTest
    {
        public static async Task Run()
        {
            Console.WriteLine("=== All Nodes Serialization Test ===\n");

            bool allPassed = true;
            allPassed &= await TestAllNodesExportImport();

            Console.WriteLine();
            if (allPassed)
            {
                Console.WriteLine("✓ All nodes serialization test PASSED");
            }
            else
            {
                Console.WriteLine("✗ All nodes serialization test FAILED");
            }

            Console.WriteLine("\n=== End All Nodes Serialization Test ===\n");
        }

        private static async Task<bool> TestAllNodesExportImport()
        {
            Console.WriteLine("[Test 1] Create All Nodes, Export, and Import Back");
            try
            {
                // Step 1: Get all available node definitions
                var allNodeDefs = DrawflowHelpers.GetNodesObjectsV2();
                Console.WriteLine($"  Discovered {allNodeDefs.Count} node definitions");

                // Step 2: Create a graph with all nodes
                var graph = new Graph();
                var nodeIndex = 1;

                foreach (var nodeDef in allNodeDefs)
                {
                    // Create a node instance with proper initialization
                    var node = new Node
                    {
                        BackingMethod = nodeDef.BackingMethod,
                        Section = nodeDef.Section,
                        Id = Guid.NewGuid().ToString(),
                        DrawflowNodeId = nodeIndex.ToString(),
                        NodeInputToMethodInputMap = new List<PathMapEntry>(),
                        MethodOutputToNodeOutputMap = new List<PathMapEntry>(),
                        DeclaredOutputPorts = nodeDef.DeclaredOutputPorts
                    };

                    graph.Nodes[nodeIndex.ToString()] = node;
                    nodeIndex++;
                }

                Console.WriteLine($"  Created graph with {graph.Nodes.Count} nodes");

                // Step 3: Serialize the graph to JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    IncludeFields = true  // Required to serialize the Nodes field in Graph class
                };

                string json;
                try
                {
                    json = JsonSerializer.Serialize(graph, options);
                    Console.WriteLine($"  Serialized graph to JSON ({json.Length} bytes)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ FAILED: Serialization failed: {ex.Message}");
                    return false;
                }

                // Step 4: Deserialize the graph back from JSON
                Graph? deserializedGraph;
                try
                {
                    deserializedGraph = JsonSerializer.Deserialize<Graph>(json, options);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ FAILED: Deserialization failed: {ex.Message}");
                    Console.WriteLine($"  Stack trace: {ex.StackTrace}");
                    return false;
                }

                if (deserializedGraph == null)
                {
                    Console.WriteLine("  ✗ FAILED: Deserialized graph is null");
                    return false;
                }

                Console.WriteLine($"  Deserialized graph with {deserializedGraph.Nodes.Count} nodes");

                // Step 5: Verify all nodes were deserialized correctly
                if (deserializedGraph.Nodes.Count != graph.Nodes.Count)
                {
                    Console.WriteLine($"  ✗ FAILED: Node count mismatch - expected {graph.Nodes.Count}, got {deserializedGraph.Nodes.Count}");
                    return false;
                }

                // Step 6: Verify each node's BackingMethod was restored correctly
                var failedNodes = new List<string>();
                foreach (var (nodeId, originalNode) in graph.Nodes)
                {
                    if (!deserializedGraph.Nodes.TryGetValue(nodeId, out var deserializedNode))
                    {
                        failedNodes.Add($"{nodeId}: Missing from deserialized graph");
                        continue;
                    }

                    if (deserializedNode.BackingMethod == null)
                    {
                        failedNodes.Add($"{nodeId} ({originalNode.BackingMethod?.Name}): BackingMethod is null");
                        continue;
                    }

                    if (deserializedNode.BackingMethod.Name != originalNode.BackingMethod?.Name)
                    {
                        failedNodes.Add($"{nodeId}: Method name mismatch - expected {originalNode.BackingMethod?.Name}, got {deserializedNode.BackingMethod.Name}");
                        continue;
                    }

                    if (deserializedNode.Section != originalNode.Section)
                    {
                        failedNodes.Add($"{nodeId} ({originalNode.BackingMethod?.Name}): Section mismatch - expected {originalNode.Section}, got {deserializedNode.Section}");
                        continue;
                    }
                }

                if (failedNodes.Any())
                {
                    Console.WriteLine($"  ✗ FAILED: {failedNodes.Count} nodes failed verification:");
                    foreach (var failure in failedNodes.Take(10))
                    {
                        Console.WriteLine($"    - {failure}");
                    }
                    if (failedNodes.Count > 10)
                    {
                        Console.WriteLine($"    ... and {failedNodes.Count - 10} more");
                    }
                    return false;
                }

                // Step 7: Report detailed statistics
                var nodesBySection = deserializedGraph.Nodes.Values
                    .GroupBy(n => n.Section)
                    .OrderBy(g => g.Key)
                    .Select(g => new { Section = g.Key, Count = g.Count() })
                    .ToList();

                Console.WriteLine("\n  Node statistics by section:");
                foreach (var stat in nodesBySection)
                {
                    Console.WriteLine($"    {stat.Section}: {stat.Count} nodes");
                }

                Console.WriteLine($"\n  ✓ PASSED: All {deserializedGraph.Nodes.Count} nodes serialized and deserialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: Unexpected error: {ex.Message}");
                Console.WriteLine($"  Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}
