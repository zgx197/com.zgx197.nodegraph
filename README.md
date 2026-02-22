# com.zgx197.nodegraph

A lightweight, engine-agnostic node graph framework for Unity.  
Provides undo/redo command system, subgraph, JSON serialization, and a cross-engine view layer.

---

## Installation

### 开发阶段（本地路径）

在宿主 Unity 工程的 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.zgx197.nodegraph": "file:../../com.zgx197.nodegraph"
  }
}
```

### 正式版本（Git URL + tag）

```json
{
  "dependencies": {
    "com.zgx197.nodegraph": "https://github.com/zgx197/com.zgx197.nodegraph.git#v0.1.0"
  }
}
```

---

## Assemblies

消费者需在 `.asmdef` 的 `references` 中**显式声明**所需程序集（所有程序集均设 `autoReferenced: false`）：

| 程序集 | 内容 | `noEngineReferences` |
|--------|------|----------------------|
| `com.zgx197.nodegraph.math` | Vec2, Rect2, Color4, BezierMath | `true` |
| `com.zgx197.nodegraph.core` | Graph, Node, Port, Edge, GraphSettings | `true` |
| `com.zgx197.nodegraph.abstraction` | IDrawContext, IEditContext, IPlatformInput 等接口 | `true` |
| `com.zgx197.nodegraph.commands` | ICommand, CommandHistory, 13 内置命令 | `true` |
| `com.zgx197.nodegraph.layout` | ForceDirected, Layered, Tree 布局算法 | `true` |
| `com.zgx197.nodegraph.serialization` | JsonGraphSerializer（内部 DTO 不对外暴露） | `true` |
| `com.zgx197.nodegraph.view` | GraphViewModel, BlueprintProfile, NodeVisualTheme | `true` |
| `com.zgx197.nodegraph.editor` | UnityGraphRenderer, UnityPlatformInput, 持久化（仅 Editor 平台） | `false` |

---

## Quick Start

```csharp
// 1. 创建图
var graph = new Graph(GraphSettings.Default);

// 2. 注册节点类型
var catalog = new NodeTypeCatalog();
catalog.Register(new NodeTypeDefinition("SpawnTask")
{
    DisplayName  = "刷怪任务",
    Category     = "Spawn",
    DefaultPorts = new[]
    {
        new PortDefinition("激活", PortDirection.Input,  PortKind.Control, "exec"),
        new PortDefinition("完成", PortDirection.Output, PortKind.Control, "exec")
    }
});

// 3. 创建视图模型
var profile = new BlueprintProfile { NodeTypes = catalog };
var vm = new GraphViewModel(graph, profile);

// 4. 执行命令（自动进入 Undo 栈）
vm.Commands.Execute(new AddNodeCommand("SpawnTask", new Vec2(100, 100)));
vm.Commands.Undo();
```

---

## Requirements

- Unity 2021.3+
- .NET Standard 2.1

---

## License

MIT — see [LICENSE](LICENSE)
