#nullable enable

namespace NodeGraph.Core
{
    /// <summary>
    /// [扩展点] 连接验证器接口——责任链中的单个校验节点。
    /// <para>
    /// <see cref="DefaultConnectionPolicy"/> 在内置校验全部通过后，
    /// 按注册顺序依次调用所有 <see cref="IConnectionValidator"/>。
    /// 任意验证器返回非 null 值时，连接被拒绝并以该值作为结果。
    /// </para>
    /// <para>
    /// 典型用途：
    /// - 为特定图类型注入额外的类型兼容规则（如 SceneBlueprint 的 DataTypeRegistry 检查）
    /// - 注入业务层约束（如禁止某些节点类型互连）
    /// </para>
    /// </summary>
    public interface IConnectionValidator
    {
        /// <summary>
        /// 验证连接是否合法。
        /// </summary>
        /// <param name="graph">当前图</param>
        /// <param name="outPort">输出端口（方向已由 DefaultConnectionPolicy 保证为 Output）</param>
        /// <param name="inPort">输入端口（方向已由 DefaultConnectionPolicy 保证为 Input）</param>
        /// <returns>
        /// <c>null</c> 表示本验证器通过，继续向后传递；
        /// 非 null 表示拒绝，以返回值作为最终 <see cref="ConnectionResult"/>。
        /// </returns>
        ConnectionResult? Validate(Graph graph, Port outPort, Port inPort);
    }
}
