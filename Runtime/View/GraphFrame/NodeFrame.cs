#nullable enable
using System.Collections.Generic;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>单个节点的渲染描述</summary>
    public class NodeFrame
    {
        /// <summary>节点 ID</summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>节点类型 ID</summary>
        public string TypeId { get; set; } = string.Empty;

        /// <summary>节点矩形（画布坐标）</summary>
        public Rect2 Bounds { get; set; }

        /// <summary>标题栏颜色</summary>
        public Color4 TitleColor { get; set; }

        /// <summary>标题文字</summary>
        public string TitleText { get; set; } = string.Empty;

        /// <summary>是否被选中</summary>
        public bool Selected { get; set; }

        /// <summary>是否为主选中节点</summary>
        public bool IsPrimary { get; set; }

        /// <summary>显示模式</summary>
        public NodeDisplayMode DisplayMode { get; set; }

        /// <summary>端口列表</summary>
        public List<PortFrame> Ports { get; } = new List<PortFrame>();

        /// <summary>节点内容信息（可为 null 表示无内容区）</summary>
        public NodeContentInfo? Content { get; set; }

        /// <summary>是否为折叠状态的子图 RepresentativeNode（用于渲染展开按钮）</summary>
        public bool IsCollapsedSubGraph { get; set; }

        /// <summary>所属子图框 ID（仅当 IsCollapsedSubGraph 为 true 时有效）</summary>
        public string? SubGraphFrameId { get; set; }

        /// <summary>节点描述文字（来自 IDescribableNode.Description，空时不渲染描述条）</summary>
        public string? Description { get; set; }

        /// <summary>诊断覆盖边框颜色（null = 无覆盖）。由宿主窗口根据分析结果写入，渲染在选中发光之上。</summary>
        public Color4? OverlayBorderColor { get; set; }
    }
}
