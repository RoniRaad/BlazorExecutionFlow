using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using DrawflowWrapper.Drawflow.Attributes;
using DrawflowWrapper.Helpers;
using DrawflowWrapper.Models.NodeV2;

namespace DrawflowWrapper.Drawflow.BaseNodes
{
    public static class BaseNodeCollection
    {
        // ---------- Events / Triggers ----------

        [DrawflowNodeMethod(Models.NodeType.Event, "Events")]
        public static void Start() { }

        [DrawflowNodeMethod(Models.NodeType.Event, "Events")]
        public static DateTime OnStartUtc() => DateTime.UtcNow;


        // ---------- Arithmetic (int) ----------

        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Add(int input1, int input2) => input1 + input2;

        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Subtract(int input1, int input2) => input1 - input2;

        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Multiply(int input1, int input2) => input1 * input2;

        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Divide(int numerator, int denominator)
        {
            if (denominator == 0) throw new DivideByZeroException("Denominator cannot be zero.");
            return numerator / denominator;
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Modulo(int input1, int input2)
        {
            if (input2 == 0) throw new DivideByZeroException("Modulo by zero is not allowed.");
            return input1 % input2;
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Min(int a, int b) => Math.Min(a, b);

        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Max(int a, int b) => Math.Max(a, b);

        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Clamp(int value, int min, int max)
        {
            if (min > max) (min, max) = (max, min);
            return Math.Min(Math.Max(value, min), max);
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Abs(int value) => Math.Abs(value);

        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Negate(int value) => -value;

        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Increment(int value) => value + 1;

        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Decrement(int value) => value - 1;

        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Sign(int value) => Math.Sign(value);

        // Map value from one range to another
        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static double MapRange(
            double value,
            double inMin,
            double inMax,
            double outMin,
            double outMax)
        {
            if (Math.Abs(inMax - inMin) < double.Epsilon)
                return outMin;
            var t = (value - inMin) / (inMax - inMin);
            return outMin + t * (outMax - outMin);
        }

        // ---------- Floating-point helpers ----------

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double AddD(double input1, double input2) => input1 + input2;

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double SubtractD(double input1, double input2) => input1 - input2;

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double MultiplyD(double input1, double input2) => input1 * input2;

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double DivideD(double numerator, double denominator)
        {
            if (Math.Abs(denominator) < double.Epsilon)
                throw new DivideByZeroException("Denominator cannot be zero.");
            return numerator / denominator;
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double Pow(double @base, double exponent) => Math.Pow(@base, exponent);

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double Sqrt(double value) => Math.Sqrt(value);

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double Sin(double value) => Math.Sin(value);

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double Cos(double value) => Math.Cos(value);

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double Tan(double value) => Math.Tan(value);

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double ClampD(double value, double min, double max)
        {
            if (min > max) (min, max) = (max, min);
            return Math.Min(Math.Max(value, min), max);
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double AbsD(double value) => Math.Abs(value);

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double RoundD(double value, int digits = 0) => Math.Round(value, digits);

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double FloorD(double value) => Math.Floor(value);

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double CeilingD(double value) => Math.Ceiling(value);

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double Lerp(double a, double b, double t)
            => a + (b - a) * ClampD(t, 0.0, 1.0);

        // ---------- Comparison (ints / doubles / strings) ----------

        [DrawflowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool Equal(int a, int b) => a == b;

        [DrawflowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool NotEqual(int a, int b) => a != b;

        [DrawflowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool GreaterThan(int a, int b) => a > b;

        [DrawflowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool GreaterOrEqual(int a, int b) => a >= b;

        [DrawflowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool LessThan(int a, int b) => a < b;

        [DrawflowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool LessOrEqual(int a, int b) => a <= b;

        [DrawflowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool EqualD(double a, double b, double tolerance = 0.0)
            => Math.Abs(a - b) <= Math.Max(tolerance, 0.0);

        [DrawflowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool StringEquals(string a, string b, bool ignoreCase = false)
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(a ?? string.Empty, b ?? string.Empty, comparison);
        }

        // ---------- Boolean logic ----------

        [DrawflowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool And(bool a, bool b) => a && b;

        [DrawflowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool Or(bool a, bool b) => a || b;

        [DrawflowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool Xor(bool a, bool b) => a ^ b;

        [DrawflowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool Not(bool value) => !value;

        [DrawflowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool CoalesceBool(bool? value, bool @default = false)
            => value ?? @default;

        // ---------- Strings ----------

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string StringConcat(string input1, string input2)
            => (input1 ?? "") + (input2 ?? "");

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string JoinWith(string input1, string input2, string separator = "")
            => string.Join(separator ?? string.Empty, input1 ?? string.Empty, input2 ?? string.Empty);

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string JoinArray(string[] items, string separator = ",")
        {
            items ??= Array.Empty<string>();
            return string.Join(separator ?? string.Empty, items);
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string ToUpper(string input) => input?.ToUpperInvariant() ?? string.Empty;

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string ToLower(string input) => input?.ToLowerInvariant() ?? string.Empty;

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string Trim(string input) => input?.Trim() ?? string.Empty;

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static int Length(string input) => input?.Length ?? 0;

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static bool Contains(string input, string value, bool ignoreCase = false)
        {
            if (input == null || value == null) return false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return input.IndexOf(value, comparison) >= 0;
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static bool StartsWith(string input, string value, bool ignoreCase = false)
        {
            if (input == null || value == null) return false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return input.StartsWith(value, comparison);
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static bool EndsWith(string input, string value, bool ignoreCase = false)
        {
            if (input == null || value == null) return false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return input.EndsWith(value, comparison);
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string Substring(string input, int startIndex, int length)
        {
            if (input == null) return string.Empty;
            if (startIndex < 0) startIndex = 0;
            if (startIndex > input.Length) return string.Empty;
            if (length < 0) length = 0;
            if (startIndex + length > input.Length) length = input.Length - startIndex;
            return input.Substring(startIndex, length);
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string Replace(string input, string oldValue, string newValue)
            => (input ?? string.Empty).Replace(oldValue ?? string.Empty, newValue ?? string.Empty);

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string[] Split(string input, string separator)
        {
            if (input == null) return Array.Empty<string>();
            separator ??= ",";
            return input.Split(separator, StringSplitOptions.None);
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string CoalesceString(string primary, string fallback = "")
            => string.IsNullOrEmpty(primary) ? fallback ?? string.Empty : primary;

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static int IndexOf(string input, string value, bool ignoreCase = false)
        {
            if (input == null || value == null) return -1;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return input.IndexOf(value, comparison);
        }

        // Simple format: replaces {0}, {1}, {2}
        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string Format3(
            string format,
            string arg0,
            string arg1,
            string arg2)
        {
            format ??= string.Empty;
            return string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2);
        }

        // ---------- Parsing / Conversion ----------

        [DrawflowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static int ParseInt(string text, int @default = 0)
            => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : @default;

        [DrawflowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static double ParseDouble(string text, double @default = 0.0)
            => double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v) ? v : @default;

        [DrawflowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static bool ParseBool(string text, bool @default = false)
            => bool.TryParse(text, out var v) ? v : @default;

        [DrawflowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static DateTime ParseDateTime(
            string text,
            string format,
            bool assumeUtc = true)
        {
            if (string.IsNullOrWhiteSpace(text))
                return DateTime.MinValue;

            if (!string.IsNullOrWhiteSpace(format) &&
                DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dtExact))
            {
                return assumeUtc ? DateTime.SpecifyKind(dtExact, DateTimeKind.Utc) : dtExact;
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return assumeUtc ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt;

            return DateTime.MinValue;
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static string ToStringInvariant(double value) => value.ToString(CultureInfo.InvariantCulture);

        [DrawflowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static string IntToString(int value) => value.ToString(CultureInfo.InvariantCulture);

        [DrawflowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static double IntToDouble(int value) => value;

        [DrawflowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static int DoubleToInt(double value) => (int)value;

        [DrawflowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static int BoolToInt(bool value) => value ? 1 : 0;

        [DrawflowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static bool IntToBool(int value) => value != 0;

        // ---------- Logging / Debug / Utility ----------

        [DrawflowNodeMethod(Models.NodeType.Function, "Utility")]
        public static void Log(string message) => Console.WriteLine(message);

        [DrawflowNodeMethod(Models.NodeType.Function, "Utility")]
        public static void LogWarning(string message) => Console.WriteLine("[WARN] " + message);

        [DrawflowNodeMethod(Models.NodeType.Function, "Utility")]
        public static void LogError(string message) => Console.Error.WriteLine("[ERROR] " + message);

        // Logs the current node's JSON input
        [DrawflowNodeMethod(Models.NodeType.Function, "Utility")]
        public static void DumpInput(NodeContext ctx)
        {
            var json = ctx.CurrentNode.Input?.ToJsonString() ?? "{}";
            Console.WriteLine($"[DumpInput:{ctx.CurrentNode.BackingMethod.Name}] {json}");
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Utility")]
        public static async Task Wait(int timeMs)
            => await Task.Delay(Math.Max(0, timeMs));

        [DrawflowNodeMethod(Models.NodeType.Function, "Utility")]
        public static string NewGuid() => Guid.NewGuid().ToString("D");

        [DrawflowNodeMethod(Models.NodeType.Function, "Utility")]
        public static string MachineName() => Environment.MachineName;

        // ---------- Random ----------

        private static readonly Random _random = new();

        [DrawflowNodeMethod(Models.NodeType.Function, "Random")]
        public static int RandomInteger() => _random.Next();

        [DrawflowNodeMethod(Models.NodeType.Function, "Random")]
        public static int RandomIntegerRange(int min, int max)
        {
            if (min == max) return min;
            if (min > max) (min, max) = (max, min);
            return _random.Next(min, max);
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Random")]
        public static double RandomDouble() => _random.NextDouble();

        [DrawflowNodeMethod(Models.NodeType.Function, "Random")]
        public static bool RandomBool() => _random.Next(0, 2) == 1;

        // ---------- Date/Time ----------

        [DrawflowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime UtcNow() => DateTime.UtcNow;

        [DrawflowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime NowLocal() => DateTime.Now;

        [DrawflowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime AddSeconds(DateTime dateTime, double seconds) => dateTime.AddSeconds(seconds);

        [DrawflowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime AddMilliseconds(DateTime dateTime, double milliseconds) => dateTime.AddMilliseconds(milliseconds);

        [DrawflowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime AddDays(DateTime dateTime, double days) => dateTime.AddDays(days);

        [DrawflowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static string FormatIso8601(DateTime dateTime)
            => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

        [DrawflowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static long ToUnixSeconds(DateTime dateTime)
            => new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();

        [DrawflowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime FromUnixSeconds(long seconds)
            => DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;

        // ---------- Variables (constants) ----------

        [DrawflowNodeMethod(Models.NodeType.Function, "Variables")]
        public static string StringVariable(string constantString) => constantString;

        [DrawflowNodeMethod(Models.NodeType.Function, "Variables")]
        public static int IntVariable(int constantInt) => constantInt;

        [DrawflowNodeMethod(Models.NodeType.Function, "Variables")]
        public static double DoubleVariable(double constantDouble) => constantDouble;

        [DrawflowNodeMethod(Models.NodeType.Function, "Variables")]
        public static bool BoolVariable(bool constantBool) => constantBool;

        // ---------- Collections (string arrays) ----------

        [DrawflowNodeMethod(Models.NodeType.Function, "Collections")]
        public static int ArrayLength(string[] items) => items?.Length ?? 0;

        [DrawflowNodeMethod(Models.NodeType.Function, "Collections")]
        public static string ArrayElementOrDefault(string[] items, int index, string @default = "")
        {
            if (items == null || items.Length == 0) return @default ?? string.Empty;
            if (index < 0 || index >= items.Length) return @default ?? string.Empty;
            return items[index] ?? string.Empty;
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Collections")]
        public static string[] ArrayAppend(string[] items, string value)
        {
            items ??= Array.Empty<string>();
            value ??= string.Empty;
            return items.Concat(new[] { value }).ToArray();
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Collections")]
        public static bool ArrayContains(string[] items, string value, bool ignoreCase = false)
        {
            if (items == null) return false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return items.Any(x => string.Equals(x ?? string.Empty, value ?? string.Empty, comparison));
        }

        // ---------- JSON helpers (for payload work) ----------

        [DrawflowNodeMethod(Models.NodeType.Function, "JSON")]
        public static string JsonNormalize(string json)
        {
            var node = ParseLoose(json);
            return node?.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ?? "{}";
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "JSON")]
        public static string JsonPretty(string json)
        {
            var node = ParseLoose(json);
            return node?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "{}";
        }

        // Merge objects deeply; arrays concatenate; non-objects fallback to right if not null else left.
        [DrawflowNodeMethod(Models.NodeType.Function, "JSON")]
        public static string JsonMergeS(string leftJson, string rightJson)
        {
            var left = ParseLoose(leftJson);
            var right = ParseLoose(rightJson);

            if (left is JsonObject lo && right is JsonObject ro)
            {
                var result = new JsonObject();
                result.Merge(lo);
                result.Merge(ro);
                return result.ToJsonString();
            }

            if (left is JsonArray la && right is JsonArray ra)
            {
                var merged = new JsonArray();
                foreach (var i in la) merged.Add(i?.DeepClone());
                foreach (var i in ra) merged.Add(i?.DeepClone());
                return merged.ToJsonString();
            }

            // Prefer right if provided, else left
            return (right ?? left ?? new JsonObject()).ToJsonString();
        }

        // Returns the JSON fragment at path, serialized as JSON string (e.g. "42", "\"text\"", "{\"a\":1}")
        [DrawflowNodeMethod(Models.NodeType.Function, "JSON")]
        public static string JsonGetS(string json, string path)
        {
            var root = ParseLoose(json);
            if (root is null) return "null";

            if (string.IsNullOrWhiteSpace(path))
                return root.ToJsonString();

            if (root is JsonObject obj)
            {
                var val = obj.GetByPath(path);
                return (val ?? JsonValue.Create((string?)null))!.ToJsonString();
            }

            // If root is array and path is empty, already returned above; otherwise unsupported
            return "null";
        }

        // Sets value at path. valueJson is parsed as JSON (use quotes if you want a raw string).
        // When root is non-object and path is empty, replaces whole root.
        [DrawflowNodeMethod(Models.NodeType.Function, "JSON")]
        public static string JsonSetS(string json, string path, string valueJson)
        {
            var root = ParseLoose(json) ?? new JsonObject();
            var valueNode = ParseLoose(valueJson) ?? JsonValue.Create((string?)null);

            // Whole-root replace if no path
            if (string.IsNullOrWhiteSpace(path))
            {
                return valueNode!.ToJsonString();
            }

            if (root is JsonObject obj)
            {
                var clone = new JsonObject();
                clone.Merge(obj);
                clone.SetByPath(path, valueNode);
                return clone.ToJsonString();
            }

            // If root is an array, support empty path only; otherwise, wrap and set "value.<path>"
            // This keeps behavior predictable without guessing array addressing syntax.
            var wrapper = new JsonObject { ["value"] = root };
            wrapper.SetByPath($"value.{path}", valueNode);
            return wrapper["value"]!.ToJsonString();
        }

        // Convenience: always force a string at path (quotes handled for you)
        [DrawflowNodeMethod(Models.NodeType.Function, "JSON")]
        public static string JsonSetStringS(string json, string path, string value)
        {
            var root = ParseLoose(json) ?? new JsonObject();
            var val = JsonValue.Create(value ?? string.Empty);

            if (string.IsNullOrWhiteSpace(path))
                return val!.ToJsonString();

            if (root is JsonObject obj)
            {
                var clone = new JsonObject();
                clone.Merge(obj);
                clone.SetByPath(path, val);
                return clone.ToJsonString();
            }

            var wrapper = new JsonObject { ["value"] = root };
            wrapper.SetByPath($"value.{path}", val);
            return wrapper["value"]!.ToJsonString();
        }

        // Remove a property/index at path. Empty path -> return {} or [] depending on original type.
        [DrawflowNodeMethod(Models.NodeType.Function, "JSON")]
        public static string JsonRemoveS(string json, string path)
        {
            var root = ParseLoose(json);
            if (root is null) return "{}";

            if (string.IsNullOrWhiteSpace(path))
            {
                // Clear to same kind
                return (root is JsonArray) ? "[]" : "{}";
            }

            if (root is JsonObject obj)
            {
                var clone = new JsonObject();
                clone.Merge(obj);
                RemoveByPath(clone, path);
                return clone.ToJsonString();
            }

            // For array roots, only empty path supported above
            return root.ToJsonString();
        }

        // Append an item (itemJson) to an array at path. If path empty and root is array, append to root.
        [DrawflowNodeMethod(Models.NodeType.Function, "JSON")]
        public static string JsonArrayPushS(string json, string path, string itemJson)
        {
            var root = ParseLoose(json) ?? new JsonArray();
            var item = ParseLoose(itemJson);

            if (string.IsNullOrWhiteSpace(path))
            {
                if (root is not JsonArray ra) ra = new JsonArray(root);
                ra.Add(item);
                return ra.ToJsonString();
            }

            if (root is JsonObject obj)
            {
                var clone = new JsonObject();
                clone.Merge(obj);

                var target = clone.GetByPath(path);
                if (target is not JsonArray arr)
                {
                    arr = new JsonArray();
                    clone.SetByPath(path, arr);
                }

                arr.Add(item);
                return clone.ToJsonString();
            }

            // For array roots with non-empty path, wrap to address path under "value"
            var wrapper = new JsonObject { ["value"] = root };
            var target2 = wrapper.GetByPath($"value.{path}");
            if (target2 is not JsonArray arr2)
            {
                arr2 = new JsonArray();
                wrapper.SetByPath($"value.{path}", arr2);
            }
            arr2.Add(item);
            return wrapper["value"]!.ToJsonString();
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "JSON")]
        public static string JsonArrayElementS(string json, string path, int index)
        {
            var root = ParseLoose(json);
            if (root is null) return "null";

            JsonNode? target = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                target = root;
            }
            else if (root is JsonObject obj)
            {
                target = obj.GetByPath(path);
            }
            else
            {
                var wrapper = new JsonObject { ["value"] = root };
                target = wrapper.GetByPath($"value.{path}");
            }

            if (target is JsonArray arr && index >= 0 && index < arr.Count)
                return (arr[index] ?? JsonValue.Create((string?)null))!.ToJsonString();

            return "null";
        }

        // Check if a token exists at path (root array supports empty path only).
        [DrawflowNodeMethod(Models.NodeType.Function, "JSON")]
        public static bool JsonPathExistsS(string json, string path)
        {
            var root = ParseLoose(json);
            if (root is null) return false;

            if (string.IsNullOrWhiteSpace(path))
                return true;

            if (root is JsonObject obj)
                return obj.GetByPath(path) != null;

            var wrapper = new JsonObject { ["value"] = root };
            return wrapper.GetByPath($"value.{path}") != null;
        }

        // Type of node at path: "object", "array", "string", "number", "bool", "null", "missing"
        [DrawflowNodeMethod(Models.NodeType.Function, "JSON")]
        public static string JsonTypeOfS(string json, string path)
        {
            var root = ParseLoose(json);
            if (root is null) return "missing";

            JsonNode? target = null;

            if (string.IsNullOrWhiteSpace(path))
                target = root;
            else if (root is JsonObject obj)
                target = obj.GetByPath(path);
            else
            {
                var wrapper = new JsonObject { ["value"] = root };
                target = wrapper.GetByPath($"value.{path}");
            }

            return target switch
            {
                null => "missing",
                JsonObject => "object",
                JsonArray => "array",
                JsonValue v when v.TryGetValue(out string? _) => "string",
                JsonValue v when v.TryGetValue(out double _) => "number",
                JsonValue v when v.TryGetValue(out bool _) => "bool",
                JsonValue => "unknown",
                _ => "unknown"
            };
        }

        // ---------- helpers ----------

        private static JsonNode? ParseLoose(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try { return JsonNode.Parse(json); }
            catch
            {
                // If it's not valid JSON, treat it as a JSON string literal
                return JsonValue.Create(json);
            }
        }

        // Minimal remove-by-path using your existing helpers for object roots.
        private static void RemoveByPath(JsonObject obj, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            // Split last segment to remove property on its parent
            var lastDot = path.LastIndexOf('.');
            if (lastDot < 0)
            {
                obj.Remove(path);
                return;
            }

            var parentPath = path[..lastDot];
            var leaf = path[(lastDot + 1)..];

            var parentNode = obj.GetByPath(parentPath);
            if (parentNode is JsonObject parentObj)
            {
                parentObj.Remove(leaf);
            }
            else if (parentNode is JsonArray parentArr && int.TryParse(leaf, out var idx))
            {
                if (idx >= 0 && idx < parentArr.Count)
                    parentArr.RemoveAt(idx);
            }
        }

        // ---------- Control Flow with ports ----------

        // Basic If node: routes to "true" or "false"
        [DrawflowNodeMethod(Models.NodeType.BooleanOperation, "Conditionals")]
        [NodeFlowPorts("true", "false")]
        public static Task If(NodeContext ctx, bool condition)
        {
            var port = condition ? "true" : "false";
            return ctx.ExecutePortAsync(port);
        }

        // IfNullable: true / false / error (null)
        [DrawflowNodeMethod(Models.NodeType.BooleanOperation, "Conditionals")]
        [NodeFlowPorts("true", "false", "error")]
        public static async Task IfNullable(NodeContext ctx, bool? condition)
        {
            if (condition is null)
            {
                await ctx.ExecutePortAsync("error");
            }
            else
            {
                await ctx.ExecutePortAsync(condition.Value ? "true" : "false");
            }
        }

        // Gate: only forwards when "open" is true
        [DrawflowNodeMethod(Models.NodeType.BooleanOperation, "Conditionals")]
        [NodeFlowPorts("open", "closed")]
        public static async Task Gate(NodeContext ctx, bool open)
        {
            if (open)
            {
                await ctx.ExecutePortAsync("open");
            }
            else
            {
                await ctx.ExecutePortAsync("closed");
            }
        }

        // SwitchInt: 3 cases + default
        [DrawflowNodeMethod(Models.NodeType.BooleanOperation, "Conditionals")]
        [NodeFlowPorts("case1", "case2", "case3", "default")]
        public static async Task SwitchInt(
            NodeContext ctx,
            int value,
            int case1,
            int case2,
            int case3)
        {
            if (value == case1)
            {
                await ctx.ExecutePortAsync("case1");
            }
            else if (value == case2)
            {
                await ctx.ExecutePortAsync("case2");
            }
            else if (value == case3)
            {
                await ctx.ExecutePortAsync("case3");
            }
            else
            {
                await ctx.ExecutePortAsync("default");
            }
        }

        // SwitchString: 3 cases + default
        [DrawflowNodeMethod(Models.NodeType.BooleanOperation, "Conditionals")]
        [NodeFlowPorts("case1", "case2", "case3", "default")]
        public static async Task SwitchString(
            NodeContext ctx,
            string value,
            string case1,
            string case2,
            string case3,
            bool ignoreCase = false)
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            value ??= string.Empty;
            case1 ??= string.Empty;
            case2 ??= string.Empty;
            case3 ??= string.Empty;

            if (string.Equals(value, case1, comparison))
            {
                await ctx.ExecutePortAsync("case1");
            }
            else if (string.Equals(value, case2, comparison))
            {
                await ctx.ExecutePortAsync("case2");
            }
            else if (string.Equals(value, case3, comparison))
            {
                await ctx.ExecutePortAsync("case3");
            }
            else
            {
                await ctx.ExecutePortAsync("default");
            }
        }

        // For loop: from start (inclusive) to end (exclusive)
        [DrawflowNodeMethod(Models.NodeType.Loop, "Loops")]
        [NodeFlowPorts("loop", "done")]
        public static async Task For(NodeContext ctx, int start, int end)
        {
            if (start > end)
            {
                await ctx.ExecutePortAsync("done");
                return;
            }

            for (int i = start; i < end; i++)
            {
                // If you want index data to downstream nodes, you can later
                // also write to JSON payload via separate nodes.
                ctx.Context["index"] = i;
                await ctx.ExecutePortAsync("loop");
            }

            await ctx.ExecutePortAsync("done");
        }

        // Repeat N times
        [DrawflowNodeMethod(Models.NodeType.Loop, "Loops")]
        [NodeFlowPorts("loop", "done")]
        public static async Task Repeat(NodeContext ctx, int count)
        {
            if (count < 0) count = 0;

            for (int i = 0; i < count; i++)
            {
                ctx.Context["index"] = i;
                await ctx.ExecutePortAsync("loop");
            }

            await ctx.ExecutePortAsync("done");
        }

        // While: calls "loop" while condition is true, then "done"
        [DrawflowNodeMethod(Models.NodeType.Loop, "Loops")]
        [NodeFlowPorts("loop", "done")]
        public static async Task While(NodeContext ctx, bool condition)
        {
            // This just branches once; to build a real while, connect "loop"
            // back into a path that recomputes the condition and re-enters this node.
            if (condition)
            {
                await ctx.ExecutePortAsync("loop");
            }
            else
            {
                await ctx.ExecutePortAsync("done");
            }
        }

        // ---------- Simple templating (for quick tests) ----------

        [DrawflowNodeMethod(Models.NodeType.Function, "Templates")]
        public static string InterpolateString(
            string template,
            string value)
        {
            template ??= string.Empty;
            value ??= string.Empty;
            return template.Replace("{{value}}", value, StringComparison.Ordinal);
        }

        // ---------- HTTP ----------
        private static readonly HttpClient _httpClient = new();

        [DrawflowNodeMethod(Models.NodeType.Function, "HTTP")]
        public static async Task<string> HttpGetString(
            string url,
            int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                var response = await _httpClient.GetAsync(url, cts.Token);
                return await response.Content.ReadAsStringAsync(cts.Token);
            }
            catch (Exception ex)
            {
                LogError($"HttpGetString failed: {ex.Message}");
                return string.Empty;
            }
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "HTTP")]
        public static async Task<int> HttpGetStatusCode(
            string url,
            int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return 0;

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                var response = await _httpClient.GetAsync(url, cts.Token);
                return (int)response.StatusCode;
            }
            catch (Exception ex)
            {
                LogError($"HttpGetStatusCode failed: {ex.Message}");
                return 0;
            }
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "HTTP")]
        public static async Task<string> HttpPostString(
            string url,
            string body,
            string contentType = "application/json",
            int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            body ??= string.Empty;
            contentType ??= "application/json";

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                using var content = new StringContent(body, Encoding.UTF8, contentType);
                var response = await _httpClient.PostAsync(url, content, cts.Token);
                return await response.Content.ReadAsStringAsync(cts.Token);
            }
            catch (Exception ex)
            {
                LogError($"HttpPostString failed: {ex.Message}");
                return string.Empty;
            }
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "HTTP")]
        public static async Task<int> HttpPostStatusCode(
            string url,
            string body,
            string contentType = "application/json",
            int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return 0;

            body ??= string.Empty;
            contentType ??= "application/json";

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                using var content = new StringContent(body, Encoding.UTF8, contentType);
                var response = await _httpClient.PostAsync(url, content, cts.Token);
                return (int)response.StatusCode;
            }
            catch (Exception ex)
            {
                LogError($"HttpPostStatusCode failed: {ex.Message}");
                return 0;
            }
        }

        // Flow-style GET: routes to "success" or "error" and also returns the body.
        [DrawflowNodeMethod(Models.NodeType.Function, "HTTP")]
        [NodeFlowPorts("success", "error")]
        public static async Task<string> HttpGetFlow(
            NodeContext ctx,
            string url,
            int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                await ctx.ExecutePortAsync("error");
                return string.Empty;
            }

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                var response = await _httpClient.GetAsync(url, cts.Token);
                var body = await response.Content.ReadAsStringAsync(cts.Token);

                // You can still branch based on success
                if (response.IsSuccessStatusCode)
                {
                    await ctx.ExecutePortAsync("success");
                }
                else
                {
                    await ctx.ExecutePortAsync("error");
                }

                // Returned body will end up as this node's output (output.result)
                return body;
            }
            catch (Exception ex)
            {
                LogError($"HttpGetFlow failed: {ex.Message}");
                await ctx.ExecutePortAsync("error");
                return string.Empty;
            }
        }

        // Flow-style POST: routes to "success" or "error" and returns the body.
        [DrawflowNodeMethod(Models.NodeType.Function, "HTTP")]
        [NodeFlowPorts("success", "error")]
        public static async Task<string> HttpPostFlow(
            NodeContext ctx,
            string url,
            string body,
            string contentType = "application/json",
            int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                await ctx.ExecutePortAsync("error");
                return string.Empty;
            }

            body ??= string.Empty;
            contentType ??= "application/json";

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                using var content = new StringContent(body, Encoding.UTF8, contentType);
                var response = await _httpClient.PostAsync(url, content, cts.Token);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    await ctx.ExecutePortAsync("success");
                }
                else
                {
                    await ctx.ExecutePortAsync("error");
                }

                return responseBody;
            }
            catch (Exception ex)
            {
                LogError($"HttpPostFlow failed: {ex.Message}");
                await ctx.ExecutePortAsync("error");
                return string.Empty;
            }
        }

    }
}
