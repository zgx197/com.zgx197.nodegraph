# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-02-23

### Added
- Initial UPM package release
- `com.zgx197.nodegraph.math` — Pure math utilities (Vec2, Rect2, Color4, BezierMath), zero dependencies
- `com.zgx197.nodegraph.core` — Core data model (Graph, Node, Port, Edge, GraphSettings)
- `com.zgx197.nodegraph.abstraction` — Rendering/input/content abstraction interfaces (IDrawContext, IEditContext, IPlatformInput, INodeContentRenderer, etc.)
- `com.zgx197.nodegraph.commands` — Undo/redo command system (ICommand, CommandHistory, 13 built-in commands)
- `com.zgx197.nodegraph.layout` — Layout algorithms (ForceDirected, Layered, Tree)
- `com.zgx197.nodegraph.serialization` — JSON serialization (internal DTO layer, JsonGraphSerializer)
- `com.zgx197.nodegraph.view` — View model (GraphViewModel, BlueprintProfile, NodeVisualTheme)
- `com.zgx197.nodegraph.editor` — Unity Editor adapter (UnityGraphRenderer, UnityPlatformInput, persistence)

### Architecture
- All assemblies set `autoReferenced: false` — consumers must reference explicitly
- Internal implementation hidden behind `internal` visibility (DTO layer, DefaultGraphPersister, NodeTypeRegistry, CompoundCommand)
- Serialization layer sealed from consumers; access via `IUserDataSerializer` extension point only
