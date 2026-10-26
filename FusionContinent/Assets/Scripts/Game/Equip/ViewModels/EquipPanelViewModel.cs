using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.CombatPower;
using UIFramework.Services;

namespace Game.Equip
{
    /// <summary>
    /// 装备界面 ViewModel
    /// </summary>
    public class EquipPanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<int> SoldierId { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> SoldierFightingNum { get; } = new BindableProperty<int>(0);
        public BindableProperty<List<PlayerEquip>> WearEquipList { get; } = new BindableProperty<List<PlayerEquip>>();
        public BindableProperty<List<PlayerEquip>> EquipList { get; } = new BindableProperty<List<PlayerEquip>>();
        public BindableProperty<string> SoldierName { get; } = new BindableProperty<string>("");
        public BindableProperty<string> SoldierTypeName { get; } = new BindableProperty<string>("");
        public BindableProperty<Sprite> SoldierTypeIcon { get; } = new BindableProperty<Sprite>();
        /// <summary>
        /// 装备品质筛选索引（Default表示全部）
        /// </summary>
        public BindableProperty<QualityType> FilterQuality { get; } = new BindableProperty<QualityType>(QualityType.Default);
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command OnUpgradeCommand { get; private set; }
        public Command OnAutoWearCommand { get; private set; }
        public Command OnFragmentCommand { get; private set; }
        public Command<EquipPartType> OnWearEquipCommand { get; private set; }
        /// <summary>
        /// 品质筛选命令
        /// </summary>
        public Command<QualityType> OnFilterQualityCommand { get; private set; }
        #endregion

        #region 依赖
        private EquipService mEquipService;
        #endregion

        #region 配置数据
        private TbCardBaseConfig mCardBaseConfig;
        private TbEquipConfig mEquipCardConfig;
        private TbSoldierBattleConfig mSoldierBattleConfig;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mEquipService = GameServiceManager.Get<EquipService>();
            
            // 加载配置
            mCardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            mEquipCardConfig = DataConfigManager.Tables.TbEquipConfig;
            mSoldierBattleConfig = DataConfigManager.Tables.TbSoldierBattleConfig;
            
            // 初始化命令
            OnCloseCommand = new Command(OnClose);
            OnUpgradeCommand = new Command(OnUpgrade);
            OnAutoWearCommand = new Command(OnAutoWear);
            OnFragmentCommand = new Command(OnFragment);
            OnWearEquipCommand = new Command<EquipPartType>(OnWearEquip);
            OnFilterQualityCommand = new Command<QualityType>(OnFilterQuality);
            
            WearEquipList.Value = new List<PlayerEquip>();
            EquipList.Value = new List<PlayerEquip>();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        public void Initialize(int soldierId)
        {
            if (soldierId == 0)
            {
                Toast.Show("英灵未解锁");
                OnCloseRequested?.Invoke();
                return;
            }

            SoldierId.Value = soldierId;
            
            var cardCfg = mCardBaseConfig.GetOrDefault(soldierId);
            var soldierCfg = mSoldierBattleConfig.GetOrDefault(soldierId);
            
            if (cardCfg == null || soldierCfg == null)
            {
                LogTool.LogError($"[EquipPanelViewModel] 角色ID错误: {soldierId}");
                return;
            }

            // 角色信息
            SoldierName.Value = cardCfg.Name;
            SoldierTypeName.Value = soldierCfg.Name;
            SoldierTypeIcon.Value = Global.CardIconSpriteAtlas.GetSpriteExt(soldierCfg.MainAttrType.ToString());
            
            // 刷新数据
            RefreshData();
        }

        /// <summary>
        /// 刷新数据（支持品质筛选）
        /// </summary>
        public void RefreshData()
        {
            // 已佩戴的装备
            WearEquipList.Value = Global.PlayerEquips.Values.Where(v => v.WearerId == SoldierId.Value).ToList();
            // 未佩戴的装备（按品质降序排列，支持品质筛选）
            var filteredEquipList = Global.PlayerEquips.Values.Where(v => v.WearerId == 0);
            
            // 应用品质筛选
            if (FilterQuality.Value != QualityType.Default)
            {
                filteredEquipList = filteredEquipList.Where(v => v.Quality == FilterQuality.Value);
            }
            
            EquipList.Value = filteredEquipList.OrderByDescending(x => x.Quality).ToList();
            
            // 计算战力
            CalculateFightingNum();
        }

        /// <summary>
        /// 品质筛选处理
        /// </summary>
        private void OnFilterQuality(QualityType quality)
        {
            FilterQuality.Value = quality;
            LogTool.Log($"[EquipPanelViewModel] OnFilterQuality: {quality}");
            
            // 刷新装备列表（不重新加载已佩戴装备）
            RefreshEquipList();
        }

        /// <summary>
        /// 刷新装备列表（仅刷新未佩戴列表）
        /// </summary>
        private void RefreshEquipList()
        {
            var filteredEquipList = Global.PlayerEquips.Values.Where(v => v.WearerId == 0);
            
            // 应用品质筛选
            if (FilterQuality.Value != QualityType.Default)
            {
                filteredEquipList = filteredEquipList.Where(v => v.Quality == FilterQuality.Value);
            }
            
            EquipList.Value = filteredEquipList.OrderByDescending(x => x.Quality).ToList();
            LogTool.Log($"[EquipPanelViewModel] RefreshEquipList: count={EquipList.Value.Count}, Filter={FilterQuality.Value}");
        }

        /// <summary>计算单英灵战力。</summary>
        private void CalculateFightingNum()
        {
            var playerSoldier = Global.GetCardById(SoldierId.Value);
            if (playerSoldier != null)
            {
                SoldierFightingNum.Value = FightNumCalcHelper.CalcSoldierFightNum(
                    playerSoldier.CardId,
                    playerSoldier.Lv);
            }
        }

        #region 命令实现

        private void OnClose()
        {
            OnCloseRequested?.Invoke();
        }

        private void OnUpgrade()
        {
            OnUpgradeRequested?.Invoke(SoldierId.Value);
        }

        private void OnAutoWear()
        {
            // 一键佩戴逻辑
            LogTool.Log("[EquipPanelViewModel] 一键佩戴");
        }

        private void OnFragment()
        {
            OnOpenEquipFragmentRequested?.Invoke(SoldierId.Value);
        }

        private async void OnWearEquip(EquipPartType partType)
        {
            CombatPowerChangeHelper.CaptureBaseline();

            var result = await mEquipService.WearEquipAsync(SoldierId.Value, partType);
            if (result.Success)
            {
                CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
                RefreshData();
            }
        }

        #endregion

        #region 事件
        public System.Action OnCloseRequested;
        public System.Action<int> OnUpgradeRequested;
        public System.Action<int> OnOpenEquipFragmentRequested;
        #endregion
    }
}
