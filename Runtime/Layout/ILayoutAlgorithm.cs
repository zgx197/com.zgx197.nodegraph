#nullable enable
using System.Collections.Generic;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Layout
{
    /// <summary>
    /// 自动布局算法接口。计算图中节点的最佳位置。
    /// </summary>
    public interface ILayoutAlgorithm
    {
        /// <summary>
        /// 计算节点布局位置。
        /// </summary>
        /// <param name="graph">目标图</param>
        /// <param name="startPosition">布局起始位置（左上角）</param>
        /// <returns>节点 ID → 新位置的映射</returns>
        Dictionary<string, Vec2> ComputeLayout(Graph graph, Vec2 startPosition);
    }
}
