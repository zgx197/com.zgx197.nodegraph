#nullable enable
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>
    /// [扩展点] 节点图视觉主题配置。纯 C#，跨引擎通用。
    /// </summary>
    /// <remarks>
    /// 通常不需自建实例，直接使用内置预设：
    /// - <see cref="Dark"/>  — 暗色主题（默认）
    /// - <see cref="Light"/> — 浅色主题（亮色 IDE 环境）
    /// 业务层需自定义主题时，可以在 <see cref="BlueprintProfile.Theme"/> 中替换。
    /// </remarks>
    public sealed class NodeVisualTheme
    {
        // ── 节点整体 ──

        /// <summary>节点最小宽度</summary>
        public float NodeMinWidth { get; set; } = 160f;

        /// <summary>节点圆角半径</summary>
        public float NodeCornerRadius { get; set; } = 8f;

        /// <summary>节点主体背景色</summary>
        public Color4 NodeBodyColor { get; set; } = new Color4(0.18f, 0.18f, 0.18f, 0.96f);

        /// <summary>节点主体描边色</summary>
        public Color4 NodeBorderColor { get; set; } = new Color4(0.10f, 0.10f, 0.10f, 1f);

        /// <summary>节点主体描边宽度</summary>
        public float NodeBorderWidth { get; set; } = 1f;

        // ── 节点阴影 ──

        /// <summary>阴影偏移</summary>
        public Vec2 ShadowOffset { get; set; } = new Vec2(3f, 4f);

        /// <summary>阴影层数（层数越多越柔和）</summary>
        public int ShadowLayers { get; set; } = 4;

        /// <summary>阴影每层扩展量</summary>
        public float ShadowExpand { get; set; } = 2f;

        /// <summary>阴影基础透明度</summary>
        public float ShadowBaseAlpha { get; set; } = 0.15f;

        /// <summary>阴影颜色</summary>
        public Color4 ShadowColor { get; set; } = new Color4(0f, 0f, 0f, 1f);

        // ── 标题栏 ──

        /// <summary>标题栏高度</summary>
        public float TitleBarHeight { get; set; } = 26f;

        /// <summary>描述条高度（节点设置了 Description 时，标题栏下方额外的区域）</summary>
        public float DescriptionBarHeight { get; set; } = 17f;

        /// <summary>描述文字字体大小</summary>
        public float DescriptionFontSize { get; set; } = 10f;

        /// <summary>描述文字颜色（偏灰，视觉权重低于标题）</summary>
        public Color4 DescriptionTextColor { get; set; } = new Color4(0.62f, 0.62f, 0.62f, 1f);

        /// <summary>标题字体大小</summary>
        public float TitleFontSize { get; set; } = 13f;

        /// <summary>标题文字颜色</summary>
        public Color4 TitleTextColor { get; set; } = Color4.White;

        /// <summary>标题文字左边距</summary>
        public float TitlePaddingLeft { get; set; } = 10f;

        /// <summary>标题栏与主体之间的分隔线颜色</summary>
        public Color4 TitleSeparatorColor { get; set; } = new Color4(0f, 0f, 0f, 0.4f);

        /// <summary>标题色变暗系数（主体部分的标题色偏暗，制造渐变感）</summary>
        public float TitleDarkenFactor { get; set; } = 0.6f;

        // ── 端口 ──

        /// <summary>端口半径</summary>
        public float PortRadius { get; set; } = 6f;

        /// <summary>端口间距（Y方向）</summary>
        public float PortSpacing { get; set; } = 24f;

        /// <summary>端口空心环宽度（未连接时）</summary>
        public float PortHollowWidth { get; set; } = 2f;

        /// <summary>端口名称字体大小</summary>
        public float PortFontSize { get; set; } = 11f;

        /// <summary>端口名称颜色</summary>
        public Color4 PortTextColor { get; set; } = new Color4(0.82f, 0.82f, 0.82f, 1f);

        /// <summary>端口名称与端口圆心的间距</summary>
        public float PortTextGap { get; set; } = 6f;

        /// <summary>端口外圈颜色（装饰）</summary>
        public Color4 PortOuterRingColor { get; set; } = new Color4(0.08f, 0.08f, 0.08f, 1f);

        /// <summary>端口外圈宽度</summary>
        public float PortOuterRingWidth { get; set; } = 1.5f;

        /// <summary>端口圆心从节点边缘向内缩进的距离（使端口圆圈完全在节点矩形内）</summary>
        public float PortInset { get; set; } = 10f;

        // ── 连线 ──

        /// <summary>连线宽度（控制流 / 事件流）</summary>
        public float EdgeWidth { get; set; } = 2.5f;

        /// <summary>数据边宽度（比控制流边细，视觉上区分"数据"与"执行"）</summary>
        public float DataEdgeWidth { get; set; } = 1.5f;

        /// <summary>选中连线宽度</summary>
        public float EdgeSelectedWidth { get; set; } = 4f;

        /// <summary>选中连线颜色</summary>
        public Color4 EdgeSelectedColor { get; set; } = new Color4(1f, 0.85f, 0.3f, 1f);

        /// <summary>未连接时连线的默认颜色（当无法确定端口色时）</summary>
        public Color4 EdgeDefaultColor { get; set; } = new Color4(0.6f, 0.6f, 0.6f, 0.9f);

        // ── 选中效果 ──

        /// <summary>主选中发光颜色</summary>
        public Color4 SelectionPrimaryColor { get; set; } = new Color4(1f, 0.85f, 0.3f, 1f);

        /// <summary>次选中发光颜色</summary>
        public Color4 SelectionSecondaryColor { get; set; } = new Color4(0.4f, 0.7f, 1f, 0.9f);

        /// <summary>发光层数</summary>
        public int SelectionGlowLayers { get; set; } = 3;

        /// <summary>发光每层扩展量</summary>
        public float SelectionGlowSpread { get; set; } = 2f;

        /// <summary>发光边框宽度</summary>
        public float SelectionBorderWidth { get; set; } = 2f;

        // ── 网格 ──

        /// <summary>小网格大小</summary>
        public float GridSmallSize { get; set; } = 20f;

        /// <summary>大网格倍数</summary>
        public int GridLargeMultiplier { get; set; } = 5;

        /// <summary>背景色</summary>
        public Color4 GridBackgroundColor { get; set; } = new Color4(0.12f, 0.12f, 0.12f, 1f);

        /// <summary>小网格线颜色</summary>
        public Color4 GridSmallLineColor { get; set; } = new Color4(1f, 1f, 1f, 0.04f);

        /// <summary>大网格线颜色</summary>
        public Color4 GridLargeLineColor { get; set; } = new Color4(1f, 1f, 1f, 0.08f);

        // ── 小地图 ──

        /// <summary>小地图背景色</summary>
        public Color4 MiniMapBgColor { get; set; } = new Color4(0.08f, 0.08f, 0.08f, 0.85f);

        /// <summary>小地图边框色</summary>
        public Color4 MiniMapBorderColor { get; set; } = new Color4(0.3f, 0.3f, 0.3f, 0.8f);

        /// <summary>小地图视口矩形颜色</summary>
        public Color4 MiniMapViewportColor { get; set; } = new Color4(1f, 1f, 1f, 0.5f);

        // ── 内容区域 ──

        /// <summary>内容区内边距</summary>
        public float ContentPadding { get; set; } = 12f;

        /// <summary>内容区与端口区之间的分隔线颜色</summary>
        public Color4 ContentSeparatorColor { get; set; } = new Color4(1f, 1f, 1f, 0.06f);

        // ── 预设主题实例 ──

        /// <summary>默认暗色主题（单例，创建一次后缓存）</summary>
        public static readonly NodeVisualTheme Dark = new NodeVisualTheme();

        /// <summary>浅色主题（适合亮色 IDE 环境）</summary>
        public static readonly NodeVisualTheme Light = new NodeVisualTheme
        {
            NodeBodyColor        = new Color4(0.92f, 0.92f, 0.92f, 0.97f),
            NodeBorderColor      = new Color4(0.70f, 0.70f, 0.70f, 1f),
            TitleTextColor       = new Color4(0.10f, 0.10f, 0.10f, 1f),
            TitleSeparatorColor  = new Color4(0f,    0f,    0f,    0.15f),
            PortTextColor        = new Color4(0.18f, 0.18f, 0.18f, 1f),
            GridBackgroundColor  = new Color4(0.85f, 0.85f, 0.85f, 1f),
            GridSmallLineColor   = new Color4(0f,    0f,    0f,    0.06f),
            GridLargeLineColor   = new Color4(0f,    0f,    0f,    0.12f),
            MiniMapBgColor       = new Color4(0.80f, 0.80f, 0.80f, 0.90f),
            MiniMapBorderColor   = new Color4(0.50f, 0.50f, 0.50f, 0.80f),
            ShadowColor          = new Color4(0.50f, 0.50f, 0.50f, 1f),
        };
    }
}
