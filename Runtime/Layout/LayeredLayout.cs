#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Layout
{
    /// <summary>
    /// 分层布局算法（简化版 Sugiyama）。适用于 DAG。
    /// 1. 拓扑排序分层
    /// 2. 同层内按重心法排序减少交叉
    /// 3. 分配坐标
    /// </summary>
    public class LayeredLayout : ILayoutAlgorithm
    {
        /// <summary>层级间距</summary>
        public float LevelSpacing { get; set; } = 250f;

        /// <summary>同层节点间距</summary>
        public float NodeSpacing { get; set; } = 80f;

        /// <summary>布局方向</summary>
        public LayoutDirection Direction { get; set; } = LayoutDirection.LeftToRight;

        /// <summary>重心排序迭代次数</summary>
        public int BarycenterIterations { get; set; } = 4;

        public Dictionary<string, Vec2> ComputeLayout(Graph graph, Vec2 startPosition)
        {
            var result = new Dictionary<string, Vec2>();
            if (graph.Nodes.Count == 0) return result;

            // Step 1: 拓扑排序分层（最长路径法）
            var layers = AssignLayers(graph);

            // Step 2: 重心排序减少交叉
            for (int iter = 0; iter < BarycenterIterations; iter++)
            {
                // 正向扫描
                for (int i = 1; i < layers.Count; i++)
                    SortByBarycenter(graph, layers, i, true);

                // 反向扫描
                for (int i = layers.Count - 2; i >= 0; i--)
                    SortByBarycenter(graph, layers, i, false);
            }

            // Step 3: 分配坐标
            for (int level = 0; level < layers.Count; level++)
            {
                var layer = layers[level];
                float totalSpan = (layer.Count - 1) * NodeSpacing;
                float startOffset = -totalSpan * 0.5f;

                for (int i = 0; i < layer.Count; i++)
                {
                    Vec2 pos;
                    if (Direction == LayoutDirection.TopToBottom)
                        pos = startPosition + new Vec2(startOffset + i * NodeSpacing, level * LevelSpacing);
                    else
                        pos = startPosition + new Vec2(level * LevelSpacing, startOffset + i * NodeSpacing);

                    result[layer[i].Id] = pos;
                }
            }

            return result;
        }

        /// <summary>最长路径分层</summary>
        private List<List<Node>> AssignLayers(Graph graph)
        {
            var layers = new List<List<Node>>();
            var nodeLayer = new Dictionary<string, int>();

            // 拓扑排序
            var sorted = GraphAlgorithms.TopologicalSort(graph);
            if (sorted == null)
            {
                // 有环，退化为简单BFS
                return FallbackBfsLayers(graph);
            }

            // 最长路径分配层
            foreach (var node in sorted)
            {
                int maxPredLayer = -1;
                foreach (var pred in graph.GetPredecessors(node.Id))
                {
                    if (nodeLayer.TryGetValue(pred.Id, out int predLayer))
                        maxPredLayer = System.Math.Max(maxPredLayer, predLayer);
                }

                int layer = maxPredLayer + 1;
                nodeLayer[node.Id] = layer;

                while (layers.Count <= layer)
                    layers.Add(new List<Node>());
                layers[layer].Add(node);
            }

            // 处理未在拓扑排序中的孤立节点
            foreach (var node in graph.Nodes)
            {
                if (!nodeLayer.ContainsKey(node.Id))
                {
                    if (layers.Count == 0) layers.Add(new List<Node>());
                    layers[0].Add(node);
                }
            }

            return layers;
        }

        private List<List<Node>> FallbackBfsLayers(Graph graph)
        {
            var layers = new List<List<Node>>();
            var visited = new HashSet<string>();
            var roots = GraphAlgorithms.GetRootNodes(graph).ToList();
            if (roots.Count == 0 && graph.Nodes.Count > 0)
                roots.Add(graph.Nodes[0]);

            var queue = new Queue<(Node, int)>();
            foreach (var r in roots)
            {
                queue.Enqueue((r, 0));
                visited.Add(r.Id);
            }

            while (queue.Count > 0)
            {
                var (node, depth) = queue.Dequeue();
                while (layers.Count <= depth)
                    layers.Add(new List<Node>());
                layers[depth].Add(node);

                foreach (var succ in graph.GetSuccessors(node.Id))
                {
                    if (!visited.Contains(succ.Id))
                    {
                        visited.Add(succ.Id);
                        queue.Enqueue((succ, depth + 1));
                    }
                }
            }

            foreach (var node in graph.Nodes)
            {
                if (!visited.Contains(node.Id))
                {
                    if (layers.Count == 0) layers.Add(new List<Node>());
                    layers[0].Add(node);
                }
            }

            return layers;
        }

        /// <summary>重心法排序：根据相邻层连接节点的平均位置排序</summary>
        private void SortByBarycenter(Graph graph, List<List<Node>> layers, int layerIndex, bool useUpLayer)
        {
            var layer = layers[layerIndex];
            var adjacentIndex = useUpLayer ? layerIndex - 1 : layerIndex + 1;
            if (adjacentIndex < 0 || adjacentIndex >= layers.Count) return;

            var adjacentLayer = layers[adjacentIndex];
            var adjacentPositions = new Dictionary<string, int>();
            for (int i = 0; i < adjacentLayer.Count; i++)
                adjacentPositions[adjacentLayer[i].Id] = i;

            var barycenters = new Dictionary<string, float>();
            foreach (var node in layer)
            {
                var neighbors = useUpLayer
                    ? graph.GetPredecessors(node.Id)
                    : graph.GetSuccessors(node.Id);

                var positions = neighbors
                    .Where(n => adjacentPositions.ContainsKey(n.Id))
                    .Select(n => (float)adjacentPositions[n.Id])
                    .ToList();

                barycenters[node.Id] = positions.Count > 0
                    ? positions.Average()
                    : float.MaxValue; // 无连接的节点排最后
            }

            layer.Sort((a, b) => barycenters[a.Id].CompareTo(barycenters[b.Id]));
        }
    }
}
