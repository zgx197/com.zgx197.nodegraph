#nullable enable
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Core;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 移除动态端口（含关联连线）。
    /// </summary>
    public class RemovePortCommand : IStructuralCommand
    {
        private readonly string _nodeId;
        private readonly string _portId;

        // Undo 所需快照
        private string? _portName;
        private PortDirection _portDirection;
        private PortKind _portKind;
        private string? _portDataType;
        private PortCapacity _portCapacity;
        private int _portSortOrder;
        private List<(string edgeId, string sourcePortId, string targetPortId, IEdgeData? userData)>? _removedEdges;

        public string Description { get; }

        public RemovePortCommand(string nodeId, string portId)
        {
            _nodeId = nodeId;
            _portId = portId;
            Description = "移除端口";
        }

        public void Execute(Graph graph)
        {
            var node = graph.FindNode(_nodeId);
            var port = node?.FindPort(_portId);
            if (node == null || port == null) return;

            // 保存端口快照
            _portName = port.Name;
            _portDirection = port.Direction;
            _portKind = port.Kind;
            _portDataType = port.DataType;
            _portCapacity = port.Capacity;
            _portSortOrder = port.SortOrder;

            // 保存并移除关联连线
            _removedEdges = graph.GetEdgesForPort(_portId)
                .Select(e => (e.Id, e.SourcePortId, e.TargetPortId, e.UserData))
                .ToList();

            foreach (var (edgeId, _, _, _) in _removedEdges)
                graph.Disconnect(edgeId);

            node.RemovePort(_portId);
        }

        public void Undo(Graph graph)
        {
            if (_portName == null || _portDataType == null) return;

            var node = graph.FindNode(_nodeId);
            if (node == null) return;

            // 恢复端口
            var port = new Port(_portId, _nodeId, _portName, _portDirection, _portKind, _portDataType, _portCapacity, _portSortOrder);
            node.AddPortDirect(port);

            // 恢复连线
            if (_removedEdges != null)
            {
                foreach (var (edgeId, sourcePortId, targetPortId, userData) in _removedEdges)
                {
                    var edge = new Edge(edgeId, sourcePortId, targetPortId) { UserData = userData };
                    graph.AddEdgeDirect(edge);
                }
            }
        }
    }
}
