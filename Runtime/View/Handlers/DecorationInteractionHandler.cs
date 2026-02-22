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
    /// 子图框交互处理器。处理 SubGraphFrame 标题栏的交互：
    /// - 单击标题栏（无拖动）：折叠/展开切换
    /// - 拖动标题栏：整体移动框及所有内部节点
    /// - 标题栏点击同时选中 RepresentativeNode
    /// 优先级 15，高于 NodeDragHandler(20)，确保标题栏拖拽优先于节点拖拽。
    /// </summary>
    public class DecorationInteractionHandler : IInteractionHandler
    {
        public int Priority => 15;
        public bool IsActive { get; private set; }

        private SubGraphFrame? _dragFrame;
        private Vec2 _dragStartFramePos;
        private Dictionary<string, Vec2>? _dragStartNodePositions;
        private bool _hasMoved;

        public bool HandleInput(GraphViewModel viewModel, IPlatformInput input)
        {
            // ── 左键按下：检测标题栏命中 ──
            if (input.IsMouseDown(MouseButton.Left) && !input.IsAltHeld)
            {
                Vec2 canvasPos = viewModel.ScreenToCanvas(input.MousePosition);

                // 如果点击位置有可见节点，优先让 NodeDragHandler 处理
                var hitNode = viewModel.HitTestNode(canvasPos);
                if (hitNode != null) return false;

                var sgf = viewModel.HitTestSubGraphFrameTitleBar(canvasPos);
                if (sgf != null)
                {
                    if (sgf.IsCollapsed)
                    {
                        // 折叠状态：直接切换展开，不进入拖动模式
                        viewModel.Commands.Execute(new ToggleSubGraphCollapseCommand(sgf.Id));
                        viewModel.RequestRepaint();
                        return true;
                    }

                    _dragFrame = sgf;
                    _dragStartFramePos = new Vec2(sgf.Bounds.X, sgf.Bounds.Y);
                    _hasMoved = false;

                    // 记录所有内部节点 + Rep 节点的起始位置
                    _dragStartNodePositions = new Dictionary<string, Vec2>();
                    foreach (var nodeId in sgf.ContainedNodeIds)
                    {
                        var node = viewModel.Graph.FindNode(nodeId);
                        if (node != null) _dragStartNodePositions[nodeId] = node.Position;
                    }
                    var repNode = viewModel.Graph.FindNode(sgf.RepresentativeNodeId);
                    if (repNode != null)
                        _dragStartNodePositions[sgf.RepresentativeNodeId] = repNode.Position;

                    // 选中 RepresentativeNode（让 Inspector 能响应）
                    viewModel.Selection.Select(sgf.RepresentativeNodeId);

                    IsActive = true;
                    return true;
                }
            }

            // ── 拖动：移动框和内部节点 ──
            if (IsActive && input.IsMouseDrag(MouseButton.Left) && _dragFrame != null)
            {
                _hasMoved = true;
                Vec2 delta = input.MouseDelta / viewModel.ZoomLevel;

                // 移动框 Bounds
                _dragFrame.Bounds = new Rect2(
                    _dragFrame.Bounds.X + delta.X,
                    _dragFrame.Bounds.Y + delta.Y,
                    _dragFrame.Bounds.Width,
                    _dragFrame.Bounds.Height);

                // 移动所有内部节点 + Rep 节点
                if (_dragStartNodePositions != null)
                {
                    foreach (var nodeId in _dragStartNodePositions.Keys)
                    {
                        var node = viewModel.Graph.FindNode(nodeId);
                        if (node != null) node.Position = node.Position + delta;
                    }
                }

                viewModel.RequestRepaint();
                return true;
            }

            // ── 松开：生成命令或触发折叠 ──
            if (IsActive && input.IsMouseUp(MouseButton.Left))
            {
                IsActive = false;

                if (_hasMoved && _dragStartNodePositions != null && _dragFrame != null)
                {
                    // 收集节点移动数据
                    var moves = new List<(string, Vec2, Vec2)>();
                    foreach (var (nodeId, oldPos) in _dragStartNodePositions)
                    {
                        var node = viewModel.Graph.FindNode(nodeId);
                        if (node != null)
                        {
                            var newPos = node.Position;
                            node.Position = oldPos; // 先恢复，由命令重新执行
                            if (Vec2.DistanceSquared(oldPos, newPos) > 0.01f)
                                moves.Add((nodeId, oldPos, newPos));
                        }
                    }

                    // 恢复框位置，由命令重新执行
                    var newFramePos = new Vec2(_dragFrame.Bounds.X, _dragFrame.Bounds.Y);
                    _dragFrame.Bounds = new Rect2(
                        _dragStartFramePos.X, _dragStartFramePos.Y,
                        _dragFrame.Bounds.Width, _dragFrame.Bounds.Height);

                    if (moves.Count > 0)
                    {
                        viewModel.Commands.Execute(
                            new MoveFrameCommand(_dragFrame.Id, _dragStartFramePos, newFramePos, moves));
                    }
                }
                else if (!_hasMoved && _dragFrame != null)
                {
                    // 单击无拖动 → 折叠/展开切换
                    viewModel.Commands.Execute(new ToggleSubGraphCollapseCommand(_dragFrame.Id));
                }

                _dragFrame = null;
                _dragStartNodePositions = null;
                viewModel.RequestRepaint();
                return true;
            }

            return false;
        }
    }
}
