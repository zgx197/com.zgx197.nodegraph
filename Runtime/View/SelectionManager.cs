#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeGraph.View
{
    /// <summary>
    /// 选中状态管理器。管理节点和连线的选中状态。
    /// </summary>
    public class SelectionManager
    {
        private readonly List<string> _selectedNodeIds = new List<string>();
        private readonly List<string> _selectedEdgeIds = new List<string>();

        /// <summary>选中的节点 ID 列表</summary>
        public IReadOnlyList<string> SelectedNodeIds => _selectedNodeIds;

        /// <summary>选中的连线 ID 列表</summary>
        public IReadOnlyList<string> SelectedEdgeIds => _selectedEdgeIds;

        /// <summary>主选中节点（最后一个被选中的）</summary>
        public string? PrimarySelectedNodeId { get; private set; }

        /// <summary>是否有选中的元素</summary>
        public bool HasSelection => _selectedNodeIds.Count > 0 || _selectedEdgeIds.Count > 0;

        /// <summary>选中状态变化时触发</summary>
        public event Action? OnSelectionChanged;

        // ── 节点选中操作 ──

        /// <summary>单选节点（清空其他选中）</summary>
        public void Select(string nodeId)
        {
            if (nodeId == null) return;
            _selectedNodeIds.Clear();
            _selectedEdgeIds.Clear();
            _selectedNodeIds.Add(nodeId);
            PrimarySelectedNodeId = nodeId;
            OnSelectionChanged?.Invoke();
        }

        /// <summary>追加选中（Shift + 点击）</summary>
        public void AddToSelection(string nodeId)
        {
            if (nodeId == null) return;
            if (_selectedNodeIds.Contains(nodeId)) return;
            _selectedNodeIds.Add(nodeId);
            PrimarySelectedNodeId = nodeId;
            OnSelectionChanged?.Invoke();
        }

        /// <summary>取消选中（Ctrl + 点击）</summary>
        public void RemoveFromSelection(string nodeId)
        {
            if (nodeId == null) return;
            if (!_selectedNodeIds.Remove(nodeId)) return;
            if (PrimarySelectedNodeId == nodeId)
                PrimarySelectedNodeId = _selectedNodeIds.Count > 0 ? _selectedNodeIds[_selectedNodeIds.Count - 1] : null;
            OnSelectionChanged?.Invoke();
        }

        /// <summary>框选多个节点</summary>
        public void SelectMultiple(IEnumerable<string> nodeIds)
        {
            _selectedNodeIds.Clear();
            _selectedEdgeIds.Clear();
            _selectedNodeIds.AddRange(nodeIds);
            PrimarySelectedNodeId = _selectedNodeIds.Count > 0 ? _selectedNodeIds[_selectedNodeIds.Count - 1] : null;
            OnSelectionChanged?.Invoke();
        }

        /// <summary>追加框选（Shift + 框选）</summary>
        public void AddMultipleToSelection(IEnumerable<string> nodeIds)
        {
            bool changed = false;
            foreach (var id in nodeIds)
            {
                if (!_selectedNodeIds.Contains(id))
                {
                    _selectedNodeIds.Add(id);
                    PrimarySelectedNodeId = id;
                    changed = true;
                }
            }
            if (changed) OnSelectionChanged?.Invoke();
        }

        /// <summary>从选中中移除多个（Ctrl + 框选）</summary>
        public void RemoveMultipleFromSelection(IEnumerable<string> nodeIds)
        {
            bool changed = false;
            foreach (var id in nodeIds)
            {
                if (_selectedNodeIds.Remove(id))
                    changed = true;
            }
            if (changed)
            {
                if (PrimarySelectedNodeId != null && !_selectedNodeIds.Contains(PrimarySelectedNodeId))
                    PrimarySelectedNodeId = _selectedNodeIds.Count > 0 ? _selectedNodeIds[_selectedNodeIds.Count - 1] : null;
                OnSelectionChanged?.Invoke();
            }
        }

        /// <summary>清空所有选中</summary>
        public void ClearSelection()
        {
            if (_selectedNodeIds.Count == 0 && _selectedEdgeIds.Count == 0) return;
            _selectedNodeIds.Clear();
            _selectedEdgeIds.Clear();
            PrimarySelectedNodeId = null;
            OnSelectionChanged?.Invoke();
        }

        /// <summary>是否选中了指定节点</summary>
        public bool IsSelected(string nodeId) => nodeId != null && _selectedNodeIds.Contains(nodeId);

        // ── 连线选中操作 ──

        /// <summary>选中连线（清空其他选中）</summary>
        public void SelectEdge(string edgeId)
        {
            if (edgeId == null) return;
            _selectedNodeIds.Clear();
            _selectedEdgeIds.Clear();
            _selectedEdgeIds.Add(edgeId);
            PrimarySelectedNodeId = null;
            OnSelectionChanged?.Invoke();
        }

        /// <summary>是否选中了指定连线</summary>
        public bool IsEdgeSelected(string edgeId) => edgeId != null && _selectedEdgeIds.Contains(edgeId);

        // ── 全选 ──

        /// <summary>全选所有节点</summary>
        public void SelectAll(IEnumerable<string> allNodeIds)
        {
            _selectedNodeIds.Clear();
            _selectedNodeIds.AddRange(allNodeIds);
            PrimarySelectedNodeId = _selectedNodeIds.Count > 0 ? _selectedNodeIds[_selectedNodeIds.Count - 1] : null;
            OnSelectionChanged?.Invoke();
        }
    }
}
