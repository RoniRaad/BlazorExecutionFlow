using System.Collections.Concurrent;
using System.Text.Json;
using DrawflowWrapper.Models.NodeV2;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static DrawflowWrapper.Models.DTOs.DfExport;

namespace DrawflowWrapper.Components;

public class DrawflowEventArgs : EventArgs
{
    public required string Name { get; init; }
    /// <summary>Raw JSON payload array from Drawflow event arguments.</summary>
    public required string PayloadJson { get; init; }

    /// <summary>Deserialize the payload as T.</summary>
    public T? GetPayload<T>() => JsonSerializer.Deserialize<T>(PayloadJson);
}

public partial class DrawflowBase : ComponentBase, IAsyncDisposable
{
    private DotNetObjectReference<DrawflowBase>? _selfRef;
    private bool _created;

    [Inject] public IJSRuntime JS { get; set; } = default!;

    /// <summary>DOM element id for this editor host.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Inline style (height/width). Default "height:500px;".</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Arbitrary options passed to Drawflow constructor.</summary>
    [Parameter] public Dictionary<string, object>? Options { get; set; }

    /// <summary>Fires for every Drawflow event name that is observed.</summary>
    [Parameter] public EventCallback<DrawflowEventArgs> OnEvent { get; set; }

    /// <summary>Called after the JS editor is created.</summary>
    [Parameter] public EventCallback OnReady { get; set; }
    // Your component/service already has something like:
 
    protected string ElementId => Id ?? $"df_{GetHashCode():x}";

    private static JsonSerializerOptions jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _selfRef ??= DotNetObjectReference.Create(this);

            var opts = Options ?? [];
            _created = await JS.InvokeAsync<bool>("DrawflowBlazor.create", ElementId, _selfRef, opts);
            if (_created)
            {
                await OnReady.InvokeAsync();
            }

            // Create the wrapper:
            Editor = new DrawflowWrapper.Drawflow.DrawflowEditor(
                callVoid: (m, a) => CallVoidAsync(m, a),
                callObject: (m, a) => JS.InvokeAsync<object?>("DrawflowBlazor.call", ElementId, m, a));
        }
    }

    [JSInvokable]
    public async Task OnDrawflowEvent(string name, string payloadJson)
    {
        if (name == "connectionCreated")
        {
            return;
        }

        if (OnEvent.HasDelegate)
        {
            await OnEvent.InvokeAsync(new DrawflowEventArgs { Name = name, PayloadJson = payloadJson });
        }
    }

    /// <summary>Subscribe to an event name (e.g., "nodeCreated").</summary>
    public async Task OnAsync(string eventName)
        => await JS.InvokeVoidAsync("DrawflowBlazor.on", ElementId, eventName);

    /// <summary>Unsubscribe from an event name.</summary>
    public async Task OffAsync(string eventName)
        => await JS.InvokeVoidAsync("DrawflowBlazor.off", ElementId, eventName);

    /// <summary>Call any Drawflow method dynamically (best-coverage path).</summary>
    public async Task<T?> CallAsync<T>(string methodName, params object[] args)
        => await JS.InvokeAsync<T?>("DrawflowBlazor.call", ElementId, methodName, args);

    public async Task<object?> CallAsync(string methodName, params object[] args)
        => await JS.InvokeAsync<object?>("DrawflowBlazor.call", ElementId, methodName, args);

    public DrawflowWrapper.Drawflow.DrawflowEditor? Editor { get; set; }
    ValueTask CallVoidAsync(string method, params object?[] args)
    => JS.InvokeVoidAsync("DrawflowBlazor.call", ElementId, method, args);

    /// <summary>Get an arbitrary property on the editor.</summary>
    public async Task<T?> GetAsync<T>(string propName)
        => await JS.InvokeAsync<T?>("DrawflowBlazor.get", ElementId, propName);

    /// <summary>Set an arbitrary property on the editor.</summary>
    public async Task SetAsync(string propName, object? value)
        => await JS.InvokeVoidAsync("DrawflowBlazor.set", ElementId, propName, value);
    public Graph GenerateGraphV2(DrawflowGraph graph)
    {
        var concurrentNodeDict = new ConcurrentDictionary<string, Node>();

        // Ensure every node in the page gets materialized
        foreach (var (id, dfNode) in graph.Page.Data)
        {
            _ = GenerateNodeV2(graph, dfNode, concurrentNodeDict);
        }

        return new Graph
        {
            Nodes = concurrentNodeDict
        };
    }
    public Node GenerateNodeV2(
    DrawflowGraph graph,
    DrawflowNode dfNode,
    ConcurrentDictionary<string, Node>? createdNodes = null)
    {
        createdNodes ??= [];

        var nodeKey = dfNode.Id.ToString();

        // Already created? just return it
        if (createdNodes.TryGetValue(nodeKey, out var existing))
        {
            return existing;
        }

        // 1. Rehydrate internal Node from the saved data
        if (dfNode.Data is null || !dfNode.Data.TryGetValue("node", out var nodeObj))
        {
            throw new InvalidOperationException($"Drawflow node {dfNode.Id} has no 'node' payload.");
        }

        var nodeJson = nodeObj.ToString();
        var internalNode = JsonSerializer.Deserialize<Node>(nodeJson, jsonSerializerOptions)
                          ?? throw new InvalidOperationException($"Failed to deserialize internal Node for Drawflow node {dfNode.Id}.");

        internalNode.DrawflowNodeId = nodeKey;
        internalNode.PosX = dfNode.PosX;
        internalNode.PosY = dfNode.PosY;

        // Put it in the map *before* recursing to handle cycles
        createdNodes[nodeKey] = internalNode;

        // 2. Build InputNodes from incoming edges
        var incomingEdges = graph.GetIncoming(nodeKey);
        var inputNodes = new List<Node>();

        foreach (var edge in incomingEdges)
        {
            var fromDfNode = graph.GetNode(edge.FromNodeId);
            var fromInternal = GenerateNodeV2(graph, fromDfNode, createdNodes);
            inputNodes.Add(fromInternal);
        }

        internalNode.InputNodes = [.. inputNodes];

        // 3. Build OutputNodes + OutputPorts from outgoing edges
        var outgoingEdges = graph.GetOutgoing(nodeKey);

        foreach (var edge in outgoingEdges)
        {
            var toDfNode = graph.GetNode(edge.ToNodeId);
            var toInternal = GenerateNodeV2(graph, toDfNode, createdNodes);

            // edge.FromOutputPort looks like "output_1", "output_2", ...
            var portName = "default";

            if (internalNode.DeclaredOutputPorts is { Count: > 0 } ports &&
                !string.IsNullOrWhiteSpace(edge.FromOutputPort))
            {
                var drawflowPortId = edge.FromOutputPort; // e.g. "output_1"
                var underscoreIndex = drawflowPortId.LastIndexOf('_');

                if (underscoreIndex >= 0 &&
                    int.TryParse(drawflowPortId[(underscoreIndex + 1)..], out var oneBasedIndex))
                {
                    var idx = oneBasedIndex - 1; // convert 1-based -> 0-based

                    if (idx >= 0 && idx < ports.Count)
                    {
                        portName = ports[idx];
                    }
                }
            }

            internalNode.AddOutputConnection(portName, toInternal);
        }

        return internalNode;
    }

    public async Task<Graph?> CreateInternalV2Graph()
    {
        if (Editor is null)
        {
            return null;
        }

        var drawflowExportObj = await Editor.ExportAsync();
        var drawflowJson = JsonSerializer.Serialize(drawflowExportObj);
        if (drawflowJson is null)
        {
            return null;
        }

        var graph = DrawflowGraph.Parse(this, drawflowJson);
        var internalGraph = GenerateGraphV2(graph);
        return internalGraph;
    }

    /// <summary>Destroy the editor instance.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (JS is not null && _created)
            {
                await JS.InvokeVoidAsync("DrawflowBlazor.destroy", ElementId);
            }
        }
        catch { /* ignore */ }
        finally
        {
            _selfRef?.Dispose();
        }
    }
}
