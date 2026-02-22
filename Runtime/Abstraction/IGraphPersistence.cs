#nullable enable
using NodeGraph.Core;

namespace NodeGraph.Abstraction
{
    /// <summary>
    /// 图持久化接口（持久化层）。
    /// 每个引擎提供自己的实现，决定如何将 Graph 存储到磁盘/数据库。
    /// 
    /// Unity：ScriptableObject 原生序列化（结构数据不经 JSON，业务数据用 JSON 字符串字段）
    /// Godot：Resource (.tres) 原生序列化
    /// 通用：JSON 文件（经过 IGraphSerializer）
    /// </summary>
    public interface IGraphPersistence
    {
        /// <summary>保存图到持久化存储</summary>
        void Save(Graph graph);

        /// <summary>从持久化存储加载图</summary>
        Graph? Load();

        /// <summary>内存中是否有未保存的修改</summary>
        bool IsDirty { get; }
    }
}
