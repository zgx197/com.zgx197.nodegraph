#nullable enable

namespace NodeGraph.Core
{
    /// <summary>
    /// [扩展点] 连线业务数据标记接口。
    /// 业务层实现此接口将自定义数据附加到连线上（如 TransitionConditionData）。
    /// </summary>
    /// <remarks>
    /// 注意：连线的类型（Control / Data）由源端口的 <see cref="PortKind"/> 推断，
    /// 不依赖 IEdgeData 字段。IEdgeData 仅用于携带连线自身的语义数据（如转换条件）。
    /// 若业务层无连线附加数据需求，则无需实现此接口。
    /// </remarks>
    public interface IEdgeData { }
}
