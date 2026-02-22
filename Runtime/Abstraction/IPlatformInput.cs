#nullable enable
using NodeGraph.Math;

namespace NodeGraph.Abstraction
{
    /// <summary>鼠标按键</summary>
    public enum MouseButton
    {
        Left,
        Right,
        Middle
    }

    /// <summary>
    /// 平台输入接口。各引擎将自己的输入事件适配为此接口。
    /// 每帧由引擎宿主窗口调用 Update 后，框架通过此接口读取输入状态。
    /// </summary>
    public interface IPlatformInput
    {
        // ── 鼠标状态 ──

        /// <summary>鼠标当前位置（屏幕/窗口坐标）</summary>
        Vec2 MousePosition { get; }

        /// <summary>鼠标帧间位移</summary>
        Vec2 MouseDelta { get; }

        /// <summary>滚轮增量（正值向上/放大，负值向下/缩小）</summary>
        float ScrollDelta { get; }

        // ── 鼠标按键 ──

        /// <summary>指定按键是否在本帧按下</summary>
        bool IsMouseDown(MouseButton button);

        /// <summary>指定按键是否在本帧抬起</summary>
        bool IsMouseUp(MouseButton button);

        /// <summary>指定按键是否正在拖拽（按住并移动）</summary>
        bool IsMouseDrag(MouseButton button);

        /// <summary>指定按键是否双击</summary>
        bool IsDoubleClick(MouseButton button);

        // ── 键盘 ──

        /// <summary>指定键是否在本帧按下</summary>
        bool IsKeyDown(string keyName);

        /// <summary>指定键是否被持续按住</summary>
        bool IsKeyHeld(string keyName);

        // ── 修饰键 ──

        /// <summary>Shift 键是否按住</summary>
        bool IsShiftHeld { get; }

        /// <summary>Ctrl / Cmd 键是否按住</summary>
        bool IsCtrlHeld { get; }

        /// <summary>Alt / Option 键是否按住</summary>
        bool IsAltHeld { get; }

        // ── 剪贴板 ──

        /// <summary>获取剪贴板文本</summary>
        string GetClipboardText();

        /// <summary>设置剪贴板文本</summary>
        void SetClipboardText(string text);
    }
}
