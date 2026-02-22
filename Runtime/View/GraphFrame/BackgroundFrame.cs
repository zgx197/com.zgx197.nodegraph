#nullable enable
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>背景网格渲染描述</summary>
    public class BackgroundFrame
    {
        /// <summary>可见区域（画布坐标）</summary>
        public Rect2 VisibleRect { get; set; }

        /// <summary>小网格大小</summary>
        public float SmallGridSize { get; set; }

        /// <summary>大网格大小</summary>
        public float LargeGridSize { get; set; }

        /// <summary>背景色</summary>
        public Color4 BackgroundColor { get; set; }

        /// <summary>小网格线颜色</summary>
        public Color4 SmallLineColor { get; set; }

        /// <summary>大网格线颜色</summary>
        public Color4 LargeLineColor { get; set; }
    }
}
