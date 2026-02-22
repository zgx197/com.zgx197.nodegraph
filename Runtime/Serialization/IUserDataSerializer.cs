#nullable enable
using NodeGraph.Core;

namespace NodeGraph.Serialization
{
    /// <summary>
    /// 业务层数据的 JSON 序列化接口。
    /// 框架不知道 INodeData 的具体类型，需要业务层实现此接口。
    /// </summary>
    public interface IUserDataSerializer
    {
        /// <summary>序列化节点业务数据为 JSON 字符串</summary>
        string SerializeNodeData(INodeData data);

        /// <summary>反序列化节点业务数据</summary>
        INodeData? DeserializeNodeData(string typeId, string json);
    }
}
