# Required References - Node Status & Labels Fix

## The Issue

If node status (pulsing animations, error states) and port labels aren't showing in your app, you're likely **missing required CSS/JS files**.

The example app loads **3 CSS files + 1 JS file** from the BlazorExecutionFlow library:

---

## Required Files in Your App.razor or _Host.cshtml

### ✅ MUST HAVE - Copy these EXACTLY:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />

    <!-- Your other CSS here -->

    <!-- ⚠️ REQUIRED: Core Drawflow CSS (from CDN) -->
    <link rel="stylesheet" href="https://unpkg.com/drawflow/dist/drawflow.min.css" />

    <!-- ⚠️ REQUIRED: BlazorExecutionFlow CSS Files (ALL 3) -->
    <link rel="stylesheet" href="_content/BlazorExecutionFlow/css/drawflowWrapper.css" />
    <link rel="stylesheet" href="_content/BlazorExecutionFlow/css/BlazorExecutionFlow.lib.module.css" />
    <link rel="stylesheet" href="_content/BlazorExecutionFlow/BlazorExecutionFlow.bundle.scp.css" />

    <!-- ⚠️ REQUIRED: BlazorExecutionFlow JavaScript -->
    <script src="_content/BlazorExecutionFlow/js/drawflowInterop.js"></script>

    <HeadOutlet />
</head>
<body>
    <Routes />

    <!-- ⚠️ REQUIRED: Core Drawflow JS (from CDN) - BEFORE blazor.web.js -->
    <script src="https://unpkg.com/drawflow/dist/drawflow.min.js"></script>

    <!-- Your Blazor script -->
    <script src="_framework/blazor.web.js"></script>
</body>
</html>
```

---

## What Each File Does

### 1. `drawflow.min.css` (CDN)
- Core Drawflow library styling
- Node structure, connections, canvas

### 2. `_content/BlazorExecutionFlow/css/drawflowWrapper.css`
- Wrapper styles for the Blazor component
- Layout and positioning

### 3. `_content/BlazorExecutionFlow/css/BlazorExecutionFlow.lib.module.css` ⭐ **STATUS & LABELS**
- **Node status animations** (`.processing-bar`, `.node-error`)
- **Port label styling** (`.port_label`, `.computed_node`)
- Error states, pulsing effects, colors
- **This is the most critical file for your issue!**

### 4. `_content/BlazorExecutionFlow/BlazorExecutionFlow.bundle.scp.css`
- Scoped component styles
- Additional component-specific styling

### 5. `drawflowInterop.js`
- JavaScript functions for status updates
- `setNodeStatus()` - Updates node visual state
- `labelPorts()` - Adds port labels
- `setNodeWidthFromTitle()` - Sizes nodes

### 6. `drawflow.min.js` (CDN)
- Core Drawflow library JavaScript
- Must load BEFORE Blazor script

---

## Order Matters!

### CSS Load Order:
1. drawflow.min.css (CDN)
2. drawflowWrapper.css
3. BlazorExecutionFlow.lib.module.css ⭐
4. BlazorExecutionFlow.bundle.scp.css

### JS Load Order:
1. drawflowInterop.js (in `<head>`)
2. drawflow.min.js (CDN, before Blazor)
3. blazor.web.js (Blazor framework)

---

## Quick Fix

If your app currently has:

```html
<!-- ❌ INCOMPLETE - Missing files -->
<link href="_content/BlazorExecutionFlow/css/BlazorExecutionFlow.lib.module.css" rel="stylesheet" />
<script src="_content/BlazorExecutionFlow/js/drawflowInterop.js"></script>
```

Change to:

```html
<!-- ✅ COMPLETE - All required files -->
<link rel="stylesheet" href="https://unpkg.com/drawflow/dist/drawflow.min.css" />
<link rel="stylesheet" href="_content/BlazorExecutionFlow/css/drawflowWrapper.css" />
<link rel="stylesheet" href="_content/BlazorExecutionFlow/css/BlazorExecutionFlow.lib.module.css" />
<link rel="stylesheet" href="_content/BlazorExecutionFlow/BlazorExecutionFlow.bundle.scp.css" />
<script src="_content/BlazorExecutionFlow/js/drawflowInterop.js"></script>
```

And in `<body>`, before your Blazor script:

```html
<script src="https://unpkg.com/drawflow/dist/drawflow.min.js"></script>
<script src="_framework/blazor.web.js"></script>
```

---

## Verification

After adding all files, **hard refresh** (Ctrl+Shift+R) and check:

### 1. Browser Network Tab (F12)
All these should return **200 OK**:
- ✅ drawflow.min.css
- ✅ drawflowWrapper.css
- ✅ BlazorExecutionFlow.lib.module.css
- ✅ BlazorExecutionFlow.bundle.scp.css
- ✅ drawflowInterop.js
- ✅ drawflow.min.js

If any return **404**, the file isn't being served correctly.

### 2. Browser Console (F12 → Console)
Type:
```javascript
typeof DrawflowBlazor
```
Should return: `"object"`

Type:
```javascript
typeof Drawflow
```
Should return: `"function"`

### 3. Node Visual Check
When a node runs:
- Should have pulsing title bar animation
- On error, should have red border
- Output ports with results should be green

If these don't happen, check:
1. CSS files loaded (Network tab)
2. No JavaScript errors (Console tab)
3. Node element has correct classes (Elements tab)

---

## Blazor Server vs WebAssembly

### Blazor Server (`App.razor` or `_Host.cshtml`):
```html
<!DOCTYPE html>
<html>
<head>
    <!-- CSS files here -->
    <link rel="stylesheet" href="https://unpkg.com/drawflow/dist/drawflow.min.css" />
    <link rel="stylesheet" href="_content/BlazorExecutionFlow/css/drawflowWrapper.css" />
    <link rel="stylesheet" href="_content/BlazorExecutionFlow/css/BlazorExecutionFlow.lib.module.css" />
    <link rel="stylesheet" href="_content/BlazorExecutionFlow/BlazorExecutionFlow.bundle.scp.css" />
    <script src="_content/BlazorExecutionFlow/js/drawflowInterop.js"></script>
</head>
<body>
    <component type="typeof(App)" render-mode="ServerPrerendered" />

    <script src="https://unpkg.com/drawflow/dist/drawflow.min.js"></script>
    <script src="_framework/blazor.server.js"></script>
</body>
</html>
```

### Blazor WebAssembly (`wwwroot/index.html`):
```html
<!DOCTYPE html>
<html>
<head>
    <!-- CSS files here -->
    <link rel="stylesheet" href="https://unpkg.com/drawflow/dist/drawflow.min.css" />
    <link rel="stylesheet" href="_content/BlazorExecutionFlow/css/drawflowWrapper.css" />
    <link rel="stylesheet" href="_content/BlazorExecutionFlow/css/BlazorExecutionFlow.lib.module.css" />
    <link rel="stylesheet" href="_content/BlazorExecutionFlow/BlazorExecutionFlow.bundle.scp.css" />
    <script src="_content/BlazorExecutionFlow/js/drawflowInterop.js"></script>
</head>
<body>
    <div id="app">Loading...</div>

    <script src="https://unpkg.com/drawflow/dist/drawflow.min.js"></script>
    <script src="_framework/blazor.webassembly.js"></script>
</body>
</html>
```

---

## Still Not Working?

If you've added all files and it's still not working:

1. **Clear Browser Cache**
   - Hard refresh: Ctrl+Shift+R
   - Or clear all browser data

2. **Rebuild Your App**
   ```bash
   dotnet clean
   dotnet build
   ```

3. **Check File Paths**
   Open browser DevTools → Network tab
   - Find a failed CSS file (if any)
   - Check what path it's trying to load from
   - Compare with the actual file path

4. **Try the Example App**
   - Run the ExampleApp from the repo
   - Verify status and labels work there
   - Compare your App.razor line-by-line

5. **Check Package Version**
   Ensure you have the latest package:
   ```bash
   dotnet list package
   dotnet add package BlazorExecutionFlow --version <latest>
   ```

---

## Cache Busting (Optional)

The example app uses cache busting with random version numbers:

```html
<link rel="stylesheet" href="_content/BlazorExecutionFlow/css/BlazorExecutionFlow.lib.module.css?v=@randomNum" />

@code{
    int randomNum = (new Random()).Next();
}
```

This forces browser to reload CSS on every page load during development. You can use this if you're making frequent changes.
