#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace NodeGraph.Core
{
    /// <summary>
    /// 图算法工具类。提供环检测、拓扑排序、可达性分析等。
    /// </summary>
    public static class GraphAlgorithms
    {
        /// <summary>
        /// 检测从 fromNodeId 到 toNodeId 添加一条边后是否会形成环。
        /// 原理：如果 toNodeId 能到达 fromNodeId，则添加 from→to 会形成环。
        /// </summary>
        public static bool WouldCreateCycle(Graph graph, string fromNodeId, string toNodeId)
        {
            // 如果 toNode 能到达 fromNode，则 from→to 会形成环
            var reachable = GetReachableNodes(graph, toNodeId);
            return reachable.Contains(fromNodeId);
        }

        /// <summary>
        /// 拓扑排序（仅 DAG 有效）。若图中存在环则返回 null。
        /// </summary>
        public static List<Node>? TopologicalSort(Graph graph)
        {
            var inDegree = new Dictionary<string, int>();
            var adjacency = new Dictionary<string, List<string>>();

            // 初始化
            foreach (var node in graph.Nodes)
            {
                inDegree[node.Id] = 0;
                adjacency[node.Id] = new List<string>();
            }

            // 构建邻接表和入度
            foreach (var edge in graph.Edges)
            {
                var sourcePort = FindPortInGraph(graph, edge.SourcePortId);
                var targetPort = FindPortInGraph(graph, edge.TargetPortId);
                if (sourcePort == null || targetPort == null) continue;

                var fromId = sourcePort.NodeId;
                var toId = targetPort.NodeId;

                if (adjacency.ContainsKey(fromId) && inDegree.ContainsKey(toId))
                {
                    adjacency[fromId].Add(toId);
                    inDegree[toId]++;
                }
            }

            // Kahn 算法
            var queue = new Queue<string>();
            foreach (var kv in inDegree)
            {
                if (kv.Value == 0) queue.Enqueue(kv.Key);
            }

            var result = new List<Node>();
            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                var node = graph.FindNode(nodeId);
                if (node != null) result.Add(node);

                foreach (var neighbor in adjacency[nodeId])
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0) queue.Enqueue(neighbor);
                }
            }

            // 如果结果数量不等于节点数量，说明存在环
            return result.Count == graph.Nodes.Count ? result : null;
        }

        /// <summary>获取所有根节点（无入边的节点）</summary>
        public static IEnumerable<Node> GetRootNodes(Graph graph)
        {
            var nodesWithInEdges = new HashSet<string>();
            foreach (var edge in graph.Edges)
            {
                var targetPort = FindPortInGraph(graph, edge.TargetPortId);
                if (targetPort != null) nodesWithInEdges.Add(targetPort.NodeId);
            }
            return graph.Nodes.Where(n => !nodesWithInEdges.Contains(n.Id));
        }

        /// <summary>获取所有叶子节点（无出边的节点）</summary>
        public static IEnumerable<Node> GetLeafNodes(Graph graph)
        {
            var nodesWithOutEdges = new HashSet<string>();
            foreach (var edge in graph.Edges)
            {
                var sourcePort = FindPortInGraph(graph, edge.SourcePortId);
                if (sourcePort != null) nodesWithOutEdges.Add(sourcePort.NodeId);
            }
            return graph.Nodes.Where(n => !nodesWithOutEdges.Contains(n.Id));
        }

        /// <summary>获取从指定节点可达的所有节点（BFS，沿 Output→Input 方向）</summary>
        public static HashSet<string> GetReachableNodes(Graph graph, string startNodeId)
        {
            var visited = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(startNodeId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current)) continue;

                // 找到该节点所有输出端口的连线对端节点
                var node = graph.FindNode(current);
                if (node == null) continue;

                foreach (var port in node.GetOutputPorts())
                {
                    foreach (var edge in graph.GetEdgesForPort(port.Id))
                    {
                        var targetPort = FindPortInGraph(graph, edge.TargetPortId);
                        if (targetPort != null && !visited.Contains(targetPort.NodeId))
                        {
                            queue.Enqueue(targetPort.NodeId);
                        }
                    }
                }
            }

            visited.Remove(startNodeId); // 不含起始节点自身
            return visited;
        }

        /// <summary>检测图中是否存在环</summary>
        public static bool HasCycle(Graph graph)
        {
            return TopologicalSort(graph) == null;
        }

        /// <summary>获取图中所有连通分量（忽略边的方向）</summary>
        public static List<HashSet<string>> GetConnectedComponents(Graph graph)
        {
            var visited = new HashSet<string>();
            var components = new List<HashSet<string>>();

            // 构建无向邻接表
            var adjacency = new Dictionary<string, HashSet<string>>();
            foreach (var node in graph.Nodes)
                adjacency[node.Id] = new HashSet<string>();

            foreach (var edge in graph.Edges)
            {
                var sourcePort = FindPortInGraph(graph, edge.SourcePortId);
                var targetPort = FindPortInGraph(graph, edge.TargetPortId);
                if (sourcePort == null || targetPort == null) continue;

                var fromId = sourcePort.NodeId;
                var toId = targetPort.NodeId;

                if (adjacency.ContainsKey(fromId)) adjacency[fromId].Add(toId);
                if (adjacency.ContainsKey(toId)) adjacency[toId].Add(fromId);
            }

            foreach (var node in graph.Nodes)
            {
                if (visited.Contains(node.Id)) continue;

                var component = new HashSet<string>();
                var queue = new Queue<string>();
                queue.Enqueue(node.Id);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    if (!component.Add(current)) continue;
                    visited.Add(current);

                    if (adjacency.TryGetValue(current, out var neighbors))
                    {
                        foreach (var neighbor in neighbors)
                        {
                            if (!component.Contains(neighbor))
                                queue.Enqueue(neighbor);
                        }
                    }
                }

                components.Add(component);
            }

            return components;
        }

        // ── 内部辅助 ──

        /// <summary>在图的所有节点中查找端口（委托给 Graph._portMap，O(1)）</summary>
        internal static Port? FindPortInGraph(Graph graph, string portId)
        {
            return graph.FindPort(portId);
        }
    }
}
