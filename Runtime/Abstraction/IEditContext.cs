#nullable enable
using NodeGraph.Math;

namespace NodeGraph.Abstraction
{
    /// <summary>
    /// 编辑控件上下文接口。IMGUI 风格——传入当前值，返回修改后的值。
    /// 用于节点内嵌编辑（DrawEditor），各引擎实现此接口。
    /// </summary>
    public interface IEditContext
    {
        // ── 帧初始化 ──

        /// <summary>
        /// 开始一帧的编辑布局。设置可用绘制区域，重置内部布局游标。
        /// 必须在调用任何控件方法之前调用。
        /// 坐标为引擎窗口坐标系（由调用方负责从画布坐标转换）。
        /// </summary>
        void Begin(Rect2 availableRect);

        // ── 基础控件 ──

        /// <summary>浮点数输入字段</summary>
        float FloatField(string label, float value);

        /// <summary>整数输入字段</summary>
        int IntField(string label, int value);

        /// <summary>文本输入字段</summary>
        string TextField(string label, string value);

        /// <summary>布尔切换</summary>
        bool Toggle(string label, bool value);

        /// <summary>滑条</summary>
        float Slider(string label, float value, float min, float max);

        /// <summary>下拉选择</summary>
        int Popup(string label, int selectedIndex, string[] options);

        /// <summary>颜色选择</summary>
        Color4 ColorField(string label, Color4 value);

        // ── 布局辅助 ──

        /// <summary>只读标签</summary>
        void Label(string text);

        /// <summary>空白间距</summary>
        void Space(float pixels);

        /// <summary>开始水平布局</summary>
        void BeginHorizontal();

        /// <summary>结束水平布局</summary>
        void EndHorizontal();

        /// <summary>可折叠区域头部</summary>
        bool Foldout(string label, bool expanded);

        /// <summary>分隔线</summary>
        void Separator();

        // ── 状态查询 ──

        /// <summary>本帧中是否有任何控件值被修改</summary>
        bool HasChanged { get; }

        // ── 可用区域 ──

        /// <summary>当前可用绘制区域</summary>
        Rect2 AvailableRect { get; }
    }
}
