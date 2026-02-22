#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace NodeGraph.Serialization
{
    /// <summary>
    /// 轻量级 JSON 序列化/反序列化工具。
    /// 零依赖，支持基本类型、List、嵌套对象。仅用于 NodeGraph 内部的图序列化。
    /// 不追求通用性，够用即可。
    /// </summary>
    public static class SimpleJson
    {
        // ══════════════════════════════════════
        //  序列化
        // ══════════════════════════════════════

        public static string Serialize(object? obj)
        {
            var sb = new StringBuilder();
            WriteValue(sb, obj, 0);
            return sb.ToString();
        }

        private static void WriteValue(StringBuilder sb, object? value, int indent)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            var type = value.GetType();

            if (value is string s)
            {
                WriteString(sb, s);
            }
            else if (value is bool b)
            {
                sb.Append(b ? "true" : "false");
            }
            else if (value is int i)
            {
                sb.Append(i);
            }
            else if (value is float f)
            {
                sb.Append(f.ToString("G9", CultureInfo.InvariantCulture));
            }
            else if (value is double d)
            {
                sb.Append(d.ToString("G17", CultureInfo.InvariantCulture));
            }
            else if (value is IList list)
            {
                WriteArray(sb, list, indent);
            }
            else if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
            {
                WriteObject(sb, value, indent);
            }
            else
            {
                sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append('"');
        }

        private static void WriteArray(StringBuilder sb, IList list, int indent)
        {
            sb.Append('[');
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                WriteValue(sb, list[i], indent + 1);
            }
            sb.Append(']');
        }

        private static void WriteObject(StringBuilder sb, object obj, int indent)
        {
            sb.Append('{');
            var fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            bool first = true;
            foreach (var field in fields)
            {
                var val = field.GetValue(obj);
                if (val == null && field.FieldType == typeof(string))
                {
                    // 跳过 null 的可选字段
                    continue;
                }

                if (!first) sb.Append(',');
                first = false;

                WriteString(sb, field.Name);
                sb.Append(':');
                WriteValue(sb, val, indent + 1);
            }
            sb.Append('}');
        }

        // ══════════════════════════════════════
        //  反序列化
        // ══════════════════════════════════════

        public static T? Deserialize<T>(string json) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            int index = 0;
            var result = ReadObject(json, ref index, typeof(T));
            return result as T;
        }

        private static object? ReadValue(string json, ref int index, Type targetType)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length) return null;

            char c = json[index];

            if (c == '"')
                return ReadString(json, ref index);
            if (c == '{')
                return ReadObject(json, ref index, targetType);
            if (c == '[')
                return ReadArray(json, ref index, targetType);
            if (c == 'n')
            {
                Expect(json, ref index, "null");
                return null;
            }
            if (c == 't')
            {
                Expect(json, ref index, "true");
                return true;
            }
            if (c == 'f')
            {
                Expect(json, ref index, "false");
                return false;
            }

            // 数字
            return ReadNumber(json, ref index, targetType);
        }

        private static string ReadString(string json, ref int index)
        {
            if (json[index] != '"')
                throw new FormatException($"期望 '\"' 在位置 {index}");

            index++; // 跳过开头引号
            var sb = new StringBuilder();

            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"') return sb.ToString();

                if (c == '\\' && index < json.Length)
                {
                    char esc = json[index++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '/': sb.Append('/'); break;
                        default: sb.Append(esc); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            throw new FormatException("未闭合的字符串");
        }

        private static object? ReadNumber(string json, ref int index, Type targetType)
        {
            int start = index;
            while (index < json.Length && "0123456789.eE+-".IndexOf(json[index]) >= 0)
                index++;

            var numStr = json.Substring(start, index - start);

            if (targetType == typeof(int))
            {
                if (int.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out int iv))
                    return iv;
                // 可能是 float 格式的整数
                if (float.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out float fiv))
                    return (int)fiv;
            }

            if (targetType == typeof(float))
            {
                if (float.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out float fv))
                    return fv;
            }

            if (targetType == typeof(double))
            {
                if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double dv))
                    return dv;
            }

            // 默认尝试 float
            if (float.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out float defF))
                return defF;

            return 0;
        }

        private static object? ReadObject(string json, ref int index, Type targetType)
        {
            if (json[index] != '{')
                throw new FormatException($"期望 '{{' 在位置 {index}");

            index++; // 跳过 {
            var obj = Activator.CreateInstance(targetType);
            var fields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var fieldMap = new Dictionary<string, FieldInfo>();
            foreach (var f in fields)
                fieldMap[f.Name] = f;

            SkipWhitespace(json, ref index);
            if (index < json.Length && json[index] == '}')
            {
                index++;
                return obj;
            }

            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                string key = ReadString(json, ref index);

                SkipWhitespace(json, ref index);
                if (json[index] != ':')
                    throw new FormatException($"期望 ':' 在位置 {index}");
                index++;

                if (fieldMap.TryGetValue(key, out var field))
                {
                    var val = ReadValue(json, ref index, field.FieldType);
                    if (val != null)
                    {
                        // 类型转换
                        if (field.FieldType == typeof(int) && val is float fToI)
                            val = (int)fToI;
                        else if (field.FieldType == typeof(float) && val is int iToF)
                            val = (float)iToF;
                        else if (field.FieldType == typeof(bool) && val is bool)
                        { /* 直接使用 */ }
                        else if (field.FieldType == typeof(string) && val is string)
                        { /* 直接使用 */ }

                        field.SetValue(obj, val);
                    }
                }
                else
                {
                    // 跳过未知字段值
                    SkipValue(json, ref index);
                }

                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                    continue;
                }
                if (index < json.Length && json[index] == '}')
                {
                    index++;
                    break;
                }
            }

            return obj;
        }

        private static object? ReadArray(string json, ref int index, Type targetType)
        {
            if (json[index] != '[')
                throw new FormatException($"期望 '[' 在位置 {index}");

            index++; // 跳过 [

            // 确定元素类型
            Type elemType = typeof(object);
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                elemType = targetType.GetGenericArguments()[0];

            var listType = typeof(List<>).MakeGenericType(elemType);
            var list = (IList)Activator.CreateInstance(listType)!;

            SkipWhitespace(json, ref index);
            if (index < json.Length && json[index] == ']')
            {
                index++;
                return list;
            }

            while (index < json.Length)
            {
                var val = ReadValue(json, ref index, elemType);

                // 类型转换
                if (val != null && elemType == typeof(string) && val is not string)
                    val = val.ToString();

                list.Add(val);

                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                    continue;
                }
                if (index < json.Length && json[index] == ']')
                {
                    index++;
                    break;
                }
            }

            return list;
        }

        private static void SkipValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length) return;

            char c = json[index];
            if (c == '"') { ReadString(json, ref index); return; }
            if (c == '{') { SkipBraced(json, ref index, '{', '}'); return; }
            if (c == '[') { SkipBraced(json, ref index, '[', ']'); return; }

            // 数字、true、false、null
            while (index < json.Length && ",]}".IndexOf(json[index]) < 0)
                index++;
        }

        private static void SkipBraced(string json, ref int index, char open, char close)
        {
            int depth = 0;
            bool inString = false;
            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"' && (index < 2 || json[index - 2] != '\\'))
                    inString = !inString;
                if (!inString)
                {
                    if (c == open) depth++;
                    if (c == close) { depth--; if (depth == 0) return; }
                }
            }
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
        }

        private static void Expect(string json, ref int index, string expected)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                if (index >= json.Length || json[index] != expected[i])
                    throw new FormatException($"期望 '{expected}' 在位置 {index}");
                index++;
            }
        }
    }
}
