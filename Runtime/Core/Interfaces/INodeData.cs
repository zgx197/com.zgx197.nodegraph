#nullable enable

namespace NodeGraph.Core
{
    /// <summary>
    /// [扩展点] 节点业务数据标记接口。
    /// 业务层通过实现此接口将自定义数据附加到节点上（如 SpawnTaskData、SkillNodeData）。
    /// </summary>
    /// <remarks>
    /// 实现约定：
    /// - 实现类应设计为不可变或仅由 Command 修改，避免绕过 Undo/Redo。
    /// - 若需要在节点标题栏下方显示描述文字，可同时实现 <see cref="IDescribableNode"/>。
    /// - 序列化由业务层提供的 IUserDataSerializer 负责，框架不关心具体字段。
    /// </remarks>
    public interface INodeData { }
}
