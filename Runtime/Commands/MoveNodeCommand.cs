#nullable enable
using System.Collections.Generic;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 移动节点（支持批量）。
    /// </summary>
    /// <remarks>
    /// 实现 <see cref="ICommand"/> 的 IStyleCommand 分支，不触发分析重计。
    /// 可覆写 <see cref="ICommand.TryMergeWith"/> 将连续拖拽合并为一次 Undo 记录。
    /// </remarks>
    public class MoveNodeCommand : IStyleCommand
    {
        private readonly List<(string nodeId, Vec2 oldPos, Vec2 newPos)> _moves;

        public string Description { get; }

        /// <summary>移动单个节点</summary>
        public MoveNodeCommand(string nodeId, Vec2 oldPosition, Vec2 newPosition)
        {
            _moves = new List<(string, Vec2, Vec2)> { (nodeId, oldPosition, newPosition) };
            Description = "移动节点";
        }

        /// <summary>批量移动多个节点</summary>
        public MoveNodeCommand(IEnumerable<(string nodeId, Vec2 oldPos, Vec2 newPos)> moves)
        {
            _moves = new List<(string, Vec2, Vec2)>(moves);
            Description = $"移动 {_moves.Count} 个节点";
        }

        public void Execute(Graph graph)
        {
            foreach (var (nodeId, _, newPos) in _moves)
            {
                var node = graph.FindNode(nodeId);
                if (node != null)
                {
                    node.Position = newPos;
                    graph.Events.RaiseNodeMoved(node);
                }
            }
        }

        public void Undo(Graph graph)
        {
            foreach (var (nodeId, oldPos, _) in _moves)
            {
                var node = graph.FindNode(nodeId);
                if (node != null)
                {
                    node.Position = oldPos;
                    graph.Events.RaiseNodeMoved(node);
                }
            }
        }
    }
}
