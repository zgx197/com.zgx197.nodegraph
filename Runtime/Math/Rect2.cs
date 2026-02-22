#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NodeGraph.Math
{
    /// <summary>
    /// 轴对齐矩形，用于节点包围盒、视口、框选区域等。
    /// </summary>
    public struct Rect2 : IEquatable<Rect2>
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Rect2(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Rect2(Vec2 position, Vec2 size)
        {
            X = position.X;
            Y = position.Y;
            Width = size.X;
            Height = size.Y;
        }

        // ── 属性 ──

        public Vec2 Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Vec2(X, Y);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { X = value.X; Y = value.Y; }
        }

        public Vec2 Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Vec2(Width, Height);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { Width = value.X; Height = value.Y; }
        }

        public Vec2 Center
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Vec2(X + Width * 0.5f, Y + Height * 0.5f);
        }

        public Vec2 TopLeft
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Vec2(X, Y);
        }

        public Vec2 BottomRight
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Vec2(X + Width, Y + Height);
        }

        public float Left
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => X;
        }

        public float Right
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => X + Width;
        }

        public float Top
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Y;
        }

        public float Bottom
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Y + Height;
        }

        // ── 查询 ──

        /// <summary>判断点是否在矩形内</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Vec2 point) =>
            point.X >= X && point.X <= X + Width &&
            point.Y >= Y && point.Y <= Y + Height;

        /// <summary>判断两个矩形是否重叠</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(Rect2 other) =>
            X < other.X + other.Width && X + Width > other.X &&
            Y < other.Y + other.Height && Y + Height > other.Y;

        // ── 变换 ──

        /// <summary>向外扩展指定 padding</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Rect2 Expand(float padding) =>
            new Rect2(X - padding, Y - padding, Width + padding * 2f, Height + padding * 2f);

        /// <summary>计算能包含所有矩形的最小包围矩形</summary>
        public static Rect2 Encapsulate(IEnumerable<Rect2> rects)
        {
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            bool any = false;

            foreach (var r in rects)
            {
                any = true;
                if (r.X < minX) minX = r.X;
                if (r.Y < minY) minY = r.Y;
                if (r.X + r.Width > maxX) maxX = r.X + r.Width;
                if (r.Y + r.Height > maxY) maxY = r.Y + r.Height;
            }

            if (!any) return new Rect2(0, 0, 0, 0);
            return new Rect2(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>返回两个矩形的交集，若不相交则返回零矩形</summary>
        public static Rect2 Intersect(Rect2 a, Rect2 b)
        {
            float x1 = MathF.Max(a.X, b.X);
            float y1 = MathF.Max(a.Y, b.Y);
            float x2 = MathF.Min(a.Right, b.Right);
            float y2 = MathF.Min(a.Bottom, b.Bottom);

            if (x2 <= x1 || y2 <= y1) return new Rect2(0, 0, 0, 0);
            return new Rect2(x1, y1, x2 - x1, y2 - y1);
        }

        // ── 运算符 ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Rect2 a, Rect2 b) =>
            a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Rect2 a, Rect2 b) => !(a == b);

        // ── IEquatable / Object ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Rect2 other) => this == other;

        public override bool Equals(object? obj) => obj is Rect2 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public override string ToString() => $"Rect2({X:F1}, {Y:F1}, {Width:F1}, {Height:F1})";
    }
}
