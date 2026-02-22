#nullable enable
using System.Linq;
using NodeGraph.Core;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 修改连线业务数据（<see cref="IEdgeData"/>）。
    /// </summary>
    /// <remarks>
    /// 典型用法：业务层修改连线的条件描述时封装为此命令。
    /// </remarks>
    public class ChangeEdgeDataCommand : IStructuralCommand
    {
        private readonly string _edgeId;
        private readonly IEdgeData? _newData;
        private IEdgeData? _oldData;

        public string Description { get; }

        public ChangeEdgeDataCommand(string edgeId, IEdgeData? newData, string description = "修改连线数据")
        {
            _edgeId = edgeId;
            _newData = newData;
            Description = description;
        }

        public void Execute(Graph graph)
        {
            var edge = graph.Edges.FirstOrDefault(e => e.Id == _edgeId);
            if (edge == null) return;

            _oldData = edge.UserData;
            edge.UserData = _newData;
        }

        public void Undo(Graph graph)
        {
            var edge = graph.Edges.FirstOrDefault(e => e.Id == _edgeId);
            if (edge == null) return;

            edge.UserData = _oldData;
        }
    }
}
