#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Layout
{
    /// <summary>
    /// 树形布局算法。适用于树形结构（行为树、单入口 DAG）。
    /// 从根节点（无入边的节点）开始，按层级从左到右排列。
    /// </summary>
    public class TreeLayout : ILayoutAlgorithm
    {
        /// <summary>层级间距（水平方向）</summary>
        public float LevelSpacing { get; set; } = 250f;

        /// <summary>同层节点间距（垂直方向）</summary>
        public float NodeSpacing { get; set; } = 80f;

        /// <summary>布局方向</summary>
        public LayoutDirection Direction { get; set; } = LayoutDirection.LeftToRight;

        public Dictionary<string, Vec2> ComputeLayout(Graph graph, Vec2 startPosition)
        {
            var result = new Dictionary<string, Vec2>();
            if (graph.Nodes.Count == 0) return result;

            // 找到根节点（无入边的节点）
            var roots = GraphAlgorithms.GetRootNodes(graph).ToList();
            if (roots.Count == 0)
            {
                // 没有根节点（有环），取第一个节点作为根
                roots.Add(graph.Nodes[0]);
            }

            // BFS 分层
            var visited = new HashSet<string>();
            var layers = new List<List<Node>>();

            var queue = new Queue<(Node node, int depth)>();
            foreach (var root in roots)
            {
                if (visited.Contains(root.Id)) continue;
                queue.Enqueue((root, 0));
                visited.Add(root.Id);
            }

            while (queue.Count > 0)
            {
                var (node, depth) = queue.Dequeue();

                while (layers.Count <= depth)
                    layers.Add(new List<Node>());
                layers[depth].Add(node);

                foreach (var successor in graph.GetSuccessors(node.Id))
                {
                    if (!visited.Contains(successor.Id))
                    {
                        visited.Add(successor.Id);
                        queue.Enqueue((successor, depth + 1));
                    }
                }
            }

            // 处理未访问的孤立节点
            foreach (var node in graph.Nodes)
            {
                if (!visited.Contains(node.Id))
                {
                    if (layers.Count == 0) layers.Add(new List<Node>());
                    layers[0].Add(node);
                }
            }

            // 分配位置
            for (int level = 0; level < layers.Count; level++)
            {
                var layer = layers[level];
                float totalHeight = layer.Count * NodeSpacing;
                float startOffset = -totalHeight * 0.5f;

                for (int i = 0; i < layer.Count; i++)
                {
                    Vec2 pos;
                    switch (Direction)
                    {
                        case LayoutDirection.LeftToRight:
                            pos = startPosition + new Vec2(level * LevelSpacing, startOffset + i * NodeSpacing);
                            break;
                        case LayoutDirection.TopToBottom:
                            pos = startPosition + new Vec2(startOffset + i * NodeSpacing, level * LevelSpacing);
                            break;
                        default:
                            pos = startPosition + new Vec2(level * LevelSpacing, startOffset + i * NodeSpacing);
                            break;
                    }
                    result[layer[i].Id] = pos;
                }
            }

            return result;
        }
    }

    /// <summary>布局方向</summary>
    public enum LayoutDirection
    {
        LeftToRight,
        TopToBottom
    }
}
