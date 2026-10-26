using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.UI.Components;
using Game.Player;
using TMPro;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    /// <summary>
    /// 通用升星面板（MVVM 迁移版）。
    /// </summary>
    [PrefabPath("UpGradeStarPanel")]
    public class UpGradeStarPanelNew : UIPanel<StarUpGradePanelViewModel>
    {
        private UpGradeStarPanel mOldPanel;
        private TextMeshProUGUI mWeaponNameText;
        private RewardEntity mWeaponRewardEntity;
        private UIPointerClickAndLongPress mCurWeaponRuleItem;
        private UIPointerClickAndLongPress mNextWeaponRuleItem;
        private readonly List<GameObject> mCurStarNodes = new List<GameObject>();
        private readonly List<GameObject> mNextStarNodes = new List<GameObject>();
        private float mCurDetailDefaultPosX;

        void Awake()
        {
            mOldPanel = GetComponent<UpGradeStarPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[StarUpGradePanelNew] 找不到 UpGradeStarPanel 组件");
                return;
            }

            CacheStaticReferences();
        }

        protected override void OnBindViewModel(StarUpGradePanelViewModel vm)
        {
            if (mOldPanel == null)
            {
                return;
            }

            BindButtons();
            BindViewModelStates(vm);
            ApplyAllFromViewModel(vm);
        }

        protected override void OnUnbindViewModel(StarUpGradePanelViewModel vm)
        {
            UnbindButtons();
            UnbindViewModelStates(vm);
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            // Provider 与 ConfigId 由调用方在 new VM 后通过 SetData(provider, configId) 初始化，
            // 此处仅刷新显示。无需从 uiData 重新初始化。
            VM.RefreshAll();
        }

        private void CacheStaticReferences()
        {
            if (mOldPanel.CurrentWeapon != null)
            {
                mWeaponNameText = mOldPanel.CurrentWeapon.Find("WeaponName")?.GetComponent<TextMeshProUGUI>();
                mWeaponRewardEntity = mOldPanel.CurrentWeapon.GetComponentInChildren<RewardEntity>(true);
            }

            CacheStarNodes(mOldPanel.CurDetail?.Find("StarField"), mCurStarNodes);
            CacheStarNodes(mOldPanel.NextDetail?.Find("StarField"), mNextStarNodes);

            mCurWeaponRuleItem = GetWeaponRuleItemClick(mOldPanel.CurDetail);
            mNextWeaponRuleItem = GetWeaponRuleItemClick(mOldPanel.NextDetail);

            if (mOldPanel.CurDetail != null)
            {
                mCurDetailDefaultPosX = mOldPanel.CurDetail.anchoredPosition.x;
            }
        }

        private static UIPointerClickAndLongPress GetWeaponRuleItemClick(RectTransform detailRoot)
        {
            if (detailRoot == null)
            {
                return null;
            }

            var ruleItem = detailRoot.Find("WeaponRuleItem");
            if (ruleItem == null)
            {
                return null;
            }

            return ruleItem.GetComponent<UIPointerClickAndLongPress>();
        }

        private void BindButtons()
        {
            if (mOldPanel.ExitBtn != null)
            {
                mOldPanel.ExitBtn.onClick.AddListener(OnExitClicked);
            }

            if (mOldPanel.Mask != null)
            {
                mOldPanel.Mask.onClick.AddListener(OnExitClicked);
            }

            if (mOldPanel.LevelUpBtn != null)
            {
                mOldPanel.LevelUpBtn.onClick.AddListener(OnLevelUpClicked);
            }

            if (mCurWeaponRuleItem != null)
            {
                mCurWeaponRuleItem.interactable = true;
                mCurWeaponRuleItem.onClick.AddListener(OnCurWeaponRuleItemClicked);
            }

            if (mNextWeaponRuleItem != null)
            {
                mNextWeaponRuleItem.interactable = true;
                mNextWeaponRuleItem.onClick.AddListener(OnNextWeaponRuleItemClicked);
            }
        }

        private void UnbindButtons()
        {
            if (mOldPanel == null)
            {
                return;
            }

            if (mOldPanel.ExitBtn != null)
            {
                mOldPanel.ExitBtn.onClick.RemoveListener(OnExitClicked);
            }

            if (mOldPanel.Mask != null)
            {
                mOldPanel.Mask.onClick.RemoveListener(OnExitClicked);
            }

            if (mOldPanel.LevelUpBtn != null)
            {
                mOldPanel.LevelUpBtn.onClick.RemoveListener(OnLevelUpClicked);
            }

            if (mCurWeaponRuleItem != null)
            {
                mCurWeaponRuleItem.onClick.RemoveListener(OnCurWeaponRuleItemClicked);
            }

            if (mNextWeaponRuleItem != null)
            {
                mNextWeaponRuleItem.onClick.RemoveListener(OnNextWeaponRuleItemClicked);
            }
        }

        private void BindViewModelStates(StarUpGradePanelViewModel vm)
        {
            vm.ItemName.OnValueChanged += OnWeaponNameChanged;
            vm.ItemIcon.OnValueChanged += OnWeaponIconChanged;
            vm.CurStarCount.OnValueChanged += OnCurStarCountChanged;
            vm.NextStarCount.OnValueChanged += OnNextStarCountChanged;
            vm.MaxStarCount.OnValueChanged += OnMaxStarCountChanged;
            vm.CurBonusText.OnValueChanged += OnCurBonusTextChanged;
            vm.NextBonusText.OnValueChanged += OnNextBonusTextChanged;
            vm.CurSkillDesc.OnValueChanged += OnCurSkillDescChanged;
            vm.NextSkillDesc.OnValueChanged += OnNextSkillDescChanged;
            vm.ShowNextDetail.OnValueChanged += OnShowNextDetailChanged;
            vm.ShowConsume.OnValueChanged += OnShowConsumeChanged;
            vm.ConsumeItemId.OnValueChanged += OnConsumeChanged;
            vm.ConsumeCount.OnValueChanged += OnConsumeChanged;
            vm.LevelUpBtnInteractable.OnValueChanged += OnLevelUpBtnInteractableChanged;
        }

        private void UnbindViewModelStates(StarUpGradePanelViewModel vm)
        {
            vm.ItemName.OnValueChanged -= OnWeaponNameChanged;
            vm.ItemIcon.OnValueChanged -= OnWeaponIconChanged;
            vm.CurStarCount.OnValueChanged -= OnCurStarCountChanged;
            vm.NextStarCount.OnValueChanged -= OnNextStarCountChanged;
            vm.MaxStarCount.OnValueChanged -= OnMaxStarCountChanged;
            vm.CurBonusText.OnValueChanged -= OnCurBonusTextChanged;
            vm.NextBonusText.OnValueChanged -= OnNextBonusTextChanged;
            vm.CurSkillDesc.OnValueChanged -= OnCurSkillDescChanged;
            vm.NextSkillDesc.OnValueChanged -= OnNextSkillDescChanged;
            vm.ShowNextDetail.OnValueChanged -= OnShowNextDetailChanged;
            vm.ShowConsume.OnValueChanged -= OnShowConsumeChanged;
            vm.ConsumeItemId.OnValueChanged -= OnConsumeChanged;
            vm.ConsumeCount.OnValueChanged -= OnConsumeChanged;
            vm.LevelUpBtnInteractable.OnValueChanged -= OnLevelUpBtnInteractableChanged;
        }

        private void OnExitClicked()
        {
            CloseSelf();
        }

        private void OnLevelUpClicked()
        {
            VM.UpgradeAsync().Forget();
        }

        private void OnCurWeaponRuleItemClicked()
        {
            OpenRuleDetail(VM.CurSkillDesc.Value, mCurWeaponRuleItem.transform);
        }

        private void OnNextWeaponRuleItemClicked()
        {
            OpenRuleDetail(VM.NextSkillDesc.Value, mNextWeaponRuleItem.transform);
        }

        private void OpenRuleDetail(string desc2, Transform anchor)
        {
            if (string.IsNullOrEmpty(desc2))
            {
                return;
            }

            GlobalFunc.OpenCommonDetailPopPanel(anchor, VM.ItemName.Value, desc2);
        }

        private void OnWeaponNameChanged(string oldValue, string newValue)
        {
            if (mWeaponNameText != null)
            {
                mWeaponNameText.text = newValue;
            }
        }

        private void OnWeaponIconChanged(string oldValue, string newValue)
        {
            RefreshItemIcon(VM);
        }

        private void OnCurStarCountChanged(int oldValue, int newValue)
        {
            RefreshStars(mCurStarNodes, newValue);
            RefreshRuleItemDisplay(mOldPanel.CurDetail, VM.CurSkillDesc.Value, newValue);
        }

        private void OnNextStarCountChanged(int oldValue, int newValue)
        {
            RefreshStars(mNextStarNodes, newValue);
            RefreshRuleItemDisplay(mOldPanel.NextDetail, VM.NextSkillDesc.Value, newValue);
        }

        private void OnMaxStarCountChanged(int oldValue, int newValue)
        {
            EnsureStarCapacity(mOldPanel.CurDetail?.Find("StarField"), mCurStarNodes, newValue);
            EnsureStarCapacity(mOldPanel.NextDetail?.Find("StarField"), mNextStarNodes, newValue);
            RefreshStars(mCurStarNodes, VM.CurStarCount.Value);
            RefreshStars(mNextStarNodes, VM.NextStarCount.Value);
        }

        private void OnCurBonusTextChanged(string oldValue, string newValue)
        {
            SetBonusText(mOldPanel.CurDetail, newValue);
        }

        private void OnNextBonusTextChanged(string oldValue, string newValue)
        {
            SetBonusText(mOldPanel.NextDetail, newValue);
        }

        private void OnCurSkillDescChanged(string oldValue, string newValue)
        {
            RefreshRuleItemDisplay(mOldPanel.CurDetail, newValue, VM.CurStarCount.Value);
        }

        private void OnNextSkillDescChanged(string oldValue, string newValue)
        {
            RefreshRuleItemDisplay(mOldPanel.NextDetail, newValue, VM.NextStarCount.Value);
        }

        private void OnShowNextDetailChanged(bool oldValue, bool newValue)
        {
            if (mOldPanel.NextDetail != null)
            {
                mOldPanel.NextDetail.gameObject.SetActive(newValue);
            }

            RefreshDetailLayout(newValue);
        }

        private void OnShowConsumeChanged(bool oldValue, bool newValue)
        {
            RefreshConsumeDisplay(VM);
        }

        private void OnConsumeChanged(int oldValue, int newValue)
        {
            RefreshConsumeDisplay(VM);
        }

        private void OnLevelUpBtnInteractableChanged(bool oldValue, bool newValue)
        {
            if (mOldPanel.LevelUpBtn != null)
            {
                mOldPanel.LevelUpBtn.interactable = newValue;
            }
        }

        private void ApplyAllFromViewModel(StarUpGradePanelViewModel vm)
        {
            if (mWeaponNameText != null)
            {
                mWeaponNameText.text = vm.ItemName.Value;
            }

            RefreshItemIcon(vm);
            EnsureStarCapacity(mOldPanel.CurDetail?.Find("StarField"), mCurStarNodes, vm.MaxStarCount.Value);
            EnsureStarCapacity(mOldPanel.NextDetail?.Find("StarField"), mNextStarNodes, vm.MaxStarCount.Value);
            RefreshStars(mCurStarNodes, vm.CurStarCount.Value);
            RefreshStars(mNextStarNodes, vm.NextStarCount.Value);
            SetBonusText(mOldPanel.CurDetail, vm.CurBonusText.Value);
            SetBonusText(mOldPanel.NextDetail, vm.NextBonusText.Value);
            RefreshRuleItemDisplay(mOldPanel.CurDetail, vm.CurSkillDesc.Value, vm.CurStarCount.Value);
            RefreshRuleItemDisplay(mOldPanel.NextDetail, vm.NextSkillDesc.Value, vm.NextStarCount.Value);

            if (mOldPanel.NextDetail != null)
            {
                mOldPanel.NextDetail.gameObject.SetActive(vm.ShowNextDetail.Value);
            }

            RefreshDetailLayout(vm.ShowNextDetail.Value);

            if (mOldPanel.LevelUpBtn != null)
            {
                mOldPanel.LevelUpBtn.interactable = vm.LevelUpBtnInteractable.Value;
            }

            RefreshConsumeDisplay(vm);
        }

        private void RefreshItemIcon(StarUpGradePanelViewModel vm)
        {
            var iconName = vm.ItemIcon.Value;
            if (string.IsNullOrEmpty(iconName) || mWeaponRewardEntity?.icon == null)
            {
                return;
            }

            mWeaponRewardEntity.gameObject.SetActive(true);
            vm.Provider?.SetIcon(mWeaponRewardEntity.icon, vm.ConfigId.Value, iconName);

            if (mWeaponRewardEntity.num != null)
            {
                mWeaponRewardEntity.num.gameObject.SetActive(false);
            }

            if (mWeaponRewardEntity.consumeNum != null)
            {
                mWeaponRewardEntity.consumeNum.gameObject.SetActive(false);
            }
        }

        private void RefreshConsumeDisplay(StarUpGradePanelViewModel vm)
        {
            var consumeRoot = mOldPanel.Consume;
            if (consumeRoot == null)
            {
                return;
            }

            var consumeBar = consumeRoot.parent;
            var visible = vm.ShowConsume.Value;
            if (consumeBar != null)
            {
                consumeBar.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            var itemId = vm.ConsumeItemId.Value;
            var needCount = vm.ConsumeCount.Value;

            var consumeItem = consumeRoot.GetComponent<UIConsumeItem>();
            if (consumeItem != null)
            {
                var show = itemId > 0 && needCount > 0;
                consumeRoot.gameObject.SetActive(show);
                if (show)
                {
                    consumeItem.SetData(itemId, needCount);
                }

                return;
            }

            for (int i = 0; i < consumeRoot.childCount; i++)
            {
                var child = consumeRoot.GetChild(i);
                var show = i == 0 && itemId > 0 && needCount > 0;
                child.gameObject.SetActive(show);
                if (!show)
                {
                    continue;
                }

                var rewardEntity = child.GetComponent<RewardEntity>();
                rewardEntity?.SetData(itemId, needCount, RewardStyleType.Consume);
            }
        }

        private void RefreshDetailLayout(bool showNextDetail)
        {
            if (mOldPanel.CurDetail != null)
            {
                var pos = mOldPanel.CurDetail.anchoredPosition;
                pos.x = showNextDetail ? mCurDetailDefaultPosX : 0f;
                mOldPanel.CurDetail.anchoredPosition = pos;
            }

            if (mOldPanel.Arrow != null)
            {
                mOldPanel.Arrow.gameObject.SetActive(showNextDetail);
            }
        }

        private static void SetBonusText(RectTransform detailRoot, string text)
        {
            if (detailRoot == null)
            {
                return;
            }

            var bonusText = detailRoot.Find("BonusText")?.GetComponent<TextMeshProUGUI>();
            if (bonusText != null)
            {
                bonusText.text = text;
            }
        }

        private static void RefreshRuleItemDisplay(RectTransform detailRoot, string ruleDesc, int level)
        {
            if (detailRoot == null)
            {
                return;
            }

            var ruleItem = detailRoot.Find("WeaponRuleItem");
            if (ruleItem == null)
            {
                return;
            }

            ruleItem.gameObject.SetActive(!string.IsNullOrEmpty(ruleDesc));

            var levelText = ruleItem.Find("Level/Text")?.GetComponent<TextMeshProUGUI>();
            if (levelText != null)
            {
                levelText.text = $"Lv.{level}";
            }
        }

        private static void CacheStarNodes(Transform starField, List<GameObject> cache)
        {
            cache.Clear();
            if (starField == null)
            {
                return;
            }

            for (int i = 0; i < starField.childCount; i++)
            {
                cache.Add(starField.GetChild(i).gameObject);
            }
        }

        private static void EnsureStarCapacity(Transform starField, List<GameObject> cache, int requiredCount)
        {
            if (starField == null || requiredCount <= 0)
            {
                return;
            }

            if (cache.Count == 0)
            {
                CacheStarNodes(starField, cache);
            }

            if (cache.Count == 0)
            {
                return;
            }

            var template = cache[0];
            while (cache.Count < requiredCount)
            {
                var star = Object.Instantiate(template, starField);
                star.name = $"Star1 ({cache.Count})";
                star.SetActive(false);
                cache.Add(star);
            }
        }

        private static void RefreshStars(List<GameObject> starNodes, int starCount)
        {
            if (starNodes == null || starNodes.Count == 0)
            {
                return;
            }

            starCount = Mathf.Clamp(starCount, 0, starNodes.Count);
            for (int i = 0; i < starNodes.Count; i++)
            {
                starNodes[i].SetActive(i < starCount);
            }
        }
    }
}
