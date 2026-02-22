#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeGraph.Core
{
    /// <summary>
    /// 节点类型注册表。管理所有已注册的节点类型定义。
    /// </summary>
    internal class NodeTypeRegistry : INodeTypeCatalog
    {
        private readonly Dictionary<string, NodeTypeDefinition> _definitions = new Dictionary<string, NodeTypeDefinition>();

        /// <summary>注册节点类型定义</summary>
        /// <exception cref="ArgumentException">TypeId 已存在时抛出</exception>
        public void Register(NodeTypeDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (_definitions.ContainsKey(definition.TypeId))
                throw new ArgumentException($"节点类型 '{definition.TypeId}' 已注册", nameof(definition));

            _definitions[definition.TypeId] = definition;
        }

        /// <summary>注销节点类型</summary>
        /// <returns>是否成功注销</returns>
        public bool Unregister(string typeId)
        {
            if (typeId == null) throw new ArgumentNullException(nameof(typeId));
            return _definitions.Remove(typeId);
        }

        /// <summary>根据 TypeId 获取定义</summary>
        public NodeTypeDefinition? GetNodeType(string typeId)
        {
            if (typeId == null) return null;
            _definitions.TryGetValue(typeId, out var def);
            return def;
        }

        /// <summary>获取所有已注册的节点类型</summary>
        /// <summary>向后兼容别名</summary>
        public NodeTypeDefinition? GetDefinition(string typeId) => GetNodeType(typeId);

        public IEnumerable<NodeTypeDefinition> GetAll() => _definitions.Values;

        /// <summary>按关键字搜索节点类型（匹配 TypeId、DisplayName 或 Category）</summary>
        public IEnumerable<NodeTypeDefinition> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return GetAll();

            var lower = keyword.ToLowerInvariant();
            return _definitions.Values.Where(d =>
                d.TypeId.ToLowerInvariant().Contains(lower) ||
                d.DisplayName.ToLowerInvariant().Contains(lower) ||
                d.Category.ToLowerInvariant().Contains(lower));
        }

        /// <summary>获取所有已注册的分类路径</summary>
        public IEnumerable<string> GetCategories() =>
            _definitions.Values
                .Select(d => d.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct();

        /// <summary>是否已注册指定类型</summary>
        public bool Contains(string typeId) =>
            typeId != null && _definitions.ContainsKey(typeId);
    }
}
