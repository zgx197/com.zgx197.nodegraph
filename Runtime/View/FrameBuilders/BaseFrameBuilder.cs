#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Abstraction;
using NodeGraph.Core;
using NodeGraph.Math;
using NodeGraph.View.Handlers;

namespace NodeGraph.View
{
    /// <summary>
    /// FrameBuilder 基类。包含所有蓝图类型共用的渲染帧构建逻辑。
    /// 子类通过重写 virtual 方法来定制：端口方向、节点尺寸、连线路由等。
    /// </summary>
    public abstract class BaseFrameBuilder : IGraphFrameBuilder
    {
        protected readonly ITextMeasurer TextMeasurer;

        // 帧内节点尺寸缓存
        private readonly Dictionary<string, Vec2> _nodeSizeCache = new Dictionary<string, Vec2>();
        private int _cacheFrameId = -1;

        protected BaseFrameBuilder(ITextMeasurer textMeasurer)
        {
            TextMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        }

        // ══════════════════════════════════════
        //  子类必须/可选重写的差异点
        // ══════════════════════════════════════

        /// <summary>是否为水平布局（端口在左右）。false 表示垂直布局（端口在上下）。</summary>
        protected virtual bool IsHorizontalLayout => true;

        /// <summary>计算连线路由切线。子类可重写为折线、箭头等风格。</summary>
        protected virtual (Vec2 tangentA, Vec2 tangentB) ComputeEdgeRoute(Vec2 start, Vec2 end)
        {
            return BezierMath.ComputePortTangents(start, end, IsHorizontalLayout);
        }

        /// <summary>计算边界端口在框边缘的位置（SubGraphFrame 展开时）。</summary>
        protected virtual Vec2 GetBoundaryPortPosition(
            Port port, Rect2 frameBounds, int index, float portSpacing, float titleBarHeight)
        {
            float offset = titleBarHeight + portSpacing * (index + 0.5f);
            if (IsHorizontalLayout)
            {
                return port.Direction == PortDirection.Input
                    ? new Vec2(frameBounds.X, frameBounds.Y + offset)
                    : new Vec2(frameBounds.Right, frameBounds.Y + offset);
            }
            else
            {
                return port.Direction == PortDirection.Input
                    ? new Vec2(frameBounds.X + offset, frameBounds.Y)
                    : new Vec2(frameBounds.X + offset, frameBounds.Bottom);
            }
        }

        // ══════════════════════════════════════
        //  IGraphFrameBuilder 实现
        // ══════════════════════════════════════

        /// <summary>
        /// 返回节点描述条的额外高度（当 UserData 实现 IDescribableNode 且 Description 非空时）。
        /// </summary>
        protected static float GetDescriptionBarHeight(Node node, NodeVisualTheme theme)
        {
            if (node.UserData is IDescribableNode d && !string.IsNullOrEmpty(d.Description))
                return theme.DescriptionBarHeight;
            return 0f;
        }

        public virtual Vec2 ComputeNodeSize(Node node, GraphViewModel viewModel)
        {
            int frameId = viewModel.FrameId;
            if (frameId != _cacheFrameId)
            {
                _nodeSizeCache.Clear();
                _cacheFrameId = frameId;
            }
            if (_nodeSizeCache.TryGetValue(node.Id, out var cached))
                return cached;

            var t = viewModel.RenderConfig.Theme;
            var renderInfo = viewModel.GetNodeRenderInfo(node.TypeId);
            string displayName = renderInfo.DisplayName;
            float titleWidth = TextMeasurer.MeasureText(displayName, t.TitleFontSize).X + t.TitlePaddingLeft * 2f;

            float maxInputWidth = 0f, maxOutputWidth = 0f;
            int inputSlots = 0, outputSlots = 0;
            foreach (var port in node.Ports)
            {
                float portTextWidth = TextMeasurer.MeasureText(port.Name, t.PortFontSize).X
                    + t.PortRadius + t.PortTextGap;
                int slots = GetPortSlotCount(port, viewModel);
                if (port.Direction == PortDirection.Input)
                {
                    maxInputWidth = MathF.Max(maxInputWidth, portTextWidth);
                    inputSlots += slots;
                }
                else
                {
                    maxOutputWidth = MathF.Max(maxOutputWidth, portTextWidth);
                    outputSlots += slots;
                }
            }

            float descH = GetDescriptionBarHeight(node, t);

            Vec2 size;
            if (IsHorizontalLayout)
            {
                float portsWidth = maxInputWidth + maxOutputWidth + t.PortRadius * 2f + 24f;
                float nodeWidth = MathF.Max(t.NodeMinWidth, MathF.Max(titleWidth, portsWidth));
                int maxSlots = System.Math.Max(inputSlots, outputSlots);
                float portAreaHeight = maxSlots > 0 ? maxSlots * t.PortSpacing : 0f;
                float contentHeight = GetContentHeight(node, viewModel);
                float nodeHeight = t.TitleBarHeight + descH + portAreaHeight + contentHeight + 8f;
                nodeHeight = MathF.Max(nodeHeight, t.TitleBarHeight + descH + 20f);
                size = new Vec2(nodeWidth, nodeHeight);
            }
            else
            {
                int maxSlots = System.Math.Max(inputSlots, outputSlots);
                float portAreaWidth = maxSlots > 0 ? maxSlots * t.PortSpacing : 0f;
                float nodeWidth = MathF.Max(t.NodeMinWidth, MathF.Max(titleWidth, portAreaWidth + 16f));
                float portsHeight = maxInputWidth + maxOutputWidth + t.PortRadius * 2f + 24f;
                float contentHeight = GetContentHeight(node, viewModel);
                float nodeHeight = t.TitleBarHeight + descH + portsHeight + contentHeight + 8f;
                nodeHeight = MathF.Max(nodeHeight, t.TitleBarHeight + descH + 20f);
                size = new Vec2(nodeWidth, nodeHeight);
            }

            _nodeSizeCache[node.Id] = size;
            return size;
        }

        public virtual Vec2 GetPortPosition(Port port, Node node, Rect2 bounds,
            NodeVisualTheme theme, GraphViewModel viewModel)
        {
            float slotOffset = 0f;
            foreach (var p in node.Ports)
            {
                if (p.Direction == port.Direction)
                {
                    int slots = GetPortSlotCount(p, viewModel);
                    if (p.Id == port.Id)
                    {
                        slotOffset += slots * 0.5f;
                        break;
                    }
                    slotOffset += slots;
                }
            }

            if (IsHorizontalLayout)
            {
                float descH = GetDescriptionBarHeight(node, theme);
                float y = bounds.Y + theme.TitleBarHeight + descH + theme.PortSpacing * slotOffset;
                return port.Direction == PortDirection.Input
                    ? new Vec2(bounds.X + theme.PortInset, y)
                    : new Vec2(bounds.Right - theme.PortInset, y);
            }
            else
            {
                float x = bounds.X + theme.PortSpacing * slotOffset + theme.PortSpacing * 0.5f;
                return port.Direction == PortDirection.Input
                    ? new Vec2(x, bounds.Y + theme.PortInset)
                    : new Vec2(x, bounds.Bottom - theme.PortInset);
            }
        }

        public virtual int GetPortSlotCount(Port port, GraphViewModel viewModel)
        {
            if (port.Direction != PortDirection.Input || port.Capacity != PortCapacity.Multiple)
                return 1;
            int edgeCount = viewModel.Graph.GetEdgeCountForPort(port.Id);
            int minSlots = System.Math.Max(edgeCount + 1, 2);
            int targetSlots = viewModel.GetPortTargetSlots(port.Id);
            return System.Math.Max(targetSlots, minSlots);
        }

        public virtual Vec2 GetEdgeTargetPosition(Edge edge, Port targetPort, Node targetNode,
            Rect2 bounds, NodeVisualTheme theme, GraphViewModel viewModel)
        {
            if (targetPort.Direction != PortDirection.Input || targetPort.Capacity != PortCapacity.Multiple)
                return GetPortPosition(targetPort, targetNode, bounds, theme, viewModel);

            float slotOffsetBefore = 0f;
            foreach (var p in targetNode.Ports)
            {
                if (p.Direction != targetPort.Direction) continue;
                if (p.Id == targetPort.Id) break;
                slotOffsetBefore += GetPortSlotCount(p, viewModel);
            }

            int edgeIndex = 0;
            foreach (var e in viewModel.Graph.Edges)
            {
                if (e.TargetPortId == targetPort.Id)
                {
                    if (e.Id == edge.Id) break;
                    edgeIndex++;
                }
            }

            if (IsHorizontalLayout)
            {
                float descH = GetDescriptionBarHeight(targetNode, theme);
                float y = bounds.Y + theme.TitleBarHeight + descH
                    + theme.PortSpacing * (slotOffsetBefore + edgeIndex + 0.5f);
                return new Vec2(bounds.X + theme.PortInset, y);
            }
            else
            {
                float x = bounds.X + theme.PortSpacing * (slotOffsetBefore + edgeIndex + 0.5f)
                    + theme.PortSpacing * 0.5f;
                return new Vec2(x, bounds.Y + theme.PortInset);
            }
        }

        // ══════════════════════════════════════
        //  BuildFrame 主流程（通用）
        // ══════════════════════════════════════

        public virtual GraphFrame BuildFrame(GraphViewModel viewModel, Rect2 viewport)
        {
            var frame = new GraphFrame
            {
                PanOffset = viewModel.PanOffset,
                ZoomLevel = viewModel.ZoomLevel
            };

            var graph = viewModel.Graph;
            var theme = viewModel.RenderConfig.Theme;

            Vec2 topLeft = (viewport.Position - viewModel.PanOffset) / viewModel.ZoomLevel;
            Vec2 bottomRight = (viewport.Position + viewport.Size - viewModel.PanOffset) / viewModel.ZoomLevel;
            var visibleRect = new Rect2(topLeft, bottomRight - topLeft);

            frame.Background = BuildBackground(theme, visibleRect);

            foreach (var node in graph.Nodes)
                node.Size = ComputeNodeSize(node, viewModel);

            // 展开状态的 SubGraphFrame 自动适应内部节点范围
            foreach (var sgf in graph.SubGraphFrames)
            {
                if (!sgf.IsCollapsed && sgf.ContainedNodeIds.Count > 0)
                    sgf.AutoFit(graph);
            }

            var hiddenNodeIds = BuildHiddenNodeSet(graph);
            var boundaryPortPositions = BuildBoundaryPortPositionMap(graph, viewModel);

            BuildDecorations(frame, viewModel, visibleRect);
            BuildEdges(frame, viewModel, visibleRect, hiddenNodeIds, boundaryPortPositions);
            BuildNodes(frame, viewModel, visibleRect, hiddenNodeIds);

            return frame;
        }

        // ══════════════════════════════════════
        //  通用内部构建方法
        // ══════════════════════════════════════

        protected BackgroundFrame BuildBackground(NodeVisualTheme theme, Rect2 visibleRect)
        {
            return new BackgroundFrame
            {
                VisibleRect = visibleRect,
                SmallGridSize = theme.GridSmallSize,
                LargeGridSize = theme.GridSmallSize * theme.GridLargeMultiplier,
                BackgroundColor = theme.GridBackgroundColor,
                SmallLineColor = theme.GridSmallLineColor,
                LargeLineColor = theme.GridLargeLineColor
            };
        }

        protected HashSet<string> BuildHiddenNodeSet(Graph graph)
        {
            var hidden = new HashSet<string>();
            foreach (var sgf in graph.SubGraphFrames)
            {
                if (sgf.IsCollapsed)
                {
                    foreach (var nid in sgf.ContainedNodeIds)
                        hidden.Add(nid);
                }
                else
                {
                    hidden.Add(sgf.RepresentativeNodeId);
                }
            }
            return hidden;
        }

        protected Dictionary<string, Vec2> BuildBoundaryPortPositionMap(Graph graph, GraphViewModel viewModel)
        {
            var map = new Dictionary<string, Vec2>();
            float titleBarHeight = 24f;
            float portSpacing = viewModel.RenderConfig.Theme.PortSpacing;

            foreach (var sgf in graph.SubGraphFrames)
            {
                if (sgf.IsCollapsed) continue;
                var repNode = graph.FindNode(sgf.RepresentativeNodeId);
                if (repNode == null) continue;

                int inputIndex = 0, outputIndex = 0;
                foreach (var port in repNode.Ports)
                {
                    int idx = port.Direction == PortDirection.Input ? inputIndex++ : outputIndex++;
                    map[port.Id] = GetBoundaryPortPosition(port, sgf.Bounds, idx, portSpacing, titleBarHeight);
                }
            }
            return map;
        }

        protected void BuildDecorations(GraphFrame frame, GraphViewModel viewModel, Rect2 visibleRect)
        {
            var graph = viewModel.Graph;

            foreach (var comment in graph.Comments)
            {
                if (!visibleRect.Overlaps(comment.Bounds)) continue;
                frame.Decorations.Add(new DecorationFrame
                {
                    Kind = DecorationKind.Comment,
                    Id = comment.Id,
                    Bounds = comment.Bounds,
                    BackgroundColor = comment.BackgroundColor,
                    BorderColor = new Color4(0.4f, 0.4f, 0.4f, 0.5f),
                    Text = comment.Text,
                    FontSize = comment.FontSize,
                    TextColor = comment.TextColor
                });
            }

            foreach (var container in graph.AllContainers)
            {
                if (!visibleRect.Overlaps(container.Bounds)) continue;

                if (container is NodeGroup group)
                {
                    frame.Decorations.Add(new DecorationFrame
                    {
                        Kind = DecorationKind.Group,
                        Id = group.Id,
                        Bounds = group.Bounds,
                        Title = group.Title,
                        BackgroundColor = group.Color,
                        BorderColor = new Color4(
                            group.Color.R * 1.5f, group.Color.G * 1.5f,
                            group.Color.B * 1.5f, 0.6f)
                    });
                }
                else if (container is SubGraphFrame sgf)
                {
                    // 折叠状态下不渲染框装饰，仅显示 RepresentativeNode
                    if (sgf.IsCollapsed) continue;

                    var decoFrame = new DecorationFrame
                    {
                        Kind = DecorationKind.SubGraph,
                        Id = sgf.Id,
                        Bounds = sgf.Bounds,
                        Title = sgf.Title,
                        BackgroundColor = new Color4(0.15f, 0.25f, 0.35f, 0.4f),
                        BorderColor = new Color4(0.3f, 0.5f, 0.7f, 0.8f),
                        ShowCollapseButton = true,
                        IsCollapsed = false
                    };

                    var repNode = graph.FindNode(sgf.RepresentativeNodeId);
                    if (repNode != null && repNode.Ports.Count > 0)
                        decoFrame.BoundaryPorts = BuildBoundaryPorts(repNode, sgf.Bounds, viewModel);

                    frame.Decorations.Add(decoFrame);
                }
            }
        }

        protected List<PortFrame> BuildBoundaryPorts(Node repNode, Rect2 frameBounds,
            GraphViewModel viewModel)
        {
            var ports = new List<PortFrame>();
            float titleBarHeight = 24f;
            float portSpacing = viewModel.RenderConfig.Theme.PortSpacing;

            // 获取拖拽源端口（用于判断兼容性高亮）
            var dragHandler = viewModel.GetHandler<ConnectionDragHandler>();
            var dragSourcePort = dragHandler?.DragSourcePort;

            int inputIndex = 0, outputIndex = 0;
            foreach (var port in repNode.Ports)
            {
                int idx = port.Direction == PortDirection.Input ? inputIndex++ : outputIndex++;
                Vec2 pos = GetBoundaryPortPosition(port, frameBounds, idx, portSpacing, titleBarHeight);
                int edgeCount = viewModel.Graph.GetEdgeCountForPort(port.Id);

                // 判断是否可连接到拖拽源
                bool canConnect = false;
                if (dragSourcePort != null && port.Id != dragSourcePort.Id)
                {
                    var policy = viewModel.Graph.Settings.Behavior.ConnectionPolicy;
                    var result = policy.CanConnect(viewModel.Graph, dragSourcePort, port);
                    canConnect = result == ConnectionResult.Success;
                }

                bool isHovered = port.Id == viewModel.HoveredPortId;
                ports.Add(new PortFrame
                {
                    PortId = port.Id,
                    Position = pos,
                    Color = GetPortColor(port),
                    Connected = edgeCount > 0,
                    Name = port.Name,
                    Direction = port.Direction,
                    Kind = port.Kind,
                    Capacity = port.Capacity,
                    DataType = port.DataType,
                    ConnectedEdgeCount = edgeCount,
                    TotalSlots = 1,
                    Hovered = isHovered,
                    HoveredSlotIndex = isHovered ? 0 : -1,
                    CanConnectToDragSource = canConnect,
                    Shape = GetPortShape(port)
                });
            }
            return ports;
        }

        protected void BuildNodes(GraphFrame frame, GraphViewModel viewModel, Rect2 visibleRect,
            HashSet<string> hiddenNodeIds)
        {
            var graph = viewModel.Graph;
            var theme = viewModel.RenderConfig.Theme;

            foreach (var node in graph.Nodes)
            {
                if (hiddenNodeIds.Count > 0 && hiddenNodeIds.Contains(node.Id)) continue;

                var bounds = node.GetBounds();
                if (!visibleRect.Overlaps(bounds)) continue;

                bool selected = viewModel.Selection.IsSelected(node.Id);
                bool isPrimary = viewModel.Selection.PrimarySelectedNodeId == node.Id;

                var renderInfo = viewModel.GetNodeRenderInfo(node.TypeId);
                var titleColor = renderInfo.TitleColor;
                string displayName = renderInfo.DisplayName;

                if (node.TypeId == SubGraphConstants.BoundaryNodeTypeId)
                {
                    var ownerFrame = graph.FindContainerSubGraphFrame(node.Id);
                    if (ownerFrame != null)
                    {
                        displayName = ownerFrame.Title;
                        titleColor = SubGraphConstants.BoundaryNodeColor;
                    }
                }

                var nodeFrame = new NodeFrame
                {
                    NodeId = node.Id,
                    TypeId = node.TypeId,
                    Bounds = bounds,
                    TitleColor = titleColor,
                    TitleText = displayName,
                    Selected = selected,
                    IsPrimary = isPrimary,
                    DisplayMode = node.DisplayMode,
                    Description = node.UserData is IDescribableNode dn ? dn.Description : null
                };

                // 标记折叠状态的 Rep 节点（用于渲染展开按钮）
                if (node.TypeId == SubGraphConstants.BoundaryNodeTypeId)
                {
                    var ownerFrame2 = graph.FindContainerSubGraphFrame(node.Id);
                    if (ownerFrame2 != null && ownerFrame2.IsCollapsed)
                    {
                        nodeFrame.IsCollapsedSubGraph = true;
                        nodeFrame.SubGraphFrameId = ownerFrame2.Id;
                    }
                }

                // 诊断覆盖边框色（由宿主窗口根据分析结果写入）
                if (viewModel.NodeOverlayColors != null &&
                    viewModel.NodeOverlayColors.TryGetValue(node.Id, out var overlayColor))
                    nodeFrame.OverlayBorderColor = overlayColor;

                BuildPorts(nodeFrame, node, bounds, viewModel, theme);
                BuildNodeContent(nodeFrame, node, bounds, viewModel, theme, selected, isPrimary);

                frame.Nodes.Add(nodeFrame);
            }
        }

        protected void BuildPorts(NodeFrame nodeFrame, Node node, Rect2 bounds,
            GraphViewModel viewModel, NodeVisualTheme theme)
        {
            // 获取拖拽源端口（用于判断兼容性高亮）
            var dragHandler = viewModel.GetHandler<ConnectionDragHandler>();
            var dragSourcePort = dragHandler?.DragSourcePort;

            foreach (var port in node.Ports)
            {
                Vec2 pos = GetPortPosition(port, node, bounds, theme, viewModel);
                int edgeCount = viewModel.Graph.GetEdgeCountForPort(port.Id);

                // 判断是否可连接到拖拽源
                bool canConnect = false;
                if (dragSourcePort != null && port.Id != dragSourcePort.Id)
                {
                    var policy = viewModel.Graph.Settings.Behavior.ConnectionPolicy;
                    var result = policy.CanConnect(viewModel.Graph, dragSourcePort, port);
                    canConnect = result == ConnectionResult.Success;
                }

                bool isHovered = port.Id == viewModel.HoveredPortId;
                nodeFrame.Ports.Add(new PortFrame
                {
                    PortId = port.Id,
                    Position = pos,
                    Color = GetPortColor(port),
                    Connected = edgeCount > 0,
                    Name = port.Name,
                    Direction = port.Direction,
                    Kind = port.Kind,
                    Capacity = port.Capacity,
                    DataType = port.DataType,
                    ConnectedEdgeCount = edgeCount,
                    TotalSlots = GetPortSlotCount(port, viewModel),
                    Hovered = isHovered,
                    HoveredSlotIndex = isHovered ? viewModel.HoveredPortSlotIndex : -1,
                    CanConnectToDragSource = canConnect,
                    Shape = GetPortShape(port)
                });
            }
        }

        protected void BuildNodeContent(NodeFrame nodeFrame, Node node, Rect2 bounds,
            GraphViewModel viewModel, NodeVisualTheme theme, bool selected, bool isPrimary)
        {
            if (!viewModel.RenderConfig.ContentRenderers.TryGetValue(node.TypeId, out var renderer))
                return;

            int inputSlots = 0, outputSlots = 0;
            foreach (var p in node.Ports)
            {
                int slots = GetPortSlotCount(p, viewModel);
                if (p.Direction == PortDirection.Input) inputSlots += slots;
                else outputSlots += slots;
            }
            int maxSlots = System.Math.Max(inputSlots, outputSlots);

            float portAreaSize = maxSlots > 0 ? maxSlots * theme.PortSpacing : 0f;
            float descH = GetDescriptionBarHeight(node, theme);
            float contentTop = bounds.Y + theme.TitleBarHeight + descH + portAreaSize + 2f;
            var contentRect = new Rect2(
                bounds.X + theme.ContentPadding,
                contentTop,
                bounds.Width - theme.ContentPadding * 2f,
                bounds.Bottom - contentTop - 4f);

            if (contentRect.Height <= 4f) return;

            bool showEditor = selected && isPrimary && renderer.SupportsInlineEdit
                && node.DisplayMode == NodeDisplayMode.Expanded;

            NodeContentInfo contentInfo;
            if (showEditor)
            {
                contentInfo = new NodeContentInfo
                {
                    ContentRect = contentRect,
                    TypeId = node.TypeId,
                    ShowEditor = true,
                    Node = node,
                    HasSeparator = portAreaSize > 0f
                };
            }
            else
            {
                contentInfo = renderer.GetSummaryInfo(node, contentRect);
                contentInfo.ContentRect = contentRect;
                contentInfo.TypeId = node.TypeId;
                contentInfo.ShowEditor = false;
                contentInfo.Node = node;
                contentInfo.HasSeparator = portAreaSize > 0f;
            }

            nodeFrame.Content = contentInfo;
        }

        protected void BuildEdges(GraphFrame frame, GraphViewModel viewModel, Rect2 visibleRect,
            HashSet<string> hiddenNodeIds, Dictionary<string, Vec2> boundaryPortPositions)
        {
            var graph = viewModel.Graph;
            var theme = viewModel.RenderConfig.Theme;

            foreach (var edge in graph.Edges)
            {
                var sourcePort = graph.FindPort(edge.SourcePortId);
                var targetPort = graph.FindPort(edge.TargetPortId);
                if (sourcePort == null || targetPort == null) continue;

                var sourceNode = graph.FindNode(sourcePort.NodeId);
                var targetNode = graph.FindNode(targetPort.NodeId);
                if (sourceNode == null || targetNode == null) continue;

                // 隐藏节点的连线可见性判断：
                // - 隐藏节点的端口如果在 boundaryPortPositions 中，说明是展开状态的边界端口，仍需渲染
                // - 否则（折叠的内部节点）跳过
                if (hiddenNodeIds.Count > 0)
                {
                    bool srcHidden = hiddenNodeIds.Contains(sourceNode.Id);
                    bool tgtHidden = hiddenNodeIds.Contains(targetNode.Id);
                    bool srcHasBP = boundaryPortPositions.ContainsKey(edge.SourcePortId);
                    bool tgtHasBP = boundaryPortPositions.ContainsKey(edge.TargetPortId);

                    if ((srcHidden && !srcHasBP) || (tgtHidden && !tgtHasBP))
                        continue;
                }

                Vec2 start;
                if (boundaryPortPositions.TryGetValue(edge.SourcePortId, out var bpStart))
                    start = bpStart;
                else
                    start = GetPortPosition(sourcePort, sourceNode, sourceNode.GetBounds(), theme, viewModel);

                Vec2 end;
                if (boundaryPortPositions.TryGetValue(edge.TargetPortId, out var bpEnd))
                    end = bpEnd;
                else
                    end = GetEdgeTargetPosition(edge, targetPort, targetNode,
                        targetNode.GetBounds(), theme, viewModel);

                var edgeBounds = new Rect2(
                    MathF.Min(start.X, end.X) - 50f,
                    MathF.Min(start.Y, end.Y) - 50f,
                    MathF.Abs(end.X - start.X) + 100f,
                    MathF.Abs(end.Y - start.Y) + 100f);
                if (!visibleRect.Overlaps(edgeBounds)) continue;

                var (tA, tB) = ComputeEdgeRoute(start, end);
                bool selected = viewModel.Selection.IsEdgeSelected(edge.Id);

                Color4 color;
                float width;
                if (selected)
                {
                    color = theme.EdgeSelectedColor;
                    width = theme.EdgeSelectedWidth;
                }
                else
                {
                    color = GetPortColor(sourcePort);
                    width = sourcePort.Kind == PortKind.Data ? theme.DataEdgeWidth : theme.EdgeWidth;
                }

                var edgeFrame = new EdgeFrame
                {
                    EdgeId = edge.Id,
                    Start = start,
                    End = end,
                    TangentA = tA,
                    TangentB = tB,
                    Color = color,
                    Width = width,
                    Selected = selected
                };

                if (viewModel.RenderConfig.EdgeLabelRenderer != null && edge.UserData != null)
                {
                    Vec2 mid = BezierMath.Evaluate(start, start + tA, end + tB, end, 0.5f);
                    edgeFrame.Label = viewModel.RenderConfig.EdgeLabelRenderer.GetLabelInfo(edge, mid);
                }

                frame.Edges.Add(edgeFrame);
            }
        }

        // ══════════════════════════════════════
        //  共用工具方法
        // ══════════════════════════════════════

        protected float GetContentHeight(Node node, GraphViewModel viewModel)
        {
            if (!viewModel.RenderConfig.ContentRenderers.TryGetValue(node.TypeId, out var renderer))
                return 0f;

            // 选中节点处于编辑模式时，使用编辑器尺寸（包含所有可见属性）
            bool isEditorMode = viewModel.Selection.PrimarySelectedNodeId == node.Id
                && renderer.SupportsInlineEdit
                && node.DisplayMode == NodeDisplayMode.Expanded;

            if (isEditorMode)
            {
                // GetEditorSize 的 IEditContext 参数仅用于渲染时的布局，
                // 尺寸计算阶段不需要实际的 editCtx，传 null 安全
                var editorSize = renderer.GetEditorSize(node, null!);
                return editorSize.Y + 6f;
            }

            var summarySize = renderer.GetSummarySize(node, TextMeasurer);
            return summarySize.Y + 6f;
        }

        protected static PortShape GetPortShape(Port port)
        {
            return port.Kind switch
            {
                PortKind.Control => PortShape.Triangle,
                PortKind.Event   => PortShape.Diamond,
                _                => PortShape.Circle
            };
        }

        protected static Color4 GetPortColor(Port port)
        {
            // 优先根据 PortKind 返回颜色
            switch (port.Kind)
            {
                case PortKind.Control:
                    return Color4.Palette.ControlPort;  // 白色
                case PortKind.Event:
                    return Color4.Palette.EventPort;    // 橙色
                case PortKind.Data:
                    return Color4.Palette.DataPort;     // 蓝色
                default:
                    // 未知类型回退到数据类型判断
                    return port.DataType switch
                    {
                        "float" => Color4.Palette.FloatPort,
                        "int" => Color4.Palette.IntPort,
                        "string" => Color4.Palette.StringPort,
                        "bool" => Color4.Palette.BoolPort,
                        "entity" => Color4.Palette.EntityPort,
                        "any" => Color4.Palette.AnyPort,
                        _ => Color4.Palette.AnyPort
                    };
            }
        }
    }
}
