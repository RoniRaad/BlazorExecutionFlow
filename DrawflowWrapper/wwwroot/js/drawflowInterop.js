// wwwroot/js/drawflowInterop.js
window.DrawflowBlazor = (function () {
    const instances = new Map();

    function ensureInstance(id) {
        if (!instances.has(id)) {
            throw new Error("Drawflow instance not found for id: " + id);
        }
        return instances.get(id);
    }

    function create(id, dotNetRef, options) {
        const el = document.getElementById(id);
        if (!el) throw new Error("Element not found: " + id);
        if (instances.has(id)) {
            try { instances.get(id).editor?.destroy(); } catch { }
            instances.delete(id);
        }

        const opts = Object.assign({}, options || {});
        if (typeof Drawflow === "undefined") {
            throw new Error("Drawflow global not found. Include the library first.");
        }

        const editor = new Drawflow(el, opts);
        if (typeof opts.reroute !== "undefined") editor.reroute = opts.reroute;
        editor.start();

        const state = { id, editor, dotNetRef, eventHandlers: {} };

        const knownEvents = [
            "nodeCreated", "nodeRemoved", "nodeSelected", "nodeUnselected",
            "nodeDataChanged", "nodeMoved", "connectionCreated", "connectionRemoved",
            "connectionSelected", "connectionUnselected", "moduleCreated",
            "moduleChanged", "moduleRemoved", "import", "zoom", "translate",
            "addReroute", "removeReroute"
        ];

        knownEvents.forEach(evt => {
            const handler = (...args) => {
                try {
                    const payload = JSON.stringify(args, (_k, v) => (v instanceof HTMLElement ? undefined : v));
                    state.dotNetRef.invokeMethodAsync("OnDrawflowEvent", evt, payload);
                } catch (e) {
                    console.warn("Failed to forward Drawflow event", evt, e);
                }
            };
            state.eventHandlers[evt] = handler;
            try { editor.on(evt, handler); } catch { }
        });

        instances.set(id, state);
        return true;
    }

    function destroy(id) {
        const s = instances.get(id);
        if (!s) return false;
        try {
            Object.entries(s.eventHandlers || {}).forEach(([evt, h]) => {
                try { s.editor.off?.(evt, h); } catch { }
            });
            s.editor?.destroy?.();
        } finally {
            instances.delete(id);
        }
        return true;
    }

    function on(id, eventName) {
        const s = ensureInstance(id);
        if (!s.eventHandlers[eventName]) {
            const handler = (...args) => {
                try {
                    const payload = JSON.stringify(args, (_k, v) => (v instanceof HTMLElement ? undefined : v));
                    s.dotNetRef.invokeMethodAsync("OnDrawflowEvent", eventName, payload);
                } catch (e) {
                    console.warn("Failed to forward Drawflow event", eventName, e);
                }
            };
            s.eventHandlers[eventName] = handler;
            s.editor.on(eventName, handler);
        }
        return true;
    }

    function off(id, eventName) {
        const s = ensureInstance(id);
        const h = s.eventHandlers[eventName];
        if (h) {
            try { s.editor.off?.(eventName, h); } catch { }
            delete s.eventHandlers[eventName];
        }
        return true;
    }

    async function call(id, methodName, args) {
        const s = ensureInstance(id);
        const target = s.editor;
        if (!target) throw new Error("Editor missing for id: " + id);
        const fn = target[methodName];
        if (typeof fn !== "function") {
            throw new Error("Method not found on Drawflow: " + methodName);
        }
        const resolvedArgs = (args || []).map(a => {
            if (typeof a === "string") {
                try { return JSON.parse(a); } catch { return a; }
            }
            return a;
        });
        const result = fn.apply(target, resolvedArgs);
        if (result && typeof result.then === "function") {
            return await result;
        }
        return result ?? null;
    }

    function get(id, propName) {
        const s = ensureInstance(id);
        const v = s.editor?.[propName];
        return v ?? null;
    }

    function set(id, propName, value) {
        const s = ensureInstance(id);
        if (!s.editor) return false;
        s.editor[propName] = value;
        return true;
    }

    function labelPorts(elementId, nodeId, inLabels = [], outLabels = []) {
        const host = document.getElementById(elementId);
        if (!host) return;

        const nodeEl = host.querySelector(`.drawflow-node[data-id="${nodeId}"]`);
        if (!nodeEl) return;

        // ensure base style (only once)
        const styleId = "df-port-label-style";
        if (!document.getElementById(styleId)) {
            const s = document.createElement("style");
            s.id = styleId;
            s.textContent = `
        .drawflow .drawflow-node .df-port-wrap {
          display:flex; align-items:center; gap:.4rem; line-height:1; white-space:nowrap;
        }
        .drawflow .drawflow-node .df-port-wrap.df-input { justify-content:flex-start; }
        .drawflow .drawflow-node .df-port-wrap.df-output { justify-content:flex-end; }
        .drawflow .drawflow-node .df-port-label { font-size:12px; opacity:.85; user-select:none; }
      `;
            document.head.appendChild(s);
        }

        const wrapPortWithLabel = (portEl, labelText, cls) => {
            if (!portEl) return;
            if (portEl.parentElement && portEl.parentElement.classList.contains("df-port-wrap")) {
                const span = portEl.parentElement.querySelector("span.df-port-label");
                if (span) span.textContent = labelText ?? "";
                return;
            }
            const wrap = document.createElement("div");
            wrap.className = `df-port-wrap ${cls}`;
            const label = document.createElement("span");
            label.className = "df-port-label";
            label.textContent = labelText ?? "";

            if (cls.includes("input")) {
                wrap.appendChild(portEl);
                wrap.appendChild(label);
            } else {
                wrap.appendChild(label);
                wrap.appendChild(portEl);
            }
            const parent = portEl.parentElement;
            if (parent) parent.replaceChild(wrap, portEl);
        };

        const inputs = nodeEl.querySelectorAll(".inputs .input");
        const outputs = nodeEl.querySelectorAll(".outputs .output");

        for (let i = 0; i < inputs.length; i++) {
            const text = inLabels[i] ?? `In ${i + 1}`;
            wrapPortWithLabel(inputs[i], text, "df-input");
            inputs[i].setAttribute("title", text);
            inputs[i].dataset.label = text;
        }

        for (let i = 0; i < outputs.length; i++) {
            const text = outLabels[i] ?? `Out ${i + 1}`;
            wrapPortWithLabel(outputs[i], text, "df-output");
            outputs[i].setAttribute("title", text);
            outputs[i].dataset.label = text;
        }
    }

    return { create, destroy, on, off, call, get, set, labelPorts };
})();

window.nextFrame = () => {
    return new Promise(resolve => requestAnimationFrame(() => resolve()));
};