#nullable enable
using System.Linq;

namespace NodeGraph.Core
{
    /// <summary>
    /// 默认连接策略。
    /// 
    /// 三种连接场景：
    /// A) 内部桥接（边界端口 ↔ 同子图内节点）：豁免方向/容量，仅校验 Kind + DataType + 重复
    /// B) 外部连接（外部节点 ↔ 边界端口）：普通校验，但边界端口跳过容量检查
    /// C) 普通连接（同作用域内两个普通节点）：完整校验
    /// 
    /// 跨作用域的普通连接被拒绝——所有跨界流量必须经过边界端口。
    /// </summary>
    public class DefaultConnectionPolicy : IConnectionPolicy
    {
        private readonly IConnectionValidator[] _validators;

        /// <param name="validators">额外校验器列表（责任链节点），在内置校验全部通过后按序执行。</param>
        public DefaultConnectionPolicy(params IConnectionValidator[] validators)
        {
            _validators = validators ?? System.Array.Empty<IConnectionValidator>();
        }

        public virtual ConnectionResult CanConnect(Graph graph, Port source, Port target)
        {
            // 0. 不能连接同一节点的端口
            if (source.NodeId == target.NodeId)
                return ConnectionResult.SameNode;

            var sourceNode = graph.FindNode(source.NodeId);
            var targetNode = graph.FindNode(target.NodeId);
            if (sourceNode == null || targetNode == null)
                return ConnectionResult.SameDirection;

            bool sourceBoundary = sourceNode.TypeId == SubGraphConstants.BoundaryNodeTypeId;
            bool targetBoundary = targetNode.TypeId == SubGraphConstants.BoundaryNodeTypeId;

            // ════════════════════════════════════════
            //  场景 A：内部桥接（边界端口 ↔ 同子图内节点）
            // ════════════════════════════════════════
            if (sourceBoundary || targetBoundary)
            {
                var boundaryNode = sourceBoundary ? sourceNode : targetNode;
                var otherNode = sourceBoundary ? targetNode : sourceNode;
                var frame = graph.FindContainerSubGraphFrame(boundaryNode.Id);

                bool isInternal = frame != null
                    && frame.ContainedNodeIds.Contains(otherNode.Id);

                if (isInternal)
                {
                    // 桥接模式：豁免方向和容量，仅校验兼容性
                    if (source.Kind != target.Kind)
                        return ConnectionResult.KindMismatch;
                    if (!graph.Settings.Behavior.TypeCompatibility.IsCompatible(source.DataType, target.DataType))
                        return ConnectionResult.DataTypeMismatch;
                    if (graph.Edges.Any(e =>
                        (e.SourcePortId == source.Id && e.TargetPortId == target.Id) ||
                        (e.SourcePortId == target.Id && e.TargetPortId == source.Id)))
                        return ConnectionResult.DuplicateEdge;
                    return ConnectionResult.Success;
                }

                // 场景 B：外部连接 → 走下方普通校验（但容量检查会特殊处理）
            }

            // ════════════════════════════════════════
            //  作用域检查：禁止跨子图边界的普通连接
            // ════════════════════════════════════════
            if (!sourceBoundary && !targetBoundary)
            {
                var sourceScope = GetContainingFrameId(graph, sourceNode.Id);
                var targetScope = GetContainingFrameId(graph, targetNode.Id);
                if (sourceScope != targetScope)
                    return ConnectionResult.SameDirection; // 跨界拒绝
            }

            // ════════════════════════════════════════
            //  普通校验（场景 B 和场景 C 共用）
            // ════════════════════════════════════════

            // 1. 方向必须不同
            if (source.Direction == target.Direction)
                return ConnectionResult.SameDirection;

            var outPort = source.Direction == PortDirection.Output ? source : target;
            var inPort = source.Direction == PortDirection.Input ? source : target;

            // 2. Kind 匹配
            if (outPort.Kind != inPort.Kind)
                return ConnectionResult.KindMismatch;

            // 3. DataType 兼容
            if (!graph.Settings.Behavior.TypeCompatibility.IsCompatible(outPort.DataType, inPort.DataType))
                return ConnectionResult.DataTypeMismatch;

            // 4. 重复连接
            if (graph.Edges.Any(e => e.SourcePortId == outPort.Id && e.TargetPortId == inPort.Id))
                return ConnectionResult.DuplicateEdge;

            // 5. 容量检查（边界端口跳过——它需同时承载外部+内部连线）
            if (outPort.Capacity == PortCapacity.Single && !sourceBoundary && !targetBoundary
                && graph.GetEdgesForPort(outPort.Id).Any())
                return ConnectionResult.CapacityExceeded;

            if (inPort.Capacity == PortCapacity.Single && !sourceBoundary && !targetBoundary
                && graph.GetEdgesForPort(inPort.Id).Any())
                return ConnectionResult.CapacityExceeded;

            // 6. DAG 环检测
            if (graph.Settings.Topology == GraphTopologyPolicy.DAG)
            {
                if (GraphAlgorithms.WouldCreateCycle(graph, outPort.NodeId, inPort.NodeId))
                    return ConnectionResult.CycleDetected;
            }

            // 7. 责任链：依次调用注册的额外验证器
            foreach (var validator in _validators)
            {
                var extra = validator.Validate(graph, outPort, inPort);
                if (extra.HasValue)
                    return extra.Value;
            }

            return ConnectionResult.Success;
        }

        /// <summary>获取节点所在的 SubGraphFrame ID，根级节点返回 null</summary>
        private static string? GetContainingFrameId(Graph graph, string nodeId)
        {
            foreach (var sgf in graph.SubGraphFrames)
            {
                if (sgf.ContainedNodeIds.Contains(nodeId))
                    return sgf.Id;
            }
            return null;
        }
    }
}
