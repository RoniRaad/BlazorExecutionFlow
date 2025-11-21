using System;

namespace BlazorExecutionFlow.Flow.Attributes
{
    /// <summary>
    /// Marks a Dictionary<string, string> parameter to use the dictionary mapping UI
    /// in the node editor, allowing users to map individual key-value pairs from the payload.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class BlazorFlowDictionaryMappingAttribute : Attribute
    {
    }
}
