using System.Collections.Generic;
using UIFramework;

namespace Game.Player
{
    /// <summary>
    /// 特殊效果面板 ViewModel（通用版，供武器/皮肤形象共用）。
    /// 数据源由 IStarItemDataProvider 提供，每条星级对应一条特殊效果行。
    /// 特殊效果描述使用 StarLevelInfo.Desc2。
    /// </summary>
    public class StarSpecialEPanelViewModel : ViewModel
    {
        /// <summary>当前展示的道具配置 Id。</summary>
        public int ConfigId { get; private set; }

        /// <summary>技能名（道具名）。</summary>
        public BindableProperty<string> SkillName { get; } = new BindableProperty<string>(string.Empty);

        /// <summary>当前星级（未激活为 0），用于判定各条效果是否解锁。</summary>
        public BindableProperty<int> CurrentLevel { get; } = new BindableProperty<int>(0);

        /// <summary>特殊效果星级配置列表，顺序与配置星级列表一致。</summary>
        public BindableProperty<List<StarLevelInfo>> EffectList { get; } =
            new BindableProperty<List<StarLevelInfo>>(new List<StarLevelInfo>());

        private IStarItemDataProvider mProvider;

        public override void OnCreate()
        {
            base.OnCreate();
        }

        /// <summary>
        /// 注入数据提供者、初始化配置 Id 并加载展示数据。
        /// </summary>
        public void Initialize(IStarItemDataProvider provider, int configId)
        {
            mProvider = provider;
            ConfigId = configId;
            Refresh();
        }

        /// <summary>根据当前数据刷新技能名与效果列表。</summary>
        public void Refresh()
        {
            if (mProvider == null)
            {
                SkillName.Value = string.Empty;
                CurrentLevel.Value = 0;
                EffectList.Value = new List<StarLevelInfo>();
                return;
            }

            SkillName.Value = mProvider.GetName(ConfigId);
            CurrentLevel.Value = mProvider.GetLevel(ConfigId);
            EffectList.Value = mProvider.GetStarLevelList(ConfigId);
        }
    }
}
