#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Core;

namespace NodeGraph.View
{
    /// <summary>
    /// 节点搜索模型。Ctrl+F 打开，搜索图中已有的节点。
    /// </summary>
    public class NodeSearchModel
    {
        /// <summary>搜索文本</summary>
        public string SearchText { get; set; } = "";

        /// <summary>搜索面板是否打开</summary>
        public bool IsOpen { get; set; }

        /// <summary>当前选中的结果索引</summary>
        public int SelectedIndex { get; set; }

        /// <summary>打开搜索</summary>
        public void Open()
        {
            SearchText = "";
            SelectedIndex = 0;
            IsOpen = true;
        }

        /// <summary>关闭搜索</summary>
        public void Close()
        {
            IsOpen = false;
            SearchText = "";
        }

        /// <summary>按名称/类型/ID 搜索图中已有节点</summary>
        public IEnumerable<Node> Search(Graph graph)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return Enumerable.Empty<Node>();

            var query = SearchText.Trim();
            return graph.Nodes.Where(node =>
            {
                // 按类型ID搜索
                if (node.TypeId.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                // 按节点ID搜索
                if (node.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                // 按类型显示名搜索
                var typeDef = graph.Settings.NodeTypes.GetNodeType(node.TypeId);
                if (typeDef != null &&
                    typeDef.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                return false;
            });
        }

        /// <summary>选中并聚焦到指定节点</summary>
        public void NavigateTo(string nodeId, GraphViewModel viewModel)
        {
            viewModel.Selection.Select(nodeId);
            viewModel.FocusNodes(new[] { nodeId });
        }
    }
}
