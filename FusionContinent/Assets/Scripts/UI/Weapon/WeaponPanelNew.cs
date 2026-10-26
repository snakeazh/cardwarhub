using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Model;
using Game.Player;
using Game.Player.UIItems;
using Game.RedDot.View;
using ScrollView;
using TMPro;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    [PrefabPath("WeaponPanel")]
    public class WeaponPanelNew : UIPanel<WeaponPanelViewModel>
    {
        private const string StarActiveName = "IsActive";

        private WeaponPanel mOldPanel;
        private Button mExitBtn;
        private TextMeshProUGUI mWearBtnText;
        private readonly List<GameObject> mStarActiveNodes = new List<GameObject>();
        private int mSubscribedConsumeItemId;
        private System.Action<object> mConsumeItemChangedHandler;

        void Awake()
        {
            mOldPanel = GetComponent<WeaponPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[WeaponPanelNew] 找不到 WeaponPanel 组件");
                return;
            }

            mExitBtn = mOldPanel.ExitBtn.GetComponent<Button>();
            if (mExitBtn == null)
            {
                LogTool.LogError("[WeaponPanelNew] ExitBtn 节点上找不到 Button 组件");
            }

            mWearBtnText = mOldPanel.WearBtn.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        }

        protected override void OnBindViewModel(WeaponPanelViewModel vm)
        {
            mConsumeItemChangedHandler ??= OnConsumeItemChanged;
            BindButtons(vm);
            BindViewModelStates(vm);
            BindWeaponScrollView();
            CacheTopStarNodes();
            RefreshWeaponList(vm.PlayerWeaponConfigIds.Value);
            ApplyDetailFromViewModel(vm);
            UpdateConsumeItemSubscription(vm.ShowNextUnlockConsume.Value ? vm.NextUnlockConsumeItemId.Value : 0);
        }

        protected override void OnUnbindViewModel(WeaponPanelViewModel vm)
        {
            UnbindButtons();
            UnbindViewModelStates(vm);
            UpdateConsumeItemSubscription(0);
            ReleaseWeaponScrollView();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            VM.RefreshAll();
            RefreshWearBtnActivateRedDot();
            RefreshForgeBtnCultivateRedDot();
        }

        protected override void ReleasePanelResources()
        {
            ReleaseWeaponScrollView();
            base.ReleasePanelResources();
        }

        private void BindButtons(WeaponPanelViewModel vm)
        {
            mExitBtn.onClick.AddListener(OnExitBtnClicked);
            mOldPanel.WearBtn.onClick.AddListener(OnWearBtnClicked);
            mOldPanel.WeaponForgeBtn.onClick.AddListener(OnForgeBtnClicked);
            mOldPanel.WeaponRuleItem.onClick.AddListener(OnWeaponRuleItemClicked);
        }

        private void UnbindButtons()
        {
            mExitBtn.onClick.RemoveListener(OnExitBtnClicked);
            mOldPanel.WearBtn.onClick.RemoveListener(OnWearBtnClicked);
            mOldPanel.WeaponForgeBtn.onClick.RemoveListener(OnForgeBtnClicked);
            mOldPanel.WeaponRuleItem.onClick.RemoveListener(OnWeaponRuleItemClicked);
        }

        private void BindViewModelStates(WeaponPanelViewModel vm)
        {
            vm.PlayerWeaponConfigIds.OnValueChanged += OnPlayerWeaponConfigIdsChanged;
            vm.SelectedWeaponConfigId.OnValueChanged += OnSelectedWeaponConfigIdChanged;
            vm.WeaponName.OnValueChanged += OnWeaponNameChanged;
            vm.WeaponStarCount.OnValueChanged += OnWeaponStarCountChanged;
            vm.WeaponMaxStarCount.OnValueChanged += OnWeaponMaxStarCountChanged;
            vm.WeaponBonusText.OnValueChanged += OnWeaponBonusTextChanged;
            vm.WeaponSkillStateText.OnValueChanged += OnWeaponSkillStateTextChanged;
            vm.WearBtnInteractable.OnValueChanged += OnWearBtnInteractableChanged;
            vm.IsSelectedWeaponActivated.OnValueChanged += OnIsSelectedWeaponActivatedChanged;
            vm.ForgeBtnInteractable.OnValueChanged += OnForgeBtnInteractableChanged;
            vm.ShowNextUnlockConsume.OnValueChanged += OnShowNextUnlockConsumeChanged;
            vm.NextUnlockConsumeItemId.OnValueChanged += OnNextUnlockConsumeChanged;
            vm.NextUnlockConsumeCount.OnValueChanged += OnNextUnlockConsumeChanged;
            vm.WearBtnText.OnValueChanged += OnWearBtnTextChanged;
        }

        private void UnbindViewModelStates(WeaponPanelViewModel vm)
        {
            vm.PlayerWeaponConfigIds.OnValueChanged -= OnPlayerWeaponConfigIdsChanged;
            vm.SelectedWeaponConfigId.OnValueChanged -= OnSelectedWeaponConfigIdChanged;
            vm.WeaponName.OnValueChanged -= OnWeaponNameChanged;
            vm.WeaponStarCount.OnValueChanged -= OnWeaponStarCountChanged;
            vm.WeaponMaxStarCount.OnValueChanged -= OnWeaponMaxStarCountChanged;
            vm.WeaponBonusText.OnValueChanged -= OnWeaponBonusTextChanged;
            vm.WeaponSkillStateText.OnValueChanged -= OnWeaponSkillStateTextChanged;
            vm.WearBtnInteractable.OnValueChanged -= OnWearBtnInteractableChanged;
            vm.IsSelectedWeaponActivated.OnValueChanged -= OnIsSelectedWeaponActivatedChanged;
            vm.ForgeBtnInteractable.OnValueChanged -= OnForgeBtnInteractableChanged;
            vm.ShowNextUnlockConsume.OnValueChanged -= OnShowNextUnlockConsumeChanged;
            vm.NextUnlockConsumeItemId.OnValueChanged -= OnNextUnlockConsumeChanged;
            vm.NextUnlockConsumeCount.OnValueChanged -= OnNextUnlockConsumeChanged;
            vm.WearBtnText.OnValueChanged -= OnWearBtnTextChanged;
        }

        private void BindWeaponScrollView()
        {
            mOldPanel.WeaponSCView.SetUpdateFunc(OnUpdateWeaponItem);
            mOldPanel.WeaponSCView.SetUpdateSelectFunc(OnUpdateWeaponSelect);
        }

        private void ReleaseWeaponScrollView()
        {
            if (mOldPanel.WeaponSCView == null)
            {
                return;
            }

            mOldPanel.WeaponSCView.SetUpdateFunc(null);
            mOldPanel.WeaponSCView.SetUpdateSelectFunc(null);
            mOldPanel.WeaponSCView.ReleaseScrollView();
        }

        private void OnExitBtnClicked()
        {
            CloseSelf();
        }

        private void OnWearBtnClicked()
        {
            VM.WearSelectedWeaponAsync().Forget();
        }

        private void OnForgeBtnClicked()
        {
            var weaponConfigId = VM.SelectedWeaponConfigId.Value;
            if (weaponConfigId <= 0)
            {
                return;
            }

            if (!VM.WeaponService.IsActivated(weaponConfigId))
            {
                Toast.Show("武器未解锁");
                return;
            }

            if (!VM.WeaponService.CanUpgrade(weaponConfigId))
            {
                Toast.Show("已达最高星级");
                return;
            }

            var vm = new StarUpGradePanelViewModel();
            vm.OnCreate();
            vm.SetData(new WeaponStarItemDataProvider(), weaponConfigId);
            UIManager.OpenPanelAsync<UpGradeStarPanelNew, StarUpGradePanelViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
        }

        private void OnWeaponRuleItemClicked()
        {
            var weaponConfigId = VM.SelectedWeaponConfigId.Value;
            if (weaponConfigId <= 0)
            {
                return;
            }

            var vm = new StarSpecialEPanelViewModel();
            vm.OnCreate();
            vm.Initialize(new WeaponStarItemDataProvider(), weaponConfigId);
            UIManager.OpenPanelAsync<StarSpecialEPanelNew, StarSpecialEPanelViewModel>(vm, UILevel.PopUI).Forget();
        }

        private void OnPlayerWeaponConfigIdsChanged(List<int> oldIds, List<int> newIds)
        {
            RefreshWeaponList(newIds);
        }

        private void OnSelectedWeaponConfigIdChanged(int oldId, int newId)
        {
            mOldPanel.WeaponSCView.RefreshSelect();
            RefreshWearBtnActivateRedDot();
            RefreshForgeBtnCultivateRedDot();
        }

        private void OnWeaponNameChanged(string oldValue, string newValue)
        {
            mOldPanel.WeaponName.text = newValue;
        }

        private void OnWeaponStarCountChanged(int oldValue, int newValue)
        {
            RefreshTopStars(newValue, VM.WeaponMaxStarCount.Value);
        }

        private void OnWeaponMaxStarCountChanged(int oldValue, int newValue)
        {
            RefreshTopStars(VM.WeaponStarCount.Value, newValue);
        }

        private void OnWeaponBonusTextChanged(string oldValue, string newValue)
        {
            mOldPanel.WeaponBonus.text = newValue;
        }

        private void OnWeaponSkillStateTextChanged(string oldValue, string newValue)
        {
            mOldPanel.WeaponSkillStateText.text = newValue;
        }

        private void OnWearBtnInteractableChanged(bool oldValue, bool newValue)
        {
            mOldPanel.WearBtn.interactable = newValue;
        }

        private void OnIsSelectedWeaponActivatedChanged(bool oldValue, bool newValue)
        {
            mOldPanel.WeaponForgeBtn.gameObject.SetActive(newValue);
            RefreshWearBtnActivateRedDot();
            RefreshForgeBtnCultivateRedDot();
        }

        private void OnForgeBtnInteractableChanged(bool oldValue, bool newValue)
        {
            mOldPanel.WeaponForgeBtn.interactable = newValue;
            RefreshForgeBtnCultivateRedDot();
        }

        private void OnShowNextUnlockConsumeChanged(bool oldValue, bool newValue)
        {
            UpdateConsumeItemSubscription(newValue ? VM.NextUnlockConsumeItemId.Value : 0);
            RefreshConsumeDisplay();
        }

        private void OnNextUnlockConsumeChanged(int oldValue, int newValue)
        {
            UpdateConsumeItemSubscription(VM.ShowNextUnlockConsume.Value ? VM.NextUnlockConsumeItemId.Value : 0);
            RefreshConsumeDisplay();
        }

        private void OnWearBtnTextChanged(string oldValue, string newValue)
        {
            if (mWearBtnText != null)
            {
                mWearBtnText.text = newValue;
            }
        }

        private void OnWeaponScrollItemClicked(int weaponConfigId)
        {
            VM.SelectWeapon(weaponConfigId);
        }

        private void ApplyDetailFromViewModel(WeaponPanelViewModel vm)
        {
            mOldPanel.WeaponName.text = vm.WeaponName.Value;
            RefreshTopStars(vm.WeaponStarCount.Value, vm.WeaponMaxStarCount.Value);
            mOldPanel.WeaponBonus.text = vm.WeaponBonusText.Value;
            mOldPanel.WeaponSkillStateText.text = vm.WeaponSkillStateText.Value;
            mOldPanel.WearBtn.interactable = vm.WearBtnInteractable.Value;
            mOldPanel.WeaponForgeBtn.gameObject.SetActive(vm.IsSelectedWeaponActivated.Value);
            mOldPanel.WeaponForgeBtn.interactable = vm.ForgeBtnInteractable.Value;
            RefreshConsumeDisplay(vm);
            if (mWearBtnText != null)
            {
                mWearBtnText.text = vm.WearBtnText.Value;
            }

            RefreshWearBtnActivateRedDot();
            RefreshForgeBtnCultivateRedDot();
        }

        private void RefreshWearBtnActivateRedDot()
        {
            if (mOldPanel?.WearBtn == null || VM == null)
            {
                return;
            }

            int weaponConfigId = VM.SelectedWeaponConfigId.Value;
            bool show = weaponConfigId > 0 && VM.WeaponService.CanActivate(weaponConfigId);
            ((RectTransform)mOldPanel.WearBtn.transform).SetLocalRedType(show ? 1 : 0);
        }

        private void RefreshForgeBtnCultivateRedDot()
        {
            if (mOldPanel?.WeaponForgeBtn == null || VM == null)
            {
                return;
            }

            int weaponConfigId = VM.SelectedWeaponConfigId.Value;
            bool show = weaponConfigId > 0 && VM.WeaponService.CanCultivate(weaponConfigId);
            ((RectTransform)mOldPanel.WeaponForgeBtn.transform).SetLocalRedType(show ? 1 : 0);
        }

        private void RefreshWeaponListActivateRedDots()
        {
            if (mOldPanel?.WeaponSCView == null || VM == null)
            {
                return;
            }

            // 强制刷新可见 item，OnUpdateWeaponItem 会重设列表红点/箭头
            var ids = VM.PlayerWeaponConfigIds.Value;
            RefreshWeaponList(ids);
            RefreshWearBtnActivateRedDot();
            RefreshForgeBtnCultivateRedDot();
        }

        private void RefreshConsumeDisplay()
        {
            RefreshConsumeDisplay(VM);
        }

        private void RefreshConsumeDisplay(WeaponPanelViewModel vm)
        {
            var consumeRoot = mOldPanel.Consume;
            if (consumeRoot == null)
            {
                return;
            }

            var visible = vm.ShowNextUnlockConsume.Value;
            consumeRoot.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            var itemId = vm.NextUnlockConsumeItemId.Value;
            var needCount = vm.NextUnlockConsumeCount.Value;
            if (itemId <= 0 || needCount <= 0)
            {
                return;
            }

            consumeRoot.SetData(itemId, needCount);
        }

        private void OnConsumeItemChanged(object _)
        {
            RefreshConsumeDisplay();
            RefreshWeaponListActivateRedDots();
        }

        private void UpdateConsumeItemSubscription(int itemId)
        {
            if (mSubscribedConsumeItemId == itemId)
            {
                return;
            }

            if (mSubscribedConsumeItemId > 0 && mConsumeItemChangedHandler != null)
            {
                Global.Publisher.Unsubscribe(
                    $"{MessagePublisher.CHANGE_ITEM_NUM}{mSubscribedConsumeItemId}",
                    mConsumeItemChangedHandler);
            }

            mSubscribedConsumeItemId = itemId;

            if (itemId > 0 && mConsumeItemChangedHandler != null)
            {
                Global.Publisher.Subscribe(
                    $"{MessagePublisher.CHANGE_ITEM_NUM}{itemId}",
                    mConsumeItemChangedHandler);
            }
        }

        private void RefreshWeaponList(List<int> configIds)
        {
            var scrollView = mOldPanel.WeaponSCView;
            var count = configIds != null ? configIds.Count : 0;
            scrollView.ShowList(0);
            scrollView.ShowList(count);
            scrollView.RefreshSelect();
        }

        private void OnUpdateWeaponItem(int dataIndex, ScrollItem scrollItem)
        {
            var configIds = VM.PlayerWeaponConfigIds.Value;
            if (configIds == null || dataIndex < 0 || dataIndex >= configIds.Count)
            {
                scrollItem.gameObject.SetActive(false);
                return;
            }

            var weaponConfigId = configIds[dataIndex];
            var cfg = VM.GetPlayerWeaponConfig(weaponConfigId);
            if (cfg == null)
            {
                scrollItem.gameObject.SetActive(false);
                return;
            }

            scrollItem.gameObject.SetActive(true);
            scrollItem.OnSpawn();

            var customBtnItem = scrollItem as CustomBtnScrollItem;
            if (customBtnItem != null)
            {
                customBtnItem.OnClick = () => OnWeaponScrollItemClicked(weaponConfigId);
            }

            RefreshWeaponItemDisplay(scrollItem.GetComponent<WeaponItem>(), cfg, weaponConfigId);
        }

        private void OnUpdateWeaponSelect(int dataIndex, ScrollItem scrollItem)
        {
            var configIds = VM.PlayerWeaponConfigIds.Value;
            if (configIds == null || dataIndex < 0 || dataIndex >= configIds.Count)
            {
                return;
            }

            var weaponConfigId = configIds[dataIndex];
            var cfg = VM.GetPlayerWeaponConfig(weaponConfigId);
            if (cfg == null)
            {
                return;
            }

            RefreshWeaponItemDisplay(scrollItem.GetComponent<WeaponItem>(), cfg, weaponConfigId);
        }

        private void RefreshWeaponItemDisplay(WeaponItem weaponItem, PlayerWeaponConfig cfg, int weaponConfigId)
        {
            var weaponService = VM.WeaponService;
            var activated = weaponService.IsActivated(weaponConfigId);
            var starCount = activated ? weaponService.GetWeaponLevelByConfigId(weaponConfigId) : 0;
            weaponItem.SetData(
                cfg.Name,
                cfg.Icon,
                cfg.Quality,
                starCount,
                activated,
                weaponService.IsEquipped(weaponConfigId),
                weaponConfigId == VM.SelectedWeaponConfigId.Value,
                GetMaxStarCount(cfg));

            // 可激活 → 红点；可养成 → 箭头（互斥：未激活不可养成）
            var itemRect = (RectTransform)weaponItem.transform;
            if (weaponService.CanActivate(weaponConfigId))
            {
                itemRect.SetLocalRedType(1);
            }
            else if (weaponService.CanCultivate(weaponConfigId))
            {
                itemRect.SetLocalRedType(1, RedDotDisplay.ArrowPointPrefab);
            }
            else
            {
                itemRect.SetLocalRedType(0);
            }
        }

        private static int GetMaxStarCount(PlayerWeaponConfig cfg)
        {
            if (cfg?.WeaponStarList == null || cfg.WeaponStarList.Count == 0)
            {
                return 0;
            }

            var maxLevel = 0;
            foreach (var starCfg in cfg.WeaponStarList)
            {
                if (starCfg.Level > maxLevel)
                {
                    maxLevel = starCfg.Level;
                }
            }

            return maxLevel;
        }

        private void CacheTopStarNodes()
        {
            if (mStarActiveNodes.Count > 0)
            {
                return;
            }

            var weaponStar = mOldPanel.WeaponStar;
            for (int i = 0; i < weaponStar.childCount; i++)
            {
                var star = weaponStar.GetChild(i);
                var active = star.Find(StarActiveName);
                if (active != null)
                {
                    mStarActiveNodes.Add(active.gameObject);
                }
            }
        }

        private void RefreshTopStars(int starCount, int maxStarCount)
        {
            CacheTopStarNodes();
            EnsureTopStarCapacity(maxStarCount);
            starCount = Mathf.Clamp(starCount, 0, mStarActiveNodes.Count);
            for (int i = 0; i < mStarActiveNodes.Count; i++)
            {
                mStarActiveNodes[i].SetActive(i < starCount);
            }
        }

        private void EnsureTopStarCapacity(int requiredCount)
        {
            if (requiredCount <= 0 || mStarActiveNodes.Count >= requiredCount)
            {
                return;
            }

            var weaponStar = mOldPanel.WeaponStar;
            if (weaponStar == null || weaponStar.childCount == 0)
            {
                return;
            }

            var template = weaponStar.GetChild(0);
            while (mStarActiveNodes.Count < requiredCount)
            {
                var star = Instantiate(template, weaponStar);
                var active = star.Find(StarActiveName);
                if (active == null)
                {
                    break;
                }

                active.gameObject.SetActive(false);
                mStarActiveNodes.Add(active.gameObject);
            }
        }
    }
}
