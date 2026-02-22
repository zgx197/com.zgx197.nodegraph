#nullable enable
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 将选中节点就地包裹为 SubGraphFrame。
    /// </summary>
    /// <remarks>
    /// 流程：
    /// 1. 创建 RepresentativeNode（含从跨边界连线推断的边界端口）
    /// 2. 计算包围盒
    /// 3. 创建 SubGraphFrame，将选中节点纳入 ContainedNodeIds
    /// 4. 重连跨边界连线：外部↔内部 → 外部↔Rep端口 + Rep端口↔内部
    /// 不允许嵌套：已属于子图的节点、Flow.Start/End 、RepresentativeNode 会被过滤。
    /// </remarks>
    public class GroupNodesCommand : IStructuralCommand
    {
        private readonly string _title;
        private readonly List<string> _nodeIds;

        // Execute 后记录，用于 Undo
        private string? _frameId;
        private string? _repNodeId;
        private List<EdgeRewire>? _rewires;

        public string Description { get; }

        /// <summary>获取执行后创建的 SubGraphFrame ID</summary>
        public string? CreatedFrameId => _frameId;

        public GroupNodesCommand(string title, IEnumerable<string> nodeIds)
        {
            _title = title;
            _nodeIds = nodeIds.ToList();
            Description = $"创建子蓝图 {title}";
        }

        /// <summary>不允许加入子图的节点类型</summary>
        private static readonly HashSet<string> ExcludedTypeIds = new HashSet<string>
        {
            "Flow.Start", "Flow.End"
        };

        public void Execute(Graph graph)
        {
            // 过滤掉不允许加入子图的节点：
            // 1. Flow.Start / Flow.End 等特殊节点
            // 2. 已属于其他子图的节点（禁止嵌套，只允许单层子图）
            // 3. RepresentativeNode（边界节点本身）
            var containedNodeIds = new HashSet<string>();
            foreach (var sgf in graph.SubGraphFrames)
                foreach (var nid in sgf.ContainedNodeIds)
                    containedNodeIds.Add(nid);

            _nodeIds.RemoveAll(id =>
            {
                var node = graph.FindNode(id);
                if (node == null) return true;
                if (ExcludedTypeIds.Contains(node.TypeId)) return true;
                if (node.TypeId == SubGraphConstants.BoundaryNodeTypeId) return true;
                if (containedNodeIds.Contains(id)) return true;
                return false;
            });
            if (_nodeIds.Count == 0) return;

            var selectedSet = new HashSet<string>(_nodeIds);

            // ── Step 1: 推断边界端口 ──
            var boundaryInfo = InferBoundaryPorts(graph, selectedSet);

            // ── Step 2: 创建 RepresentativeNode ──
            _repNodeId = IdGenerator.NewId();
            var repNode = new Node(_repNodeId, SubGraphConstants.BoundaryNodeTypeId, Vec2.Zero);

            foreach (var portDef in boundaryInfo.PortDefinitions)
                repNode.AddPort(portDef);

            graph.AddNodeDirect(repNode);

            // ── Step 3: 重连跨边界连线 ──
            _rewires = new List<EdgeRewire>();
            ReconnectBoundaryEdges(graph, selectedSet, repNode, boundaryInfo);

            // ── Step 4: 计算包围盒 ──
            var bounds = ComputeBounds(graph, selectedSet);

            // 将 Rep 节点放在框的左上角（框外偏上）
            repNode.Position = new Vec2(bounds.X, bounds.Y - 60f);

            // ── Step 5: 创建 SubGraphFrame ──
            _frameId = IdGenerator.NewId();
            var frame = new SubGraphFrame(_frameId, _title, _repNodeId)
            {
                Bounds = bounds,
                IsCollapsed = false
            };
            foreach (var nid in _nodeIds)
                frame.ContainedNodeIds.Add(nid);

            graph.AddSubGraphFrameDirect(frame);
        }

        public void Undo(Graph graph)
        {
            // 1. 移除 Frame
            if (_frameId != null)
                graph.RemoveSubGraphFrame(_frameId);

            // 2. 恢复重连的边（删除新建的边，恢复原始边）
            if (_rewires != null)
            {
                foreach (var rw in _rewires)
                {
                    // 删除创建的新边
                    foreach (var newEdgeId in rw.CreatedEdgeIds)
                    {
                        var edge = graph.FindEdge(newEdgeId);
                        if (edge != null) graph.RemoveEdge(newEdgeId);
                    }

                    // 恢复原始边
                    var originalEdge = new Edge(rw.OriginalEdgeId, rw.OriginalSourcePortId, rw.OriginalTargetPortId);
                    graph.AddEdgeDirect(originalEdge);
                }
            }

            // 3. 删除 Rep 节点
            if (_repNodeId != null)
                graph.RemoveNode(_repNodeId);
        }

        // ══════════════════════════════════════
        //  边界端口推断
        // ══════════════════════════════════════

        private class BoundaryPortInfo
        {
            public List<PortDefinition> PortDefinitions = new List<PortDefinition>();
            /// <summary>原始端口ID → Rep上对应的边界端口名</summary>
            public Dictionary<string, string> InternalPortToRepPortName = new Dictionary<string, string>();
            /// <summary>边界端口名 → 方向</summary>
            public Dictionary<string, PortDirection> RepPortDirections = new Dictionary<string, PortDirection>();
        }

        /// <summary>
        /// 推断边界端口：分析跨越选中集合边界的连线，
        /// 按 (方向, Kind, DataType) 合并为单个边界端口。
        /// 同类型的跨界连线共享一个边界端口（如所有 Output/Control/exec → 一个"完成"端口）。
        /// </summary>
        private static BoundaryPortInfo InferBoundaryPorts(Graph graph, HashSet<string> selectedSet)
        {
            var info = new BoundaryPortInfo();

            // 按 (方向, Kind, DataType) 分组的边界端口
            // key = "direction|kind|dataType"
            var portGroups = new Dictionary<string, string>(); // groupKey → repPortName
            int inputOrder = 0, outputOrder = 0;

            foreach (var edge in graph.Edges)
            {
                var sp = graph.FindPort(edge.SourcePortId);
                var tp = graph.FindPort(edge.TargetPortId);
                if (sp == null || tp == null) continue;

                bool sourceInside = selectedSet.Contains(sp.NodeId);
                bool targetInside = selectedSet.Contains(tp.NodeId);

                if (sourceInside && !targetInside)
                {
                    // 内部 → 外部：需要 Output 边界端口
                    string groupKey = $"out|{sp.Kind}|{sp.DataType}";
                    if (!portGroups.TryGetValue(groupKey, out var repPortName))
                    {
                        repPortName = sp.Kind == PortKind.Control ? "完成" : $"out_{outputOrder}";
                        portGroups[groupKey] = repPortName;
                        info.PortDefinitions.Add(new PortDefinition(
                            repPortName, PortDirection.Output, sp.Kind,
                            sp.DataType, PortCapacity.Multiple, outputOrder++));
                        info.RepPortDirections[repPortName] = PortDirection.Output;
                    }
                    // 所有同类型的内部端口都映射到同一个边界端口
                    info.InternalPortToRepPortName[sp.Id] = repPortName;
                }
                else if (!sourceInside && targetInside)
                {
                    // 外部 → 内部：需要 Input 边界端口
                    string groupKey = $"in|{tp.Kind}|{tp.DataType}";
                    if (!portGroups.TryGetValue(groupKey, out var repPortName))
                    {
                        repPortName = tp.Kind == PortKind.Control ? "激活" : $"in_{inputOrder}";
                        portGroups[groupKey] = repPortName;
                        info.PortDefinitions.Add(new PortDefinition(
                            repPortName, PortDirection.Input, tp.Kind,
                            tp.DataType, PortCapacity.Single, inputOrder++));
                        info.RepPortDirections[repPortName] = PortDirection.Input;
                    }
                    info.InternalPortToRepPortName[tp.Id] = repPortName;
                }
            }

            // 确保始终有 Input 和 Output 边界端口（缺少的补上默认端口）
            bool hasInput = info.PortDefinitions.Any(p => p.Direction == PortDirection.Input);
            bool hasOutput = info.PortDefinitions.Any(p => p.Direction == PortDirection.Output);

            if (!hasInput)
            {
                info.PortDefinitions.Insert(0, new PortDefinition(
                    "激活", PortDirection.Input, PortKind.Control, "exec", PortCapacity.Single, 0));
            }
            if (!hasOutput)
            {
                info.PortDefinitions.Add(new PortDefinition(
                    "完成", PortDirection.Output, PortKind.Control, "exec", PortCapacity.Multiple,
                    info.PortDefinitions.Count));
            }

            return info;
        }

        /// <summary>
        /// 重连跨边界连线：
        /// - 外部A → 内部B → 变为 外部A → Rep.inPort + Rep.inPort → 内部B
        /// - 内部X → 外部Y → 变为 内部X → Rep.outPort + Rep.outPort → 外部Y
        /// </summary>
        private void ReconnectBoundaryEdges(
            Graph graph, HashSet<string> selectedSet, Node repNode, BoundaryPortInfo boundaryInfo)
        {
            var edgesToRewire = new List<Edge>();

            // 去重：同一对 (repPortId, externalPortId) 只创建一条外部段边
            var createdExternalEdges = new HashSet<string>(); // "repPortId→externalPortId"

            foreach (var edge in graph.Edges.ToList())
            {
                var sp = graph.FindPort(edge.SourcePortId);
                var tp = graph.FindPort(edge.TargetPortId);
                if (sp == null || tp == null) continue;

                bool sourceInside = selectedSet.Contains(sp.NodeId);
                bool targetInside = selectedSet.Contains(tp.NodeId);

                if (sourceInside && !targetInside &&
                    boundaryInfo.InternalPortToRepPortName.TryGetValue(sp.Id, out var outRepName))
                {
                    // 内部 → 外部：拆为 内部→Rep.outPort + Rep.outPort→外部
                    var repPort = repNode.Ports.FirstOrDefault(p => p.Name == outRepName);
                    if (repPort == null) continue;

                    var rw = new EdgeRewire
                    {
                        OriginalEdgeId = edge.Id,
                        OriginalSourcePortId = edge.SourcePortId,
                        OriginalTargetPortId = edge.TargetPortId,
                        CreatedEdgeIds = new List<string>()
                    };

                    // 删除原始边
                    graph.RemoveEdge(edge.Id);

                    // 创建 内部→Rep.outPort（每条原始边都需要）
                    var e1Id = IdGenerator.NewId();
                    graph.AddEdgeDirect(new Edge(e1Id, sp.Id, repPort.Id));
                    rw.CreatedEdgeIds.Add(e1Id);

                    // 创建 Rep.outPort→外部（同一对端口只创建一次）
                    string extKey = $"{repPort.Id}→{tp.Id}";
                    if (!createdExternalEdges.Contains(extKey))
                    {
                        createdExternalEdges.Add(extKey);
                        var e2Id = IdGenerator.NewId();
                        graph.AddEdgeDirect(new Edge(e2Id, repPort.Id, tp.Id));
                        rw.CreatedEdgeIds.Add(e2Id);
                    }

                    _rewires!.Add(rw);
                }
                else if (!sourceInside && targetInside &&
                    boundaryInfo.InternalPortToRepPortName.TryGetValue(tp.Id, out var inRepName))
                {
                    // 外部 → 内部：拆为 外部→Rep.inPort + Rep.inPort→内部
                    var repPort = repNode.Ports.FirstOrDefault(p => p.Name == inRepName);
                    if (repPort == null) continue;

                    var rw = new EdgeRewire
                    {
                        OriginalEdgeId = edge.Id,
                        OriginalSourcePortId = edge.SourcePortId,
                        OriginalTargetPortId = edge.TargetPortId,
                        CreatedEdgeIds = new List<string>()
                    };

                    // 删除原始边
                    graph.RemoveEdge(edge.Id);

                    // 创建 外部→Rep.inPort（同一对端口只创建一次）
                    string extKey = $"{sp.Id}→{repPort.Id}";
                    if (!createdExternalEdges.Contains(extKey))
                    {
                        createdExternalEdges.Add(extKey);
                        var e1Id = IdGenerator.NewId();
                        graph.AddEdgeDirect(new Edge(e1Id, sp.Id, repPort.Id));
                        rw.CreatedEdgeIds.Add(e1Id);
                    }

                    // 创建 Rep.inPort→内部（每条原始边都需要）
                    var e2Id = IdGenerator.NewId();
                    graph.AddEdgeDirect(new Edge(e2Id, repPort.Id, tp.Id));
                    rw.CreatedEdgeIds.Add(e2Id);

                    _rewires!.Add(rw);
                }
            }
        }

        /// <summary>计算选中节点的包围盒</summary>
        private static Rect2 ComputeBounds(Graph graph, HashSet<string> nodeIds)
        {
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var nid in nodeIds)
            {
                var node = graph.FindNode(nid);
                if (node == null) continue;

                var b = node.GetBounds();
                if (b.X < minX) minX = b.X;
                if (b.Y < minY) minY = b.Y;
                if (b.Right > maxX) maxX = b.Right;
                if (b.Bottom > maxY) maxY = b.Bottom;
            }

            if (minX > maxX)
                return new Rect2(0, 0, 200f, 150f);

            float padding = 30f;
            float titleBarHeight = 24f;
            return new Rect2(
                minX - padding, minY - padding - titleBarHeight,
                maxX - minX + padding * 2f, maxY - minY + padding * 2f + titleBarHeight);
        }

        // ── 重连记录 ──

        private class EdgeRewire
        {
            public string OriginalEdgeId = "";
            public string OriginalSourcePortId = "";
            public string OriginalTargetPortId = "";
            public List<string> CreatedEdgeIds = new List<string>();
        }
    }
}
