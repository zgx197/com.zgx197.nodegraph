#nullable enable
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 创建节点分组（视觉分组，不影响图结构）。
    /// </summary>
    public class CreateGroupCommand : IStyleCommand
    {
        private readonly string _title;
        private readonly List<string> _nodeIds;

        private string? _createdGroupId;

        public string Description { get; }

        /// <summary>获取执行后创建的分组 ID</summary>
        public string? CreatedGroupId => _createdGroupId;

        public CreateGroupCommand(string title, IEnumerable<string>? nodeIds = null)
        {
            _title = title;
            _nodeIds = nodeIds?.ToList() ?? new List<string>();
            Description = $"创建分组 {title}";
        }

        public void Execute(Graph graph)
        {
            var group = graph.CreateGroup(_title, _nodeIds);
            group.AutoFit(graph);
            _createdGroupId = group.Id;
        }

        public void Undo(Graph graph)
        {
            if (_createdGroupId != null)
                graph.RemoveGroup(_createdGroupId);
        }
    }
}
