using Game.Soldier;
using Game.Soldier.RedDot;
using Game.RedDot;
using Game.RedDot.Rules;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;
using UIFramework;
using Config;
using FusionContinent.Model;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FusionContinent.Helper;

namespace FusionContinent
{
    /// <summary>
    /// 觉醒界面 - 新框架版本 (MVVM)
    /// 复用原 AwakeningPanel 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("AwakeningPanel")]
    public class AwakeningPanelNew : UIFramework.UIPanel<AwakeningPanelViewModel>
    {
        // 引用原 Panel，复用其 UI 组件
        private AwakeningPanel mOldPanel;
        // 消耗材料模板（ConsumeItems 下提前放置的 RewardEntity）
        private GameObject mConsumeTemplate;
        private readonly SoldierPanelRedDotScope _redDotScope = new SoldierPanelRedDotScope();

        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<AwakeningPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[AwakeningPanelNew] 找不到 AwakeningPanel 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            // 缓存消耗材料模板（ConsumeItems 下的第一个子物体），并隐藏
            if (mOldPanel.ConsumeItems != null && mOldPanel.ConsumeItems.transform.childCount > 0)
            {
                mConsumeTemplate = mOldPanel.ConsumeItems.transform.GetChild(0).gameObject;
                mConsumeTemplate.SetActive(false);
            }

            LogTool.Log("[AwakeningPanelNew] 已关联 AwakeningPanel");
        }

        protected override void OnBindViewModel(AwakeningPanelViewModel vm)
        {
            LogTool.Log($"[AwakeningPanelNew] OnBindViewModel: mOldPanel={mOldPanel != null}, VM={vm != null}");
            
            if (mOldPanel == null)
            {
                LogTool.LogError("[AwakeningPanelNew] OnBindViewModel: mOldPanel 为空！");
                return;
            }

            // 绑定属性
            BindProperties(vm);
            
            // 绑定 ViewModel 事件
            vm.OnAwakeningSuccess += OnAwakeningSuccess;
            vm.OnAwakeningFailed += OnAwakeningFailed;
            vm.OnCloseRequested += OnCloseRequested;
            vm.OnShowSkillDetailRequested += OnShowSkillDetailRequested;

            // 与 SoldierPanel 共享局部红点树
            _redDotScope.OpenShared(SoldierRedDotHelper.GetSoldierCardIds());

            // 初始化按钮
            InitButtons(vm);
            
            LogTool.Log("[AwakeningPanelNew] OnBindViewModel: 完成");
        }

        protected override void OnUnbindViewModel(AwakeningPanelViewModel vm)
        {
            // 取消事件订阅
            vm.OnAwakeningSuccess -= OnAwakeningSuccess;
            vm.OnAwakeningFailed -= OnAwakeningFailed;
            vm.OnCloseRequested -= OnCloseRequested;
            vm.OnShowSkillDetailRequested -= OnShowSkillDetailRequested;

            _redDotScope.CloseShared();
            
            // 清理材料
            RecycleMaterials();
        }

        #region 属性绑定

        private void BindProperties(AwakeningPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 觉醒状态
            vm.IsAwakened.OnValueChanged += (oldVal, newVal) =>
            {
                if (newVal)
                {
                    SetAsAwakened(vm);
                }
            };

            // 是否可以觉醒
            vm.CanAwaken.OnValueChanged += (oldVal, newVal) =>
            {
                if (mOldPanel.AwakeningBtn != null)
                    mOldPanel.AwakeningBtn.interactable = newVal;
            };

            // 技能配置变化
            vm.SkillConfig.OnValueChanged += (oldVal, newVal) =>
            {
                if (newVal != null)
                {
                    SetSkillDisplay(newVal, vm);
                }
            };

            // 消耗材料变化
            vm.ConsumeItems.OnValueChanged += (oldVal, newVal) =>
            {
                if (newVal != null)
                {
                    SetConsumeMaterials(newVal, vm);
                }
            };

            // 英灵本体配置变化（显示角色描述）
            vm.HeroConfig.OnValueChanged += (oldVal, newVal) =>
            {
                if (newVal != null && mOldPanel.BackGroudDesText != null)
                    mOldPanel.BackGroudDesText.text = newVal.Desc;
            };
        }

        #endregion

        #region 初始化按钮

        private void InitButtons(AwakeningPanelViewModel vm)
        {
            if (mOldPanel == null) 
            {
                LogTool.LogError("[AwakeningPanelNew] InitButtons: mOldPanel 为空！");
                return;
            }

            LogTool.Log($"[AwakeningPanelNew] InitButtons: AwakeningBtn={mOldPanel.AwakeningBtn != null}, Closebtn={mOldPanel.Closebtn != null}");

            // 觉醒按钮
            if (mOldPanel.AwakeningBtn != null)
            {
                mOldPanel.AwakeningBtn.onClick.RemoveAllListeners();
                mOldPanel.AwakeningBtn.onClick.AddListener(() => 
                {
                    LogTool.Log("[AwakeningPanelNew] 觉醒按钮被点击");
                    vm.OnAwakeningCommand.Execute();
                });
            }
            else
            {
                LogTool.LogWarning("[AwakeningPanelNew] InitButtons: AwakeningBtn 为空！");
            }

            // 关闭按钮
            if (mOldPanel.Closebtn != null)
            {
                mOldPanel.Closebtn.onClick.RemoveAllListeners();
                mOldPanel.Closebtn.onClick.AddListener(() => 
                {
                    LogTool.Log("[AwakeningPanelNew] 关闭按钮被点击");
                    vm.OnCloseCommand.Execute();
                });
            }
            else
            {
                LogTool.LogWarning("[AwakeningPanelNew] InitButtons: Closebtn 为空！");
            }
        }

        #endregion

        #region UI 设置

        /// <summary>
        /// 设置技能显示
        /// </summary>
        private void SetSkillDisplay(CardBaseConfig skillConfig, AwakeningPanelViewModel vm)
        {
            if (mOldPanel == null || skillConfig == null) return;

            // 觉醒技能名称
            if (mOldPanel.AwakeSkillNameText != null)
                mOldPanel.AwakeSkillNameText.text = skillConfig.Name;
            // 觉醒技能简介
            if (mOldPanel.AwakeSkillDesText != null)
                mOldPanel.AwakeSkillDesText.text = skillConfig.Desc;

            // 觉醒技能图标（SkillItem 节点上挂了 SkillItemView，取其 SkillIcon 赋值）
            if (mOldPanel.SkillItem != null)
            {
                var skillItemView = mOldPanel.SkillItem.GetComponent<SkillItemView>();
                if (skillItemView != null && skillItemView.SkillIcon != null)
                {
                    skillItemView.SkillIcon.sprite = Global.CardIconSpriteAtlas.GetSpriteExt(skillConfig.Icon);
                    skillItemView.SkillIcon.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// 设置消耗材料
        /// </summary>
        private void SetConsumeMaterials(List<RewardAndNum> consumeItems, AwakeningPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 先清空旧材料
            RecycleMaterials();

            if (mConsumeTemplate == null)
            {
                LogTool.LogError("[AwakeningPanelNew] SetConsumeMaterials: 消耗材料模板为空！请在 ConsumeItems 下放置一个 RewardEntity 作为模板");
                return;
            }

            bool isBuy = true;
            var parent = mOldPanel.ConsumeItems.transform;

            // 以模板克隆生成材料
            for (int i = 0; i < consumeItems.Count; i++)
            {
                var rewardObj = Instantiate(mConsumeTemplate, parent);
                rewardObj.SetActive(true);
                bool isReward = rewardObj.GetComponent<RewardEntity>()
                    .SetData(consumeItems[i].Id, consumeItems[i].Num, RewardStyleType.Consume);

                if (!isReward) isBuy = false;
            }

            // 更新按钮状态
            if (mOldPanel.AwakeningBtn != null)
            {
                mOldPanel.AwakeningBtn.interactable = vm.CanAwaken.Value && isBuy;
            }
        }

        /// <summary>
        /// 设置为已觉醒状态
        /// </summary>
        private void SetAsAwakened(AwakeningPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 清理材料（已觉醒无需再显示消耗）
            RecycleMaterials();

            // 隐藏觉醒按钮和消耗区（已觉醒不可再操作）
            if (mOldPanel.AwakeningBtn != null)
                mOldPanel.AwakeningBtn.gameObject.SetActive(false);
            if (mOldPanel.ConsumeItems != null)
                mOldPanel.ConsumeItems.gameObject.SetActive(false);

            // 仍需显示觉醒技能信息
            if (vm.SkillConfig.Value != null)
                SetSkillDisplay(vm.SkillConfig.Value, vm);
        }

        /// <summary>
        /// 回收材料：销毁 ConsumeItems 下所有克隆体，保留模板
        /// </summary>
        private void RecycleMaterials()
        {
            if (mOldPanel == null || mOldPanel.ConsumeItems == null) return;

            var parent = mOldPanel.ConsumeItems.transform;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.gameObject != mConsumeTemplate)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        #endregion

        #region 事件处理

        private void OnAwakeningSuccess(int skinId)
        {
            // 刷新 UI 状态（显示已觉醒状态）
            RefreshUIState();
            _redDotScope.NotifyAwakeningDataChanged();
            
            // 完成引导
            // GuideFlow.Instance.SetStepComplete(GuideType.HeroAwakening);

            // 显示战力提示
            CombatTipController.Instance.SetCombat();
            
            // 通知外部（通过 Global.Publisher 事件分发器）
            Global.Publisher.Publish(MessagePublisher.CARD_SKIN_CHANGED, skinId);
        }

        private void OnAwakeningFailed(string errorMessage)
        {
            FusionContinent.Toast.Show(errorMessage);
        }

        private void OnCloseRequested()
        {
            // 使用新框架 API 关闭界面
            UIFramework.UIManager.ClosePanel(this);
        }

        private void OnShowSkillDetailRequested(int skillId)
        {
            // 使用MVVM架构打开技能升级面板
            var skillUpgradeVm = new Game.Skill.SkillUpGradePanelViewModel();
            skillUpgradeVm.OnCreate();
            skillUpgradeVm.SetData(skillId);
            UIFramework.UIManager.OpenPanelAsync<SkillUpGradePanelNew, Game.Skill.SkillUpGradePanelViewModel>(skillUpgradeVm, UIFramework.UILevel.PopUI).Forget();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置数据（供外部调用）
        /// </summary>
        public void SetAwakening(int cardId, int cardLv)
        {
            LogTool.Log($"[AwakeningPanelNew] SetAwakening: cardId={cardId}, cardLv={cardLv}");
            
            if (VM != null)
            {
                // 初始化数据
                VM.Initialize(cardId, cardLv);
            }
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            LogTool.Log($"[AwakeningPanelNew] OnOpen: VM={VM != null}, CardId={VM?.CardId.Value}");
            
            // 在 OnOpen 时初始化 UI 显示
            if (VM != null && VM.CardId.Value > 0)
            {
                // 根据当前状态刷新 UI
                RefreshUIState();
                BindAwakeningBtnRedDot(VM.CardId.Value);
            }
            else
            {
                LogTool.LogWarning("[AwakeningPanelNew] OnOpen: CardId 为 0，可能还没有调用 SetAwakening");
            }
        }

        void BindAwakeningBtnRedDot(int cardId)
        {
            if (mOldPanel?.AwakeningBtn == null || !_redDotScope.IsOpen || cardId <= 0)
            {
                return;
            }

            mOldPanel.AwakeningBtn.BindLocalRedDot(
                _redDotScope,
                SoldierPanelRedDotScope.Paths.CardAwakening(cardId));
        }

        /// <summary>
        /// 刷新所有 UI 状态（用于初始化和状态变更）
        /// </summary>
        private void RefreshUIState()
        {
            if (VM == null || mOldPanel == null) return;

            LogTool.Log($"[AwakeningPanelNew] RefreshUIState: IsAwakened={VM.IsAwakened.Value}, CanAwaken={VM.CanAwaken.Value}");

            // 刷新觉醒按钮状态
            if (mOldPanel.AwakeningBtn != null)
                mOldPanel.AwakeningBtn.interactable = VM.CanAwaken.Value;

            // 角色描述：HeroConfig 在 Initialize 里已赋值，早于 OnValueChanged 订阅，这里主动刷新一次
            if (VM.HeroConfig.Value != null && mOldPanel.BackGroudDesText != null)
                mOldPanel.BackGroudDesText.text = VM.HeroConfig.Value.Desc;

            // 觉醒标记：已觉醒显示，未觉醒隐藏
            if (mOldPanel.IsAwaken != null)
                mOldPanel.IsAwaken.gameObject.SetActive(VM.IsAwakened.Value);

            // 根据觉醒状态显示不同内容
            if (VM.IsAwakened.Value)
            {
                // 已觉醒状态
                SetAsAwakened(VM);
            }
            else if (VM.SkillConfig.Value != null)
            {
                // 显示觉醒内容（可觉醒或预览）
                if (mOldPanel.AwakeningBtn != null)
                    mOldPanel.AwakeningBtn.gameObject.SetActive(true);
                if (mOldPanel.ConsumeItems != null)
                    mOldPanel.ConsumeItems.gameObject.SetActive(true);
                SetSkillDisplay(VM.SkillConfig.Value, VM);
                if (VM.ConsumeItems.Value != null)
                    SetConsumeMaterials(VM.ConsumeItems.Value, VM);
            }
            else
            {
                // 暂无觉醒状态 - 隐藏觉醒相关内容
                if (mOldPanel.AwakeningBtn != null)
                    mOldPanel.AwakeningBtn.gameObject.SetActive(false);
                if (mOldPanel.ConsumeItems != null)
                    mOldPanel.ConsumeItems.gameObject.SetActive(false);
            }
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
            RecycleMaterials();
        }

        protected override void OnClose()
        {
            RecycleMaterials();
        }

        #endregion
    }
}
