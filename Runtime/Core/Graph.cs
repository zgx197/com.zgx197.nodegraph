#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Math;

namespace NodeGraph.Core
{
    /// <summary>
    /// [框架核心模型] 图数据模型。管理节点、连线、分组、注释等所有图元素。
    /// </summary>
    /// <remarks>
    /// 这是低层 API，业务层应优先通过 <c>GraphViewModel</c> 操作。
    /// 直接调用 Graph 方法的场景：Command 实现内部、反序列化还原、单元测试。
    /// </remarks>
    public class Graph
    {
        private readonly List<Node> _nodes = new List<Node>();
        private readonly List<Edge> _edges = new List<Edge>();
        private readonly List<NodeGroup> _groups = new List<NodeGroup>();
        private readonly SubGraphIndex _subGraphIndex = new SubGraphIndex();
        private readonly List<GraphComment> _comments = new List<GraphComment>();

        // 快速查找表
        private readonly Dictionary<string, Node> _nodeMap = new Dictionary<string, Node>();
        private readonly Dictionary<string, Edge> _edgeMap = new Dictionary<string, Edge>();
        private readonly Dictionary<string, Port> _portMap = new Dictionary<string, Port>();
        private readonly Dictionary<string, List<Edge>> _portEdgeIndex = new Dictionary<string, List<Edge>>();

        /// <summary>图唯一 ID（GUID）</summary>
        public string Id { get; }

        /// <summary>图设置</summary>
        public GraphSettings Settings { get; }

        /// <summary>图事件</summary>
        public GraphEvents Events { get; }

        // ── 元素集合（只读） ──

        public IReadOnlyList<Node> Nodes => _nodes;
        public IReadOnlyList<Edge> Edges => _edges;
        public IReadOnlyList<NodeGroup> Groups => _groups;
        public IReadOnlyList<SubGraphFrame> SubGraphFrames => _subGraphIndex.All;

        /// <summary>子图框索引（供需要直接访问索引能力的调用方使用）</summary>
        public SubGraphIndex SubGraphIndex => _subGraphIndex;
        public IReadOnlyList<GraphComment> Comments => _comments;

        /// <summary>统一的容器迭代（FrameBuilder 用于生成 DecorationFrame）</summary>
        public IEnumerable<GraphContainer> AllContainers =>
            _groups.Cast<GraphContainer>().Concat(_subGraphIndex.All);

        // ── 构造 ──

        public Graph(GraphSettings? settings = null)
        {
            Id = IdGenerator.NewId();
            Settings = settings ?? new GraphSettings();
            Events = new GraphEvents();
        }

        /// <summary>内部构造：用于反序列化时指定 ID</summary>
        internal Graph(string id, GraphSettings? settings = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Settings = settings ?? new GraphSettings();
            Events = new GraphEvents();
        }

        // ═══════════════════════════════════════
        //  节点操作
        // ═══════════════════════════════════════

        /// <summary>
        /// 添加节点。根据 NodeTypeRegistry 中的定义创建节点和默认端口。
        /// </summary>
        /// <param name="typeId">节点类型 ID</param>
        /// <param name="position">画布位置</param>
        /// <returns>创建的节点；若类型未注册则返回 null</returns>
        public Node? AddNode(string typeId, Vec2 position)
        {
            if (typeId == null) throw new ArgumentNullException(nameof(typeId));

            var typeDef = Settings.NodeTypes.GetNodeType(typeId);

            // 生成唯一 ID（兜底重复检测）
            string id;
            do { id = IdGenerator.NewId(); } while (_nodeMap.ContainsKey(id));

            var node = new Node(id, typeId, position);

            // 如果有类型定义，根据定义初始化节点
            if (typeDef != null)
            {
                node.AllowDynamicPorts = typeDef.AllowDynamicPorts;

                // 创建默认端口
                foreach (var portDef in typeDef.DefaultPorts)
                {
                    node.AddPort(portDef);
                }

                // 创建默认业务数据
                if (typeDef.CreateDefaultData != null)
                {
                    node.UserData = typeDef.CreateDefaultData();
                }
            }

            _nodes.Add(node);
            _nodeMap[id] = node;
            RegisterNodePorts(node);
            Events.RaiseNodeAdded(node);
            return node;
        }

        /// <summary>
        /// 添加已构造好的节点（用于反序列化）。
        /// </summary>
        internal void AddNodeDirect(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            _nodes.Add(node);
            _nodeMap[node.Id] = node;
            RegisterNodePorts(node);
        }

        /// <summary>移除节点及其关联的所有连线</summary>
        public void RemoveNode(string nodeId)
        {
            if (nodeId == null) throw new ArgumentNullException(nameof(nodeId));
            if (!_nodeMap.TryGetValue(nodeId, out var node)) return;

            Events.RaiseNodeRemoved(node);

            // 通过端口索引快速收集关联连线
            var edgesToRemove = new List<Edge>();
            foreach (var port in node.Ports)
            {
                if (_portEdgeIndex.TryGetValue(port.Id, out var edges))
                {
                    foreach (var edge in edges)
                    {
                        if (!edgesToRemove.Contains(edge))
                            edgesToRemove.Add(edge);
                    }
                }
            }

            foreach (var edge in edgesToRemove)
            {
                RemoveEdgeInternal(edge);
            }

            // 从所有容器中移除
            foreach (var group in _groups)
                group.ContainedNodeIds.Remove(nodeId);
            _subGraphIndex.OnNodeRemoved(nodeId);

            UnregisterNodePorts(node);
            _nodes.Remove(node);
            _nodeMap.Remove(nodeId);
        }

        /// <summary>根据 ID 查找节点</summary>
        public Node? FindNode(string nodeId)
        {
            if (nodeId == null) return null;
            _nodeMap.TryGetValue(nodeId, out var node);
            return node;
        }

        // ═══════════════════════════════════════
        //  连线操作
        // ═══════════════════════════════════════

        /// <summary>
        /// 连接两个端口。自动纠正方向（确保 source=Output, target=Input）。
        /// 返回 ConnectResult，含新建的边和被顶替的旧边（Single 端口替换时非 null）。
        /// </summary>
        public ConnectResult Connect(string sourcePortId, string targetPortId)
        {
            if (sourcePortId == null) throw new ArgumentNullException(nameof(sourcePortId));
            if (targetPortId == null) throw new ArgumentNullException(nameof(targetPortId));

            var sourcePort = FindPort(sourcePortId);
            var targetPort = FindPort(targetPortId);
            if (sourcePort == null || targetPort == null) return ConnectResult.Rejected;

            // 判断是否为边界端口的内部连接（桥接模式）
            var sNode = FindNode(sourcePort.NodeId);
            var tNode = FindNode(targetPort.NodeId);
            bool sBoundary = sNode?.TypeId == SubGraphConstants.BoundaryNodeTypeId;
            bool tBoundary = tNode?.TypeId == SubGraphConstants.BoundaryNodeTypeId;
            bool isInternalBoundary = false;
            if (sBoundary || tBoundary)
            {
                var boundaryNode = sBoundary ? sNode! : tNode!;
                var otherNode = sBoundary ? tNode! : sNode!;
                var frame = _subGraphIndex.FindContainer(boundaryNode.Id);
                isInternalBoundary = frame != null && frame.ContainedNodeIds.Contains(otherNode.Id);
            }

            // 自动纠正方向（内部桥接连接跳过，外部连接正常纠正）
            if (isInternalBoundary)
            {
                // 边界端口内部桥接：不按方向纠正，保持用户拖拽的方向
            }
            else if (sourcePort.Direction == PortDirection.Input && targetPort.Direction == PortDirection.Output)
            {
                (sourcePort, targetPort) = (targetPort, sourcePort);
                (sourcePortId, targetPortId) = (targetPortId, sourcePortId);
            }

            // 连接验证
            var validationResult = Settings.Behavior.ConnectionPolicy.CanConnect(this, sourcePort, targetPort);
            if (validationResult != ConnectionResult.Success) return ConnectResult.Rejected;

            // 如果目标端口是 Single 容量且已有连接，先断开旧连接并记录（供调用方 Undo 使用）
            // 边界端口始终跳过（需同时承载外部+内部多条连线）
            bool targetIsBoundary = tBoundary || sBoundary;
            Edge? displacedEdge = null;
            if (targetPort.Capacity == PortCapacity.Single && !targetIsBoundary)
            {
                if (_portEdgeIndex.TryGetValue(targetPortId, out var existing) && existing.Count > 0)
                {
                    // 复制列表避免迭代中修改
                    foreach (var e in existing.ToList())
                    {
                        if (e.TargetPortId == targetPortId)
                        {
                            displacedEdge = e;
                            RemoveEdgeInternal(e);
                            break;
                        }
                    }
                }
            }

            // 创建连线
            string edgeId;
            do { edgeId = IdGenerator.NewId(); } while (_edgeMap.ContainsKey(edgeId));

            var edge = new Edge(edgeId, sourcePortId, targetPortId);
            _edges.Add(edge);
            _edgeMap[edgeId] = edge;
            AddEdgeToIndex(edge);
            Events.RaiseEdgeAdded(edge);
            return new ConnectResult(edge, displacedEdge);
        }

        /// <summary>断开指定连线</summary>
        public void Disconnect(string edgeId)
        {
            if (edgeId == null) throw new ArgumentNullException(nameof(edgeId));
            if (!_edgeMap.TryGetValue(edgeId, out var edge)) return;
            RemoveEdgeInternal(edge);
        }

        /// <summary>内部方法：添加已构造好的连线（用于反序列化）</summary>
        internal void AddEdgeDirect(Edge edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            _edges.Add(edge);
            _edgeMap[edge.Id] = edge;
            AddEdgeToIndex(edge);
        }

        /// <summary>内部方法：添加已构造好的分组（用于反序列化）</summary>
        internal void AddGroupDirect(NodeGroup group)
        {
            if (group == null) throw new ArgumentNullException(nameof(group));
            _groups.Add(group);
        }

        /// <summary>内部方法：添加已构造好的注释（用于反序列化）</summary>
        internal void AddCommentDirect(GraphComment comment)
        {
            if (comment == null) throw new ArgumentNullException(nameof(comment));
            _comments.Add(comment);
        }

        // ═══════════════════════════════════════
        //  查询
        // ═══════════════════════════════════════

        /// <summary>获取指定节点关联的所有连线</summary>
        public IEnumerable<Edge> GetEdgesForNode(string nodeId)
        {
            if (nodeId == null) return Enumerable.Empty<Edge>();
            var node = FindNode(nodeId);
            if (node == null) return Enumerable.Empty<Edge>();

            var result = new List<Edge>();
            foreach (var port in node.Ports)
            {
                if (_portEdgeIndex.TryGetValue(port.Id, out var edges))
                {
                    foreach (var e in edges)
                    {
                        if (!result.Contains(e))
                            result.Add(e);
                    }
                }
            }
            return result;
        }

        /// <summary>根据 ID 查找连线</summary>
        public Edge? FindEdge(string edgeId)
        {
            if (edgeId == null) return null;
            return _edgeMap.TryGetValue(edgeId, out var edge) ? edge : null;
        }

        /// <summary>根据 ID 移除连线（公共 API，供 Command 使用）</summary>
        public void RemoveEdge(string edgeId)
        {
            Disconnect(edgeId);
        }

        /// <summary>获取指定端口关联的所有连线（O(1) 查找）</summary>
        public IEnumerable<Edge> GetEdgesForPort(string portId)
        {
            if (portId == null) return Enumerable.Empty<Edge>();
            if (_portEdgeIndex.TryGetValue(portId, out var edges))
                return edges;
            return Enumerable.Empty<Edge>();
        }

        /// <summary>获取指定端口的连线数量（O(1)）</summary>
        public int GetEdgeCountForPort(string portId)
        {
            if (portId == null) return 0;
            if (_portEdgeIndex.TryGetValue(portId, out var edges))
                return edges.Count;
            return 0;
        }

        /// <summary>获取指定节点的所有后继节点</summary>
        public IEnumerable<Node> GetSuccessors(string nodeId)
        {
            if (nodeId == null) return Enumerable.Empty<Node>();

            var node = FindNode(nodeId);
            if (node == null) return Enumerable.Empty<Node>();

            var successorIds = new HashSet<string>();
            foreach (var port in node.GetOutputPorts())
            {
                foreach (var edge in GetEdgesForPort(port.Id))
                {
                    var targetPort = FindPort(edge.TargetPortId);
                    if (targetPort != null && targetPort.NodeId != nodeId)
                        successorIds.Add(targetPort.NodeId);
                }
            }

            return successorIds.Select(id => FindNode(id)!).Where(n => n != null);
        }

        /// <summary>获取指定节点的所有前驱节点</summary>
        public IEnumerable<Node> GetPredecessors(string nodeId)
        {
            if (nodeId == null) return Enumerable.Empty<Node>();

            var node = FindNode(nodeId);
            if (node == null) return Enumerable.Empty<Node>();

            var predecessorIds = new HashSet<string>();
            foreach (var port in node.GetInputPorts())
            {
                foreach (var edge in GetEdgesForPort(port.Id))
                {
                    var sourcePort = FindPort(edge.SourcePortId);
                    if (sourcePort != null && sourcePort.NodeId != nodeId)
                        predecessorIds.Add(sourcePort.NodeId);
                }
            }

            return predecessorIds.Select(id => FindNode(id)!).Where(n => n != null);
        }

        /// <summary>在图的所有节点中查找端口（O(1) 查找）</summary>
        public Port? FindPort(string portId)
        {
            if (portId == null) return null;
            _portMap.TryGetValue(portId, out var port);
            return port;
        }

        // ═══════════════════════════════════════
        //  装饰元素
        // ═══════════════════════════════════════

        /// <summary>创建分组</summary>
        public NodeGroup CreateGroup(string title, IEnumerable<string>? nodeIds = null)
        {
            if (title == null) throw new ArgumentNullException(nameof(title));

            var group = new NodeGroup(IdGenerator.NewId(), title);
            if (nodeIds != null)
            {
                foreach (var id in nodeIds)
                    group.ContainedNodeIds.Add(id);
            }

            _groups.Add(group);
            Events.RaiseGroupAdded(group);
            return group;
        }

        /// <summary>移除分组（不移除分组内的节点）</summary>
        public void RemoveGroup(string groupId)
        {
            if (groupId == null) throw new ArgumentNullException(nameof(groupId));
            var group = _groups.FirstOrDefault(g => g.Id == groupId);
            if (group == null) return;

            Events.RaiseGroupRemoved(group);
            _groups.Remove(group);
        }

        /// <summary>根据 ID 查找分组</summary>
        public NodeGroup? FindGroup(string groupId)
        {
            if (groupId == null) return null;
            return _groups.FirstOrDefault(g => g.Id == groupId);
        }

        /// <summary>创建注释</summary>
        public GraphComment CreateComment(string text, Vec2 position)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var comment = new GraphComment(IdGenerator.NewId(), text, position);

            _comments.Add(comment);
            Events.RaiseCommentAdded(comment);
            return comment;
        }

        /// <summary>移除注释</summary>
        public void RemoveComment(string commentId)
        {
            if (commentId == null) throw new ArgumentNullException(nameof(commentId));
            var comment = _comments.FirstOrDefault(c => c.Id == commentId);
            if (comment == null) return;

            Events.RaiseCommentRemoved(comment);
            _comments.Remove(comment);
        }

        /// <summary>根据 ID 查找注释</summary>
        public GraphComment? FindComment(string commentId)
        {
            if (commentId == null) return null;
            return _comments.FirstOrDefault(c => c.Id == commentId);
        }

        // ═══════════════════════════════════════
        //  子图框操作
        // ═══════════════════════════════════════

        /// <summary>内部方法：添加已构造好的子图框（供 Command 实现内部和反序列化使用）</summary>
        internal void AddSubGraphFrameDirect(SubGraphFrame frame) => _subGraphIndex.Add(frame);

        /// <summary>移除子图框（不移除框内的节点和代表节点）</summary>
        public void RemoveSubGraphFrame(string frameId) => _subGraphIndex.Remove(frameId);

        /// <summary>根据 ID 查找子图框</summary>
        public SubGraphFrame? FindSubGraphFrame(string frameId) => _subGraphIndex.FindById(frameId);

        /// <summary>查找包含指定节点的子图框</summary>
        public SubGraphFrame? FindContainerSubGraphFrame(string nodeId) => _subGraphIndex.FindContainer(nodeId);

        // ═══════════════════════════════════════
        //  内部辅助
        // ═══════════════════════════════════════

        private void RemoveEdgeInternal(Edge edge)
        {
            Events.RaiseEdgeRemoved(edge);
            RemoveEdgeFromIndex(edge);
            _edges.Remove(edge);
            _edgeMap.Remove(edge.Id);
        }

        /// <summary>查找端口所属节点的 ID（O(1)）</summary>
        private string? FindPortOwnerNodeId(string portId)
        {
            if (portId != null && _portMap.TryGetValue(portId, out var port))
                return port.NodeId;
            return null;
        }

        // ═══════════════════════════════════════
        //  索引维护
        // ═══════════════════════════════════════

        /// <summary>注册节点的所有端口到 _portMap，并订阅动态端口事件</summary>
        private void RegisterNodePorts(Node node)
        {
            foreach (var port in node.Ports)
                _portMap[port.Id] = port;
            node.OnPortAdded += OnPortAddedToNode;
            node.OnPortRemoved += OnPortRemovedFromNode;
        }

        /// <summary>注销节点的所有端口，并取消订阅动态端口事件</summary>
        private void UnregisterNodePorts(Node node)
        {
            node.OnPortAdded -= OnPortAddedToNode;
            node.OnPortRemoved -= OnPortRemovedFromNode;
            foreach (var port in node.Ports)
            {
                _portMap.Remove(port.Id);
                _portEdgeIndex.Remove(port.Id);
            }
        }

        private void OnPortAddedToNode(Port port) => _portMap[port.Id] = port;
        private void OnPortRemovedFromNode(Port port)
        {
            _portMap.Remove(port.Id);
            _portEdgeIndex.Remove(port.Id);
        }

        /// <summary>将边添加到端口→边索引</summary>
        private void AddEdgeToIndex(Edge edge)
        {
            AddToPortEdgeList(edge.SourcePortId, edge);
            AddToPortEdgeList(edge.TargetPortId, edge);
        }

        /// <summary>将边从端口→边索引移除</summary>
        private void RemoveEdgeFromIndex(Edge edge)
        {
            RemoveFromPortEdgeList(edge.SourcePortId, edge);
            RemoveFromPortEdgeList(edge.TargetPortId, edge);
        }

        private void AddToPortEdgeList(string portId, Edge edge)
        {
            if (!_portEdgeIndex.TryGetValue(portId, out var list))
            {
                list = new List<Edge>(2);
                _portEdgeIndex[portId] = list;
            }
            list.Add(edge);
        }

        private void RemoveFromPortEdgeList(string portId, Edge edge)
        {
            if (_portEdgeIndex.TryGetValue(portId, out var list))
                list.Remove(edge);
        }

        public override string ToString() => $"Graph({Id}: {_nodes.Count} nodes, {_edges.Count} edges)";
    }
}
