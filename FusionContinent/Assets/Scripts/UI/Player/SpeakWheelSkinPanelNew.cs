using Game.Player;
using UnityEngine;
using UnityEngine.UI;
using UIFramework;
using Config;
using ScrollView;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Cysharp.Threading.Tasks;
using FusionContinent.Helper;
using UIFramework.Services;
using FusionContinent.Model;
using TMPro;

namespace FusionContinent
{
    [PrefabPath("SpeakWheelSkinPanel")]
    public class SpeakWheelSkinPanelNew : UIPanel<SpeakWheelSkinPanelViewModel>
    {
        private const string StarActiveName = "IsActive";

        private SpeakWheelSkinPanel mOldPanel;
        private UnityEngine.Events.UnityAction mExitBtnClickHandler;
        private UnityEngine.Events.UnityAction mWearBtnClickHandler;
        private UnityEngine.Events.UnityAction mForgeBtnClickHandler;
        private UnityEngine.Events.UnityAction mWeaponRuleItemClickHandler;
        private TextMeshProUGUI mWearBtnText;

        private TbPlayerFigureConfig mTbPlayerFigureConfig;
        private List<PlayerFigureConfig> mAllSkins = new List<PlayerFigureConfig>();
        private int mSelectedSkinId;

        private PrefabInstanceLoader.Handle mSkinSpineHandle = new PrefabInstanceLoader.Handle();
        private SpeakerService mSpeakerService;
        private System.Action<int> mWearSkinChangedHandler;

        private readonly List<GameObject> mStarActiveNodes = new List<GameObject>();
        private int mSubscribedConsumeItemId;
        private System.Action<object> mConsumeItemChangedHandler;

        void Awake()
        {
            mOldPanel = GetComponent<SpeakWheelSkinPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[SpeakWheelSkinPanelNew] 找不到 SpeakWheelSkinPanel 组件");
                return;
            }

            mWearBtnText = mOldPanel.WearBtn?.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        }

        protected override void OnBindViewModel(SpeakWheelSkinPanelViewModel vm)
        {
            if (mOldPanel == null)
            {
                return;
            }

            mTbPlayerFigureConfig = DataConfigManager.Tables.TbPlayerFigureConfig;
            mAllSkins = mTbPlayerFigureConfig?.DataList?.ToList() ?? new List<PlayerFigureConfig>();
            mSpeakerService = GameServiceManager.Get<SpeakerService>();
            mConsumeItemChangedHandler = OnConsumeItemChanged;

            BindExitButton();
            BindWearButton(vm);
            BindForgeButton();
            BindWeaponRuleItem();

            BindPlayerScrollView();
            InitSelection();
            vm.SelectSkin(mSelectedSkinId);
            RefreshAll();

            BindServiceEvents();
        }

        protected override void OnUnbindViewModel(SpeakWheelSkinPanelViewModel vm)
        {
            UnbindServiceEvents();
            UnbindExitButton();
            UnbindWearButton();
            UnbindForgeButton();
            UnbindWeaponRuleItem();
            UpdateConsumeItemSubscription(0);
            ReleasePlayerScrollView();
            mSkinSpineHandle.Clear();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            VM.SelectSkin(mSelectedSkinId);
            RefreshAll();
        }

        protected override void OnShow() { }
        protected override void OnHide() { }

        private void BindExitButton()
        {
            if (mOldPanel?.ExitBtn == null) return;
            var exitBtn = mOldPanel.ExitBtn.GetComponent<Button>();
            if (exitBtn == null) return;

            mExitBtnClickHandler = () => UIManager.ClosePanel(this);
            exitBtn.onClick.AddListener(mExitBtnClickHandler);
        }

        private void UnbindExitButton()
        {
            if (mOldPanel?.ExitBtn == null || mExitBtnClickHandler == null) return;
            var exitBtn = mOldPanel.ExitBtn.GetComponent<Button>();
            if (exitBtn == null) return;

            exitBtn.onClick.RemoveListener(mExitBtnClickHandler);
            mExitBtnClickHandler = null;
        }

        private void BindWearButton(SpeakWheelSkinPanelViewModel vm)
        {
            if (mOldPanel?.WearBtn == null) return;
            mWearBtnClickHandler = () => vm.WearSelectedSkinAsync().Forget();
            mOldPanel.WearBtn.onClick.AddListener(mWearBtnClickHandler);
        }

        private void UnbindWearButton()
        {
            if (mOldPanel?.WearBtn == null || mWearBtnClickHandler == null) return;
            mOldPanel.WearBtn.onClick.RemoveListener(mWearBtnClickHandler);
            mWearBtnClickHandler = null;
        }

        private void BindForgeButton()
        {
            if (mOldPanel?.ForgeBtn == null) return;
            mForgeBtnClickHandler = OnForgeBtnClicked;
            mOldPanel.ForgeBtn.onClick.AddListener(mForgeBtnClickHandler);
        }

        private void UnbindForgeButton()
        {
            if (mOldPanel?.ForgeBtn == null || mForgeBtnClickHandler == null) return;
            mOldPanel.ForgeBtn.onClick.RemoveListener(mForgeBtnClickHandler);
            mForgeBtnClickHandler = null;
        }

        private void BindWeaponRuleItem()
        {
            if (mOldPanel?.WeaponRuleItem == null) return;
            mWeaponRuleItemClickHandler = OnWeaponRuleItemClicked;
            mOldPanel.WeaponRuleItem.onClick.AddListener(mWeaponRuleItemClickHandler);
        }

        private void UnbindWeaponRuleItem()
        {
            if (mOldPanel?.WeaponRuleItem == null || mWeaponRuleItemClickHandler == null) return;
            mOldPanel.WeaponRuleItem.onClick.RemoveListener(mWeaponRuleItemClickHandler);
            mWeaponRuleItemClickHandler = null;
        }

        private void OnForgeBtnClicked()
        {
            var skinConfigId = mSelectedSkinId;
            if (skinConfigId <= 0)
            {
                return;
            }

            if (mSpeakerService == null || !mSpeakerService.IsActivated(skinConfigId))
            {
                Toast.Show("皮肤未解锁");
                return;
            }

            if (!mSpeakerService.CanUpgrade(skinConfigId))
            {
                Toast.Show("已达最高星级");
                return;
            }

            var vm = new StarUpGradePanelViewModel();
            vm.OnCreate();
            vm.SetData(new FigureStarItemDataProvider(), skinConfigId);
            UIManager.OpenPanelAsync<UpGradeStarPanelNew, StarUpGradePanelViewModel>(vm, UILevel.PopUI).Forget();
        }

        private void OnWeaponRuleItemClicked()
        {
            var skinConfigId = mSelectedSkinId;
            if (skinConfigId <= 0)
            {
                return;
            }

            var vm = new StarSpecialEPanelViewModel();
            vm.OnCreate();
            vm.Initialize(new FigureStarItemDataProvider(), skinConfigId);
            UIManager.OpenPanelAsync<StarSpecialEPanelNew, StarSpecialEPanelViewModel>(vm, UILevel.PopUI).Forget();
        }

        private void BindPlayerScrollView()
        {
            if (mOldPanel?.PlayerScrollView == null)
            {
                LogTool.LogError("[SpeakWheelSkinPanelNew] PlayerScrollView 为空");
                return;
            }

            mOldPanel.PlayerScrollView.SetUpdateFunc(OnUpdateSkinItem);
            mOldPanel.PlayerScrollView.SetUpdateSelectFunc(OnUpdateSkinSelect);
        }
        /// <summary>
        /// 更新属性列表
        /// </summary>
        private void UpdateAttributeList()
        {
            if (mOldPanel == null)
            {
                return;
            }

            var AttributeScrollContent = mOldPanel.AttributeScrollContent;
            for (int i = 0; i < AttributeScrollContent.childCount; i++)
            {
                AttributeScrollContent.GetChild(i).gameObject.SetActive(false);
            }

            var skinId = VM.SelectedSkinId.Value;
            var figureConfig = DataConfigManager.Tables.TbPlayerFigureConfig.GetOrDefault(skinId);
            if (figureConfig == null)
            {
                ShowNoAttributeBonus(AttributeScrollContent);
                return;
            }

            var level = mSpeakerService.GetSkinLevelByConfigId(skinId);
            if (level <= 0)
            {
                level = 1;
            }
            var levelCfg = figureConfig.ImageStarList.FirstOrDefault(cfg => cfg.Level == level);
            var attributes = new List<AttributeStruct>();
            if (levelCfg != null)
            {
                attributes.AddRange(levelCfg.BaseAttributes);
                attributes.AddRange(levelCfg.ExtraAttributes);
            }

            if (attributes.Count == 0)
            {
                ShowNoAttributeBonus(AttributeScrollContent);
                return;
            }

            for (int i = 0; i < attributes.Count; i++)
            {
                var attrV = attributes[i];
                var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attrV.Type);
                if (attrCfg == null)
                {
                    LogTool.LogWarning($"[SpeakWheelSkinPanelNew] 属性配置未找到: Type={attrV.Type}, SkinId={figureConfig.Id}");
                    continue;
                }

                GameObject attrObj;
                if (i < AttributeScrollContent.childCount)
                {
                    attrObj = AttributeScrollContent.GetChild(i).gameObject;
                }
                else
                {
                    attrObj = Instantiate(AttributeScrollContent.GetChild(0).gameObject, AttributeScrollContent);
                }

                attrObj.SetActive(true);
                SetAttributeRowText(attrObj, attrCfg.Name, attrV.Value);
            }
        }

        private static void ShowNoAttributeBonus(Transform attributeScrollContent)
        {
            if (attributeScrollContent.childCount == 0)
            {
                return;
            }

            var defObj = attributeScrollContent.GetChild(0).gameObject;
            defObj.SetActive(true);
            SetAttributeRowText(defObj, "UI_ATTR_NOT_BONUS", null);
        }

        private static void SetAttributeRowText(GameObject attrObj, string nameKey, float? value)
        {
            var symbolStr = value.HasValue && value.Value > 0 ? "+" : "";
            var valueStr = value.HasValue ? $"{symbolStr}{value.Value}" : null;

            var localAutoWidthText = attrObj.transform.Find("Text")?.GetComponent<LocalizationaAutoWidthText>();
            if (localAutoWidthText != null)
            {
                if (valueStr == null)
                {
                    localAutoWidthText.SetData(LocalizationModule.UITexts, nameKey);
                }
                else
                {
                    localAutoWidthText.SetData(LocalizationModule.UITexts, nameKey, valueStr);
                }

                return;
            }

            var localText = attrObj.transform.Find("Text")?.GetComponent<LocalizationText>();
            if (localText != null)
            {
                if (valueStr == null)
                {
                    localText.SetData(LocalizationModule.UITexts, nameKey);
                }
                else
                {
                    localText.SetData(LocalizationModule.UITexts, nameKey, valueStr);
                }

                return;
            }

            var tmpText = attrObj.transform.Find("Text")?.GetComponent<TMP_Text>();
            if (tmpText == null)
            {
                return;
            }

            var localizedName = LocalizationManager.Instance.GetText(LocalizationModule.UITexts, nameKey);
            tmpText.text = valueStr == null ? localizedName : $"{localizedName}{valueStr}";
        }
        private void ReleasePlayerScrollView()
        {
            mOldPanel?.PlayerScrollView?.ReleaseScrollView();
        }

        private void InitSelection()
        {
            var wearId = Global.UserPlayerSpeaker?.UseSkinId ?? 0;
            if (wearId > 0)
            {
                mSelectedSkinId = wearId;
                return;
            }

            // 优先选择已拥有的第一个，否则选配置表第一个
            foreach (var kv in Global.PlayerCycleSpeakerSkins)
            {
                if (kv.Value != null && kv.Value.SkinId > 0)
                {
                    mSelectedSkinId = kv.Value.SkinId;
                    return;
                }
            }

            mSelectedSkinId = mAllSkins.Count > 0 ? mAllSkins[0].Id : 0;
        }

        private void RefreshAll()
        {
            RefreshTopInfo();
            RefreshSpine();
            RefreshSkinStars();
            RefreshListOnly();
            UpdateAttributeList();
            RefreshConsumeDisplay();
        }

        private void RefreshConsumeDisplay()
        {
            var consumeRoot = mOldPanel.Consume;
            if (consumeRoot == null)
            {
                return;
            }

            var skinConfigId = mSelectedSkinId;
            if (skinConfigId <= 0
                || mSpeakerService == null
                || !mSpeakerService.TryGetNextUnlockConsume(skinConfigId, out var itemId, out var needCount)
                || itemId <= 0
                || needCount <= 0)
            {
                consumeRoot.gameObject.SetActive(false);
                UpdateConsumeItemSubscription(0);
                return;
            }

            consumeRoot.gameObject.SetActive(true);
            consumeRoot.SetData(itemId, needCount);
            UpdateConsumeItemSubscription(itemId);
        }

        private void OnConsumeItemChanged(object _)
        {
            RefreshConsumeDisplay();
            RefreshListOnly();
            RefreshWearBtnActivateRedDot();
        }

        private void UpdateConsumeItemSubscription(int itemId)
        {
            if (mSubscribedConsumeItemId == itemId)
            {
                return;
            }

            if (mSubscribedConsumeItemId > 0)
            {
                Global.Publisher.Unsubscribe(
                    $"{MessagePublisher.CHANGE_ITEM_NUM}{mSubscribedConsumeItemId}",
                    mConsumeItemChangedHandler);
            }

            mSubscribedConsumeItemId = itemId;

            if (itemId > 0)
            {
                Global.Publisher.Subscribe(
                    $"{MessagePublisher.CHANGE_ITEM_NUM}{itemId}",
                    mConsumeItemChangedHandler);
            }
        }

        private void RefreshTopInfo()
        {
            var cfg = mTbPlayerFigureConfig?.GetOrDefault(mSelectedSkinId);
            if (mOldPanel?.PlayerName != null)
            {
                mOldPanel.PlayerName.text = cfg?.Name ?? string.Empty;
            }

            var activated = IsOwned(mSelectedSkinId);
            mOldPanel.SkinSkillStateText.text = activated ? "已激活" : "未激活";

            // Wear按钮
            if (mOldPanel?.WearBtn != null)
            {
                var isEquipped = activated
                    && mSpeakerService != null
                    && mSpeakerService.IsEquipped(mSelectedSkinId);
                mOldPanel.WearBtn.interactable = true;
                if (mWearBtnText != null)
                {
                    mWearBtnText.text = activated
                        ? (isEquipped ? "卸下" : "穿戴")
                        : "激活";
                }

                RefreshWearBtnActivateRedDot();
            }

            // Forge按钮：仅已激活皮肤显示，可升级时才可点
            if (mOldPanel?.ForgeBtn != null)
            {
                mOldPanel.ForgeBtn.gameObject.SetActive(activated);
                mOldPanel.ForgeBtn.interactable = activated
                    && (mSpeakerService?.CanUpgrade(mSelectedSkinId) ?? false);
            }
        }

        private void RefreshWearBtnActivateRedDot()
        {
            if (mOldPanel?.WearBtn == null)
            {
                return;
            }

            bool show = mSelectedSkinId > 0
                && mSpeakerService != null
                && mSpeakerService.CanActivate(mSelectedSkinId);
            ((RectTransform)mOldPanel.WearBtn.transform).SetLocalRedType(show ? 1 : 0);
        }

        private void RefreshSpine()
        {
            if (mOldPanel?.SkinRoot == null)
            {
                return;
            }

            var cfg = mTbPlayerFigureConfig?.GetOrDefault(mSelectedSkinId);
            if (string.IsNullOrEmpty(cfg?.ShowSpine))
            {
                mSkinSpineHandle.Clear();
                return;
            }

            var address = $"Speaker_{cfg.ShowSpine}";
            mSkinSpineHandle.LoadAndReplace(address, mOldPanel.SkinRoot, normalizeRect: true);
        }

        private void RefreshSkinStars()
        {
            if (mOldPanel?.SkinStar == null)
            {
                return;
            }

            var cfg = mTbPlayerFigureConfig?.GetOrDefault(mSelectedSkinId);
            var maxStarCount = GetMaxStarCount(cfg);
            var activated = mSelectedSkinId > 0 && mSpeakerService != null && mSpeakerService.IsActivated(mSelectedSkinId);
            var starCount = activated ? mSpeakerService.GetSkinLevelByConfigId(mSelectedSkinId) : 0;

            CacheSkinStarNodes();
            EnsureSkinStarCapacity(maxStarCount);
            starCount = Mathf.Clamp(starCount, 0, mStarActiveNodes.Count);
            for (int i = 0; i < mStarActiveNodes.Count; i++)
            {
                mStarActiveNodes[i].SetActive(i < starCount);
            }
        }

        private void CacheSkinStarNodes()
        {
            if (mStarActiveNodes.Count > 0)
            {
                return;
            }

            var skinStar = mOldPanel?.SkinStar;
            if (skinStar == null)
            {
                return;
            }

            for (int i = 0; i < skinStar.childCount; i++)
            {
                var star = skinStar.GetChild(i);
                var active = star.Find(StarActiveName);
                if (active != null)
                {
                    mStarActiveNodes.Add(active.gameObject);
                }
            }
        }

        private void EnsureSkinStarCapacity(int requiredCount)
        {
            if (requiredCount <= 0 || mStarActiveNodes.Count >= requiredCount)
            {
                return;
            }

            var skinStar = mOldPanel?.SkinStar;
            if (skinStar == null || skinStar.childCount == 0)
            {
                return;
            }

            var template = skinStar.GetChild(0);
            while (mStarActiveNodes.Count < requiredCount)
            {
                var star = Instantiate(template, skinStar);
                var active = star.Find(StarActiveName);
                if (active == null)
                {
                    break;
                }

                active.gameObject.SetActive(false);
                mStarActiveNodes.Add(active.gameObject);
            }
        }

        private static int GetMaxStarCount(PlayerFigureConfig cfg)
        {
            if (cfg?.ImageStarList == null || cfg.ImageStarList.Count == 0)
            {
                return 0;
            }

            var maxLevel = 0;
            foreach (var starCfg in cfg.ImageStarList)
            {
                if (starCfg.Level > maxLevel)
                {
                    maxLevel = starCfg.Level;
                }
            }

            return maxLevel;
        }

        private void RefreshListOnly()
        {
            if (mOldPanel?.PlayerScrollView == null)
            {
                return;
            }

            mOldPanel.PlayerScrollView.ShowList(0);
            mOldPanel.PlayerScrollView.ShowList(mAllSkins.Count);
            mOldPanel.PlayerScrollView.RefreshSelect();
        }

        private void OnUpdateSkinSelect(int dataIndex, ScrollItem scrollItem)
        {
            var entity = scrollItem.GetComponent<RewardEntity>();
            if (entity == null) return;

            var cfg = GetConfigByIndex(dataIndex);
            if (cfg == null) return;

            if (entity.frameCheck != null)
            {
                entity.frameCheck.gameObject.SetActive(cfg.Id == mSelectedSkinId);
            }
        }

        private void OnUpdateSkinItem(int dataIndex, ScrollItem scrollItem)
        {
            var cfg = GetConfigByIndex(dataIndex);
            if (cfg == null) return;

            var entity = scrollItem.GetComponent<RewardEntity>();
            if (entity == null) return;

            bool owned = IsOwned(cfg.Id);
            bool isWear = (Global.UserPlayerSpeaker?.UseSkinId ?? 0) == cfg.Id;
            bool isSelected = cfg.Id == mSelectedSkinId;

            // 通过 SetData 初始化图标/边框并注入点击回调；_rewardEntityData 非空可避免
            // RewardEntity.Start() 里点击回调访问空对象。StyleType=Equip 让 num/consumeNum 隐藏。
            var data = new RewardEntityData
            {
                StyleType = RewardStyleType.Equip,
                // 品质框需按 PlayerFigureConfig.Quality 渲染；不传则会在 SetData 里被强制
                // 改成 Ordinary，导致列表所有皮肤品质框都一样。
                Quality = cfg.Quality,
                OnClick = () =>
                {
                    mSelectedSkinId = cfg.Id;
                    VM.SelectSkin(mSelectedSkinId);
                    RefreshTopInfo();
                    RefreshSpine();
                    RefreshSkinStars();
                    UpdateAttributeList();
                    RefreshConsumeDisplay();
                    RefreshWearBtnActivateRedDot();
                    mOldPanel?.PlayerScrollView?.RefreshSelect();
                }
            };
            entity.SetData(data);

            // 皮肤头像取自 Speaker 图集，覆盖 SetData 默认的道具图标
            if (entity.icon != null)
            {
                SpriteHelper.TryLoadPaintAsync(cfg.Icon, entity.icon, groupName: "Speaker");
            }

            // 皮肤列表专属状态，覆盖 SetData 内部按通用奖励语义的设置
            if (entity.lockImg != null) entity.lockImg.gameObject.SetActive(!owned);
            if (entity.maskImg != null) entity.maskImg.gameObject.SetActive(!owned);
            if (entity.equipIsHave != null) entity.equipIsHave.gameObject.SetActive(isWear);
            if (entity.frameCheck != null) entity.frameCheck.gameObject.SetActive(isSelected);

            // 可激活（未激活且材料够）时显示红点
            bool canActivate = mSpeakerService != null && mSpeakerService.CanActivate(cfg.Id);
            ((RectTransform)entity.transform).SetLocalRedType(canActivate ? 1 : 0);
        }

        private PlayerFigureConfig GetConfigByIndex(int dataIndex)
        {
            if (dataIndex < 0 || dataIndex >= mAllSkins.Count)
            {
                return null;
            }
            return mAllSkins[dataIndex];
        }

        private bool IsOwned(int skinId)
        {
            return mSpeakerService.IsActivated(skinId);
        }

        private void BindServiceEvents()
        {
            var service = mSpeakerService ?? GameServiceManager.Get<SpeakerService>();
            if (service == null)
            {
                return;
            }

            // 保留引用便于解绑
            mWearSkinChangedHandler = skinId =>
            {
                if (mSelectedSkinId <= 0 || mSelectedSkinId == skinId)
                {
                    mSelectedSkinId = skinId;
                }

                VM.SelectSkin(mSelectedSkinId);
                RefreshTopInfo();
                RefreshSpine();
                RefreshSkinStars();
                RefreshListOnly();
                UpdateAttributeList();
                RefreshConsumeDisplay();
            };
            service.OnWearSkinChanged += mWearSkinChangedHandler;
            service.OnSkinDataChanged += OnSkinDataChanged;
        }

        private void OnSkinDataChanged(int skinConfigId)
        {
            if (mSelectedSkinId == skinConfigId || mSelectedSkinId <= 0)
            {
                VM.SelectSkin(mSelectedSkinId);
                RefreshAll();
            }
            else
            {
                RefreshListOnly();
                RefreshConsumeDisplay();
            }
        }

        private void UnbindServiceEvents()
        {
            var service = mSpeakerService ?? GameServiceManager.Get<SpeakerService>();
            if (service != null)
            {
                if (mWearSkinChangedHandler != null)
                {
                    service.OnWearSkinChanged -= mWearSkinChangedHandler;
                    mWearSkinChangedHandler = null;
                }

                service.OnSkinDataChanged -= OnSkinDataChanged;
            }
        }
    }
}
