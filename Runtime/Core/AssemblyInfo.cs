using System.Runtime.CompilerServices;

// 允许 Commands 程序集访问 Core 的 internal 成员（用于命令的 Undo 恢复操作）
[assembly: InternalsVisibleTo("com.zgx197.nodegraph.commands")]

// 允许 Serialization 程序集访问 Core 的 internal 成员（用于反序列化直接构造）
[assembly: InternalsVisibleTo("com.zgx197.nodegraph.serialization")]

// 允许 Unity 适配层访问 Core 的 internal 成员（用于持久化转换）
[assembly: InternalsVisibleTo("com.zgx197.nodegraph.editor")]

// 允许 CombatBlueprint 业务层访问 Core 的 internal 成员（用于图构建）
[assembly: InternalsVisibleTo("CombatBlueprint")]

// 允许测试程序集访问 Core 的 internal 成员
[assembly: InternalsVisibleTo("com.zgx197.nodegraph.tests")]
[assembly: InternalsVisibleTo("com.zgx197.nodegraph.tests.editor")]
