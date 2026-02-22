#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NodeGraph.Abstraction;
using NodeGraph.Core;
using NodeGraph.Math;
using NodeGraph.View;

namespace NodeGraph.Unity
{
    /// <summary>
    /// Unity 引擎原生渲染器。消费 GraphFrame 数据，使用纯矢量 IMGUI/Handles 绘制。
    ///
    /// ── Zero-Matrix 模式 ──
    /// 不设置 GUI.matrix，所有画布坐标通过 C2W() 手动转换为窗口坐标后再绘制。
    ///
    /// 原因：Handles API（DrawSolidDisc、DrawBezier 等）在 GUI.matrix 包含缩放分量时，
    /// 与 GUI.BeginClip / EditorWindow 的坐标系交互存在不可预测的偏移，导致端口圆圈的
    /// 渲染位置与命中检测位置不匹配。Zero-Matrix 完全消除对 GUI.matrix 的依赖，
    /// 保证 Handles、EditorGUI、GUI.Label 全部使用相同的窗口坐标系。
    ///
    /// 坐标转换公式：
    ///   windowPos  = canvasPos  * zoom + pan + screenOffset
    ///   windowSize = canvasSize * zoom
    /// 其中 screenOffset = graphRect.position（画布区域在 EditorWindow 中的偏移）
    /// </summary>
    public class UnityGraphRenderer
    {
        private readonly Dictionary<string, INodeContentRenderer> _contentRenderers;
        private readonly IEdgeLabelRenderer? _edgeLabelRenderer;

        // ── Zero-Matrix 坐标变换状态（每帧在 Render 开头设置）──

        /// <summary>当前缩放级别</summary>
        private float _zoom = 1f;
        /// <summary>画布平移 X（frame.PanOffset.X）</summary>
        private float _panX;
        /// <summary>画布平移 Y（frame.PanOffset.Y）</summary>
        private float _panY;
        /// <summary>画布区域在窗口中的 X 偏移（graphRect.x）</summary>
        private float _offsetX;
        /// <summary>画布区域在窗口中的 Y 偏移（graphRect.y，通常等于 ToolbarHeight）</summary>
        private float _offsetY;

        /// <summary>当前帧可见的画布区域（用于视口裁剪），含 margin</summary>
        private Rect _visibleCanvasRect;

        // ── GUIStyle 缓存（避免每帧每节点 new GUIStyle 产生 GC 压力）──
        private GUIStyle? _titleStyle;
        private GUIStyle? _portNameStyle;
        private GUIStyle? _summaryStyle;
        private GUIStyle? _edgeLabelStyle;

        public UnityGraphRenderer(
            Dictionary<string, INodeContentRenderer> contentRenderers,
            IEdgeLabelRenderer? edgeLabelRenderer = null)
        {
            _contentRenderers = contentRenderers;
            _edgeLabelRenderer = edgeLabelRenderer;
        }

        // ══════════════════════════════════════
        //  Zero-Matrix 坐标转换辅助方法
        //  所有 Canvas→Window 转换在此完成，
        //  不依赖 GUI.matrix / GUI.BeginClip
        // ══════════════════════════════════════

        /// <summary>画布点 → 窗口点（用于 Handles 绘制、GUI.Label 定位等）</summary>
        private Vector3 C2W(float cx, float cy) =>
            new Vector3(cx * _zoom + _panX + _offsetX, cy * _zoom + _panY + _offsetY, 0);

        /// <summary>画布点 → 窗口点</summary>
        private Vector3 C2W(Vec2 p) => C2W(p.X, p.Y);

        /// <summary>画布矩形 → 窗口矩形（位置和尺寸都经过缩放）</summary>
        private Rect C2WRect(Rect2 r) => new Rect(
            r.X * _zoom + _panX + _offsetX,
            r.Y * _zoom + _panY + _offsetY,
            r.Width * _zoom,
            r.Height * _zoom);

        /// <summary>画布矩形 → 窗口矩形（分量版本，避免临时 Rect2）</summary>
        private Rect C2WRect(float x, float y, float w, float h) => new Rect(
            x * _zoom + _panX + _offsetX,
            y * _zoom + _panY + _offsetY,
            w * _zoom,
            h * _zoom);

        /// <summary>画布尺寸 → 窗口尺寸（半径、线宽等标量乘以缩放）</summary>
        private float S(float canvasSize) => canvasSize * _zoom;

        /// <summary>缩放后的字号（最小 1，避免零字号异常）</summary>
        private int ScaledFontSize(float baseFontSize) => Mathf.Max(1, (int)(baseFontSize * _zoom));

        // ══════════════════════════════════════
        //  主渲染入口
        // ══════════════════════════════════════

        /// <summary>渲染完整的 GraphFrame（Zero-Matrix 模式，不设置 GUI.matrix）</summary>
        /// <param name="screenOffset">画布区域在窗口中的偏移（graphRect.position）</param>
        public void Render(GraphFrame frame, NodeVisualTheme theme, Rect viewport,
            IEditContext? editCtx = null, Vector2 screenOffset = default)
        {
            // ── 存储 Zero-Matrix 坐标变换参数 ──
            _zoom = frame.ZoomLevel;
            _panX = frame.PanOffset.X;
            _panY = frame.PanOffset.Y;
            _offsetX = screenOffset.x;
            _offsetY = screenOffset.y;

            // 计算可见的画布区域（带 margin 容错，避免端口/阴影被裁掉）
            float invZoom = _zoom > 0.001f ? 1f / _zoom : 1f;
            float margin = 80f;
            _visibleCanvasRect = new Rect(
                -_panX * invZoom - margin,
                -_panY * invZoom - margin,
                viewport.width * invZoom + margin * 2f,
                viewport.height * invZoom + margin * 2f);

            // ── 初始化缓存的 GUIStyle（仅首次创建，后续复用）──
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
                _portNameStyle = new GUIStyle(EditorStyles.label);
                _summaryStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    normal = { textColor = new Color(0.75f, 0.75f, 0.75f) }
                };
                _edgeLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
                };
            }

            // Layer 0: 背景填充（直接窗口坐标）
            EditorGUI.DrawRect(
                new Rect(viewport.x + _offsetX, viewport.y + _offsetY,
                    viewport.width, viewport.height),
                frame.Background.BackgroundColor.ToUnity());

            // ── Zero-Matrix：不设置 GUI.matrix，所有绘制使用 C2W() 手动转换 ──

            // Layer 1: 背景网格线
            DrawBackgroundGrid(frame.Background);

            // Layer 2: 装饰层（注释 → 分组框/子图框，在节点下方）
            foreach (var deco in frame.Decorations)
                DrawDecoration(deco, theme);

            // Layer 3: 连线（视口裁剪）
            foreach (var edge in frame.Edges)
            {
                if (IsEdgeVisible(edge))
                    DrawEdge(edge, theme);
            }

            // Layer 4: 节点（视口裁剪，先阴影再主体）
            foreach (var node in frame.Nodes)
            {
                if (IsNodeVisible(node))
                    DrawNodeShadow(node, theme);
            }

            foreach (var node in frame.Nodes)
            {
                if (IsNodeVisible(node))
                    DrawNode(node, theme, editCtx);
            }

            // Layer 5: 覆盖层
            foreach (var overlay in frame.Overlays)
                DrawOverlay(overlay);

            // Layer 6: 小地图（已在窗口坐标，无需转换）
            if (frame.MiniMap != null)
                DrawMiniMap(frame.MiniMap);
        }

        // ══════════════════════════════════════
        //  背景
        // ══════════════════════════════════════

        private void DrawBackgroundGrid(BackgroundFrame bg)
        {
            // 小网格
            DrawGridLines(bg.VisibleRect, bg.SmallGridSize, bg.SmallLineColor);
            // 大网格
            DrawGridLines(bg.VisibleRect, bg.LargeGridSize, bg.LargeLineColor);
        }

        /// <summary>
        /// 绘制网格线。在画布空间遍历网格交点，通过 C2W 转换为窗口坐标后绘制。
        /// 线宽固定 1 像素（不随缩放变化，保持视觉一致性）。
        /// </summary>
        private void DrawGridLines(Rect2 viewport, float gridSize, Color4 color)
        {
            if (gridSize <= 0f) return;

            float startX = Mathf.Floor(viewport.X / gridSize) * gridSize;
            float startY = Mathf.Floor(viewport.Y / gridSize) * gridSize;

            // 网格线宽固定 1 屏幕像素（Handles.DrawAAPolyLine 线宽不受 GUI.matrix 影响）
            const float lineWidth = 1f;

            var oldColor = Handles.color;
            Handles.color = color.ToUnity();

            // 垂直线（画布 X 遍历，Y 端点经 C2W 转换）
            for (float x = startX; x <= viewport.Right; x += gridSize)
            {
                Handles.DrawAAPolyLine(lineWidth,
                    C2W(x, viewport.Top),
                    C2W(x, viewport.Bottom));
            }

            // 水平线
            for (float y = startY; y <= viewport.Bottom; y += gridSize)
            {
                Handles.DrawAAPolyLine(lineWidth,
                    C2W(viewport.Left, y),
                    C2W(viewport.Right, y));
            }

            Handles.color = oldColor;
        }

        // ══════════════════════════════════════
        //  装饰层（分组框 / 子图框 / 注释块）
        // ══════════════════════════════════════

        private void DrawDecoration(DecorationFrame deco, NodeVisualTheme theme)
        {
            var bounds = deco.Bounds;
            var bgRect = C2WRect(bounds.X, bounds.Y, bounds.Width, bounds.Height);

            // 背景
            EditorGUI.DrawRect(bgRect, deco.BackgroundColor.ToUnity());

            // 边框（四条线）
            float borderWidth = S(1f);
            var borderColor = deco.BorderColor.ToUnity();
            EditorGUI.DrawRect(new Rect(bgRect.x, bgRect.y, bgRect.width, borderWidth), borderColor);
            EditorGUI.DrawRect(new Rect(bgRect.x, bgRect.yMax - borderWidth, bgRect.width, borderWidth), borderColor);
            EditorGUI.DrawRect(new Rect(bgRect.x, bgRect.y, borderWidth, bgRect.height), borderColor);
            EditorGUI.DrawRect(new Rect(bgRect.xMax - borderWidth, bgRect.y, borderWidth, bgRect.height), borderColor);

            switch (deco.Kind)
            {
                case DecorationKind.Group:
                case DecorationKind.SubGraph:
                    DrawDecorationTitle(deco, bgRect, theme);
                    // 展开时绘制边界端口（定位到框边缘）
                    if (deco.BoundaryPorts != null)
                    {
                        foreach (var bp in deco.BoundaryPorts)
                            DrawPort(bp, theme);
                    }
                    break;
                case DecorationKind.Comment:
                    DrawCommentText(deco, bgRect);
                    break;
            }
        }

        private void DrawDecorationTitle(DecorationFrame deco, Rect bgRect, NodeVisualTheme theme)
        {
            if (string.IsNullOrEmpty(deco.Title)) return;

            float titleBarH = S(deco.TitleBarHeight);

            // 标题栏背景（稍深于容器背景）
            var titleRect = new Rect(bgRect.x, bgRect.y, bgRect.width, titleBarH);
            var titleBgColor = new Color(
                deco.BackgroundColor.R * 0.7f,
                deco.BackgroundColor.G * 0.7f,
                deco.BackgroundColor.B * 0.7f,
                Mathf.Min(deco.BackgroundColor.A + 0.2f, 1f));
            EditorGUI.DrawRect(titleRect, titleBgColor);

            // 折叠按钮图标（SubGraph 专用）
            string prefix = "";
            if (deco.ShowCollapseButton)
                prefix = deco.IsCollapsed ? "▶ " : "▼ ";

            // 标题文字
            var labelRect = new Rect(titleRect.x + S(6f), titleRect.y, titleRect.width - S(12f), titleBarH);
            _titleStyle!.fontSize = ScaledFontSize(theme.TitleFontSize - 1);
            _titleStyle.normal.textColor = Color.white;
            GUI.Label(labelRect, prefix + deco.Title, _titleStyle);
        }

        private void DrawCommentText(DecorationFrame deco, Rect bgRect)
        {
            if (string.IsNullOrEmpty(deco.Text)) return;

            float padding = S(6f);
            var textRect = new Rect(bgRect.x + padding, bgRect.y + padding,
                bgRect.width - padding * 2f, bgRect.height - padding * 2f);

            if (_summaryStyle == null) return;
            _summaryStyle.fontSize = ScaledFontSize(deco.FontSize);
            _summaryStyle.normal.textColor = deco.TextColor.ToUnity();
            _summaryStyle.wordWrap = true;
            GUI.Label(textRect, deco.Text, _summaryStyle);
        }

        // ══════════════════════════════════════
        //  节点
        // ══════════════════════════════════════

        /// <summary>节点阴影（高斯模糊风格：指数衰减 alpha + 矢量圆角矩形多层叠加）</summary>
        private void DrawNodeShadow(NodeFrame node, NodeVisualTheme t)
        {
            var bounds = node.Bounds;
            int layers = Mathf.Max(t.ShadowLayers, 3);
            for (int i = layers; i >= 1; i--)
            {
                float ratio = (float)i / layers;
                float expand = t.ShadowExpand * i;
                // 指数衰减：外层 alpha 快速降低，内层较浓
                float alpha = t.ShadowBaseAlpha * Mathf.Exp(-2.5f * ratio);
                var shadowRect = C2WRect(
                    bounds.X + t.ShadowOffset.X - expand,
                    bounds.Y + t.ShadowOffset.Y - expand,
                    bounds.Width + expand * 2f,
                    bounds.Height + expand * 2f);
                float shadowR = Mathf.Max(S(4f), S(t.NodeCornerRadius + expand));
                var shadowColor = t.ShadowColor.WithAlpha(alpha).ToUnity();
                DrawFilledRoundedRect(shadowRect, shadowR, shadowColor);
            }
        }

        /// <summary>节点主体绘制（矢量圆角矩形 + 标题渐变 + 选中发光）</summary>
        private void DrawNode(NodeFrame node, NodeVisualTheme t, IEditContext? editCtx)
        {
            var bounds = node.Bounds;
            int cornerPx = Mathf.Max(2, (int)(t.NodeCornerRadius));

            // 1. 整体节点形状（标题色圆角）→ 顶部圆角由此定义
            var titleColor = node.TitleColor.ToUnity();
            float windowR = S(t.NodeCornerRadius);
            DrawFilledRoundedRect(C2WRect(bounds), windowR, titleColor);

            // 2. 主体区域（标题栏下方）→ 圆角，底部圆角由此定义
            var bodyColor = t.NodeBodyColor.ToUnity();
            float bodyY = bounds.Y + t.TitleBarHeight;
            float bodyH = bounds.Height - t.TitleBarHeight;
            DrawFilledRoundedRect(
                C2WRect(bounds.X, bodyY, bounds.Width, bodyH),
                windowR, bodyColor);
            // 覆盖主体区域顶部的多余圆角，使标题→主体交界处为直线
            float overlapH = Mathf.Min(cornerPx + 2f, bodyH * 0.5f);
            EditorGUI.DrawRect(C2WRect(bounds.X, bodyY, bounds.Width, overlapH), bodyColor);

            // 3. 标题渐变过渡（从标题色渐变到主体色，4条色带）
            {
                int gradientSteps = 4;
                float gradientH = 6f; // 画布空间渐变高度
                float stepH = gradientH / gradientSteps;
                float startY = bounds.Y + t.TitleBarHeight;
                for (int i = 0; i < gradientSteps; i++)
                {
                    float ratio = (float)(i + 1) / gradientSteps;
                    var blended = Color.Lerp(titleColor, bodyColor, ratio);
                    EditorGUI.DrawRect(C2WRect(bounds.X, startY + stepH * i, bounds.Width, stepH + 0.5f), blended);
                }
            }

            // 4. 标题/主体分隔线
            DrawLine(
                new Vec2(bounds.X, bounds.Y + t.TitleBarHeight),
                new Vec2(bounds.Right, bounds.Y + t.TitleBarHeight),
                t.TitleSeparatorColor, 1f);

            // 5. 标题文字（折叠的子图节点在标题前加 ▶ 图标）
            _titleStyle!.fontSize = ScaledFontSize(t.TitleFontSize);
            _titleStyle.normal.textColor = t.TitleTextColor.ToUnity();
            string titlePrefix = node.IsCollapsedSubGraph ? "▶ " : "";
            GUI.Label(C2WRect(
                bounds.X + t.TitlePaddingLeft,
                bounds.Y,
                bounds.Width - t.TitlePaddingLeft * 2f,
                t.TitleBarHeight), titlePrefix + node.TitleText, _titleStyle);

            // 5b. 描述条（位于标题栏下方，端口区域上方）
            if (!string.IsNullOrEmpty(node.Description))
            {
                float descTop = bounds.Y + t.TitleBarHeight;
                float descH = t.DescriptionBarHeight;

                // 细分隔线
                DrawLine(
                    new Vec2(bounds.X + t.ContentPadding, descTop),
                    new Vec2(bounds.Right - t.ContentPadding, descTop),
                    t.TitleSeparatorColor, 0.5f);

                // 描述文字
                if (_summaryStyle != null)
                {
                    _summaryStyle.fontSize = ScaledFontSize(t.DescriptionFontSize);
                    _summaryStyle.normal.textColor = t.DescriptionTextColor.ToUnity();
                    _summaryStyle.wordWrap = false;
                    _summaryStyle.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(C2WRect(
                        bounds.X + t.TitlePaddingLeft,
                        descTop,
                        bounds.Width - t.TitlePaddingLeft * 2f,
                        descH), node.Description, _summaryStyle);
                    _summaryStyle.alignment = TextAnchor.UpperLeft;
                }
            }

            // 6. 节点外边框（矢量圆角边框）
            DrawRoundedBorder(bounds, t.NodeBorderColor, t.NodeBorderWidth, cornerPx);

            // 7. 端口
            foreach (var port in node.Ports)
                DrawPort(port, t);

            // 8. 节点内容
            if (node.Content != null)
                DrawNodeContent(node.Content, t, editCtx);

            // 9. 选中外发光（矢量圆角边框多层叠加）
            if (node.Selected)
            {
                var glowColor = node.IsPrimary ? t.SelectionPrimaryColor : t.SelectionSecondaryColor;

                for (int i = t.SelectionGlowLayers; i >= 1; i--)
                {
                    float expand = t.SelectionGlowSpread * i;
                    float alpha = 0.15f / i;
                    var glowRect = bounds.Expand(expand);
                    int glowRadius = Mathf.Max(2, cornerPx + (int)expand);
                    DrawRoundedBorder(glowRect, glowColor.WithAlpha(alpha), t.SelectionBorderWidth, glowRadius);
                }

                DrawRoundedBorder(bounds, glowColor, t.SelectionBorderWidth, cornerPx);
            }

            // 10. 诊断覆盖边框（Error=红色 / Warning=黄色，始终渲染在选中发光之上）
            if (node.OverlayBorderColor.HasValue)
            {
                var oc = node.OverlayBorderColor.Value;
                for (int i = 2; i >= 1; i--)
                {
                    float expand = 2f * i;
                    var glowRect = bounds.Expand(expand);
                    int glowRadius = Mathf.Max(2, cornerPx + (int)expand);
                    DrawRoundedBorder(glowRect, oc.WithAlpha(0.2f / i), t.SelectionBorderWidth + 1f, glowRadius);
                }
                DrawRoundedBorder(bounds, oc, t.SelectionBorderWidth + 1f, cornerPx);
            }
        }

        /// <summary>
        /// 端口绘制（Zero-Matrix）。
        /// 所有 Handles.DrawSolidDisc 的 center 通过 C2W 转换为窗口坐标，
        /// radius 通过 S() 缩放为窗口像素。端口名称字号也随缩放。
        /// </summary>
        private void DrawPort(PortFrame port, NodeVisualTheme t)
        {
            var pos = port.Position;
            var oldColor = Handles.color;

            bool isMultipleInput = port.Direction == PortDirection.Input
                                   && port.Capacity == PortCapacity.Multiple;

            if (isMultipleInput)
            {
                // ── Multiple Input 端口：堆叠槽位（每个槽位按 Shape 绘制）──
                int connectedCount = port.ConnectedEdgeCount;
                int totalSlots = port.TotalSlots;
                float totalHeight = (totalSlots - 1) * t.PortSpacing;
                float startY = pos.Y - totalHeight * 0.5f;

                for (int i = 0; i < totalSlots; i++)
                {
                    float cy = startY + i * t.PortSpacing;
                    var wSlot = C2W(pos.X, cy);

                    if (port.Hovered && port.HoveredSlotIndex == i)
                        DrawPortHoverGlow(wSlot, t, port.Color);

                    bool isPlus = (i == totalSlots - 1);
                    bool slotConnected = !isPlus && i < connectedCount;
                    DrawPortShape(port.Shape, port.Direction, wSlot, t, port.Color, slotConnected);
                    if (isPlus) DrawPlusOverlay(wSlot, t, port.Color);
                }
            }
            else
            {
                // ── 普通端口：单个形状 ──
                var wCenter = C2W(pos);

                if (port.CanConnectToDragSource)
                    DrawPortCompatibleGlow(wCenter, t);

                if (port.Hovered)
                    DrawPortHoverGlow(wCenter, t, port.Color);

                DrawPortShape(port.Shape, port.Direction, wCenter, t, port.Color, port.Connected);
            }

            Handles.color = oldColor;

            // 端口名称（字号随缩放，位置在窗口坐标系计算）
            _portNameStyle!.fontSize = ScaledFontSize(t.PortFontSize);
            _portNameStyle.normal.textColor = t.PortTextColor.ToUnity();

            // CalcSize 返回窗口像素尺寸（因为字号已缩放）
            var textSize = _portNameStyle.CalcSize(new GUIContent(port.Name));
            // 端口锚点的窗口坐标
            var wPort = C2W(pos);
            float windowTextX;
            if (port.Direction == PortDirection.Input)
            {
                // 文字在端口右侧，间距也需缩放
                windowTextX = wPort.x + S(t.PortRadius + t.PortTextGap);
            }
            else
            {
                // 文字在端口左侧
                windowTextX = wPort.x - S(t.PortRadius + t.PortTextGap) - textSize.x;
            }

            GUI.Label(new Rect(windowTextX, wPort.y - textSize.y * 0.5f, textSize.x, textSize.y),
                port.Name, _portNameStyle);

            // 鼠标悬停时显示端口类型信息
            if (port.Hovered)
            {
                string kindText = port.Kind switch
                {
                    PortKind.Control => "控制流 (Control)",
                    PortKind.Event => "事件 (Event)",
                    PortKind.Data => "数据 (Data)",
                    _ => "未知"
                };
                string directionText = port.Direction == PortDirection.Input ? "输入 (Input)" : "输出 (Output)";
                string dataTypeText = string.IsNullOrEmpty(port.DataType) ? "" : $"\n类型: {port.DataType}";
                
                GUI.tooltip = $"{kindText} - {directionText}{dataTypeText}";
            }
        }

        /// <summary>
        /// 兼容端口脉冲光效（拖线时绿色呼吸动画）
        /// </summary>
        private void DrawPortCompatibleGlow(Vector3 wCenter, NodeVisualTheme t)
        {
            float time = (float)EditorApplication.timeSinceStartup;
            float pulse = Mathf.Abs(Mathf.Sin(time * 3f)); // 快速脉冲
            float alpha = 0.3f + pulse * 0.4f;
            
            var glowColor = new Color(0.2f, 1f, 0.3f, alpha); // 绿色
            float glowRadius = S(t.PortRadius + t.PortOuterRingWidth + 4f);
            
            Handles.color = glowColor;
            Handles.DrawSolidDisc(wCenter, Vector3.forward, glowRadius);
        }

        /// <summary>
        /// 端口悬停光效（窗口坐标）。
        /// 简洁风格：柔和的半透明底光 + 端口色细线环。
        /// </summary>
        private void DrawPortHoverGlow(Vector3 wCenter, NodeVisualTheme t, Color4 portColor)
        {
            var c = portColor.ToUnity();
            float outerR = S(t.PortRadius + t.PortOuterRingWidth);

            // 柔和底光（端口色低透明度）
            Handles.color = new Color(c.r, c.g, c.b, 0.15f);
            Handles.DrawSolidDisc(wCenter, Vector3.forward, outerR + S(4f));

            // 亮色细线环（纯矢量折线逼近圆，避免 DrawWireDisc 虚线问题）
            float ringR = outerR + S(2f);
            float lineW = Mathf.Max(1.5f, S(1f));
            var ringColor = new Color(
                Mathf.Min(c.r + 0.3f, 1f),
                Mathf.Min(c.g + 0.3f, 1f),
                Mathf.Min(c.b + 0.3f, 1f), 0.8f);
            Handles.color = ringColor;
            DrawSolidCircleRing(wCenter, ringR, lineW, 32);
        }

        /// <summary>
        /// 纯矢量实线圆环（DrawAAPolyLine 折线逼近）。
        /// 避免 Handles.DrawWireDisc 带 thickness 参数时产生虚线的问题。
        /// </summary>
        private static void DrawSolidCircleRing(Vector3 center, float radius, float lineWidth, int segments)
        {
            // segments+1 个点形成闭合环
            var points = new Vector3[segments + 1];
            float step = 360f / segments * Mathf.Deg2Rad;
            for (int i = 0; i <= segments; i++)
            {
                float angle = step * i;
                points[i] = new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius,
                    0f);
            }
            Handles.DrawAAPolyLine(lineWidth, points);
        }

        /// <summary>
        /// 在端口形状上叠加绘制 "+" 号（用于 Multiple Input 端口的空槽位）。
        /// 调用前应已通过 DrawPortShape 绘制底部形状。
        /// </summary>
        private void DrawPlusOverlay(Vector3 wCenter, NodeVisualTheme t, Color4 portColor)
        {
            float halfLen = S(t.PortRadius * 0.45f);
            float thickness = Mathf.Max(1f, 2.5f * _zoom);
            Handles.color = portColor.ToUnity();
            Handles.DrawAAPolyLine(thickness,
                new Vector3(wCenter.x - halfLen, wCenter.y, 0),
                new Vector3(wCenter.x + halfLen, wCenter.y, 0));
            Handles.DrawAAPolyLine(thickness,
                new Vector3(wCenter.x, wCenter.y - halfLen, 0),
                new Vector3(wCenter.x, wCenter.y + halfLen, 0));
        }

        /// <summary>按 PortShape 分派绘制端口形状（圆 / 三角 / 菱形）</summary>
        private void DrawPortShape(PortShape shape, PortDirection direction, Vector3 wCenter,
            NodeVisualTheme t, Color4 portColor, bool connected)
        {
            switch (shape)
            {
                case PortShape.Triangle:
                    DrawTrianglePort(direction, wCenter, t, portColor, connected);
                    break;
                case PortShape.Diamond:
                    DrawDiamondPort(wCenter, t, portColor, connected);
                    break;
                default:
                    DrawCirclePort(wCenter, t, portColor, connected);
                    break;
            }
        }

        /// <summary>绘制圆形端口（Data 端口）</summary>
        private void DrawCirclePort(Vector3 wCenter, NodeVisualTheme t, Color4 portColor, bool connected)
        {
            Handles.color = t.PortOuterRingColor.ToUnity();
            Handles.DrawSolidDisc(wCenter, Vector3.forward, S(t.PortRadius + t.PortOuterRingWidth));
            Handles.color = portColor.ToUnity();
            Handles.DrawSolidDisc(wCenter, Vector3.forward, S(t.PortRadius));
            if (!connected)
            {
                Handles.color = t.NodeBodyColor.ToUnity();
                Handles.DrawSolidDisc(wCenter, Vector3.forward, S(t.PortRadius - t.PortHollowWidth));
            }
        }

        /// <summary>
        /// 绘制三角形端口（Control 端口）。以 wCenter 为几何中心，尖端朝向连线方向。
        /// 输入端（左侧）：尖端朝左 ◁；输出端（右侧）：尖端朝右 ▷
        /// </summary>
        private void DrawTrianglePort(PortDirection direction, Vector3 wCenter,
            NodeVisualTheme t, Color4 portColor, bool connected)
        {
            float R = S(t.PortRadius);
            float halfR = R * 0.5f;

            Vector3 tip, cornerA, cornerB;
            if (direction == PortDirection.Input)
            {
                tip     = new Vector3(wCenter.x - R,      wCenter.y,     0);
                cornerA = new Vector3(wCenter.x + halfR,  wCenter.y - R, 0);
                cornerB = new Vector3(wCenter.x + halfR,  wCenter.y + R, 0);
            }
            else
            {
                tip     = new Vector3(wCenter.x + R,      wCenter.y,     0);
                cornerA = new Vector3(wCenter.x - halfR,  wCenter.y - R, 0);
                cornerB = new Vector3(wCenter.x - halfR,  wCenter.y + R, 0);
            }

            float outerScale = 1f + t.PortOuterRingWidth / t.PortRadius;
            Handles.color = t.PortOuterRingColor.ToUnity();
            Handles.DrawAAConvexPolygon(
                ScaleFrom(wCenter, tip,     outerScale),
                ScaleFrom(wCenter, cornerA, outerScale),
                ScaleFrom(wCenter, cornerB, outerScale));

            Handles.color = portColor.ToUnity();
            Handles.DrawAAConvexPolygon(tip, cornerA, cornerB);

            if (!connected)
            {
                float hollowScale = (t.PortRadius - t.PortHollowWidth) / t.PortRadius;
                if (hollowScale > 0f)
                {
                    Handles.color = t.NodeBodyColor.ToUnity();
                    Handles.DrawAAConvexPolygon(
                        ScaleFrom(wCenter, tip,     hollowScale),
                        ScaleFrom(wCenter, cornerA, hollowScale),
                        ScaleFrom(wCenter, cornerB, hollowScale));
                }
            }
        }

        /// <summary>绘制菱形端口（Event 端口）。以 wCenter 为几何中心。</summary>
        private void DrawDiamondPort(Vector3 wCenter, NodeVisualTheme t, Color4 portColor, bool connected)
        {
            float R = S(t.PortRadius);
            var top    = new Vector3(wCenter.x,      wCenter.y - R, 0);
            var right  = new Vector3(wCenter.x + R,  wCenter.y,     0);
            var bottom = new Vector3(wCenter.x,      wCenter.y + R, 0);
            var left   = new Vector3(wCenter.x - R,  wCenter.y,     0);

            float outerScale = 1f + t.PortOuterRingWidth / t.PortRadius;
            Handles.color = t.PortOuterRingColor.ToUnity();
            Handles.DrawAAConvexPolygon(
                ScaleFrom(wCenter, top,    outerScale),
                ScaleFrom(wCenter, right,  outerScale),
                ScaleFrom(wCenter, bottom, outerScale),
                ScaleFrom(wCenter, left,   outerScale));

            Handles.color = portColor.ToUnity();
            Handles.DrawAAConvexPolygon(top, right, bottom, left);

            if (!connected)
            {
                float hollowScale = (t.PortRadius - t.PortHollowWidth) / t.PortRadius;
                if (hollowScale > 0f)
                {
                    Handles.color = t.NodeBodyColor.ToUnity();
                    Handles.DrawAAConvexPolygon(
                        ScaleFrom(wCenter, top,    hollowScale),
                        ScaleFrom(wCenter, right,  hollowScale),
                        ScaleFrom(wCenter, bottom, hollowScale),
                        ScaleFrom(wCenter, left,   hollowScale));
                }
            }
        }

        /// <summary>从 center 出发对 point 做缩放（用于生成外圈/内圈顶点）</summary>
        private static Vector3 ScaleFrom(Vector3 center, Vector3 point, float scale)
            => center + (point - center) * scale;

        /// <summary>
        /// 节点内容绘制（Zero-Matrix）。
        /// 分隔线通过 DrawLine（内部 C2W 转换），摘要文字通过 C2WRect 转换。
        /// 编辑模式下内容渲染器接收画布坐标矩形（内部使用 editCtx 绘制）。
        /// </summary>
        private void DrawNodeContent(NodeContentInfo content, NodeVisualTheme t, IEditContext? editCtx)
        {
            var rect = content.ContentRect; // 画布坐标
            if (rect.Height <= 4f) return;

            // 内容区分隔线（DrawLine 内部会 C2W 转换）
            if (content.HasSeparator)
            {
                DrawLine(
                    new Vec2(rect.X - t.ContentPadding + 8f, rect.Y - 1f),
                    new Vec2(rect.X + rect.Width + t.ContentPadding - 8f, rect.Y - 1f),
                    t.ContentSeparatorColor, 1f);
            }

            if (content.ShowEditor && editCtx != null && content.Node != null)
            {
                // 编辑模式：将画布坐标转换为窗口坐标，初始化 editCtx 布局区域
                var windowRect = C2WRect(rect);
                editCtx.Begin(new Rect2(windowRect.x, windowRect.y,
                    windowRect.width, windowRect.height));

                if (_contentRenderers.TryGetValue(content.TypeId, out var renderer))
                {
                    renderer.DrawEditor(content.Node, rect, editCtx);
                }
            }
            else
            {
                // 摘要模式：绘制摘要文本行（字号随缩放）
                _summaryStyle!.fontSize = ScaledFontSize(EditorStyles.miniLabel.fontSize > 0
                    ? EditorStyles.miniLabel.fontSize : 10);

                // 窗口空间的内容宽度，用于 CalcHeight
                float windowWidth = S(rect.Width);
                float windowY = C2W(0, rect.Y).y;
                float windowX = C2W(rect.X, 0).x;
                float windowBottom = C2W(0, rect.Bottom).y;

                foreach (var line in content.SummaryLines)
                {
                    var lineHeight = _summaryStyle.CalcHeight(new GUIContent(line), windowWidth);
                    GUI.Label(new Rect(windowX, windowY, windowWidth, lineHeight), line, _summaryStyle);
                    windowY += lineHeight + S(1f);
                    if (windowY > windowBottom) break;
                }
            }
        }

        // ══════════════════════════════════════
        //  连线
        // ══════════════════════════════════════

        /// <summary>
        /// 连线绘制（Zero-Matrix）。
        /// 贝塞尔曲线的 4 个控制点通过 C2W 转换为窗口坐标。
        /// 线宽通过 S() 缩放（Handles.DrawBezier 的 width 始终为屏幕像素）。
        /// </summary>
        private void DrawEdge(EdgeFrame edge, NodeVisualTheme t)
        {
            var start = edge.Start;
            var end = edge.End;
            var cp1 = start + edge.TangentA;
            var cp2 = end + edge.TangentB;

            // 画布控制点 → 窗口控制点
            var wStart = C2W(start);
            var wEnd = C2W(end);
            var wCp1 = C2W(cp1);
            var wCp2 = C2W(cp2);

            // 线宽缩放到窗口像素
            float scaledWidth = Mathf.Max(1f, edge.Width * _zoom);

            // 选中时先绘制外发光底层
            if (edge.Selected)
            {
                Handles.DrawBezier(wStart, wEnd, wCp1, wCp2,
                    edge.Color.WithAlpha(0.2f).ToUnity(),
                    null, scaledWidth + 6f * _zoom);
            }

            Handles.DrawBezier(wStart, wEnd, wCp1, wCp2,
                edge.Color.ToUnity(),
                null, scaledWidth);

            // 流动动画：沿贝塞尔曲线移动的小圆点（表示数据流方向）
            DrawEdgeFlowDots(start, cp1, cp2, end, edge.Color, scaledWidth);

            // 连线标签
            if (edge.Label != null)
                DrawEdgeLabel(edge.Label);
        }

        /// <summary>
        /// 连线流动动画：沿贝塞尔曲线绘制 3 个移动小圆点。
        /// 使用 EditorApplication.timeSinceStartup 驱动动画，无需额外状态管理。
        /// </summary>
        private void DrawEdgeFlowDots(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3,
            Color4 edgeColor, float scaledWidth)
        {
            float time = (float)EditorApplication.timeSinceStartup;
            float speed = 0.4f; // 流动速度（每秒走过曲线百分比）
            int dotCount = 3;
            float dotSpacing = 1f / dotCount;
            float dotRadius = Mathf.Max(2f, scaledWidth * 1.2f);

            var oldColor = Handles.color;
            // 圆点颜色：连线色但更亮更不透明
            var dotColor = new Color(
                Mathf.Min(edgeColor.R * 1.5f, 1f),
                Mathf.Min(edgeColor.G * 1.5f, 1f),
                Mathf.Min(edgeColor.B * 1.5f, 1f),
                0.8f);
            Handles.color = dotColor;

            for (int i = 0; i < dotCount; i++)
            {
                float t = (time * speed + i * dotSpacing) % 1f;
                var canvasPoint = BezierMath.Evaluate(p0, p1, p2, p3, t);
                Handles.DrawSolidDisc(C2W(canvasPoint), Vector3.forward, dotRadius);
            }

            Handles.color = oldColor;
        }

        /// <summary>连线标签（药丸形圆角背景 + 文字居中）</summary>
        private void DrawEdgeLabel(EdgeLabelInfo label)
        {
            if (string.IsNullOrEmpty(label.Text)) return;

            _edgeLabelStyle!.fontSize = ScaledFontSize(EditorStyles.miniLabel.fontSize > 0
                ? EditorStyles.miniLabel.fontSize : 10);

            // 标签中心位置和尺寸从画布坐标转换为窗口坐标（加点额外内边距）
            float padX = 4f;
            float padY = 1f;
            var bgRect = C2WRect(
                label.Position.X - label.Size.X * 0.5f - padX,
                label.Position.Y - label.Size.Y * 0.5f - padY,
                label.Size.X + padX * 2f, label.Size.Y + padY * 2f);

            // 药丸形背景（圆角半径 = 高度的一半，形成胶囊形状）
            float pillR = bgRect.height * 0.5f;
            var pillColor = new Color(0.12f, 0.12f, 0.12f, 0.92f);
            DrawFilledRoundedRect(bgRect, pillR, pillColor);

            // 药丸边框
            var borderColor = new Color(0.35f, 0.35f, 0.35f, 0.6f);
            Handles.color = borderColor;
            float borderWidth = Mathf.Max(1f, _zoom);
            // 简化：用圆角矩形边框
            float r = bgRect.height * 0.5f;
            // 上下直线
            Handles.DrawAAPolyLine(borderWidth,
                new Vector3(bgRect.x + r, bgRect.y, 0),
                new Vector3(bgRect.xMax - r, bgRect.y, 0));
            Handles.DrawAAPolyLine(borderWidth,
                new Vector3(bgRect.x + r, bgRect.yMax, 0),
                new Vector3(bgRect.xMax - r, bgRect.yMax, 0));
            // 左右半圆弧
            DrawScreenArc(bgRect.x + r, bgRect.y + r, r, 90f, 270f, 8, borderWidth);
            DrawScreenArc(bgRect.xMax - r, bgRect.y + r, r, -90f, 90f, 8, borderWidth);

            // 文字居中
            var textRect = C2WRect(
                label.Position.X - label.Size.X * 0.5f,
                label.Position.Y - label.Size.Y * 0.5f,
                label.Size.X, label.Size.Y);
            GUI.Label(textRect, label.Text, _edgeLabelStyle);
        }

        /// <summary>屏幕坐标圆弧辅助（用于药丸标签边框，坐标已在窗口空间）</summary>
        private static void DrawScreenArc(float cx, float cy, float radius,
            float startAngle, float endAngle, int segments, float lineWidth)
        {
            float step = (endAngle - startAngle) / segments;
            for (int i = 0; i < segments; i++)
            {
                float a1 = (startAngle + step * i) * Mathf.Deg2Rad;
                float a2 = (startAngle + step * (i + 1)) * Mathf.Deg2Rad;
                Handles.DrawAAPolyLine(lineWidth,
                    new Vector3(cx + Mathf.Cos(a1) * radius, cy + Mathf.Sin(a1) * radius, 0),
                    new Vector3(cx + Mathf.Cos(a2) * radius, cy + Mathf.Sin(a2) * radius, 0));
            }
        }

        // ══════════════════════════════════════
        //  覆盖层
        // ══════════════════════════════════════

        /// <summary>
        /// 覆盖层绘制（Zero-Matrix）。
        /// 框选矩形通过 C2WRect 转换，拖拽连线通过 C2W 转换控制点。
        /// </summary>
        private void DrawOverlay(OverlayFrame overlay)
        {
            switch (overlay.Type)
            {
                case OverlayType.MarqueeSelection:
                    // 框选矩形：半透明填充 + 边框（画布坐标 → 窗口坐标）
                    EditorGUI.DrawRect(C2WRect(overlay.Rect),
                        overlay.Color.WithAlpha(0.15f).ToUnity());
                    DrawRectOutline(overlay.Rect, overlay.Color, 1f);
                    break;

                case OverlayType.DragConnection:
                    // 拖拽连线预览（画布控制点 → 窗口控制点）
                    var start = overlay.Start;
                    var end = overlay.End;
                    var cp1 = start + overlay.TangentA;
                    var cp2 = end + overlay.TangentB;
                    float scaledOverlayWidth = Mathf.Max(1f, overlay.Width * _zoom);
                    Handles.DrawBezier(
                        C2W(start), C2W(end), C2W(cp1), C2W(cp2),
                        overlay.Color.ToUnity(),
                        null, scaledOverlayWidth);
                    break;
            }
        }

        // ══════════════════════════════════════
        //  小地图
        // ══════════════════════════════════════

        /// <summary>
        /// 小地图绘制。小地图坐标已在窗口空间，不经过 C2W 转换，
        /// 使用 DrawScreenRectOutline 直接以窗口坐标绘制边框。
        /// </summary>
        private void DrawMiniMap(MiniMapFrame miniMap)
        {
            var screenRect = miniMap.ScreenRect.ToUnity();

            // 背景
            EditorGUI.DrawRect(screenRect, miniMap.BackgroundColor.ToUnity());

            // 节点缩略
            foreach (var node in miniMap.Nodes)
            {
                EditorGUI.DrawRect(node.Rect.ToUnity(), node.Color.ToUnity());
            }

            // 视口矩形（窗口坐标，不转换）
            DrawScreenRectOutline(miniMap.ViewportRect.ToUnity(), miniMap.ViewportColor, 1f);

            // 边框
            DrawScreenRectOutline(miniMap.ScreenRect.ToUnity(), miniMap.BorderColor, 1f);
        }

        // ══════════════════════════════════════
        //  绘制工具方法
        // ══════════════════════════════════════

        /// <summary>
        /// 画线（Zero-Matrix）。from/to 为画布坐标，通过 C2W 转换后绘制。
        /// width 为画布空间线宽，通过 S() 缩放为窗口像素（最小 1px）。
        /// </summary>
        private void DrawLine(Vec2 from, Vec2 to, Color4 color, float width)
        {
            var oldColor = Handles.color;
            Handles.color = color.ToUnity();
            float scaledWidth = Mathf.Max(1f, width * _zoom);
            Handles.DrawAAPolyLine(scaledWidth, C2W(from), C2W(to));
            Handles.color = oldColor;
        }

        /// <summary>
        /// 矩形边框（Zero-Matrix）。rect 为画布坐标，通过 C2WRect 转换后绘制。
        /// width 为画布空间线宽，通过 S() 缩放为窗口像素（最小 1px）。
        /// </summary>
        private void DrawRectOutline(Rect2 rect, Color4 color, float width)
        {
            var c = color.ToUnity();
            // 画布矩形 → 窗口矩形
            var r = C2WRect(rect);
            float w = Mathf.Max(1f, S(width));
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, w), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - w, r.width, w), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y + w, w, r.height - w * 2), c);
            EditorGUI.DrawRect(new Rect(r.xMax - w, r.y + w, w, r.height - w * 2), c);
        }

        /// <summary>
        /// 圆角矩形边框（通过四条直线 + 四个圆弧绘制，画布坐标）。
        /// 使用 Handles.DrawAAPolyLine 绘制抗锯齿圆角边框。
        /// </summary>
        private void DrawRoundedBorder(Rect2 rect, Color4 color, float width, int cornerRadius)
        {
            var c = color.ToUnity();
            float r = cornerRadius;
            float scaledWidth = Mathf.Max(1f, S(width));
            int segments = 6; // 每个圆弧的分段数

            var oldColor = Handles.color;
            Handles.color = c;

            // 四条直边（内缩圆角部分）
            // 上边
            Handles.DrawAAPolyLine(scaledWidth,
                C2W(rect.X + r, rect.Y),
                C2W(rect.Right - r, rect.Y));
            // 下边
            Handles.DrawAAPolyLine(scaledWidth,
                C2W(rect.X + r, rect.Bottom),
                C2W(rect.Right - r, rect.Bottom));
            // 左边
            Handles.DrawAAPolyLine(scaledWidth,
                C2W(rect.X, rect.Y + r),
                C2W(rect.X, rect.Bottom - r));
            // 右边
            Handles.DrawAAPolyLine(scaledWidth,
                C2W(rect.Right, rect.Y + r),
                C2W(rect.Right, rect.Bottom - r));

            // 四个圆弧
            DrawCornerArc(rect.X + r, rect.Y + r, r, 180f, 270f, segments, scaledWidth);     // 左上
            DrawCornerArc(rect.Right - r, rect.Y + r, r, 270f, 360f, segments, scaledWidth);  // 右上
            DrawCornerArc(rect.Right - r, rect.Bottom - r, r, 0f, 90f, segments, scaledWidth); // 右下
            DrawCornerArc(rect.X + r, rect.Bottom - r, r, 90f, 180f, segments, scaledWidth);  // 左下

            Handles.color = oldColor;
        }

        /// <summary>圆弧辅助：在画布坐标绘制一段圆弧，转换到窗口坐标</summary>
        private void DrawCornerArc(float cx, float cy, float radius,
            float startAngle, float endAngle, int segments, float lineWidth)
        {
            float step = (endAngle - startAngle) / segments;
            for (int i = 0; i < segments; i++)
            {
                float a1 = (startAngle + step * i) * Mathf.Deg2Rad;
                float a2 = (startAngle + step * (i + 1)) * Mathf.Deg2Rad;
                Handles.DrawAAPolyLine(lineWidth,
                    C2W(cx + Mathf.Cos(a1) * radius, cy + Mathf.Sin(a1) * radius),
                    C2W(cx + Mathf.Cos(a2) * radius, cy + Mathf.Sin(a2) * radius));
            }
        }

        /// <summary>
        /// 纯矢量填充圆角矩形（窗口坐标）。
        /// 使用十字形矩形 + 四角圆盘组合，避免纹理 9-slice 的边缘伪影。
        /// </summary>
        private static void DrawFilledRoundedRect(Rect rect, float radius, Color color)
        {
            if (radius < 0.5f)
            {
                EditorGUI.DrawRect(rect, color);
                return;
            }

            // 限制半径不超过短边的一半
            radius = Mathf.Min(radius, rect.width * 0.5f, rect.height * 0.5f);

            // 十字形填充：水平条 + 垂直条
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + radius, rect.width, rect.height - radius * 2f), color);
            EditorGUI.DrawRect(new Rect(rect.x + radius, rect.y, rect.width - radius * 2f, rect.height), color);

            // 四角圆盘填充
            var oldColor = Handles.color;
            Handles.color = color;
            Handles.DrawSolidDisc(new Vector3(rect.x + radius, rect.y + radius, 0), Vector3.forward, radius);
            Handles.DrawSolidDisc(new Vector3(rect.xMax - radius, rect.y + radius, 0), Vector3.forward, radius);
            Handles.DrawSolidDisc(new Vector3(rect.x + radius, rect.yMax - radius, 0), Vector3.forward, radius);
            Handles.DrawSolidDisc(new Vector3(rect.xMax - radius, rect.yMax - radius, 0), Vector3.forward, radius);
            Handles.color = oldColor;
        }

        /// <summary>
        /// 矩形边框（窗口坐标版本，不做 C2W 转换）。
        /// 用于小地图等已在窗口空间的元素。
        /// </summary>
        private static void DrawScreenRectOutline(Rect r, Color4 color, float width)
        {
            var c = color.ToUnity();
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, width), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - width, r.width, width), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y + width, width, r.height - width * 2), c);
            EditorGUI.DrawRect(new Rect(r.xMax - width, r.y + width, width, r.height - width * 2), c);
        }

        // ══════════════════════════════════════
        //  视口裁剪
        // ══════════════════════════════════════

        /// <summary>节点是否在可见画布区域内</summary>
        private bool IsNodeVisible(NodeFrame node)
        {
            var b = node.Bounds;
            return _visibleCanvasRect.Overlaps(new Rect(b.X, b.Y, b.Width, b.Height));
        }

        /// <summary>连线是否在可见画布区域内（起点或终点至少有一个在可见区域）</summary>
        private bool IsEdgeVisible(EdgeFrame edge)
        {
            return _visibleCanvasRect.Contains(new Vector2(edge.Start.X, edge.Start.Y))
                || _visibleCanvasRect.Contains(new Vector2(edge.End.X, edge.End.Y));
        }
    }
}
