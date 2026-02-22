#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Math;

namespace NodeGraph.Core
{
    // ════════════════════════════════════════════════════════
    //  类型层次（v2.3）：
    //  GraphDecoration (abstract)      ← 画布上的非拓扑元素基类
    //  ├── GraphContainer (abstract)   ← 包含节点的容器基类
    //  │   ├── NodeGroup                ← 纯视觉分组（Color）
    //  │   └── SubGraphFrame            ← 增强容器（边界端口、折叠、来源追溯）
    //  └── GraphComment                 ← 文本注释
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// 图装饰元素基类（非拓扑元素：容器、注释等）。
    /// 有位置和大小，但不参与图的连接逻辑。
    /// </summary>
    public abstract class GraphDecoration
    {
        /// <summary>唯一 ID（GUID）</summary>
        public string Id { get; }

        /// <summary>画布上的边界矩形（位置 + 尺寸）</summary>
        public Rect2 Bounds { get; set; }

        protected GraphDecoration(string id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }
    }

    /// <summary>
    /// 节点容器基类。管理一组节点的归属关系。
    /// NodeGroup 和 SubGraphFrame 共享此公共契约。
    /// </summary>
    public abstract class GraphContainer : GraphDecoration
    {
        /// <summary>容器标题</summary>
        public string Title { get; set; }

        /// <summary>包含的节点 ID 集合（HashSet 保证 O(1) 查找）</summary>
        public HashSet<string> ContainedNodeIds { get; } = new HashSet<string>();

        protected GraphContainer(string id, string title) : base(id)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
        }

        /// <summary>根据包含的节点自动计算边界</summary>
        public void AutoFit(Graph graph, float padding = 20f)
        {
            if (ContainedNodeIds.Count == 0) return;

            var rects = ContainedNodeIds
                .Select(nid => graph.FindNode(nid))
                .Where(n => n != null)
                .Select(n => n!.GetBounds());

            var encapsulated = Rect2.Encapsulate(rects);
            if (encapsulated.Width <= 0 && encapsulated.Height <= 0) return;

            Bounds = new Rect2(
                encapsulated.X - padding,
                encapsulated.Y - padding - 24f, // 24f 预留标题栏高度
                encapsulated.Width + padding * 2f,
                encapsulated.Height + padding * 2f + 24f);
        }
    }

    /// <summary>
    /// 分组框。将若干节点归为一组，可整体拖动。纯视觉分组，无行为语义。
    /// </summary>
    public class NodeGroup : GraphContainer
    {
        /// <summary>分组颜色</summary>
        public Color4 Color { get; set; } = new Color4(0.3f, 0.5f, 0.8f, 0.3f);

        public NodeGroup(string id, string title) : base(id, title) { }

        public NodeGroup(string id, string title, Vec2 position) : base(id, title)
        {
            Bounds = new Rect2(position.X, position.Y, 0, 0);
        }
    }

    /// <summary>
    /// 子图框。继承 GraphContainer，在节点容器能力之上增加：
    /// - 代表节点（RepresentativeNode）：承载边界端口，折叠时作为可连线的普通节点
    /// - 折叠/展开状态
    /// - 来源资产追溯
    /// </summary>
    public class SubGraphFrame : GraphContainer
    {
        /// <summary>折叠状态</summary>
        public bool IsCollapsed { get; set; }

        /// <summary>
        /// 代表节点 ID。指向 Graph 中的一个真实 Node，该节点拥有所有边界端口。
        /// 折叠时：RepresentativeNode 正常渲染为紧凑节点（标题 + 边界端口）
        /// 展开时：RepresentativeNode 自身隐藏，其端口由 FrameBuilder 重新定位到框边缘
        /// </summary>
        public string RepresentativeNodeId { get; }

        /// <summary>来源资产引用（可选，用于追溯拷贝来源）</summary>
        public string? SourceAssetId { get; set; }

        public SubGraphFrame(string id, string title, string representativeNodeId)
            : base(id, title)
        {
            RepresentativeNodeId = representativeNodeId ?? throw new ArgumentNullException(nameof(representativeNodeId));
        }
    }

    /// <summary>
    /// 注释块。在画布上显示的自由文本注释。
    /// </summary>
    public class GraphComment : GraphDecoration
    {
        /// <summary>注释文本</summary>
        public string Text { get; set; }

        /// <summary>字体大小</summary>
        public float FontSize { get; set; } = 14f;

        /// <summary>文字颜色</summary>
        public Color4 TextColor { get; set; } = Color4.White;

        /// <summary>背景颜色</summary>
        public Color4 BackgroundColor { get; set; } = new Color4(0.2f, 0.2f, 0.2f, 0.7f);

        public GraphComment(string id, string text) : base(id)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public GraphComment(string id, string text, Vec2 position) : this(id, text)
        {
            Bounds = new Rect2(position.X, position.Y, 200f, 80f);
        }
    }
}
