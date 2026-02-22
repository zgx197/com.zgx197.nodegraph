#nullable enable
using NodeGraph.Abstraction;
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>
    /// 行为树渲染帧构建器。垂直布局（端口上进下出）+ 垂直折线连线。
    /// 适用于行为树、对话树等上下布局的蓝图类型。
    /// </summary>
    public class BehaviorTreeFrameBuilder : BaseFrameBuilder
    {
        public BehaviorTreeFrameBuilder(ITextMeasurer textMeasurer) : base(textMeasurer) { }

        protected override bool IsHorizontalLayout => false;

        /// <summary>垂直贝塞尔路由：输出端口朝下，输入端口朝上</summary>
        protected override (Vec2 tangentA, Vec2 tangentB) ComputeEdgeRoute(Vec2 start, Vec2 end)
        {
            return BezierMath.ComputeVerticalTangents(start, end);
        }
    }
}
