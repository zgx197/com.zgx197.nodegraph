#nullable enable
using System.Collections.Generic;
using NodeGraph.Core;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 复合命令。将多个子命令合并为一次 Undo/Redo 操作。
    /// </summary>
    /// <remarks>
    /// 通常不直接构造，而是通过 <see cref="CommandHistory.BeginCompound"/> 的 using 块自动创建。
    /// Undo 时按倒序撤销所有子命令。
    /// </remarks>
    internal class CompoundCommand : ICommand
    {
        private readonly List<ICommand> _commands = new List<ICommand>();

        public string Description { get; }

        public IReadOnlyList<ICommand> Commands => _commands;

        public CompoundCommand(string description)
        {
            Description = description ?? "复合操作";
        }

        /// <summary>添加子命令</summary>
        internal void Add(ICommand command)
        {
            _commands.Add(command);
        }

        public void Execute(Graph graph)
        {
            for (int i = 0; i < _commands.Count; i++)
                _commands[i].Execute(graph);
        }

        public void Undo(Graph graph)
        {
            // 逆序撤销
            for (int i = _commands.Count - 1; i >= 0; i--)
                _commands[i].Undo(graph);
        }
    }
}
