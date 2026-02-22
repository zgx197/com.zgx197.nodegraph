#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeGraph.Core
{
    /// <summary>
    /// 子图框索引——管理 <see cref="SubGraphFrame"/> 的增删查，
    /// 从 <see cref="Graph"/> 中剥离，使 Graph 核心类保持精简。
    /// </summary>
    public sealed class SubGraphIndex
    {
        private readonly List<SubGraphFrame> _frames = new List<SubGraphFrame>();

        /// <summary>所有子图框（只读视图）</summary>
        public IReadOnlyList<SubGraphFrame> All => _frames;

        /// <summary>添加子图框（内部低层 API，供 Command/反序列化使用）</summary>
        internal void Add(SubGraphFrame frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            _frames.Add(frame);
        }

        /// <summary>移除子图框（不移除框内的节点和代表节点）</summary>
        public void Remove(string frameId)
        {
            if (frameId == null) throw new ArgumentNullException(nameof(frameId));
            var frame = _frames.FirstOrDefault(f => f.Id == frameId);
            if (frame != null) _frames.Remove(frame);
        }

        /// <summary>当节点被删除时，从所有框的 ContainedNodeIds 中移除该节点 ID</summary>
        public void OnNodeRemoved(string nodeId)
        {
            foreach (var frame in _frames)
                frame.ContainedNodeIds.Remove(nodeId);
        }

        /// <summary>根据 ID 查找子图框</summary>
        public SubGraphFrame? FindById(string frameId)
        {
            if (frameId == null) return null;
            return _frames.FirstOrDefault(f => f.Id == frameId);
        }

        /// <summary>查找包含指定节点（ContainedNodeIds 或 RepresentativeNodeId）的子图框</summary>
        public SubGraphFrame? FindContainer(string nodeId)
        {
            if (nodeId == null) return null;
            return _frames.FirstOrDefault(f =>
                f.ContainedNodeIds.Contains(nodeId) || f.RepresentativeNodeId == nodeId);
        }
    }
}
