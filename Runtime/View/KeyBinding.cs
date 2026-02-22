#nullable enable
using System;

namespace NodeGraph.View
{
    /// <summary>
    /// 快捷键组合。
    /// </summary>
    public struct KeyCombination : IEquatable<KeyCombination>
    {
        /// <summary>主键名（"Delete", "D", "F", "Space" 等）</summary>
        public string Key { get; }

        public bool Ctrl { get; }
        public bool Shift { get; }
        public bool Alt { get; }

        public KeyCombination(string key, bool ctrl = false, bool shift = false, bool alt = false)
        {
            Key = key ?? "";
            Ctrl = ctrl;
            Shift = shift;
            Alt = alt;
        }

        /// <summary>空快捷键（未绑定）</summary>
        public static KeyCombination None => new KeyCombination("");

        public bool IsValid => !string.IsNullOrEmpty(Key);

        public bool Equals(KeyCombination other) =>
            Key == other.Key && Ctrl == other.Ctrl && Shift == other.Shift && Alt == other.Alt;

        public override bool Equals(object? obj) => obj is KeyCombination other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Key, Ctrl, Shift, Alt);

        public static bool operator ==(KeyCombination a, KeyCombination b) => a.Equals(b);
        public static bool operator !=(KeyCombination a, KeyCombination b) => !a.Equals(b);

        public override string ToString()
        {
            var parts = new System.Text.StringBuilder();
            if (Ctrl) parts.Append("Ctrl+");
            if (Shift) parts.Append("Shift+");
            if (Alt) parts.Append("Alt+");
            parts.Append(Key);
            return parts.ToString();
        }
    }

    /// <summary>
    /// 快捷键绑定项。
    /// </summary>
    public class KeyBinding
    {
        /// <summary>动作标识（如 "delete", "copy", "undo"）</summary>
        public string ActionId { get; }

        /// <summary>显示名称</summary>
        public string DisplayName { get; }

        /// <summary>默认快捷键</summary>
        public KeyCombination DefaultKey { get; }

        /// <summary>当前快捷键（用户可自定义修改）</summary>
        public KeyCombination CurrentKey { get; set; }

        public KeyBinding(string actionId, string displayName, KeyCombination defaultKey)
        {
            ActionId = actionId ?? throw new ArgumentNullException(nameof(actionId));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            DefaultKey = defaultKey;
            CurrentKey = defaultKey;
        }

        /// <summary>重置为默认快捷键</summary>
        public void ResetToDefault() => CurrentKey = DefaultKey;
    }
}
