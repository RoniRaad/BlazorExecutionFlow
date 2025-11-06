using DrawflowWrapper.Drawflow.Attributes;

namespace DrawflowWrapper.Models.Nodes
{
    public class IfStatementOutput
    {
        [DrawflowOutputTriggerAction]
        public bool True { get; set; } = false;

        [DrawflowOutputTriggerAction]
        public bool False { get; set; } = false;
    }
}
