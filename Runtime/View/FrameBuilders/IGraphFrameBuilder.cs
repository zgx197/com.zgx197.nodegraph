#nullable enable
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>
    /// 渲染帧构建器接口。不同蓝图类型可提供不同实现来定制视觉风格。
    /// 纯 C# 层，不依赖任何引擎 API。
    /// </summary>
    public interface IGraphFrameBuilder
    {
        /// <summary>计算节点尺寸（基于端口数量、标题宽度、内容区等）</summary>
        Vec2 ComputeNodeSize(Node node, GraphViewModel viewModel);

        /// <summary>计算端口在画布坐标系中的绝对位置（Multiple 端口返回槽位中心）</summary>
        Vec2 GetPortPosition(Port port, Node node, Rect2 nodeBounds, NodeVisualTheme theme, GraphViewModel viewModel);

        /// <summary>计算端口占用的视觉槽位数（用于命中检测定位）</summary>
        int GetPortSlotCount(Port port, GraphViewModel viewModel);

        /// <summary>
        /// 获取连线在目标端口上的具体槽位位置。
        /// Multiple Input 端口按边顺序分配槽位，非 Multiple 端口返回端口中心。
        /// </summary>
        Vec2 GetEdgeTargetPosition(Edge edge, Port targetPort, Node targetNode,
            Rect2 bounds, NodeVisualTheme theme, GraphViewModel viewModel);

        /// <summary>构建完整的渲染帧</summary>
        GraphFrame BuildFrame(GraphViewModel viewModel, Rect2 viewport);
    }
}
