#nullable enable
using System.Collections.Generic;
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>小地图中单个节点的简化信息</summary>
    public class MiniMapNodeInfo
    {
        /// <summary>节点矩形（小地图坐标）</summary>
        public Rect2 Rect { get; set; }

        /// <summary>节点颜色（标题栏颜色）</summary>
        public Color4 Color { get; set; }
    }

    /// <summary>小地图渲染描述</summary>
    public class MiniMapFrame
    {
        /// <summary>小地图在屏幕上的矩形</summary>
        public Rect2 ScreenRect { get; set; }

        /// <summary>小地图背景色</summary>
        public Color4 BackgroundColor { get; set; }

        /// <summary>小地图边框色</summary>
        public Color4 BorderColor { get; set; }

        /// <summary>当前视口在小地图中的矩形</summary>
        public Rect2 ViewportRect { get; set; }

        /// <summary>视口矩形颜色</summary>
        public Color4 ViewportColor { get; set; }

        /// <summary>节点列表</summary>
        public List<MiniMapNodeInfo> Nodes { get; } = new List<MiniMapNodeInfo>();
    }
}
