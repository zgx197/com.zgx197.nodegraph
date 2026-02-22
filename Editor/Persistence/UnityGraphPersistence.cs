#nullable enable
using UnityEditor;
using NodeGraph.Abstraction;
using NodeGraph.Core;

namespace NodeGraph.Unity.Persistence
{
    /// <summary>
    /// Unity 持久化实现。通过 GraphAsset (ScriptableObject) 保存和加载图。
    /// </summary>
    public class UnityGraphPersistence : IGraphPersistence
    {
        private readonly GraphAsset _asset;
        private bool _isDirty;

        public bool IsDirty => _isDirty;

        public UnityGraphPersistence(GraphAsset asset)
        {
            _asset = asset;
        }

        public void Save(Graph graph)
        {
            GraphAssetConverter.SaveToAsset(graph, _asset);
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssetIfDirty(_asset);
            _isDirty = false;
        }

        public Graph? Load()
        {
            if (string.IsNullOrEmpty(_asset.GraphId)) return null;
            var graph = GraphAssetConverter.LoadFromAsset(_asset);
            _isDirty = false;
            return graph;
        }

        /// <summary>标记为脏（有未保存的修改）</summary>
        public void MarkDirty() => _isDirty = true;
    }
}
