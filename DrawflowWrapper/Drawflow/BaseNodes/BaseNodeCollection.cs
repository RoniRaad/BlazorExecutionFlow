using System;
using System.Globalization;
using System.Threading.Tasks;
using DrawflowWrapper.Drawflow.Attributes;
using DrawflowWrapper.Models.Nodes;

namespace DrawflowWrapper.Drawflow.BaseNodes
{
    public static class BaseNodeCollection
    {
        // ---------- Arithmetic ----------
        [DrawflowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Add(int input1, int input2) => input1 + input2;

        // Fixed: was returning input1 + input2
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

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double Pow(double @base, double exponent) => Math.Pow(@base, exponent);

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
        public static double ClampD(double value, double min, double max)
        {
            if (min > max) (min, max) = (max, min);
            return Math.Min(Math.Max(value, min), max);
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double AbsD(double value) => Math.Abs(value);

        [DrawflowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double RoundD(double value, int digits = 0) => Math.Round(value, digits);

        // ---------- Comparison (ints) ----------
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

        // ---------- Boolean logic ----------
        [DrawflowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool And(bool a, bool b) => a && b;

        [DrawflowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool Or(bool a, bool b) => a || b;

        [DrawflowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool Xor(bool a, bool b) => a ^ b;

        [DrawflowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool Not(bool value) => !value;

        // ---------- Strings ----------
        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string StringConcat(string input1, string input2) => input1 + input2;

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string JoinWith(string input1, string input2, [DrawflowInputField] string separator = "")
            => string.Join(separator ?? string.Empty, input1, input2);

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string ToUpper(string input) => input?.ToUpperInvariant();

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string ToLower(string input) => input?.ToLowerInvariant();

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string Trim(string input) => input?.Trim();

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static int Length(string input) => input?.Length ?? 0;

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static bool Contains(string input, string value, [DrawflowInputField] bool ignoreCase = false)
        {
            if (input == null || value == null) return false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return input.IndexOf(value, comparison) >= 0;
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static bool StartsWith(string input, string value, [DrawflowInputField] bool ignoreCase = false)
        {
            if (input == null || value == null) return false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return input.StartsWith(value, comparison);
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static bool EndsWith(string input, string value, [DrawflowInputField] bool ignoreCase = false)
        {
            if (input == null || value == null) return false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return input.EndsWith(value, comparison);
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string Substring(string input, int startIndex, int length)
        {
            if (input == null) return null;
            if (startIndex < 0) startIndex = 0;
            if (startIndex > input.Length) return string.Empty;
            if (length < 0) length = 0;
            if (startIndex + length > input.Length) length = input.Length - startIndex;
            return input.Substring(startIndex, length);
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string Replace(string input, string oldValue, string newValue)
            => input?.Replace(oldValue ?? string.Empty, newValue ?? string.Empty);

        // ---------- Parsing / Conversion ----------
        [DrawflowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static int ParseInt(string text, [DrawflowInputField] int @default = 0)
            => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : @default;

        [DrawflowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static double ParseDouble(string text, [DrawflowInputField] double @default = 0.0)
            => double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v) ? v : @default;

        [DrawflowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static bool ParseBool(string text, [DrawflowInputField] bool @default = false)
            => bool.TryParse(text, out var v) ? v : @default;

        // ---------- Logging / Timing ----------
        [DrawflowNodeMethod(Models.NodeType.Function, "Utility")]
        public static void Log(string message) => Console.WriteLine(message);

        [DrawflowNodeMethod(Models.NodeType.Function, "Utility")]
        public static async Task Wait([DrawflowInputField] int timeMs) => await Task.Delay(timeMs);

        // ---------- Random ----------
        [DrawflowNodeMethod(Models.NodeType.Function, "Random")]
        public static int RandomInteger() => new Random().Next();

        // Inclusive min, exclusive max (like System.Random)
        [DrawflowNodeMethod(Models.NodeType.Function, "Random")]
        public static int RandomIntegerRange([DrawflowInputField] int min, [DrawflowInputField] int max)
        {
            if (min == max) return min;
            if (min > max) (min, max) = (max, min);
            return new Random().Next(min, max);
        }

        [DrawflowNodeMethod(Models.NodeType.Function, "Random")]
        public static double RandomDouble() => new Random().NextDouble();

        // ---------- Date/Time ----------
        [DrawflowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime UtcNow() => DateTime.UtcNow;

        [DrawflowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime NowLocal() => DateTime.Now;

        [DrawflowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime AddSeconds(DateTime dateTime, double seconds) => dateTime.AddSeconds(seconds);

        [DrawflowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime AddMilliseconds(DateTime dateTime, double milliseconds) => dateTime.AddMilliseconds(milliseconds);

        // ---------- Variables (constants) ----------
        [DrawflowNodeMethod(Models.NodeType.Function, "Variables")]
        public static string StringVariable([DrawflowInputField] string constantString) => constantString;

        [DrawflowNodeMethod(Models.NodeType.Function, "Variables")]
        public static int IntVariable([DrawflowInputField] int constantInt) => constantInt;

        [DrawflowNodeMethod(Models.NodeType.Function, "Variables")]
        public static double DoubleVariable([DrawflowInputField] double constantDouble) => constantDouble;

        [DrawflowNodeMethod(Models.NodeType.Function, "Variables")]
        public static bool BoolVariable([DrawflowInputField] bool constantBool) => constantBool;

        // ---------- Conditionals -----------
        [DrawflowNodeBranchingMethod(Models.NodeType.BooleanOperation, "Conditionals")]
        public static IfStatementOutput If(bool condition)
        {
            IfStatementOutput ifStatement = new()
            {
                True = condition,
                False = !condition
            };

            return ifStatement;
        }

        [DrawflowNodeMethod(Models.NodeType.Loop, "Conditionals")]
        public static async Task For(
            [DrawflowInputContextField] LoopContext ctx,
            int start,
            int end,
            [DrawflowInputField] int step = 1)
        {
            if (step == 0) step = 1;

            var cmp = step > 0
                ? new Func<int, bool>(i => i < end)
                : i => i > end;

            for (var i = start; cmp(i); i += step)
            {
                ctx.Index = i;

                // this is where the BODY subgraph runs
                await ctx.RunBodyAsync();
            }

            await ctx.RunDoneAsync();
        }
    }

    public sealed class LoopContext
    {
        public int Index { get; set; }
        public Func<Task> RunBodyAsync = () => Task.CompletedTask;
        public Func<Task> RunDoneAsync = () => Task.CompletedTask;
    }
}
