using BlazorWorkflow.Models;

namespace BlazorWorkflow.Flow.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class BlazorFlowNodeMethodAttribute : Attribute
    {
        public readonly NodeType NodeType;
        public string Section { get; set; }

        public BlazorFlowNodeMethodAttribute(NodeType nodeType, string section)
        {
            NodeType = nodeType;
            Section = section;
        }
    }
}
