namespace ScrollView
{
    public class UIHorizontalScrollViewLock : UIHorizontalScrollView
    {
        protected override int ComputeRowsPerColumn()
        {
            return this.ItemsPerLine;
        }
    }
}