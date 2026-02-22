#nullable enable
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Math;

namespace NodeGraph.Core
{
    /// <summary>
    /// 子图实例化工具。从源 Graph 拷贝节点/边到目标 Graph，
    /// 创建 RepresentativeNode 和 SubGraphFrame 容器。
    /// </summary>
    public static class SubGraphInstantiator
    {
        /// <summary>实例化结果</summary>
        public class Result
        {
            /// <summary>创建的 SubGraphFrame</summary>
            public SubGraphFrame Frame { get; }

            /// <summary>创建的 RepresentativeNode</summary>
            public Node RepresentativeNode { get; }

            /// <summary>源节点ID → 目标节点ID 的映射</summary>
            public Dictionary<string, string> NodeIdMap { get; }

            /// <summary>源端口ID → 目标端口ID 的映射</summary>
            public Dictionary<string, string> PortIdMap { get; }

            public Result(SubGraphFrame frame, Node repNode,
                Dictionary<string, string> nodeIdMap, Dictionary<string, string> portIdMap)
            {
                Frame = frame;
                RepresentativeNode = repNode;
                NodeIdMap = nodeIdMap;
                PortIdMap = portIdMap;
            }
        }

        /// <summary>
        /// 将源 Graph 的内容拷贝到目标 Graph 中，创建内联子图框。
        /// </summary>
        /// <param name="targetGraph">目标图</param>
        /// <param name="sourceGraph">源图（被拷贝的子图资产）</param>
        /// <param name="title">子图框标题</param>
        /// <param name="insertPosition">插入位置（画布坐标）</param>
        /// <param name="boundaryPorts">
        /// 边界端口定义。RepresentativeNode 将拥有这些端口，
        /// 作为子图框折叠时的外部接口。若为 null，则自动从源图推断。
        /// </param>
        /// <param name="sourceAssetId">源资产 ID（可选，用于溯源）</param>
        /// <returns>实例化结果</returns>
        public static Result Instantiate(
            Graph targetGraph,
            Graph sourceGraph,
            string title,
            Vec2 insertPosition,
            PortDefinition[]? boundaryPorts = null,
            string? sourceAssetId = null)
        {
            var nodeIdMap = new Dictionary<string, string>();
            var portIdMap = new Dictionary<string, string>();

            // ── Step 1: 拷贝所有节点（生成新 ID）──
            foreach (var srcNode in sourceGraph.Nodes)
            {
                string newNodeId = IdGenerator.NewId();
                nodeIdMap[srcNode.Id] = newNodeId;

                var newNode = new Node(newNodeId, srcNode.TypeId,
                    srcNode.Position + insertPosition);
                newNode.DisplayMode = srcNode.DisplayMode;
                newNode.AllowDynamicPorts = srcNode.AllowDynamicPorts;

                // 拷贝端口（生成新 ID，记录映射）
                foreach (var srcPort in srcNode.Ports)
                {
                    string newPortId = IdGenerator.NewId();
                    portIdMap[srcPort.Id] = newPortId;

                    var newPort = new Port(
                        newPortId, newNodeId, srcPort.Name,
                        srcPort.Direction, srcPort.Kind,
                        srcPort.DataType, srcPort.Capacity, srcPort.SortOrder,
                        semanticId: srcPort.SemanticId);
                    // 通过内部 API 添加端口
                    newNode.AddPortDirect(newPort);
                }

                targetGraph.AddNodeDirect(newNode);
            }

            // ── Step 2: 拷贝所有边（重映射端口 ID）──
            foreach (var srcEdge in sourceGraph.Edges)
            {
                if (!portIdMap.TryGetValue(srcEdge.SourcePortId, out var newSourcePortId)) continue;
                if (!portIdMap.TryGetValue(srcEdge.TargetPortId, out var newTargetPortId)) continue;

                var newEdge = new Edge(IdGenerator.NewId(), newSourcePortId, newTargetPortId);
                targetGraph.AddEdgeDirect(newEdge);
            }

            // ── Step 3: 创建 RepresentativeNode ──
            string repNodeId = IdGenerator.NewId();
            var repNode = new Node(repNodeId, SubGraphConstants.BoundaryNodeTypeId, insertPosition);

            if (boundaryPorts != null)
            {
                foreach (var portDef in boundaryPorts)
                    repNode.AddPort(portDef);
            }
            else
            {
                // 自动推断边界端口：源图中没有入边的输入端口 → 子图输入
                //                   源图中没有出边的输出端口 → 子图输出
                InferBoundaryPorts(repNode, sourceGraph);
            }

            targetGraph.AddNodeDirect(repNode);

            // ── Step 4: 计算子图框包围盒 ──
            var containedIds = new HashSet<string>(nodeIdMap.Values);
            var bounds = ComputeBounds(targetGraph, containedIds, insertPosition);

            // ── Step 5: 创建 SubGraphFrame ──
            var frame = new SubGraphFrame(IdGenerator.NewId(), title, repNodeId)
            {
                Bounds = bounds,
                SourceAssetId = sourceAssetId,
                IsCollapsed = false
            };
            foreach (var nid in containedIds)
                frame.ContainedNodeIds.Add(nid);

            targetGraph.AddSubGraphFrameDirect(frame);

            return new Result(frame, repNode, nodeIdMap, portIdMap);
        }

        /// <summary>自动推断边界端口：未被内部连线覆盖的端口成为边界</summary>
        private static void InferBoundaryPorts(Node repNode, Graph sourceGraph)
        {
            // 收集所有有连线的端口 ID
            var connectedInputPorts = new HashSet<string>();
            var connectedOutputPorts = new HashSet<string>();
            foreach (var edge in sourceGraph.Edges)
            {
                connectedInputPorts.Add(edge.TargetPortId);
                connectedOutputPorts.Add(edge.SourcePortId);
            }

            int inputOrder = 0, outputOrder = 0;

            // 没有入边的 Input 端口 → 子图框的 Input 边界端口
            foreach (var node in sourceGraph.Nodes)
            {
                foreach (var port in node.Ports)
                {
                    if (port.Direction == PortDirection.Input && !connectedInputPorts.Contains(port.Id))
                    {
                        repNode.AddPort(new PortDefinition(
                            port.Name, PortDirection.Input, port.Kind,
                            port.DataType, PortCapacity.Single, inputOrder++));
                    }
                }
            }

            // 没有出边的 Output 端口 → 子图框的 Output 边界端口
            foreach (var node in sourceGraph.Nodes)
            {
                foreach (var port in node.Ports)
                {
                    if (port.Direction == PortDirection.Output && !connectedOutputPorts.Contains(port.Id))
                    {
                        repNode.AddPort(new PortDefinition(
                            port.Name, PortDirection.Output, port.Kind,
                            port.DataType, PortCapacity.Multiple, outputOrder++));
                    }
                }
            }
        }

        /// <summary>计算所有包含节点的最小包围盒（含 padding）</summary>
        private static Rect2 ComputeBounds(Graph graph, HashSet<string> nodeIds, Vec2 fallback)
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
                return new Rect2(fallback.X, fallback.Y, 200f, 150f);

            float padding = 30f;
            float titleBarHeight = 24f;
            return new Rect2(
                minX - padding, minY - padding - titleBarHeight,
                maxX - minX + padding * 2f, maxY - minY + padding * 2f + titleBarHeight);
        }
    }
}
