#nullable enable
using System.Collections.Generic;
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>
    /// 装饰元素类别。
    /// </summary>
    public enum DecorationKind
    {
        /// <summary>分组框（NodeGroup）</summary>
        Group,

        /// <summary>子图框（SubGraphFrame）</summary>
        SubGraph,

        /// <summary>注释块（GraphComment）</summary>
        Comment
    }

    /// <summary>
    /// 装饰层渲染帧。描述 NodeGroup / SubGraphFrame / GraphComment 的视觉信息。
    /// 纯数据，无绘制调用。由 FrameBuilder 构建，由引擎渲染器消费。
    /// </summary>
    public class DecorationFrame
    {
        /// <summary>装饰元素类别</summary>
        public DecorationKind Kind { get; set; }

        /// <summary>对应数据模型的 ID</summary>
        public string Id { get; set; } = "";

        /// <summary>画布坐标边界矩形</summary>
        public Rect2 Bounds { get; set; }

        /// <summary>标题（Group / SubGraph 使用）</summary>
        public string? Title { get; set; }

        /// <summary>背景颜色</summary>
        public Color4 BackgroundColor { get; set; }

        /// <summary>边框颜色</summary>
        public Color4 BorderColor { get; set; }

        /// <summary>标题栏高度</summary>
        public float TitleBarHeight { get; set; } = 24f;

        // ── SubGraph 专用 ──

        /// <summary>是否显示折叠按钮</summary>
        public bool ShowCollapseButton { get; set; }

        /// <summary>折叠状态</summary>
        public bool IsCollapsed { get; set; }

        /// <summary>SubGraph 展开时的边界端口（由 FrameBuilder 重新定位到框边缘）</summary>
        public List<PortFrame>? BoundaryPorts { get; set; }

        // ── Comment 专用 ──

        /// <summary>注释文本</summary>
        public string? Text { get; set; }

        /// <summary>注释字体大小</summary>
        public float FontSize { get; set; }

        /// <summary>注释文字颜色</summary>
        public Color4 TextColor { get; set; }
    }
}
