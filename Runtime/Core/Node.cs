#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Math;

namespace NodeGraph.Core
{
    /// <summary>节点显示模式</summary>
    public enum NodeDisplayMode
    {
        /// <summary>完整：标题 + 端口 + 内容/编辑器</summary>
        Expanded,
        /// <summary>摘要：标题 + 端口 + 一行摘要</summary>
        Collapsed,
        /// <summary>最小：单行，仅标题和端口</summary>
        Minimized
    }

    /// <summary>节点视觉状态</summary>
    public enum NodeState
    {
        Normal,
        Selected,
        Highlighted,
        Error,
        /// <summary>用于调试/预览执行状态</summary>
        Running
    }

    /// <summary>
    /// [框架核心模型] 节点实例。每个节点有唯一 ID、类型、位置、端口列表和可选的业务数据。
    /// </summary>
    /// <remarks>
    /// 节点实例由 <see cref="Graph"/> 创建和管理，不应在业务层直接构造。
    /// 通过 <see cref="INodeData"/> （<see cref="UserData"/>）附加业务数据。
    /// </remarks>
    public class Node
    {
        private readonly List<Port> _ports = new List<Port>();

        /// <summary>节点唯一 ID（GUID）</summary>
        public string Id { get; }

        /// <summary>节点类型标识（对应 NodeTypeDefinition.TypeId）</summary>
        public string TypeId { get; }

        /// <summary>画布坐标</summary>
        public Vec2 Position { get; set; }

        /// <summary>节点尺寸（由渲染层计算后写回）</summary>
        public Vec2 Size { get; set; }

        /// <summary>显示模式</summary>
        public NodeDisplayMode DisplayMode { get; set; } = NodeDisplayMode.Expanded;

        /// <summary>视觉状态</summary>
        public NodeState State { get; set; } = NodeState.Normal;

        /// <summary>业务层附加数据</summary>
        public INodeData? UserData { get; set; }

        /// <summary>是否允许动态增减端口</summary>
        public bool AllowDynamicPorts { get; set; }

        /// <summary>端口只读列表</summary>
        public IReadOnlyList<Port> Ports => _ports;

        // ── 端口事件 ──

        /// <summary>端口被添加时触发</summary>
        public event Action<Port>? OnPortAdded;

        /// <summary>端口被移除时触发</summary>
        public event Action<Port>? OnPortRemoved;

        // ── 构造 ──

        public Node(string id, string typeId, Vec2 position)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            TypeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
            Position = position;
        }

        // ── 端口操作 ──

        /// <summary>根据端口定义添加新端口，返回创建的端口实例</summary>
        public Port AddPort(PortDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            var port = new Port(IdGenerator.NewId(), Id, definition);
            port.SortOrder = _ports.Count;
            _ports.Add(port);
            OnPortAdded?.Invoke(port);
            return port;
        }

        /// <summary>移除指定 ID 的端口</summary>
        /// <returns>是否成功移除</returns>
        public bool RemovePort(string portId)
        {
            if (portId == null) throw new ArgumentNullException(nameof(portId));

            var port = _ports.Find(p => p.Id == portId);
            if (port == null) return false;

            _ports.Remove(port);
            OnPortRemoved?.Invoke(port);
            return true;
        }

        /// <summary>根据 ID 查找端口</summary>
        public Port? FindPort(string portId)
        {
            if (portId == null) return null;
            return _ports.Find(p => p.Id == portId);
        }

        /// <summary>获取所有输入端口</summary>
        public IEnumerable<Port> GetInputPorts() =>
            _ports.Where(p => p.Direction == PortDirection.Input);

        /// <summary>获取所有输出端口</summary>
        public IEnumerable<Port> GetOutputPorts() =>
            _ports.Where(p => p.Direction == PortDirection.Output);

        /// <summary>获取节点的包围矩形</summary>
        public Rect2 GetBounds() => new Rect2(Position, Size);

        /// <summary>内部方法：直接添加已构造好的端口（用于反序列化）</summary>
        internal void AddPortDirect(Port port)
        {
            _ports.Add(port);
        }

        public override string ToString() => $"Node({Id}: {TypeId})";
    }
}
