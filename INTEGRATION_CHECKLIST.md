# BlazorExecutionFlow Integration Checklist

## Issue: Node Status and Labels Not Showing in Your App

If node status animations (pulsing, error states) and port labels aren't displaying in your application but work in the example app, follow this checklist:

---

## 1. CSS File Loading ⚠️ CRITICAL

**Problem**: The CSS animations and styling won't work without the CSS file.

**Check**: Verify the CSS is being loaded in your app's `_Host.cshtml` or `App.razor`:

```html
<!-- In _Host.cshtml (Blazor Server) or index.html (Blazor WebAssembly) -->
<link href="_content/BlazorExecutionFlow/css/BlazorExecutionFlow.lib.module.css" rel="stylesheet" />
```

**Or in App.razor / MainLayout.razor:**

```razor
<link href="_content/BlazorExecutionFlow/css/BlazorExecutionFlow.lib.module.css" rel="stylesheet" />
```

**Key CSS Classes:**
- `.processing-bar` - Pulsing animation during node execution
- `.node-error` - Red border with glow for errors
- `.computed_node` - Green color for output ports with results

**Test**: Inspect a node in browser DevTools. If it has `processing-bar` class but no animation, CSS isn't loaded.

---

## 2. JavaScript Interop Loading ⚠️ CRITICAL

**Problem**: Status updates and labels use JavaScript interop.

**Check**: Verify JavaScript is loaded:

```html
<!-- In _Host.cshtml or index.html -->
<script src="_content/BlazorExecutionFlow/js/drawflowInterop.js"></script>
```

**Key Functions Used:**
- `DrawflowBlazor.setNodeStatus()` - Updates node visual state
- `DrawflowBlazor.labelPorts()` - Adds port labels
- `DrawflowBlazor.setNodeWidthFromTitle()` - Sizes nodes properly

**Test**: Open browser console and type:
```javascript
typeof DrawflowBlazor
```
Should return `"object"`, not `"undefined"`.

---

## 3. Component Initialization

**Problem**: Component must be properly initialized for events to work.

**Required Code in Your Page/Component:**

```razor
@page "/your-page"
@using BlazorExecutionFlow.Components

<BlazorExecutionFlowGraph
    Id="your-unique-id"
    @ref="_graphRef"
    Graph="@yourGraph"
    Style="height:600px;" />

@code {
    private BlazorExecutionFlowGraph? _graphRef;
    private NodeGraph yourGraph = new();

    protected override async Task OnInitializedAsync()
    {
        // Create your graph here
        yourGraph = new NodeGraph();

        // Add nodes, etc.
    }
}
```

**Common Mistakes:**
- ❌ Not providing a unique `Id`
- ❌ Not passing `Graph` parameter
- ❌ Creating nodes before component initializes

---

## 4. Event Handler Setup

**Problem**: Status updates require event handlers to be connected.

**Automatic Setup** (happens in `OnAfterRenderAsync`):
```csharp
// This is done automatically by BlazorExecutionFlowGraph component:
node.OnStartExecuting += (x, y) => { _ = SetStatusAsync(nodeId, true); };
node.OnStopExecuting += (x, y) => { _ = SetStatusAsync(nodeId, false); };
node.OnError += HandleNodeError;
```

**What You Need to Do:**
Nothing! Just make sure you're using the `BlazorExecutionFlowGraph` component (not manually creating the Drawflow editor).

**Test**: Set a breakpoint in a node's execution. The status should change when it runs.

---

## 5. Static Files Configuration

**Problem**: Blazor might not be serving static files from the library.

**Check `Program.cs` (or `Startup.cs`):**

For Blazor Server:
```csharp
app.UseStaticFiles();
```

For Blazor WebAssembly:
```csharp
builder.Services.AddScoped(sp => new HttpClient {
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
```

**Test**: Navigate to:
```
https://your-app/_content/BlazorExecutionFlow/css/BlazorExecutionFlow.lib.module.css
```
Should show CSS file, not 404.

---

## 6. Port Labels Specifically

**Problem**: Labels aren't showing on nodes with multiple output ports.

**Automatic Labeling** happens after import:
```csharp
// This is done automatically in OnAfterRenderAsync:
foreach (var node in Graph.Nodes)
{
    if (node.Value.DeclaredOutputPorts.Count > 0)
    {
        await JS.InvokeVoidAsync("DrawflowBlazor.labelPorts",
            Id,
            node.Value.DrawflowNodeId,
            new List<List<string>>(),
            node.Value.DeclaredOutputPorts.Select(x =>
                new List<string>() { x, "" }));
    }
}
```

**Check:**
- Are you using nodes with `DeclaredOutputPorts`?
- Are you calling `Graph.AddNode()` properly?

**Example node with ports:**
```csharp
var node = new Node
{
    Name = "MyNode",
    DeclaredOutputPorts = new List<string> { "success", "failure" }
};
yourGraph.AddNode(node);
```

---

## 7. Timing Issues

**Problem**: Status/labels applied before DOM is ready.

**Solution**: The component uses `nextFrame()` internally:
```javascript
await JS.InvokeVoidAsync("nextFrame");
```

**If you're programmatically updating:**
```csharp
await Task.Delay(100); // Give DOM time to render
await _graphRef.SetNodeStatusAsync(nodeId, status);
```

---

## 8. Browser Console Errors

**Check for JavaScript Errors:**

Open browser DevTools (F12) → Console tab

**Common Errors:**
- `DrawflowBlazor is not defined` → JS file not loaded
- `Cannot read property 'querySelector' of null` → Element ID mismatch
- `setNodeStatus is not a function` → Wrong JS file version

---

## 9. Complete Minimal Working Example

Here's a complete minimal example that **should work**:

### Your Page (e.g., `YourPage.razor`):
```razor
@page "/workflow"
@using BlazorExecutionFlow.Components
@using BlazorExecutionFlow.Models
@using BlazorExecutionFlow.Drawflow.BaseNodes

<h3>My Workflow</h3>

<BlazorExecutionFlowGraph
    Id="workflow-graph"
    @ref="_graph"
    Graph="@_nodeGraph"
    Style="height:600px;width:100%;" />

<button @onclick="RunWorkflow">Run</button>

@code {
    private BlazorExecutionFlowGraph? _graph;
    private NodeGraph _nodeGraph = new();

    protected override void OnInitialized()
    {
        // Create a simple node
        var logNode = NodeRegistry.CreateNode("Log", _nodeGraph);
        logNode.Inputs["message"].StringValue = "Hello World";

        _nodeGraph.AddNode(logNode);
    }

    private async Task RunWorkflow()
    {
        if (_graph?.Graph != null)
        {
            await _graph.Graph.RunAsync();
        }
    }
}
```

### Your `_Host.cshtml` (Blazor Server) or `index.html` (WASM):
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Your App</title>
    <base href="~/" />

    <!-- REQUIRED: BlazorExecutionFlow CSS -->
    <link href="_content/BlazorExecutionFlow/css/BlazorExecutionFlow.lib.module.css" rel="stylesheet" />

    <!-- Your other CSS -->
    <link href="css/site.css" rel="stylesheet" />
</head>
<body>
    <!-- Your content -->

    <!-- REQUIRED: BlazorExecutionFlow JS -->
    <script src="_content/BlazorExecutionFlow/js/drawflowInterop.js"></script>

    <!-- Blazor script -->
    <script src="_framework/blazor.server.js"></script>
</body>
</html>
```

### Your `Program.cs`:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

app.UseStaticFiles(); // ← REQUIRED

app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

---

## 10. Verification Steps

### Step 1: Check Browser Network Tab
1. Open DevTools (F12) → Network tab
2. Reload page
3. Search for `BlazorExecutionFlow.lib.module.css`
4. Should return 200 (not 404)
5. Search for `drawflowInterop.js`
6. Should return 200 (not 404)

### Step 2: Check Elements Tab
1. Inspect a node element
2. Should have class `drawflow-node`
3. When running, should add class `processing-bar`
4. When error, should add class `node-error`
5. Output ports should have class `output`

### Step 3: Check Console
1. Type: `DrawflowBlazor`
2. Should show object with methods
3. Type: `DrawflowBlazor.setNodeStatus`
4. Should show function

### Step 4: Test Status Update
```csharp
// In your code:
await _graph.SetNodeStatusAsync("node-id", new NodeStatus
{
    IsRunning = true
});

await Task.Delay(2000);

await _graph.SetNodeStatusAsync("node-id", new NodeStatus
{
    IsRunning = false,
    HasError = true,
    ErrorMessage = "Test error"
});
```

Node should pulse while running, then show red border with error.

---

## 11. Common Integration Mistakes

### ❌ Wrong: Using Drawflow Directly
```csharp
// Don't do this:
var editor = new DrawflowEditor(...);
editor.Import(json);
// Status and labels won't work!
```

### ✅ Correct: Using BlazorExecutionFlowGraph Component
```razor
<BlazorExecutionFlowGraph Graph="@_nodeGraph" />
```

---

### ❌ Wrong: Missing CSS/JS References
```html
<!-- Missing the library files -->
<link href="css/site.css" rel="stylesheet" />
<script src="_framework/blazor.server.js"></script>
```

### ✅ Correct: Including Library Files
```html
<link href="_content/BlazorExecutionFlow/css/BlazorExecutionFlow.lib.module.css" rel="stylesheet" />
<script src="_content/BlazorExecutionFlow/js/drawflowInterop.js"></script>
```

---

### ❌ Wrong: Manually Setting Status
```csharp
// Don't manually manipulate DOM
await JS.InvokeVoidAsync("eval", "document.querySelector('.drawflow-node').classList.add('processing-bar')");
```

### ✅ Correct: Using Built-in Methods
```csharp
await _graph.SetNodeStatusAsync(nodeId, new NodeStatus { IsRunning = true });
```

---

## 12. Still Not Working?

If you've checked everything above and it still doesn't work:

1. **Compare with Example App**
   - Copy `ExampleApp` folder
   - Run it to verify it works
   - Compare your `_Host.cshtml` with `ExampleApp`'s
   - Compare your `Program.cs` with `ExampleApp`'s

2. **Check Package Version**
   - Ensure you're using the same package version
   - Clear NuGet cache: `dotnet nuget locals all --clear`
   - Rebuild: `dotnet build --no-incremental`

3. **Browser Cache**
   - Hard refresh: Ctrl+Shift+R (or Cmd+Shift+R on Mac)
   - Clear browser cache
   - Try incognito/private mode

4. **Enable Verbose Logging**
   ```csharp
   // In Program.cs
   builder.Services.AddLogging(logging =>
   {
       logging.AddConsole();
       logging.SetMinimumLevel(LogLevel.Debug);
   });
   ```

5. **Create Minimal Repro**
   - Start with empty Blazor Server app
   - Add only BlazorExecutionFlow package
   - Add one component with one node
   - Test if status/labels work

---

## Quick Diagnostic Script

Add this to your page to diagnose the issue:

```razor
@inject IJSRuntime JS

<button @onclick="DiagnoseAsync">Diagnose</button>

@code {
    private async Task DiagnoseAsync()
    {
        Console.WriteLine("=== BlazorExecutionFlow Diagnostic ===");

        // Check if JS object exists
        var hasDrawflowBlazor = await JS.InvokeAsync<bool>("eval",
            "typeof DrawflowBlazor !== 'undefined'");
        Console.WriteLine($"DrawflowBlazor loaded: {hasDrawflowBlazor}");

        // Check if setNodeStatus exists
        if (hasDrawflowBlazor)
        {
            var hasSetNodeStatus = await JS.InvokeAsync<bool>("eval",
                "typeof DrawflowBlazor.setNodeStatus === 'function'");
            Console.WriteLine($"setNodeStatus available: {hasSetNodeStatus}");

            var hasLabelPorts = await JS.InvokeAsync<bool>("eval",
                "typeof DrawflowBlazor.labelPorts === 'function'");
            Console.WriteLine($"labelPorts available: {hasLabelPorts}");
        }

        // Check if component is initialized
        if (_graph != null)
        {
            Console.WriteLine($"Graph component initialized: {_graph.Id}");
            Console.WriteLine($"Node count: {_graph.Graph?.Nodes.Count ?? 0}");
        }
        else
        {
            Console.WriteLine("Graph component is NULL");
        }

        Console.WriteLine("=== End Diagnostic ===");
    }
}
```

Run this and check the browser console for results.
