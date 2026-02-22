#nullable enable
using System;

namespace NodeGraph.Core
{
    /// <summary>
    /// 图事件集合。上层（View / ViewModel）可订阅这些事件来响应图的变化。
    /// </summary>
    public class GraphEvents
    {
        // ── 节点事件 ──

        /// <summary>节点被添加后触发</summary>
        public event Action<Node>? OnNodeAdded;

        /// <summary>节点被移除前触发（此时节点仍在图中）</summary>
        public event Action<Node>? OnNodeRemoved;

        /// <summary>节点位置发生变化后触发</summary>
        public event Action<Node>? OnNodeMoved;

        // ── 连线事件 ──

        /// <summary>连线被创建后触发</summary>
        public event Action<Edge>? OnEdgeAdded;

        /// <summary>连线被移除前触发</summary>
        public event Action<Edge>? OnEdgeRemoved;

        // ── 装饰元素事件 ──

        /// <summary>分组被创建后触发</summary>
        public event Action<NodeGroup>? OnGroupAdded;

        /// <summary>分组被移除前触发</summary>
        public event Action<NodeGroup>? OnGroupRemoved;

        /// <summary>注释被创建后触发</summary>
        public event Action<GraphComment>? OnCommentAdded;

        /// <summary>注释被移除前触发</summary>
        public event Action<GraphComment>? OnCommentRemoved;

        // ── 内部触发方法（仅供 Graph 调用） ──

        internal void RaiseNodeAdded(Node node) => OnNodeAdded?.Invoke(node);
        internal void RaiseNodeRemoved(Node node) => OnNodeRemoved?.Invoke(node);
        internal void RaiseNodeMoved(Node node) => OnNodeMoved?.Invoke(node);
        internal void RaiseEdgeAdded(Edge edge) => OnEdgeAdded?.Invoke(edge);
        internal void RaiseEdgeRemoved(Edge edge) => OnEdgeRemoved?.Invoke(edge);
        internal void RaiseGroupAdded(NodeGroup group) => OnGroupAdded?.Invoke(group);
        internal void RaiseGroupRemoved(NodeGroup group) => OnGroupRemoved?.Invoke(group);
        internal void RaiseCommentAdded(GraphComment comment) => OnCommentAdded?.Invoke(comment);
        internal void RaiseCommentRemoved(GraphComment comment) => OnCommentRemoved?.Invoke(comment);
    }
}
