#nullable enable
using System;
using System.Collections.Generic;

namespace NodeGraph.Core.Conditions
{
    /// <summary>
    /// 条件描述基类。可序列化的条件树结构，框架层只管组合，不知道业务语义。
    /// 叶子节点由业务层通过 TypeId 定义具体含义。
    /// </summary>
    [Serializable]
    public abstract class ConditionDescriptor
    {
        /// <summary>深拷贝</summary>
        public abstract ConditionDescriptor Clone();
    }

    /// <summary>
    /// 叶子条件。具体语义由业务层通过 TypeId 定义。
    /// 例如："Delay"（延迟）、"OnComplete"（完成时）、"CompareInt"（整数比较）等。
    /// </summary>
    [Serializable]
    public class LeafCondition : ConditionDescriptor
    {
        /// <summary>条件类型标识（如 "Delay", "CompareInt", "HasTarget"）</summary>
        public string TypeId { get; set; } = "";

        /// <summary>参数键值对（由业务层根据 TypeId 解释）</summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        public override ConditionDescriptor Clone()
        {
            return new LeafCondition
            {
                TypeId = TypeId,
                Parameters = new Dictionary<string, string>(Parameters)
            };
        }

        public override string ToString() => $"Leaf({TypeId})";
    }

    /// <summary>逻辑与组合：所有子条件都满足时为 true</summary>
    [Serializable]
    public class AndCondition : ConditionDescriptor
    {
        public List<ConditionDescriptor> Children { get; set; } = new List<ConditionDescriptor>();

        public override ConditionDescriptor Clone()
        {
            var clone = new AndCondition();
            foreach (var child in Children)
                clone.Children.Add(child.Clone());
            return clone;
        }

        public override string ToString() => $"AND({Children.Count})";
    }

    /// <summary>逻辑或组合：任一子条件满足时为 true</summary>
    [Serializable]
    public class OrCondition : ConditionDescriptor
    {
        public List<ConditionDescriptor> Children { get; set; } = new List<ConditionDescriptor>();

        public override ConditionDescriptor Clone()
        {
            var clone = new OrCondition();
            foreach (var child in Children)
                clone.Children.Add(child.Clone());
            return clone;
        }

        public override string ToString() => $"OR({Children.Count})";
    }

    /// <summary>逻辑非：内部条件取反</summary>
    [Serializable]
    public class NotCondition : ConditionDescriptor
    {
        public ConditionDescriptor Inner { get; set; } = null!;

        public override ConditionDescriptor Clone()
        {
            return new NotCondition { Inner = Inner.Clone() };
        }

        public override string ToString() => $"NOT({Inner})";
    }
}
