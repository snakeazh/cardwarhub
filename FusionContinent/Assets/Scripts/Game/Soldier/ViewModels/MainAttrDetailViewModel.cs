using System.Collections.Generic;
using UIFramework;

namespace Game.Soldier
{
    public class MainAttrRowData
    {
        public string AttrNameText;
        public string AttrValueText;
    }

    public class MainAttrDetailViewModel : ViewModel
    {
        public List<MainAttrRowData> Rows { get; } = new List<MainAttrRowData>();

        public override void OnCreate()
        {
            base.OnCreate();
        }
    }
}
