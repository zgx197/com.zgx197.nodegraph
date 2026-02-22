#nullable enable
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 切换子图框折叠/展开状态。
    /// </summary>
    /// <remarks>
    /// 折叠时将 RepresentativeNode 移动到框左上角，展开时恢复原位。
    /// 实现 IStyleCommand（不触发分析重计）。
    /// </remarks>
    public class ToggleSubGraphCollapseCommand : IStyleCommand
    {
        private readonly string _frameId;
        private bool _previousState;
        private Vec2 _previousRepNodePos;

        public string Description { get; }

        public ToggleSubGraphCollapseCommand(string frameId)
        {
            _frameId = frameId;
            Description = $"切换子图框折叠状态 {frameId}";
        }

        public void Execute(Graph graph)
        {
            var frame = graph.FindSubGraphFrame(_frameId);
            if (frame == null) return;

            _previousState = frame.IsCollapsed;

            var repNode = graph.FindNode(frame.RepresentativeNodeId);
            if (repNode != null)
                _previousRepNodePos = repNode.Position;

            frame.IsCollapsed = !frame.IsCollapsed;

            // 折叠时：将 Rep 节点移到框左上角
            if (frame.IsCollapsed && repNode != null)
            {
                repNode.Position = new Vec2(frame.Bounds.X, frame.Bounds.Y);
            }
        }

        public void Undo(Graph graph)
        {
            var frame = graph.FindSubGraphFrame(_frameId);
            if (frame == null) return;

            frame.IsCollapsed = _previousState;

            // 恢复 Rep 节点原始位置
            var repNode = graph.FindNode(frame.RepresentativeNodeId);
            if (repNode != null)
                repNode.Position = _previousRepNodePos;
        }
    }
}
