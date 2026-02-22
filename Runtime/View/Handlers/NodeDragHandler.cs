#nullable enable
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Abstraction;
using NodeGraph.Commands;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.View.Handlers
{
    /// <summary>
    /// 节点拖拽处理器。左键拖拽选中的节点移动。
    /// </summary>
    public class NodeDragHandler : IInteractionHandler
    {
        public int Priority => 20;
        public bool IsActive { get; private set; }

        private Dictionary<string, Vec2>? _dragStartPositions;
        private bool _hasMoved;

        public bool HandleInput(GraphViewModel viewModel, IPlatformInput input)
        {
            if (input.IsMouseDown(MouseButton.Left) && !input.IsAltHeld)
            {
                Vec2 canvasPos = viewModel.ScreenToCanvas(input.MousePosition);
                var hitNode = viewModel.HitTestNode(canvasPos);

                if (hitNode == null)
                    return false; // 没命中节点，交给后续处理器（框选等）

                {
                    // 双击折叠的 Rep 节点 → 展开子蓝图
                    if (input.IsDoubleClick(MouseButton.Left)
                        && hitNode.TypeId == SubGraphConstants.BoundaryNodeTypeId)
                    {
                        var frame = viewModel.Graph.FindContainerSubGraphFrame(hitNode.Id);
                        if (frame != null && frame.IsCollapsed)
                        {
                            viewModel.Commands.Execute(new ToggleSubGraphCollapseCommand(frame.Id));
                            viewModel.RequestRepaint();
                            return true;
                        }
                    }

                    // 如果点击未选中的节点
                    if (!viewModel.Selection.IsSelected(hitNode.Id))
                    {
                        if (input.IsShiftHeld)
                            viewModel.Selection.AddToSelection(hitNode.Id);
                        else if (input.IsCtrlHeld)
                            viewModel.Selection.RemoveFromSelection(hitNode.Id);
                        else
                            viewModel.Selection.Select(hitNode.Id);
                    }

                    // 记录拖拽起始位置
                    _dragStartPositions = new Dictionary<string, Vec2>();
                    foreach (var nodeId in viewModel.Selection.SelectedNodeIds)
                    {
                        var node = viewModel.Graph.FindNode(nodeId);
                        if (node != null)
                            _dragStartPositions[nodeId] = node.Position;
                    }

                    IsActive = true;
                    _hasMoved = false;
                    return true;
                }
            }

            if (IsActive && input.IsMouseDrag(MouseButton.Left))
            {
                _hasMoved = true;
                Vec2 delta = input.MouseDelta / viewModel.ZoomLevel;

                foreach (var nodeId in viewModel.Selection.SelectedNodeIds)
                {
                    var node = viewModel.Graph.FindNode(nodeId);
                    if (node != null)
                        node.Position = node.Position + delta;
                }

                viewModel.RequestRepaint();
                return true;
            }

            if (IsActive && input.IsMouseUp(MouseButton.Left))
            {
                IsActive = false;

                // 如果实际发生了移动，生成移动命令
                if (_hasMoved && _dragStartPositions != null)
                {
                    var moves = _dragStartPositions
                        .Select(kv =>
                        {
                            var node = viewModel.Graph.FindNode(kv.Key);
                            return (kv.Key, kv.Value, node?.Position ?? kv.Value);
                        })
                        .Where(m => Vec2.DistanceSquared(m.Item2, m.Item3) > 0.01f)
                        .ToList();

                    if (moves.Count > 0)
                    {
                        // 先恢复到原始位置，然后通过命令执行
                        foreach (var (nodeId, oldPos, _) in moves)
                        {
                            var node = viewModel.Graph.FindNode(nodeId);
                            if (node != null) node.Position = oldPos;
                        }
                        viewModel.Commands.Execute(new MoveNodeCommand(moves));
                    }
                }

                _dragStartPositions = null;
                return true;
            }

            return false;
        }
    }
}
