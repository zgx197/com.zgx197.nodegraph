#nullable enable
using NodeGraph.Abstraction;
using NodeGraph.Commands;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.View.Handlers
{
    /// <summary>
    /// 连线拖拽处理器。从端口拖出连线。
    /// </summary>
    public class ConnectionDragHandler : IInteractionHandler
    {
        public int Priority => 10;
        public bool IsActive { get; private set; }

        private Port? _sourcePort;
        private Vec2 _dragEndPos;

        /// <summary>端口点击检测半径</summary>
        public float PortHitRadius { get; set; } = 12f;

        /// <summary>当前拖拽的源端口（供 FrameBuilder 判断兼容性）</summary>
        public Port? DragSourcePort => IsActive ? _sourcePort : null;

        public bool HandleInput(GraphViewModel viewModel, IPlatformInput input)
        {
            if (!IsActive && input.IsMouseDown(MouseButton.Left) && !input.IsAltHeld)
            {
                Vec2 canvasPos = viewModel.ScreenToCanvas(input.MousePosition);
                var hitPort = viewModel.HitTestPort(canvasPos, PortHitRadius);

                if (hitPort != null)
                {
                    // 点击 Multiple Input 端口的"+"槽位 → 展开（增加空圆圈），不发起拖拽
                    if (viewModel.HitTestPlusSlot(canvasPos, hitPort, PortHitRadius))
                    {
                        viewModel.ExpandMultiplePort(hitPort.Id);
                        return true;
                    }

                    _sourcePort = hitPort;
                    _dragEndPos = canvasPos;
                    IsActive = true;
                    return true;
                }

                // 不在点击空白时收起展开状态，展开的圆圈保持直到连接完成或按 ESC
            }

            if (IsActive && input.IsMouseDrag(MouseButton.Left))
            {
                _dragEndPos = viewModel.ScreenToCanvas(input.MousePosition);
                viewModel.RequestRepaint();
                return true;
            }

            if (IsActive && input.IsMouseUp(MouseButton.Left))
            {
                Vec2 canvasPos = viewModel.ScreenToCanvas(input.MousePosition);
                var targetPort = viewModel.HitTestPort(canvasPos, PortHitRadius);

                if (targetPort != null && _sourcePort != null && targetPort.Id != _sourcePort.Id)
                {
                    // 尝试连接（不自动收起展开状态，由用户按 ESC 手动收起）
                    viewModel.Commands.Execute(new ConnectCommand(_sourcePort.Id, targetPort.Id));
                }

                _sourcePort = null;
                IsActive = false;
                viewModel.RequestRepaint();
                return true;
            }

            return false;
        }

        public OverlayFrame? GetOverlay(GraphViewModel viewModel)
        {
            if (!IsActive || _sourcePort == null) return null;

            Vec2 startPos = viewModel.GetPortPosition(_sourcePort);
            bool isOutput = _sourcePort.Direction == PortDirection.Output;

            var (tangentA, tangentB) = BezierMath.ComputePortTangents(startPos, _dragEndPos, isOutput);

            return new OverlayFrame
            {
                Type = OverlayType.DragConnection,
                Start = startPos,
                End = _dragEndPos,
                TangentA = tangentA,
                TangentB = tangentB,
                Color = new Color4(1f, 1f, 1f, 0.6f),
                Width = 2f
            };
        }
    }
}
