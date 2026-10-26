using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using FusionContinent.Model;
using FusionContinent.Utils;
using Newtonsoft.Json;
using UIFramework.Services;

namespace Game.Equip
{
    /// <summary>
    /// 装备分解界面 ViewModel
    /// </summary>
    public class EquipFragmentPanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<int> SoldierId { get; } = new BindableProperty<int>(0);
        /// <summary>部位筛选：-1 表示全部，其余为 EquipPartType 枚举值。</summary>
        public BindableProperty<int> FilterPart { get; } = new BindableProperty<int>(-1);
        /// <summary>品质筛选：Default 表示全部。</summary>
        public BindableProperty<QualityType> FilterQuality { get; } = new BindableProperty<QualityType>(QualityType.Default);
        public BindableProperty<List<PlayerEquip>> EquipList { get; } = new BindableProperty<List<PlayerEquip>>();
        public BindableProperty<List<int>> SelectedEquipIds { get; } = new BindableProperty<List<int>>();
        public BindableProperty<bool> CanFragment { get; } = new BindableProperty<bool>(false);
        public BindableProperty<Dictionary<int, int>> FragmentRewards { get; } = new BindableProperty<Dictionary<int, int>>();
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command OnFragmentCommand { get; private set; }
        public Command<int> OnToggleEquipCommand { get; private set; }
        public Command<int> OnFilterPartCommand { get; private set; }
        public Command<QualityType> OnFilterQualityCommand { get; private set; }
        #endregion

        #region 依赖
        private EquipService mEquipService;
        #endregion

        #region 配置数据
        private TbEquipConfig mEquipCardConfig;
        private TbEquipEntryConfig mEntryConfig;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mEquipService = GameServiceManager.Get<EquipService>();
            
            // 加载配置
            mEquipCardConfig = DataConfigManager.Tables.TbEquipConfig;
            mEntryConfig = DataConfigManager.Tables.TbEquipEntryConfig;
            
            // 初始化命令
            OnCloseCommand = new Command(OnClose);
            OnFragmentCommand = new Command(OnFragment, CanExecuteFragment);
            OnToggleEquipCommand = new Command<int>(OnToggleEquip);
            OnFilterPartCommand = new Command<int>(OnFilterPart);
            OnFilterQualityCommand = new Command<QualityType>(OnFilterQuality);
            
            SelectedEquipIds.Value = new List<int>();
            FragmentRewards.Value = new Dictionary<int, int>();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        public void Initialize(int soldierId)
        {
            SoldierId.Value = soldierId;
            FilterPart.Value = -1;
            FilterQuality.Value = QualityType.Default;
            SelectedEquipIds.Value = new List<int>();
            CanFragment.Value = false;
            RefreshEquipList();
            CalculateFragmentRewards();
        }

        /// <summary>
        /// 刷新装备列表
        /// </summary>
        public void RefreshEquipList()
        {
            // 获取未佩戴的装备，按品质降序排列
            var query = Global.PlayerEquips.Values.Where(v => v.WearerId == 0);
            if (FilterPart.Value >= 0)
            {
                var partType = (EquipPartType)FilterPart.Value;
                query = query.Where(v => v.PartType == partType);
            }

            if (FilterQuality.Value != QualityType.Default)
            {
                query = query.Where(v => v.Quality == FilterQuality.Value);
            }

            EquipList.Value = query.OrderByDescending(x => x.Quality).ToList();
        }

        private void OnFilterPart(int partValue)
        {
            if (FilterPart.Value == partValue)
            {
                return;
            }

            FilterPart.Value = partValue;
            ClearSelectionAndRefresh();
        }

        private void OnFilterQuality(QualityType quality)
        {
            if (FilterQuality.Value == quality)
            {
                return;
            }

            FilterQuality.Value = quality;
            ClearSelectionAndRefresh();
        }

        private void ClearSelectionAndRefresh()
        {
            SelectedEquipIds.Value = new List<int>();
            CanFragment.Value = false;
            RefreshEquipList();
            CalculateFragmentRewards();
        }

        /// <summary>
        /// 切换装备选择状态
        /// </summary>
        private void OnToggleEquip(int equipId)
        {
            var selectedIds = new List<int>(SelectedEquipIds.Value);
            
            if (selectedIds.Contains(equipId))
            {
                selectedIds.Remove(equipId);
            }
            else
            {
                selectedIds.Add(equipId);
            }
            
            SelectedEquipIds.Value = selectedIds;
            CanFragment.Value = selectedIds.Count > 0;
            
            // 计算分解奖励
            CalculateFragmentRewards();
        }

        /// <summary>
        /// 计算分解奖励
        /// </summary>
        private void CalculateFragmentRewards()
        {
            var rewards = new Dictionary<int, int>();
            
            foreach (var equipId in SelectedEquipIds.Value)
            {
                if (!Global.PlayerEquips.TryGetValue(equipId, out var equip)) continue;
                
                var phaseCfg = mEquipCardConfig.GetOrDefault(equip.EquipId);
                if (phaseCfg == null) continue;
                
                // 基础分解奖励
                foreach (var item in phaseCfg.FragmentItems)
                {
                    if (!rewards.TryGetValue(item.Id, out var currentNum))
                    {
                        currentNum = 0;
                    }
                    rewards[item.Id] = currentNum + RandomTool.RandomNumRange(item.RandomNumRange);
                }
                
                // 词条分解返还
                foreach (var entryId in equip.Entrys)
                {
                    var entryCfg = mEntryConfig.GetOrDefault(entryId);
                    if (entryCfg == null) continue;
                    
                    // foreach (var item in entryCfg.FragmentItems)
                    // {
                    //     if (!rewards.TryGetValue(item.Id, out var currentNum))
                    //     {
                    //         currentNum = 0;
                    //     }
                    //     rewards[item.Id] = currentNum + RandomTool.RandomNumRange(item.RandomNumRange);
                    // }
                }
            }
            
            FragmentRewards.Value = rewards;
        }

        private bool CanExecuteFragment()
        {
            return SelectedEquipIds.Value.Count > 0;
        }

        private async void OnFragment()
        {
            if (SelectedEquipIds.Value.Count <= 0) return;
            
            try
            {
                var result = await mEquipService.FragmentEquipAsync(SelectedEquipIds.Value);
                
                if (result != null && result.Success)
                {
                    // 清空选择
                    SelectedEquipIds.Value = new List<int>();
                    CanFragment.Value = false;
                    
                    // 刷新列表
                    RefreshEquipList();
                    CalculateFragmentRewards();
                    
                    // 触发分解成功事件
                    OnFragmentSuccess?.Invoke(result.Rewards);
                }
                else
                {
                    Toast.Error(result?.Message ?? "分解失败");
                }
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[EquipFragmentPanelViewModel] 分解失败: {ex.Message}");
                Toast.Error($"操作失败：{ex.Message}");
            }
        }

        private void OnClose()
        {
            OnCloseRequested?.Invoke();
        }

        #region 事件
        public System.Action OnCloseRequested;
        public System.Action<List<RewardItem>> OnFragmentSuccess;
        #endregion
    }
}
