#nullable enable
using System;
using System.Linq;
using NodeGraph.Abstraction;
using NodeGraph.Math;

namespace NodeGraph.View.Handlers
{
    /// <summary>
    /// 框选处理器。左键在空白区域拖拽进行框选。
    /// - 无修饰键：清空已有选择，选中框内节点
    /// - Shift：追加框内节点
    /// - Ctrl：从选择中移除框内节点
    /// </summary>
    public class MarqueeSelectionHandler : IInteractionHandler
    {
        public int Priority => 30;
        public bool IsActive { get; private set; }

        private Vec2 _startCanvasPos;
        private Vec2 _currentCanvasPos;
        private bool _isShift;
        private bool _isCtrl;

        public bool HandleInput(GraphViewModel viewModel, IPlatformInput input)
        {
            if (!IsActive && input.IsMouseDown(MouseButton.Left) && !input.IsAltHeld)
            {
                Vec2 canvasPos = viewModel.ScreenToCanvas(input.MousePosition);

                // 只有点击空白区域才开始框选
                if (viewModel.HitTestNode(canvasPos) == null &&
                    viewModel.HitTestPort(canvasPos, 12f) == null)
                {
                    _startCanvasPos = canvasPos;
                    _currentCanvasPos = canvasPos;
                    _isShift = input.IsShiftHeld;
                    _isCtrl = input.IsCtrlHeld;
                    IsActive = true;

                    // 无修饰键时先清空选择
                    if (!_isShift && !_isCtrl)
                        viewModel.Selection.ClearSelection();

                    return true;
                }
            }

            if (IsActive && input.IsMouseDrag(MouseButton.Left))
            {
                _currentCanvasPos = viewModel.ScreenToCanvas(input.MousePosition);
                viewModel.RequestRepaint();
                return true;
            }

            if (IsActive && input.IsMouseUp(MouseButton.Left))
            {
                IsActive = false;

                // 计算框选矩形
                var rect = GetSelectionRect();
                var nodesInRect = viewModel.Graph.Nodes
                    .Where(n => rect.Overlaps(n.GetBounds()))
                    .Select(n => n.Id)
                    .ToList();

                if (_isShift)
                    viewModel.Selection.AddMultipleToSelection(nodesInRect);
                else if (_isCtrl)
                    viewModel.Selection.RemoveMultipleFromSelection(nodesInRect);
                else
                    viewModel.Selection.SelectMultiple(nodesInRect);

                viewModel.RequestRepaint();
                return true;
            }

            return false;
        }

        public OverlayFrame? GetOverlay(GraphViewModel viewModel)
        {
            if (!IsActive) return null;

            var rect = GetSelectionRect();
            // 拖拽距离太小时不显示框选矩形，避免单击时闪现微小蓝框
            // 使用起点到终点距离（而非宽高）判断，任意方向微小移动都不会误触
            float dragDistSq = Vec2.DistanceSquared(_startCanvasPos, _currentCanvasPos);
            if (dragDistSq < 5f * 5f) return null;

            return new OverlayFrame
            {
                Type = OverlayType.MarqueeSelection,
                Rect = rect,
                Color = new Color4(0.3f, 0.5f, 0.8f, 0.8f)
            };
        }

        private Rect2 GetSelectionRect()
        {
            float x = MathF.Min(_startCanvasPos.X, _currentCanvasPos.X);
            float y = MathF.Min(_startCanvasPos.Y, _currentCanvasPos.Y);
            float w = MathF.Abs(_currentCanvasPos.X - _startCanvasPos.X);
            float h = MathF.Abs(_currentCanvasPos.Y - _startCanvasPos.Y);
            return new Rect2(x, y, w, h);
        }
    }
}
