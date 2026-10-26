using Config;
using FusionContinent;
using UIFramework;

namespace Game.Challenge
{
    /// <summary>
    /// 怪物详情小弹窗 ViewModel（UIFramework MVVM）
    /// 数据源：TbCardBaseConfig（按怪物 CardId 取）→ Name / Desc。
    /// 由外部调用 Setup(monsterId) 注入要展示的怪物。
    /// </summary>
    public class MonsterDetailPopPanelViewModel : ViewModel
    {
        #region 属性
        /// <summary>怪物名称</summary>
        public BindableProperty<string> MonsterName { get; } = new BindableProperty<string>("");
        /// <summary>怪物描述</summary>
        public BindableProperty<string> MonsterDetail { get; } = new BindableProperty<string>("");
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        #endregion

        #region 事件
        /// <summary>请求关闭弹窗</summary>
        public System.Action OnClosePanel;
        #endregion

        private TbCardBaseConfig _cardConfig;

        public override void OnCreate()
        {
            base.OnCreate();

            _cardConfig = DataConfigManager.Tables.TbCardBaseConfig;
            OnCloseCommand = new Command(OnClose);
        }

        /// <summary>
        /// 配置本弹窗要展示的怪物。从 CardBaseConfig 取名字与描述。
        /// </summary>
        public void Setup(int monsterId)
        {
            var cardCfg = _cardConfig.GetOrDefault(monsterId);
            if (cardCfg == null)
            {
                LogTool.LogError("[MonsterDetailPopPanelViewModel] 配置缺失，monsterId=" + monsterId);
                return;
            }

            MonsterName.Value = cardCfg.Name;
            MonsterDetail.Value = cardCfg.Desc;
        }

        private void OnClose()
        {
            OnClosePanel?.Invoke();
        }
    }
}
