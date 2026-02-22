#nullable enable

namespace NodeGraph.Core
{
    /// <summary>
    /// Graph.Connect() 的返回结果。显式描述连接操作的所有副作用。
    /// </summary>
    public readonly struct ConnectResult
    {
        /// <summary>本次操作创建的新边。若连接被拒绝则为 null。</summary>
        public Edge? CreatedEdge { get; }

        /// <summary>
        /// 被顶替的旧边（仅当目标端口为 Single 容量且原有连线时非 null）。
        /// 调用方（如 ConnectCommand）可直接用此字段实现 Undo，无需在调用前预检。
        /// </summary>
        public Edge? DisplacedEdge { get; }

        /// <summary>连接是否成功（即 CreatedEdge != null）</summary>
        public bool Success => CreatedEdge != null;

        public ConnectResult(Edge? createdEdge, Edge? displacedEdge)
        {
            CreatedEdge = createdEdge;
            DisplacedEdge = displacedEdge;
        }

        /// <summary>连接被拒绝时的空结果</summary>
        public static ConnectResult Rejected => new ConnectResult(null, null);
    }
}
