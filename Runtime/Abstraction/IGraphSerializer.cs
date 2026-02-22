#nullable enable
using System.Collections.Generic;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Abstraction
{
    /// <summary>
    /// 图序列化接口（序列化层）。
    /// 将 Graph 内存对象转换为中间格式（如 JSON 字符串），用于：
    /// - 复制粘贴（选中子图序列化到剪贴板）
    /// - 跨引擎导入导出
    /// - 调试查看
    /// 
    /// 各引擎的持久化（IGraphPersistence）可以选择是否经过此层。
    /// </summary>
    public interface IGraphSerializer
    {
        /// <summary>将整个图序列化为字符串</summary>
        string Serialize(Graph graph);

        /// <summary>从字符串反序列化为图</summary>
        Graph? Deserialize(string data);

        /// <summary>序列化子图（选中的节点和连线，用于复制粘贴）</summary>
        string SerializeSubGraph(Graph graph, IEnumerable<string> nodeIds);

        /// <summary>
        /// 反序列化子图并合并到目标图中。
        /// 会生成新的 ID 以避免冲突，并应用 offset 偏移。
        /// </summary>
        /// <returns>新创建的节点列表</returns>
        IEnumerable<Node> DeserializeSubGraphInto(Graph target, string data, Vec2 offset);
    }
}
