#nullable enable
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>
    /// 节点渲染所需的轻量显示信息——由 <see cref="GraphViewModel.GetNodeRenderInfo"/> 按需缓存提供。
    /// View 层（FrameBuilder、MiniMap 等）通过此结构获取节点颜色和显示名，
    /// 不再直接访问 Graph.Settings.NodeTypes，消除 View→Core 的直接穿透。
    /// </summary>
    public readonly struct NodeRenderInfo
    {
        /// <summary>节点显示名（无类型定义时回退为 TypeId）</summary>
        public string DisplayName { get; }

        /// <summary>节点标题色（无类型定义时为默认灰色）</summary>
        public Color4 TitleColor { get; }

        public NodeRenderInfo(string displayName, Color4 titleColor)
        {
            DisplayName = displayName;
            TitleColor  = titleColor;
        }

        /// <summary>从 <see cref="NodeTypeDefinition"/> 构建（定义为 null 时使用 typeId 作为显示名）</summary>
        public static NodeRenderInfo FromDefinition(NodeTypeDefinition? def, string typeId)
            => def != null
                ? new NodeRenderInfo(def.DisplayName, def.Color)
                : new NodeRenderInfo(typeId, new Color4(0.35f, 0.35f, 0.35f, 1f));
    }
}
