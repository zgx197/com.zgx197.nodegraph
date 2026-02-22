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
    /// [框架内置实现] 默认图持久器。实现 <see cref="IGraphPersister"/>。
    /// </summary>
    /// <remarks>
    /// 负责 <see cref="Graph"/>（内存核心模型）与 <see cref="GraphDto"/> 之间的双向转换，
    /// 与具体存储格式（JSON / SO）完全解耦。
    /// 关键方法：
    /// - <see cref="Capture"/>                 — Graph → GraphDto
    /// - <see cref="Restore"/>                 — GraphDto → Graph（静默跳过失效连线）
    /// - <see cref="RestoreWithDiagnostics"/>  — GraphDto → <see cref="RestoreResult"/>（返回快照序列化跳过信息）
    /// </remarks>
    internal sealed class DefaultGraphPersister : IGraphPersister
    {
        // ══════════════════════════════════════
        //  Graph → GraphDto
        // ══════════════════════════════════════

        public GraphDto Capture(Graph graph,
            IUserDataSerializer? userDataSerializer = null,
            INodeTypeCatalog?   typeProvider       = null)
        {
            var dto = new GraphDto
            {
                id            = graph.Id,
                schemaVersion = 2,
                settings      = new GraphSettingsDto { topology = graph.Settings.Topology.ToString() }
            };

            foreach (var node in graph.Nodes)
                dto.nodes.Add(NodeToDto(node, userDataSerializer, typeProvider));

            foreach (var edge in graph.Edges)
                dto.edges.Add(EdgeToDto(edge, graph));

            foreach (var group in graph.Groups)
            {
                var gd = new GroupDto
                {
                    id       = group.Id,
                    title    = group.Title,
                    position = V(new Vec2(group.Bounds.X, group.Bounds.Y)),
                    size     = V(new Vec2(group.Bounds.Width, group.Bounds.Height)),
                    color    = C(group.Color)
                };
                gd.nodeIds.AddRange(group.ContainedNodeIds);
                dto.groups.Add(gd);
            }

            foreach (var comment in graph.Comments)
            {
                dto.comments.Add(new CommentDto
                {
                    id              = comment.Id,
                    text            = comment.Text,
                    position        = V(new Vec2(comment.Bounds.X, comment.Bounds.Y)),
                    size            = V(new Vec2(comment.Bounds.Width, comment.Bounds.Height)),
                    fontSize        = comment.FontSize,
                    textColor       = C(comment.TextColor),
                    backgroundColor = C(comment.BackgroundColor)
                });
            }

            foreach (var sgf in graph.SubGraphFrames)
            {
                var sd = new SubGraphFrameDto
                {
                    id                   = sgf.Id,
                    title                = sgf.Title,
                    position             = V(new Vec2(sgf.Bounds.X, sgf.Bounds.Y)),
                    size                 = V(new Vec2(sgf.Bounds.Width, sgf.Bounds.Height)),
                    representativeNodeId = sgf.RepresentativeNodeId,
                    isCollapsed          = sgf.IsCollapsed,
                    sourceAssetId        = sgf.SourceAssetId
                };
                sd.nodeIds.AddRange(sgf.ContainedNodeIds);
                dto.subGraphFrames.Add(sd);
            }

            return dto;
        }

        private NodeDto NodeToDto(Node node, IUserDataSerializer? udSer, INodeTypeCatalog? typeProv)
        {
            var nd = new NodeDto
            {
                id               = node.Id,
                typeId           = node.TypeId,
                position         = V(node.Position),
                size             = V(node.Size),
                displayMode      = node.DisplayMode.ToString(),
                allowDynamicPorts = node.AllowDynamicPorts
            };

            bool skipPorts = typeProv != null
                && !node.AllowDynamicPorts
                && typeProv.GetNodeType(node.TypeId) != null;

            if (!skipPorts)
            {
                foreach (var port in node.Ports)
                {
                    nd.ports.Add(new PortDto
                    {
                        id         = port.Id,
                        name       = port.Name,
                        semanticId = port.SemanticId,
                        direction  = port.Direction.ToString(),
                        kind       = port.Kind.ToString(),
                        dataType   = port.DataType,
                        capacity   = port.Capacity.ToString(),
                        sortOrder  = port.SortOrder
                    });
                }
            }

            if (node.UserData != null && udSer != null)
                nd.userData = udSer.SerializeNodeData(node.UserData);

            return nd;
        }

        private static EdgeDto EdgeToDto(Edge edge, Graph graph)
        {
            var src = graph.FindPort(edge.SourcePortId);
            var tgt = graph.FindPort(edge.TargetPortId);
            return new EdgeDto
            {
                id         = edge.Id,
                fromNodeId = src?.NodeId   ?? "",
                fromPortId = src?.SemanticId ?? "",
                toNodeId   = tgt?.NodeId   ?? "",
                toPortId   = tgt?.SemanticId ?? ""
            };
        }

        // ══════════════════════════════════════
        //  GraphDto → Graph
        // ══════════════════════════════════════

        public Graph Restore(GraphDto dto,
            IUserDataSerializer? userDataSerializer = null,
            INodeTypeCatalog?   typeProvider       = null)
        {
            if (dto.schemaVersion < 2)
                throw new InvalidOperationException(
                    $"不支持的图格式版本 v{dto.schemaVersion}。当前仅支持 v2 及以上。" +
                    $"请用上一版本的编辑器重新保存该文件以完成迁移。");

            var topology = GraphTopologyPolicy.DAG;
            if (Enum.TryParse<GraphTopologyPolicy>(dto.settings.topology, out var parsed))
                topology = parsed;

            var settings = new GraphSettings { Topology = topology };
            var graph    = new Graph(dto.id, settings);

            foreach (var nd in dto.nodes)
                graph.AddNodeDirect(NodeFromDto(nd, typeProvider, userDataSerializer));

            foreach (var ed in dto.edges)
            {
                var src = FindPortBySemanticId(graph, ed.fromNodeId, ed.fromPortId);
                var tgt = FindPortBySemanticId(graph, ed.toNodeId,   ed.toPortId);
                if (src == null || tgt == null) continue;
                graph.AddEdgeDirect(new Edge(ed.id, src.Id, tgt.Id));
            }

            foreach (var gd in dto.groups)
            {
                var pos = F(gd.position); var sz = F(gd.size);
                var group = new NodeGroup(gd.id, gd.title)
                {
                    Bounds = new Rect2(pos.X, pos.Y, sz.X, sz.Y),
                    Color  = F4(gd.color)
                };
                foreach (var nid in gd.nodeIds) group.ContainedNodeIds.Add(nid);
                graph.AddGroupDirect(group);
            }

            foreach (var cd in dto.comments)
            {
                var pos = F(cd.position); var sz = F(cd.size);
                var c = new GraphComment(cd.id, cd.text)
                {
                    Bounds          = new Rect2(pos.X, pos.Y, sz.X, sz.Y),
                    FontSize        = cd.fontSize,
                    TextColor       = F4(cd.textColor),
                    BackgroundColor = F4(cd.backgroundColor)
                };
                graph.AddCommentDirect(c);
            }

            foreach (var sd in dto.subGraphFrames)
            {
                var pos = F(sd.position); var sz = F(sd.size);
                var sgf = new SubGraphFrame(sd.id, sd.title, sd.representativeNodeId)
                {
                    Bounds        = new Rect2(pos.X, pos.Y, sz.X, sz.Y),
                    IsCollapsed   = sd.isCollapsed,
                    SourceAssetId = sd.sourceAssetId
                };
                foreach (var nid in sd.nodeIds) sgf.ContainedNodeIds.Add(nid);
                graph.AddSubGraphFrameDirect(sgf);
            }

            return graph;
        }

        private static Node NodeFromDto(NodeDto nd,
            INodeTypeCatalog?   typeProv,
            IUserDataSerializer? udSer)
        {
            Enum.TryParse<NodeDisplayMode>(nd.displayMode, out var displayMode);
            var node = new Node(nd.id, nd.typeId, F(nd.position))
            {
                Size              = F(nd.size),
                DisplayMode       = displayMode,
                AllowDynamicPorts = nd.allowDynamicPorts
            };

            var typeDef = (!nd.allowDynamicPorts) ? typeProv?.GetNodeType(nd.typeId) : null;
            if (typeDef != null)
            {
                foreach (var portDef in typeDef.DefaultPorts)
                    node.AddPortDirect(new Port(IdGenerator.NewId(), node.Id, portDef));
            }
            else
            {
                foreach (var pd in nd.ports)
                {
                    Enum.TryParse<PortDirection>(pd.direction, out var dir);
                    Enum.TryParse<PortKind>(pd.kind, out var kind);
                    Enum.TryParse<PortCapacity>(pd.capacity, out var cap);
                    var semId = string.IsNullOrEmpty(pd.semanticId) ? pd.name : pd.semanticId;
                    node.AddPortDirect(new Port(pd.id, node.Id, pd.name, dir, kind, pd.dataType, cap, pd.sortOrder, semId));
                }
            }

            if (nd.userData != null && udSer != null)
                node.UserData = udSer.DeserializeNodeData(nd.typeId, nd.userData);

            return node;
        }

        private static Port? FindPortBySemanticId(Graph graph, string nodeId, string semanticId)
        {
            var node = graph.FindNode(nodeId);
            if (node == null) return null;
            foreach (var p in node.Ports)
                if (p.SemanticId == semanticId) return p;
            return null;
        }

        // ══════════════════════════════════════
        //  带诊断的反序列化（问题9）
        // ══════════════════════════════════════

        /// <summary>
        /// 与 <see cref="Restore"/> 逻辑相同，但返回 <see cref="RestoreResult"/>
        /// 以暴露被静默跳过的元素（供调试/日志场景使用）。
        /// <para><see cref="Restore"/> 保持原有签名不变，静默跳过行为不变。</para>
        /// </summary>
        public RestoreResult RestoreWithDiagnostics(GraphDto dto,
            IUserDataSerializer? userDataSerializer = null,
            INodeTypeCatalog?   typeProvider       = null)
        {
            var warnings       = new List<string>();
            var skippedEdgeIds = new List<string>();

            if (dto.schemaVersion < 2)
                throw new InvalidOperationException(
                    $"不支持的图格式版本 v{dto.schemaVersion}。当前仅支持 v2 及以上。");

            var topology = GraphTopologyPolicy.DAG;
            if (Enum.TryParse<GraphTopologyPolicy>(dto.settings.topology, out var parsed))
                topology = parsed;

            var settings = new GraphSettings { Topology = topology };
            var graph    = new Graph(dto.id, settings);

            foreach (var nd in dto.nodes)
                graph.AddNodeDirect(NodeFromDto(nd, typeProvider, userDataSerializer));

            foreach (var ed in dto.edges)
            {
                var src = FindPortBySemanticId(graph, ed.fromNodeId, ed.fromPortId);
                var tgt = FindPortBySemanticId(graph, ed.toNodeId,   ed.toPortId);
                if (src == null || tgt == null)
                {
                    skippedEdgeIds.Add(ed.id);
                    warnings.Add(
                        $"连线 {ed.id} 跳过：端口未找到 (from={ed.fromNodeId}/{ed.fromPortId}, to={ed.toNodeId}/{ed.toPortId})");
                    continue;
                }
                graph.AddEdgeDirect(new Edge(ed.id, src.Id, tgt.Id));
            }

            foreach (var gd in dto.groups)
            {
                var pos = F(gd.position); var sz = F(gd.size);
                var group = new NodeGroup(gd.id, gd.title)
                    { Bounds = new Rect2(pos.X, pos.Y, sz.X, sz.Y), Color = F4(gd.color) };
                foreach (var nid in gd.nodeIds) group.ContainedNodeIds.Add(nid);
                graph.AddGroupDirect(group);
            }

            foreach (var cd in dto.comments)
            {
                var pos = F(cd.position); var sz = F(cd.size);
                var c = new GraphComment(cd.id, cd.text)
                {
                    Bounds = new Rect2(pos.X, pos.Y, sz.X, sz.Y),
                    FontSize = cd.fontSize, TextColor = F4(cd.textColor),
                    BackgroundColor = F4(cd.backgroundColor)
                };
                graph.AddCommentDirect(c);
            }

            foreach (var sd in dto.subGraphFrames)
            {
                var pos = F(sd.position); var sz = F(sd.size);
                var sgf = new SubGraphFrame(sd.id, sd.title, sd.representativeNodeId)
                {
                    Bounds = new Rect2(pos.X, pos.Y, sz.X, sz.Y),
                    IsCollapsed = sd.isCollapsed, SourceAssetId = sd.sourceAssetId
                };
                foreach (var nid in sd.nodeIds) sgf.ContainedNodeIds.Add(nid);
                graph.AddSubGraphFrameDirect(sgf);
            }

            return new RestoreResult(graph, skippedEdgeIds, warnings);
        }

        // ══════════════════════════════════════
        //  基本类型转换
        // ══════════════════════════════════════

        private static Vec2Dto  V(Vec2   v) => new Vec2Dto  { x = v.X, y = v.Y };
        private static Vec2     F(Vec2Dto d) => new Vec2(d.x, d.y);
        private static Color4Dto C(Color4 c) => new Color4Dto { r = c.R, g = c.G, b = c.B, a = c.A };
        private static Color4  F4(Color4Dto d) => new Color4(d.r, d.g, d.b, d.a);
    }
}
