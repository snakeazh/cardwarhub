using System;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Model;
using UIFramework;

namespace Game.Challenge
{
    /// <summary>
    /// 怪物图鉴面板 ViewModel（UIFramework MVVM）
    /// 数据源：TbMonsterBattleConfig.DataList，配合 TbCardBaseConfig 取名字/图标，
    /// 解锁状态取自 Global.PlayerAtlas.GetMonsterAtlasStatus(id)。
    /// </summary>
    public class MonsterAtlasPanelViewModel : ViewModel
    {
        #region 属性
        /// <summary>当前筛选的品质分类（普通/精英/BOSS），变更后 Items 会重新过滤</summary>
        public BindableProperty<MonsterQuality> SelectedQuality { get; } = new BindableProperty<MonsterQuality>(MonsterQuality.Ordinary);

        /// <summary>图鉴条目列表（供 View 的 ScrollView 填充，已按 SelectedQuality 过滤）</summary>
        public BindableProperty<List<MonsterAtlasItemData>> Items { get; } = new BindableProperty<List<MonsterAtlasItemData>>(new List<MonsterAtlasItemData>());

        /// <summary>图鉴解锁进度文案，格式 "解锁怪物 X/Y"，统计全量怪物（不受 Tab 筛选影响）</summary>
        public BindableProperty<string> UnlockProgressText { get; } = new BindableProperty<string>("解锁怪物 0/0");
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command<int> OnItemClickCommand { get; private set; }
        /// <summary>切换品质筛选，参数为目标品质的 int 值（对应 MonsterQuality 枚举）</summary>
        public Command<int> OnSwitchQualityCommand { get; private set; }
        #endregion

        private TbCardBaseConfig _cardConfig;
        // 全量条目（未过滤），过滤结果放入 Items
        private List<MonsterAtlasItemData> _allItems = new List<MonsterAtlasItemData>();

        public override void OnCreate()
        {
            base.OnCreate();

            OnCloseCommand = new Command(OnClose);
            OnItemClickCommand = new Command<int>(OnItemClick);
            OnSwitchQualityCommand = new Command<int>(OnSwitchQuality);

            BuildAllItems();
            ApplyFilter();
            RefreshUnlockProgress();
        }

        /// <summary>
        /// 构建全量图鉴条目。每条把 MonsterBattleConfig 与 CardBaseConfig 联结，
        /// 解锁状态实时取自 Global.PlayerAtlas。
        /// </summary>
        private void BuildAllItems()
        {
            _cardConfig = DataConfigManager.Tables.TbCardBaseConfig;
            var monsterList = DataConfigManager.Tables.TbMonsterBattleConfig.DataList;

            _allItems = new List<MonsterAtlasItemData>();
            foreach (var monsterCfg in monsterList)
            {
                var cardCfg = _cardConfig.GetOrDefault(monsterCfg.Id);
                _allItems.Add(new MonsterAtlasItemData
                {
                    MonsterId = monsterCfg.Id,
                    Quality = monsterCfg.MonsterQuality,
                    Name = cardCfg != null ? cardCfg.Name : null,
                    Icon = cardCfg != null ? cardCfg.Icon : null,
                    Status = Global.PlayerAtlas.GetMonsterAtlasStatus(monsterCfg.Id),
                    Label = monsterCfg.Label,
                });
            }
        }

        /// <summary>按当前 SelectedQuality 过滤全量条目，结果写入 Items</summary>
        private void ApplyFilter()
        {
            var quality = SelectedQuality.Value;
            var filtered = new List<MonsterAtlasItemData>();
            foreach (var item in _allItems)
            {
                if (item.Quality == quality) filtered.Add(item);
            }
            Items.Value = filtered;
        }

        private void OnSwitchQuality(int qualityValue)
        {
            var quality = (MonsterQuality)qualityValue;
            if (quality == SelectedQuality.Value) return;
            SelectedQuality.Value = quality;
            ApplyFilter();
            OnItemsChanged?.Invoke();
        }

        /// <summary>
        /// 刷新列表的解锁/红点状态。DetailPanel 领奖后通过事件通知本面板调用。
        /// 更新全量条目状态后重新按当前筛选过滤，再通知 View 重绘。
        /// </summary>
        public void RefreshStatus()
        {
            if (_allItems == null) return;
            foreach (var item in _allItems)
            {
                item.Status = Global.PlayerAtlas.GetMonsterAtlasStatus(item.MonsterId);
            }
            ApplyFilter();
            RefreshUnlockProgress();
            OnItemsChanged?.Invoke();
        }

        /// <summary>
        /// 统计全量怪物解锁进度并更新文案。解锁定义：Status != RewardStatus.Default（已收集）。
        /// </summary>
        private void RefreshUnlockProgress()
        {
            if (_allItems == null)
            {
                UnlockProgressText.Value = "解锁怪物 0/0";
                return;
            }

            int total = _allItems.Count;
            int unlocked = 0;
            foreach (var item in _allItems)
            {
                if (item.Status != RewardStatus.Default) unlocked++;
            }
            UnlockProgressText.Value = "解锁怪物 " + unlocked + "/" + total;
        }

        private void OnItemClick(int monsterId)
        {
            // 解锁状态判定放在 View 的点击处（数据刷新更及时），这里只负责转发意图
            OnOpenDetailRequested?.Invoke(monsterId);
        }

        private void OnClose()
        {
            OnClosePanel?.Invoke();
        }

        #region 事件
        public Action OnClosePanel;
        /// <summary>点击图鉴条目，参数为 monsterId，由 View 判定解锁后打开详情面板</summary>
        public Action<int> OnOpenDetailRequested;
        /// <summary>列表状态变化（领奖后红点刷新），通知 View 重绘 ScrollView</summary>
        public Action OnItemsChanged;
        #endregion
    }

    /// <summary>图鉴条目数据</summary>
    public class MonsterAtlasItemData
    {
        public int MonsterId;
        public MonsterQuality Quality;
        public string Name;
        public string Icon;
        public RewardStatus Status;
        /// <summary>怪物类型标签（飞行怪/远程怪等），来自 MonsterBattleConfig.Label，空则不显示标签</summary>
        public string Label;
    }
}
