#nullable enable

namespace NodeGraph.Core
{
    /// <summary>
    /// 图的行为策略集合——连接规则与类型兼容性注册表。
    /// <para>
    /// 与 <see cref="GraphSettings"/> 分离的设计意图：
    /// <list type="bullet">
    /// <item>GraphSettings = 图的"是什么"（拓扑、节点类型体系）</item>
    /// <item>GraphBehavior = 图的"怎么做"（连接策略、类型兼容规则）</item>
    /// </list>
    /// 两者独立可替换：宿主可在不换节点类型体系的前提下切换连接策略，反之亦然。
    /// </para>
    /// </summary>
    public class GraphBehavior
    {
        /// <summary>连接策略（可替换，默认为 DefaultConnectionPolicy）</summary>
        public IConnectionPolicy ConnectionPolicy { get; set; }

        /// <summary>类型兼容性注册表</summary>
        public TypeCompatibilityRegistry TypeCompatibility { get; }

        public GraphBehavior()
        {
            TypeCompatibility = new TypeCompatibilityRegistry();
            ConnectionPolicy  = new DefaultConnectionPolicy();
        }

        public GraphBehavior(IConnectionPolicy connectionPolicy)
        {
            TypeCompatibility = new TypeCompatibilityRegistry();
            ConnectionPolicy  = connectionPolicy;
        }
    }
}
