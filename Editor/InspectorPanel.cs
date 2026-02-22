#nullable enable
using UnityEngine;
using UnityEditor;
using NodeGraph.Core;
using NodeGraph.View;

namespace NodeGraph.Unity
{
    /// <summary>
    /// Inspector 面板基础设施。
    /// 提供标题栏、滚动区域、空状态显示，调用 INodeInspectorDrawer 绘制具体内容。
    /// 宿主窗口在分栏布局中调用 Draw(panelRect) 即可。
    /// </summary>
    public class InspectorPanel
    {
        private readonly INodeInspectorDrawer _drawer;
        private Vector2 _scrollPosition;
        private string? _lastNodeId;

        // ── 样式缓存 ──
        private GUIStyle? _headerStyle;
        private GUIStyle? _emptyLabelStyle;

        public InspectorPanel(INodeInspectorDrawer drawer)
        {
            _drawer = drawer;
        }

        /// <summary>
        /// 绘制 Inspector 面板。
        /// 根据 viewModel 的选中状态自动决定显示节点属性或蓝图全局属性。
        /// 返回 true 表示有属性被修改，需要刷新画布。
        /// </summary>
        public bool Draw(Rect panelRect, GraphViewModel viewModel)
        {
            EnsureStyles();

            bool changed = false;
            var selectedNode = GetSelectedNode(viewModel);

            GUILayout.BeginArea(panelRect);
            {
                // ── 标题栏 ──
                DrawHeader(selectedNode);

                // ── 内容区（带滚动） ──
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                {
                    if (selectedNode != null && _drawer.CanInspect(selectedNode))
                    {
                        changed = _drawer.DrawInspector(selectedNode);
                    }
                    else if (selectedNode == null)
                    {
                        // 无选中：显示蓝图全局属性或空状态
                        changed = _drawer.DrawBlueprintInspector(viewModel.Graph);
                        if (!changed)
                        {
                            GUILayout.Space(20);
                            GUILayout.Label("选择一个节点以编辑属性", _emptyLabelStyle);
                        }
                    }
                    else
                    {
                        GUILayout.Space(20);
                        GUILayout.Label("该节点类型不支持 Inspector 编辑", _emptyLabelStyle);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            GUILayout.EndArea();

            // 选中节点变化时重置滚动位置
            string? currentNodeId = selectedNode?.Id;
            if (currentNodeId != _lastNodeId)
            {
                _scrollPosition = Vector2.zero;
                _lastNodeId = currentNodeId;
            }

            return changed;
        }

        private Node? GetSelectedNode(GraphViewModel viewModel)
        {
            var primaryId = viewModel.Selection.PrimarySelectedNodeId;
            if (primaryId == null) return null;
            return viewModel.Graph.FindNode(primaryId);
        }

        private void DrawHeader(Node? selectedNode)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (selectedNode != null && _drawer.CanInspect(selectedNode))
                {
                    string title = _drawer.GetTitle(selectedNode);
                    GUILayout.Label($"▶ {title}", _headerStyle);
                }
                else
                {
                    GUILayout.Label("Inspector", _headerStyle);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void EnsureStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(4, 4, 0, 0)
                };
            }

            if (_emptyLabelStyle == null)
            {
                _emptyLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    wordWrap = true
                };
            }
        }
    }
}
