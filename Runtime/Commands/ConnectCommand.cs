#nullable enable
using NodeGraph.Core;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 连接两个端口。
    /// </summary>
    /// <remarks>
    /// 如目标端口为 Single 容量且已有连线，顶替的旧边存入 _displacedEdge，
    /// Undo 时会同时恢复旧边。这个副作用由 <see cref="Graph.Connect"/> 返回的
    /// <see cref="ConnectResult.DisplacedEdge"/> 自动记录，不需外部预检。
    /// </remarks>
    public class ConnectCommand : IStructuralCommand
    {
        private readonly string _sourcePortId;
        private readonly string _targetPortId;

        // Execute 后由 ConnectResult 填充，供 Undo 使用
        private string? _createdEdgeId;
        private Edge? _displacedEdge;

        public string Description { get; }

        /// <summary>获取执行后创建的连线 ID</summary>
        public string? CreatedEdgeId => _createdEdgeId;

        public ConnectCommand(string sourcePortId, string targetPortId)
        {
            _sourcePortId = sourcePortId;
            _targetPortId = targetPortId;
            Description = "连接端口";
        }

        public void Execute(Graph graph)
        {
            var result = graph.Connect(_sourcePortId, _targetPortId);
            _createdEdgeId = result.CreatedEdge?.Id;
            _displacedEdge = result.DisplacedEdge;
        }

        public void Undo(Graph graph)
        {
            if (_createdEdgeId != null)
                graph.Disconnect(_createdEdgeId);

            if (_displacedEdge != null)
                graph.AddEdgeDirect(_displacedEdge);
        }
    }
}
