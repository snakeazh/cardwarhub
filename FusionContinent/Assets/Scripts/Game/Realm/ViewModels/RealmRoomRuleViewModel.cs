using System;
using UIFramework;

namespace Game.Realm
{
    /// <summary>
    /// 秘境规则弹窗 ViewModel。
    /// 纯静态展示弹窗，文案由调用方通过 Initialize 注入。
    /// </summary>
    public class RealmRoomRuleViewModel : ViewModel
    {
        #region 属性

        public BindableProperty<string> Title { get; } = new BindableProperty<string>("");
        public BindableProperty<string> Desc { get; } = new BindableProperty<string>("");

        #endregion

        #region 命令

        public Command OnCloseCommand { get; private set; }

        #endregion

        #region 事件

        public Action OnCloseRequested;

        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            OnCloseCommand = new Command(RaiseCloseRequested);
        }

        /// <summary>
        /// 注入规则文案（打开面板前调用）。
        /// </summary>
        public void Initialize(string title, string desc)
        {
            Title.Value = title;
            Desc.Value = desc;
        }

        private void RaiseCloseRequested()
        {
            if (OnCloseRequested != null)
            {
                OnCloseRequested.Invoke();
            }
        }
    }
}
