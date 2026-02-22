#nullable enable
using System.Collections.Generic;

namespace NodeGraph.Core
{
    /// <summary>
    /// [扩展点] 节点类型目录接口。支持按 TypeId 查询、枚举和关键字搜索。
    /// </summary>
    /// <remarks>
    /// 内置实现：<see cref="NodeTypeRegistry"/>（NodeGraph 内建）。
    /// 业务层实现：ActionRegistryNodeTypeCatalog（SceneBlueprint 侧）。
    /// 实现该接口后通过 <see cref="BlueprintProfile"/> 注入到图视图。
    /// </remarks>
    public interface INodeTypeCatalog
    {
        /// <summary>根据 TypeId 返回节点类型定义。找不到时返回 null。</summary>
        NodeTypeDefinition? GetNodeType(string typeId);

        /// <summary>返回全部已注册的节点类型定义。</summary>
        IEnumerable<NodeTypeDefinition> GetAll();

        /// <summary>按关键字搜索节点类型（匹配 TypeId、DisplayName 或 Category）。</summary>
        IEnumerable<NodeTypeDefinition> Search(string keyword);

        /// <summary>获取所有分类路径（去重）。</summary>
        IEnumerable<string> GetCategories();
    }
}
