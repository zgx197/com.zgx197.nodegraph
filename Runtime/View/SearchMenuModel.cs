#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.View
{
    /// <summary>
    /// 添加节点搜索菜单模型。右键空白区域或按 Space 打开。
    /// 提供节点类型的搜索过滤和分类分组。
    /// </summary>
    public class SearchMenuModel
    {
        /// <summary>搜索文本</summary>
        public string SearchText { get; set; } = "";

        /// <summary>菜单在画布上的位置（节点将创建在此处）</summary>
        public Vec2 Position { get; set; }

        /// <summary>菜单是否打开</summary>
        public bool IsOpen { get; set; }

        /// <summary>当前选中的索引（键盘导航）</summary>
        public int SelectedIndex { get; set; }

        /// <summary>打开菜单</summary>
        public void Open(Vec2 canvasPosition)
        {
            Position = canvasPosition;
            SearchText = "";
            SelectedIndex = 0;
            IsOpen = true;
        }

        /// <summary>关闭菜单</summary>
        public void Close()
        {
            IsOpen = false;
            SearchText = "";
        }

        /// <summary>根据搜索文本过滤可用节点类型</summary>
        public IEnumerable<NodeTypeDefinition> GetFilteredTypes(INodeTypeCatalog registry)
        {
            var all = registry.GetAll();

            if (string.IsNullOrWhiteSpace(SearchText))
                return all;

            var query = SearchText.Trim();
            return all.Where(def =>
                def.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                def.TypeId.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (def.Category != null && def.Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        /// <summary>按分类分组</summary>
        public IEnumerable<(string Category, IEnumerable<NodeTypeDefinition> Types)> GetGroupedTypes(INodeTypeCatalog registry)
        {
            return GetFilteredTypes(registry)
                .GroupBy(def => def.Category ?? "未分类")
                .OrderBy(g => g.Key)
                .Select(g => (g.Key, (IEnumerable<NodeTypeDefinition>)g.OrderBy(d => d.DisplayName)));
        }
    }
}
