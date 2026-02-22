#nullable enable
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 从源 Graph 实例化子图框（拷贝节点 + RepresentativeNode）。
    /// </summary>
    /// <remarks>
    /// 与 <see cref="GroupNodesCommand"/>（就地包裹）的差异：
    /// 此命令从外部 Graph（如模板或子蓝图资产）将内容实例化到当前图，
    /// 选中节点不变。
    /// </remarks>
    public class CreateSubGraphCommand : IStructuralCommand
    {
        private readonly Graph _sourceGraph;
        private readonly string _title;
        private readonly Vec2 _insertPosition;
        private readonly PortDefinition[]? _boundaryPorts;
        private readonly string? _sourceAssetId;

        // Undo 快照
        private SubGraphInstantiator.Result? _result;
        private List<string>? _createdNodeIds;
        private List<string>? _createdEdgeIds;

        public string Description { get; }

        public CreateSubGraphCommand(
            Graph sourceGraph,
            string title,
            Vec2 insertPosition,
            PortDefinition[]? boundaryPorts = null,
            string? sourceAssetId = null)
        {
            _sourceGraph = sourceGraph;
            _title = title;
            _insertPosition = insertPosition;
            _boundaryPorts = boundaryPorts;
            _sourceAssetId = sourceAssetId;
            Description = $"创建子图框 {title}";
        }

        public void Execute(Graph graph)
        {
            if (_result == null)
            {
                // 首次执行：实例化子图
                _result = SubGraphInstantiator.Instantiate(
                    graph, _sourceGraph, _title, _insertPosition,
                    _boundaryPorts, _sourceAssetId);

                // 记录所有创建的节点和边 ID（含 RepresentativeNode）
                _createdNodeIds = _result.NodeIdMap.Values.ToList();
                _createdNodeIds.Add(_result.RepresentativeNode.Id);

                _createdEdgeIds = new List<string>();
                foreach (var edge in graph.Edges)
                {
                    var sourcePort = graph.FindPort(edge.SourcePortId);
                    if (sourcePort != null && _createdNodeIds.Contains(sourcePort.NodeId))
                        _createdEdgeIds.Add(edge.Id);
                }
            }
            else
            {
                // Redo：重新添加保存的节点、边和框
                ReAddAll(graph);
            }
        }

        public void Undo(Graph graph)
        {
            if (_result == null) return;

            // 移除所有创建的边
            if (_createdEdgeIds != null)
            {
                foreach (var edgeId in _createdEdgeIds)
                    graph.Disconnect(edgeId);
            }

            // 移除所有创建的节点（含 RepresentativeNode）
            if (_createdNodeIds != null)
            {
                foreach (var nodeId in _createdNodeIds)
                    graph.RemoveNode(nodeId);
            }

            // 移除 SubGraphFrame
            graph.RemoveSubGraphFrame(_result.Frame.Id);
        }

        /// <summary>Redo 时重新添加所有元素</summary>
        private void ReAddAll(Graph graph)
        {
            if (_result == null || _createdNodeIds == null) return;

            // 重新执行一次完整实例化（ID 已固定在 _result 中）
            // 由于我们需要保持相同的 ID，这里用 Instantiate 重新跑一遍
            // 但更好的方式是缓存 Node/Edge 快照 —— 这是后续优化项
            var newResult = SubGraphInstantiator.Instantiate(
                graph, _sourceGraph, _title, _insertPosition,
                _boundaryPorts, _sourceAssetId);

            // 更新内部引用
            _result = newResult;
            _createdNodeIds = newResult.NodeIdMap.Values.ToList();
            _createdNodeIds.Add(newResult.RepresentativeNode.Id);

            _createdEdgeIds = new List<string>();
            foreach (var edge in graph.Edges)
            {
                var sourcePort = graph.FindPort(edge.SourcePortId);
                if (sourcePort != null && _createdNodeIds.Contains(sourcePort.NodeId))
                    _createdEdgeIds.Add(edge.Id);
            }
        }
    }
}
