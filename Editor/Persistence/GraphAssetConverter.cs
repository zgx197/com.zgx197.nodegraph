#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using NodeGraph.Core;
using NodeGraph.Serialization;

namespace NodeGraph.Unity.Persistence
{
    /// <summary>
    /// Graph ↔ GraphAsset 之间的转换器。
    /// <para>
    /// v2: 节点/分组/注释/子图框的 Graph↔DTO 转换统一委托给 <see cref="DefaultGraphPersister"/>，
    /// <see cref="GraphAssetConverter"/> 只负责 DTO↔SO 字段映射。
    /// 连线使用 SO 原生 rawPortGUID 存储，直接操作不经语义查找。
    /// </para>
    /// </summary>
    public static class GraphAssetConverter
    {
        private static readonly IGraphPersister _persister = new DefaultGraphPersister();

        // ══════════════════════════════════════
        //  Graph → GraphAsset
        // ══════════════════════════════════════

        /// <summary>将 Graph 写入 GraphAsset</summary>
        public static void SaveToAsset(Graph graph, GraphAsset asset)
        {
            // Graph → GraphDto（节点/分组/注释/子图框的转换逻辑在 DefaultGraphPersister 中统一维护）
            var dto = _persister.Capture(graph, userDataSerializer: null, typeProvider: null);

            asset.SetGraphId(dto.id);
            asset.Topology = ParseTopology(dto.settings.topology);

            // 节点：GraphDto → SerializedNode
            asset.Nodes.Clear();
            foreach (var nd in dto.nodes)
            {
                var sn = new SerializedNode
                {
                    id = nd.id, typeId = nd.typeId,
                    position = new Vector2(nd.position.x, nd.position.y),
                    size     = new Vector2(nd.size.x,     nd.size.y),
                    displayMode       = ParseDisplayMode(nd.displayMode),
                    allowDynamicPorts = nd.allowDynamicPorts,
                    userDataJson      = nd.userData ?? ""
                };
                foreach (var pd in nd.ports)
                    sn.ports.Add(new SerializedPort
                    {
                        id         = pd.id,
                        name       = pd.name,
                        semanticId = pd.semanticId,
                        direction  = ParseDirection(pd.direction),
                        kind       = ParseKind(pd.kind),
                        dataType   = pd.dataType,
                        capacity   = ParseCapacity(pd.capacity),
                        sortOrder  = pd.sortOrder
                    });
                asset.Nodes.Add(sn);
            }

            // 连线：直接用 rawPortGUID，不经过 DTO
            asset.Edges.Clear();
            foreach (var edge in graph.Edges)
                asset.Edges.Add(new SerializedEdge
                {
                    id           = edge.Id,
                    sourcePortId = edge.SourcePortId,
                    targetPortId = edge.TargetPortId,
                    userDataJson = ""
                });

            // 分组：GroupDto → SerializedGroup
            asset.Groups.Clear();
            foreach (var gd in dto.groups)
            {
                var sg = new SerializedGroup
                {
                    id       = gd.id,   title    = gd.title,
                    position = new Vector2(gd.position.x, gd.position.y),
                    size     = new Vector2(gd.size.x,     gd.size.y),
                    color    = new Color(gd.color.r, gd.color.g, gd.color.b, gd.color.a)
                };
                sg.nodeIds.AddRange(gd.nodeIds);
                asset.Groups.Add(sg);
            }

            // 注释：CommentDto → SerializedComment
            asset.Comments.Clear();
            foreach (var cd in dto.comments)
                asset.Comments.Add(new SerializedComment
                {
                    id = cd.id, text = cd.text,
                    position        = new Vector2(cd.position.x, cd.position.y),
                    size            = new Vector2(cd.size.x,     cd.size.y),
                    fontSize        = cd.fontSize,
                    textColor       = new Color(cd.textColor.r,       cd.textColor.g,       cd.textColor.b,       cd.textColor.a),
                    backgroundColor = new Color(cd.backgroundColor.r, cd.backgroundColor.g, cd.backgroundColor.b, cd.backgroundColor.a)
                });

            // 子图框：SubGraphFrameDto → SerializedSubGraphFrame
            asset.SubGraphFrames.Clear();
            foreach (var sd in dto.subGraphFrames)
            {
                var ss = new SerializedSubGraphFrame
                {
                    id = sd.id, title = sd.title,
                    position             = new Vector2(sd.position.x, sd.position.y),
                    size                 = new Vector2(sd.size.x,     sd.size.y),
                    representativeNodeId = sd.representativeNodeId,
                    isCollapsed          = sd.isCollapsed,
                    sourceAssetId        = sd.sourceAssetId ?? ""
                };
                ss.nodeIds.AddRange(sd.nodeIds);
                asset.SubGraphFrames.Add(ss);
            }
        }

        // ══════════════════════════════════════
        //  GraphAsset → Graph
        // ══════════════════════════════════════

        /// <summary>从 GraphAsset 恢徤 Graph</summary>
        public static Graph LoadFromAsset(GraphAsset asset)
        {
            // SO 字段 → GraphDto（仅节点/分组/注释/子图框）
            var dto = AssetToDto(asset);

            // GraphDto → Graph（DefaultGraphPersister 处理节点/分组/注释/子图框）
            // dto.edges 为空，连线在下方直接添加
            var graph = _persister.Restore(dto, userDataSerializer: null, typeProvider: null);

            // 连线：SO 存储的 rawPortGUID 直接构建 Edge，无需语义查找
            foreach (var se in asset.Edges)
                graph.AddEdgeDirect(new Edge(se.id, se.sourcePortId, se.targetPortId));

            return graph;
        }

        // ══════════════════════════════════════
        //  内部：SO 字段 ↔ GraphDto 映射
        // ══════════════════════════════════════

        private static GraphDto AssetToDto(GraphAsset asset)
        {
            var dto = new GraphDto
            {
                id            = asset.GraphId,
                schemaVersion = 2,
                settings      = new GraphSettingsDto { topology = ((GraphTopologyPolicy)asset.Topology).ToString() }
            };

            foreach (var sn in asset.Nodes)
            {
                var nd = new NodeDto
                {
                    id = sn.id, typeId = sn.typeId,
                    position         = new Vec2Dto { x = sn.position.x, y = sn.position.y },
                    size             = new Vec2Dto { x = sn.size.x,     y = sn.size.y },
                    displayMode      = ((NodeDisplayMode)sn.displayMode).ToString(),
                    allowDynamicPorts = sn.allowDynamicPorts,
                    userData         = string.IsNullOrEmpty(sn.userDataJson) ? null : sn.userDataJson
                };
                foreach (var sp in sn.ports)
                    nd.ports.Add(new PortDto
                    {
                        id         = sp.id,
                        name       = sp.name,
                        semanticId = string.IsNullOrEmpty(sp.semanticId) ? sp.name : sp.semanticId,
                        direction  = ((PortDirection)sp.direction).ToString(),
                        kind       = ((PortKind)sp.kind).ToString(),
                        dataType   = sp.dataType,
                        capacity   = ((PortCapacity)sp.capacity).ToString(),
                        sortOrder  = sp.sortOrder
                    });
                dto.nodes.Add(nd);
            }

            foreach (var sg in asset.Groups)
            {
                var gd = new GroupDto
                {
                    id       = sg.id,  title    = sg.title,
                    position = new Vec2Dto { x = sg.position.x, y = sg.position.y },
                    size     = new Vec2Dto { x = sg.size.x,     y = sg.size.y },
                    color    = new Color4Dto { r = sg.color.r, g = sg.color.g, b = sg.color.b, a = sg.color.a }
                };
                gd.nodeIds.AddRange(sg.nodeIds);
                dto.groups.Add(gd);
            }

            foreach (var sc in asset.Comments)
                dto.comments.Add(new CommentDto
                {
                    id = sc.id, text = sc.text,
                    position        = new Vec2Dto { x = sc.position.x, y = sc.position.y },
                    size            = new Vec2Dto { x = sc.size.x,     y = sc.size.y },
                    fontSize        = sc.fontSize,
                    textColor       = new Color4Dto { r = sc.textColor.r,       g = sc.textColor.g,       b = sc.textColor.b,       a = sc.textColor.a },
                    backgroundColor = new Color4Dto { r = sc.backgroundColor.r, g = sc.backgroundColor.g, b = sc.backgroundColor.b, a = sc.backgroundColor.a }
                });

            if (asset.SubGraphFrames != null)
                foreach (var ss in asset.SubGraphFrames)
                {
                    var sd = new SubGraphFrameDto
                    {
                        id = ss.id, title = ss.title,
                        position             = new Vec2Dto { x = ss.position.x, y = ss.position.y },
                        size                 = new Vec2Dto { x = ss.size.x,     y = ss.size.y },
                        representativeNodeId = ss.representativeNodeId,
                        isCollapsed          = ss.isCollapsed,
                        sourceAssetId        = string.IsNullOrEmpty(ss.sourceAssetId) ? null : ss.sourceAssetId
                    };
                    sd.nodeIds.AddRange(ss.nodeIds);
                    dto.subGraphFrames.Add(sd);
                }

            return dto;
        }

        // ══════════════════════════════════════
        //  枚举字符串解析辅助
        // ══════════════════════════════════════

        private static int ParseTopology(string s)
            => (int)(Enum.TryParse<GraphTopologyPolicy>(s, out var v) ? v : GraphTopologyPolicy.DAG);
        private static int ParseDisplayMode(string s)
            => (int)(Enum.TryParse<NodeDisplayMode>(s, out var v) ? v : NodeDisplayMode.Expanded);
        private static int ParseDirection(string s)
            => (int)(Enum.TryParse<PortDirection>(s, out var v) ? v : PortDirection.Input);
        private static int ParseKind(string s)
            => (int)(Enum.TryParse<PortKind>(s, out var v) ? v : PortKind.Data);
        private static int ParseCapacity(string s)
            => (int)(Enum.TryParse<PortCapacity>(s, out var v) ? v : PortCapacity.Multiple);
    }
}
