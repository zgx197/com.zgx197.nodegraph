#nullable enable
using NodeGraph.Core.Conditions;

namespace NodeGraph.Serialization
{
    /// <summary>
    /// ConditionModel 的轻量级 JSON 序列化包装。
    /// 放在 Serialization 程序集中（可访问 SimpleJson + Core.Conditions）。
    /// </summary>
    public static class SimpleJsonCondition
    {
        public static string Serialize(ConditionModel model)
        {
            return SimpleJson.Serialize(model);
        }

        public static ConditionModel? Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return SimpleJson.Deserialize<ConditionModel>(json);
        }
    }
}
