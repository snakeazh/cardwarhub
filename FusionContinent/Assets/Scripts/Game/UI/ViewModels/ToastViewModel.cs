using UIFramework;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Toast提示数据
    /// </summary>
    public class ToastData
    {
        public string Message;
        public float Duration;
        public ToastType Type;
    }

    /// <summary>
    /// Toast提示 ViewModel
    /// </summary>
    public class ToastViewModel : ViewModel
    {
        public override void OnCreate()
        {
            base.OnCreate();
            LogTool.Log("[ToastViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogTool.Log("[ToastViewModel] 销毁");
        }
    }
}
