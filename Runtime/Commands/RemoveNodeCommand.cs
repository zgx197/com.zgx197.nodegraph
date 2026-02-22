#nullable enable
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 删除节点（含关联连线）。
    /// </summary>
    /// <remarks>
    /// Execute 前将节点及其所有关联连线完整快照（NodeSnapshot + EdgeSnapshot），
    /// Undo 时通过内部的 <see cref="Graph.AddNodeDirect"/> / <see cref="Graph.AddEdgeDirect"/> 恢复。
    /// </remarks>
    public class RemoveNodeCommand : IStructuralCommand
    {
        private readonly string _nodeId;

        // Undo 时恢复所需的快照
        private NodeSnapshot? _snapshot;
        private List<EdgeSnapshot>? _removedEdges;

        public string Description { get; }

        public RemoveNodeCommand(string nodeId)
        {
            _nodeId = nodeId;
            Description = $"删除节点 {nodeId}";
        }

        public void Execute(Graph graph)
        {
            var node = graph.FindNode(_nodeId);
            if (node == null) return;

            // 保存节点快照
            _snapshot = NodeSnapshot.Capture(node);

            // 保存关联连线
            _removedEdges = graph.GetEdgesForNode(_nodeId)
                .Select(e => new EdgeSnapshot(e.Id, e.SourcePortId, e.TargetPortId, e.UserData))
                .ToList();

            graph.RemoveNode(_nodeId);
        }

        public void Undo(Graph graph)
        {
            if (_snapshot == null) return;

            // 恢复节点
            var node = new Node(_snapshot.Id, _snapshot.TypeId, _snapshot.Position)
            {
                Size = _snapshot.Size,
                DisplayMode = _snapshot.DisplayMode,
                State = NodeState.Normal,
                UserData = _snapshot.UserData,
                AllowDynamicPorts = _snapshot.AllowDynamicPorts
            };

            // 恢复端口
            foreach (var ps in _snapshot.Ports)
            {
                var port = new Port(ps.Id, node.Id, ps.Name, ps.Direction, ps.Kind, ps.DataType, ps.Capacity, ps.SortOrder);
                node.AddPortDirect(port);
            }

            graph.AddNodeDirect(node);

            // 恢复连线
            if (_removedEdges != null)
            {
                foreach (var es in _removedEdges)
                {
                    var edge = new Edge(es.Id, es.SourcePortId, es.TargetPortId)
                    {
                        UserData = es.UserData
                    };
                    graph.AddEdgeDirect(edge);
                }
            }
        }

        // ── 快照类型 ──

        private class NodeSnapshot
        {
            public string Id = "";
            public string TypeId = "";
            public Vec2 Position;
            public Vec2 Size;
            public NodeDisplayMode DisplayMode;
            public INodeData? UserData;
            public bool AllowDynamicPorts;
            public List<PortSnapshot> Ports = new List<PortSnapshot>();

            public static NodeSnapshot Capture(Node node)
            {
                var snap = new NodeSnapshot
                {
                    Id = node.Id,
                    TypeId = node.TypeId,
                    Position = node.Position,
                    Size = node.Size,
                    DisplayMode = node.DisplayMode,
                    UserData = node.UserData,
                    AllowDynamicPorts = node.AllowDynamicPorts
                };
                foreach (var port in node.Ports)
                {
                    snap.Ports.Add(new PortSnapshot
                    {
                        Id = port.Id,
                        Name = port.Name,
                        Direction = port.Direction,
                        Kind = port.Kind,
                        DataType = port.DataType,
                        Capacity = port.Capacity,
                        SortOrder = port.SortOrder
                    });
                }
                return snap;
            }
        }

        private class PortSnapshot
        {
            public string Id = "";
            public string Name = "";
            public PortDirection Direction;
            public PortKind Kind;
            public string DataType = "";
            public PortCapacity Capacity;
            public int SortOrder;
        }

        private class EdgeSnapshot
        {
            public string Id;
            public string SourcePortId;
            public string TargetPortId;
            public IEdgeData? UserData;

            public EdgeSnapshot(string id, string sourcePortId, string targetPortId, IEdgeData? userData)
            {
                Id = id;
                SourcePortId = sourcePortId;
                TargetPortId = targetPortId;
                UserData = userData;
            }
        }
    }
}
