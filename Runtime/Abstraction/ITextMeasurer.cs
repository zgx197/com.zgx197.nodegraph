#nullable enable
using NodeGraph.Math;

namespace NodeGraph.Abstraction
{
    /// <summary>
    /// 文字测量接口。各引擎实现此接口提供文字尺寸计算能力。
    /// 用于纯 C# 层的节点尺寸计算（FrameBuilder 等）。
    /// </summary>
    public interface ITextMeasurer
    {
        /// <summary>测量指定文字在指定字号下的像素尺寸</summary>
        Vec2 MeasureText(string text, float fontSize);
    }
}
