#nullable enable
using System;
using NodeGraph.Math;

namespace NodeGraph.Core
{
    /// <summary>
    /// [扩展点] 节点类型定义。描述一种节点的元数据：显示名、分类、颜色、默认端口等。
    /// </summary>
    /// <remarks>
    /// 业务层创建并通过 <c>NodeTypeRegistry.Register()</c> 注册自定义节点类型。
    /// 关键字段：<see cref="TypeId"/> 必须全局唯一；<see cref="DefaultPorts"/> 中声明的端口由框架在创建节点时自动实例化。
    /// 若需初始化业务数据，设置 <see cref="CreateDefaultData"/> 工厂方法。
    /// </remarks>
    public class NodeTypeDefinition
    {
        /// <summary>唯一类型标识（如 "SpawnTask"、"StateMachine/State"）</summary>
        public string TypeId { get; }

        /// <summary>显示名称</summary>
        public string DisplayName { get; }

        /// <summary>分类路径（如 "Spawn/Task"），用于搜索菜单分组</summary>
        public string Category { get; }

        /// <summary>节点标题栏颜色</summary>
        public Color4 Color { get; set; } = new Color4(0.3f, 0.3f, 0.3f, 1f);

        /// <summary>默认端口模板</summary>
        public PortDefinition[] DefaultPorts { get; }

        /// <summary>图中是否允许该类型的多个实例</summary>
        public bool AllowMultiple { get; set; } = true;

        /// <summary>是否允许动态增减端口</summary>
        public bool AllowDynamicPorts { get; set; }

        /// <summary>创建默认业务数据的工厂方法（可为 null）</summary>
        public Func<INodeData>? CreateDefaultData { get; set; }

        public NodeTypeDefinition(
            string typeId,
            string displayName,
            string category = "",
            PortDefinition[]? defaultPorts = null)
        {
            TypeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Category = category ?? "";
            DefaultPorts = defaultPorts ?? Array.Empty<PortDefinition>();
        }

        public override string ToString() => $"NodeType({TypeId}: {DisplayName})";
    }
}
