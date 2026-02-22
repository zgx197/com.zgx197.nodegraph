#nullable enable
using NodeGraph.Core;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 修改节点业务数据（<see cref="INodeData"/>）。
    /// </summary>
    /// <remarks>
    /// 典型用法：业务层在属性面板修改节点参数时封装为此命令。
    /// 快照旧数据用于 Undo，新数据直接覆写节点的 UserData 字段。
    /// </remarks>
    public class ChangeNodeDataCommand : IStructuralCommand
    {
        private readonly string _nodeId;
        private readonly INodeData? _newData;
        private INodeData? _oldData;

        public string Description { get; }

        public ChangeNodeDataCommand(string nodeId, INodeData? newData, string description = "修改节点数据")
        {
            _nodeId = nodeId;
            _newData = newData;
            Description = description;
        }

        public void Execute(Graph graph)
        {
            var node = graph.FindNode(_nodeId);
            if (node == null) return;

            _oldData = node.UserData;
            node.UserData = _newData;
        }

        public void Undo(Graph graph)
        {
            var node = graph.FindNode(_nodeId);
            if (node == null) return;

            node.UserData = _oldData;
        }
    }
}
