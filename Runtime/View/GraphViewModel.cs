#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Abstraction;
using NodeGraph.Commands;
using NodeGraph.Core;
using NodeGraph.Math;
using NodeGraph.View.Handlers;

namespace NodeGraph.View
{
    /// <summary>
    /// [框架核心模型] 图视图模型。是业务层与框架交互的主入口。
    /// </summary>
    /// <remarks>
    /// 常用 API：
    /// - <see cref="Commands"/>      — 命令历史（Execute / Undo / Redo）
    /// - <see cref="Selection"/>     — 选中状态管理
    /// - <see cref="BuildFrame"/>    — 每帧调用，输出 GraphFrame 渲染描述
    /// - <see cref="ProcessInput"/> — 将引擎库输入转化为框架输入后传入
    /// 引擎宿主窗口在其 Update/Draw 回调中驱动 ProcessInput/Update/BuildFrame。
    /// </remarks>
    public class GraphViewModel
    {
        private readonly List<IInteractionHandler> _handlers = new List<IInteractionHandler>();
        private Rect2 _viewport;

        // ── Multiple 端口目标槽位数（点击"+"后递增，ESC 重置）──
        // Key: portId, Value: 用户期望的最小槽位数
        private readonly Dictionary<string, int> _portTargetSlots = new Dictionary<string, int>();

        // ══════════════════════════════════════
        //  公开属性
        // ══════════════════════════════════════

        /// <summary>当前正在编辑的图</summary>
        public Graph Graph { get; private set; }

        /// <summary>画布平移偏移（屏幕像素）</summary>
        public Vec2 PanOffset { get; set; }

        /// <summary>缩放级别（1.0 = 100%）</summary>
        public float ZoomLevel { get; set; } = 1f;

        /// <summary>最小缩放</summary>
        public float MinZoom { get; set; } = 0.1f;

        /// <summary>最大缩放</summary>
        public float MaxZoom { get; set; } = 3.0f;

        /// <summary>帧 ID，每帧递增，用于帧内缓存失效判断</summary>
        public int FrameId { get; private set; }

        /// <summary>选中状态管理器</summary>
        public SelectionManager Selection { get; }

        /// <summary>命令历史（Undo/Redo）</summary>
        public CommandHistory Commands { get; }

        /// <summary>快捷键管理器</summary>
        public KeyBindingManager KeyBindings { get; }

        /// <summary>是否需要重绘（由交互处理器设置）</summary>
        public bool NeedsRepaint { get; private set; }

        /// <summary>渲染配置（帧构建器、主题、标签渲染器、内容渲染器）</summary>
        public GraphRenderConfig RenderConfig { get; }

        /// <summary>当前悬停的端口 ID（用于渲染高亮效果，每帧更新）</summary>
        public string? HoveredPortId { get; set; }

        /// <summary>节点诊断覆盖颜色（节点 ID → 边框颜色）。由宿主窗口根据分析结果写入，null 表示无覆盖。</summary>
        public Dictionary<string, Color4>? NodeOverlayColors { get; set; }

        /// <summary>当前悬停的槽位索引（Multiple 端口用，-1 表示非 Multiple 或无悬停）</summary>
        public int HoveredPortSlotIndex { get; set; } = -1;

        /// <summary>
        /// 节点类型目录（由宿主在创建 Profile 后注入）。
        /// View 层通过 <see cref="GetNodeRenderInfo"/> 读取，不直接访问 Graph.Settings.NodeTypes。
        /// </summary>
        public INodeTypeCatalog? NodeTypeCatalog
        {
            get => _nodeTypeCatalog;
            set { _nodeTypeCatalog = value; _renderInfoCache.Clear(); }
        }
        private INodeTypeCatalog? _nodeTypeCatalog;
        private readonly Dictionary<string, NodeRenderInfo> _renderInfoCache = new();

        /// <summary>获取节点渲染信息（带缓存）。无类型定义时返回默认值。</summary>
        public NodeRenderInfo GetNodeRenderInfo(string typeId)
        {
            if (_renderInfoCache.TryGetValue(typeId, out var cached)) return cached;
            var info = _nodeTypeCatalog != null
                ? NodeRenderInfo.FromDefinition(_nodeTypeCatalog.GetNodeType(typeId), typeId)
                : new NodeRenderInfo(typeId, new Color4(0.35f, 0.35f, 0.35f, 1f));
            _renderInfoCache[typeId] = info;
            return info;
        }

        // ── 上下文菜单事件（宿主窗口订阅，框架层 Handler 触发）──

        /// <summary>
        /// 右键点击画布空白区域时触发。参数为画布坐标。
        /// 宿主窗口订阅此事件以显示平台原生的"添加节点"菜单。
        /// </summary>
        public Action<Vec2>? OnContextMenuRequested { get; set; }

        /// <summary>
        /// 右键点击节点时触发。参数为 (节点, 画布坐标)。
        /// 宿主窗口订阅此事件以显示节点上下文菜单（删除、复制等）。
        /// </summary>
        public Action<Node, Vec2>? OnNodeContextMenuRequested { get; set; }

        /// <summary>
        /// 右键点击端口时触发。参数为 (端口, 画布坐标)。
        /// 宿主窗口订阅此事件以显示端口连线管理菜单。
        /// </summary>
        public Action<Port, Vec2>? OnPortContextMenuRequested { get; set; }

        // ══════════════════════════════════════
        //  构造
        // ══════════════════════════════════════

        public GraphViewModel(Graph graph, GraphRenderConfig? renderConfig = null)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            RenderConfig = renderConfig ?? new GraphRenderConfig();
            Selection = new SelectionManager();
            Commands = new CommandHistory(graph);
            KeyBindings = new KeyBindingManager();
            KeyBindings.RegisterDefaults();

            // 注册默认交互处理器
            AddHandler(new PanZoomController());
            AddHandler(new DecorationInteractionHandler());
            AddHandler(new ConnectionDragHandler());
            AddHandler(new NodeDragHandler());
            AddHandler(new MarqueeSelectionHandler());
            AddHandler(new ContextMenuHandler());
        }

        // ══════════════════════════════════════
        //  交互处理器管理
        // ══════════════════════════════════════

        /// <summary>添加交互处理器</summary>
        public void AddHandler(IInteractionHandler handler)
        {
            _handlers.Add(handler);
            _handlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        /// <summary>移除交互处理器</summary>
        public bool RemoveHandler(IInteractionHandler handler) => _handlers.Remove(handler);

        /// <summary>获取指定类型的处理器</summary>
        public T? GetHandler<T>() where T : class, IInteractionHandler =>
            _handlers.OfType<T>().FirstOrDefault();

        // ══════════════════════════════════════
        //  主循环入口（引擎宿主窗口驱动）
        // ══════════════════════════════════════

        /// <summary>处理输入（每帧由引擎宿主调用）</summary>
        public void ProcessInput(IPlatformInput input)
        {
            // 递增帧 ID，用于帧内缓存失效
            FrameId++;

            // 重置重绘标记（处理器在下面的 HandleInput 中可重新设置）
            NeedsRepaint = false;

            // 更新悬停端口（每帧轻量级检测，用于渲染高亮）
            var mouseCanvas = ScreenToCanvas(input.MousePosition);
            var hoveredPort = HitTestPort(mouseCanvas, 14f);
            var newHoveredId = hoveredPort?.Id;
            int newSlotIndex = -1;

            // Multiple Input 端口：确定悬停的具体槽位
            if (hoveredPort != null
                && hoveredPort.Direction == PortDirection.Input
                && hoveredPort.Capacity == PortCapacity.Multiple)
            {
                var node = Graph.FindNode(hoveredPort.NodeId);
                if (node != null)
                {
                    var slotPositions = GetMultiplePortSlotPositions(hoveredPort, node);
                    float hitRadiusSq = 14f * 14f;
                    for (int si = 0; si < slotPositions.Count; si++)
                    {
                        if (Vec2.DistanceSquared(mouseCanvas, slotPositions[si]) <= hitRadiusSq)
                        {
                            newSlotIndex = si;
                            break;
                        }
                    }
                }
            }

            if (newHoveredId != HoveredPortId || newSlotIndex != HoveredPortSlotIndex)
            {
                HoveredPortId = newHoveredId;
                HoveredPortSlotIndex = newSlotIndex;
                NeedsRepaint = true; // 悬停状态变化 → 触发重绘
            }

            // 连线流动动画需要持续重绘
            if (Graph.Edges.Count > 0)
                NeedsRepaint = true;

            // 快捷键处理
            ProcessKeyBindings(input);

            // 交互处理器链（按优先级）
            foreach (var handler in _handlers)
            {
                if (handler.HandleInput(this, input))
                    break; // 事件已消费
            }
        }

        /// <summary>
        /// 预更新节点尺寸（在 ProcessInput 之前调用）。
        /// 确保命中检测使用的端口位置与当前帧渲染一致。
        /// </summary>
        public void PreUpdateNodeSizes()
        {
            foreach (var node in Graph.Nodes)
                node.Size = RenderConfig.FrameBuilder.ComputeNodeSize(node, this);
        }

        /// <summary>更新状态（动画等，每帧由引擎宿主调用）</summary>
        public void Update(float deltaTime)
        {
            // 预留：后续可添加动画插值等
        }

        /// <summary>
        /// 构建渲染帧（每帧由引擎宿主调用）。
        /// 输出纯数据的 GraphFrame，由引擎原生渲染器消费绘制。
        /// </summary>
        public GraphFrame BuildFrame(Rect2 viewport)
        {
            _viewport = viewport;
            // NeedsRepaint 已在 ProcessInput 开头重置，此处不再清除，
            // 确保处理器的重绘请求能存活到窗口代码检查

            // 委托给 FrameBuilder 构建核心内容
            var frame = RenderConfig.FrameBuilder.BuildFrame(this, viewport);

            // 收集交互覆盖层
            foreach (var handler in _handlers)
            {
                var overlay = handler.GetOverlay(this);
                if (overlay != null)
                    frame.Overlays.Add(overlay);
            }

            return frame;
        }

        /// <summary>请求下一帧重绘</summary>
        public void RequestRepaint() => NeedsRepaint = true;

        // ══════════════════════════════════════
        //  坐标转换
        // ══════════════════════════════════════

        /// <summary>屏幕坐标 → 画布坐标</summary>
        public Vec2 ScreenToCanvas(Vec2 screenPos) =>
            (screenPos - PanOffset) / ZoomLevel;

        /// <summary>画布坐标 → 屏幕坐标</summary>
        public Vec2 CanvasToScreen(Vec2 canvasPos) =>
            canvasPos * ZoomLevel + PanOffset;

        /// <summary>获取当前可见的画布区域</summary>
        public Rect2 GetVisibleCanvasRect()
        {
            Vec2 topLeft = ScreenToCanvas(_viewport.Position);
            Vec2 bottomRight = ScreenToCanvas(_viewport.Position + _viewport.Size);
            return new Rect2(topLeft, bottomRight - topLeft);
        }

        // ══════════════════════════════════════
        //  命中测试
        // ══════════════════════════════════════

        /// <summary>命中测试节点（返回最上层命中的节点，跳过隐藏节点）</summary>
        public Node? HitTestNode(Vec2 canvasPos)
        {
            // 从后向前遍历（后绘制的在上层）
            for (int i = Graph.Nodes.Count - 1; i >= 0; i--)
            {
                var node = Graph.Nodes[i];
                if (node.GetBounds().Contains(canvasPos) && !IsNodeHidden(node))
                    return node;
            }
            return null;
        }

        /// <summary>
        /// 判断节点是否被隐藏（不可见不可交互）。
        /// - 展开状态的框：RepresentativeNode 隐藏
        /// - 折叠状态的框：内部节点隐藏
        /// </summary>
        private bool IsNodeHidden(Node node)
        {
            foreach (var sgf in Graph.SubGraphFrames)
            {
                if (sgf.IsCollapsed)
                {
                    // 折叠时：内部节点隐藏
                    if (sgf.ContainedNodeIds.Contains(node.Id))
                        return true;
                }
                else
                {
                    // 展开时：RepresentativeNode 隐藏
                    if (sgf.RepresentativeNodeId == node.Id)
                        return true;
                }
            }
            return false;
        }

        /// <summary>命中测试端口（Multiple Input 端口检测所有槽位，跳过隐藏节点，但检测展开状态的边界端口）</summary>
        public Port? HitTestPort(Vec2 canvasPos, float hitRadius = 12f)
        {
            float hitRadiusSq = hitRadius * hitRadius;

            // 第一遍：检测可见节点的端口
            for (int i = Graph.Nodes.Count - 1; i >= 0; i--)
            {
                var node = Graph.Nodes[i];
                if (IsNodeHidden(node)) continue;
                foreach (var port in node.Ports)
                {
                    bool isMultipleInput = port.Direction == PortDirection.Input
                                           && port.Capacity == PortCapacity.Multiple;
                    if (isMultipleInput)
                    {
                        // Multiple Input 端口：检测每个槽位（圆圈 + "+"位置）
                        var slotPositions = GetMultiplePortSlotPositions(port, node);
                        foreach (var slotPos in slotPositions)
                        {
                            if (Vec2.DistanceSquared(canvasPos, slotPos) <= hitRadiusSq)
                                return port;
                        }
                    }
                    else
                    {
                        Vec2 portPos = GetPortPosition(port);
                        if (Vec2.DistanceSquared(canvasPos, portPos) <= hitRadiusSq)
                            return port;
                    }
                }
            }

            // 第二遍：检测展开状态的 SubGraphFrame 边界端口（Rep 节点隐藏但端口在框边缘可交互）
            foreach (var sgf in Graph.SubGraphFrames)
            {
                if (sgf.IsCollapsed) continue;
                var repNode = Graph.FindNode(sgf.RepresentativeNodeId);
                if (repNode == null) continue;

                foreach (var port in repNode.Ports)
                {
                    Vec2 edgePos = GetBoundaryPortEdgePosition(port, repNode, sgf);
                    if (Vec2.DistanceSquared(canvasPos, edgePos) <= hitRadiusSq)
                        return port;
                }
            }

            return null;
        }

        /// <summary>命中测试"+"槽位（仅 Multiple Input 端口，返回是否点击了"+"位置）</summary>
        public bool HitTestPlusSlot(Vec2 canvasPos, Port port, float hitRadius = 12f)
        {
            if (port.Direction != PortDirection.Input || port.Capacity != PortCapacity.Multiple)
                return false;

            var node = Graph.FindNode(port.NodeId);
            if (node == null) return false;

            var slotPositions = GetMultiplePortSlotPositions(port, node);
            int edgeCount = Graph.GetEdgeCountForPort(port.Id);

            // "+"槽位是 edgeCount 之后的位置（最后一个始终是"+"，展开时倒数第二个也是空圆圈）
            float hitRadiusSq = hitRadius * hitRadius;
            int lastPlusIndex = slotPositions.Count - 1;
            if (lastPlusIndex >= 0 && Vec2.DistanceSquared(canvasPos, slotPositions[lastPlusIndex]) <= hitRadiusSq)
                return true;

            return false;
        }

        /// <summary>计算 Multiple Input 端口所有槽位的画布位置</summary>
        private List<Vec2> GetMultiplePortSlotPositions(Port port, Node node)
        {
            var bounds = node.GetBounds();
            var theme  = RenderConfig.Theme;

            // 计算当前端口之前的累积槽位偏移
            float slotOffsetBefore = 0f;
            int totalSlots = 0;
            foreach (var p in node.Ports)
            {
                if (p.Direction != port.Direction) continue;
                int slots = RenderConfig.FrameBuilder.GetPortSlotCount(p, this);
                if (p.Id == port.Id)
                {
                    totalSlots = slots;
                    break;
                }
                slotOffsetBefore += slots;
            }

            var positions = new List<Vec2>(totalSlots);
            for (int i = 0; i < totalSlots; i++)
            {
                float y = bounds.Y + theme.TitleBarHeight + theme.PortSpacing * (slotOffsetBefore + i + 0.5f);
                positions.Add(new Vec2(bounds.X + theme.PortInset, y));
            }
            return positions;
        }

        /// <summary>命中测试连线（检查点到贝塞尔曲线的距离，跳过隐藏节点的边）</summary>
        public Edge? HitTestEdge(Vec2 canvasPos, float hitDistance = 8f)
        {
            foreach (var edge in Graph.Edges)
            {
                var sourcePort = Graph.FindPort(edge.SourcePortId);
                var targetPort = Graph.FindPort(edge.TargetPortId);
                if (sourcePort == null || targetPort == null) continue;

                var sourceNode = Graph.FindNode(sourcePort.NodeId);
                var targetNode = Graph.FindNode(targetPort.NodeId);
                if (sourceNode == null || targetNode == null) continue;

                // 跳过任一端点为隐藏节点的连线
                if (IsNodeHidden(sourceNode) || IsNodeHidden(targetNode))
                    continue;

                Vec2 start = GetPortPosition(sourcePort);
                // Multiple Input 端口：使用每条边各自的槽位位置
                Vec2 end = RenderConfig.FrameBuilder.GetEdgeTargetPosition(edge, targetPort, targetNode,
                    targetNode.GetBounds(), RenderConfig.Theme, this);
                var (tA, tB) = BezierMath.ComputePortTangents(start, end, true);

                float dist = BezierMath.DistanceToPoint(
                    start, start + tA, end + tB, end, canvasPos);

                if (dist <= hitDistance)
                    return edge;
            }
            return null;
        }

        /// <summary>
        /// 命中测试子图框的折叠按钮区域（标题栏左侧 ▼/▶ 图标区域）。
        /// 返回被点击的 SubGraphFrame，或 null。
        /// </summary>
        public SubGraphFrame? HitTestSubGraphCollapseButton(Vec2 canvasPos)
        {
            foreach (var sgf in Graph.SubGraphFrames)
            {
                if (sgf.IsCollapsed) continue; // 折叠状态下由 Rep 节点接管
                // 折叠按钮在标题栏左侧，约 24x24 区域
                var buttonRect = new Rect2(
                    sgf.Bounds.X, sgf.Bounds.Y,
                    sgf.Bounds.Width, 24f); // 整个标题栏可点击
                if (buttonRect.Contains(canvasPos))
                    return sgf;
            }
            return null;
        }

        /// <summary>
        /// 命中测试子图框标题栏（展开和折叠状态均可命中）。
        /// 展开时用于拖动和折叠切换，折叠时用于展开切换。
        /// </summary>
        public SubGraphFrame? HitTestSubGraphFrameTitleBar(Vec2 canvasPos)
        {
            foreach (var sgf in Graph.SubGraphFrames)
            {
                var titleBar = new Rect2(
                    sgf.Bounds.X, sgf.Bounds.Y,
                    sgf.Bounds.Width, 24f);
                if (titleBar.Contains(canvasPos))
                    return sgf;
            }
            return null;
        }

        // ══════════════════════════════════════
        //  端口位置计算
        // ══════════════════════════════════════

        /// <summary>
        /// 获取端口在画布坐标系的位置。
        /// 对展开状态的 SubGraphFrame 边界端口返回框边缘位置；其他端口委托给 FrameBuilder。
        /// </summary>
        public Vec2 GetPortPosition(Port port)
        {
            var node = Graph.FindNode(port.NodeId);
            if (node == null) return Vec2.Zero;

            // 边界端口特殊处理：展开状态下返回框边缘位置
            if (node.TypeId == SubGraphConstants.BoundaryNodeTypeId)
            {
                var frame = Graph.FindContainerSubGraphFrame(node.Id);
                if (frame != null && !frame.IsCollapsed)
                    return GetBoundaryPortEdgePosition(port, node, frame);
            }

            return RenderConfig.FrameBuilder.GetPortPosition(port, node, node.GetBounds(), RenderConfig.Theme, this);
        }

        /// <summary>计算边界端口在框边缘的位置（与 BaseFrameBuilder 一致）</summary>
        private Vec2 GetBoundaryPortEdgePosition(Port port, Node repNode, SubGraphFrame frame)
        {
            float titleBarHeight = 24f;
            float portSpacing = RenderConfig.Theme.PortSpacing;

            // 计算该端口在同方向端口中的索引
            int index = 0;
            foreach (var p in repNode.Ports)
            {
                if (p.Id == port.Id) break;
                if (p.Direction == port.Direction) index++;
            }

            float offset = titleBarHeight + portSpacing * (index + 0.5f);
            return port.Direction == PortDirection.Input
                ? new Vec2(frame.Bounds.X,    frame.Bounds.Y + offset)
                : new Vec2(frame.Bounds.Right, frame.Bounds.Y + offset);
        }

        /// <summary>检查端口是否有连线</summary>
        public bool IsPortConnected(Port port)
        {
            return Graph.GetEdgesForPort(port.Id).Any();
        }

        // ══════════════════════════════════════
        //  Multiple 端口展开管理
        // ══════════════════════════════════════

        /// <summary>获取端口的用户目标槽位数（0 表示未设置，使用默认值）</summary>
        public int GetPortTargetSlots(string portId) =>
            _portTargetSlots.TryGetValue(portId, out int target) ? target : 0;

        /// <summary>检查端口是否处于展开状态（目标槽位数超出最低需求）</summary>
        public bool IsPortExpanded(string portId)
        {
            if (!_portTargetSlots.TryGetValue(portId, out int target)) return false;
            int edgeCount = Graph.GetEdgeCountForPort(portId);
            int minSlots = System.Math.Max(edgeCount + 1, 2);
            return target > minSlots;
        }

        /// <summary>
        /// 展开 Multiple 端口（目标槽位数 +1）。
        /// 实际槽位 = Max(targetSlots, edgeCount + 1, 2)，
        /// 连接消耗空位但不增长额外圆圈。
        /// </summary>
        public void ExpandMultiplePort(string portId)
        {
            int edgeCount = Graph.GetEdgeCountForPort(portId);
            int minSlots = System.Math.Max(edgeCount + 1, 2);
            int currentTarget = GetPortTargetSlots(portId);
            int effectiveSlots = System.Math.Max(currentTarget, minSlots);
            _portTargetSlots[portId] = effectiveSlots + 1;
            RequestRepaint();
        }

        /// <summary>收起 Multiple 端口（重置目标槽位数为默认）</summary>
        public void CollapseMultiplePort(string portId)
        {
            if (_portTargetSlots.Remove(portId))
                RequestRepaint();
        }

        /// <summary>清除所有 Multiple 端口的目标槽位数</summary>
        public void CollapseAllMultiplePorts()
        {
            if (_portTargetSlots.Count > 0)
            {
                _portTargetSlots.Clear();
                RequestRepaint();
            }
        }

        // ══════════════════════════════════════
        //  聚焦
        // ══════════════════════════════════════

        /// <summary>聚焦到指定节点集合（调整平移和缩放使所有节点可见）</summary>
        public void FocusNodes(IEnumerable<string> nodeIds, float padding = 50f)
        {
            var rects = nodeIds
                .Select(id => Graph.FindNode(id))
                .Where(n => n != null)
                .Select(n => n!.GetBounds());

            var bounds = Rect2.Encapsulate(rects);
            if (bounds.Width <= 0 && bounds.Height <= 0) return;

            bounds = bounds.Expand(padding);
            FocusOnRect(bounds);
        }

        /// <summary>聚焦到所有节点</summary>
        public void FocusAll(float padding = 50f)
        {
            FocusNodes(Graph.Nodes.Select(n => n.Id), padding);
        }

        /// <summary>聚焦到选中的节点</summary>
        public void FocusSelected(float padding = 50f)
        {
            if (Selection.SelectedNodeIds.Count == 0)
            {
                FocusAll(padding);
                return;
            }
            FocusNodes(Selection.SelectedNodeIds, padding);
        }

        private void FocusOnRect(Rect2 canvasRect)
        {
            if (_viewport.Width <= 0 || _viewport.Height <= 0) return;

            float scaleX = _viewport.Width / canvasRect.Width;
            float scaleY = _viewport.Height / canvasRect.Height;
            float newZoom = MathF.Min(scaleX, scaleY);
            newZoom = System.Math.Clamp(newZoom, MinZoom, MaxZoom);

            ZoomLevel = newZoom;
            PanOffset = new Vec2(
                _viewport.Width * 0.5f - canvasRect.Center.X * newZoom,
                _viewport.Height * 0.5f - canvasRect.Center.Y * newZoom);

            RequestRepaint();
        }

        // ══════════════════════════════════════
        //  快捷键处理
        // ══════════════════════════════════════

        private void ProcessKeyBindings(IPlatformInput input)
        {
            if (KeyBindings.IsActionTriggered("undo", input))
            {
                Commands.Undo();
                RequestRepaint();
            }
            else if (KeyBindings.IsActionTriggered("redo", input))
            {
                Commands.Redo();
                RequestRepaint();
            }
            else if (KeyBindings.IsActionTriggered("delete", input))
            {
                DeleteSelected();
            }
            else if (KeyBindings.IsActionTriggered("select_all", input))
            {
                Selection.SelectAll(Graph.Nodes.Select(n => n.Id));
                RequestRepaint();
            }
            else if (KeyBindings.IsActionTriggered("focus_selected", input))
            {
                FocusSelected();
            }
            else if (KeyBindings.IsActionTriggered("focus_all", input))
            {
                FocusAll();
            }
            else if (KeyBindings.IsActionTriggered("collapse", input))
            {
                ToggleCollapseSelected();
            }
            else if (KeyBindings.IsActionTriggered("create_group", input))
            {
                if (Selection.SelectedNodeIds.Count > 0)
                    Commands.Execute(new CreateGroupCommand("新分组", Selection.SelectedNodeIds));
                RequestRepaint();
            }
            // "back" 快捷键预留给未来 SubGraphFrame 的折叠/取消操作
        }

        /// <summary>删除选中的节点和连线</summary>
        public void DeleteSelected()
        {
            if (!Selection.HasSelection) return;

            using (Commands.BeginCompound("删除选中"))
            {
                // 先删除选中的连线
                foreach (var edgeId in Selection.SelectedEdgeIds.ToList())
                    Commands.Execute(new DisconnectCommand(edgeId));

                // 再删除选中的节点
                foreach (var nodeId in Selection.SelectedNodeIds.ToList())
                {
                    // 如果选中的是 SubGraphFrame 的 RepresentativeNode，执行 Ungroup 而非删除
                    var frame = Graph.FindContainerSubGraphFrame(nodeId);
                    if (frame != null && frame.RepresentativeNodeId == nodeId)
                    {
                        Commands.Execute(new UngroupSubGraphCommand(frame.Id));
                    }
                    else
                    {
                        Commands.Execute(new RemoveNodeCommand(nodeId));
                    }
                }
            }

            Selection.ClearSelection();
            RequestRepaint();
        }

        /// <summary>切换选中节点的折叠/展开</summary>
        private void ToggleCollapseSelected()
        {
            using (Commands.BeginCompound("切换折叠"))
            {
                foreach (var nodeId in Selection.SelectedNodeIds)
                {
                    var node = Graph.FindNode(nodeId);
                    if (node == null) continue;

                    var newMode = node.DisplayMode == NodeDisplayMode.Expanded
                        ? NodeDisplayMode.Collapsed
                        : NodeDisplayMode.Expanded;

                    Commands.Execute(new ChangeDisplayModeCommand(nodeId, newMode));
                }
            }
            RequestRepaint();
        }

    }
}
