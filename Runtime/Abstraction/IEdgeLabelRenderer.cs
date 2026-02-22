#nullable enable
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Abstraction
{
    /// <summary>
    /// 连线标签渲染器接口。用于在连线中点绘制标签（如 TransitionCondition 的 "AllKilled" / "Delay 3s"）。
    /// v2.0: 返回 EdgeLabelInfo 纯数据，由引擎渲染器消费绘制。
    /// </summary>
    public interface IEdgeLabelRenderer
    {
        /// <summary>计算标签尺寸</summary>
        Vec2 GetLabelSize(Edge edge, ITextMeasurer measurer);

        /// <summary>获取连线标签信息（纯数据）</summary>
        EdgeLabelInfo GetLabelInfo(Edge edge, Vec2 midpoint);

        /// <summary>处理标签点击（返回 true 表示已消费该事件）</summary>
        bool HandleLabelClick(Edge edge, Rect2 labelRect);
    }
}
