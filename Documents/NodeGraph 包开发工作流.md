# NodeGraph 包开发工作流

> **文档状态**：active  
> **版本**：v0.1  
> **日期**：2026-02-23  
> **目标读者**：NodeGraph 包维护者

---

## 一、为什么无法用 Unity Hub 直接打开包仓库

`com.zgx197.nodegraph` 是一个 **UPM 包仓库**，不是 Unity 项目。Unity Hub 打开的是 Unity 项目（需要 `Assets/`、`ProjectSettings/`、`Packages/manifest.json` 等目录结构），包仓库中没有这些，因此无法直接打开。

**包永远不能独立运行**，它只能被安装到一个 Unity 项目中使用。

---

## 二、开发环境搭建：本地路径引用

### 原理

在宿主 Unity 项目（消费者工程）的 `Packages/manifest.json` 中，使用 `file:` 协议引用本地磁盘上的包目录：

```json
{
  "dependencies": {
    "com.zgx197.nodegraph": "file:../../com.zgx197.nodegraph"
  }
}
```

> 路径是相对于宿主项目 `Packages/` 目录的相对路径，根据实际磁盘布局调整。

Unity 会直接读取并编译该目录下的代码。**修改包源码后 Unity 自动热重载，无需任何额外操作**。

### 与 SceneBlueprint 工程联调

目录布局示例：

```
d:\work\
├── com.zgx197.nodegraph\      ← 包仓库（独立 git）
└── UnityProjectXxx\           ← 宿主 Unity 工程（SceneBlueprint 所在工程）
    └── Packages\
        └── manifest.json
```

`manifest.json` 配置：

```json
{
  "dependencies": {
    "com.zgx197.nodegraph": "file:../../com.zgx197.nodegraph"
  }
}
```

安装后 Package Manager 会显示 NodeGraph 为本地包，可以在 Unity 中直接编辑和调试。

---

## 三、完整开发 → 测试 → 提交流程

```
1. 编辑代码
   └─ 在 IDE（Rider / VS Code）中直接编辑 d:\work\com.zgx197.nodegraph\ 下的源码

2. Unity 自动热重载
   └─ 保存文件后，宿主工程 Unity 编辑器自动重新编译，编译错误即时可见

3. 运行测试
   └─ Unity 菜单：Window → General → Test Runner
   └─ 选择 EditMode，运行 com.zgx197.nodegraph.tests.editor 中的测试用例

4. 场景验证（可选）
   └─ 在宿主工程中打开 SceneBlueprint 相关场景，验证功能表现

5. 提交到包仓库
   └─ 在 d:\work\com.zgx197.nodegraph 目录下执行 git commit / git push
   └─ 打版本 tag：git tag v0.x.x && git push origin v0.x.x

6. 消费者工程切换到正式版本
   └─ 将 manifest.json 中的 file: 路径改回 Git URL + tag：
      "com.zgx197.nodegraph": "https://github.com/zgx197/com.zgx197.nodegraph.git#v0.x.x"
```

---

## 四、两个仓库的 git 独立性

包仓库和消费者工程是**两个完全独立的 git 仓库**，互不干扰：

| | 包仓库 | 消费者工程 |
|--|--------|-----------|
| 路径 | `d:\work\com.zgx197.nodegraph` | `d:\work\UnityProjectXxx` |
| 内容 | NodeGraph 源码 | SceneBlueprint + 业务代码 |
| 版本引用 | — | 通过 manifest.json 引用包版本 |

开发期间用 `file:` 路径联调，发布后消费者锁定到具体 tag，两者解耦。

---

## 五、注意事项

- **不要**把包仓库放进宿主工程的 `Assets/` 或 `Packages/` 子目录，否则会变成嵌入包（Embedded Package），失去独立仓库的版本管理优势。
- `file:` 路径引用时，包内 `.meta` 文件中的 GUID 会被直接使用，切换回 Git URL 安装后 GUID 保持一致，不会出现引用丢失。
- 包中的 `Tests/Editor/` 程序集（`com.zgx197.nodegraph.tests.editor`）在 Test Runner 中的 **EditMode** 选项卡下可见和运行。
