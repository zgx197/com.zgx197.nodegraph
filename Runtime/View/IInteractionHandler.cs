#nullable enable
using NodeGraph.Abstraction;
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>
    /// 交互处理器接口。所有交互处理器（拖拽、框选、连线等）实现此接口。
    /// GraphViewModel 按优先级顺序调用处理器链。
    /// </summary>
    public interface IInteractionHandler
    {
        /// <summary>处理器优先级（越小越先处理）</summary>
        int Priority { get; }

        /// <summary>当前是否正在活跃状态（如正在拖拽）</summary>
        bool IsActive { get; }

        /// <summary>
        /// 处理输入。返回 true 表示事件已消费，后续处理器不再接收。
        /// </summary>
        bool HandleInput(GraphViewModel viewModel, IPlatformInput input);

        /// <summary>获取交互覆盖层数据（如框选矩形、拖拽连线预览）。返回 null 表示无覆盖层。</summary>
        OverlayFrame? GetOverlay(GraphViewModel viewModel) => null;
    }
}
