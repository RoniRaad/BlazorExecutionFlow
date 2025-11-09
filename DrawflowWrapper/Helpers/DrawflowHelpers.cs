using System.Reflection;
using DrawflowWrapper.Components;
using DrawflowWrapper.Drawflow.Attributes;
using DrawflowWrapper.Drawflow.BaseNodes;
using DrawflowWrapper.Models;
using DrawflowWrapper.Models.NodeV2;
using Microsoft.JSInterop;
using NJsonSchema;

namespace DrawflowWrapper.Helpers
{
    public static class DrawflowHelpers
    {
        public static async Task<int> CreateEventNode(this DrawflowBase dfBase, string name)
        {
            var node = new FunctionNode()
            {
                Name = name,
                Outputs = [new() { BackingType = typeof(Action), PortType = DfPortType.Action, TypeStringName = "trigger" }],
                Type = NodeType.Event
            };

            return await dfBase.CreateNode(node, "▶︎");
        }

        public static async Task<int> CreateFunctionNode<TDelegate>(this DrawflowBase dfBase, TDelegate del) where TDelegate : Delegate
        {
            var nodeObject = GetNodeObject(del);
            return await dfBase.CreateFunctionNode(nodeObject);
        }
        public static async Task<int> CreateFunctionNode(this DrawflowBase dfBase, FunctionNode node)
        {
            if (node.Inputs.FirstOrDefault()?.TypeStringName != "trigger")
            {
                node.Inputs = [.. node.Inputs.Prepend(new() { BackingType = typeof(Action), PortType = DfPortType.Action, Name = "", TypeStringName = "trigger" })];
            }
            
            if (node.Outputs.FirstOrDefault()?.TypeStringName != "trigger")
            {
                node.Outputs = [.. node.Outputs.Prepend(new() { BackingType = typeof(Action), PortType = DfPortType.Action, Name = "", TypeStringName = "trigger" })];
            }

            node.Type = NodeType.Function;

            return await dfBase.CreateNode(node, "ƒ");
        }

        public static async Task<int> CreateBooleanNode(this DrawflowBase dfBase, FunctionNode node)
        {
            node.Type = NodeType.BooleanOperation;
            if (node.Inputs.FirstOrDefault()?.TypeStringName != "trigger")
            {
                node.Inputs = [.. node.Inputs.Prepend(new() { BackingType = typeof(Action), PortType = DfPortType.Action, Name = "", TypeStringName = "trigger" })];
            }

            return await dfBase.CreateNode(node, "β");
        }

        public static async Task<int> CreateLoopNode(this DrawflowBase dfBase, FunctionNode node)
        {
            node.Type = NodeType.Loop;
            node.Inputs = [.. node.Inputs.Prepend(new() { BackingType = typeof(Action), PortType = DfPortType.Action, Name = "", TypeStringName = "trigger" })];
            node.Outputs = [.. node.Outputs.Prepend(new() { BackingType = typeof(Action), PortType = DfPortType.Action, Name = "", TypeStringName = "trigger" })];
            return await dfBase.CreateNode(node, "♾️");
        }

        public static (DfPortType dfPortType, string typeName) GetPortType(Type type)
        {
            if (type == typeof(string))
                return (DfPortType.String, "string");

            if (type == typeof(int) || type == typeof(long) || type == typeof(short))
                return (DfPortType.Integer, "integer");

            if (type == typeof(bool))
                return (DfPortType.Boolean, "boolean");

            if (type == typeof(void))
                return (DfPortType.Null, "null");

            return (DfPortType.Object, type.Name);
        }

        public static async Task SetNodeStatus(this DrawflowBase dfBase, string nodeId, NodeStatus nodeStatus)
        {
            await dfBase.JS.InvokeVoidAsync("DrawflowBlazor.setNodeStatus",
                dfBase.Id,
                nodeId,
                nodeStatus
            );
        }

        public class NodeStatus
        {
            public bool IsRunning { get; set; }
            public Dictionary<int, object> OutputPortResults { get; set; } = [];
        }

        public static List<Node> GetNodesObjectsV2()
        {
            var nodes = new List<Node>();
            Type type = typeof(BaseNodeCollection);

            var methodsWithAttr = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetCustomAttributes(typeof(DrawflowNodeMethodAttribute), false).Length > 0);

            var branchingMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetCustomAttributes(typeof(DrawflowNodeBranchingMethodAttribute), false).Length > 0);

            foreach (var method in methodsWithAttr.Concat(branchingMethods))
            {
                var nodeType = NodeType.Function;
                var section = "Default";
                var parameters = method.GetParameters();
                var output = method.ReturnParameter;
                var (dfPortType, typeName) = GetPortType(method.ReturnType);
                List<DfPorts> dfOutputPorts = [];
                List<DfPorts> dfInputPorts = [];

                if (!branchingMethods.Contains(method))
                {
                    var functionAttribute = method.GetCustomAttribute(typeof(DrawflowNodeMethodAttribute)) as DrawflowNodeMethodAttribute;
                    section = functionAttribute?.Section ?? section;
                    nodeType = functionAttribute?.NodeType ?? nodeType;
                }
                else
                {
                    var functionAttribute = method.GetCustomAttribute(typeof(DrawflowNodeBranchingMethodAttribute)) as DrawflowNodeBranchingMethodAttribute;
                    section = functionAttribute?.Section ?? section;
                    nodeType = functionAttribute?.NodeType ?? nodeType;
                }

                var paramsFromPorts = parameters.Where(x => !x.CustomAttributes.Any()
                || x.CustomAttributes.All(attr => attr.AttributeType != typeof(DrawflowInputFieldAttribute)));

                var paramsFromInputFields = parameters.Where(x => x.CustomAttributes.Any(attr => attr.AttributeType == typeof(DrawflowInputFieldAttribute)));

                if (dfPortType != DfPortType.Null && !branchingMethods.Contains(method))
                {
                    if (output.ParameterType.IsAssignableFrom(typeof(Task)))
                    {
                        if (output.ParameterType.ContainsGenericParameters)
                        {
                            var genericReturnType = output.ParameterType.GetGenericArguments()[0];
                            dfOutputPorts = [new DfPorts() { Name = "result", PortType = dfPortType, TypeStringName = typeName, BackingType = genericReturnType }];
                        }
                    }
                    else
                    {
                        dfOutputPorts = [new DfPorts() { Name = "result", PortType = dfPortType, TypeStringName = typeName, BackingType = output.ParameterType }];
                    }
                }

                if (branchingMethods.Contains(method))
                {
                    var props = method.ReturnType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in props)
                    {
                        dfOutputPorts.Add(new DfPorts() { Name = prop.Name, PortType = DfPortType.Action, TypeStringName = "trigger", BackingType = typeof(Action) });
                    }
                }

                var node = new Node
                {
                    Section = section,
                    BackingMethod = method
                };

                var serializedMethod = MethodInfoHelpers.ToSerializableString(method);

                nodes.Add(node);
            }

            return nodes;
        }

        public static List<FunctionNode> GetNodesObjects()
        {
            var nodes = new List<FunctionNode>();
            Type type = typeof(BaseNodeCollection);

            var methodsWithAttr = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetCustomAttributes(typeof(DrawflowNodeMethodAttribute), false).Length > 0);

            var branchingMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetCustomAttributes(typeof(DrawflowNodeBranchingMethodAttribute), false).Length > 0); 

            foreach (var method in methodsWithAttr.Concat(branchingMethods))
            {
                var nodeType = NodeType.Function;
                var section = "Default";
                var parameters = method.GetParameters();
                var output = method.ReturnParameter;
                var (dfPortType, typeName) = GetPortType(method.ReturnType);
                List<DfPorts> dfOutputPorts = [];
                List<DfPorts> dfInputPorts = [];

                if (!branchingMethods.Contains(method))
                {
                    var functionAttribute = method.GetCustomAttribute(typeof(DrawflowNodeMethodAttribute)) as DrawflowNodeMethodAttribute;
                    section = functionAttribute?.Section ?? section;
                    nodeType = functionAttribute?.NodeType ?? nodeType;
                }
                else
                {
                    var functionAttribute = method.GetCustomAttribute(typeof(DrawflowNodeBranchingMethodAttribute)) as DrawflowNodeBranchingMethodAttribute;
                    section = functionAttribute?.Section ?? section;
                    nodeType = functionAttribute?.NodeType ?? nodeType;
                }

                var paramsFromPorts = parameters.Where(x => !x.CustomAttributes.Any()
                || x.CustomAttributes.All(attr => attr.AttributeType != typeof(DrawflowInputFieldAttribute)));

                var paramsFromInputFields = parameters.Where(x => x.CustomAttributes.Any(attr => attr.AttributeType == typeof(DrawflowInputFieldAttribute)));

                if (dfPortType != DfPortType.Null && !branchingMethods.Contains(method))
                {
                    if (output.ParameterType.IsAssignableFrom(typeof(Task)))
                    {
                        if (output.ParameterType.ContainsGenericParameters)
                        {
                            var genericReturnType = output.ParameterType.GetGenericArguments()[0];
                            dfOutputPorts = [new DfPorts() { Name = "result", PortType = dfPortType, TypeStringName = typeName, BackingType = genericReturnType }];
                        }
                    }
                    else
                    {
                        dfOutputPorts = [new DfPorts() { Name = "result", PortType = dfPortType, TypeStringName = typeName, BackingType = output.ParameterType }];
                    }
                }

                if (branchingMethods.Contains(method))
                {
                    var props = method.ReturnType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in props)
                    {
                        dfOutputPorts.Add(new DfPorts() { Name = prop.Name, PortType = DfPortType.Action, TypeStringName = "trigger", BackingType = typeof(Action) });
                    }
                }

                var node = new FunctionNode
                {
                    Name = StringHelpers.AddSpaces(method.Name),
                    Outputs = dfOutputPorts,
                    Inputs = [.. paramsFromPorts
                                .Where(x => !x.CustomAttributes
                                    .Any(y => y.AttributeType == typeof(DrawflowInputContextFieldAttribute)))
                                .Select(x => {
                                    var type = GetPortType(x.ParameterType);
                                    var port = new DfPorts()
                                    {
                                        Name = x.Name ?? "undefined",
                                        PortType = type.dfPortType,
                                        TypeStringName = type.typeName,
                                        BackingType = x.ParameterType
                                    };

                                    return port;
                                })],
                    FieldInputs = paramsFromInputFields
                                    .DistinctBy(x => x.ParameterType)
                                    .ToDictionary(x => x.ParameterType, x => x.Name!),
                    Type = nodeType,
                    Section = section
                };

                var serializedMethod = MethodInfoHelpers.ToSerializableString(method);

                if (serializedMethod is not null)
                {
                    node.FullBackingFunctionAssemblyNameWithParams = serializedMethod;
                }

                nodes.Add(node);
            }

            return nodes;
        }

        public static FunctionNode GetNodeObject<TDelegate>(TDelegate del) where TDelegate : Delegate
        {
            var method = del.Method;

            var parameters = method.GetParameters();
            var output = method.ReturnParameter;
            var (dfPortType, typeName) = GetPortType(method.ReturnType);
            List<DfPorts> dfOutputPorts = [];
            List<DfPorts> dfInputPorts = [];

            var paramsFromPorts = parameters.Where(x => !x.CustomAttributes.Any()
            || x.CustomAttributes.All(attr => attr.AttributeType != typeof(DrawflowInputFieldAttribute)));

            var paramsFromInputFields = parameters.Where(x => x.CustomAttributes.Any(attr => attr.AttributeType == typeof(DrawflowInputFieldAttribute)));

            if (dfPortType != DfPortType.Null)
            {
                if (output.ParameterType.IsAssignableFrom(typeof(Task)))
                {
                    if (output.ParameterType.ContainsGenericParameters)
                    {
                        var genericReturnType = output.ParameterType.GetGenericArguments()[0];
                        dfOutputPorts = [new DfPorts() { Name = "result", PortType = dfPortType, TypeStringName = typeName, BackingType = genericReturnType }];
                    }
                }
                else
                {
                    dfOutputPorts = [new DfPorts() { Name = "result", PortType = dfPortType, TypeStringName = typeName, BackingType = output.ParameterType }];
                }
            }

            var functionAttribute = (DrawflowNodeMethodAttribute)method.GetCustomAttribute(typeof(DrawflowNodeMethodAttribute));

            var node = new FunctionNode
            {
                Name = StringHelpers.AddSpaces(method.Name),
                Outputs = dfOutputPorts,
                Inputs = [.. paramsFromPorts
                                .Where(x => !x.CustomAttributes
                                    .Any(y => y.AttributeType == typeof(DrawflowInputContextFieldAttribute)))
                                .Select(x => {
                                    var type = GetPortType(x.ParameterType);
                                    var port = new DfPorts()
                                    {
                                        Name = x.Name ?? "undefined",
                                        PortType = type.dfPortType,
                                        TypeStringName = type.typeName,
                                        BackingType = x.ParameterType
                                    };

                                    return port;
                                })],
                FieldInputs = paramsFromInputFields.ToDictionary(x => x.ParameterType, x => x.Name!),
                Section = functionAttribute?.Section
            };

            var serializedMethod = MethodInfoHelpers.ToSerializableString(method);

            if (serializedMethod is not null)
            {
                node.FullBackingFunctionAssemblyNameWithParams = serializedMethod;
            }

            return node;
        }

        private static async Task<int> CreateNode(this DrawflowBase dfBase, FunctionNode node, string symbol)
        {
            Dictionary<string, string> typesSchemas = [];
            Dictionary<string, string> typesAssemblyPath = [];
            Dictionary<int, string> outputTypePortMap = [];
            Dictionary<int, string> inputTypePortMap = [];
            Dictionary<int, string> outputNamePortMap = [];
            Dictionary<int, string> inputNamePortMap = [];

            for (int i = 0; i < node.Outputs.Count; i++)
            {
                var outputNode = node.Outputs[i];
                if (outputNode.PortType == DfPortType.Action)
                {
                    outputTypePortMap[i] = "trigger";
                    outputNamePortMap[i] = node.Type == NodeType.Function ? "" : (outputNode.Name ?? "");

                    continue;
                }

                var type = outputNode.BackingType;
                var schema = JsonSchema.FromType(type);
                var schemaData = schema.ToJson();
                typesSchemas[type.Name] = schemaData;
                typesAssemblyPath[type.Name] = type.AssemblyQualifiedName!;
                outputTypePortMap[i] = type.Name!;
                outputNamePortMap[i] = outputNode.Name;
            }

            for (int i = 0; i < node.Inputs.Count; i++)
            {
                var inputNode = node.Inputs[i];
                if (inputNode.PortType == DfPortType.Action)
                {
                    inputTypePortMap[i] = "trigger";
                    inputNamePortMap[i] = "";

                    continue;
                }

                var type = inputNode.BackingType;
                var schema = JsonSchema.FromType(type);
                var schemaData = schema.ToJson();
                typesSchemas[type.Name] = schemaData;
                typesAssemblyPath[type.Name] = type.AssemblyQualifiedName!;
                inputTypePortMap[i] = type.Name!;
                inputNamePortMap[i] = inputNode.Name;
            }

            var inputHtml = "";

            foreach (var inputField in node.FieldInputs)
            {
                var type = inputField.Key;
                var name = inputField.Value;

                if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
                {
                    string inputType;

                    // Map C# type -> HTML input type
                    if (type == typeof(int) || type == typeof(long) || type == typeof(short))
                        inputType = "number";
                    else if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                        inputType = "number";
                    else if (type == typeof(bool))
                        inputType = "checkbox";
                    else if (type == typeof(string))
                        inputType = "text";
                    else if (type == typeof(char))
                        inputType = "text";
                    else if (type == typeof(DateTime))
                        inputType = "datetime-local";
                    else
                        inputType = "text"; // Fallback

                    // Generate the HTML input tag
                    if (type == typeof(bool))
                    {
                        // checkboxes handle checked state differently
                        inputHtml += $"<input type='{inputType}' data-param='{name}' />";
                    }
                    else
                    {
                        inputHtml += $"<input type='{inputType}' data-param='{name}' />";
                    }
                }
            }

            var nodeId = await dfBase.Editor!.AddNodeAsync(
                name: node.Name,
                inputs: node.Inputs.Count,
                outputs: node.Outputs.Count,
                x: 480, y: 260,
                cssClass: node.Type.ToString(),
                data: 
                new 
                {
                    typesSchemas,
                    typesAssemblyPath,
                    outputTypePortMap,
                    inputTypePortMap,
                    nodeType = node.Type.ToString(),
                    outputNamePortMap,
                    inputNamePortMap,
                    node.FullBackingFunctionAssemblyNameWithParams
                },
                html: $@"
                    <div class='node-type-id-container'>
                        <h5 class='node-type-id'>
                            {symbol}
                        </h5>
                    </div>
                    <div class='title-container'>
                        <div class='title' style='text-align: center;'>{node.Name}</div>
                    </div>
                    <div class='main-content'>
                        {inputHtml}
                    </div>
                    "
            );

            await dfBase.JS.InvokeVoidAsync("nextFrame");

            await dfBase.JS.InvokeVoidAsync("DrawflowBlazor.labelPorts",
                dfBase.Id,
                nodeId!.Value,
                node.Inputs.Select(x => new List<string>() { x.TypeStringName, x.Name }),
                node.Outputs.Select(x => new List<string>() { x.TypeStringName, x.Name })
            );

            return nodeId.Value;
        }

        public static async Task<int> CreateNodeV2(this DrawflowBase dfBase, Node node, string symbol)
        {
            var inputHtml = "";
            var nodeId = await dfBase.Editor!.AddNodeAsync(
                name: node.BackingMethod.Name,
                inputs: 1,
                outputs: 1,
                x: 480, y: 260,
                cssClass: "",
                data:
                new
                {
                    node
                },
                html: $@"
                    <div class='node-type-id-container'>
                        <h5 class='node-type-id'>
                            {symbol}
                        </h5>
                    </div>
                    <div class='title-container'>
                        <div class='title' style='text-align: center;'>{node.BackingMethod.Name}</div>
                    </div>
                    <div class='main-content' style='min-width:300px'>
                        {inputHtml}
                    </div>
                    "
            );

            await dfBase.JS.InvokeVoidAsync("nextFrame");

            return nodeId.Value;
        }
    }
}
