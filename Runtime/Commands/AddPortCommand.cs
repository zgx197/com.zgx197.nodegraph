#nullable enable
using NodeGraph.Core;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 为节点添加动态端口（需 <see cref="Node.AllowDynamicPorts"/> 为 true）。
    /// </summary>
    public class AddPortCommand : IStructuralCommand
    {
        private readonly string _nodeId;
        private readonly PortDefinition _definition;

        private string? _createdPortId;

        public string Description { get; }

        /// <summary>获取执行后创建的端口 ID</summary>
        public string? CreatedPortId => _createdPortId;

        public AddPortCommand(string nodeId, PortDefinition definition)
        {
            _nodeId = nodeId;
            _definition = definition;
            Description = $"添加端口 {definition.Name}";
        }

        public void Execute(Graph graph)
        {
            var node = graph.FindNode(_nodeId);
            if (node == null) return;

            var port = node.AddPort(_definition);
            _createdPortId = port.Id;
        }

        public void Undo(Graph graph)
        {
            if (_createdPortId == null) return;

            var node = graph.FindNode(_nodeId);
            node?.RemovePort(_createdPortId);
        }
    }
}
