#nullable enable
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>单条连线的渲染描述</summary>
    public class EdgeFrame
    {
        /// <summary>连线 ID</summary>
        public string EdgeId { get; set; } = string.Empty;

        /// <summary>起点（源端口位置）</summary>
        public Vec2 Start { get; set; }

        /// <summary>终点（目标端口位置）</summary>
        public Vec2 End { get; set; }

        /// <summary>贝塞尔切线 A（从起点出发）</summary>
        public Vec2 TangentA { get; set; }

        /// <summary>贝塞尔切线 B（从终点出发）</summary>
        public Vec2 TangentB { get; set; }

        /// <summary>连线颜色</summary>
        public Color4 Color { get; set; }

        /// <summary>连线宽度</summary>
        public float Width { get; set; }

        /// <summary>是否被选中</summary>
        public bool Selected { get; set; }

        /// <summary>连线标签信息（可为 null 表示无标签）</summary>
        public EdgeLabelInfo? Label { get; set; }
    }
}
