using UIFramework.Services;
using Cysharp.Threading.Tasks;
using FusionContinent;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using Config;

namespace Game.Battle
{
    /// <summary>
    /// 战斗技能管理服务
    /// 管理技能卡牌的自动释放逻辑
    /// </summary>
    public class BattleSkillService : GameServiceBase
    {
        #region 属性
        /// <summary>
        /// 技能卡牌列表
        /// </summary>
        private List<BattleCardGroup> _skillCards = new List<BattleCardGroup>();

        /// <summary>
        /// 当前自动释放状态
        /// </summary>
        public bool IsAutoReleaseEnabled { get; private set; }

        /// <summary>
        /// 旋转动画 Tween
        /// </summary>
        private Tween _rotateTween;

        /// <summary>
        /// AutoToggle 的 Image Transform
        /// </summary>
        private Transform _autoToggleImageTransform;
        #endregion

        #region 依赖
        private BattleTimeViewModel _timeViewModel;
        private GameLand _gameLand;
        #endregion

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            
            // 初始化技能卡牌列表
            _skillCards = Global.SoldierSkillTeams.Where(t => t.CardType == CardType.Skill).ToList();
            
            LogTool.Log($"[BattleSkillService] 初始化完成，技能卡牌数量: {_skillCards.Count}");
        }

        /// <summary>
        /// 设置 AutoToggle 的 Image Transform（用于旋转动画）
        /// </summary>
        public void SetAutoToggleImage(Transform imageTransform)
        {
            _autoToggleImageTransform = imageTransform;
            InitializeRotateTween();
        }

        /// <summary>
        /// 绑定 ViewModel
        /// </summary>
        public void BindViewModel(BattleTimeViewModel viewModel)
        {
            _timeViewModel = viewModel;
            
            // 订阅自动释放技能状态变化
            _timeViewModel.OnAutoReleaseSkillStateChanged += OnAutoReleaseSkillStateChanged;
            
            // 初始化状态
            if (_timeViewModel.IsAutoReleaseSkill.Value)
            {
                // OnAutoReleaseSkillStateChanged(true);
            }

            OnAutoReleaseSkillStateChanged(Global.AutoReleaseSkillState.Value);
        }

        /// <summary>
        /// 解绑 ViewModel
        /// </summary>
        public void UnbindViewModel()
        {
            if (_timeViewModel != null)
            {
                _timeViewModel.OnAutoReleaseSkillStateChanged -= OnAutoReleaseSkillStateChanged;
                _timeViewModel = null;
            }
        }

        #region 自动释放技能逻辑

        /// <summary>
        /// 初始化旋转动画
        /// </summary>
        private void InitializeRotateTween()
        {
            if (_autoToggleImageTransform == null) return;

            LogTool.Log("[BattleSkillService] 旋转动画初始化完成");
        }

        /// <summary>
        /// 自动释放技能状态变化回调
        /// </summary>
        private void OnAutoReleaseSkillStateChanged(bool isOn)
        {
            IsAutoReleaseEnabled = isOn;

            // if (isOn)
            // {
            //
            // }
            // else
            // {
            //     if (_rotateTween != null)
            //     {
            //         _rotateTween.Kill();
            //         _rotateTween = null;
            //     }
            // }

            // 更新所有技能卡牌状态
            UpdateSkillCardsState(isOn);

            LogTool.Log($"[BattleSkillService] 自动释放技能状态变化: {isOn}");
        }

        /// <summary>
        /// 更新技能卡牌状态
        /// </summary>
        private void UpdateSkillCardsState(bool isAutoRelease)
        {
            _gameLand = GameLand.Default;
            if (_gameLand == null)
            {
                LogTool.LogWarning("[BattleSkillService] GameLand 为空，无法更新技能卡牌状态");
                return;
            }

            var cardSlot = _gameLand.CardSlot;
            if (cardSlot == null)
            {
                LogTool.LogWarning("[BattleSkillService] CardSlot 为空");
                return;
            }

            for (int i = 0; i < _skillCards.Count; i++)
            {
                var card = _skillCards[i];
                if (card.CardId == 0 || card.CardType != CardType.Skill) continue;
                if (i >= cardSlot.childCount) continue;

                var cardInfo = cardSlot.transform.GetChild(i);
                if (cardInfo == null) continue;

                var cardSlotLongClick = cardInfo.GetComponent<CardSlotLongClick>();
                if (cardSlotLongClick == null) continue;

                // 获取卡牌配置
                var cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
                var specialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;
                var cardCfg = cardBaseConfig.GetOrDefault(card.CardId);
                var specialCardCfg = specialEffectsConfig.GetOrDefault(cardCfg.Id);

                if (isAutoRelease)
                {
                    // 开启自动释放：禁用拖拽
                    cardSlotLongClick.dragState = DragState.DragFalse;
                    
                    // 延迟关闭特效
                    DOTween.To(v => { }, 0, 0, 0.5f).onComplete += () =>
                    {
                        if (cardInfo != null)
                        {
                            var canvas = cardInfo.transform.Find("Canvas");
                            if (canvas != null)
                            {
                                var effect1 = canvas.Find("kapaizhunbei_02");
                                var effect2 = canvas.Find("kapaizhunbei_01");
                                if (effect1 != null) effect1.localScale = Vector3.zero;
                                if (effect2 != null) effect2.localScale = Vector3.zero;
                            }
                        }
                    };
                }
                else
                {
                    // 关闭自动释放：恢复拖拽（如果已解锁且不是被动技能）
                    if (card.IsUnlock.Value && specialCardCfg.PassiveSkill == 0)
                    {
                        cardSlotLongClick.dragState = DragState.GamePlay;
                    }
                }
            }
        }

        /// <summary>
        /// 强制更新所有技能卡牌到自动释放状态
        /// 用于战斗开始时初始化
        /// </summary>
        public void ForceUpdateToAutoReleaseState()
        {
            if (_timeViewModel != null && _timeViewModel.IsAutoReleaseSkill.Value)
            {
                UpdateSkillCardsState(true);
            }
        }

        /// <summary>
        /// 手动点击可用技能卡时取消自动战斗
        /// </summary>
        public void CancelAutoReleaseFromManualSkill()
        {
            if (_timeViewModel != null)
            {
                _timeViewModel.SetAutoReleaseSkillState(false);
                return;
            }

            Global.AutoReleaseSkillState.Value = false;
            PlayerPrefs.SetString("AutoReleaseSkillState", "false");
            OnAutoReleaseSkillStateChanged(false);
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            
            UnbindViewModel();
            
            // 清理旋转动画
            if (_rotateTween != null)
            {
                _rotateTween.Kill();
                _rotateTween = null;
            }
            
            _skillCards.Clear();
            _autoToggleImageTransform = null;
            
            LogTool.Log("[BattleSkillService] 已销毁清理");
        }
    }
}
