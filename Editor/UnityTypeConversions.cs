#nullable enable
using UnityEngine;
using NodeGraph.Math;

namespace NodeGraph.Unity
{
    /// <summary>
    /// NodeGraph 自定义数学类型与 Unity 原生类型之间的转换扩展方法。
    /// </summary>
    public static class UnityTypeConversions
    {
        // ── Vec2 ↔ Vector2 ──

        public static Vector2 ToUnity(this Vec2 v) => new Vector2(v.X, v.Y);
        public static Vec2 ToNodeGraph(this Vector2 v) => new Vec2(v.x, v.y);

        // ── Rect2 ↔ Rect ──

        public static Rect ToUnity(this Rect2 r) => new Rect(r.X, r.Y, r.Width, r.Height);
        public static Rect2 ToNodeGraph(this Rect r) => new Rect2(r.x, r.y, r.width, r.height);

        // ── Color4 ↔ Color ──

        public static Color ToUnity(this Color4 c) => new Color(c.R, c.G, c.B, c.A);
        public static Color4 ToNodeGraph(this Color c) => new Color4(c.r, c.g, c.b, c.a);

        // ── Vec2 ↔ Vector2 隐式转换辅助 ──

        public static Vector2 V2(this Vec2 v) => new Vector2(v.X, v.Y);
        public static Vec2 NG(this Vector2 v) => new Vec2(v.x, v.y);
    }
}
