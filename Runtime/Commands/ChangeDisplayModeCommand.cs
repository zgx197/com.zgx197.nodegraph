#nullable enable
using NodeGraph.Core;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 切换节点显示模式（Expanded / Collapsed / Minimized）。
    /// </summary>
    public class ChangeDisplayModeCommand : IStyleCommand
    {
        private readonly string _nodeId;
        private readonly NodeDisplayMode _newMode;
        private NodeDisplayMode _oldMode;

        public string Description { get; }

        public ChangeDisplayModeCommand(string nodeId, NodeDisplayMode newMode)
        {
            _nodeId = nodeId;
            _newMode = newMode;
            Description = $"切换显示模式为 {newMode}";
        }

        public void Execute(Graph graph)
        {
            var node = graph.FindNode(_nodeId);
            if (node == null) return;

            _oldMode = node.DisplayMode;
            node.DisplayMode = _newMode;
        }

        public void Undo(Graph graph)
        {
            var node = graph.FindNode(_nodeId);
            if (node == null) return;

            node.DisplayMode = _oldMode;
        }
    }
}
