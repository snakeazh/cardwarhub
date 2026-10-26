namespace ScrollView
{
    /// <summary>
    /// 不等高列表项需实现此接口，供 VariableHeightScrollView 测量行高（不依赖 LayoutRebuilder）。
    /// </summary>
    public interface IVariableHeightScrollItem
    {
        float GetPreferredHeight();
    }
}
