#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Abstraction;

namespace NodeGraph.View
{
    /// <summary>
    /// 快捷键管理器。注册、查询和触发快捷键动作。
    /// </summary>
    public class KeyBindingManager
    {
        private readonly Dictionary<string, KeyBinding> _bindings = new Dictionary<string, KeyBinding>();

        /// <summary>注册快捷键绑定</summary>
        public void Register(KeyBinding binding)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            _bindings[binding.ActionId] = binding;
        }

        /// <summary>设置指定动作的快捷键</summary>
        public void SetBinding(string actionId, KeyCombination key)
        {
            if (_bindings.TryGetValue(actionId, out var binding))
                binding.CurrentKey = key;
        }

        /// <summary>获取指定动作的当前快捷键</summary>
        public KeyCombination GetBinding(string actionId)
        {
            return _bindings.TryGetValue(actionId, out var binding)
                ? binding.CurrentKey
                : KeyCombination.None;
        }

        /// <summary>获取所有绑定</summary>
        public IEnumerable<KeyBinding> GetAllBindings() => _bindings.Values;

        /// <summary>检查当前帧是否触发了指定动作</summary>
        public bool IsActionTriggered(string actionId, IPlatformInput input)
        {
            if (!_bindings.TryGetValue(actionId, out var binding)) return false;

            var key = binding.CurrentKey;
            if (!key.IsValid) return false;

            // 修饰键必须完全匹配
            if (input.IsCtrlHeld != key.Ctrl) return false;
            if (input.IsShiftHeld != key.Shift) return false;
            if (input.IsAltHeld != key.Alt) return false;

            return input.IsKeyDown(key.Key);
        }

        /// <summary>重置所有快捷键为默认值</summary>
        public void ResetAll()
        {
            foreach (var binding in _bindings.Values)
                binding.ResetToDefault();
        }

        /// <summary>注册所有内置默认快捷键</summary>
        public void RegisterDefaults()
        {
            Register(new KeyBinding("delete", "删除选中", new KeyCombination("Delete")));
            Register(new KeyBinding("duplicate", "复制选中", new KeyCombination("D", ctrl: true)));
            Register(new KeyBinding("copy", "复制", new KeyCombination("C", ctrl: true)));
            Register(new KeyBinding("paste", "粘贴", new KeyCombination("V", ctrl: true)));
            Register(new KeyBinding("cut", "剪切", new KeyCombination("X", ctrl: true)));
            Register(new KeyBinding("undo", "撤销", new KeyCombination("Z", ctrl: true)));
            Register(new KeyBinding("redo", "重做", new KeyCombination("Y", ctrl: true)));
            Register(new KeyBinding("select_all", "全选", new KeyCombination("A", ctrl: true)));
            Register(new KeyBinding("focus_selected", "聚焦选中节点", new KeyCombination("F")));
            Register(new KeyBinding("focus_all", "聚焦全部节点", new KeyCombination("A")));
            Register(new KeyBinding("collapse", "折叠/展开", new KeyCombination("H")));
            Register(new KeyBinding("minimize", "最小化", new KeyCombination("H", shift: true)));
            Register(new KeyBinding("create_group", "创建分组", new KeyCombination("G", ctrl: true)));
            Register(new KeyBinding("search", "搜索", new KeyCombination("F", ctrl: true)));
            Register(new KeyBinding("add_node", "添加节点", new KeyCombination("Space")));
            Register(new KeyBinding("back", "返回上级", new KeyCombination("Backspace")));
        }
    }
}
