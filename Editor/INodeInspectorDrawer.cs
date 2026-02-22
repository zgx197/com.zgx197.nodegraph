#nullable enable
using NodeGraph.Core;

namespace NodeGraph.Unity
{
    /// <summary>
    /// 节点 Inspector 面板绘制器接口。
    /// 业务层实现此接口，为特定类型的节点提供 Inspector 面板编辑 UI。
    /// 
    /// 与 INodeContentRenderer 的职责区分：
    /// - INodeContentRenderer → 画布上"节点长什么样"（摘要文本、折叠显示）
    /// - INodeInspectorDrawer → Inspector 面板"怎么编辑节点"（完整属性、复杂控件）
    /// 
    /// 使用 EditorGUILayout 自动布局，不受画布缩放影响。
    /// </summary>
    public interface INodeInspectorDrawer
    {
        /// <summary>是否能绘制该节点的 Inspector</summary>
        bool CanInspect(Node node);

        /// <summary>获取 Inspector 标题（通常为节点类型的显示名）</summary>
        string GetTitle(Node node);

        /// <summary>
        /// 绘制节点属性 Inspector（使用 EditorGUILayout 自动布局）。
        /// 返回 true 表示有属性被修改，需要刷新画布。
        /// </summary>
        bool DrawInspector(Node node);

        /// <summary>
        /// 绘制蓝图全局属性（无节点选中时显示）。
        /// 可选实现，默认不绘制。返回 true 表示有修改。
        /// </summary>
        bool DrawBlueprintInspector(Graph graph) => false;
    }
}
