#nullable enable
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>覆盖层类型</summary>
    public enum OverlayType
    {
        /// <summary>框选矩形</summary>
        MarqueeSelection,
        /// <summary>拖拽连线预览</summary>
        DragConnection,
        /// <summary>拖拽节点预览</summary>
        DragNode
    }

    /// <summary>覆盖层渲染描述（框选、拖拽预览等临时视觉元素）</summary>
    public class OverlayFrame
    {
        /// <summary>覆盖层类型</summary>
        public OverlayType Type { get; set; }

        /// <summary>矩形区域（框选时使用）</summary>
        public Rect2 Rect { get; set; }

        /// <summary>起点（拖拽连线时使用）</summary>
        public Vec2 Start { get; set; }

        /// <summary>终点（拖拽连线时使用）</summary>
        public Vec2 End { get; set; }

        /// <summary>贝塞尔切线 A</summary>
        public Vec2 TangentA { get; set; }

        /// <summary>贝塞尔切线 B</summary>
        public Vec2 TangentB { get; set; }

        /// <summary>颜色</summary>
        public Color4 Color { get; set; }

        /// <summary>线宽</summary>
        public float Width { get; set; } = 2f;
    }
}
