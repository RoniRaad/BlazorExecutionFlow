# DrawflowWrapper (Blazor, .NET 8)

A lightweight Blazor Razor Class Library that wraps the [Drawflow](https://github.com/jerosoler/Drawflow) editor with JS interop.

## Features

- Works with .NET 8 (Blazor Server or WebAssembly)
- Dynamic method invocation: call **any** Drawflow method by name (`CallAsync("addNode", ...)` etc.)
- Event bridge: subscribe to **any** Drawflow event and receive a catch‑all `OnEvent` callback with the event name and JSON payload
- Property get/set helpers for advanced scenarios
- Minimal styling and a sample app included

> Because the wrapper exposes dynamic `CallAsync` and `OnEvent`, it covers **all current and future Drawflow methods and events** without needing to recompile the library.

## Install

1. Add a project reference to `DrawflowWrapper.csproj` from your Blazor app.
2. Ensure the Drawflow library is loaded in your host page via CDN (below).
3. Use the `<Drawflow />` component in any Razor page.

### Load Drawflow via CDN

Add to `wwwroot/index.html` (WASM) or `Pages/_Host.cshtml` (Server) **before** the closing `</body>`:

```html
<link rel="stylesheet" href="https://unpkg.com/drawflow/dist/drawflow.min.css" />
<script src="https://unpkg.com/drawflow/dist/drawflow.min.js"></script>
<link rel="stylesheet" href="_content/DrawflowWrapper/css/drawflowWrapper.css" />
```

> If you want to self-host assets, download those files and place them in your app's `wwwroot`, then update the paths accordingly.

## Usage

```razor
@page "/demo"
@using DrawflowWrapper.Components

<Drawflow Id="editor"
         Style="height:600px;"
         Options="new() { { "reroute", true } }"
         OnEvent="HandleEvent"
         @ref="editorRef" />

@code {
    private DrawflowBase? editorRef;

    private async Task HandleEvent(DrawflowEventArgs e)
    {
        Console.WriteLine($"Drawflow event: {e.Name} -> {e.PayloadJson}");
        // Example: react to node creation
        if (e.Name == "nodeCreated")
        {
            // ...
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && editorRef is not null)
        {
            // Example: add a node
            await editorRef.CallAsync<object>("addNode", "MyNode", 1, 1, 200, 200,
                "myclass",
                new { name = "demo" },   // data
                "My HTML",               // html string
                false);                  // typenode
        }
    }
}
```

### Common Methods (invoked dynamically)

- `addNode(name, num_in, num_out, pos_x, pos_y, class, data, html, typenode)`
- `updateNodeDataFromId(id, data)`
- `removeNodeId(id)`
- `addConnection(nodeIdOut, outputIndex, nodeIdIn, inputIndex, reroute?)`
- `removeSingleConnection(nodeIdOut, outputIndex, nodeIdIn, inputIndex)`
- `getNodesFromName(name)`
- `getNodeFromId(id)`
- `export()` -> returns the full flow as JSON
- `import(data)` -> load a flow
- `clear()`
- `zoom_in() / zoom_out() / zoom_reset()`
- `translate(x, y)`
- ...and any other method exposed by Drawflow

Invoke with:

```csharp
await editorRef!.CallAsync<object>("export");
await editorRef!.CallAsync<object>("import", jsonString);
```

### Events

Subscribe via `OnEvent` (catch‑all) or call `await editorRef.OnAsync("eventName")` to ensure a specific name is wired.

Known Drawflow events pre-wired by default include:
`nodeCreated`, `nodeRemoved`, `nodeSelected`, `nodeUnselected`, `nodeDataChanged`, `nodeMoved`, `connectionCreated`, `connectionRemoved`, `connectionSelected`, `connectionUnselected`, `moduleCreated`, `moduleChanged`, `moduleRemoved`, `import`, `zoom`, `translate`, `addReroute`, `removeReroute`

If you need another event, just call:

```csharp
await editorRef!.OnAsync("myCustomEvent");
```

## Sample App

The `SampleApp` demonstrates using the wrapper in a Blazor WebAssembly project. It references the library and shows basic interactions.
