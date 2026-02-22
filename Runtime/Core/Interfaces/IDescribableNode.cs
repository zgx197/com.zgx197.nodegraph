#nullable enable

namespace NodeGraph.Core
{
    /// <summary>
    /// [扩展点/可选] 可携带节点描述文本的标记接口。
    /// <see cref="INodeData"/> 实现类可选择同时实现此接口，
    /// 编辑器将在节点标题栏下方显示 <see cref="Description"/> 文字。
    /// </summary>
    public interface IDescribableNode
    {
        /// <summary>节点描述文本（空或 null 表示不显示）</summary>
        string? Description { get; }
    }
}
