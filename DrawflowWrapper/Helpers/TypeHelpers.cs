using System.Reflection;

namespace DrawflowWrapper.Helpers
{
    public static class TypeHelpers
    {
        public static List<(string? Type, string? Name)>? GetReturnProperties(MethodInfo methodInfo)
        {
            var methodReturnType = methodInfo?.ReturnType;
            if (methodReturnType == typeof(void) || methodReturnType == typeof(Task))
            {
                return null;
            }

            if (methodReturnType?.BaseType == typeof(Task) && methodReturnType.GenericTypeArguments.Length > 0)
            {
                methodReturnType = methodReturnType.GenericTypeArguments.First();
            }

            if (methodReturnType == typeof(string) || methodReturnType.IsPrimitive)
            {
                return [new() { Name = "result", Type = methodReturnType.Name }];
            }

            var returnProperties = methodReturnType?.GetProperties(BindingFlags.Instance | BindingFlags.Public) ?? [];
            return [.. returnProperties.Select<PropertyInfo, (string? Type, string? Name)>(x => new() { Name = x.Name, Type = methodReturnType?.Name })];
        }
    }
}
