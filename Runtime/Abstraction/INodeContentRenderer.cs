#nullable enable
using NodeGraph.Core;
using NodeGraph.Math;

namespace NodeGraph.Abstraction
{
    /// <summary>
    /// 节点内容渲染器接口。业务层实现此接口来自定义节点的"内容区域"渲染。
    /// 框架负责绘制节点外壳（标题栏、端口、状态栏），业务层只负责内容区域。
    /// v2.0: 摘要模式返回数据（NodeContentInfo），编辑模式仍由引擎渲染器直接调用。
    /// </summary>
    public interface INodeContentRenderer
    {
        /// <summary>是否支持内嵌编辑（为 true 时选中节点会调用 DrawEditor）</summary>
        bool SupportsInlineEdit { get; }

        /// <summary>计算摘要视图所需尺寸</summary>
        Vec2 GetSummarySize(Node node, ITextMeasurer measurer);

        /// <summary>获取摘要信息（纯数据，由引擎渲染器消费绘制）</summary>
        NodeContentInfo GetSummaryInfo(Node node, Rect2 contentRect);

        /// <summary>获取折叠模式下的一行文字摘要</summary>
        string GetOneLiner(Node node);

        /// <summary>计算编辑视图所需尺寸</summary>
        Vec2 GetEditorSize(Node node, IEditContext ctx);

        /// <summary>绘制编辑视图（可交互，仅 Expanded + 选中时由引擎渲染器调用）</summary>
        void DrawEditor(Node node, Rect2 rect, IEditContext ctx);
    }
}
