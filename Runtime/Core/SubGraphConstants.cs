#nullable enable
using NodeGraph.Math;

namespace NodeGraph.Core
{
    /// <summary>
    /// SubGraphFrame 相关常量。
    /// </summary>
    public static class SubGraphConstants
    {
        /// <summary>
        /// 代表节点的 NodeTypeId。
        /// FrameBuilder 通过此 TypeId 识别 RepresentativeNode，
        /// 折叠时渲染为紧凑节点，展开时隐藏自身。
        /// </summary>
        public const string BoundaryNodeTypeId = "__SubGraphBoundary";

        /// <summary>代表节点的默认标题栏颜色（深蓝色调）</summary>
        public static readonly Color4 BoundaryNodeColor = new Color4(0.2f, 0.35f, 0.55f, 1f);
    }
}
