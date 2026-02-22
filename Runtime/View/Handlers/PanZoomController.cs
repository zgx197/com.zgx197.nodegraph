#nullable enable
using System;
using NodeGraph.Abstraction;
using NodeGraph.Math;

namespace NodeGraph.View.Handlers
{
    /// <summary>
    /// 画布平移缩放控制器。
    /// - 鼠标中键拖拽：平移
    /// - 滚轮：缩放（以鼠标位置为中心）
    /// - Alt + 左键拖拽：平移（备选方案）
    /// </summary>
    public class PanZoomController : IInteractionHandler
    {
        public int Priority => 0;
        public bool IsActive { get; private set; }

        /// <summary>缩放灵敏度</summary>
        public float ZoomSensitivity { get; set; } = 0.1f;

        public bool HandleInput(GraphViewModel viewModel, IPlatformInput input)
        {
            // 滚轮缩放
            if (MathF.Abs(input.ScrollDelta) > 0.001f)
            {
                float oldZoom = viewModel.ZoomLevel;
                float zoomDelta = input.ScrollDelta * ZoomSensitivity;
                float newZoom = System.Math.Clamp(oldZoom + zoomDelta, viewModel.MinZoom, viewModel.MaxZoom);

                if (MathF.Abs(newZoom - oldZoom) > 0.0001f)
                {
                    // 以鼠标位置为中心缩放
                    Vec2 mouseCanvas = viewModel.ScreenToCanvas(input.MousePosition);
                    viewModel.ZoomLevel = newZoom;

                    // 调整平移使鼠标位置不变
                    Vec2 newMouseCanvas = viewModel.ScreenToCanvas(input.MousePosition);
                    Vec2 delta = newMouseCanvas - mouseCanvas;
                    viewModel.PanOffset = viewModel.PanOffset + delta * newZoom;

                    viewModel.RequestRepaint();
                }
                return true;
            }

            // 中键拖拽平移
            if (input.IsMouseDown(MouseButton.Middle))
            {
                IsActive = true;
                return true;
            }

            if (IsActive && input.IsMouseDrag(MouseButton.Middle))
            {
                viewModel.PanOffset = viewModel.PanOffset + input.MouseDelta;
                viewModel.RequestRepaint();
                return true;
            }

            if (IsActive && input.IsMouseUp(MouseButton.Middle))
            {
                IsActive = false;
                return true;
            }

            // Alt + 左键备选平移
            if (input.IsAltHeld && input.IsMouseDown(MouseButton.Left))
            {
                IsActive = true;
                return true;
            }

            if (IsActive && input.IsAltHeld && input.IsMouseDrag(MouseButton.Left))
            {
                viewModel.PanOffset = viewModel.PanOffset + input.MouseDelta;
                viewModel.RequestRepaint();
                return true;
            }

            if (IsActive && input.IsMouseUp(MouseButton.Left))
            {
                IsActive = false;
                return true;
            }

            return false;
        }
    }
}
