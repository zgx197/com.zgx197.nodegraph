#nullable enable
using System;
using System.Collections.Generic;
using NodeGraph.Core;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架核心模型] 命令历史管理器。支持 Undo/Redo 和复合命令。
    /// </summary>
    /// <remarks>
    /// 业务层通过 <see cref="Execute"/> 提交命令，不应直接调用 <see cref="ICommand.Execute"/> / <see cref="ICommand.Undo"/>。
    /// 关键 API：
    /// - <see cref="Execute"/>        — 执行命令并入栈。
    /// - <see cref="BeginCompound"/>  — 开始复合命令（using 块内的命令合并为一次 Undo）。
    /// - <see cref="Undo"/> / <see cref="Redo"/> — 撤销/重做。
    /// 命令合并：<see cref="ICommand.TryMergeWith"/> 默认返回 false；
    /// 实现该方法可将连续操作（如连续拖拽）合并为一次 Undo 记录。
    /// </remarks>
    public class CommandHistory
    {
        private readonly List<ICommand> _undoStack = new List<ICommand>();
        private readonly List<ICommand> _redoStack = new List<ICommand>();
        private readonly Graph _graph;

        // 复合命令嵌套支持
        private CompoundCommand? _activeCompound;
        private int _compoundDepth;

        /// <summary>最大历史记录数，超过后丢弃最早的记录</summary>
        public int MaxHistorySize { get; set; } = 100;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;

        /// <summary>历史变化时触发（Undo/Redo/Execute 后）</summary>
        public event Action? OnHistoryChanged;

        /// <summary>命令执行后触发</summary>
        public event Action<ICommand>? OnCommandExecuted;

        public CommandHistory(Graph graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        /// <summary>执行命令并压入 Undo 栈</summary>
        public void Execute(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            // 如果在复合命令中，则添加到复合命令
            if (_activeCompound != null)
            {
                command.Execute(_graph);
                _activeCompound.Add(command);
                OnCommandExecuted?.Invoke(command);
                return;
            }

            command.Execute(_graph);
            PushUndo(command);
            _redoStack.Clear();
            OnCommandExecuted?.Invoke(command);
            OnHistoryChanged?.Invoke();
        }

        /// <summary>撤销最近一次操作</summary>
        public void Undo()
        {
            if (!CanUndo) return;

            var command = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            command.Undo(_graph);
            _redoStack.Add(command);
            OnHistoryChanged?.Invoke();
        }

        /// <summary>重做最近一次撤销的操作</summary>
        public void Redo()
        {
            if (!CanRedo) return;

            var command = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
            command.Execute(_graph);
            PushUndo(command);
            OnHistoryChanged?.Invoke();
        }

        /// <summary>
        /// 开始一个复合命令。在 using 块内执行的所有命令将合并为一次 Undo。
        /// 支持嵌套调用，只有最外层 Dispose 时才真正提交。
        /// </summary>
        public IDisposable BeginCompound(string description)
        {
            _compoundDepth++;
            if (_compoundDepth == 1)
            {
                _activeCompound = new CompoundCommand(description);
            }
            return new CompoundScope(this);
        }

        /// <summary>清空所有历史记录</summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _activeCompound = null;
            _compoundDepth = 0;
            OnHistoryChanged?.Invoke();
        }

        // ── 内部辅助 ──

        private void PushUndo(ICommand command)
        {
            // 尝试与栈顶合并（连续拖拽等场景下合并为一次 Undo 记录）
            if (_undoStack.Count > 0)
            {
                var top = _undoStack[_undoStack.Count - 1];
                if (command.TryMergeWith(top)) return;
            }
            _undoStack.Add(command);
            // 限制历史大小
            while (_undoStack.Count > MaxHistorySize)
                _undoStack.RemoveAt(0);
        }

        private void EndCompound()
        {
            _compoundDepth--;
            if (_compoundDepth == 0 && _activeCompound != null)
            {
                var compound = _activeCompound;
                _activeCompound = null;

                if (compound.Commands.Count > 0)
                {
                    PushUndo(compound);
                    _redoStack.Clear();
                    OnHistoryChanged?.Invoke();
                }
            }
        }

        /// <summary>复合命令作用域，Dispose 时结束复合命令</summary>
        private class CompoundScope : IDisposable
        {
            private readonly CommandHistory _history;
            private bool _disposed;

            public CompoundScope(CommandHistory history)
            {
                _history = history;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _history.EndCompound();
            }
        }
    }
}
