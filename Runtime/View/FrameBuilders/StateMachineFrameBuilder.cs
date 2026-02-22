#nullable enable
using System;
using NodeGraph.Abstraction;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>
    /// 状态机渲染帧构建器。自由布局 + 贝塞尔曲线（带箭头语义）。
    /// 端口在左右两侧（与 Default 相同），但连线风格可扩展为带箭头。
    /// 适用于状态机、有限自动机等自由布局的蓝图类型。
    /// </summary>
    public class StateMachineFrameBuilder : BaseFrameBuilder
    {
        public StateMachineFrameBuilder(ITextMeasurer textMeasurer) : base(textMeasurer) { }

        // 状态机默认水平布局（端口左右），后续可按需扩展为四周端口
        protected override bool IsHorizontalLayout => true;

        /// <summary>
        /// 状态机连线路由：使用水平贝塞尔，但切线系数更大以形成更优雅的弧线。
        /// 后续可在此扩展箭头、自环等特殊路由。
        /// </summary>
        protected override (Vec2 tangentA, Vec2 tangentB) ComputeEdgeRoute(Vec2 start, Vec2 end)
        {
            // 更大的切线系数，使连线弧度更明显（适合状态间多条连线的视觉区分）
            float dx = MathF.Abs(end.X - start.X);
            float dy = MathF.Abs(end.Y - start.Y);
            float tangentLength = MathF.Max(dx * 0.5f, MathF.Min(dy * 0.4f, 120f));
            tangentLength = MathF.Max(tangentLength, 40f);

            var tangentA = new Vec2(tangentLength, 0f);
            var tangentB = new Vec2(-tangentLength, 0f);
            return (tangentA, tangentB);
        }
    }
}
