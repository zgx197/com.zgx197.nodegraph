#nullable enable
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace NodeGraph.Math
{
    /// <summary>
    /// RGBA 颜色（各分量 0~1），用于节点颜色、端口颜色、装饰元素颜色等。
    /// </summary>
    public struct Color4 : IEquatable<Color4>
    {
        public float R;
        public float G;
        public float B;
        public float A;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color4(float r, float g, float b, float a = 1f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        // ── 常用常量 ──

        public static Color4 White => new Color4(1f, 1f, 1f, 1f);
        public static Color4 Black => new Color4(0f, 0f, 0f, 1f);
        public static Color4 Clear => new Color4(0f, 0f, 0f, 0f);
        public static Color4 Red => new Color4(1f, 0f, 0f, 1f);
        public static Color4 Green => new Color4(0f, 1f, 0f, 1f);
        public static Color4 Blue => new Color4(0f, 0f, 1f, 1f);
        public static Color4 Yellow => new Color4(1f, 1f, 0f, 1f);
        public static Color4 Gray => new Color4(0.5f, 0.5f, 0.5f, 1f);

        // ── 从 Hex 字符串构造 ──

        /// <summary>
        /// 从十六进制字符串构造颜色，支持 "#RGB" / "#RGBA" / "#RRGGBB" / "#RRGGBBAA" 格式。
        /// </summary>
        public static Color4 FromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Black;

            ReadOnlySpan<char> span = hex.AsSpan();
            if (span[0] == '#') span = span.Slice(1);

            switch (span.Length)
            {
                case 3: // RGB
                {
                    float r = ParseHexChar(span[0]) / 15f;
                    float g = ParseHexChar(span[1]) / 15f;
                    float b = ParseHexChar(span[2]) / 15f;
                    return new Color4(r, g, b, 1f);
                }
                case 4: // RGBA
                {
                    float r = ParseHexChar(span[0]) / 15f;
                    float g = ParseHexChar(span[1]) / 15f;
                    float b = ParseHexChar(span[2]) / 15f;
                    float a = ParseHexChar(span[3]) / 15f;
                    return new Color4(r, g, b, a);
                }
                case 6: // RRGGBB
                {
                    float r = ParseHexByte(span[0], span[1]) / 255f;
                    float g = ParseHexByte(span[2], span[3]) / 255f;
                    float b = ParseHexByte(span[4], span[5]) / 255f;
                    return new Color4(r, g, b, 1f);
                }
                case 8: // RRGGBBAA
                {
                    float r = ParseHexByte(span[0], span[1]) / 255f;
                    float g = ParseHexByte(span[2], span[3]) / 255f;
                    float b = ParseHexByte(span[4], span[5]) / 255f;
                    float a = ParseHexByte(span[6], span[7]) / 255f;
                    return new Color4(r, g, b, a);
                }
                default:
                    return Black;
            }
        }

        // ── 变换 ──

        /// <summary>返回具有指定透明度的新颜色</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color4 WithAlpha(float alpha) => new Color4(R, G, B, alpha);

        /// <summary>线性插值</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 Lerp(Color4 a, Color4 b, float t) =>
            new Color4(
                a.R + (b.R - a.R) * t,
                a.G + (b.G - a.G) * t,
                a.B + (b.B - a.B) * t,
                a.A + (b.A - a.A) * t);

        // ── 端口类型预定义调色板 ──

        /// <summary>端口类型预定义颜色</summary>
        public static class Palette
        {
            // 端口 Kind 颜色
            public static Color4 ControlPort => FromHex("#FFFFFF");  // 白色 - 控制流
            public static Color4 EventPort => FromHex("#FF9933");    // 橙色 - 事件
            public static Color4 DataPort => FromHex("#6BB5FF");     // 蓝色 - 数据
            
            // 数据类型颜色（向后兼容）
            public static Color4 FloatPort => FromHex("#84E084");
            public static Color4 IntPort => FromHex("#6BB5FF");
            public static Color4 StringPort => FromHex("#F5A623");
            public static Color4 BoolPort => FromHex("#E05252");
            public static Color4 EntityPort => FromHex("#C899E5");
            public static Color4 AnyPort => FromHex("#AAAAAA");
        }

        // ── 运算符 ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Color4 a, Color4 b) =>
            a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Color4 a, Color4 b) => !(a == b);

        // ── IEquatable / Object ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Color4 other) => this == other;

        public override bool Equals(object? obj) => obj is Color4 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(R, G, B, A);

        public override string ToString() => $"Color4({R:F2}, {G:F2}, {B:F2}, {A:F2})";

        /// <summary>输出为 #RRGGBB 或 #RRGGBBAA 格式</summary>
        public string ToHex(bool includeAlpha = false)
        {
            int r = (int)(MathF.Round(R * 255f));
            int g = (int)(MathF.Round(G * 255f));
            int b = (int)(MathF.Round(B * 255f));

            if (includeAlpha)
            {
                int a = (int)(MathF.Round(A * 255f));
                return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
            }
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        // ── 内部辅助 ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ParseHexChar(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ParseHexByte(char hi, char lo) =>
            ParseHexChar(hi) * 16 + ParseHexChar(lo);
    }
}
