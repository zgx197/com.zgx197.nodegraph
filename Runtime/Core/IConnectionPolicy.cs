#nullable enable

namespace NodeGraph.Core
{
    /// <summary>连接验证结果</summary>
    public enum ConnectionResult
    {
        /// <summary>允许连接</summary>
        Success,
        /// <summary>不能连接同一个节点的端口</summary>
        SameNode,
        /// <summary>两个端口方向相同（都是 Input 或都是 Output）</summary>
        SameDirection,
        /// <summary>端口类别不匹配（Control 连 Data）</summary>
        KindMismatch,
        /// <summary>数据类型不兼容</summary>
        DataTypeMismatch,
        /// <summary>端口已满（Single 容量端口已有连接）</summary>
        CapacityExceeded,
        /// <summary>会形成环（仅 DAG 模式）</summary>
        CycleDetected,
        /// <summary>已存在相同的连接</summary>
        DuplicateEdge,
        /// <summary>业务层自定义拒绝</summary>
        CustomRejected
    }

    /// <summary>
    /// [扩展点] 连接策略接口。定义两个端口能否建立连接的规则。
    /// </summary>
    /// <remarks>
    /// 内置实现：<see cref="DefaultConnectionPolicy"/>（基础校验 + DAG 环检测 + IConnectionValidator 责任链）。
    /// 业务层通常继承 <see cref="DefaultConnectionPolicy"/> 并调用 <c>base.CanConnect()</c>，
    /// 或通过实现 <see cref="IConnectionValidator"/> 向责任链注入额外规则而无需重写本接口。
    /// </remarks>
    public interface IConnectionPolicy
    {
        /// <summary>检查两个端口是否可以连接</summary>
        ConnectionResult CanConnect(Graph graph, Port source, Port target);
    }
}
