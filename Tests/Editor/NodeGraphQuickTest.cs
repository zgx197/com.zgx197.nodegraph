using UnityEngine;
using UnityEditor;
using NodeGraph.Core;
using NodeGraph.Core.Conditions;
using NodeGraph.Math;
using NodeGraph.Commands;
using NodeGraph.Layout;
using NodeGraph.Serialization;
using NodeGraph.View;
using NodeGraph.Abstraction;
using System.Collections.Generic;
using System.Linq;

public static class NodeGraphQuickTest
{
    [MenuItem("Tools/NodeGraph/运行快速测试")]
    public static void RunTest()
    {
        Debug.Log("=== NodeGraph 快速测试开始 ===");

        // ── 1. 数学类型 ──
        var v1 = new Vec2(3, 4);
        Debug.Assert(Mathf.Approximately(v1.Length(), 5f), "Vec2.Length 失败");
        var c = Color4.FromHex("#FF0000");
        Debug.Assert(c.R > 0.99f && c.G < 0.01f, "Color4.FromHex 失败");
        Debug.Log("✓ Math 模块正常");

        // ── 2. 图创建 + 节点/连线 ──
        var settings = new GraphSettings { Topology = GraphTopologyPolicy.DAG };
        var graph = new Graph(settings);
        Debug.Assert(graph.Nodes.Count == 0, "初始节点数应为0");

        var typeDef = new NodeTypeDefinition("Test", "测试节点", "测试",
            new[] {
                new PortDefinition("In", PortDirection.Input),
                new PortDefinition("Out", PortDirection.Output)
            });
        settings.NodeTypes = GraphSettings.CreateEmptyNodeTypeCatalog();

        var nodeA = new Node(IdGenerator.NewId(), "Test", new Vec2(100, 100));
        var portAIn  = new Port(IdGenerator.NewId(), nodeA.Id, typeDef.DefaultPorts[0]);
        var portAOut = new Port(IdGenerator.NewId(), nodeA.Id, typeDef.DefaultPorts[1]);
        nodeA.AddPortDirect(portAIn);
        nodeA.AddPortDirect(portAOut);
        graph.AddNodeDirect(nodeA);

        var nodeB = new Node(IdGenerator.NewId(), "Test", new Vec2(300, 100));
        var portBIn  = new Port(IdGenerator.NewId(), nodeB.Id, typeDef.DefaultPorts[0]);
        var portBOut = new Port(IdGenerator.NewId(), nodeB.Id, typeDef.DefaultPorts[1]);
        nodeB.AddPortDirect(portBIn);
        nodeB.AddPortDirect(portBOut);
        graph.AddNodeDirect(nodeB);

        Debug.Assert(graph.Nodes.Count == 2, "节点数应为2");

        var edge = new Edge(IdGenerator.NewId(), portAOut.Id, portBIn.Id);
        graph.AddEdgeDirect(edge);
        Debug.Assert(graph.Edges.Count == 1, "连线数应为1");
        Debug.Log("✓ Core 模块（Graph/Node/Port/Edge）正常");

        // ── 3. 命令系统 ──
        var history = new CommandHistory(graph);
        Debug.Assert(history.UndoCount == 0, "初始 Undo 栈应为空");
        Debug.Log("✓ Commands 模块正常");

        // ── 4. 图算法 ──
        var roots = GraphAlgorithms.GetRootNodes(graph);
        Debug.Assert(roots.Count() >= 1, "应至少有1个根节点");

        var sorted = GraphAlgorithms.TopologicalSort(graph);
        Debug.Assert(sorted != null, "DAG 应能拓扑排序");
        Debug.Assert(sorted.Count == 2, "拓扑排序应包含2个节点");
        Debug.Log("✓ GraphAlgorithms 正常");

        // ── 5. 布局算法 ──
        var treeLayout = new TreeLayout();
        var positions = treeLayout.ComputeLayout(graph, Vec2.Zero);
        Debug.Assert(positions.Count == 2, "布局应返回2个位置");
        Debug.Log("✓ Layout 模块正常");

        // ── 6. JSON 序列化 ──
        var serializer = new JsonGraphSerializer();
        string json = serializer.Serialize(graph);
        Debug.Assert(!string.IsNullOrEmpty(json), "序列化结果不应为空");
        Debug.Assert(json.Contains(nodeA.Id), "JSON 应包含节点ID");

        var restored = serializer.Deserialize(json);
        Debug.Assert(restored != null, "反序列化不应为null");
        Debug.Assert(restored.Nodes.Count == 2, "反序列化应恢复2个节点");
        Debug.Assert(restored.Edges.Count == 1, "反序列化应恢复1条连线");
        Debug.Log("✓ Serialization 模块正常");

        Debug.Log("=== NodeGraph 快速测试全部通过 ===");

        // 继续运行扩展测试
        TestConditionSystem();
        TestFrameBuilderRefactor(graph);
    }

    // ══════════════════════════════════════
    //  Phase 12: 条件描述系统测试
    // ══════════════════════════════════════

    static void TestConditionSystem()
    {
        Debug.Log("--- Phase 12: 条件描述系统测试 ---");

        // 7.1 LeafCondition 创建 + Clone
        var leaf = new LeafCondition
        {
            TypeId = "Delay",
            Parameters = { ["Duration"] = "3.00" }
        };
        Debug.Assert(leaf.TypeId == "Delay", "LeafCondition.TypeId 应为 Delay");
        Debug.Assert(leaf.Parameters["Duration"] == "3.00", "LeafCondition 参数应为 3.00");

        var leafClone = (LeafCondition)leaf.Clone();
        Debug.Assert(leafClone.TypeId == "Delay", "Clone 后 TypeId 应保持");
        Debug.Assert(leafClone.Parameters["Duration"] == "3.00", "Clone 后参数应保持");
        leafClone.Parameters["Duration"] = "5.00";
        Debug.Assert(leaf.Parameters["Duration"] == "3.00", "Clone 应为深拷贝，修改不影响原始");

        // 7.2 组合条件
        var and = new AndCondition();
        and.Children.Add(new LeafCondition { TypeId = "OnComplete" });
        and.Children.Add(leaf);
        Debug.Assert(and.Children.Count == 2, "AndCondition 应有2个子条件");
        Debug.Assert(and.ToString() == "AND(2)", "AndCondition.ToString");

        var not = new NotCondition { Inner = leaf };
        Debug.Assert(not.ToString() == "NOT(Leaf(Delay))", "NotCondition.ToString");

        Debug.Log("✓ ConditionDescriptor 创建/Clone/组合正常");

        // 7.3 ConditionSerializer 往返
        var model = ConditionSerializer.ToModel(and);
        Debug.Assert(model != null, "ToModel 不应为 null");
        Debug.Assert(model.kind == "and", "Model.kind 应为 and");
        Debug.Assert(model.children != null && model.children.Count == 2, "Model 应有2个子条件");

        var restored = ConditionSerializer.FromModel(model);
        Debug.Assert(restored is AndCondition, "FromModel 应还原为 AndCondition");
        var restoredAnd = (AndCondition)restored;
        Debug.Assert(restoredAnd.Children.Count == 2, "还原后应有2个子条件");
        Debug.Assert(restoredAnd.Children[0] is LeafCondition, "子条件0应为 LeafCondition");

        // null 往返
        Debug.Assert(ConditionSerializer.ToModel(null) == null, "null → ToModel 应为 null");
        Debug.Assert(ConditionSerializer.FromModel(null) == null, "null → FromModel 应为 null");

        Debug.Log("✓ ConditionSerializer 往返正常");

        // 7.4 SimpleJsonCondition 往返
        var leafModel = ConditionSerializer.ToModel(leaf);
        string json = SimpleJsonCondition.Serialize(leafModel);
        Debug.Assert(!string.IsNullOrEmpty(json), "JSON 不应为空");
        Debug.Assert(json.Contains("Delay"), "JSON 应包含 Delay");

        var deserModel = SimpleJsonCondition.Deserialize(json);
        Debug.Assert(deserModel != null, "反序列化不应为 null");
        var deserLeaf = (LeafCondition)ConditionSerializer.FromModel(deserModel);
        Debug.Assert(deserLeaf.TypeId == "Delay", "JSON 往返后 TypeId 应保持");
        Debug.Assert(deserLeaf.Parameters["Duration"] == "3.00", "JSON 往返后参数应保持");

        Debug.Log("✓ SimpleJsonCondition JSON 往返正常");

        // 7.5 ConditionTypeRegistry
        var registry = new ConditionTypeRegistry();
        registry.Register(new ConditionTypeDef
        {
            TypeId = "Delay",
            DisplayName = "延迟",
            Category = "时间",
            Parameters = { new ConditionParamDef { Key = "Duration", DisplayName = "秒数", ParamType = ConditionParamType.Float, DefaultValue = "1.0" } }
        });
        registry.Register(new ConditionTypeDef { TypeId = "OnComplete", DisplayName = "完成时", Category = "信号" });

        Debug.Assert(registry.GetDefinition("Delay") != null, "应能获取 Delay 定义");
        Debug.Assert(registry.GetDefinition("Delay").Parameters.Count == 1, "Delay 应有1个参数");
        Debug.Assert(registry.GetDefinition("OnComplete").IsParameterless, "OnComplete 应无参数");
        Debug.Assert(registry.GetDefinition("Unknown") == null, "未注册类型应返回 null");
        Debug.Assert(registry.AllDefinitions.Count() == 2, "应有2个已注册类型");

        Debug.Log("✓ ConditionTypeRegistry 正常");
        Debug.Log("--- Phase 12 测试全部通过 ---");
    }

    // ══════════════════════════════════════
    //  Phase 13: FrameBuilder 重构测试
    // ══════════════════════════════════════

    /// <summary>简易 ITextMeasurer 实现，用于测试</summary>
    class DummyTextMeasurer : ITextMeasurer
    {
        public Vec2 MeasureText(string text, float fontSize)
        {
            return new Vec2(text.Length * fontSize * 0.5f, fontSize * 1.2f);
        }
    }

    static void TestFrameBuilderRefactor(Graph graph)
    {
        Debug.Log("--- Phase 13: FrameBuilder 重构测试 ---");

        var measurer = new DummyTextMeasurer();
        var theme = NodeVisualTheme.Dark;

        // 8.1 DefaultFrameBuilder 回归测试
        var defaultBuilder = new DefaultFrameBuilder(measurer);
        Debug.Assert(defaultBuilder is BaseFrameBuilder, "DefaultFrameBuilder 应继承 BaseFrameBuilder");

        // 创建 ViewModel 用于 BuildFrame
        var vm = new GraphViewModel(graph, new NodeGraph.View.GraphRenderConfig
        {
            FrameBuilder = defaultBuilder,
            Theme        = theme
        });
        var viewport = new Rect2(0, 0, 800, 600);
        var frame = vm.BuildFrame(viewport);
        Debug.Assert(frame != null, "BuildFrame 不应为 null");
        Debug.Assert(frame.Nodes.Count == 2, $"应有2个节点帧，实际 {frame.Nodes.Count}");
        Debug.Assert(frame.Edges.Count == 1, $"应有1条连线帧，实际 {frame.Edges.Count}");
        Debug.Assert(frame.Background != null, "应有背景帧");

        // 验证端口位置（水平布局：Input 在左，Output 在右）
        var nodeFrameA = frame.Nodes.FirstOrDefault(n => n.NodeId == graph.Nodes[0].Id);
        Debug.Assert(nodeFrameA != null, "应找到节点A的帧");
        var inputPort = nodeFrameA.Ports.FirstOrDefault(p => p.Direction == PortDirection.Input);
        var outputPort = nodeFrameA.Ports.FirstOrDefault(p => p.Direction == PortDirection.Output);
        if (inputPort != null && outputPort != null)
        {
            Debug.Assert(inputPort.Position.X < outputPort.Position.X,
                "水平布局：Input 端口 X 应小于 Output 端口 X");
        }
        Debug.Log("✓ DefaultFrameBuilder 回归测试通过");

        // 8.2 BehaviorTreeFrameBuilder 垂直布局测试
        var btBuilder = new BehaviorTreeFrameBuilder(measurer);
        var btVm = new GraphViewModel(graph, new NodeGraph.View.GraphRenderConfig
        {
            FrameBuilder = btBuilder,
            Theme        = theme
        });
        var btFrame = btVm.BuildFrame(viewport);
        Debug.Assert(btFrame != null, "BT BuildFrame 不应为 null");
        Debug.Assert(btFrame.Nodes.Count == 2, "BT 应有2个节点帧");

        // 验证端口位置（垂直布局：Input 在上，Output 在下）
        var btNodeFrame = btFrame.Nodes.FirstOrDefault(n => n.NodeId == graph.Nodes[0].Id);
        if (btNodeFrame != null)
        {
            var btInput = btNodeFrame.Ports.FirstOrDefault(p => p.Direction == PortDirection.Input);
            var btOutput = btNodeFrame.Ports.FirstOrDefault(p => p.Direction == PortDirection.Output);
            if (btInput != null && btOutput != null)
            {
                Debug.Assert(btInput.Position.Y < btOutput.Position.Y,
                    "垂直布局：Input 端口 Y 应小于 Output 端口 Y");
            }
        }

        // 验证连线切线方向（垂直布局：切线应为 Y 方向）
        if (btFrame.Edges.Count > 0)
        {
            var btEdge = btFrame.Edges[0];
            Debug.Assert(Mathf.Abs(btEdge.TangentA.Y) > 0.1f,
                $"垂直布局：TangentA.Y 应非零，实际 {btEdge.TangentA.Y}");
            Debug.Assert(Mathf.Approximately(btEdge.TangentA.X, 0f),
                $"垂直布局：TangentA.X 应为0，实际 {btEdge.TangentA.X}");
        }
        Debug.Log("✓ BehaviorTreeFrameBuilder 垂直布局测试通过");

        // 8.3 StateMachineFrameBuilder 测试
        var smBuilder = new StateMachineFrameBuilder(measurer);
        var smVm = new GraphViewModel(graph, new NodeGraph.View.GraphRenderConfig
        {
            FrameBuilder = smBuilder,
            Theme        = theme
        });
        var smFrame = smVm.BuildFrame(viewport);
        Debug.Assert(smFrame != null, "SM BuildFrame 不应为 null");

        if (smFrame.Edges.Count > 0)
        {
            var smEdge = smFrame.Edges[0];
            Debug.Assert(Mathf.Abs(smEdge.TangentA.X) > 0.1f,
                $"SM 布局：TangentA.X 应非零，实际 {smEdge.TangentA.X}");
        }
        Debug.Log("✓ StateMachineFrameBuilder 测试通过");

        // 8.4 BezierMath.ComputeVerticalTangents 测试
        var (vTanA, vTanB) = BezierMath.ComputeVerticalTangents(
            new Vec2(100, 100), new Vec2(200, 300));
        Debug.Assert(vTanA.X == 0f, "垂直切线 A.X 应为 0");
        Debug.Assert(vTanA.Y > 0f, "垂直切线 A.Y 应为正（朝下）");
        Debug.Assert(vTanB.X == 0f, "垂直切线 B.X 应为 0");
        Debug.Assert(vTanB.Y < 0f, "垂直切线 B.Y 应为负（朝上）");
        Debug.Log("✓ BezierMath.ComputeVerticalTangents 正常");

        Debug.Log("--- Phase 13 测试全部通过 ---");
    }
}
