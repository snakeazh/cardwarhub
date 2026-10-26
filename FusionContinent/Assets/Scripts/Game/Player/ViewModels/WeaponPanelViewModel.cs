using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using Game.UserTool;
using Game.CombatPower;
using UIFramework;
using UIFramework.Services;

namespace Game.Player
{
    /// <summary>
    /// 武器面板 ViewModel：列表、详情展示与穿戴/锻造操作均通过 WeaponService 完成。
    /// </summary>
    public class WeaponPanelViewModel : ViewModel, IUserToolConfigurable
    {
        /// <summary>当前选中的武器配置 Id（<see cref="PlayerWeaponConfig.Id"/>）。</summary>
        public BindableProperty<int> SelectedWeaponConfigId { get; } = new BindableProperty<int>(0);
        /// <summary>武器配置 Id 列表（<see cref="PlayerWeaponConfig.Id"/>），顺序与 TbPlayerWeaponConfig 一致。</summary>
        public BindableProperty<List<int>> PlayerWeaponConfigIds { get; } = new BindableProperty<List<int>>(new List<int>());

        public BindableProperty<string> WeaponName { get; } = new BindableProperty<string>(string.Empty);
        public BindableProperty<int> WeaponStarCount { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> WeaponMaxStarCount { get; } = new BindableProperty<int>(0);
        public BindableProperty<string> WeaponBonusText { get; } = new BindableProperty<string>(string.Empty);
        public BindableProperty<string> WeaponSkillStateText { get; } = new BindableProperty<string>(string.Empty);

        public BindableProperty<bool> WearBtnInteractable { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> IsSelectedWeaponEquipped { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> ForgeBtnInteractable { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> IsSelectedWeaponActivated { get; } = new BindableProperty<bool>(false);

        public BindableProperty<bool> ShowNextUnlockConsume { get; } = new BindableProperty<bool>(false);
        public BindableProperty<int> NextUnlockConsumeItemId { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> NextUnlockConsumeCount { get; } = new BindableProperty<int>(0);
        public BindableProperty<string> WearBtnText { get; } = new BindableProperty<string>("装备");

        public WeaponService WeaponService => mWeaponService;

        private WeaponService mWeaponService;
        private TbPlayerWeaponConfig mTbPlayerWeaponConfig;
        private System.Action<int> mWeaponDataChangedHandler;
        private System.Action<object> mEquippedWeaponChangedHandler;

        public override void OnCreate()
        {
            base.OnCreate();
            mWeaponService = GameServiceManager.Get<WeaponService>();
            mTbPlayerWeaponConfig = DataConfigManager.Tables.TbPlayerWeaponConfig;

            mWeaponDataChangedHandler = OnWeaponDataChanged;
            mEquippedWeaponChangedHandler = OnEquippedWeaponChanged;

            if (mWeaponService != null)
            {
                mWeaponService.OnWeaponDataChanged += mWeaponDataChangedHandler;
            }

            Global.Publisher.Subscribe(MessagePublisher.SPEAKER_EQUIPPED_WEAPON_CHANGED, mEquippedWeaponChangedHandler);

            InitSelection();
            RefreshAll();
        }

        public override void OnDestroy()
        {
            if (mWeaponService != null && mWeaponDataChangedHandler != null)
            {
                mWeaponService.OnWeaponDataChanged -= mWeaponDataChangedHandler;
            }

            if (mEquippedWeaponChangedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.SPEAKER_EQUIPPED_WEAPON_CHANGED, mEquippedWeaponChangedHandler);
                mEquippedWeaponChangedHandler = null;
            }

            base.OnDestroy();
        }

        public override UserToolData GetUserToolConfig()
        {
            return GlobalFunc.GetUserResourceConfig(PageType.PlayerPage);
        }

        public PlayerWeaponConfig GetPlayerWeaponConfig(int weaponConfigId)
        {
            return mTbPlayerWeaponConfig.GetOrDefault(weaponConfigId);
        }

        public void SelectWeapon(int weaponConfigId)
        {
            if (weaponConfigId <= 0)
            {
                return;
            }

            SelectedWeaponConfigId.Value = weaponConfigId;
            RefreshSelectedDetail();
        }

        public void RefreshAll()
        {
            RefreshPlayerWeaponConfigIds();
            RefreshSelectedDetail();
        }

        public async UniTask WearSelectedWeaponAsync()
        {
            var weaponConfigId = SelectedWeaponConfigId.Value;
            if (weaponConfigId <= 0 || mWeaponService == null)
            {
                return;
            }

            if (!mWeaponService.IsActivated(weaponConfigId))
            {
                if (!mWeaponService.CanAffordActivate(weaponConfigId))
                {
                    Toast.Show("材料不足");
                    return;
                }

                CombatPowerChangeHelper.CaptureBaseline();
                var activated = await mWeaponService.ActivateAsync(weaponConfigId);
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
            if (mWeaponService.IsEquipped(weaponConfigId))
            {
                success = await mWeaponService.UnequipAsync(weaponConfigId);
                if (success)
                {
                    Toast.Show("已卸下");
                }
            }
            else
            {
                success = await mWeaponService.EquipAsync(weaponConfigId);
                if (success)
                {
                    Toast.Show("穿戴成功");
                }
            }

            if (success)
            {
                CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
                RefreshAll();
            }
        }

        public async UniTask ForgeSelectedWeaponAsync()
        {
            var weaponConfigId = SelectedWeaponConfigId.Value;
            if (weaponConfigId <= 0 || mWeaponService == null)
            {
                return;
            }

            if (mTbPlayerWeaponConfig.GetOrDefault(weaponConfigId) == null)
            {
                return;
            }

            CombatPowerChangeHelper.CaptureBaseline();

            bool success;
            if (!mWeaponService.IsActivated(weaponConfigId))
            {
                if (!mWeaponService.CanAffordActivate(weaponConfigId))
                {
                    Toast.Show("材料不足");
                    return;
                }

                success = await mWeaponService.ActivateAsync(weaponConfigId);
                if (success)
                {
                    Toast.Show("激活成功");
                }
            }
            else
            {
                if (!mWeaponService.CanUpgrade(weaponConfigId))
                {
                    Toast.Show("已达最高星级");
                    return;
                }

                if (!mWeaponService.CanAffordUpgrade(weaponConfigId))
                {
                    Toast.Show("材料不足");
                    return;
                }

                success = await mWeaponService.UpgradeAsync(weaponConfigId);
                if (success)
                {
                    Toast.Show("锻造成功");
                }
            }

            if (success)
            {
                CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
                RefreshAll();
            }
        }

        private void InitSelection()
        {
            var equippedId = mWeaponService.GetEquippedWeaponConfigId();
            if (equippedId > 0)
            {
                SelectedWeaponConfigId.Value = equippedId;
                return;
            }

            var firstOwned = mWeaponService.GetFirstOwnedWeaponConfigId();
            if (firstOwned > 0)
            {
                SelectedWeaponConfigId.Value = firstOwned;
                return;
            }

            var configs = mTbPlayerWeaponConfig.DataList;
            SelectedWeaponConfigId.Value = configs.Count > 0 ? configs[0].Id : 0;
        }

        private void RefreshPlayerWeaponConfigIds()
        {
            var configs = mTbPlayerWeaponConfig.DataList;
            // 已解锁武器排前、未解锁排后，各自保持配置表原始顺序
            var unlocked = new List<int>(configs.Count);
            var locked = new List<int>(configs.Count);
            foreach (var cfg in configs)
            {
                if (mWeaponService != null && mWeaponService.IsActivated(cfg.Id))
                {
                    unlocked.Add(cfg.Id);
                }
                else
                {
                    locked.Add(cfg.Id);
                }
            }

            unlocked.AddRange(locked);
            PlayerWeaponConfigIds.Value = unlocked;
        }

        private void RefreshSelectedDetail()
        {
            var weaponConfigId = SelectedWeaponConfigId.Value;
            var cfg = mTbPlayerWeaponConfig.GetOrDefault(weaponConfigId);
            if (cfg == null || mWeaponService == null)
            {
                WeaponName.Value = string.Empty;
                WeaponStarCount.Value = 0;
                WeaponMaxStarCount.Value = 0;
                WeaponBonusText.Value = string.Empty;
                WeaponSkillStateText.Value = string.Empty;
                WearBtnInteractable.Value = false;
                IsSelectedWeaponEquipped.Value = false;
                ForgeBtnInteractable.Value = false;
                IsSelectedWeaponActivated.Value = false;
                ShowNextUnlockConsume.Value = false;
                WearBtnText.Value = "激活";
                return;
            }

            var activated = mWeaponService.IsActivated(weaponConfigId);
            var level = activated ? mWeaponService.GetWeaponLevelByConfigId(weaponConfigId) : 1;

            var levelCfg = cfg.WeaponStarList.FirstOrDefault(v => v.Level == level)
                           ?? cfg.WeaponStarList.FirstOrDefault();

            WeaponName.Value = cfg.Name;
            WeaponStarCount.Value = activated ? level : 0;
            WeaponMaxStarCount.Value = GetMaxStarCount(cfg);
            WeaponBonusText.Value = levelCfg != null ? levelCfg.Desc1 : string.Empty;
            WeaponSkillStateText.Value = activated ? "已激活" : "未激活";

            IsSelectedWeaponActivated.Value = activated;
            IsSelectedWeaponEquipped.Value = mWeaponService.IsEquipped(weaponConfigId);
            WearBtnInteractable.Value = true;
            ForgeBtnInteractable.Value = activated && mWeaponService.CanUpgrade(weaponConfigId);

            if (mWeaponService.TryGetNextUnlockConsume(weaponConfigId, out var consumeItemId, out var consumeCount)
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
                ? (IsSelectedWeaponEquipped.Value ? "卸下" : "装备")
                : "激活";
        }

        private static int GetMaxStarCount(PlayerWeaponConfig cfg)
        {
            if (cfg?.WeaponStarList == null || cfg.WeaponStarList.Count == 0)
            {
                return 0;
            }

            var maxLevel = 0;
            foreach (var starCfg in cfg.WeaponStarList)
            {
                if (starCfg.Level > maxLevel)
                {
                    maxLevel = starCfg.Level;
                }
            }

            return maxLevel;
        }

        private void OnWeaponDataChanged(int weaponConfigId)
        {
            RefreshAll();
        }

        private void OnEquippedWeaponChanged(object _)
        {
            RefreshAll();
        }
    }
}
