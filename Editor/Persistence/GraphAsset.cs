#nullable enable
using System.Collections.Generic;
using UnityEngine;
using NodeGraph.Core;

namespace NodeGraph.Unity.Persistence
{
    /// <summary>
    /// 图资产 ScriptableObject。Unity 原生序列化存储。
    /// 结构数据直接用 [Serializable] 字段，业务数据用 JSON 字符串。
    /// </summary>
    [CreateAssetMenu(menuName = "NodeGraph/Graph Asset", fileName = "NewGraphAsset")]
    public class GraphAsset : ScriptableObject
    {
        [SerializeField] private string _graphId = "";
        [SerializeField] private int _topology;  // GraphTopologyPolicy 枚举值
        [SerializeField] private List<SerializedNode> _nodes = new List<SerializedNode>();
        [SerializeField] private List<SerializedEdge> _edges = new List<SerializedEdge>();
        [SerializeField] private List<SerializedGroup> _groups = new List<SerializedGroup>();
        [SerializeField] private List<SerializedComment> _comments = new List<SerializedComment>();
        [SerializeField] private List<SerializedSubGraphFrame> _subGraphFrames = new List<SerializedSubGraphFrame>();

        public string GraphId => _graphId;
        public int Topology { get => _topology; set => _topology = value; }
        public List<SerializedNode> Nodes => _nodes;
        public List<SerializedEdge> Edges => _edges;
        public List<SerializedGroup> Groups => _groups;
        public List<SerializedComment> Comments => _comments;
        public List<SerializedSubGraphFrame> SubGraphFrames => _subGraphFrames;

        /// <summary>设置图ID（首次创建时）</summary>
        public void SetGraphId(string id) => _graphId = id;
    }
}
