#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace NodeGraph.Math
{
    /// <summary>
    /// 三次贝塞尔曲线工具，用于计算连线路径。
    /// </summary>
    public static class BezierMath
    {
        /// <summary>
        /// 计算三次贝塞尔曲线上 t 处的点。
        /// p0=起点, p1=控制点1, p2=控制点2, p3=终点, t∈[0,1]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 Evaluate(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, float t)
        {
            float u = 1f - t;
            float uu = u * u;
            float uuu = uu * u;
            float tt = t * t;
            float ttt = tt * t;

            return new Vec2(
                uuu * p0.X + 3f * uu * t * p1.X + 3f * u * tt * p2.X + ttt * p3.X,
                uuu * p0.Y + 3f * uu * t * p1.Y + 3f * u * tt * p2.Y + ttt * p3.Y
            );
        }

        /// <summary>
        /// 将三次贝塞尔曲线分段为折线点数组（用于不支持原生贝塞尔的引擎）。
        /// </summary>
        /// <param name="segments">分段数，越大越平滑，推荐 16~32</param>
        /// <returns>共 segments+1 个点</returns>
        public static Vec2[] Tessellate(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, int segments = 20)
        {
            if (segments < 1) segments = 1;
            var points = new Vec2[segments + 1];
            float inv = 1f / segments;

            for (int i = 0; i <= segments; i++)
            {
                points[i] = Evaluate(p0, p1, p2, p3, i * inv);
            }
            return points;
        }

        /// <summary>
        /// 根据两个端口的位置和方向，计算连线的贝塞尔曲线控制点切线偏移。
        /// 返回 (tangentA, tangentB)，即 p1 = sourcePos + tangentA, p2 = targetPos + tangentB。
        /// </summary>
        /// <param name="sourcePos">输出端口位置</param>
        /// <param name="targetPos">输入端口位置</param>
        /// <param name="sourceIsOutput">源端口是否为输出端口</param>
        public static (Vec2 tangentA, Vec2 tangentB) ComputePortTangents(
            Vec2 sourcePos, Vec2 targetPos, bool sourceIsOutput = true)
        {
            // 切线强度：取两端水平距离的一部分，限制最小/最大值
            float dx = MathF.Abs(targetPos.X - sourcePos.X);
            float tangentStrength = MathF.Max(50f, MathF.Min(dx * 0.5f, 200f));

            Vec2 tangentA, tangentB;

            if (sourceIsOutput)
            {
                // 输出端口朝右，输入端口朝左
                tangentA = new Vec2(tangentStrength, 0f);
                tangentB = new Vec2(-tangentStrength, 0f);
            }
            else
            {
                // 反向情况
                tangentA = new Vec2(-tangentStrength, 0f);
                tangentB = new Vec2(tangentStrength, 0f);
            }

            return (tangentA, tangentB);
        }

        /// <summary>
        /// 垂直布局的切线计算。输出端口朝下，输入端口朝上。
        /// 适用于行为树、对话树等上下布局的蓝图类型。
        /// </summary>
        public static (Vec2 tangentA, Vec2 tangentB) ComputeVerticalTangents(
            Vec2 sourcePos, Vec2 targetPos)
        {
            float dy = MathF.Abs(targetPos.Y - sourcePos.Y);
            float tangentStrength = MathF.Max(50f, MathF.Min(dy * 0.5f, 200f));

            // 输出端口朝下（+Y），输入端口朝上（-Y）
            var tangentA = new Vec2(0f, tangentStrength);
            var tangentB = new Vec2(0f, -tangentStrength);
            return (tangentA, tangentB);
        }

        /// <summary>
        /// 计算点到三次贝塞尔曲线的近似最短距离（用于连线点击检测）。
        /// 通过均匀采样 N 个点来近似。
        /// </summary>
        /// <param name="point">待检测的点</param>
        /// <param name="samples">采样数，越大越精确</param>
        /// <returns>近似最短距离</returns>
        public static float DistanceToPoint(
            Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, Vec2 point, int samples = 20)
        {
            float minDistSq = float.MaxValue;
            float inv = 1f / samples;

            for (int i = 0; i <= samples; i++)
            {
                Vec2 curvePoint = Evaluate(p0, p1, p2, p3, i * inv);
                float distSq = Vec2.DistanceSquared(curvePoint, point);
                if (distSq < minDistSq) minDistSq = distSq;
            }

            return MathF.Sqrt(minDistSq);
        }
    }
}
