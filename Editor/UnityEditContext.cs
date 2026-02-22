#nullable enable
using UnityEngine;
using UnityEditor;
using NodeGraph.Abstraction;
using NodeGraph.Math;

namespace NodeGraph.Unity
{
    /// <summary>
    /// Unity EditorGUI 实现的编辑控件上下文。
    /// IMGUI 风格：传入当前值，返回修改后的值。
    /// </summary>
    public class UnityEditContext : IEditContext
    {
        private bool _hasChanged;
        private Rect _availableRect;
        private float _currentY;
        private float _lineHeight;
        private float _spacing = 2f;

        /// <summary>开始一帧的编辑（在绘制节点内容前调用）</summary>
        public void Begin(Rect2 availableRect)
        {
            _availableRect = new Rect(availableRect.X, availableRect.Y,
                availableRect.Width, availableRect.Height);
            _currentY = availableRect.Y;
            _lineHeight = EditorGUIUtility.singleLineHeight;
            _hasChanged = false;
        }

        // ── 基础控件 ──

        public float FloatField(string label, float value)
        {
            var rect = GetNextRect();
            EditorGUI.BeginChangeCheck();
            float result = EditorGUI.FloatField(rect, label, value);
            if (EditorGUI.EndChangeCheck()) _hasChanged = true;
            return result;
        }

        public int IntField(string label, int value)
        {
            var rect = GetNextRect();
            EditorGUI.BeginChangeCheck();
            int result = EditorGUI.IntField(rect, label, value);
            if (EditorGUI.EndChangeCheck()) _hasChanged = true;
            return result;
        }

        public string TextField(string label, string value)
        {
            var rect = GetNextRect();
            EditorGUI.BeginChangeCheck();
            string result = EditorGUI.TextField(rect, label, value);
            if (EditorGUI.EndChangeCheck()) _hasChanged = true;
            return result;
        }

        public bool Toggle(string label, bool value)
        {
            var rect = GetNextRect();
            EditorGUI.BeginChangeCheck();
            bool result = EditorGUI.Toggle(rect, label, value);
            if (EditorGUI.EndChangeCheck()) _hasChanged = true;
            return result;
        }

        public float Slider(string label, float value, float min, float max)
        {
            var rect = GetNextRect();
            EditorGUI.BeginChangeCheck();
            float result = EditorGUI.Slider(rect, label, value, min, max);
            if (EditorGUI.EndChangeCheck()) _hasChanged = true;
            return result;
        }

        public int Popup(string label, int selectedIndex, string[] options)
        {
            var rect = GetNextRect();
            EditorGUI.BeginChangeCheck();
            int result = EditorGUI.Popup(rect, label, selectedIndex, options);
            if (EditorGUI.EndChangeCheck()) _hasChanged = true;
            return result;
        }

        public Color4 ColorField(string label, Color4 value)
        {
            var rect = GetNextRect();
            EditorGUI.BeginChangeCheck();
            var result = EditorGUI.ColorField(rect, label, value.ToUnity());
            if (EditorGUI.EndChangeCheck()) _hasChanged = true;
            return result.ToNodeGraph();
        }

        // ── 布局辅助 ──

        public void Label(string text)
        {
            var rect = GetNextRect();
            EditorGUI.LabelField(rect, text);
        }

        public void Space(float pixels)
        {
            _currentY += pixels;
        }

        public void BeginHorizontal()
        {
            // IMGUI 模式下简化处理（不实际嵌套布局）
        }

        public void EndHorizontal()
        {
        }

        public bool Foldout(string label, bool expanded)
        {
            var rect = GetNextRect();
            return EditorGUI.Foldout(rect, expanded, label, true);
        }

        public void Separator()
        {
            var rect = new Rect(_availableRect.x, _currentY, _availableRect.width, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            _currentY += 4f;
        }

        // ── 状态查询 ──

        public bool HasChanged => _hasChanged;

        // ── 可用区域 ──

        public Rect2 AvailableRect => _availableRect.ToNodeGraph();

        // ── 内部辅助 ──

        private Rect GetNextRect()
        {
            var rect = new Rect(_availableRect.x, _currentY, _availableRect.width, _lineHeight);
            _currentY += _lineHeight + _spacing;
            return rect;
        }
    }
}
