#nullable enable
using NodeGraph.Abstraction;

namespace NodeGraph.View
{
    /// <summary>
    /// 默认渲染帧构建器。水平布局（端口左进右出）+ 贝塞尔连线。
    /// 适用于刷怪蓝图、技能蓝图等左右水平布局的蓝图类型。
    /// 继承 BaseFrameBuilder，无需重写任何方法（基类默认即水平布局）。
    /// </summary>
    public class DefaultFrameBuilder : BaseFrameBuilder
    {
        public DefaultFrameBuilder(ITextMeasurer textMeasurer) : base(textMeasurer) { }

        // 基类默认 IsHorizontalLayout = true，ComputeEdgeRoute 使用水平贝塞尔
        // DefaultFrameBuilder 无需重写任何方法
    }
}
