#nullable enable
using NodeGraph.Core;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [扩展点] 命令接口。所有可撤销的图操作都实现此接口。
    /// </summary>
    /// <remarks>
    /// 实现约定：
    /// - Execute 和 Undo 必须互为逆操作，且对同一个 <see cref="Graph"/> 实例操作。
    /// - 命令实例应封装所有所需参数，实例创建后应为不可变。
    /// - 不得在 Execute/Undo 内存储外部状态，操作必须完全基于传入的 Graph 。
    /// - 影响图结构的命令应同时实现 <see cref="IStructuralCommand"/>；
    ///   仅影响视觉位置的命令应实现 <see cref="IStyleCommand"/>。
    /// - 所有命令通过 <see cref="CommandHistory.Execute"/> 来调用，不应直接调用 Execute/Undo 。
    /// </remarks>
    public interface ICommand
    {
        /// <summary>命令描述（用于 UI 显示，如"添加节点 SpawnTask"）</summary>
        string Description { get; }

        /// <summary>执行命令</summary>
        void Execute(Graph graph);

        /// <summary>撤销命令</summary>
        void Undo(Graph graph);

        /// <summary>
        /// 尝试与撤销栈顶的 <paramref name="previous"/> 命令合并。
        /// <para>
        /// 合并成功时 <paramref name="previous"/> 的状态已被修改（纳入了本命令的增量），
        /// 调用方丢弃 <c>this</c> 不入栈。默认返回 <c>false</c>（不合并）。
        /// </para>
        /// </summary>
        bool TryMergeWith(ICommand previous) => false;
    }
}
