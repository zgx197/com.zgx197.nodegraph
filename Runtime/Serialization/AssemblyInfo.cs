using System.Runtime.CompilerServices;

// NodeGraph.Unity 是 Unity 适配层，需要直接操作 Serialization 内部 DTO（GraphAssetConverter）
[assembly: InternalsVisibleTo("com.zgx197.nodegraph.editor")]
