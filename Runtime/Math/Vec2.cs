#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace NodeGraph.Math
{
    /// <summary>
    /// 二维向量，用于画布坐标、节点位置和尺寸等。
    /// 零引擎依赖，引擎适配层通过隐式转换对接 Unity.Vector2 / Godot.Vector2 等。
    /// </summary>
    public struct Vec2 : IEquatable<Vec2>
    {
        public float X;
        public float Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        // ── 常用常量 ──

        public static Vec2 Zero => new Vec2(0f, 0f);
        public static Vec2 One => new Vec2(1f, 1f);
        public static Vec2 Up => new Vec2(0f, 1f);
        public static Vec2 Down => new Vec2(0f, -1f);
        public static Vec2 Left => new Vec2(-1f, 0f);
        public static Vec2 Right => new Vec2(1f, 0f);

        // ── 长度与距离 ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Length() => MathF.Sqrt(X * X + Y * Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float LengthSquared() => X * X + Y * Y;

        public Vec2 Normalized()
        {
            float len = Length();
            if (len < 1e-6f) return Zero;
            float inv = 1f / len;
            return new Vec2(X * inv, Y * inv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSquared(Vec2 a, Vec2 b) => (a - b).LengthSquared();

        // ── 插值与点积 ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 Lerp(Vec2 a, Vec2 b, float t) =>
            new Vec2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;

        // ── 分量级 Min / Max ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 Min(Vec2 a, Vec2 b) =>
            new Vec2(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 Max(Vec2 a, Vec2 b) =>
            new Vec2(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y));

        // ── 运算符 ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator *(Vec2 v, float s) => new Vec2(v.X * s, v.Y * s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator *(float s, Vec2 v) => new Vec2(v.X * s, v.Y * s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator /(Vec2 v, float s) => new Vec2(v.X / s, v.Y / s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator -(Vec2 v) => new Vec2(-v.X, -v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vec2 a, Vec2 b) => a.X == b.X && a.Y == b.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vec2 a, Vec2 b) => a.X != b.X || a.Y != b.Y;

        // ── IEquatable / Object ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vec2 other) => X == other.X && Y == other.Y;

        public override bool Equals(object? obj) => obj is Vec2 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"({X:F2}, {Y:F2})";
    }
}
