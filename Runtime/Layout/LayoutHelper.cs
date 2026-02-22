#nullable enable
using System.Collections.Generic;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Layout
{
    /// <summary>
    /// 布局辅助工具。将布局算法的结果应用到图中。
    /// </summary>
    public static class LayoutHelper
    {
        /// <summary>将布局结果应用到图中（直接设置节点位置）</summary>
        public static void ApplyLayout(Graph graph, Dictionary<string, Vec2> layout)
        {
            foreach (var (nodeId, position) in layout)
            {
                var node = graph.FindNode(nodeId);
                if (node != null)
                    node.Position = position;
            }
        }

        /// <summary>
        /// 将布局结果应用到图中，带动画中间状态。
        /// 返回插值后的位置（调用方按 t 从 0→1 驱动动画）。
        /// </summary>
        public static Dictionary<string, Vec2> InterpolateLayout(
            Graph graph, Dictionary<string, Vec2> targetLayout, float t)
        {
            var result = new Dictionary<string, Vec2>();
            foreach (var (nodeId, targetPos) in targetLayout)
            {
                var node = graph.FindNode(nodeId);
                if (node != null)
                    result[nodeId] = Vec2.Lerp(node.Position, targetPos, t);
            }
            return result;
        }

        /// <summary>计算布局后将所有节点居中到指定中心点</summary>
        public static Dictionary<string, Vec2> CenterLayout(
            Dictionary<string, Vec2> layout, Vec2 center)
        {
            if (layout.Count == 0) return layout;

            // 计算当前中心
            Vec2 sum = Vec2.Zero;
            foreach (var pos in layout.Values)
                sum = sum + pos;
            Vec2 currentCenter = sum / layout.Count;

            // 偏移
            Vec2 offset = center - currentCenter;
            var result = new Dictionary<string, Vec2>();
            foreach (var (id, pos) in layout)
                result[id] = pos + offset;

            return result;
        }
    }
}
