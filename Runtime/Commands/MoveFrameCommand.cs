#nullable enable
using System.Collections.Generic;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 移动子图框，同时移动框的 Bounds 和内部所有节点。
    /// </summary>
    public class MoveFrameCommand : IStyleCommand
    {
        private readonly string _frameId;
        private readonly Vec2 _oldFramePos;
        private readonly Vec2 _newFramePos;
        private readonly List<(string nodeId, Vec2 oldPos, Vec2 newPos)> _nodeMoves;

        public string Description { get; }

        public MoveFrameCommand(
            string frameId, Vec2 oldFramePos, Vec2 newFramePos,
            List<(string nodeId, Vec2 oldPos, Vec2 newPos)> nodeMoves)
        {
            _frameId = frameId;
            _oldFramePos = oldFramePos;
            _newFramePos = newFramePos;
            _nodeMoves = nodeMoves;
            Description = "移动子蓝图框";
        }

        public void Execute(Graph graph)
        {
            var frame = graph.FindSubGraphFrame(_frameId);
            if (frame != null)
            {
                var delta = _newFramePos - _oldFramePos;
                frame.Bounds = new Rect2(
                    _newFramePos.X, _newFramePos.Y,
                    frame.Bounds.Width, frame.Bounds.Height);
            }

            foreach (var (nodeId, _, newPos) in _nodeMoves)
            {
                var node = graph.FindNode(nodeId);
                if (node != null) node.Position = newPos;
            }
        }

        public void Undo(Graph graph)
        {
            var frame = graph.FindSubGraphFrame(_frameId);
            if (frame != null)
            {
                frame.Bounds = new Rect2(
                    _oldFramePos.X, _oldFramePos.Y,
                    frame.Bounds.Width, frame.Bounds.Height);
            }

            foreach (var (nodeId, oldPos, _) in _nodeMoves)
            {
                var node = graph.FindNode(nodeId);
                if (node != null) node.Position = oldPos;
            }
        }
    }
}
