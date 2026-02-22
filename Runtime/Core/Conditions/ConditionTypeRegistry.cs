#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeGraph.Core.Conditions
{
    /// <summary>条件参数的值类型</summary>
    public enum ConditionParamType
    {
        String,
        Int,
        Float,
        Bool,
        Enum
    }

    /// <summary>
    /// 条件参数定义。描述一个条件类型所需的单个参数。
    /// </summary>
    public class ConditionParamDef
    {
        /// <summary>参数键名（存储在 LeafCondition.Parameters 中的 key）</summary>
        public string Key { get; set; } = "";

        /// <summary>参数显示名称（Inspector 中显示）</summary>
        public string DisplayName { get; set; } = "";

        /// <summary>参数值类型</summary>
        public ConditionParamType ParamType { get; set; } = ConditionParamType.String;

        /// <summary>默认值（字符串形式）</summary>
        public string? DefaultValue { get; set; }

        /// <summary>枚举类型的可选值列表（仅 ParamType == Enum 时有效）</summary>
        public string[]? EnumValues { get; set; }
    }

    /// <summary>
    /// 条件类型定义。由业务层注册，描述一种叶子条件的元数据。
    /// </summary>
    public class ConditionTypeDef
    {
        /// <summary>条件类型标识（与 LeafCondition.TypeId 对应）</summary>
        public string TypeId { get; set; } = "";

        /// <summary>显示名称（Inspector / 标签中显示）</summary>
        public string DisplayName { get; set; } = "";

        /// <summary>分类（用于 Inspector 下拉分组）</summary>
        public string Category { get; set; } = "";

        /// <summary>参数定义列表</summary>
        public List<ConditionParamDef> Parameters { get; set; } = new List<ConditionParamDef>();

        /// <summary>是否无参数（如 OnComplete、Immediate 等信号型条件）</summary>
        public bool IsParameterless => Parameters.Count == 0;
    }

    /// <summary>
    /// 条件类型注册表接口。业务层通过此接口注册自定义条件类型。
    /// </summary>
    public interface IConditionTypeRegistry
    {
        void Register(ConditionTypeDef definition);
        ConditionTypeDef? GetDefinition(string typeId);
        IEnumerable<ConditionTypeDef> AllDefinitions { get; }
    }

    /// <summary>
    /// 条件类型注册表默认实现。
    /// </summary>
    public class ConditionTypeRegistry : IConditionTypeRegistry
    {
        private readonly Dictionary<string, ConditionTypeDef> _definitions
            = new Dictionary<string, ConditionTypeDef>();

        public void Register(ConditionTypeDef definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrEmpty(definition.TypeId))
                throw new ArgumentException("ConditionTypeDef.TypeId 不能为空");
            _definitions[definition.TypeId] = definition;
        }

        public ConditionTypeDef? GetDefinition(string typeId)
        {
            if (typeId == null) return null;
            _definitions.TryGetValue(typeId, out var def);
            return def;
        }

        public IEnumerable<ConditionTypeDef> AllDefinitions => _definitions.Values;
    }
}
