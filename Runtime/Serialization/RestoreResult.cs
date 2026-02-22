#nullable enable
using System.Collections.Generic;
using NodeGraph.Core;

namespace NodeGraph.Serialization
{
    /// <summary>
    /// [框架内置] <see cref="DefaultGraphPersister.RestoreWithDiagnostics"/> 的返回值。
    /// 包含成功还原的图对象，以及反序列化期间被静默跳过的元素诊断信息。
    /// </summary>
    internal sealed class RestoreResult
    {
        /// <summary>还原后的图对象。</summary>
        public Graph Graph { get; }

        /// <summary>因端口未找到而跳过的连线 ID 列表。</summary>
        public IReadOnlyList<string> SkippedEdgeIds { get; }

        /// <summary>所有警告信息（人类可读）。</summary>
        public IReadOnlyList<string> Warnings { get; }

        /// <summary>是否存在任何诊断警告。</summary>
        public bool HasWarnings => Warnings.Count > 0;

        public RestoreResult(
            Graph graph,
            IReadOnlyList<string> skippedEdgeIds,
            IReadOnlyList<string> warnings)
        {
            Graph          = graph;
            SkippedEdgeIds = skippedEdgeIds;
            Warnings       = warnings;
        }
    }
}
