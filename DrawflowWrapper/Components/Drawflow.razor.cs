using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using DrawflowWrapper.Drawflow.Attributes;
using DrawflowWrapper.Drawflow.BaseNodes;
using DrawflowWrapper.Helpers;
using DrawflowWrapper.Models;
using DrawflowWrapper.Models.DTOs;
using DrawflowWrapper.Models.NodeV2;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static DrawflowWrapper.Helpers.DrawflowHelpers;
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

    public async Task CreateInternalGraph()
    {
        var drawflowExportObj = await Editor.ExportAsync();
        var drawflowJson = JsonSerializer.Serialize(drawflowExportObj);
        if (drawflowJson != null)
        {
            var graph = DrawflowGraph.Parse(this, drawflowJson);
            var events = graph.Page.Data.Where(kvp => kvp.Value.Class == NodeType.Event.ToString());
            foreach (var eventNode in events)
            {
                await ExecuteFlowFromEventNode(graph, eventNode.Value).ConfigureAwait(false);
            }
        }
    }

    public Graph GenerateGraph(DrawflowGraph graph)
    { 
        var concurentNodeDict = new ConcurrentDictionary<string, Node>();

        var nodesDict = graph
            .Page
            .Data
            .ToDictionary(x => x.Key, x => GenerateNode(graph, x.Value, concurentNodeDict));

        var returnGraph = new Graph()
        {
            Nodes = concurentNodeDict
        };

        return returnGraph;
    }

    public Node GenerateNode(DrawflowGraph graph, DrawflowNode node, ConcurrentDictionary<string, Node>? createdNodes = null)
    {
        createdNodes ??= [];

        if (createdNodes.TryGetValue(node.Id.ToString(), out var value))
        {
            return value;
        }

        var inputEdges = graph.GetIncoming(node.Id.ToString());
        var inputNodes = new List<DrawflowNode>();

        var outputEdges = graph.GetOutgoing(node.Id.ToString());
        var outputNodes = new List<DrawflowNode>();

        foreach (var inputEdge in inputEdges)
        {
            var otherNodeId = inputEdge.FromNodeId;
            var otherNode = graph.GetNode(otherNodeId);
            inputNodes.Add(otherNode);
        }

        foreach (var outputEdge in outputEdges)
        {
            var otherNodeId = outputEdge.ToNodeId;
            var otherNode = graph.GetNode(otherNodeId);
            outputNodes.Add(otherNode);
        }

        var internalNode = JsonSerializer.Deserialize<Node>(node.Data["node"].ToString(), jsonSerializerOptions);
        createdNodes[node.Id.ToString()] = internalNode!;

        internalNode!.InputNodes = [..inputNodes.Select(x => GenerateNode(graph, x, createdNodes))];
        internalNode!.OutputNodes = [..outputNodes.Select(x => GenerateNode(graph, x, createdNodes))];

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
        JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var internalGraph = GenerateGraph(graph);
        return internalGraph;
    }

    public ConcurrentDictionary<string, DrawflowNodeExecutionGraph> nodes = [];

    public DrawflowExecutionEdge CreateExecutionEdge(DrawflowGraph drawflowGraph, Edge edge)
    {
        var returnEdge = new DrawflowExecutionEdge()
        {
            FromNodeId = edge.FromNodeId,
            ToNodeId = edge.ToNodeId,
            FromOutputPortId = edge.FromOutputPort,
            ToInputPortId = edge.ToInputPort,
        };

        if (nodes.TryGetValue(edge.ToNodeId, out var toNode))
        {
            returnEdge.ToNode = toNode;
        }
        else
        {
            returnEdge.ToNode = CreateInternalNode(drawflowGraph, drawflowGraph.GetNode(edge.ToNodeId));
        }

        if (nodes.TryGetValue(edge.FromNodeId, out var fromNode))
        {
            returnEdge.FromNode = fromNode;
        }
        else
        {
            returnEdge.FromNode = CreateInternalNode(drawflowGraph, drawflowGraph.GetNode(edge.FromNodeId));
        }

        return returnEdge;
    }

    public DrawflowExecutionPort CreateInternalPort(DrawflowGraph drawflowGraph, DrawflowNode node, 
        string portId, List<Edge> edges)
    {
        var portIdSplit = portId.Split('_');
        var direction = portIdSplit[0];
        var portIdInt = int.Parse(portIdSplit[1]);

        node.Data.TryGetValue("typesAssemblyPath", out var typesAssemblyPathString);
        node.Data.TryGetValue($"{direction}TypePortMap", out var typePortMapString);
        node.Data.TryGetValue($"{direction}NamePortMap", out var namePortMapString);

        var connectedNodeOutputTypePortMap = ConvertObjectUsingJsonSerializer<Dictionary<int, string>>(typePortMapString);
        var connectedTypesAssemblyPath = ConvertObjectUsingJsonSerializer<Dictionary<string, string>>(typesAssemblyPathString);
        var namePortMap = ConvertObjectUsingJsonSerializer<Dictionary<int, string>>(namePortMapString);
        var portName = connectedNodeOutputTypePortMap?[portIdInt - 1];

        if (!string.IsNullOrEmpty(portName) 
            && connectedTypesAssemblyPath.TryGetValue(portName, out var typeName))
        {
            var backingType = Type.GetType(typeName)!;
            (var portType, _) = GetPortType(backingType);

            var returnPorts = new DrawflowExecutionPort()
            {
                BackingType = backingType,
                Connections = [.. edges.Select(x => CreateExecutionEdge(drawflowGraph, x))],
                Name = namePortMap[portIdInt - 1].ToString(),
                PortType = portType,
                Pos = portIdInt - 1
            };

            return returnPorts;
        }

        return new DrawflowExecutionPort() { BackingType = typeof(Action), Name = namePortMap[portIdInt - 1].ToString(), PortType = DfPortType.Action, Connections = [.. edges.Select(x => CreateExecutionEdge(drawflowGraph, x))] };
    }

    public DrawflowNodeExecutionGraph CreateInternalNode(DrawflowGraph drawflowGraph, DrawflowNode node)
    {
        if (nodes.TryGetValue(node.Id.ToString(), out var createdNode))
        {
            return createdNode;
        }

        var returnNode = new DrawflowNodeExecutionGraph()
        {
            Id = node.Id.ToString(),
        };

        nodes[node.Id.ToString()] = returnNode;
        var outGoingEdges = drawflowGraph.GetOutgoing(node.Id.ToString()).ToList();
        var incomingEdges = drawflowGraph.GetIncoming(node.Id.ToString()).ToList();

        List<DrawflowExecutionPort> unorderedOutputs = [.. node.Outputs
            .Where(x => x.Key.Contains("output"))
            .Select(x => CreateInternalPort(drawflowGraph,
                node, x.Key, [..
                    outGoingEdges.Where(y => y.FromNodeId == node.Id.ToString() && y.FromOutputPort == x.Key)]))];


        returnNode.Outputs = [.. unorderedOutputs.OrderBy(x => x.Pos)];
        returnNode.Inputs = [.. 
            node.Inputs
            .Where(x => x.Key.Contains("input"))
            .Select(x => CreateInternalPort(drawflowGraph, node, x.Key, 
                [.. incomingEdges.Where(y => y.ToNodeId == node.Id.ToString() && y.ToInputPort == x.Key)]))];
       
        returnNode.Type = Enum.Parse<NodeType>(node?.Data?["nodeType"].ToString()!);
        if (node?.Data?.TryGetValue("fullBackingFunctionAssemblyNameWithParams", out var fullBackingFunctionAssemblyNameWithParams) is true
            && fullBackingFunctionAssemblyNameWithParams is not null)
        {
            returnNode.ExecutionMethod = MethodInfoHelpers.FromSerializableString(fullBackingFunctionAssemblyNameWithParams.ToString());
            if (node?.Data?.TryGetValue("params", out var paramsObj) is true
            && paramsObj is not null)
            {
                var paramsValue = ConvertObjectUsingJsonSerializer<Dictionary<string, JsonElement>>(paramsObj);
                returnNode.ExecutionParams = paramsValue.ToDictionary(x => x.Key, x => x.Value.ToValue()) ?? [];
            }
        }

        return returnNode;
    }

    public async Task ExecuteFlow()
    {
        var drawflowExportObj = await Editor.ExportAsync();
        var drawflowJson = JsonSerializer.Serialize(drawflowExportObj);
        if (drawflowJson != null)
        {
            var graph = DrawflowGraph.Parse(this, drawflowJson);
            var events = graph.Page.Data.Where(kvp => kvp.Value.Class == NodeType.Event.ToString());
            foreach (var eventNode in events)
            {
                await ExecuteFlowFromEventNode(graph, eventNode.Value);
            }
        }
    }

    public static async Task ExecuteNode(DrawflowGraph drawflowGraph, DrawflowNodeExecutionGraph node)
    {
        async Task SetStatusAsync(bool isRunning, int? outputPos = null, object? value = null)
        {
            var status = new NodeStatus { IsRunning = isRunning };
            if (outputPos is not null)
            {
                status.OutputPortResults[outputPos.Value] = value!;
            }
            await drawflowGraph.DrawflowBase.SetNodeStatus(node.Id, status).ConfigureAwait(false);
        }

        static Type UnwrapTaskResultType(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>))
                return t.GetGenericArguments()[0];
            return t == typeof(Task) ? typeof(void) : t;
        }

        static object? Coerce(object? value, Type targetType)
        {
            if (value is null) return null;

            var valueType = value.GetType();
            if (targetType.IsAssignableFrom(valueType)) return value;

            // Handle common numeric coercions explicitly (e.g., long -> int)
            if (targetType == typeof(int) && value is long l) return checked((int)l);
            if (targetType == typeof(long) && value is int i) return (long)i;

            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                // Last resort: return original; caller can decide what to do.
                return value;
            }
        }

        async Task<object?> AwaitIfTaskAsync(object? returnValue)
        {
            if (returnValue is not Task task) return returnValue;

            await task.ConfigureAwait(false);

            var t = returnValue.GetType();
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // Read Task<T>.Result via reflection
                var resultProp = t.GetProperty("Result");
                return resultProp?.GetValue(returnValue);
            }

            // Task (non-generic) completes with no result
            return null;
        }

        await SetStatusAsync(true).ConfigureAwait(false);

        try
        {
            if (node.Type == NodeType.Function
                || node.Type == NodeType.BooleanOperation
                || node.Type == NodeType.Loop)
            {
                // 1) Collect/compute input values in parallel, overlay onto execution params
                var unordered = new Dictionary<string, object?>(node.ExecutionParams, StringComparer.Ordinal);

                // Build a list of (name, task) to await together
                var valueTasks = node.Inputs
                    .Where(inp => inp.PortType != DfPortType.Action)
                    .Select(inp =>
                    {
                        var name = inp.Name!;
                        var task = (inp.Connections?.FirstOrDefault()?.FromOutputPort?.ComputedValue)
                                   ?? Task.FromResult<object?>(null);
                        return (name, task);
                    })
                    .ToList();

                // Await all at once
                var awaited = await Task.WhenAll(valueTasks.Select(tuple => tuple.task)).ConfigureAwait(false);
                for (int idx = 0; idx < valueTasks.Count; idx++)
                {
                    unordered[valueTasks[idx].name] = awaited[idx];
                }

                // 2) Order parameters to match the method signature (with coercion)
                var method = node.ExecutionMethod;
                var paramInfos = method.GetParameters();
                var orderedParams = new object?[paramInfos.Length];

                for (int p = 0; p < paramInfos.Length; p++)
                {
                    var pi = paramInfos[p];
                    if (pi.CustomAttributes
                        .Any(x => x.AttributeType == typeof(DrawflowInputContextFieldAttribute)))
                    {
                        if (node.Type != NodeType.Loop)
                        {
                            throw new InvalidOperationException("DrawflowInputContextFieldAttribute cannot be used outside of a Loop type node");
                        }
                        var loopContext = new LoopContext
                        {
                            RunBodyAsync = async () =>
                            {
                                var firstOutput = node.Outputs?.FirstOrDefault();
                                if (firstOutput?.PortType == DfPortType.Action && firstOutput.Connections?.Count > 0)
                                {
                                    foreach (var conn in firstOutput.Connections)
                                    {
                                        _ = ExecuteNode(drawflowGraph, conn.ToNode);
                                    }
                                }

                                await SetStatusAsync(false).ConfigureAwait(false);
                            },

                            RunDoneAsync = async () =>
                            {
                                // no cleanup needed
                            }
                        };

                        orderedParams[p] = loopContext;
                    }
                    else
                    {
                        unordered.TryGetValue(pi.Name!, out var provided);
                        orderedParams[p] = Coerce(provided, pi.ParameterType);
                    }
                }

                // 3) Invoke and unwrap/await result if it is a Task
                var rawReturn = method.Invoke(null, orderedParams);
                var result = await AwaitIfTaskAsync(rawReturn).ConfigureAwait(false);

                // 4) Route result to the correct output (match by unwrapped return type)
                var resultType = UnwrapTaskResultType(method.ReturnType);
                var matchedOutput = node.Outputs.FirstOrDefault(x => x.BackingType == resultType);

                if (matchedOutput is not null)
                {
                    // Final tiny safety: coerce runtime value to the backing type if needed
                    var coerced = Coerce(result, matchedOutput.BackingType);
                    matchedOutput.SetResult(coerced);

                    await SetStatusAsync(false, matchedOutput.Pos, coerced).ConfigureAwait(false);
                }
                else
                {
                    // No output port to receive the value—just mark done
                    await SetStatusAsync(false).ConfigureAwait(false);
                }

                if (node.Type == NodeType.BooleanOperation)
                {
                    var conditionBranchingTriggers = resultType.GetProperties()
                        .Where(x => x.CustomAttributes.Any(y => y.AttributeType == typeof(DrawflowOutputTriggerActionAttribute)));

                    foreach (var branch in conditionBranchingTriggers)
                    {
                        var value = branch.GetValue(result);
                        if (value?.GetType() == typeof(bool) && (bool)value)
                        {
                            node.Outputs.FirstOrDefault(x => x.Name == branch.Name)
                                .Connections.ForEach(x =>
                                {
                                    _ = ExecuteNode(drawflowGraph, x.ToNode);
                                });
                        }
                    }

                    await SetStatusAsync(false).ConfigureAwait(false);
                    return;
                }
                else if (node.Type == NodeType.Function)
                {
                    // 5) Trigger action outputs (fire-and-forget semantics preserved)
                    var firstOut = node.Outputs?.FirstOrDefault();
                    if (firstOut?.PortType == DfPortType.Action && firstOut.Connections?.Count > 0)
                    {
                        foreach (var conn in firstOut.Connections)
                        {
                            _ = ExecuteNode(drawflowGraph, conn.ToNode);
                        }
                    }
                }
            }
            else if (node.Type == NodeType.Event)
            {
                // Fan-out to all connected nodes and wait for them
                var nextNodes = node.Outputs.SelectMany(o => o.Connections).Select(c => c.ToNode).ToList();
                await Task.WhenAll(nextNodes.Select(n => ExecuteNode(drawflowGraph, n))).ConfigureAwait(false);
                await SetStatusAsync(false).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            // Ideally capture/log the exception somewhere; at minimum mark node as not running.
            // Optionally: write ex to a logger on drawflowGraph if available.
            await SetStatusAsync(false).ConfigureAwait(false);
            throw; // or swallow if the engine wants isolated failures; adjust to your needs
        }
    }

    public async Task ExecuteFlowFromEventNode(DrawflowGraph drawflowGraph, DrawflowNode eventNode)
    {
        nodes = [];
        var eventTree = CreateInternalNode(drawflowGraph, eventNode);
        await ExecuteNode(drawflowGraph, eventTree);
    }

    public class DrawflowNodeExecutionGraph
    {
        public string Id { get; set; }
        public List<DrawflowExecutionPort> Outputs { get; set; } = [];
        public List<DrawflowExecutionPort> Inputs { get; set; } = [];
        public NodeType Type { get; set; }
        public MethodInfo? ExecutionMethod { get; set; }
        public Dictionary<string, object> ExecutionParams { get; set; } = [];
    }
    public class DrawflowExecutionPort
    {
        private readonly TaskCompletionSource<object?> TaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string Name { get; set; }
        public int Pos { get; set; }
        public List<DrawflowExecutionEdge> Connections { get; set; }
        public DfPortType PortType { get; set; }
        public required Type BackingType { get; set; }
        public Task<object?> ComputedValue => TaskCompletionSource.Task;

        public void SetResult(object? result)
        {
            TaskCompletionSource.SetResult(result);
        }
    }

    public class DrawflowExecutionEdge
    {
        public string FromNodeId { get; init; } = "";
        public string FromOutputPortId { get; init; } = ""; // e.g., "output_1"
        public string ToNodeId { get; init; } = "";
        public string ToInputPortId { get; init; } = "";    // e.g., "input_2"
        public string InputPortTypeName { get; set; } = "";
        public string OutputPortTypeName { get; set; } = "";
        public DrawflowNodeExecutionGraph? ToNode { get; set; }
        public DrawflowNodeExecutionGraph? FromNode { get; set; }
        public DrawflowExecutionPort FromOutputPort => 
            FromNode.Outputs.FirstOrDefault(x => (x.Pos + 1).ToString() == FromOutputPortId.Split('_')[1]);
        public DrawflowExecutionPort? ToInputPort =>
            ToNode.Inputs.FirstOrDefault(x => (x.Pos + 1).ToString() == ToInputPortId.Split('_')[1]);
    }

    public T? ConvertObjectUsingJsonSerializer<T>(object obj) where T : class
    {
        var objString = JsonSerializer.Serialize(obj);
        try
        {
            return JsonSerializer.Deserialize<T>(objString);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Destroy the editor instance.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (JS is not null && _created)
            {
              //  await JS.InvokeVoidAsync("DrawflowBlazor.destroy", ElementId);
            }
        }
        catch { /* ignore */ }
        finally
        {
            _selfRef?.Dispose();
        }
    }
}
