#nullable enable
using System;
using System.Linq;
using NodeGraph.Abstraction;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>小地图位置</summary>
    public enum MiniMapPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    /// <summary>
    /// 小地图。在窗口角落显示整个图的缩略视图。
    /// v2.0: BuildFrame() 输出 MiniMapFrame 数据，由引擎渲染器消费绘制。
    /// 输入处理保留在此类中（直接操作 GraphViewModel）。
    /// </summary>
    public class MiniMapRenderer
    {
        /// <summary>小地图尺寸（像素）</summary>
        public Vec2 Size { get; set; } = new Vec2(200, 150);

        /// <summary>小地图位置</summary>
        public MiniMapPosition Position { get; set; } = MiniMapPosition.BottomRight;

        /// <summary>不透明度</summary>
        public float Opacity { get; set; } = 0.8f;

        /// <summary>是否可见</summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>边距</summary>
        public float Margin { get; set; } = 10f;

        private bool _isDragging;

        /// <summary>构建小地图帧数据</summary>
        public MiniMapFrame? BuildFrame(GraphViewModel viewModel, Rect2 windowRect)
        {
            if (!IsVisible || viewModel.Graph.Nodes.Count == 0) return null;

            var miniMapRect = GetMiniMapRect(windowRect);

            var graphBounds = GetGraphBounds(viewModel.Graph);
            if (graphBounds.Width <= 0 || graphBounds.Height <= 0) return null;

            var theme = viewModel.RenderConfig.Theme;

            float padding = 10f;
            var innerRect = miniMapRect.Expand(-padding);
            float scaleX = innerRect.Width / graphBounds.Width;
            float scaleY = innerRect.Height / graphBounds.Height;
            float scale = MathF.Min(scaleX, scaleY);

            float offsetX = innerRect.X + (innerRect.Width - graphBounds.Width * scale) * 0.5f;
            float offsetY = innerRect.Y + (innerRect.Height - graphBounds.Height * scale) * 0.5f;

            var frame = new MiniMapFrame
            {
                ScreenRect = miniMapRect,
                BackgroundColor = theme.MiniMapBgColor,
                BorderColor = theme.MiniMapBorderColor,
                ViewportColor = theme.MiniMapViewportColor
            };

            // 节点缩略矩形
            foreach (var node in viewModel.Graph.Nodes)
            {
                var bounds = node.GetBounds();
                var miniRect = new Rect2(
                    offsetX + (bounds.X - graphBounds.X) * scale,
                    offsetY + (bounds.Y - graphBounds.Y) * scale,
                    MathF.Max(bounds.Width * scale, 2f),
                    MathF.Max(bounds.Height * scale, 2f));

                var color = viewModel.GetNodeRenderInfo(node.TypeId).TitleColor;
                color = color.WithAlpha(Opacity);

                frame.Nodes.Add(new MiniMapNodeInfo { Rect = miniRect, Color = color });
            }

            // 当前视口矩形
            var visibleCanvas = viewModel.GetVisibleCanvasRect();
            frame.ViewportRect = new Rect2(
                offsetX + (visibleCanvas.X - graphBounds.X) * scale,
                offsetY + (visibleCanvas.Y - graphBounds.Y) * scale,
                visibleCanvas.Width * scale,
                visibleCanvas.Height * scale);

            return frame;
        }

        /// <summary>处理小地图上的点击（快速跳转）</summary>
        public bool HandleInput(GraphViewModel viewModel, IPlatformInput input, Rect2 windowRect)
        {
            if (!IsVisible || viewModel.Graph.Nodes.Count == 0) return false;

            var miniMapRect = GetMiniMapRect(windowRect);

            // 检查鼠标是否在小地图区域内
            if (!miniMapRect.Contains(input.MousePosition)) 
            {
                _isDragging = false;
                return false;
            }

            if (input.IsMouseDown(MouseButton.Left) || _isDragging)
            {
                _isDragging = input.IsMouseDrag(MouseButton.Left) || input.IsMouseDown(MouseButton.Left);

                var graphBounds = GetGraphBounds(viewModel.Graph);
                if (graphBounds.Width <= 0 || graphBounds.Height <= 0) return true;

                float padding = 10f;
                var innerRect = miniMapRect.Expand(-padding);
                float scaleX = innerRect.Width / graphBounds.Width;
                float scaleY = innerRect.Height / graphBounds.Height;
                float scale = MathF.Min(scaleX, scaleY);

                float offsetX = innerRect.X + (innerRect.Width - graphBounds.Width * scale) * 0.5f;
                float offsetY = innerRect.Y + (innerRect.Height - graphBounds.Height * scale) * 0.5f;

                // 鼠标位置 → 画布坐标
                float canvasX = graphBounds.X + (input.MousePosition.X - offsetX) / scale;
                float canvasY = graphBounds.Y + (input.MousePosition.Y - offsetY) / scale;

                // 将视口中心移到点击位置
                var visibleRect = viewModel.GetVisibleCanvasRect();
                viewModel.PanOffset = new Vec2(
                    windowRect.Width * 0.5f - canvasX * viewModel.ZoomLevel,
                    windowRect.Height * 0.5f - canvasY * viewModel.ZoomLevel);

                viewModel.RequestRepaint();
                return true;
            }

            if (input.IsMouseUp(MouseButton.Left))
            {
                _isDragging = false;
                return true;
            }

            return false;
        }

        private Rect2 GetMiniMapRect(Rect2 windowRect)
        {
            float x, y;
            switch (Position)
            {
                case MiniMapPosition.TopLeft:
                    x = windowRect.X + Margin;
                    y = windowRect.Y + Margin;
                    break;
                case MiniMapPosition.TopRight:
                    x = windowRect.Right - Size.X - Margin;
                    y = windowRect.Y + Margin;
                    break;
                case MiniMapPosition.BottomLeft:
                    x = windowRect.X + Margin;
                    y = windowRect.Bottom - Size.Y - Margin;
                    break;
                default: // BottomRight
                    x = windowRect.Right - Size.X - Margin;
                    y = windowRect.Bottom - Size.Y - Margin;
                    break;
            }
            return new Rect2(x, y, Size.X, Size.Y);
        }

        private Rect2 GetGraphBounds(Graph graph)
        {
            if (graph.Nodes.Count == 0)
                return new Rect2(0, 0, 100, 100);

            return Rect2.Encapsulate(graph.Nodes.Select(n => n.GetBounds())).Expand(50f);
        }
    }
}
