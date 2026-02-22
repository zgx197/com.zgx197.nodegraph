# NodeGraph Package 化规划

> **文档状态**：active  
> **版本**：v0.4  
> **日期**：2026-02-23  
> **目标读者**：框架维护者
>
> **包名**：`com.zgx197.nodegraph`  
> **GitHub 仓库**：https://github.com/zgx197/com.zgx197.nodegraph  
> **当前进度**：Phase 1 ✅ | Phase 2 ✅ | Phase 3 ✅ | Pre-P4 API 清理 ✅ | Phase 4 ✅

---

## 一、目标与意义

NodeGraph 将以 **Unity Package Manager（UPM）** 格式发布。UPM 使用与 NPM 兼容的注册表协议，可托管到：

- `npmjs.com` 公共注册表（开源）
- 私有 NPM 兼容注册表（如 Verdaccio、GitHub Packages）
- Git 仓库 URL（开发阶段最轻量）

Package 化带来的额外价值：

1. **显式区分扩展点与内部实现** — `internal` 可见性在编译期拦截错误依赖，外部包无法访问。
2. **版本化管理** — 业务项目通过 `package.json` 锁定版本，升级时有明确的 API 兼容承诺。
3. **跨项目复用** — `SceneBlueprint` 作为消费者包，其他项目也能独立接入 NodeGraph。
4. **程序集隔离** — `autoReferenced: false` 配合显式依赖，防止业务层意外访问序列化内部层。

> 路径：注释标注（✅ 已完成）→ `internal` 化（✅ 已完成）→ `autoReferenced: false`（✅ 已完成）→ 整理包结构（🚧 进行中）→ 发布。

---

## 二、当前程序集结构

NodeGraph 已经是多程序集结构，各程序集均设置 `noEngineReferences: true`（Unity 程序集除外）。

```
NodeGraph.Math          ← 纯数学工具（Vec2/Rect2/Color4 等），零依赖
    ↑
NodeGraph.Core          ← 核心数据模型（Graph/Node/Port/Edge）
NodeGraph.Abstraction   ← 渲染/输入/内容渲染抽象接口（IDrawContext 等）
    ↑
NodeGraph.Commands      ← 命令系统（ICommand + 内置命令）
NodeGraph.Serialization ← 序列化（GraphDto / DefaultGraphPersister）
NodeGraph.Layout        ← 布局算法
    ↑
NodeGraph.View          ← 视图模型（GraphViewModel / BlueprintProfile）
    ↑
NodeGraph.Unity         ← Unity Editor 适配层（仅 Editor 平台）
```

依赖关系无环，已具备拆包的物理条件。

---

## 三、API 边界分析

根据本轮注释工作的标注结果，将所有公共类型分为三类：

### 3.1 扩展点（业务层实现/配置，保持 `public`）

| 类型 | 程序集 | 说明 |
|------|--------|------|
| `INodeData` | Core | 节点业务数据标记接口 |
| `IEdgeData` | Core | 连线业务数据标记接口 |
| `IDescribableNode` | Core | 节点描述文本可选接口 |
| `IConnectionPolicy` | Core | 连接规则策略 |
| `IConnectionValidator` | Core | 责任链中的单个校验节点 |
| `INodeTypeCatalog` | Core | 节点类型目录（搜索菜单数据源） |
| `ICommand` | Commands | 可撤销命令接口 |
| `IStructuralCommand` | Commands | 标记：结构性命令 |
| `IStyleCommand` | Commands | 标记：纯视觉命令 |
| `NodeTypeDefinition` | Core | 节点类型元数据描述 |
| `PortDefinition` | Core | 端口定义模板 |
| `GraphSettings` | Core | 图的拓扑策略配置 |
| `BlueprintProfile` | View | 蓝图类型配置包 |
| `NodeVisualTheme` | View | 视觉主题配置 |
| `INodeContentRenderer` | Abstraction | 节点内容渲染（业务层按 TypeId 注册） |
| `IEdgeLabelRenderer` | Abstraction | 连线标签渲染（可选） |
| `IGraphPersister` | Serialization | 持久化接口（已改 internal，不再作为扩展点）|
| `IUserDataSerializer` | Abstraction | 业务数据 JSON 序列化接口 |
| `IDrawContext` | Abstraction | 引擎绘制接口（新引擎适配时实现） |
| `IEditContext` | Abstraction | IMGUI 风格编辑控件接口 |
| `IPlatformInput` | Abstraction | 平台输入抽象接口 |
| `IGraphValidator` | Abstraction | 图验证接口 |

### 3.2 框架核心模型（业务层只读，保持 `public`，内部变更方法考虑隐藏）

| 类型 | 程序集 | 说明 |
|------|--------|------|
| `Graph` | Core | 图的核心数据容器 |
| `Node` | Core | 节点实例 |
| `Port` | Core | 端口实例 |
| `Edge` | Core | 连线实例 |
| `ConnectResult` | Core | 连接操作返回值 |
| `CommandHistory` | Commands | 命令历史管理器（业务层唯一入口） |
| `GraphViewModel` | View | 视图模型（业务层主入口） |

### 3.3 框架内部实现（当前 `public`，Package 化后改 `internal`）

| 类型 | 程序集 | 当前状态 | 目标 |
|------|--------|----------|------|
| `DefaultGraphPersister` | Serialization | ~~public~~ | ✅ internal |
| `GraphDto` 及所有子 DTO | Serialization | ~~public~~ | ✅ internal |
| `RestoreResult` | Serialization | ~~public~~ | ✅ internal |
| `IGraphPersister` | Serialization | ~~public~~ | ✅ internal |
| `INodeTypeProvider` | Core | ~~public~~ | ✅ **已删除**（合并入 `INodeTypeCatalog`）|
| `CompoundCommand` | Commands | ~~public~~ | ✅ internal（通过 BeginCompound 使用）|
| `NodeTypeRegistry` | Core | ~~public~~ | ✅ internal |
| `SubGraphInstantiator` | Core | public | **保持 public**（消费者合法 API，`BlueprintTemplateUtils` 需要 `NodeIdMap`）|
| `Graph.AddSubGraphFrameDirect` | Core | ~~public~~ | ✅ internal |
| `SubGraphIndex.Add` | Core | ~~public~~ | ✅ internal |

---

## 四、可见性变更路线

### Phase 1 — 注释标注（**已完成**）

通过 XML 注释明确标注每个类型的分类（`[扩展点]` / `[框架核心模型]` / `[框架内部]`），建立"纸面边界"。

**验收标准**：所有公开类型均有分类标记，无歧义接口。

---

### Phase 2 — 内部化高风险类型（✅ **已完成**）

**目标**：将最容易被误用的内部实现改为 `internal`，防止业务层直接依赖。

**P2-A：Serialization 层内部化** ✅
```
GraphDto / GraphSettingsDto / NodeDto / PortDto
EdgeDto / GroupDto / CommentDto / SubGraphFrameDto
Vec2Dto / Color4Dto → internal ✅
IGraphPersister → internal ✅
DefaultGraphPersister → internal ✅
RestoreResult → internal ✅
JsonGraphSerializer：移除 IGraphPersister? 参数，INodeTypeProvider? → INodeTypeCatalog? ✅
```

**P2-B：Commands 层清理** ✅
```
CompoundCommand → internal ✅（业务层通过 BeginCompound 使用）
ActionNodeTypeAdapter.RegisterAll → 删除（死代码）✅
```

**P2-C：Core 层清理** ✅
```
NodeTypeRegistry → internal ✅（业务层通过 INodeTypeCatalog 使用）
INodeTypeProvider → 已删除 ✅（GetNodeType 合并入 INodeTypeCatalog）
INodeTypeCatalog 断开 : INodeTypeProvider 继承 ✅（避免 CS0060）
Graph.AddSubGraphFrameDirect → internal ✅
Graph.AddNodeDirect / AddEdgeDirect / AddGroupDirect / AddCommentDirect → 均已是 internal ✅
SubGraphIndex.Add → internal ✅
GraphSettings.CreateEmptyNodeTypeCatalog() 工厂方法 → 新增（跨程序集创建空目录）✅
```

**友元声明（InternalsVisibleTo）**
- `NodeGraph.Core/AssemblyInfo.cs`：已声明 Commands / Serialization / Unity / Editor.Tests 为友元 ✅
- `NodeGraph.Serialization/AssemblyInfo.cs`：已声明 NodeGraph.Unity 为友元（GraphAssetConverter）✅

**验收结果**：`SceneBlueprint` 编译通过，无 `internal` 访问错误 ✅

---

### Phase 3 — 程序集分层强化（✅ **已完成**）

所有 NodeGraph 程序集已关闭 `autoReferenced`，消费层已验证显式引用完整。

**关闭 autoReferenced 的程序集：**
```
NodeGraph.Math        → autoReferenced: false ✅
NodeGraph.Core        → autoReferenced: false ✅
NodeGraph.Abstraction → autoReferenced: false ✅
NodeGraph.Commands    → autoReferenced: false ✅
NodeGraph.Layout      → autoReferenced: false ✅
NodeGraph.Serialization → autoReferenced: false ✅
NodeGraph.View        → autoReferenced: false ✅
NodeGraph.Unity       → autoReferenced: false ✅
NodeGraph.Editor.Tests → autoReferenced: false ✅
```

**SceneBlueprint 消费层显式引用验证：**
- `SceneBlueprint.Editor`：已有完整 NodeGraph 引用列表 ✅
- `SceneBlueprint.Core`：引用 NodeGraph.Math / NodeGraph.Core ✅
- `SceneBlueprint.Actions`：引用 NodeGraph.Math / NodeGraph.Core ✅
- `SceneBlueprint.Tests`：引用 NodeGraph.Math / NodeGraph.Core ✅
- `SceneBlueprint.Domain / Runtime / Application / Adapters`：无 NodeGraph 直接依赖 ✅

---

### Phase 4 — UPM 打包与发布（🚧 **进行中**）

**已完成：**
- GitHub 仓库已创建：https://github.com/zgx197/com.zgx197.nodegraph ✅
- 包名确定：`com.zgx197.nodegraph` ✅
- 仓库设置：Public，MIT 许可证，Unity .gitignore ✅

**待完成：**
- [x] 将源码迁移到新仓库（目录结构重组：Runtime/ + Editor/ + Tests/Editor/）✅
- [x] 创建 `package.json` ✅
- [x] 创建 `CHANGELOG.md` ✅
- [x] 创建 `.npmignore`（排除 Documents/ 等）✅
- [x] asmdef 名称规范化（`NodeGraph.Core` → `com.zgx197.nodegraph.core` 等）✅
- [ ] 打 `v0.1.0` tag，更新消费者 manifest.json

#### 4.1 目录结构

UPM 要求包根目录包含 `package.json`，Runtime/Editor 代码分别放在对应子目录。

```
com.zgx197.nodegraph/                ← 包根目录（Git 仓库根）
├── package.json                     ← UPM 包描述（必须）✅
├── CHANGELOG.md                     ← 版本变更记录 ✅
├── .npmignore                       ← 排除 Documents/ 等 ✅
├── README.md
├── LICENSE
├── Runtime/                         ← 无引擎依赖的程序集（noEngineReferences: true）✅
│   ├── Math/
│   │   └── com.zgx197.nodegraph.math.asmdef
│   ├── Core/
│   │   └── com.zgx197.nodegraph.core.asmdef
│   ├── Abstraction/
│   │   └── com.zgx197.nodegraph.abstraction.asmdef
│   ├── Commands/
│   │   └── com.zgx197.nodegraph.commands.asmdef
│   ├── Layout/
│   │   └── com.zgx197.nodegraph.layout.asmdef
│   ├── Serialization/
│   │   └── com.zgx197.nodegraph.serialization.asmdef
│   └── View/
│       └── com.zgx197.nodegraph.view.asmdef
├── Editor/                          ← Unity Editor 专属程序集 ✅
│   └── com.zgx197.nodegraph.editor.asmdef
└── Tests/                           ← 测试程序集（UPM 标准位置）✅
    └── Editor/
        └── com.zgx197.nodegraph.tests.editor.asmdef
```

> **当前状态**：目录结构已按 UPM 规范重组完成，asmdef 名称已规范化，package.json / CHANGELOG.md / .npmignore 已创建。待打 `v0.1.0` tag 并在消费者工程验证 Git URL 安装。

#### 4.2 package.json（UPM 格式）

UPM 的 `package.json` 与普通 NPM 格式兼容，但有几个 Unity 专用字段：

```json
{
  "name": "com.zgx197.nodegraph",
  "version": "0.1.0",
  "displayName": "NodeGraph",
  "description": "A lightweight, engine-agnostic node graph framework for Unity. Provides undo/redo command system, subgraph, serialization and cross-engine view layer.",
  "unity": "2021.3",
  "documentationUrl": "https://github.com/zgx197/com.zgx197.nodegraph#readme",
  "changelogUrl": "https://github.com/zgx197/com.zgx197.nodegraph/blob/main/CHANGELOG.md",
  "licensesUrl": "https://github.com/zgx197/com.zgx197.nodegraph/blob/main/LICENSE.md",
  "dependencies": {},
  "keywords": [
    "node-graph",
    "blueprint",
    "command-pattern",
    "editor-tool"
  ],
  "author": {
    "name": "ZhangGuoxin",
    "url": "https://github.com/zgx197"
  }
}
```

**关键字段说明**：

| 字段 | 说明 |
|------|------|
| `name` | 必须是反向域名格式（`com.xxx.yyy`），与 npmjs 包名一致 |
| `version` | 遵循 semver：`MAJOR.MINOR.PATCH` |
| `unity` | 最低支持的 Unity 版本（`2021.3` 指 LTS）|
| `unityRelease` | 可选，精确到小版本 |
| `dependencies` | 依赖其他 UPM 包时填写，格式与 npm 一致 |

#### 4.3 程序集名称规范

UPM 包发布后，消费者项目通过 GUID 或名称引用程序集。建议在 asmdef 中加入包前缀，防止命名冲突：

```
当前名称               → 发布后建议名称
NodeGraph.Core        → com.zgx197.nodegraph.core
NodeGraph.Commands    → com.zgx197.nodegraph.commands
NodeGraph.View        → com.zgx197.nodegraph.view
NodeGraph.Unity       → com.zgx197.nodegraph.editor
```

> ⚠️ asmdef 重命名是破坏性变更，需一次性完成并同步更新所有消费者引用。建议在首次发布（v0.1.0）时完成。

同时所有 asmdef 设置 `"autoReferenced": false`，消费者按需显式引用。

#### 4.4 发布方式

**方式 A：Git URL（开发阶段推荐）**

消费者在 `Packages/manifest.json` 中添加：
```json
{
  "dependencies": {
    "com.zgx197.nodegraph": "https://github.com/zgx197/com.zgx197.nodegraph.git#v0.1.0"
  }
}
```
优点：零配置，直接用 tag 锁版本。缺点：不支持语义化版本范围查询。

**方式 B：发布到 npmjs.com（开源公开）**

```bash
# 登录 npm
npm login

# 在包根目录执行
npm publish --access public
```

消费者在 `Packages/manifest.json` 和 `.upmconfig.toml` 中配置：
```toml
# %USERPROFILE%\.upmconfig.toml（Windows）或 ~/.upmconfig.toml（macOS/Linux）
[npmAuth."https://registry.npmjs.org"]
  token = "your-npm-token"
```

**方式 C：私有注册表（团队内部）**

使用 Verdaccio 或 GitHub Packages 搭建私有注册表：
```bash
npm publish --registry https://your-private-registry.com
```

消费者 `.upmconfig.toml`：
```toml
[npmAuth."https://your-private-registry.com"]
  token = "your-token"
```

`Packages/manifest.json`：
```json
{
  "scopedRegistries": [
    {
      "name": "Studio Private",
      "url": "https://your-private-registry.com",
      "scopes": ["com.studio"]
    }
  ],
  "dependencies": {
    "com.studio.nodegraph": "1.0.0"
  }
}
```

#### 4.5 消费者包（SceneBlueprint）依赖声明

`SceneBlueprint` 作为独立包，其 `package.json` 声明对 NodeGraph 的依赖：

```json
{
  "name": "com.zgx197.sceneblueprint",
  "version": "0.1.0",
  "dependencies": {
    "com.zgx197.nodegraph": "0.1.0"
  }
}
```

`SceneBlueprint` 的 asmdef 只引用需要的 NodeGraph 程序集，不引用 `NodeGraph.Serialization`（内部化后不可见）：
```json
{
  "references": [
    "com.zgx197.nodegraph.core",
    "com.zgx197.nodegraph.commands",
    "com.zgx197.nodegraph.view",
    "com.zgx197.nodegraph.abstraction"
  ]
}
```

---

## 五、业务层正确接入方式（约束规范）

### ✅ 正确：通过扩展点接入

```csharp
// 1. 定义节点数据（实现 INodeData）
public class SpawnTaskData : INodeData
{
    public string TemplateName;
    public int WaveCount;
}

// 2. 定义节点类型（填充 NodeTypeDefinition）
NodeTypeDefinition.Register("SpawnTask", new NodeTypeDefinition
{
    TypeId      = "SpawnTask",
    DisplayName = "刷怪任务",
    Category    = "Spawn",
    DefaultPorts = new[]
    {
        new PortDefinition("激活", PortDirection.Input, PortKind.Control, "exec"),
        new PortDefinition("完成", PortDirection.Output, PortKind.Control, "exec")
    }
});

// 3. 通过命令修改数据（绝不直接赋值后跳过命令系统）
viewModel.Commands.Execute(new ChangeNodeDataCommand(nodeId, newData));

// 4. 通过 BlueprintProfile 组装配置
var profile = new BlueprintProfile
{
    NodeTypes        = myNodeTypeCatalog,
    ConnectionPolicy = new MyConnectionPolicy(),
    ContentRenderers = { ["SpawnTask"] = new SpawnTaskContentRenderer() }
};
```

### ❌ 禁止：直接操作框架内部

```csharp
// ❌ 直接构造 Node（应通过 Graph.AddNode 或 AddNodeCommand）
var node = new Node(id, typeId, position);
graph.AddNodeDirect(node); // AddNodeDirect 是框架内部方法

// ❌ 直接修改 Node.UserData（应通过 ChangeNodeDataCommand）
node.UserData = new SpawnTaskData();  // 绕过命令系统，无法 Undo

// ❌ 直接操作 GraphDto（序列化层内部数据）
var dto = new GraphDto();  // Phase 2 完成后将编译报错

// ❌ 直接调用 ICommand.Execute（应通过 CommandHistory.Execute）
myCommand.Execute(graph);  // 绕过 Undo 栈
```

---

## 六、当前已知问题与技术债

| 编号 | 问题 | 影响 | 建议处理时机 |
|------|------|------|-------------|
| D1 | `Graph.AddNodeDirect` 等 Direct 方法为 `public`，外部可绕过事件系统 | 中 | ✅ Phase 2-C 已修复（全部改 internal）|
| D2 | `INodeTypeProvider` 作为 `IGraphPersister` 参数暴露，实际只供内部序列化使用 | 低 | ✅ Phase 2-A/C 已修复（INodeTypeProvider 已删除，合并入 INodeTypeCatalog）|
| D3 | `CompoundCommand` 可被外部直接构造和执行，绕过 `BeginCompound` 的嵌套机制 | 低 | ✅ Phase 2-B 已修复（CompoundCommand 改 internal）|
| D4 | `RemoveNodeCommand` / `UngroupSubGraphCommand` 内部有重复的快照类（NodeSnapshot / EdgeSnapshot）| 低 | 重构时提取为共享内部类 |
| D5 | `DefaultGraphPersister.Restore` 与 `RestoreWithDiagnostics` 实现重复 | 低 | 合并为一个实现，Restore 调用后者 |

---

## 七、阶段验收检查项

```
Phase 1（注释标注）
[x] 所有公开类型有分类标记
[x] Core 接口：INodeData/IEdgeData/IConnectionPolicy/IConnectionValidator/INodeTypeCatalog/INodeTypeProvider
[x] Command 接口：ICommand/IStructuralCommand/IStyleCommand
[x] 数据模型：Graph/Node/Port/Edge/NodeTypeDefinition/GraphSettings
[x] 命令系统：CommandHistory + 全部内置命令（21个文件）
[x] 视图层：GraphViewModel/BlueprintProfile/NodeVisualTheme
[x] 序列化层：DefaultGraphPersister/GraphDto/RestoreResult

Phase 2（内部化）
[x] Serialization 层 DTO 改 internal（GraphDto/NodeDto/PortDto/EdgeDto/GroupDto/CommentDto/SubGraphFrameDto/Vec2Dto/Color4Dto）
[x] IGraphPersister 改 internal
[x] DefaultGraphPersister 改 internal
[x] RestoreResult 改 internal
[x] INodeTypeProvider 删除（合并入 INodeTypeCatalog）
[x] NodeTypeRegistry 改 internal
[x] CompoundCommand 改 internal
[x] Graph.AddSubGraphFrameDirect 改 internal
[x] SubGraphIndex.Add 改 internal
[x] SceneBlueprint 编译通过验证（含 InternalsVisibleTo 友元声明）

Phase 3（程序集显式引用）
[x] 所有 9 个 NodeGraph asmdef 关闭 autoReferenced
[x] SceneBlueprint 消费层显式引用验证通过

Phase 4（UPM 打包）
[x] GitHub 仓库创建（https://github.com/zgx197/com.zgx197.nodegraph）
[x] 目录重组（Runtime/ + Editor/ + Tests/Editor/）
[x] 创建 package.json（com.zgx197.nodegraph，v0.1.0）
[x] 创建 CHANGELOG.md
[x] 创建 .npmignore（排除 Documents/）
[x] asmdef 名称规范化（NodeGraph.Core → com.zgx197.nodegraph.core 等）
[x] 打 v0.1.0 tag，在消费者工程验证 Git URL 安装
```
