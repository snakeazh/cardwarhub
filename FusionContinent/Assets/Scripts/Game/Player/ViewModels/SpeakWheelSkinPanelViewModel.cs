using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using Game.CombatPower;
using Game.UserTool;
using UIFramework;
using UIFramework.Services;

namespace Game.Player
{
    /// <summary>
    /// 轮语者皮肤面板 ViewModel：列表、详情展示与穿戴/锻造操作均通过 SpeakerService 完成。
    /// </summary>
    public class SpeakWheelSkinPanelViewModel : ViewModel, IUserToolConfigurable
    {
        public BindableProperty<int> SelectedSkinId { get; } = new BindableProperty<int>(0);
        public BindableProperty<List<int>> PlayerSkinConfigIds { get; } = new BindableProperty<List<int>>(new List<int>());

        public BindableProperty<string> SkinName { get; } = new BindableProperty<string>(string.Empty);
        public BindableProperty<int> SkinStarCount { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> SkinMaxStarCount { get; } = new BindableProperty<int>(0);
        public BindableProperty<string> SkinSkillStateText { get; } = new BindableProperty<string>(string.Empty);

        public BindableProperty<bool> WearBtnInteractable { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> IsSelectedSkinEquipped { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> ForgeBtnInteractable { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> IsSelectedSkinActivated { get; } = new BindableProperty<bool>(false);

        public BindableProperty<bool> ShowNextUnlockConsume { get; } = new BindableProperty<bool>(false);
        public BindableProperty<int> NextUnlockConsumeItemId { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> NextUnlockConsumeCount { get; } = new BindableProperty<int>(0);
        public BindableProperty<string> WearBtnText { get; } = new BindableProperty<string>("穿戴");

        public SpeakerService SpeakerService => mSpeakerService;

        private SpeakerService mSpeakerService;
        private TbPlayerFigureConfig mTbPlayerFigureConfig;
        private System.Action<int> mSkinDataChangedHandler;
        private System.Action<object> mEquippedSkinChangedHandler;

        public override void OnCreate()
        {
            base.OnCreate();
            mSpeakerService = GameServiceManager.Get<SpeakerService>();
            mTbPlayerFigureConfig = DataConfigManager.Tables.TbPlayerFigureConfig;

            mSkinDataChangedHandler = OnSkinDataChanged;
            mEquippedSkinChangedHandler = OnEquippedSkinChanged;

            if (mSpeakerService != null)
            {
                mSpeakerService.OnSkinDataChanged += mSkinDataChangedHandler;
                mSpeakerService.OnWearSkinChanged += mSkinDataChangedHandler;
            }

            Global.Publisher.Subscribe(MessagePublisher.SPEAKER_EQUIPPED_SKIN_CHANGED, mEquippedSkinChangedHandler);

            InitSelection();
            RefreshAll();
        }

        public override void OnDestroy()
        {
            if (mSpeakerService != null)
            {
                if (mSkinDataChangedHandler != null)
                {
                    mSpeakerService.OnSkinDataChanged -= mSkinDataChangedHandler;
                    mSpeakerService.OnWearSkinChanged -= mSkinDataChangedHandler;
                }
            }

            if (mEquippedSkinChangedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.SPEAKER_EQUIPPED_SKIN_CHANGED, mEquippedSkinChangedHandler);
                mEquippedSkinChangedHandler = null;
            }

            base.OnDestroy();
        }

        public override UserToolData GetUserToolConfig()
        {
            return GlobalFunc.GetUserResourceConfig(PageType.PlayerPage);
        }

        public PlayerFigureConfig GetPlayerFigureConfig(int skinConfigId)
        {
            return mTbPlayerFigureConfig.GetOrDefault(skinConfigId);
        }

        public void SelectSkin(int skinConfigId)
        {
            if (skinConfigId <= 0)
            {
                return;
            }

            SelectedSkinId.Value = skinConfigId;
            RefreshSelectedDetail();
        }

        public void RefreshAll()
        {
            RefreshPlayerSkinConfigIds();
            RefreshSelectedDetail();
        }

        public async UniTask WearSelectedSkinAsync()
        {
            var skinConfigId = SelectedSkinId.Value;
            if (skinConfigId <= 0 || mSpeakerService == null)
            {
                return;
            }

            if (!mSpeakerService.IsActivated(skinConfigId))
            {
                if (!mSpeakerService.CanAffordActivate(skinConfigId))
                {
                    Toast.Show("材料不足");
                    return;
                }

                CombatPowerChangeHelper.CaptureBaseline();
                var activated = await mSpeakerService.ActivateAsync(skinConfigId);
                if (activated)
                {
                    Toast.Show("激活成功");
                    CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
                    RefreshAll();
                }

                return;
            }

            CombatPowerChangeHelper.CaptureBaseline();

            bool success;
            if (mSpeakerService.IsEquipped(skinConfigId))
            {
                success = await mSpeakerService.UnequipAsync(skinConfigId);
                if (success)
                {
                    Toast.Show("已卸下");
                }
            }
            else
            {
                var result = await mSpeakerService.TryWearSkinAsync(skinConfigId);
                success = result != null && result.Success;
                if (success)
                {
                    Toast.Show("穿戴成功");
                }
                else if (result != null && !string.IsNullOrEmpty(result.Reason))
                {
                    Toast.Show(result.Reason);
                }
            }

            if (success)
            {
                CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
                RefreshAll();
            }
        }

        public async UniTask ForgeSelectedSkinAsync()
        {
            var skinConfigId = SelectedSkinId.Value;
            if (skinConfigId <= 0 || mSpeakerService == null)
            {
                return;
            }

            if (!mSpeakerService.IsActivated(skinConfigId))
            {
                Toast.Show("皮肤未解锁");
                return;
            }

            if (!mSpeakerService.CanUpgrade(skinConfigId))
            {
                Toast.Show("已达最高星级");
                return;
            }

            if (!mSpeakerService.CanAffordUpgrade(skinConfigId))
            {
                Toast.Show("材料不足");
                return;
            }

            CombatPowerChangeHelper.CaptureBaseline();

            var success = await mSpeakerService.UpgradeAsync(skinConfigId);
            if (success)
            {
                Toast.Show("升星成功");
                CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
                RefreshAll();
            }
        }

        private void InitSelection()
        {
            var equippedId = mSpeakerService != null ? mSpeakerService.GetEquippedSkinConfigId() : 0;
            if (equippedId > 0)
            {
                SelectedSkinId.Value = equippedId;
                return;
            }

            var firstOwned = mSpeakerService != null ? mSpeakerService.GetFirstOwnedSkinConfigId() : 0;
            if (firstOwned > 0)
            {
                SelectedSkinId.Value = firstOwned;
                return;
            }

            var configs = mTbPlayerFigureConfig.DataList;
            SelectedSkinId.Value = configs.Count > 0 ? configs[0].Id : 0;
        }

        private void RefreshPlayerSkinConfigIds()
        {
            var configs = mTbPlayerFigureConfig.DataList;
            var unlocked = new List<int>(configs.Count);
            var locked = new List<int>(configs.Count);
            foreach (var cfg in configs)
            {
                if (mSpeakerService != null && mSpeakerService.IsActivated(cfg.Id))
                {
                    unlocked.Add(cfg.Id);
                }
                else
                {
                    locked.Add(cfg.Id);
                }
            }

            unlocked.AddRange(locked);
            PlayerSkinConfigIds.Value = unlocked;
        }

        private void RefreshSelectedDetail()
        {
            var skinConfigId = SelectedSkinId.Value;
            var cfg = mTbPlayerFigureConfig.GetOrDefault(skinConfigId);
            if (cfg == null || mSpeakerService == null)
            {
                SkinName.Value = string.Empty;
                SkinStarCount.Value = 0;
                SkinMaxStarCount.Value = 0;
                SkinSkillStateText.Value = string.Empty;
                WearBtnInteractable.Value = false;
                IsSelectedSkinEquipped.Value = false;
                ForgeBtnInteractable.Value = false;
                IsSelectedSkinActivated.Value = false;
                ShowNextUnlockConsume.Value = false;
                WearBtnText.Value = "激活";
                return;
            }

            var activated = mSpeakerService.IsActivated(skinConfigId);
            var level = activated ? mSpeakerService.GetSkinLevelByConfigId(skinConfigId) : 0;

            SkinName.Value = cfg.Name;
            SkinStarCount.Value = level;
            SkinMaxStarCount.Value = GetMaxStarCount(cfg);
            SkinSkillStateText.Value = activated ? "已激活" : "未激活";

            IsSelectedSkinActivated.Value = activated;
            IsSelectedSkinEquipped.Value = mSpeakerService.IsEquipped(skinConfigId);
            WearBtnInteractable.Value = true;
            ForgeBtnInteractable.Value = activated && mSpeakerService.CanUpgrade(skinConfigId);

            if (mSpeakerService.TryGetNextUnlockConsume(skinConfigId, out var consumeItemId, out var consumeCount)
                && consumeItemId > 0 && consumeCount > 0)
            {
                NextUnlockConsumeItemId.Value = consumeItemId;
                NextUnlockConsumeCount.Value = consumeCount;
                ShowNextUnlockConsume.Value = true;
            }
            else
            {
                ShowNextUnlockConsume.Value = false;
            }

            WearBtnText.Value = activated
                ? (IsSelectedSkinEquipped.Value ? "卸下" : "穿戴")
                : "激活";
        }

        private static int GetMaxStarCount(PlayerFigureConfig cfg)
        {
            if (cfg?.ImageStarList == null || cfg.ImageStarList.Count == 0)
            {
                return 0;
            }

            var maxLevel = 0;
            foreach (var starCfg in cfg.ImageStarList)
            {
                if (starCfg.Level > maxLevel)
                {
                    maxLevel = starCfg.Level;
                }
            }

            return maxLevel;
        }

        private void OnSkinDataChanged(int skinConfigId)
        {
            RefreshAll();
        }

        private void OnEquippedSkinChanged(object _)
        {
            RefreshAll();
        }
    }
}
