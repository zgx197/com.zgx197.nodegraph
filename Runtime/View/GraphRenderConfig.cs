#nullable enable
using System.Collections.Generic;
using NodeGraph.Abstraction;

namespace NodeGraph.View
{
    /// <summary>
    /// 图渲染配置——封装所有与"如何渲染"相关的依赖，
    /// 与 <see cref="GraphViewModel"/>（管理"视图状态"）职责分离。
    /// <para>
    /// 宿主在构造 <see cref="GraphViewModel"/> 时传入此配置，
    /// ViewModel 内部所有渲染决策均通过 RenderConfig 获取。
    /// </para>
    /// </summary>
    public sealed class GraphRenderConfig
    {
        /// <summary>节点/边界帧构建策略</summary>
        public IGraphFrameBuilder FrameBuilder { get; set; } = null!;

        /// <summary>视觉主题（颜色、字号、间距等）</summary>
        public NodeVisualTheme Theme { get; set; } = NodeVisualTheme.Dark;

        /// <summary>连线标签渲染器（可为 null）</summary>
        public IEdgeLabelRenderer? EdgeLabelRenderer { get; set; }

        /// <summary>节点内容渲染器注册表（TypeId → Renderer）</summary>
        public Dictionary<string, INodeContentRenderer> ContentRenderers { get; }
            = new Dictionary<string, INodeContentRenderer>();
    }
}
