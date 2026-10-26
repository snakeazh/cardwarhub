using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;

namespace Game.Battle
{
    /// <summary>
    /// 战斗技能 ViewModel
    /// 管理技能卡牌的显示、等级、冷却等
    /// 使用原界面的 BattleCardGroup 结构
    /// </summary>
    public class BattleSkillsViewModel : ViewModel
    {
        #region 属性
        /// <summary>
        /// 技能卡牌列表 - 直接使用原界面的 BattleCardGroup
        /// </summary>
        public BindableProperty<List<BattleCardGroup>> SkillCards { get; } = 
            new BindableProperty<List<BattleCardGroup>>(new List<BattleCardGroup>());

        /// <summary>
        /// 当前选中的技能
        /// </summary>
        public BindableProperty<int> SelectedSkillId { get; } = new BindableProperty<int>(0);

        /// <summary>
        /// 是否处于拖拽状态
        /// </summary>
        public BindableProperty<bool> IsDragging { get; } = new BindableProperty<bool>(false);
        #endregion

        #region 命令
        public Command<int> OnSelectSkillCommand { get; private set; }
        public Command<int> OnReleaseSkillCommand { get; private set; }
        public Command<int> OnDragSkillCommand { get; private set; }
        public Command OnCancelDragCommand { get; private set; }
        #endregion

        #region 事件
        public System.Action<int> OnSkillSelected;
        public System.Action<int> OnSkillReleased;
        public System.Action<int> OnSkillDragStarted;
        public System.Action OnSkillDragCancelled;
        public System.Action<int, bool> OnSkillUnlockChanged;
        public System.Action<int, float> OnSkillCoolingUpdated;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            // 初始化命令
            OnSelectSkillCommand = new Command<int>(OnSelectSkill);
            OnReleaseSkillCommand = new Command<int>(OnReleaseSkill);
            OnDragSkillCommand = new Command<int>(OnDragSkill);
            OnCancelDragCommand = new Command(OnCancelDrag);

            // 初始化技能数据 - 直接使用 Global.SoldierSkillTeams
            InitializeSkillCards();

            LogTool.Log("[BattleSkillsViewModel] 创建完成");
        }

        /// <summary>
        /// 初始化技能卡牌 - 直接使用原界面的 BattleCardGroup
        /// </summary>
        private void InitializeSkillCards()
        {
            // 直接使用 Global.SoldierSkillTeams 中的 BattleCardGroup
            var skillCards = Global.SoldierSkillTeams
                .Where(t => t.CardType == CardType.Skill)
                .ToList();

            SkillCards.Value = skillCards;

            // 监听每个卡牌的解锁状态变化
            foreach (var card in skillCards)
            {
                card.IsUnlock.RegisterWithInitValue(isUnlock =>
                {
                    OnSkillUnlockChanged?.Invoke(card.CardId, isUnlock);
                });
            }
        }

        /// <summary>
        /// 获取技能卡牌
        /// </summary>
        public BattleCardGroup GetSkillCard(int cardId)
        {
            return SkillCards.Value.FirstOrDefault(c => c.CardId == cardId);
        }

        /// <summary>
        /// 更新技能冷却
        /// </summary>
        public void UpdateSkillCooling(int cardId, float coolingTime)
        {
            OnSkillCoolingUpdated?.Invoke(cardId, coolingTime);
        }

        #region 命令实现

        private void OnSelectSkill(int cardId)
        {
            SelectedSkillId.Value = cardId;
            OnSkillSelected?.Invoke(cardId);
            LogTool.Log($"[BattleSkillsViewModel] 选中技能: {cardId}");
        }

        private void OnReleaseSkill(int cardId)
        {
            OnSkillReleased?.Invoke(cardId);
            LogTool.Log($"[BattleSkillsViewModel] 释放技能: {cardId}");
        }

        private void OnDragSkill(int cardId)
        {
            IsDragging.Value = true;
            OnSkillDragStarted?.Invoke(cardId);
            LogTool.Log($"[BattleSkillsViewModel] 开始拖拽技能: {cardId}");
        }

        private void OnCancelDrag()
        {
            IsDragging.Value = false;
            OnSkillDragCancelled?.Invoke();
            LogTool.Log("[BattleSkillsViewModel] 取消拖拽");
        }

        #endregion
    }
}
