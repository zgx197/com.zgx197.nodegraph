#nullable enable
using System.Collections.Generic;
using NodeGraph.Math;

namespace NodeGraph.Core
{
    /// <summary>节点内容信息（由 INodeContentRenderer 提供给引擎渲染器）</summary>
    public class NodeContentInfo
    {
        /// <summary>内容区域矩形</summary>
        public Rect2 ContentRect { get; set; }

        /// <summary>内容类型标识（引擎渲染器据此决定如何绘制）</summary>
        public string TypeId { get; set; } = string.Empty;

        /// <summary>摘要文本行（简单场景直接显示文字）</summary>
        public List<string> SummaryLines { get; } = new List<string>();

        /// <summary>是否显示编辑模式（而非摘要模式）</summary>
        public bool ShowEditor { get; set; }

        /// <summary>关联的节点引用（引擎渲染器可通过此访问 UserData）</summary>
        public Node? Node { get; set; }

        /// <summary>端口区域下方是否有分隔线</summary>
        public bool HasSeparator { get; set; }
    }

    /// <summary>连线标签信息</summary>
    public class EdgeLabelInfo
    {
        /// <summary>标签位置（连线中点）</summary>
        public Vec2 Position { get; set; }

        /// <summary>标签文本</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>标签尺寸</summary>
        public Vec2 Size { get; set; }

        /// <summary>关联的 Edge 引用</summary>
        public Edge? Edge { get; set; }
    }
}
