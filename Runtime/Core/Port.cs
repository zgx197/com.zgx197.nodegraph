#nullable enable
using System;

namespace NodeGraph.Core
{
    /// <summary>端口方向</summary>
    public enum PortDirection
    {
        Input,
        Output
    }

    /// <summary>端口类别：控制流 / 事件流 / 数据流</summary>
    public enum PortKind
    {
        /// <summary>控制流——同步执行，决定执行顺序（黑色实线）</summary>
        Control,
        /// <summary>数据流——传递配置或状态，不影响执行（蓝色线）</summary>
        Data,
        /// <summary>事件流——异步触发，条件满足时触发（橙色虚线）</summary>
        Event
    }

    /// <summary>端口连接容量</summary>
    public enum PortCapacity
    {
        /// <summary>只能连一条线</summary>
        Single,
        /// <summary>可以连多条线</summary>
        Multiple
    }

    /// <summary>
    /// [扩展点] 端口定义模板。业务层在 <see cref="NodeTypeDefinition.DefaultPorts"/> 中声明默认端口时使用。
    /// </summary>
    /// <remarks>
    /// 业务层负责创建，框架负责从定义实例化 <see cref="Port"/>。
    /// 关键字段：<see cref="SemanticId"/>（稳定序列化 ID）和 <see cref="Name"/>（UI 显示名）分离设计。
    /// </remarks>
    public class PortDefinition
    {
        /// <summary>端口显示名称（UI 用，可自由修改）</summary>
        public string Name { get; }
        /// <summary>
        /// 稳定语义 ID（序列化、连线引用使用）。
        /// 若未显式指定，默认等于 Name。
        /// </summary>
        public string SemanticId { get; }
        public PortDirection Direction { get; }
        public PortKind Kind { get; }
        public string DataType { get; }
        public PortCapacity Capacity { get; }
        public int SortOrder { get; }

        public PortDefinition(
            string name,
            PortDirection direction,
            PortKind kind = PortKind.Control,
            string dataType = "exec",
            PortCapacity capacity = PortCapacity.Multiple,
            int sortOrder = 0,
            string? semanticId = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SemanticId = string.IsNullOrEmpty(semanticId) ? name : semanticId;
            Direction = direction;
            Kind = kind;
            DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            Capacity = capacity;
            SortOrder = sortOrder;
        }
    }

    /// <summary>
    /// [框架核心模型] 端口实例，属于某个节点。每个端口有唯一 ID、方向、类别和数据类型。
    /// </summary>
    /// <remarks>
    /// <see cref="Id"/>    — 实例 GUID，在 Graph 内唯一，连线与端口索引使用此 ID。<br/>
    /// <see cref="SemanticId"/> — 稳定语义 ID，来自 <see cref="PortDefinition.SemanticId"/>，序列化和连线匹配使用此 ID。<br/>
    /// 实例由 <see cref="Node.AddPort"/> 创建，不应在业务层直接构造。
    /// </remarks>
    public class Port
    {
        /// <summary>端口唯一 ID（GUID）</summary>
        public string Id { get; }

        /// <summary>所属节点 ID</summary>
        public string NodeId { get; }

        /// <summary>端口显示名称</summary>
        public string Name { get; set; }

        /// <summary>
        /// 稳定语义 ID（序列化、连线引用使用）。
        /// 来自 PortDefinition.SemanticId，与实例 GUID（Id）不同。
        /// </summary>
        public string SemanticId { get; }

        /// <summary>输入 / 输出</summary>
        public PortDirection Direction { get; }

        /// <summary>控制流 / 数据流</summary>
        public PortKind Kind { get; }

        /// <summary>数据类型标识（如 "exec" / "float" / "int" / "string"）</summary>
        public string DataType { get; }

        /// <summary>连接容量：单连接 / 多连接</summary>
        public PortCapacity Capacity { get; }

        /// <summary>端口排序顺序（行为树中子节点顺序等）</summary>
        public int SortOrder { get; set; }

        public Port(string id, string nodeId, PortDefinition definition)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            Name = definition.Name;
            SemanticId = definition.SemanticId;
            Direction = definition.Direction;
            Kind = definition.Kind;
            DataType = definition.DataType;
            Capacity = definition.Capacity;
        }

        /// <summary>内部构造：用于反序列化等需要直接指定所有字段的场景</summary>
        internal Port(
            string id,
            string nodeId,
            string name,
            PortDirection direction,
            PortKind kind,
            string dataType,
            PortCapacity capacity,
            int sortOrder = 0,
            string? semanticId = null)
        {
            Id = id;
            NodeId = nodeId;
            Name = name;
            SemanticId = string.IsNullOrEmpty(semanticId) ? name : semanticId;
            Direction = direction;
            Kind = kind;
            DataType = dataType;
            Capacity = capacity;
            SortOrder = sortOrder;
        }

        public override string ToString() => $"Port({SemanticId}, {Direction}, {Kind}, {DataType})";
    }
}
