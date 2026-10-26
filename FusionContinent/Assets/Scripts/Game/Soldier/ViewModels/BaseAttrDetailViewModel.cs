using System.Collections.Generic;
using UIFramework;

namespace Game.Soldier
{
    public class BaseAttrRowData
    {
        public string AttrName;
        public string AttrValue;
    }

    public class BaseAttrDetailViewModel : ViewModel
    {
        public List<BaseAttrRowData> Rows { get; } = new List<BaseAttrRowData>();

        public override void OnCreate()
        {
            base.OnCreate();
        }
    }
}
