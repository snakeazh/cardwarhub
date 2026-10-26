using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using Game.CombatPower;
using Game.Equip;
using Game.UI;
using UIFramework;
using UIFramework.Services;
using UILevel = QFramework.UILevel;

namespace Game.Player
{
    public enum SpeakWhellTab
    {
        Material = 0,
        FusionGem = 1,
        Equip = 2,
    }

    public class SpeakWhellPanelViewModel : ViewModel, Game.UserTool.IUserToolConfigurable
    {
        /// <summary>
        /// 获取 UserTool 配置
        /// </summary>
        public override Game.UserTool.UserToolData GetUserToolConfig()
        {
            return GlobalFunc.GetUserResourceConfig(Config.PageType.PlayerPage);
        }
        public Command OnTalentCommand { get; private set; }
        public Command OnSkinCommand { get; private set; }
        public Command OnEquipUpGradeCommand { get; private set; }
        public BindableProperty<int> FightPowerValue { get; } = new BindableProperty<int>(0);

        public BindableProperty<SpeakWhellTab> SelectedTab { get; } = new BindableProperty<SpeakWhellTab>(SpeakWhellTab.Equip);
        public BindableProperty<int> SelectedMaterialIndex { get; } = new BindableProperty<int>(-1);
        public BindableProperty<int> SelectedFusionGemIndex { get; } = new BindableProperty<int>(-1);

        public BindableProperty<int> SpeakerSkinId { get; } = new BindableProperty<int>(0);
        public BindableProperty<string> SpeakerName { get; } = new BindableProperty<string>(string.Empty);
        public BindableProperty<string> SpeakerSpineAddress { get; } = new BindableProperty<string>(string.Empty);

        public BindableProperty<List<CardItems>> MaterialDisplayedCards { get; } = new BindableProperty<List<CardItems>>(new List<CardItems>());
        public BindableProperty<List<PlayerFusionGem>> FusionGemDisplayed { get; } = new BindableProperty<List<PlayerFusionGem>>(new List<PlayerFusionGem>());
        /// <summary>
        /// 背包中未穿戴的装备（WearerId==0），供轮语者页「装备」页签展示。
        /// </summary>
        public BindableProperty<List<PlayerEquip>> EquipDisplayed { get; } = new BindableProperty<List<PlayerEquip>>(new List<PlayerEquip>());
        public BindableProperty<int> SelectedEquipIndex { get; } = new BindableProperty<int>(-1);
        /// <summary>
        /// 轮语者已穿戴装备按部位顺序排列（索引与 TbGameBattleConst.EquipPartTypeList 一致），空槽为 null。
        /// 佩戴者 Id 使用 TbPlayerConst.GlobalUserWearId。
        /// </summary>
        public BindableProperty<List<PlayerEquip>>  SpeakerWearPartSlots { get; } =
            new BindableProperty<List<PlayerEquip>>(new List<PlayerEquip>());
        
        /// <summary>
        /// 当前槽位已装备融合石列表（按部位顺序），数据源：轮语者已穿戴装备上的融合石。
        /// 槽位为空则为 null。
        /// </summary>
        public BindableProperty<List<PlayerFusionGem>> WearFusionGemSlotList { get; } =
            new BindableProperty<List<PlayerFusionGem>>(new List<PlayerFusionGem>());

        private TbItemConfig mItemConfig;
        private TbGemFusionConfig mTbGemFusionConfig;
        private TbPlayerFigureConfig mTbPlayerFigureConfig;
        private TbGameBattleConst mTbGameBattleConst;
        private SpeakerService mSpeakerService;
        private CombatPowerService mCombatPowerService;
        private System.Action<int> mWearSkinChangedHandler;
        private System.Action<object> mWearFusionGemListChangedHandler;
        private System.Action<object> mPlayerEquipDataChangedHandler;

        public override void OnCreate()
        {
            base.OnCreate();

            OnTalentCommand = new Command(OnTalent);
            OnSkinCommand = new Command(OnSkin);
            OnEquipUpGradeCommand = new Command(OnEquipUpGrade);

            mCombatPowerService = GameServiceManager.Get<CombatPowerService>();
            mCombatPowerService.OnCombatPowerChanged += OnCombatPowerChanged;

            RefreshFightPower();
            RefreshMaterialDisplayedCards();
            RefreshFusionGemDisplayed();
            RefreshWearFusionGemSlotList();
            RefreshSpeakerEquipLists();

            BindSpeakerEvents();
            BindFusionGemWearEvents();
            BindPlayerEquipChangedEvents();
            RefreshSpeakerPresentation(force: true);
        }

        public override void OnDestroy()
        {
            if (mCombatPowerService != null)
            {
                mCombatPowerService.OnCombatPowerChanged -= OnCombatPowerChanged;
                mCombatPowerService = null;
            }

            UnbindPlayerEquipChangedEvents();
            UnbindFusionGemWearEvents();
            UnbindSpeakerEvents();
            base.OnDestroy();
        }

        public void RefreshFightPower()
        {
            FightPowerValue.Value = mCombatPowerService.GetDisplayedAccountPower();
        }

        private void OnCombatPowerChanged(CombatPowerSnapshot snapshot)
        {
            FightPowerValue.Value = snapshot.TotalPower;
        }

        public void RefreshMaterialDisplayedCards()
        {
            if (mItemConfig == null)
            {
                mItemConfig = DataConfigManager.Tables.TbItemConfig;
            }

            var displayed = new List<CardItems>();

            if (Global.PlayerItems != null)
            {
                foreach (var v in Global.PlayerItems)
                {
                    var cfg = mItemConfig.GetOrDefault(v.Value.ItemId);
                    if (cfg == null) continue;

                    var card = new CardItems
                    {
                        cardid = v.Value.ItemId,
                        cardnumber = v.Value.ItemNum,
                        cardtype = (int)cfg.Quality
                    };

                    displayed.Add(card);
                }
            }

            displayed = displayed
                .Select(t => new { Card = t, Config = mItemConfig.GetOrDefault(t.cardid) })
                .Where(x => x.Config != null &&
                            (x.Config.ModuleType == ModuleType.Prop ||
                             x.Config.ModuleType == ModuleType.CardFragment ||
                             x.Config.ModuleType == ModuleType.GiftPack) &&
                            x.Card.cardnumber > 0)
                .OrderByDescending(x => x.Card.cardtype)
                .Select(x => x.Card)
                .ToList();

            MaterialDisplayedCards.Value = displayed;
            SelectedMaterialIndex.Value = displayed.Count > 0 ? 0 : -1;
        }

        public void RefreshFusionGemDisplayed()
        {
            if (mTbGemFusionConfig == null)
            {
                mTbGemFusionConfig = DataConfigManager.Tables.TbGemFusionConfig;
            }

            var displayed = new List<PlayerFusionGem>();

            if (Global.PlayerFusionGems != null && Global.PlayerFusionGems.Count > 0)
            {
                foreach (var gem in Global.PlayerFusionGems.Values)
                {
                    if (gem == null) continue;

                    // 兜底：部分数据可能 Config 为空
                    if (gem.Config == null && mTbGemFusionConfig != null && gem.GemId > 0)
                    {
                        gem.Config = mTbGemFusionConfig.GetOrDefault(gem.GemId);
                    }

                    // 若仍无配置，跳过（避免后续 Icon/Name 空引用）
                    if (gem.Config == null) continue;

                    // Quality 以配置为准（避免历史数据不一致）
                    gem.Quality = gem.Config.Quality;
                    displayed.Add(gem);
                }
            }

            displayed.Sort((a, b) =>
            {
                var q = b.Quality.CompareTo(a.Quality);
                if (q != 0) return q;
                var l = b.Level.CompareTo(a.Level);
                if (l != 0) return l;
                return b.Id.CompareTo(a.Id);
            });

            FusionGemDisplayed.Value = displayed;
            SelectedFusionGemIndex.Value = displayed.Count > 0 ? 0 : -1;
            
            // 融合石列表变化后，同步刷新当前槽位穿戴列表（按流水id映射）
            RefreshWearFusionGemSlotList();
        }
        
        public void RefreshWearFusionGemSlotList()
        {
            var equipService = GameServiceManager.Get<EquipService>();
            var wearerId = GetSpeakerEquipWearerId();
            var partNameList = DataConfigManager.Tables.TbGameBattleConst.EquipPartTypeList;
            var count = partNameList != null ? partNameList.Count : 0;
            var slotList = new List<PlayerFusionGem>(count);

            for (int i = 0; i < count; i++)
            {
                var partType = (EquipPartType)i;
                PlayerFusionGem gem = null;
                if (equipService != null)
                {
                    var slotData = equipService.GetWearPartSlot(wearerId, partType);
                    gem = slotData?.FusionGem;
                    if (gem != null && gem.Config == null && gem.GemId > 0)
                    {
                        mTbGemFusionConfig ??= DataConfigManager.Tables.TbGemFusionConfig;
                        if (mTbGemFusionConfig != null)
                        {
                            gem.Config = mTbGemFusionConfig.GetOrDefault(gem.GemId);
                        }
                    }

                    if (gem?.Config != null)
                    {
                        gem.Quality = gem.Config.Quality;
                    }
                }

                slotList.Add(gem);
            }

            WearFusionGemSlotList.Value = slotList;
        }

        /// <summary>
        /// 轮语者装备穿戴者 Id（与服务器约定一致，见 TbPlayerConst）。
        /// </summary>
        public int GetSpeakerEquipWearerId()
        {
            return DataConfigManager.Tables.TbPlayerConst.GlobalUserWearId;
        }

        /// <summary>
        /// 刷新轮语者部位槽位 + 背包装备列表。
        /// </summary>
        public void RefreshSpeakerEquipLists()
        {
            // 先刷新背包装备列表，再刷新部位槽位，避免槽位 OnValueChanged 触发时 EquipDisplayed 仍为旧数据
            RefreshEquipDisplayed();
            RefreshSpeakerWearPartSlots();
        }

        public void RefreshSpeakerWearPartSlots()
        {
            mTbGameBattleConst ??= DataConfigManager.Tables.TbGameBattleConst;
            var wearerId = GetSpeakerEquipWearerId();
            var wearList = Global.PlayerEquips != null
                ? Global.PlayerEquips.Values.Where(v => v != null && v.WearerId == wearerId).ToList()
                : new List<PlayerEquip>();

            var partNameList = mTbGameBattleConst?.EquipPartTypeList;
            var count = partNameList != null ? partNameList.Count : 0;
            var slots = new List<PlayerEquip>(count);
            for (int i = 0; i < count; i++)
            {
                var pt = (EquipPartType)i;
                slots.Add(wearList.FirstOrDefault(e => e.PartType == pt));
            }

            SpeakerWearPartSlots.Value = slots;
        }

        public void RefreshEquipDisplayed()
        {
            // 仅展示未穿戴（WearerId==0）；已穿在任意角色（含轮语者 GlobalUserWearId）上的实例不会进入列表
            var displayed = Global.PlayerEquips != null
                ? Global.PlayerEquips.Values
                    .Where(v => v != null && v.WearerId == 0)
                    .OrderByDescending(x => x.Quality)
                    .ThenByDescending(x => x.Phase)
                    .ThenByDescending(x => x.Id)
                    .ToList()
                : new List<PlayerEquip>();

            EquipDisplayed.Value = displayed;
            SelectedEquipIndex.Value = displayed.Count > 0 ? 0 : -1;
        }

        private void BindSpeakerEvents()
        {
            mSpeakerService ??= GameServiceManager.Get<SpeakerService>();
            if (mSpeakerService == null)
            {
                return;
            }

            mWearSkinChangedHandler = _ => RefreshSpeakerPresentation(force: true);
            mSpeakerService.OnWearSkinChanged += mWearSkinChangedHandler;
        }

        private void BindFusionGemWearEvents()
        {
            if (mWearFusionGemListChangedHandler != null)
            {
                return;
            }

            mWearFusionGemListChangedHandler = _ =>
            {
                RefreshFusionGemDisplayed();
            };
            Global.Publisher.Subscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, mWearFusionGemListChangedHandler);
        }

        private void UnbindFusionGemWearEvents()
        {
            if (mWearFusionGemListChangedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, mWearFusionGemListChangedHandler);
                mWearFusionGemListChangedHandler = null;
            }
        }

        private void BindPlayerEquipChangedEvents()
        {
            if (mPlayerEquipDataChangedHandler != null)
            {
                return;
            }

            mPlayerEquipDataChangedHandler = OnPlayerEquipDataChanged;
            Global.Publisher.Subscribe(MessagePublisher.PLAYER_EQUIP_DATA_CHANGED, mPlayerEquipDataChangedHandler);
        }

        private void UnbindPlayerEquipChangedEvents()
        {
            if (mPlayerEquipDataChangedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.PLAYER_EQUIP_DATA_CHANGED, mPlayerEquipDataChangedHandler);
                mPlayerEquipDataChangedHandler = null;
            }
        }

        private void OnPlayerEquipDataChanged(object _)
        {
            RefreshSpeakerEquipLists();
        }

        private void UnbindSpeakerEvents()
        {
            if (mSpeakerService != null && mWearSkinChangedHandler != null)
            {
                mSpeakerService.OnWearSkinChanged -= mWearSkinChangedHandler;
                mWearSkinChangedHandler = null;
            }
        }

        public void RefreshSpeakerPresentation(bool force)
        {
            if (mTbPlayerFigureConfig == null)
            {
                mTbPlayerFigureConfig = DataConfigManager.Tables.TbPlayerFigureConfig;
            }

            var skinId = Global.UserPlayerSpeaker?.UseSkinId ?? 0;
            if (!force && skinId == SpeakerSkinId.Value)
            {
                return;
            }

            SpeakerSkinId.Value = skinId;

            if (skinId <= 0)
            {
                SpeakerName.Value = string.Empty;
                SpeakerSpineAddress.Value = string.Empty;
                return;
            }

            var cfg = mTbPlayerFigureConfig.GetOrDefault(skinId);
            if (cfg == null)
            {
                LogTool.LogWarning($"[SpeakWhellPanelViewModel] TbPlayerFigureConfig 未找到轮语者皮肤: id={skinId}");
                SpeakerName.Value = string.Empty;
                SpeakerSpineAddress.Value = string.Empty;
                return;
            }

            SpeakerName.Value = cfg.Name ?? string.Empty;

            // ShowSpine 约定：取第0个作为预制体Key
            var spineKey = cfg.ShowSpine;
            if (string.IsNullOrEmpty(spineKey))
            {
                LogTool.LogWarning($"[SpeakWhellPanelViewModel] PlayerFigureConfig.ShowSpine 为空: id={skinId}");
                SpeakerSpineAddress.Value = string.Empty;
                return;
            }

            SpeakerSpineAddress.Value = $"Speaker_{spineKey}";
        }

        private void OnTalent()
        {
            // TODO: 打开天赋界面/轮语者天赋逻辑
            // UIFramework
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Talent))
            {
                return;
            }
            QFramework.UIKit.OpenPanel<TalentPanel>(UILevel.Panel);
        }

        private void OnSkin()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.PlayerAvatar))
            {
                return;
            }
            var vm = new SpeakWheelSkinPanelViewModel();
            vm.OnCreate();
            UIFramework.UIManager.OpenPanelAsync<SpeakWheelSkinPanelNew, SpeakWheelSkinPanelViewModel>(vm, UIFramework.UILevel.Panel).Forget();
        }

        private void OnEquipUpGrade()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.EquipPhase))
            {
                return;
            }
            var vm = new Game.Equip.EquipUpGradePanelViewModel();
            vm.OnCreate();
            if (!vm.Initialize(DataConfigManager.Tables.TbPlayerConst.GlobalUserWearId))
            {
                Toast.Show(EquipUpGradePanelViewModel.NoWearEquipMessage);
                return;
            }
            UIFramework.UIManager.OpenPanelAsync<EquipUpGradePanelNew, Game.Equip.EquipUpGradePanelViewModel>(
                vm,
                UIFramework.UILevel.Panel).Forget();
        }
    }
}
