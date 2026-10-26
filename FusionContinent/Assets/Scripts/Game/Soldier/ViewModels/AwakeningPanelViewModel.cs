using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using Cysharp.Threading.Tasks;
using FusionContinent;
using UIFramework.Services;

namespace Game.Soldier
{
    /// <summary>
    /// 觉醒界面 ViewModel
    /// </summary>
    public class AwakeningPanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<int> CardId { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> CardLv { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> SkinId { get; } = new BindableProperty<int>(0);
        public BindableProperty<bool> IsAwakened { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> CanAwaken { get; } = new BindableProperty<bool>(false);
        public BindableProperty<string> TipsText { get; } = new BindableProperty<string>("");
        public BindableProperty<List<RewardAndNum>> ConsumeItems { get; } = new BindableProperty<List<RewardAndNum>>();
        public BindableProperty<CardBaseConfig> SkillConfig { get; } = new BindableProperty<CardBaseConfig>();
        /// <summary>英灵本体配置（用于显示角色描述等）</summary>
        public BindableProperty<CardBaseConfig> HeroConfig { get; } = new BindableProperty<CardBaseConfig>();
        #endregion

        #region 命令
        public Command OnAwakeningCommand { get; private set; }
        public Command OnCloseCommand { get; private set; }
        public Command<int> OnShowSkillDetailCommand { get; private set; }
        #endregion

        #region 依赖
        private SoldierService mSoldierService;
        #endregion

        #region 配置数据
        private TbCardDevelopConfig mCardDevelopConfig;
        private TbCardBaseConfig mCardBaseConfig;
        private TbSpecialEffectsConfig mSpecialEffectsConfig;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mSoldierService = GameServiceManager.Get<SoldierService>();
            
            // 加载配置
            mCardDevelopConfig = DataConfigManager.Tables.TbCardDevelopConfig;
            mCardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            mSpecialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;
            
            // 初始化命令
            OnAwakeningCommand = new Command(OnAwakening, CanExecuteAwakening);
            OnCloseCommand = new Command(OnClose);
            OnShowSkillDetailCommand = new Command<int>(OnShowSkillDetail);
            
            LogTool.Log("[AwakeningPanelViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogTool.Log("[AwakeningPanelViewModel] 销毁");
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        public void Initialize(int cardId, int cardLv)
        {
            CardId.Value = cardId;
            CardLv.Value = cardLv;

            // 英灵本体配置（用于显示角色描述）
            HeroConfig.Value = mCardBaseConfig.GetOrDefault(cardId);

            // 获取卡牌数据
            var cardInfo = Global.GetCardById(cardId);
            if (cardInfo != null)
            {
                SkinId.Value = cardInfo.ObjData.Skin;
            }

            // 检查觉醒状态
            CheckAwakeningStatus();
        }

        #region 命令实现

        private async void OnAwakening()
        {
            if (!CanExecuteAwakening()) return;
            
            var result = await mSoldierService.AwakeningSoldierAsync(CardId.Value, CardLv.Value);
            if (result.Success)
            {
                // 更新数据
                var cardInfo = Global.GetCardById(CardId.Value);
                if (cardInfo != null)
                {
                    SkinId.Value = cardInfo.ObjData.Skin;
                    CardLv.Value = cardInfo.Lv;
                    IsAwakened.Value = cardInfo.ObjData.IsAwaked;
                }

                CanAwaken.Value = false;

                
                // 触发觉醒成功事件（View和ViewModel之间使用event）
                OnAwakeningSuccess?.Invoke(SkinId.Value);
            
            // 跨界面通知使用事件分发器
            Global.Publisher.Publish(MessagePublisher.CARD_SKIN_CHANGED, SkinId.Value);
                
                LogTool.Log($"[AwakeningPanelViewModel] 觉醒成功，CardId={CardId.Value}");
            }
            else
            {
                // 触发觉醒失败事件（View和ViewModel之间使用event）
                OnAwakeningFailed?.Invoke(result.ErrorMessage);
            }
        }

        private bool CanExecuteAwakening()
        {
            if (!CanAwaken.Value || IsAwakened.Value) return false;

            // 检查材料是否足够
            if (ConsumeItems.Value != null)
            {
                foreach (var consumeItem in ConsumeItems.Value)
                {
                    long hasNum = Global.GetItemNumById(consumeItem.Id);
                    if (hasNum < consumeItem.Num)
                    {
                        // 材料不足
                        Toast.Warning("材料或等级不够");
                        return false;
                    }
                }
            }

            return true;
        }

        private void OnClose()
        {
            // 触发关闭界面事件（View和ViewModel之间使用event）
            OnCloseRequested?.Invoke();
        }

        private void OnShowSkillDetail(int skillId)
        {
            // 触发显示技能详情事件（View和ViewModel之间使用event）
            OnShowSkillDetailRequested?.Invoke(skillId);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 检查觉醒状态
        /// </summary>
        public void CheckAwakeningStatus()
        {
            var levelPropertyList = mCardDevelopConfig.GetOrDefault(CardId.Value)?.LevelPropertyList;
            if (levelPropertyList == null || levelPropertyList.Count == 0)
            {
                // 无配表/无觉醒档
                IsAwakened.Value = false;
                CanAwaken.Value = false;
                TipsText.Value = "暂无觉醒";
                Toast.Show("暂无觉醒");
                OnCloseRequested?.Invoke();
                return;
            }

            var cardInfo = Global.GetCardById(CardId.Value);
            IsAwakened.Value = cardInfo != null && cardInfo.ObjData != null && cardInfo.ObjData.IsAwaked;

            // 找到最近的觉醒点：>= 当前等级 中 Level 最小的那一档
            var awakeningLevel = levelPropertyList
                .Where(x => x.Level >= CardLv.Value && x.AwakeningConsume.Any())
                .OrderBy(x => x.Level)
                .FirstOrDefault();

            if (awakeningLevel == null)
            {
                // 没有可觉醒档（可能是已觉醒到满级）
                CanAwaken.Value = false;
                TipsText.Value = IsAwakened.Value ? "英灵已觉醒" : "暂无觉醒";
                // 已觉醒时仍需显示觉醒技能信息
                if (IsAwakened.Value)
                {
                    SetupAwakeningSkill();
                }
                return;
            }

            // 设置觉醒数据
            SetupAwakeningData(awakeningLevel);
        }

        /// <summary>
        /// 设置觉醒数据
        /// </summary>
        private void SetupAwakeningData(DevelopLevelPropertyList awakeningLevel)
        {
            // 觉醒技能（与等级无关，无论是否已觉醒都应显示）
            SetupAwakeningSkill();

            // 消耗材料
            ConsumeItems.Value = awakeningLevel.AwakeningConsume;

            // 检查是否可以觉醒
            CanAwaken.Value = !IsAwakened.Value && CardLv.Value == awakeningLevel.Level;
            TipsText.Value = "英灵满级后可进行觉醒";
        }

        /// <summary>
        /// 设置觉醒技能：从 SpecialEffectsConfig 按「作用于该英灵 + 觉醒技能」直接查
        /// </summary>
        private void SetupAwakeningSkill()
        {
            var awakingSkill = mSpecialEffectsConfig.DataList.FirstOrDefault(
                x => x.ActsOnCard == CardId.Value && x.AwakingSkill == 1);
            if (awakingSkill != null)
            {
                SkillConfig.Value = mCardBaseConfig.GetOrDefault(awakingSkill.Id);
            }
        }

        /// <summary>
        /// 获取已觉醒的皮肤ID（来自服务端卡牌数据）
        /// </summary>
        public int GetAwakenedSkinId()
        {
            var cardInfo = Global.GetCardById(CardId.Value);
            if (cardInfo == null || cardInfo.ObjData == null || !cardInfo.ObjData.IsAwaked)
            {
                return 0;
            }

            return cardInfo.ObjData.Skin;
        }

        #endregion

        #region 事件
        public System.Action<int> OnAwakeningSuccess;
        public System.Action<string> OnAwakeningFailed;
        public System.Action OnCloseRequested;
        public System.Action<int> OnShowSkillDetailRequested;
        #endregion
    }
}
