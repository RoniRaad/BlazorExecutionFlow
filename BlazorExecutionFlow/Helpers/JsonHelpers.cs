using System.Text.Json;
using System.Text.Json.Nodes;

namespace BlazorExecutionFlow.Helpers
{

    public static class JsonHelpers
    {
        /// <summary>
        /// Walks the JsonNode tree and, for any string value that contains valid JSON,
        /// replaces that string with its parsed JSON representation.
        /// 
        /// This mutates the input node.
        /// </summary>
        public static void ExpandEmbeddedJson(this JsonNode? node)
        {
            if (node is null)
                return;

            switch (node)
            {
                case JsonObject obj:
                    // Copy to a temporary list so we can safely modify while iterating
                    foreach (var kvp in obj.ToList())
                    {
                        var key = kvp.Key;
                        var child = kvp.Value;

                        if (TryConvertStringNodeToJson(child, out var converted))
                        {
                            // Replace property with parsed JSON
                            obj[key] = converted;
                            // Also recurse into the newly parsed value
                            ExpandEmbeddedJson(converted);
                        }
                        else
                        {
                            ExpandEmbeddedJson(child);
                        }
                    }
                    break;

                case JsonArray arr:
                    for (int i = 0; i < arr.Count; i++)
                    {
                        var child = arr[i];

                        if (TryConvertStringNodeToJson(child, out var converted))
                        {
                            arr[i] = converted;
                            ExpandEmbeddedJson(converted);
                        }
                        else
                        {
                            ExpandEmbeddedJson(child);
                        }
                    }
                    break;

                default:
                    // JsonValue (non-string) – nothing to do
                    break;
            }
        }

        /// <summary>
        /// If the node is a JsonValue<string> that contains valid JSON,
        /// parses it and returns the parsed node.
        /// </summary>
        private static bool TryConvertStringNodeToJson(JsonNode? node, out JsonNode? parsed)
        {
            parsed = null;

            if (node is not JsonValue value)
                return false;

            if (!value.TryGetValue<string>(out var str))
                return false;

            if (string.IsNullOrWhiteSpace(str))
                return false;

            try
            {
                // Optional: if you *only* want to parse complex JSON (objects/arrays),
                // you can gate this by checking the first non-whitespace char:
                //
                // char c = str.TrimStart()[0];
                // if (c != '{' && c != '[') return false;
                //
                parsed = JsonNode.Parse(str);
                return parsed is not null;
            }
            catch (JsonException)
            {
                // Not valid JSON
                return false;
            }
        }
    }
}
