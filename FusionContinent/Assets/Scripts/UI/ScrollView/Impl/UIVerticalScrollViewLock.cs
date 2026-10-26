namespace ScrollView
{
    public class UIVerticalScrollViewLock : UIVerticalScrollView
    {
        protected override int ComputeColumnsPerRow()
        {
            return this.ItemsPerLine;
        }
    }
}