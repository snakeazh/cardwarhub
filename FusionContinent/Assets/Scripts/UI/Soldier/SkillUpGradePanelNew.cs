using System;
using System.Collections.Generic;
using Assets.Scripts.Localization;
using Cysharp.Threading.Tasks;
using FusionContinent.Animation;
using Game.Skill;
using TMPro;
using UIFramework;
using UnityEngine;
using UnityEngine.Events;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    /// <summary>
    /// 技能详情面板（原技能升级面板，MVVM迁移版本）
    /// 继承UIPanel<SkillUpGradePanelViewModel>，实现数据驱动
    /// 产品改动：技能升级已移除，技能等级跟随英灵等级同步提升，本面板只读展示 + 穿戴/卸下
    /// </summary>
    [PrefabPath("SkillUpGradePanel")]
    public class SkillUpGradePanelNew : UIPanel<SkillUpGradePanelViewModel>
    {
        #region UI组件引用

        private SkillUpGradePanel mOldPanel;
        private readonly Dictionary<int, LevelUpAnimation.State> mLevelStateCache = new Dictionary<int, LevelUpAnimation.State>();

        #endregion

        #region 生命周期

        void Awake()
        {
            // 获取原Panel组件引用
            mOldPanel = GetComponent<SkillUpGradePanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[SkillUpGradePanelNew] 找不到 SkillUpGradePanel 组件！请确保 Prefab 上同时有 SkillUpGradePanel 和 SkillUpGradePanelNew 组件。");
                return;
            }

            // 技能升级已移除（技能等级跟随英灵等级）：隐藏旧面板的升级区 UI，不改 prefab
            mOldPanel.ConfirmUpGradeBtn.gameObject.SetActive(false);
            mOldPanel.Consumes.gameObject.SetActive(false);
        }

        protected override void OnBindViewModel(SkillUpGradePanelViewModel vm)
        {
            if (mOldPanel == null || mOldPanel.QuitBtn == null)
            {
                LogTool.LogError("[SkillUpGradePanelNew] UI组件未初始化，无法绑定 ViewModel。");
                return;
            }

            // 1. 绑定关闭按钮
            mOldPanel.QuitBtn.onClick.AddListener(OnQuitButtonClicked);

            // 4. 绑定技能名称
            vm.SkillName.OnValueChanged += OnSkillNameChanged;
            // 初始化技能名称显示
            OnSkillNameChanged("", vm.SkillName.Value);

            // 5. 绑定卡牌等级文本
            vm.CardLevelText.OnValueChanged += OnCardLevelTextChanged;
            // 初始化卡牌等级显示
            OnCardLevelTextChanged("", vm.CardLevelText.Value);

            // 6. 绑定技能描述
            vm.SkillDesc.OnValueChanged += OnSkillDescChanged;
            // 初始化技能描述显示
            OnSkillDescChanged("", vm.SkillDesc.Value);

            // 7. 绑定基础攻击力
            vm.BaseAttackValue.OnValueChanged += OnBaseAttackValueChanged;
            vm.BaseAttackDiff.OnValueChanged += OnBaseAttackDiffChanged;
            vm.ShowBaseAttackDiff.OnValueChanged += OnShowBaseAttackDiffChanged;
            // 初始化基础攻击力显示
            OnBaseAttackValueChanged("", vm.BaseAttackValue.Value);
            OnShowBaseAttackDiffChanged(false, vm.ShowBaseAttackDiff.Value);
            OnBaseAttackDiffChanged("", vm.BaseAttackDiff.Value);

            // 8. 绑定伤害类型
            vm.DamageTypeName.OnValueChanged += OnDamageTypeNameChanged;
            // 初始化伤害类型显示
            OnDamageTypeNameChanged("", vm.DamageTypeName.Value);

            // 9. 绑定继承伤害
            vm.InheritDamageValue.OnValueChanged += OnInheritDamageValueChanged;
            vm.InheritDamageColor.OnValueChanged += OnInheritDamageColorChanged;
            vm.InheritDamageMaterial.OnValueChanged += OnInheritDamageMaterialChanged;
            vm.InheritDamageDiff.OnValueChanged += OnInheritDamageDiffChanged;
            vm.ShowInheritDamageDiff.OnValueChanged += OnShowInheritDamageDiffChanged;
            // 初始化继承伤害显示
            OnInheritDamageValueChanged("", vm.InheritDamageValue.Value);
            OnInheritDamageColorChanged(Color.white, vm.InheritDamageColor.Value);
            OnInheritDamageMaterialChanged(null, vm.InheritDamageMaterial.Value);
            OnShowInheritDamageDiffChanged(false, vm.ShowInheritDamageDiff.Value);
            OnInheritDamageDiffChanged("", vm.InheritDamageDiff.Value);

            // 10. 绑定冷却时间
            vm.CooldownValue.OnValueChanged += OnCooldownValueChanged;
            vm.CooldownDiff.OnValueChanged += OnCooldownDiffChanged;
            vm.ShowCooldownDiff.OnValueChanged += OnShowCooldownDiffChanged;
            // 初始化冷却时间显示
            OnCooldownValueChanged("", vm.CooldownValue.Value);
            OnShowCooldownDiffChanged(false, vm.ShowCooldownDiff.Value);
            OnCooldownDiffChanged("", vm.CooldownDiff.Value);

            // 14. 绑定升品描述（刷新由 OnOpen / LevelDescs 变更驱动，避免 Bind 与 OnOpen 重复刷动画）
            vm.LevelDescs.OnValueChanged += OnLevelDescsChanged;

            // 15. 绑定卡牌槽位数据
            vm.CardSlotData.OnValueChanged += OnCardSlotDataChanged;
            // 初始化卡牌槽位显示
            OnCardSlotDataChanged(null, vm.CardSlotData.Value);
        }

        protected override void OnUnbindViewModel(SkillUpGradePanelViewModel vm)
        {
            // 移除按钮监听
            if (mOldPanel != null && mOldPanel.QuitBtn != null)
            {
                mOldPanel.QuitBtn.onClick.RemoveListener(OnQuitButtonClicked);
            }

            // 移除属性监听
            vm.SkillName.OnValueChanged -= OnSkillNameChanged;
            vm.CardLevelText.OnValueChanged -= OnCardLevelTextChanged;
            vm.SkillDesc.OnValueChanged -= OnSkillDescChanged;
            vm.BaseAttackValue.OnValueChanged -= OnBaseAttackValueChanged;
            vm.BaseAttackDiff.OnValueChanged -= OnBaseAttackDiffChanged;
            vm.ShowBaseAttackDiff.OnValueChanged -= OnShowBaseAttackDiffChanged;
            vm.DamageTypeName.OnValueChanged -= OnDamageTypeNameChanged;
            vm.InheritDamageValue.OnValueChanged -= OnInheritDamageValueChanged;
            vm.InheritDamageColor.OnValueChanged -= OnInheritDamageColorChanged;
            vm.InheritDamageMaterial.OnValueChanged -= OnInheritDamageMaterialChanged;
            vm.InheritDamageDiff.OnValueChanged -= OnInheritDamageDiffChanged;
            vm.ShowInheritDamageDiff.OnValueChanged -= OnShowInheritDamageDiffChanged;
            vm.CooldownValue.OnValueChanged -= OnCooldownValueChanged;
            vm.CooldownDiff.OnValueChanged -= OnCooldownDiffChanged;
            vm.ShowCooldownDiff.OnValueChanged -= OnShowCooldownDiffChanged;
            vm.LevelDescs.OnValueChanged -= OnLevelDescsChanged;
            vm.CardSlotData.OnValueChanged -= OnCardSlotDataChanged;
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            // 清除动画状态缓存，确保每次打开面板都重新初始化默认状态
            mLevelStateCache.Clear();
            WarmUpLevelDetailAnimators();

            // 从UIData获取技能ID（如果调用方传递了UIData）
            var data = uiData as SkillUpGradePanelUIData;
            if (data != null && VM != null)
            {
                VM.SetData(data.SkillId);
            }
            else if (VM != null)
            {
                // 如果没有传递UIData，则使用ViewModel中已有的数据刷新界面
                // 调用方通过 skillUpgradeVm.SetData(skillId) 已经设置了数据
                VM.RefreshUI();
            }

            // 播放打开动画
            var openNode = GetAnimationOpenNode;
            if (openNode != null)
            {
                RunAnimation(openNode, null);
            }
        }

        public override void Show()
        {
            base.Show();

            // 播放打开动画
            var openNode = GetAnimationOpenNode;
            if (openNode != null)
            {
                RunAnimation(openNode, null);
            }
        }


        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected GameObject GetAnimationCloseNode
        {
            get => mOldPanel?.CloseAnimation?.gameObject;
        }

        protected GameObject GetAnimationOpenNode
        {
            get => mOldPanel?.OpenAnimation?.gameObject;
        }

        protected void RunAnimation(GameObject target, UnityAction complete)
        {
            if (target == null)
            {
                complete?.Invoke();
                return;
            }

            UIAnimationUtils.PlayAnimations(target, complete);
        }

        #endregion

        #region 属性变化处理

        private void OnQuitButtonClicked()
        {
            VM?.OnCloseCommand.Execute();

            var closeNode = GetAnimationCloseNode;
            if (closeNode != null)
            {
                RunAnimation(closeNode, () => CloseSelf());
                return;
            }

            CloseSelf();
        }

        private void OnSkillNameChanged(string oldVal, string newVal)
        {
            if (mOldPanel?.SkillName != null)
            {
                mOldPanel.SkillName.text = newVal;
            }
        }

        private void OnCardLevelTextChanged(string oldVal, string newVal)
        {
            SetLocalizationTextValue(mOldPanel?.CardLevelText, newVal);
        }

        private void OnSkillDescChanged(string oldVal, string newVal)
        {
            SetLocalizationTextValue(mOldPanel?.DescText, newVal);
        }

        private void OnBaseAttackValueChanged(string oldVal, string newVal)
        {
            UpdateAttributeSlot(0, newVal, null);
        }

        private void OnBaseAttackDiffChanged(string oldVal, string newVal)
        {
            UpdateAttributeSlotDiff(0, newVal);
        }

        private void OnShowBaseAttackDiffChanged(bool oldVal, bool newVal)
        {
            UpdateAttributeSlotShowDiff(0, newVal);
        }

        private void OnDamageTypeNameChanged(string oldVal, string newVal)
        {
            UpdateAttributeSlot(1, newVal, null);
        }

        private void OnInheritDamageValueChanged(string oldVal, string newVal)
        {
            UpdateAttributeSlot(2, newVal, null);
        }

        private void OnInheritDamageColorChanged(Color oldVal, Color newVal)
        {
            UpdateAttributeSlotColor(2, newVal);
        }

        private void OnInheritDamageMaterialChanged(Material oldVal, Material newVal)
        {
            UpdateAttributeSlotMaterial(2, newVal);
        }

        private void OnInheritDamageDiffChanged(string oldVal, string newVal)
        {
            UpdateAttributeSlotDiff(2, newVal);
        }

        private void OnShowInheritDamageDiffChanged(bool oldVal, bool newVal)
        {
            UpdateAttributeSlotShowDiff(2, newVal);
        }

        private void OnCooldownValueChanged(string oldVal, string newVal)
        {
            UpdateAttributeSlot(3, newVal, null);
        }

        private void OnCooldownDiffChanged(string oldVal, string newVal)
        {
            UpdateAttributeSlotDiff(3, newVal);
        }

        private void OnShowCooldownDiffChanged(bool oldVal, bool newVal)
        {
            UpdateAttributeSlotShowDiff(3, newVal);
        }

        private void OnLevelDescsChanged(List<SkillUpGradePanelViewModel.LevelDescData> oldVal, List<SkillUpGradePanelViewModel.LevelDescData> newVal)
        {
            RefreshLevelDetail(newVal);
        }

        private void OnCardSlotDataChanged(CardSlotData oldVal, CardSlotData newVal)
        {
            if (mOldPanel?.CardSlot != null && newVal != null)
            {
                mOldPanel.CardSlot.SetData(newVal);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新属性槽位数值
        /// </summary>
        private void UpdateAttributeSlot(int index, string value, string suffix)
        {
            if (mOldPanel?.SkillAttributeScroll == null || mOldPanel.SkillAttributeScroll.content == null) return;
            if (index >= mOldPanel.SkillAttributeScroll.content.childCount || index >= 4) return;

            var slot = mOldPanel.SkillAttributeScroll.content.GetChild(index);
            var attrNum = slot.Find("AttrNum");
            if (attrNum != null)
            {
                var text = attrNum.GetComponent<TMP_Text>();
                if (text != null)
                {
                    text.text = suffix != null ? $"{value}{suffix}" : value;
                }
            }
        }

        /// <summary>
        /// 更新属性槽位差值
        /// </summary>
        private void UpdateAttributeSlotDiff(int index, string diffValue)
        {
            if (mOldPanel?.SkillAttributeScroll == null || mOldPanel.SkillAttributeScroll.content == null) return;
            if (index >= mOldPanel.SkillAttributeScroll.content.childCount || index >= 4) return;

            var slot = mOldPanel.SkillAttributeScroll.content.GetChild(index);
            var attrDiff = slot.Find("AttrDiff");
            if (attrDiff != null)
            {
                var text = attrDiff.GetComponent<TMP_Text>();
                if (text != null)
                {
                    text.text = diffValue;
                }
            }
        }

        /// <summary>
        /// 更新属性槽位差值显示
        /// </summary>
        private void UpdateAttributeSlotShowDiff(int index, bool show)
        {
            if (mOldPanel?.SkillAttributeScroll == null || mOldPanel.SkillAttributeScroll.content == null) return;
            if (index >= mOldPanel.SkillAttributeScroll.content.childCount || index >= 4) return;

            var slot = mOldPanel.SkillAttributeScroll.content.GetChild(index);
            var attrDiff = slot.Find("AttrDiff");
            var isDiffBg = slot.Find("IsDiffBg");

            if (attrDiff != null)
            {
                attrDiff.gameObject.SetActive(show);
            }
            if (isDiffBg != null)
            {
                isDiffBg.gameObject.SetActive(show);
            }
        }

        /// <summary>
        /// 更新属性槽位颜色
        /// </summary>
        private void UpdateAttributeSlotColor(int index, Color color)
        {
            if (mOldPanel?.SkillAttributeScroll == null || mOldPanel.SkillAttributeScroll.content == null) return;
            if (index >= mOldPanel.SkillAttributeScroll.content.childCount || index >= 4) return;

            var slot = mOldPanel.SkillAttributeScroll.content.GetChild(index);
            var attrNum = slot.Find("AttrNum");
            if (attrNum != null)
            {
                var text = attrNum.GetComponent<TMP_Text>();
                if (text != null)
                {
                    text.color = color;
                }
            }
        }

        /// <summary>
        /// 更新属性槽位材质
        /// </summary>
        private void UpdateAttributeSlotMaterial(int index, Material material)
        {
            if (mOldPanel?.SkillAttributeScroll == null || mOldPanel.SkillAttributeScroll.content == null) return;
            if (index >= mOldPanel.SkillAttributeScroll.content.childCount || index >= 4) return;

            var slot = mOldPanel.SkillAttributeScroll.content.GetChild(index);
            var attrNum = slot.Find("AttrNum");
            if (attrNum != null)
            {
                var text = attrNum.GetComponent<TMP_Text>();
                if (text != null && material != null)
                {
                    text.fontMaterial = material;
                }
            }
        }

        /// <summary>
        /// 刷新升品描述
        /// </summary>
        private void RefreshLevelDetail(List<SkillUpGradePanelViewModel.LevelDescData> levelDescs)
        {
            if (mOldPanel?.LevelDetail == null) return;
            levelDescs ??= new List<SkillUpGradePanelViewModel.LevelDescData>();

            int maxCount = Math.Min(mOldPanel.LevelDetail.childCount, 4);

            for (int i = 0; i < maxCount; i++)
            {
                var child = mOldPanel.LevelDetail.GetChild(i);
                var childLevel = i + 1;
                var levelUpAnim = child.GetComponent<LevelUpAnimation>();
                EnsureLevelDetailRowActive(child, levelUpAnim);

                var descText = child.Find("LevelDescText")?.GetComponent<TMP_Text>();
                var levelText = child.Find("LevelText")?.GetComponent<TMP_Text>();

                if (i < levelDescs.Count)
                {
                    var data = levelDescs[i];
                    if (descText != null)
                    {
                        descText.text = data.Desc;
                    }
                    if (levelText != null)
                    {
                        levelText.text = $"Lv.{data.Level}";
                    }

                    ApplyLevelDetailAnimator(levelUpAnim, data.IsLocked, data.Level);

                    if (descText != null)
                    {
                        descText.color = data.IsLocked
                            ? new Color32(255, 255, 255, 255)
                            : new Color32(150, 140, 106, 255);
                    }
                }
                else
                {
                    if (descText != null) descText.text = string.Empty;
                    if (levelText != null) levelText.text = $"Lv.{childLevel}";
                    ApplyLevelDetailAnimator(levelUpAnim, true, childLevel);
                    if (descText != null)
                    {
                        descText.color = new Color32(255, 255, 255, 255);
                    }
                }
            }
        }

        private void WarmUpLevelDetailAnimators()
        {
            if (mOldPanel?.LevelDetail == null) return;

            int count = Math.Min(mOldPanel.LevelDetail.childCount, 4);
            for (int i = 0; i < count; i++)
            {
                var child = mOldPanel.LevelDetail.GetChild(i);
                var levelUpAnim = child.GetComponent<LevelUpAnimation>();
                EnsureLevelDetailRowActive(child, levelUpAnim);
            }
        }

        private static void EnsureLevelDetailRowActive(Transform child, LevelUpAnimation levelUpAnim)
        {
            bool wasActive = child.gameObject.activeSelf;
            child.gameObject.SetActive(true);
            if (!wasActive)
            {
                levelUpAnim.PrepareAnimator();
            }
        }

        private void ApplyLevelDetailAnimator(LevelUpAnimation levelUpAnim, bool isLocked, int cacheKey)
        {
            bool hasCachedState = mLevelStateCache.TryGetValue(cacheKey, out var cachedState);
            var playState = LevelUpAnimation.ResolvePlayState(isLocked, hasCachedState, cachedState);
            levelUpAnim.SetAnimationState(playState);
            mLevelStateCache[cacheKey] = LevelUpAnimation.ToCachedState(isLocked);
        }

        private void SetLocalizationTextValue(LocalizationText localizationText, string value)
        {
            if (localizationText == null)
            {
                return;
            }

            var text = localizationText.TextUGUI ?? localizationText.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        #endregion
    }

    /// <summary>
    /// 技能详情面板UI数据
    /// </summary>
    public class SkillUpGradePanelUIData : IUIData
    {
        public int SkillId;
    }
}
