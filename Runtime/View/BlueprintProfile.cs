#nullable enable
using System;
using System.Collections.Generic;
using NodeGraph.Abstraction;
using NodeGraph.Core;
using NodeGraph.Layout;

namespace NodeGraph.View
{
    /// <summary>功能开关</summary>
    [Flags]
    public enum BlueprintFeatureFlags
    {
        None = 0,
        MiniMap = 1,
        Search = 2,
        AutoLayout = 4,
        SubGraph = 8,
        DebugOverlay = 16,
        All = MiniMap | Search | AutoLayout | SubGraph
    }

    /// <summary>
    /// [扩展点] 蓝图配置包。将蓝图类型相关的所有配置集中管理。
    /// </summary>
    /// <remarks>
    /// 业务层为每种蓝图类型实例化一个 Profile，然后传入 <c>GraphViewModel</c> 。
    /// 关键字段：
    /// - <see cref="NodeTypes"/>       — 节点类型目录（搜索菜单数据源）
    /// - <see cref="ConnectionPolicy"/> — 连接规则（为 null 时使用 DefaultConnectionPolicy）
    /// - <see cref="ContentRenderers"/>  — TypeId → 自定义节点内容渲染器
    /// - <see cref="Theme"/>            — 视觉主题（默认 Dark）
    /// - <see cref="Features"/>         — 功能开关（小地图/搜索/自动布局/子图）
    /// 调用 <see cref="BuildRenderConfig"/> 将配置组装为 <see cref="GraphRenderConfig"/> 传入渲染层。
    /// </remarks>
    public class BlueprintProfile
    {
        /// <summary>蓝图类型名称（用于调试和日志）</summary>
        public string Name { get; set; } = "Default";

        /// <summary>渲染描述构建器（决定"怎么布局"）</summary>
        public IGraphFrameBuilder FrameBuilder { get; set; } = null!;

        /// <summary>视觉主题（决定"什么颜色/尺寸"）</summary>
        public NodeVisualTheme Theme { get; set; } = NodeVisualTheme.Dark;

        /// <summary>图拓扑策略</summary>
        public GraphTopologyPolicy Topology { get; set; } = GraphTopologyPolicy.DAG;

        /// <summary>默认布局方向</summary>
        public LayoutDirection DefaultLayoutDirection { get; set; } = LayoutDirection.LeftToRight;

        /// <summary>节点类型目录</summary>
        public INodeTypeCatalog NodeTypes { get; set; } = GraphSettings.CreateEmptyNodeTypeCatalog();

        /// <summary>节点内容渲染器（TypeId → Renderer）</summary>
        public Dictionary<string, INodeContentRenderer> ContentRenderers { get; } = new Dictionary<string, INodeContentRenderer>();

        /// <summary>连线标签渲染器（可为 null）</summary>
        public IEdgeLabelRenderer? EdgeLabelRenderer { get; set; }

        /// <summary>连接策略（可为 null，使用默认）</summary>
        public IConnectionPolicy? ConnectionPolicy { get; set; }

        /// <summary>功能开关</summary>
        public BlueprintFeatureFlags Features { get; set; } = BlueprintFeatureFlags.All;

        /// <summary>检查功能是否启用</summary>
        public bool HasFeature(BlueprintFeatureFlags flag) => (Features & flag) != 0;

        /// <summary>
        /// 将 Profile 的渲染相关字段组装成 <see cref="GraphRenderConfig"/>。
        /// 调用方无需了解 Profile 内部字段结构，始终通过此方法获取渲染配置。
        /// </summary>
        public GraphRenderConfig BuildRenderConfig()
        {
            var config = new GraphRenderConfig
            {
                FrameBuilder      = FrameBuilder,
                Theme             = Theme,
                EdgeLabelRenderer = EdgeLabelRenderer
            };
            foreach (var kvp in ContentRenderers)
                config.ContentRenderers[kvp.Key] = kvp.Value;
            return config;
        }
    }
}
