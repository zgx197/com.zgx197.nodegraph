#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeGraph.Core
{
    /// <summary>
    /// 端口类型兼容性注册表。管理隐式类型转换规则，决定哪些端口可以互相连接。
    /// 内置规则：
    /// - 相同类型总是兼容
    /// - "any" 类型与任何类型兼容（通配符）
    /// - "exec" 类型只能连 "exec"（控制流不允许隐式转换）
    /// </summary>
    public class TypeCompatibilityRegistry
    {
        /// <summary>通配符类型</summary>
        public const string AnyType = "any";

        /// <summary>控制流类型</summary>
        public const string ExecType = "exec";

        // fromType -> 可转换到的类型集合
        private readonly Dictionary<string, HashSet<string>> _conversions = new Dictionary<string, HashSet<string>>();

        /// <summary>注册隐式转换：fromType 可以连到 toType</summary>
        public void RegisterImplicitConversion(string fromType, string toType)
        {
            if (fromType == null) throw new ArgumentNullException(nameof(fromType));
            if (toType == null) throw new ArgumentNullException(nameof(toType));

            if (!_conversions.TryGetValue(fromType, out var set))
            {
                set = new HashSet<string>();
                _conversions[fromType] = set;
            }
            set.Add(toType);
        }

        /// <summary>
        /// 空安全处理：仅将 null 转为 ""，<b>不将空串归一为 AnyType</b>。
        /// <para>通配符必须显式使用 <see cref="AnyType"/> 常量，空串表示未指定类型，不走通配。</para>
        /// </summary>
        public static string NormalizeDataType(string type)
            => type ?? "";

        /// <summary>查询两个类型是否兼容（源端口类型能否连到目标端口类型）</summary>
        public bool IsCompatible(string sourceType, string targetType)
        {
            if (sourceType == null || targetType == null) return false;

            // 相同类型总是兼容（含两端都是 ""、都是 "exec" 等情况）
            if (sourceType == targetType) return true;

            // exec 只能连 exec
            if (sourceType == ExecType || targetType == ExecType) return false;

            // 只有显式使用 AnyType 的端口才是通配符（空串不是通配）
            if (sourceType == AnyType || targetType == AnyType) return true;

            // 检查已注册的隐式转换
            if (_conversions.TryGetValue(sourceType, out var set) && set.Contains(targetType))
                return true;

            return false;
        }

        /// <summary>获取指定类型可连接的所有类型（含自身和 any）</summary>
        public IEnumerable<string> GetCompatibleTypes(string type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            // 自身总是兼容
            yield return type;

            // exec 不兼容其他类型
            if (type == ExecType) yield break;

            // any 与一切非-exec 类型兼容（只返回 any 本身，无法枚举全集）
            if (type != AnyType) yield return AnyType;

            // 已注册的隐式转换目标
            if (_conversions.TryGetValue(type, out var set))
            {
                foreach (var t in set) yield return t;
            }
        }
    }
}
