#nullable enable

namespace NodeGraph.Core
{
    /// <summary>图拓扑策略</summary>
    public enum GraphTopologyPolicy
    {
        /// <summary>有向无环图（刷怪蓝图、技能编辑器）</summary>
        DAG,
        /// <summary>有向图，允许环（状态机、对话树）</summary>
        DirectedGraph,
        /// <summary>无向图（关系图）</summary>
        Undirected
    }

    /// <summary>
    /// [扩展点] 图的核心配置——拓扑策略与节点类型体系。
    /// </summary>
    /// <remarks>
    /// 业务层在创建 <see cref="Graph"/> 时传入配置实例。
    /// - <see cref="NodeTypes"/>    — 可替换为任意 <see cref="INodeTypeCatalog"/> 实现（如 ActionRegistryNodeTypeCatalog）。
    /// - <see cref="Behavior"/>     — 连接规则、类型兼容性等行为策略集合。
    /// - <see cref="Topology"/>    — DAG / 有向图 / 无向图拓扑约束。
    /// </remarks>
    public class GraphSettings
    {
        /// <summary>图拓扑策略</summary>
        public GraphTopologyPolicy Topology { get; set; } = GraphTopologyPolicy.DAG;

        /// <summary>节点类型目录（可替换为任意 INodeTypeCatalog 实现）</summary>
        public INodeTypeCatalog NodeTypes { get; set; }

        /// <summary>行为策略集合（连接规则、类型兼容性）</summary>
        public GraphBehavior Behavior { get; set; }

        public GraphSettings()
        {
            NodeTypes = new NodeTypeRegistry();
            Behavior  = new GraphBehavior();
        }

        /// <summary>创建空节点类型目录（内置默认实现）。供需要初始值但不依赖具体实现类的调用方使用。</summary>
        public static INodeTypeCatalog CreateEmptyNodeTypeCatalog() => new NodeTypeRegistry();
    }
}
