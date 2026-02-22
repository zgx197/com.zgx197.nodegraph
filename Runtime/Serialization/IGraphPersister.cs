#nullable enable
using NodeGraph.Abstraction;
using NodeGraph.Core;

namespace NodeGraph.Serialization
{
    /// <summary>
    /// [框架内部接口] 图持久器接口——在 <see cref="Core.Graph"/>（内存核心模型）
    /// 与 <see cref="GraphDto"/>（格式无关中间 DTO）之间做双向转换。
    /// <para>
    /// 数据流：
    /// <code>
    /// Graph ──Capture──▶ GraphDto ──▶ JSON / Unity SO / 其他
    ///       ◀──Restore──           ◀──
    /// </code>
    /// 与 <see cref="IGraphPersistence"/>（持久化层，负责读写磁盘/SO）不同：
    /// IGraphPersister 只做内存模型 ↔ DTO 的纯转换，不涉及 I/O。
    /// </para>
    /// </summary>
    internal interface IGraphPersister
    {
        /// <summary>
        /// 将 Graph 内存对象快照为 GraphDto。
        /// </summary>
        /// <param name="graph">要快照的图</param>
        /// <param name="userDataSerializer">业务数据序列化器（可为 null）</param>
        /// <param name="typeProvider">
        /// 节点类型提供者（可为 null）。非 null 时跳过已知类型端口的 DTO 存储，减小体积。
        /// </param>
        GraphDto Capture(Graph graph,
            IUserDataSerializer? userDataSerializer = null,
            INodeTypeCatalog?    typeProvider       = null);

        /// <summary>
        /// 从 GraphDto 恢复 Graph 内存对象。
        /// </summary>
        /// <param name="dto">源 DTO</param>
        /// <param name="userDataSerializer">业务数据反序列化器（可为 null）</param>
        /// <param name="typeProvider">节点类型提供者，用于从类型定义重建端口（可为 null）</param>
        Graph Restore(GraphDto dto,
            IUserDataSerializer? userDataSerializer = null,
            INodeTypeCatalog?    typeProvider       = null);
    }
}
