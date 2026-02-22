#nullable enable

namespace NodeGraph.Commands
{
    /// <summary>
    /// [扩展点/标记接口] 影响图结构（节点/边增删、数据变更）的命令。
    /// 执行此类命令后 Session 层应触发图分析调度和预览缓存失效。
    /// </summary>
    public interface IStructuralCommand : ICommand { }

    /// <summary>
    /// [扩展点/标记接口] 仅影响视觉/位置，不改变图语义的命令。
    /// 执行此类命令后 Session 层跳过分析调度，只触发重绘。
    /// </summary>
    public interface IStyleCommand : ICommand { }
}
