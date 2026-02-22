#nullable enable
using UnityEngine;
using UnityEditor;
using NodeGraph.Abstraction;
using NodeGraph.Math;

namespace NodeGraph.Unity
{
    /// <summary>
    /// Unity 实现的文字测量器。使用 EditorStyles 计算文字像素尺寸。
    /// </summary>
    public class UnityTextMeasurer : ITextMeasurer
    {
        public Vec2 MeasureText(string text, float fontSize)
        {
            var style = new GUIStyle(EditorStyles.label) { fontSize = (int)fontSize };
            var size = style.CalcSize(new GUIContent(text));
            return new Vec2(size.x, size.y);
        }
    }
}
