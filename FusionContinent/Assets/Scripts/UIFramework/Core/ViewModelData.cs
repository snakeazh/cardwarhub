namespace UIFramework
{
    /// <summary>
    /// 包装ViewModel的数据，用于传递
    /// </summary>
    public class ViewModelData : IUIData
    {
        public ViewModel ViewModel { get; set; }
        
        public static ViewModelData Create<T>(T viewModel) where T : ViewModel
        {
            return new ViewModelData { ViewModel = viewModel };
        }
    }
}
