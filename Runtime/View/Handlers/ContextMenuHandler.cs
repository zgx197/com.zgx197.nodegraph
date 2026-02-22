#nullable enable
using NodeGraph.Abstraction;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.View.Handlers
{
    /// <summary>
    /// 右键上下文菜单处理器。
    /// 检测右键点击（非拖拽），根据点击位置触发不同的事件回调：
    /// - 空白区域右键 → <see cref="GraphViewModel.OnContextMenuRequested"/>
    /// - 节点上右键 → <see cref="GraphViewModel.OnNodeContextMenuRequested"/>
    ///
    /// 采用混合架构：框架层负责"何时"触发，宿主窗口负责"如何"展示菜单。
    /// 宿主窗口可自由选择平台原生菜单（Unity GenericMenu）或自绘搜索面板。
    /// </summary>
    public class ContextMenuHandler : IInteractionHandler
    {
        /// <summary>优先级设为 90，在大多数交互处理器之后处理</summary>
        public int Priority => 90;

        public bool IsActive => false;

        // 追踪右键是否产生了拖拽（拖拽时不触发菜单）
        private bool _rightDragDetected;

        public bool HandleInput(GraphViewModel viewModel, IPlatformInput input)
        {
            // 右键按下：重置拖拽追踪
            if (input.IsMouseDown(MouseButton.Right))
            {
                _rightDragDetected = false;
                return false; // 不消费，允许其他处理器也接收
            }

            // 右键拖拽：标记为拖拽（不触发菜单）
            if (input.IsMouseDrag(MouseButton.Right))
            {
                _rightDragDetected = true;
                return false;
            }

            // 右键释放且未拖拽 → 触发上下文菜单
            if (input.IsMouseUp(MouseButton.Right) && !_rightDragDetected)
            {
                var canvasPos = viewModel.ScreenToCanvas(input.MousePosition);

                // 优先检测端口（端口比节点更小，需要优先命中）
                var hitPort = viewModel.HitTestPort(canvasPos, 14f);
                if (hitPort != null)
                {
                    viewModel.OnPortContextMenuRequested?.Invoke(hitPort, canvasPos);
                    return true;
                }

                var hitNode = viewModel.HitTestNode(canvasPos);

                if (hitNode == null)
                {
                    // 空白区域右键 → 添加节点菜单
                    viewModel.OnContextMenuRequested?.Invoke(canvasPos);
                }
                else
                {
                    // 节点上右键 → 节点上下文菜单
                    viewModel.OnNodeContextMenuRequested?.Invoke(hitNode, canvasPos);
                }

                return true; // 消费事件
            }

            return false;
        }
    }
}
