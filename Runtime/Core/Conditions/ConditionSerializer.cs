#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeGraph.Core.Conditions
{
    /// <summary>
    /// ConditionDescriptor 的序列化中间模型。
    /// 使用 "kind" 字段区分多态类型，供 JSON 和 Unity SO 序列化共用。
    /// </summary>
    [Serializable]
    public class ConditionModel
    {
        /// <summary>条件类型：leaf / and / or / not</summary>
        public string kind = "";

        /// <summary>叶子条件的 TypeId（仅 kind=leaf）</summary>
        public string? typeId;

        /// <summary>叶子条件的参数（仅 kind=leaf）</summary>
        public List<ConditionParamModel>? parameters;

        /// <summary>子条件列表（kind=and/or）</summary>
        public List<ConditionModel>? children;

        /// <summary>内部条件（kind=not）</summary>
        public ConditionModel? inner;
    }

    /// <summary>条件参数键值对</summary>
    [Serializable]
    public class ConditionParamModel
    {
        public string key = "";
        public string value = "";
    }

    /// <summary>
    /// ConditionDescriptor ↔ ConditionModel 双向转换工具。
    /// 框架层提供，JSON 序列化器和 Unity 持久化层共用。
    /// </summary>
    public static class ConditionSerializer
    {
        // ══════════════════════════════════════
        //  ConditionDescriptor → ConditionModel
        // ══════════════════════════════════════

        public static ConditionModel? ToModel(ConditionDescriptor? descriptor)
        {
            if (descriptor == null) return null;

            switch (descriptor)
            {
                case LeafCondition leaf:
                    return new ConditionModel
                    {
                        kind = "leaf",
                        typeId = leaf.TypeId,
                        parameters = leaf.Parameters.Count > 0
                            ? leaf.Parameters.Select(kv => new ConditionParamModel { key = kv.Key, value = kv.Value }).ToList()
                            : null
                    };

                case AndCondition and:
                    return new ConditionModel
                    {
                        kind = "and",
                        children = and.Children.Select(c => ToModel(c)!).ToList()
                    };

                case OrCondition or:
                    return new ConditionModel
                    {
                        kind = "or",
                        children = or.Children.Select(c => ToModel(c)!).ToList()
                    };

                case NotCondition not:
                    return new ConditionModel
                    {
                        kind = "not",
                        inner = ToModel(not.Inner)
                    };

                default:
                    throw new ArgumentException($"未知的 ConditionDescriptor 类型: {descriptor.GetType().Name}");
            }
        }

        // ══════════════════════════════════════
        //  ConditionModel → ConditionDescriptor
        // ══════════════════════════════════════

        public static ConditionDescriptor? FromModel(ConditionModel? model)
        {
            if (model == null) return null;

            switch (model.kind)
            {
                case "leaf":
                    var leaf = new LeafCondition { TypeId = model.typeId ?? "" };
                    if (model.parameters != null)
                    {
                        foreach (var p in model.parameters)
                            leaf.Parameters[p.key] = p.value;
                    }
                    return leaf;

                case "and":
                    var and = new AndCondition();
                    if (model.children != null)
                    {
                        foreach (var child in model.children)
                        {
                            var c = FromModel(child);
                            if (c != null) and.Children.Add(c);
                        }
                    }
                    return and;

                case "or":
                    var or = new OrCondition();
                    if (model.children != null)
                    {
                        foreach (var child in model.children)
                        {
                            var c = FromModel(child);
                            if (c != null) or.Children.Add(c);
                        }
                    }
                    return or;

                case "not":
                    var inner = FromModel(model.inner);
                    if (inner == null) return null;
                    return new NotCondition { Inner = inner };

                default:
                    return null;
            }
        }
    }
}
