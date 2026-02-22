#nullable enable
using UnityEngine;
using NodeGraph.Abstraction;
using NodeGraph.Math;

namespace NodeGraph.Unity
{
    /// <summary>
    /// Unity 平台输入适配器。将 Unity Event 系统适配为 IPlatformInput。
    /// 每帧在 OnGUI 开始时调用 Update(Event.current) 更新状态。
    /// </summary>
    public class UnityPlatformInput : IPlatformInput
    {
        private Event? _current;
        private Vec2 _mousePosition;
        private Vec2 _mouseDelta;
        private float _scrollDelta;

        // 按键状态跟踪
        private bool _leftDown, _leftUp, _leftDrag;
        private bool _rightDown, _rightUp, _rightDrag;
        private bool _middleDown, _middleUp, _middleDrag;
        private bool _leftDoubleClick, _rightDoubleClick;
        private string _keyDown = "";

        /// <summary>
        /// 每帧 OnGUI 开始时调用，传入当前 Event。
        /// 注意：如果在 GUI.BeginClip 之后调用此方法，鼠标位置可能不准确。
        /// 推荐使用 Update(Event, CanvasCoordinateHelper) 重载，
        /// 通过 CanvasCoordinateHelper 获取校正后的鼠标位置。
        /// </summary>
        public void Update(Event evt)
        {
            UpdateInternal(evt, null);
        }

        /// <summary>
        /// 每帧 OnGUI 开始时调用，使用 CanvasCoordinateHelper 提供的校正鼠标位置。
        /// 此重载解决了 GUI.BeginClip 在某些 Unity 版本下无法正确调整鼠标坐标的问题。
        /// </summary>
        /// <param name="evt">当前 Unity Event</param>
        /// <param name="coordinateHelper">坐标校正工具（须在 GUI.BeginClip 之前调用 CaptureMousePosition）</param>
        public void Update(Event evt, CanvasCoordinateHelper coordinateHelper)
        {
            UpdateInternal(evt, coordinateHelper);
        }

        private void UpdateInternal(Event evt, CanvasCoordinateHelper? coordinateHelper)
        {
            _current = evt;

            // 重置每帧状态
            _leftDown = _leftUp = _leftDrag = false;
            _rightDown = _rightUp = _rightDrag = false;
            _middleDown = _middleUp = _middleDrag = false;
            _leftDoubleClick = _rightDoubleClick = false;
            _scrollDelta = 0f;
            _mouseDelta = Vec2.Zero;
            _keyDown = "";

            if (evt == null) return;

            // 使用 CanvasCoordinateHelper 校正的鼠标位置（绕过 GUI.BeginClip 的坐标调整问题）
            if (coordinateHelper != null)
            {
                var corrected = coordinateHelper.CorrectedMousePosition;
                _mousePosition = corrected;
            }
            else
            {
                _mousePosition = new Vec2(evt.mousePosition.x, evt.mousePosition.y);
            }
            _mouseDelta = new Vec2(evt.delta.x, evt.delta.y);

            switch (evt.type)
            {
                case EventType.MouseDown:
                    SetMouseDown(evt.button);
                    if (evt.clickCount == 2)
                        SetDoubleClick(evt.button);
                    break;

                case EventType.MouseUp:
                    SetMouseUp(evt.button);
                    break;

                case EventType.MouseDrag:
                    SetMouseDrag(evt.button);
                    break;

                case EventType.ScrollWheel:
                    _scrollDelta = -evt.delta.y;
                    break;

                case EventType.KeyDown:
                    if (evt.keyCode != KeyCode.None)
                        _keyDown = evt.keyCode.ToString();
                    break;
            }
        }

        // ── IPlatformInput 实现 ──

        public Vec2 MousePosition => _mousePosition;
        public Vec2 MouseDelta => _mouseDelta;
        public float ScrollDelta => _scrollDelta;

        public bool IsMouseDown(MouseButton button) => button switch
        {
            MouseButton.Left => _leftDown,
            MouseButton.Right => _rightDown,
            MouseButton.Middle => _middleDown,
            _ => false
        };

        public bool IsMouseUp(MouseButton button) => button switch
        {
            MouseButton.Left => _leftUp,
            MouseButton.Right => _rightUp,
            MouseButton.Middle => _middleUp,
            _ => false
        };

        public bool IsMouseDrag(MouseButton button) => button switch
        {
            MouseButton.Left => _leftDrag,
            MouseButton.Right => _rightDrag,
            MouseButton.Middle => _middleDrag,
            _ => false
        };

        public bool IsDoubleClick(MouseButton button) => button switch
        {
            MouseButton.Left => _leftDoubleClick,
            MouseButton.Right => _rightDoubleClick,
            _ => false
        };

        public bool IsKeyDown(string keyName)
        {
            if (string.IsNullOrEmpty(keyName) || string.IsNullOrEmpty(_keyDown)) return false;
            return string.Equals(_keyDown, keyName, System.StringComparison.OrdinalIgnoreCase);
        }

        public bool IsKeyHeld(string keyName)
        {
            if (string.IsNullOrEmpty(keyName) || _current == null) return false;
            if (System.Enum.TryParse<KeyCode>(keyName, true, out var kc))
                return _current.type == EventType.KeyDown && _current.keyCode == kc;
            return false;
        }

        public bool IsShiftHeld => _current?.shift ?? false;
        public bool IsCtrlHeld => _current?.control ?? false;
        public bool IsAltHeld => _current?.alt ?? false;

        public string GetClipboardText() => GUIUtility.systemCopyBuffer ?? "";
        public void SetClipboardText(string text) => GUIUtility.systemCopyBuffer = text;

        // ── 内部辅助 ──

        private void SetMouseDown(int button)
        {
            switch (button)
            {
                case 0: _leftDown = true; break;
                case 1: _rightDown = true; break;
                case 2: _middleDown = true; break;
            }
        }

        private void SetMouseUp(int button)
        {
            switch (button)
            {
                case 0: _leftUp = true; break;
                case 1: _rightUp = true; break;
                case 2: _middleUp = true; break;
            }
        }

        private void SetMouseDrag(int button)
        {
            switch (button)
            {
                case 0: _leftDrag = true; break;
                case 1: _rightDrag = true; break;
                case 2: _middleDrag = true; break;
            }
        }

        private void SetDoubleClick(int button)
        {
            switch (button)
            {
                case 0: _leftDoubleClick = true; break;
                case 1: _rightDoubleClick = true; break;
            }
        }
    }
}
