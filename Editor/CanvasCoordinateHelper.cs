#nullable enable
using UnityEngine;
using NodeGraph.Math;

namespace NodeGraph.Unity
{
    /// <summary>
    /// 画布坐标系统工具类。
    /// 提供画布区域内的鼠标坐标校正，解决不同渲染模式下的坐标偏移问题。
    ///
    /// ── 支持的两种渲染模式 ──
    ///
    /// 【模式 A：Zero-Matrix + BeginClip（推荐）】
    /// GUI.matrix 保持 identity，使用 GUI.BeginClip 做视觉裁剪。
    /// 1. 调用 SetGraphAreaRect(graphRect) 设置画布区域
    /// 2. 在 GUI.BeginClip 之前调用 UnityPlatformInput.Update(evt, this)
    ///    （此时 evt.mousePosition 是窗口坐标，CorrectedMousePosition 减去 graphRect.position）
    /// 3. GUI.BeginClip(graphRect) 内调用 Render，screenOffset = Vector2.zero
    /// 4. GUI.EndClip()
    /// 此模式下 Handles/EditorGUI 在 BeginClip 内正常工作（因 GUI.matrix 无缩放分量）。
    ///
    /// 【模式 B：Zero-Matrix 无 BeginClip】
    /// 不使用 GUI.BeginClip，将 graphRect 偏移直接传给 Render 的 screenOffset 参数。
    /// 注意：此模式下节点可能渲染到画布区域外（如工具栏上方），需自行处理裁剪。
    ///
    /// ── 注意事项 ──
    /// - 当 GUI.matrix 包含缩放分量（scale≠1）时，不要使用 GUI.BeginClip，
    ///   因为 Handles 在此组合下会出现 clipOffset 偏移问题。
    /// - Zero-Matrix 模式（GUI.matrix = identity）下使用 BeginClip 是安全的。
    /// </summary>
    public class CanvasCoordinateHelper
    {
        /// <summary>画布区域在窗口中的矩形</summary>
        private Rect _graphAreaRect;

        /// <summary>
        /// 设置画布区域矩形。
        /// 鼠标坐标将通过减去 graphRect.position 转换为画布区域相对坐标。
        /// </summary>
        public void SetGraphAreaRect(Rect graphAreaRect)
        {
            _graphAreaRect = graphAreaRect;
        }

        /// <summary>
        /// 画布区域相对鼠标位置（从窗口坐标减去 graphRect.position）。
        /// 等价于正确工作的 GUI.BeginClip 之后的 Event.current.mousePosition。
        ///
        /// 注意：在画布区域内获取鼠标坐标请使用此属性。
        /// </summary>
        public Vec2 CorrectedMousePosition
        {
            get
            {
                var mouse = Event.current.mousePosition;
                return new Vec2(mouse.x - _graphAreaRect.x, mouse.y - _graphAreaRect.y);
            }
        }

        /// <summary>鼠标是否在画布区域内</summary>
        public bool IsMouseInGraphArea
        {
            get
            {
                var mouse = Event.current.mousePosition;
                return _graphAreaRect.Contains(mouse);
            }
        }

        /// <summary>画布区域的屏幕偏移（用于渲染矩阵）</summary>
        public Vector2 ScreenOffset => _graphAreaRect.position;

        /// <summary>画布区域矩形</summary>
        public Rect GraphAreaRect => _graphAreaRect;
    }
}
