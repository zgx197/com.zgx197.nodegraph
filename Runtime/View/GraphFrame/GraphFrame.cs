#nullable enable
using System.Collections.Generic;
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>
    /// 完整的图渲染帧描述。纯数据，无绘制调用。
    /// 由 IGraphFrameBuilder 在纯 C# 层构建，由各引擎原生渲染器消费。
    /// </summary>
    public class GraphFrame
    {
        /// <summary>背景网格</summary>
        public BackgroundFrame Background { get; set; } = new BackgroundFrame();

        /// <summary>装饰层（分组框 / 子图框 / 注释块，在节点下方绘制）</summary>
        public List<DecorationFrame> Decorations { get; } = new List<DecorationFrame>();

        /// <summary>节点列表（已按绘制顺序排列）</summary>
        public List<NodeFrame> Nodes { get; } = new List<NodeFrame>();

        /// <summary>连线列表</summary>
        public List<EdgeFrame> Edges { get; } = new List<EdgeFrame>();

        /// <summary>覆盖层（框选、拖拽预览等）</summary>
        public List<OverlayFrame> Overlays { get; } = new List<OverlayFrame>();

        /// <summary>小地图（为 null 表示不显示）</summary>
        public MiniMapFrame? MiniMap { get; set; }

        /// <summary>画布平移偏移</summary>
        public Vec2 PanOffset { get; set; }

        /// <summary>画布缩放级别</summary>
        public float ZoomLevel { get; set; } = 1f;

        /// <summary>清空所有帧数据（用于对象池复用）</summary>
        public void Clear()
        {
            Decorations.Clear();
            Nodes.Clear();
            Edges.Clear();
            Overlays.Clear();
            MiniMap = null;
        }
    }
}
