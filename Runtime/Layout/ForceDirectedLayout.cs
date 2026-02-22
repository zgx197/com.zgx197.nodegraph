#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Layout
{
    /// <summary>
    /// 力导向布局算法。适用于通用无明确方向的图。
    /// 使用简化版 Fruchterman-Reingold 算法：
    /// - 节点间斥力（库仑力）
    /// - 连线弹力（胡克力）
    /// - 阻尼衰减
    /// </summary>
    public class ForceDirectedLayout : ILayoutAlgorithm
    {
        /// <summary>迭代次数</summary>
        public int Iterations { get; set; } = 200;

        /// <summary>理想弹簧长度</summary>
        public float IdealLength { get; set; } = 200f;

        /// <summary>斥力系数</summary>
        public float RepulsionStrength { get; set; } = 10000f;

        /// <summary>弹力系数</summary>
        public float AttractionStrength { get; set; } = 0.01f;

        /// <summary>阻尼系数（每帧衰减）</summary>
        public float Damping { get; set; } = 0.9f;

        /// <summary>最大位移（防止爆炸）</summary>
        public float MaxDisplacement { get; set; } = 50f;

        public Dictionary<string, Vec2> ComputeLayout(Graph graph, Vec2 startPosition)
        {
            var result = new Dictionary<string, Vec2>();
            if (graph.Nodes.Count == 0) return result;

            // 初始化位置（圆形分布）
            var positions = new Dictionary<string, Vec2>();
            var velocities = new Dictionary<string, Vec2>();
            int count = graph.Nodes.Count;
            float radius = MathF.Max(count * 30f, 200f);

            for (int i = 0; i < count; i++)
            {
                float angle = 2f * MathF.PI * i / count;
                positions[graph.Nodes[i].Id] = startPosition + new Vec2(
                    MathF.Cos(angle) * radius,
                    MathF.Sin(angle) * radius);
                velocities[graph.Nodes[i].Id] = Vec2.Zero;
            }

            // 预计算连线关系
            var edgePairs = new List<(string src, string tgt)>();
            foreach (var edge in graph.Edges)
            {
                var sp = graph.FindPort(edge.SourcePortId);
                var tp = graph.FindPort(edge.TargetPortId);
                if (sp != null && tp != null)
                    edgePairs.Add((sp.NodeId, tp.NodeId));
            }

            float temperature = MaxDisplacement;

            // 迭代
            for (int iter = 0; iter < Iterations; iter++)
            {
                var forces = new Dictionary<string, Vec2>();
                foreach (var node in graph.Nodes)
                    forces[node.Id] = Vec2.Zero;

                // 斥力（所有节点对）
                for (int i = 0; i < count; i++)
                {
                    for (int j = i + 1; j < count; j++)
                    {
                        string idA = graph.Nodes[i].Id;
                        string idB = graph.Nodes[j].Id;
                        Vec2 delta = positions[idA] - positions[idB];
                        float distSq = delta.LengthSquared();
                        if (distSq < 1f) distSq = 1f; // 防止除零

                        float force = RepulsionStrength / distSq;
                        Vec2 dir = delta.Normalized();
                        Vec2 f = dir * force;

                        forces[idA] = forces[idA] + f;
                        forces[idB] = forces[idB] - f;
                    }
                }

                // 弹力（连线节点对）
                foreach (var (src, tgt) in edgePairs)
                {
                    if (!positions.ContainsKey(src) || !positions.ContainsKey(tgt)) continue;

                    Vec2 delta = positions[tgt] - positions[src];
                    float dist = delta.Length();
                    if (dist < 0.1f) dist = 0.1f;

                    float force = AttractionStrength * (dist - IdealLength);
                    Vec2 dir = delta.Normalized();
                    Vec2 f = dir * force;

                    forces[src] = forces[src] + f;
                    forces[tgt] = forces[tgt] - f;
                }

                // 应用力（带阻尼和温度限制）
                foreach (var node in graph.Nodes)
                {
                    var vel = velocities[node.Id] * Damping + forces[node.Id];

                    // 限制位移
                    float speed = vel.Length();
                    if (speed > temperature)
                        vel = vel.Normalized() * temperature;

                    positions[node.Id] = positions[node.Id] + vel;
                    velocities[node.Id] = vel;
                }

                // 降温
                temperature *= 0.95f;
                if (temperature < 0.1f) break;
            }

            // 输出结果
            foreach (var node in graph.Nodes)
                result[node.Id] = positions[node.Id];

            return result;
        }
    }
}
