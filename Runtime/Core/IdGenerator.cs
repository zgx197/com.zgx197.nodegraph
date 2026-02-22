#nullable enable
using System;

namespace NodeGraph.Core
{
    /// <summary>
    /// 全局 ID 生成器。所有图元素使用完整 GUID 作为唯一标识符，确保多人协作零冲突。
    /// </summary>
    public static class IdGenerator
    {
        /// <summary>
        /// 生成新的唯一 ID（完整 GUID，格式: "3a7f2b1c-e4d8-4a5f-b9c2-1d3e5f7a8b0c"）
        /// </summary>
        public static string NewId() => Guid.NewGuid().ToString("D");
    }
}
