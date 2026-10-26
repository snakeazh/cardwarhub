using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FusionContinent;
using UIFramework;

namespace Game.Player
{
    /// <summary>
    /// 升星面板 ViewModel（通用版，供武器/皮肤形象共用）。
    /// 数据源由 IStarItemDataProvider 提供。
    /// </summary>
    public class StarUpGradePanelViewModel : ViewModel
    {
        public BindableProperty<int> ConfigId { get; } = new BindableProperty<int>(0);
        public BindableProperty<string> ItemName { get; } = new BindableProperty<string>(string.Empty);
        public BindableProperty<string> ItemIcon { get; } = new BindableProperty<string>(string.Empty);

        public BindableProperty<int> CurStarCount { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> NextStarCount { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> MaxStarCount { get; } = new BindableProperty<int>(0);
        public BindableProperty<string> CurBonusText { get; } = new BindableProperty<string>(string.Empty);
        public BindableProperty<string> NextBonusText { get; } = new BindableProperty<string>(string.Empty);
        public BindableProperty<string> CurSkillDesc { get; } = new BindableProperty<string>(string.Empty);
        public BindableProperty<string> NextSkillDesc { get; } = new BindableProperty<string>(string.Empty);

        public BindableProperty<bool> ShowNextDetail { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> ShowConsume { get; } = new BindableProperty<bool>(false);
        public BindableProperty<int> ConsumeItemId { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> ConsumeCount { get; } = new BindableProperty<int>(0);
        public BindableProperty<bool> LevelUpBtnInteractable { get; } = new BindableProperty<bool>(false);

        private IStarItemDataProvider mProvider;
        private System.Action<int> mDataChangedHandler;

        public IStarItemDataProvider Provider => mProvider;

        public override void OnCreate()
        {
            base.OnCreate();
            mDataChangedHandler = OnDataChanged;
        }

        public override void OnDestroy()
        {
            if (mProvider != null && mDataChangedHandler != null)
            {
                mProvider.OnDataChanged -= mDataChangedHandler;
            }

            base.OnDestroy();
        }

        /// <summary>注入数据提供者、设置配置 Id 并刷新。</summary>
        public void SetData(IStarItemDataProvider provider, int configId)
        {
            // 切换 provider 时解绑旧的
            if (mProvider != null && mDataChangedHandler != null && mProvider != provider)
            {
                mProvider.OnDataChanged -= mDataChangedHandler;
            }

            mProvider = provider;
            if (mProvider != null && mDataChangedHandler != null)
            {
                mProvider.OnDataChanged += mDataChangedHandler;
            }

            ConfigId.Value = configId;
            RefreshAll();
        }

        public void RefreshAll()
        {
            var configId = ConfigId.Value;
            if (mProvider == null || !mProvider.IsActivated(configId))
            {
                ResetDisplay();
                return;
            }

            var curLevel = mProvider.GetLevel(configId);
            var maxStarCount = mProvider.GetMaxLevel(configId);
            var canUpgrade = mProvider.CanUpgrade(configId);
            var nextLevel = canUpgrade ? curLevel + 1 : curLevel;
            var curLevelCfg = mProvider.GetLevelInfo(configId, curLevel);
            var nextLevelCfg = mProvider.GetLevelInfo(configId, nextLevel);

            ItemName.Value = mProvider.GetName(configId);
            ItemIcon.Value = mProvider.GetIcon(configId);
            MaxStarCount.Value = maxStarCount;
            CurStarCount.Value = curLevel;
            NextStarCount.Value = nextLevel;
            CurBonusText.Value = curLevelCfg != null ? curLevelCfg.Desc1 : string.Empty;
            NextBonusText.Value = nextLevelCfg != null ? nextLevelCfg.Desc1 : string.Empty;
            CurSkillDesc.Value = curLevelCfg != null ? curLevelCfg.Desc2 : string.Empty;
            NextSkillDesc.Value = nextLevelCfg != null ? nextLevelCfg.Desc2 : string.Empty;
            ShowNextDetail.Value = canUpgrade;
            LevelUpBtnInteractable.Value = canUpgrade;

            var consume = mProvider.GetUpgradeConsume(configId);
            if (canUpgrade && consume.HasConsume && consume.ItemId > 0 && consume.NeedCount > 0)
            {
                ConsumeItemId.Value = consume.ItemId;
                ConsumeCount.Value = consume.NeedCount;
                ShowConsume.Value = true;
            }
            else
            {
                ConsumeItemId.Value = 0;
                ConsumeCount.Value = 0;
                ShowConsume.Value = false;
            }
        }

        public async UniTask UpgradeAsync()
        {
            var configId = ConfigId.Value;
            if (configId <= 0 || mProvider == null)
            {
                return;
            }

            if (!mProvider.CanUpgrade(configId))
            {
                Toast.Show("已达最高星级");
                return;
            }

            if (!mProvider.CanAffordUpgrade(configId))
            {
                Toast.Show("材料不足");
                return;
            }

            var success = await mProvider.UpgradeAsync(configId);
            if (success)
            {
                Toast.Show("升星成功");
                RefreshAll();
            }
        }

        private void ResetDisplay()
        {
            ItemName.Value = string.Empty;
            ItemIcon.Value = string.Empty;
            CurStarCount.Value = 0;
            NextStarCount.Value = 0;
            MaxStarCount.Value = 0;
            CurBonusText.Value = string.Empty;
            NextBonusText.Value = string.Empty;
            CurSkillDesc.Value = string.Empty;
            NextSkillDesc.Value = string.Empty;
            ShowNextDetail.Value = false;
            ShowConsume.Value = false;
            LevelUpBtnInteractable.Value = false;
        }

        private void OnDataChanged(int configId)
        {
            if (configId == ConfigId.Value)
            {
                RefreshAll();
            }
        }
    }
}
