#nullable enable
using System.Collections.Generic;

namespace NodeGraph.Serialization
{
    /// <summary>
    /// [框架内部] 图的中间数据传输对象（DTO）。纯 C# POCO，不依赖 Unity 或任何具体存储格式。
    /// </summary>
    /// <remarks>
    /// 在 <see cref="IGraphPersister"/> 的两侧充当桥接层：
    /// Graph（内存）→ GraphDto → JSON/SO ｜ JSON/SO → GraphDto → Graph。
    /// 字段名与 JSON 序列化键保持一致，以保证向后兼容。
    /// </remarks>
    internal sealed class GraphDto
    {
        public string id            = "";
        public int    schemaVersion = 2;
        public GraphSettingsDto     settings       = new();
        public List<NodeDto>        nodes          = new();
        public List<EdgeDto>        edges          = new();
        public List<GroupDto>       groups         = new();
        public List<CommentDto>     comments       = new();
        public List<SubGraphFrameDto> subGraphFrames = new();
    }

    internal sealed class GraphSettingsDto
    {
        public string topology = "DAG";
    }

    internal sealed class NodeDto
    {
        public string  id               = "";
        public string  typeId           = "";
        public Vec2Dto position         = new();
        public Vec2Dto size             = new() { x = 200, y = 100 };
        public string  displayMode      = "Expanded";
        public bool    allowDynamicPorts;
        public List<PortDto> ports      = new();
        public string? userData;
    }

    internal sealed class PortDto
    {
        public string id         = "";
        public string name       = "";
        public string semanticId = "";
        public string direction  = "Input";
        public string kind       = "Data";
        public string dataType   = "";
        public string capacity   = "Multiple";
        public int    sortOrder;
    }

    internal sealed class EdgeDto
    {
        public string id         = "";
        public string fromNodeId = "";
        public string fromPortId = "";
        public string toNodeId   = "";
        public string toPortId   = "";
    }

    internal sealed class GroupDto
    {
        public string          id       = "";
        public string          title    = "";
        public Vec2Dto         position = new();
        public Vec2Dto         size     = new();
        public Color4Dto       color    = new() { r = 0.3f, g = 0.5f, b = 0.8f, a = 0.3f };
        public List<string>    nodeIds  = new();
    }

    internal sealed class CommentDto
    {
        public string   id              = "";
        public string   text            = "";
        public Vec2Dto  position        = new();
        public Vec2Dto  size            = new() { x = 200, y = 60 };
        public float    fontSize        = 14f;
        public Color4Dto textColor      = new() { r = 1, g = 1, b = 1, a = 1 };
        public Color4Dto backgroundColor = new() { r = 0.2f, g = 0.2f, b = 0.2f, a = 0.7f };
    }

    internal sealed class SubGraphFrameDto
    {
        public string       id                   = "";
        public string       title                = "";
        public Vec2Dto      position             = new();
        public Vec2Dto      size                 = new();
        public List<string> nodeIds              = new();
        public string       representativeNodeId = "";
        public bool         isCollapsed;
        public string?      sourceAssetId;
    }

    internal sealed class Vec2Dto
    {
        public float x;
        public float y;
    }

    internal sealed class Color4Dto
    {
        public float r;
        public float g;
        public float b;
        public float a = 1f;
    }
}
