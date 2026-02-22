#nullable enable
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Commands
{
    /// <summary>
    /// [框架内置命令] 在画布上创建文字注释。
    /// </summary>
    public class CreateCommentCommand : IStyleCommand
    {
        private readonly string _text;
        private readonly Vec2 _position;

        private string? _createdCommentId;

        public string Description { get; }

        /// <summary>获取执行后创建的注释 ID</summary>
        public string? CreatedCommentId => _createdCommentId;

        public CreateCommentCommand(string text, Vec2 position)
        {
            _text = text;
            _position = position;
            Description = "创建注释";
        }

        public void Execute(Graph graph)
        {
            var comment = graph.CreateComment(_text, _position);
            _createdCommentId = comment.Id;
        }

        public void Undo(Graph graph)
        {
            if (_createdCommentId != null)
                graph.RemoveComment(_createdCommentId);
        }
    }
}
