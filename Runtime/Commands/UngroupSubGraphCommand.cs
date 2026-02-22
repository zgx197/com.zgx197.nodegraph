#nullable enable
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 解散子图框。
    /// </summary>
    /// <remarks>
    /// 将 SubGraphFrame 移除，内部节点回到顶层，删除 RepresentativeNode，
    /// 并将穿过边界的连线直接连通内外部节点。支持完整 Undo/Redo。
    /// </remarks>
    public class UngroupSubGraphCommand : IStructuralCommand
    {
        private readonly string _frameId;

        // ── Undo 快照 ──
        private FrameSnapshot? _frameSnapshot;
        private NodeSnapshot? _repNodeSnapshot;
        private List<EdgeSnapshot>? _removedEdges;
        private List<EdgeSnapshot>? _createdEdges;

        public string Description { get; }

        public UngroupSubGraphCommand(string frameId)
        {
            _frameId = frameId;
            Description = $"解散子蓝图 {frameId}";
        }

        public void Execute(Graph graph)
        {
            var frame = graph.FindSubGraphFrame(_frameId);
            if (frame == null) return;

            var repNode = graph.FindNode(frame.RepresentativeNodeId);

            // 1. 保存 Frame 快照
            _frameSnapshot = new FrameSnapshot
            {
                Id = frame.Id,
                Title = frame.Title,
                RepresentativeNodeId = frame.RepresentativeNodeId,
                Bounds = frame.Bounds,
                IsCollapsed = frame.IsCollapsed,
                SourceAssetId = frame.SourceAssetId,
                ContainedNodeIds = new List<string>(frame.ContainedNodeIds)
            };

            // 2. 保存 Rep 节点快照
            if (repNode != null)
            {
                _repNodeSnapshot = NodeSnapshot.Capture(repNode);
                _removedEdges = graph.GetEdgesForNode(repNode.Id)
                    .Select(e => new EdgeSnapshot(e.Id, e.SourcePortId, e.TargetPortId, e.UserData))
                    .ToList();
            }

            // 3. 计算穿过边界的重连关系
            _createdEdges = new List<EdgeSnapshot>();
            if (repNode != null)
            {
                ReconnectThroughBoundary(graph, repNode);
            }

            // 4. 删除 Rep 节点（连同其所有边）
            if (repNode != null)
            {
                graph.RemoveNode(repNode.Id);
            }

            // 5. 移除 Frame（内部节点保留在图中，成为顶层节点）
            graph.RemoveSubGraphFrame(_frameId);
        }

        public void Undo(Graph graph)
        {
            // 1. 删除 Execute 时创建的直连边
            if (_createdEdges != null)
            {
                foreach (var es in _createdEdges)
                {
                    var edge = graph.FindEdge(es.Id);
                    if (edge != null) graph.RemoveEdge(es.Id);
                }
            }

            // 2. 恢复 Rep 节点
            if (_repNodeSnapshot != null)
            {
                var repNode = new Node(_repNodeSnapshot.Id, _repNodeSnapshot.TypeId, _repNodeSnapshot.Position)
                {
                    Size = _repNodeSnapshot.Size,
                    DisplayMode = _repNodeSnapshot.DisplayMode,
                    UserData = _repNodeSnapshot.UserData,
                    AllowDynamicPorts = _repNodeSnapshot.AllowDynamicPorts
                };
                foreach (var ps in _repNodeSnapshot.Ports)
                {
                    var port = new Port(ps.Id, repNode.Id, ps.Name, ps.Direction, ps.Kind,
                        ps.DataType, ps.Capacity, ps.SortOrder);
                    repNode.AddPortDirect(port);
                }
                graph.AddNodeDirect(repNode);
            }

            // 3. 恢复 Rep 节点的所有原始边
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

            // 4. 恢复 SubGraphFrame
            if (_frameSnapshot != null)
            {
                var frame = new SubGraphFrame(_frameSnapshot.Id, _frameSnapshot.Title,
                    _frameSnapshot.RepresentativeNodeId)
                {
                    Bounds = _frameSnapshot.Bounds,
                    IsCollapsed = _frameSnapshot.IsCollapsed,
                    SourceAssetId = _frameSnapshot.SourceAssetId
                };
                foreach (var nid in _frameSnapshot.ContainedNodeIds)
                    frame.ContainedNodeIds.Add(nid);
                graph.AddSubGraphFrameDirect(frame);
            }
        }

        /// <summary>
        /// 计算并创建绕过 Rep 节点的直连边。
        /// 对于每对 (外部→Rep.port, Rep.port→内部) 的边，创建 外部→内部 的直连边。
        /// </summary>
        private void ReconnectThroughBoundary(Graph graph, Node repNode)
        {
            var repPortIds = new HashSet<string>(repNode.Ports.Select(p => p.Id));

            // 收集 Rep 节点每个端口的入边来源和出边目标
            var incomingToPort = new Dictionary<string, List<string>>();  // portId → source port ids
            var outgoingFromPort = new Dictionary<string, List<string>>(); // portId → target port ids

            foreach (var edge in graph.GetEdgesForNode(repNode.Id))
            {
                bool sourceIsRep = repPortIds.Contains(edge.SourcePortId);
                bool targetIsRep = repPortIds.Contains(edge.TargetPortId);

                if (!sourceIsRep && targetIsRep)
                {
                    // 外部/内部 → Rep端口
                    if (!incomingToPort.TryGetValue(edge.TargetPortId, out var list))
                    {
                        list = new List<string>();
                        incomingToPort[edge.TargetPortId] = list;
                    }
                    list.Add(edge.SourcePortId);
                }
                else if (sourceIsRep && !targetIsRep)
                {
                    // Rep端口 → 外部/内部
                    if (!outgoingFromPort.TryGetValue(edge.SourcePortId, out var list))
                    {
                        list = new List<string>();
                        outgoingFromPort[edge.SourcePortId] = list;
                    }
                    list.Add(edge.TargetPortId);
                }
            }

            // 对每个 Rep 端口，如果同时有入边和出边，则创建直连
            foreach (var repPort in repNode.Ports)
            {
                var hasIncoming = incomingToPort.TryGetValue(repPort.Id, out var sources);
                var hasOutgoing = outgoingFromPort.TryGetValue(repPort.Id, out var targets);

                if (hasIncoming && hasOutgoing)
                {
                    foreach (var srcPortId in sources!)
                    {
                        foreach (var tgtPortId in targets!)
                        {
                            var newEdgeId = IdGenerator.NewId();
                            var newEdge = new Edge(newEdgeId, srcPortId, tgtPortId);
                            graph.AddEdgeDirect(newEdge);
                            _createdEdges!.Add(new EdgeSnapshot(newEdgeId, srcPortId, tgtPortId, null));
                        }
                    }
                }
            }
        }

        // ── 快照类型 ──

        private class FrameSnapshot
        {
            public string Id = "";
            public string Title = "";
            public string RepresentativeNodeId = "";
            public Rect2 Bounds;
            public bool IsCollapsed;
            public string? SourceAssetId;
            public List<string> ContainedNodeIds = new List<string>();
        }

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
