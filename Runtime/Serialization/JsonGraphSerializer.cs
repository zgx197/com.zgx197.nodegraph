#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Abstraction;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Serialization
{
    /// <summary>
    /// JSON 图序列化器。实现 IGraphSerializer，用于跨引擎导入导出和剪贴板。
    /// v2: Graph↔模型转换委托给 <see cref="DefaultGraphPersister"/>，
    ///     本类只负责 GraphDto ↔ JSON 字符串。
    /// </summary>
    public class JsonGraphSerializer : IGraphSerializer
    {
        private readonly IUserDataSerializer? _userDataSerializer;
        private readonly INodeTypeCatalog?    _typeProvider;
        private readonly IGraphPersister      _persister;

        /// <param name="userDataSerializer">节点/边业务数据序列化器</param>
        /// <param name="typeProvider">
        /// 节点类型提供者（S4）。非 null 时反序列化从 TypeDefinition 重建端口结构，
        /// 同时序列化会跳过非动态节点的端口元数据。
        /// </param>
        public JsonGraphSerializer(
            IUserDataSerializer? userDataSerializer = null,
            INodeTypeCatalog?    typeProvider       = null)
        {
            _userDataSerializer = userDataSerializer;
            _typeProvider       = typeProvider;
            _persister          = new DefaultGraphPersister();
        }

        // ══════════════════════════════════════
        //  序列化：Graph → JSON string
        // ══════════════════════════════════════

        public string Serialize(Graph graph)
        {
            var dto = _persister.Capture(graph, _userDataSerializer, _typeProvider);
            return SimpleJson.Serialize(dto);
        }

        public Graph Deserialize(string json)
        {
            var dto = SimpleJson.Deserialize<GraphDto>(json);
            if (dto == null)
                throw new InvalidOperationException("无法解析 JSON 数据");
            return _persister.Restore(dto, _userDataSerializer, _typeProvider);
        }

        public string SerializeSubGraph(Graph graph, IEnumerable<string> nodeIds)
        {
            var nodeIdSet = new HashSet<string>(nodeIds);
            // 局部子图：尝试全量快照下筛选指定节点
            var fullDto = _persister.Capture(graph, _userDataSerializer, _typeProvider);
            var sub = new GraphDto { id = "", schemaVersion = fullDto.schemaVersion, settings = fullDto.settings };
            sub.nodes.AddRange(fullDto.nodes.Where(n => nodeIdSet.Contains(n.id)));
            sub.edges.AddRange(fullDto.edges.Where(e =>
                nodeIdSet.Contains(e.fromNodeId) && nodeIdSet.Contains(e.toNodeId)));
            return SimpleJson.Serialize(sub);
        }

        public IEnumerable<Node> DeserializeSubGraphInto(Graph target, string data, Vec2 offset)
        {
            var sub = SimpleJson.Deserialize<GraphDto>(data);
            if (sub == null) return Enumerable.Empty<Node>();

            // 生成新 ID 映射
            var idMap = new Dictionary<string, string>();
            foreach (var nd in sub.nodes)
            {
                idMap[nd.id] = IdGenerator.NewId();
                foreach (var pd in nd.ports) idMap[pd.id] = IdGenerator.NewId();
            }
            foreach (var ed in sub.edges) idMap[ed.id] = IdGenerator.NewId();

            var newNodes = new List<Node>();
            foreach (var nd in sub.nodes)
            {
                Enum.TryParse<NodeDisplayMode>(nd.displayMode, out var displayMode);
                var node = new Node(idMap[nd.id], nd.typeId, new Vec2(nd.position.x, nd.position.y) + offset)
                {
                    Size              = new Vec2(nd.size.x, nd.size.y),
                    DisplayMode       = displayMode,
                    AllowDynamicPorts = nd.allowDynamicPorts
                };
                foreach (var pd in nd.ports)
                {
                    Enum.TryParse<PortDirection>(pd.direction, out var dir);
                    Enum.TryParse<PortKind>(pd.kind,      out var kind);
                    Enum.TryParse<PortCapacity>(pd.capacity,  out var cap);
                    var sem = string.IsNullOrEmpty(pd.semanticId) ? pd.name : pd.semanticId;
                    node.AddPortDirect(new Port(idMap[pd.id], node.Id, pd.name, dir, kind, pd.dataType, cap, pd.sortOrder, sem));
                }
                if (nd.userData != null && _userDataSerializer != null)
                    node.UserData = _userDataSerializer.DeserializeNodeData(nd.typeId, nd.userData);
                target.AddNodeDirect(node);
                newNodes.Add(node);
            }

            foreach (var ed in sub.edges)
            {
                string newFrom = idMap.TryGetValue(ed.fromNodeId, out var fn) ? fn : ed.fromNodeId;
                string newTo   = idMap.TryGetValue(ed.toNodeId,   out var tn) ? tn : ed.toNodeId;
                var src = FindPortBySemanticId(target, newFrom, ed.fromPortId);
                var tgt = FindPortBySemanticId(target, newTo,   ed.toPortId);
                if (src == null || tgt == null) continue;
                target.AddEdgeDirect(new Edge(idMap[ed.id], src.Id, tgt.Id));
            }
            return newNodes;
        }

        private static Port? FindPortBySemanticId(Graph graph, string nodeId, string semanticId)
        {
            var node = graph.FindNode(nodeId);
            if (node == null) return null;
            foreach (var p in node.Ports)
                if (p.SemanticId == semanticId) return p;
            return null;
        }
    }
}
