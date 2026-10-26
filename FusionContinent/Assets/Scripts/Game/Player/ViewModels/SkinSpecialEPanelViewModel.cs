using System.Collections.Generic;
using Config;
using FusionContinent;
using UIFramework;
using UIFramework.Services;

namespace Game.Player
{
    /// <summary>
    /// 皮肤特殊效果面板 ViewModel。
    /// 数据源：PlayerFigureConfig.ImageStarList，每条星级对应一条特殊效果行（Desc2）。
    /// </summary>
    public class SkinSpecialEPanelViewModel : ViewModel
    {
        public int SkinConfigId { get; private set; }

        public BindableProperty<string> SkillName { get; } = new BindableProperty<string>(string.Empty);
        public BindableProperty<int> CurrentLevel { get; } = new BindableProperty<int>(0);
        public BindableProperty<List<ImageStarListStruct>> EffectList { get; } =
            new BindableProperty<List<ImageStarListStruct>>(new List<ImageStarListStruct>());

        private TbPlayerFigureConfig mTbPlayerFigureConfig;
        private SpeakerService mSpeakerService;

        public override void OnCreate()
        {
            base.OnCreate();
            mTbPlayerFigureConfig = DataConfigManager.Tables.TbPlayerFigureConfig;
            mSpeakerService = GameServiceManager.Get<SpeakerService>();
        }

        public void Initialize(int skinConfigId)
        {
            SkinConfigId = skinConfigId;
            Refresh();
        }

        public void Refresh()
        {
            var cfg = mTbPlayerFigureConfig.GetOrDefault(SkinConfigId);
            if (cfg == null)
            {
                SkillName.Value = string.Empty;
                CurrentLevel.Value = 0;
                EffectList.Value = new List<ImageStarListStruct>();
                return;
            }

            SkillName.Value = cfg.Name;
            CurrentLevel.Value = mSpeakerService != null && mSpeakerService.IsActivated(SkinConfigId)
                ? mSpeakerService.GetSkinLevelByConfigId(SkinConfigId)
                : 0;
            EffectList.Value = cfg.ImageStarList;
        }
    }
}
