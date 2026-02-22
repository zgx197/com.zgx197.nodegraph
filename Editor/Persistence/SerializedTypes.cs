#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraph.Unity.Persistence
{
    /// <summary>Unity 原生序列化用的端口数据</summary>
    [Serializable]
    public class SerializedPort
    {
        public string id = "";
        public string name = "";
        /// <summary>稳定语义 ID（v2 写入）。空时以 name 作为 fallback。</summary>
        public string semanticId = "";
        public int direction;       // PortDirection 枚举值
        public int kind;            // PortKind 枚举值
        public string dataType = "";
        public int capacity;        // PortCapacity 枚举值
        public int sortOrder;
    }

    /// <summary>Unity 原生序列化用的节点数据</summary>
    [Serializable]
    public class SerializedNode
    {
        public string id = "";
        public string typeId = "";
        public Vector2 position;
        public Vector2 size = new Vector2(200, 100);
        public int displayMode;     // NodeDisplayMode 枚举值
        public bool allowDynamicPorts;
        public List<SerializedPort> ports = new List<SerializedPort>();
        public string userDataJson = "";   // 业务数据 JSON
    }

    /// <summary>Unity 原生序列化用的连线数据</summary>
    [Serializable]
    public class SerializedEdge
    {
        public string id = "";
        public string sourcePortId = "";
        public string targetPortId = "";
        public string userDataJson = "";   // 业务数据 JSON
    }

    /// <summary>Unity 原生序列化用的分组数据</summary>
    [Serializable]
    public class SerializedGroup
    {
        public string id = "";
        public string title = "";
        public Vector2 position;
        public Vector2 size;
        public Color color = new Color(0.2f, 0.3f, 0.5f, 0.3f);
        public List<string> nodeIds = new List<string>();
    }

    /// <summary>Unity 原生序列化用的注释数据</summary>
    [Serializable]
    public class SerializedComment
    {
        public string id = "";
        public string text = "";
        public Vector2 position;
        public Vector2 size = new Vector2(200, 60);
        public float fontSize = 14f;
        public Color textColor = Color.white;
        public Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
    }

    /// <summary>Unity 原生序列化用的子图框数据</summary>
    [Serializable]
    public class SerializedSubGraphFrame
    {
        public string id = "";
        public string title = "";
        public Vector2 position;
        public Vector2 size;
        public List<string> nodeIds = new List<string>();
        public string representativeNodeId = "";
        public bool isCollapsed;
        public string sourceAssetId = "";
    }
}
