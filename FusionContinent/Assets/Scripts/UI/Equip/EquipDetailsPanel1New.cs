using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Animation;
using FusionContinent.Helper;
using FusionContinent.Model;
using Game.CombatPower;
using Game.Equip;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UIFramework;
using UIFramework.Services;

namespace FusionContinent
{
    [PrefabPath("EquipDetailsPanel1")]
    public class EquipDetailsPanel1New : UIFramework.UIPanel<EquipDetailsPanel1ViewModel>
    {
        private EquipDetailsPanel1 mOldPanel;

        private TbEquipConfig mEquipCardConfig;
        private TbMainAttributeConfig mTbMainAttributeConfig;
        private TbAttributeConfig mTbAttributeConfig;
        private TbGemFusionConfig mTbGemFusionConfig;

        private Toggle mEquipToggle;
        private Toggle mFusionGemToggle;

        private readonly List<Button> mUnWearGemBtns = new List<Button>();
        private readonly List<UnityEngine.Events.UnityAction> mUnWearGemHandlers = new List<UnityEngine.Events.UnityAction>();

        void Awake()
        {
            mOldPanel = GetComponent<EquipDetailsPanel1>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[EquipDetailsPanel1New] 找不到 EquipDetailsPanel1 组件");
                return;
            }

            // 禁用旧 Panel 组件
            mOldPanel.enabled = false;

            mEquipCardConfig = DataConfigManager.Tables.TbEquipConfig;
            mTbMainAttributeConfig = DataConfigManager.Tables.TbMainAttributeConfig;
            mTbAttributeConfig = DataConfigManager.Tables.TbAttributeConfig;
            mTbGemFusionConfig = DataConfigManager.Tables.TbGemFusionConfig;
        }

        protected override void OnBindViewModel(EquipDetailsPanel1ViewModel vm)
        {
            if (mOldPanel == null)
            {
                return;
            }

            vm.CurrentEquip.OnValueChanged += OnCurrentEquipChanged;
            vm.WearRequestPending.OnValueChanged += OnWearRequestPendingChanged;
            vm.OnCloseRequested += OnCloseRequestedFromVm;
            vm.OnNavigateUpgradeRequested += OnNavigateUpgradeFromVm;
            vm.OnWearCompleted += OnWearCompletedFromVm;

            mOldPanel.Mask.onClick.AddListener(OnQuitClicked);
            mOldPanel.WearBtn.onClick.AddListener(OnWearClicked);
            mOldPanel.UpGradeBtn.onClick.AddListener(OnUpgradeClicked);

            // 绑定 Toggle 切换：Equip ↔ FusionGem
            mEquipToggle = mOldPanel.Toggle?.transform.Find("Equip")?.GetComponent<Toggle>();
            mFusionGemToggle = mOldPanel.Toggle?.transform.Find("FusionGem")?.GetComponent<Toggle>();

            if (mEquipToggle != null)
            {
                mEquipToggle.onValueChanged.AddListener(OnEquipToggleChanged);
            }
            if (mFusionGemToggle != null)
            {
                mFusionGemToggle.onValueChanged.AddListener(OnFusionGemToggleChanged);
            }

            // 默认选中 Equip
            if (mEquipToggle != null)
            {
                mEquipToggle.isOn = true;
            }

            RefreshAllDisplay(vm);
        }

        protected override void OnUnbindViewModel(EquipDetailsPanel1ViewModel vm)
        {
            vm.CurrentEquip.OnValueChanged -= OnCurrentEquipChanged;
            vm.WearRequestPending.OnValueChanged -= OnWearRequestPendingChanged;
            vm.OnCloseRequested -= OnCloseRequestedFromVm;
            vm.OnNavigateUpgradeRequested -= OnNavigateUpgradeFromVm;
            vm.OnWearCompleted -= OnWearCompletedFromVm;

            if (mEquipToggle != null)
            {
                mEquipToggle.onValueChanged.RemoveListener(OnEquipToggleChanged);
                mEquipToggle = null;
            }
            if (mFusionGemToggle != null)
            {
                mFusionGemToggle.onValueChanged.RemoveListener(OnFusionGemToggleChanged);
                mFusionGemToggle = null;
            }

            ClearUnWearGemHandlers();

            if (mOldPanel != null)
            {
                mOldPanel.Mask.onClick.RemoveListener(OnQuitClicked);
                mOldPanel.WearBtn.onClick.RemoveListener(OnWearClicked);
                mOldPanel.UpGradeBtn.onClick.RemoveListener(OnUpgradeClicked);
            }
        }

        private const float GuideRegisterDelaySeconds = 0.35f;

        protected override void OnOpen(IUIData uiData = null)
        {
            base.OnOpen(uiData);
            if (mOldPanel?.OpenAnimation != null)
            {
                UIAnimationUtils.PlayAnimations(mOldPanel.OpenAnimation.gameObject, () =>
                {
                    RegisterEquipGuideTargetsDelayed().Forget();
                });
            }
            else
            {
                RegisterEquipGuideTargetsDelayed().Forget();
            }
        }

        async UniTaskVoid RegisterEquipGuideTargetsDelayed()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(GuideRegisterDelaySeconds));
            if (!this)
            {
                return;
            }

            RegisterEquipGuideTargets();
        }

        void RegisterEquipGuideTargets()
        {
            if (UnlockGuideManager.Instance == null || mOldPanel == null)
            {
                return;
            }

            if (mOldPanel.WearBtn != null)
            {
                UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Equip, 3,
                    mOldPanel.WearBtn.GetComponent<RectTransform>());
            }

            if (mOldPanel.UpGradeBtn != null)
            {
                UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.EquipPhase, 3,
                    mOldPanel.UpGradeBtn.GetComponent<RectTransform>());
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
        }

        private void OnCurrentEquipChanged(PlayerEquip oldVal, PlayerEquip newVal)
        {
            if (VM != null)
            {
                RefreshAllDisplay(VM);
            }
        }

        private void OnWearRequestPendingChanged(bool oldVal, bool newVal)
        {
            if (mOldPanel?.WearBtn == null || VM == null)
            {
                return;
            }

            if (newVal)
            {
                mOldPanel.WearBtn.interactable = false;
                return;
            }

            InitWearButtonState(VM);
        }

        private void OnCloseRequestedFromVm()
        {
            PlayCloseAnimationAndClose();
        }

        private void OnNavigateUpgradeFromVm(int soldierId, int equipOnlyId)
        {
            // UnlockGuideManager.Send(ModuleFunctionType.Equip, 5);
            UIFramework.UIManager.ClosePanel(this);
            var upgradeVm = new EquipUpGradePanelViewModel();
            upgradeVm.OnCreate();
            if (!upgradeVm.Initialize(soldierId, equipOnlyId))
            {
                Toast.Show(EquipUpGradePanelViewModel.NoWearEquipMessage);
                return;
            }
            UIFramework.UIManager
                .OpenPanelAsync<EquipUpGradePanelNew, EquipUpGradePanelViewModel>(upgradeVm, UIFramework.UILevel.Panel)
                .Forget();
        }

        private void OnWearCompletedFromVm()
        {
            CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
            PlayCloseAnimationAndClose();
        }

        private void OnQuitClicked()
        {
            VM?.OnQuitCommand?.Execute();
        }

        private void OnWearClicked()
        {
            UnlockGuideManager.Send(ModuleFunctionType.Equip, 3);
            VM?.OnWearCommand?.Execute();
        }

        private void OnUpgradeClicked()
        {
            UnlockGuideManager.Send(ModuleFunctionType.EquipPhase, 3);
            VM?.OnUpgradeCommand?.Execute();
        }

        private void OnEquipToggleChanged(bool isOn)
        {
            if (mOldPanel?.EquipAttrPanel != null)
            {
                mOldPanel.EquipAttrPanel.gameObject.SetActive(isOn);
            }
        }

        private void OnFusionGemToggleChanged(bool isOn)
        {
            if (mOldPanel?.FusionGemPanel != null)
            {
                mOldPanel.FusionGemPanel.gameObject.SetActive(isOn);
            }
        }

        private void PlayCloseAnimationAndClose()
        {
            if (mOldPanel?.CloseAnimation != null)
            {
                // UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, CloseSelf);
                CloseSelf();
            }
            else
            {
                CloseSelf();
            }
        }

        private void RefreshAllDisplay(EquipDetailsPanel1ViewModel vm)
        {
            var equip = vm.CurrentEquip.Value;
            if (equip == null)
            {
                return;
            }

            var equipCfg = mEquipCardConfig.GetOrDefault(equip.EquipId);
            if (equipCfg == null)
            {
                LogTool.LogError("[EquipDetailsPanel1New] 装备配置不存在");
                return;
            }

            RefreshHead(vm, equip, equipCfg);
            RefreshProperty(vm, equip, equipCfg);

            RefreshFusionGem(equip);
            InitWearButtonState(vm);
        }

        private void RefreshHead(EquipDetailsPanel1ViewModel vm, PlayerEquip equip, EquipConfig equipCfg)
        {
            if (mOldPanel == null)
            {
                return;
            }

            // SetHeadQualityImg();
            mOldPanel.RewardEntity.interactable = false;
            var itemCfg = GameServiceManager.Get<EquipService>().GetSoldierEquipItemByEquipId(equip.EquipId);
            if (itemCfg == null)
            {
                LogTool.LogError("[EquipDetailsPanel1New] 找不到装备对应道具，equipId=" + equip.EquipId);
                return;
            }

            mOldPanel.RewardEntity.GetComponent<RewardEntity>().SetData(new RewardEntityData
            {
                ItemId = itemCfg.Id,
                EquipLevel = equipCfg.EquipLevel,
                IconSprite = Global.EquipIconSpriteAtlas.GetSprite(equipCfg.Icon),
                Quality = equipCfg.Quality,
                StyleType = RewardStyleType.Equip
            });
            mOldPanel.EquipStyleBg.sprite = Global.EquipBgStyleAtlas.GetSprite(equipCfg.Quality.ToString());
            // SpriteHelper.TryLoadTexture($"Equip{equipCfg.Quality}Frame", mOldPanel.EquipStyleBg);
            mOldPanel.EquipNameTxt.text = equipCfg.Name;
            mOldPanel.QualityTxt.gameObject.GetComponent<LocalizationText>()
                .SetData(LocalizationModule.UITexts, GlobalFunc.GetQualityName(equipCfg.Quality));
            mOldPanel.CombatPowerTxt.text = $"战斗力：{equipCfg.FightNum}";
            var isWear = CheckEquipIsWear(vm, equip);
            SetIsWearImgState(equipCfg.Quality, isWear);
        }

        // private void SetHeadQualityImg(QualityType qualityType)
        // {
        //     if (mOldPanel?.HeadQualityImg == null)
        //     {
        //         return;
        //     }
        //
        //     switch (qualityType)
        //     {
        //         case QualityType.Default:
        //         case QualityType.Ordinary:
        //             mOldPanel.HeadQualityImg.sprite =
        //                 Global.EquipDetailQualityIconAtlas.GetSprite("CommonEquipmentSlot");
        //             break;
        //         case QualityType.Rarity:
        //             mOldPanel.HeadQualityImg.sprite = Global.EquipDetailQualityIconAtlas.GetSprite("RareEquipmentSlot");
        //             break;
        //         case QualityType.Epic:
        //             mOldPanel.HeadQualityImg.sprite = Global.EquipDetailQualityIconAtlas.GetSprite("EpicEquipmentSlot");
        //             break;
        //         case QualityType.Legend:
        //         case QualityType.Ultimate:
        //         case QualityType.Mythology:
        //             mOldPanel.HeadQualityImg.sprite =
        //                 Global.EquipDetailQualityIconAtlas.GetSprite("LegendaryEquipmentSlot");
        //             break;
        //         default:
        //             throw new ArgumentOutOfRangeException(nameof(qualityType), qualityType, null);
        //     }
        // }

        private void SetIsWearImgState(QualityType qualityType, bool isWear)
        {
            if (mOldPanel?.IsWearImg == null)
            {
                return;
            }

            if (!isWear)
            {
                mOldPanel.IsWearImg.gameObject.SetActive(false);
            }
            else
            {
                mOldPanel.IsWearImg.gameObject.SetActive(true);
                string hexColor;
                Color newColor;
                switch (qualityType)
                {
                    case QualityType.Default:
                    case QualityType.Ordinary:
                        hexColor = "#C9C9C9";
                        ColorUtility.TryParseHtmlString(hexColor, out newColor);
                        break;
                    case QualityType.Rarity:
                        hexColor = "#505594";
                        ColorUtility.TryParseHtmlString(hexColor, out newColor);
                        break;
                    case QualityType.Epic:
                        hexColor = "#EF31F7";
                        ColorUtility.TryParseHtmlString(hexColor, out newColor);
                        break;
                    case QualityType.Legend:
                    case QualityType.Ultimate:
                    case QualityType.Mythology:
                        hexColor = "#EFBF00";
                        ColorUtility.TryParseHtmlString(hexColor, out newColor);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(qualityType), qualityType, null);
                }

                mOldPanel.IsWearImg.GetComponent<Image>().color = newColor;
            }
        }

        private bool CheckEquipIsWear(EquipDetailsPanel1ViewModel vm, PlayerEquip equip)
        {
            if (Global.PlayerEquips == null)
            {
                return false;
            }

            var list = Global.PlayerEquips.Values.Where(v => v.WearerId == vm.SoldierId.Value).ToList();
            foreach (var v in list)
            {
                if (v.Id == equip.Id)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshProperty(EquipDetailsPanel1ViewModel _, PlayerEquip equip, EquipConfig equipCfg)
        {


            var attrSums = new System.Collections.Generic.Dictionary<AttributeType, float>();

            int index = 0;
            // foreach (var v in equipCfg.MainAttributes)
            // {
            //     Transform propertyObj = mOldPanel.PropertyBg.transform.GetChild(index);
            //     propertyObj.gameObject.SetActive(true);
            //     SetPropertyBgIcon(propertyObj, v.Type);
            //     SetPropertyTypeTxt(propertyObj, v.Type, v.Value);
            //     SetPropertyBonusTxt(propertyObj, v.Type, v.Value);
            //     index++;
            //
            //     // 累加 ConvertToBaseAttributes，按 AttributeType 合并
            //     var mainAttrCfg = mTbMainAttributeConfig.GetOrDefault(v.Type);
            //     if (mainAttrCfg != null)
            //     {
            //         foreach (var cv in mainAttrCfg.ConvertToBaseAttributes)
            //         {
            //             float bonus = v.Value * cv.Value;
            //             attrSums.TryGetValue(cv.Type, out float cur);
            //             attrSums[cv.Type] = cur + bonus;
            //         }
            //     }
            // }

            // 写入 BaseAttrText
            if (mOldPanel.BaseAttrText != null)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var attr in equipCfg.Attributes)
                {
                    var attrName = LocalizationManager.Instance.GetText(LocalizationModule.UITexts,
                        mTbAttributeConfig.GetOrDefault(attr.Type).Name,$"+{attr.Value:F0}");
                    sb.AppendLine(attrName);
                }
                mOldPanel.BaseAttrText.text = sb.ToString();
            }
        }

        private void SetPropertyBgIcon(Transform obj, MainAttributeType type)
        {
            switch (type)
            {
                case MainAttributeType.Strength:
                    obj.GetComponent<Image>().sprite = Global.CardIconSpriteAtlas.GetSprite("Strength");
                    break;
                case MainAttributeType.Dexterity:
                    obj.GetComponent<Image>().sprite = Global.CardIconSpriteAtlas.GetSprite("Dexterity");
                    break;
                case MainAttributeType.Intelligence:
                    obj.GetComponent<Image>().sprite = Global.CardIconSpriteAtlas.GetSprite("Intelligence");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private void SetPropertyTypeTxt(Transform obj, MainAttributeType type, float value)
        {
            Transform objTxt = obj.Find("PropertyTypeTxt");
            switch (type)
            {
                case MainAttributeType.Strength:
                    objTxt.GetComponent<TextMeshProUGUI>().text = $"力量   {value}";
                    break;
                case MainAttributeType.Dexterity:
                    objTxt.GetComponent<TextMeshProUGUI>().text = $"敏捷   {value}";
                    break;
                case MainAttributeType.Intelligence:
                    objTxt.GetComponent<TextMeshProUGUI>().text = $"智力   {value}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private void SetPropertyBonusTxt(Transform obj, MainAttributeType type, float value)
        {
            var mainAttributeConfig = mTbMainAttributeConfig.GetOrDefault(type);
            Transform objTxt = obj.Find("PropertyScrollView/Viewport/Content");
            Transform objChild;
            int j = 0;
            foreach (var v in mainAttributeConfig.ConvertToBaseAttributes)
            {
                objChild = objTxt.GetChild(j);
                objChild.gameObject.SetActive(true);
                string typeName = mTbAttributeConfig.GetOrDefault(v.Type).Name;
                objChild.Find("PropertyNum").GetComponent<LocalizationText>()
                    .SetData(LocalizationModule.UITexts, typeName, $"+{value * v.Value}");
                j++;
            }
        }


        private void RefreshFusionGem(PlayerEquip equip)
        {
            var gemPanel = mOldPanel?.FusionGemPanel?.transform;
            if (gemPanel == null) return;

            var gems = Global.PlayerFusionGems?.Values
                .Where(g => g.WearerId == equip.Id)
                .ToList() ?? new List<PlayerFusionGem>();

            // 兜底补齐 Config（部分数据可能 Config 为空），避免 Icon/Name 空引用
            foreach (var gem in gems)
            {
                if (gem.Config == null && mTbGemFusionConfig != null && gem.GemId > 0)
                {
                    gem.Config = mTbGemFusionConfig.GetOrDefault(gem.GemId);
                }
            }

            // 清除旧的 UnWear 按钮监听
            ClearUnWearGemHandlers();

            for (int i = 0; i < gemPanel.childCount; i++)
            {
                var slot = gemPanel.GetChild(i);
                // var locked = slot.Find("Locked");
                // var unlocked = slot.Find("UnLocked");

                var gemIcon = slot.Find("UnLocked/GemIcon");
                var nameText = slot.Find("UnLocked/Text");
                var unwearBtn = slot.Find("UnLocked/UnWearBtn");

                if (i < gems.Count)
                {
                    var gem = gems[i];
                    // if (locked != null) locked.gameObject.SetActive(false);
                    // if (unlocked != null) unlocked.gameObject.SetActive(true);

                    if (gemIcon != null)
                    {
                        gemIcon.gameObject.SetActive(true);
                        var img = gemIcon.GetComponent<Image>();
                        if (img != null)
                            img.sprite = Global.FusionGemIconSpriteAtlas?.GetSprite(gem.Config?.Icon);
                    }

                    if (nameText != null)
                    {
                        var tmp = nameText.GetComponent<TMP_Text>();
                        if (tmp != null)
                            tmp.text = gem.Config?.Name ?? string.Empty;
                    }

                    // 绑定卸下按钮
                    if (unwearBtn != null)
                    {
                        unwearBtn.gameObject.SetActive(true);
                        var btn = unwearBtn.GetComponent<Button>();
                        if (btn != null)
                        {
                            var gemId = gem.Id;
                            UnityEngine.Events.UnityAction handler = () => UnWearGem(gemId);
                            btn.onClick.AddListener(handler);
                            mUnWearGemBtns.Add(btn);
                            mUnWearGemHandlers.Add(handler);
                        }
                    }
                }
                else
                {
                    // if (locked != null) locked.gameObject.SetActive(false);
                    // if (unlocked != null) unlocked.gameObject.SetActive(true);

                    if (gemIcon != null) gemIcon.gameObject.SetActive(false);
                    if (unwearBtn != null) unwearBtn.gameObject.SetActive(false);

                    if (nameText != null)
                    {
                        var tmp = nameText.GetComponent<TMP_Text>();
                        if (tmp != null)
                            tmp.text = "未佩戴融合石";
                    }
                }
            }
        }

        private async void UnWearGem(int gemId)
        {
            var service = GameServiceManager.Get<EquipService>();
            if (service == null) return;

            CombatPowerChangeHelper.CaptureBaseline();

            var result = await service.WearFusionGemAsync(gemId, 0);
            if (result != null && result.Success)
            {
                CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
                Global.Publisher.Publish(MessagePublisher.FUSION_GEM_WEAR_CHANGED, gemId);
                if (VM != null)
                {
                    RefreshFusionGem(VM.CurrentEquip.Value);
                }
            }
            else
            {
                Toast.Error("卸下" + (result?.Message ?? "失败"));
            }
        }

        private void ClearUnWearGemHandlers()
        {
            for (int i = 0; i < mUnWearGemBtns.Count && i < mUnWearGemHandlers.Count; i++)
            {
                if (mUnWearGemBtns[i] != null && mUnWearGemHandlers[i] != null)
                {
                    mUnWearGemBtns[i].onClick.RemoveListener(mUnWearGemHandlers[i]);
                }
            }
            mUnWearGemBtns.Clear();
            mUnWearGemHandlers.Clear();
        }

        private void InitWearButtonState(EquipDetailsPanel1ViewModel vm)
        {
            if (mOldPanel == null)
            {
                return;
            }

            var equip = vm.CurrentEquip.Value;
            if (equip == null)
            {
                return;
            }

            var equipCfg = mEquipCardConfig.GetOrDefault(equip.EquipId);
            if (equipCfg == null)
            {
                return;
            }

            var wearBtnText = "替换";
            if (equip.WearerId != 0)
            {
                if (equip.WearerId == vm.SoldierId.Value)
                {
                    wearBtnText = "卸下";
                }
            }
            else
            {
                var wearEquips = Global.PlayerEquips.Values
                    .Where(v => v.WearerId == vm.SoldierId.Value && v.PartType == equipCfg.Part)
                    .ToList();
                if (wearEquips.Count == 0)
                {
                    wearBtnText = "佩戴";
                }
            }

            mOldPanel.WearBtn.transform.Find("Text").GetComponent<TMP_Text>().text = wearBtnText;
            mOldPanel.WearBtn.interactable = true;
            mOldPanel.WearBtn.gameObject.SetActive(equip.WearerId != vm.SoldierId.Value);
            mOldPanel.UpGradeBtn.gameObject.SetActive(equip.WearerId == vm.SoldierId.Value);
            RegisterEquipGuideTargetsDelayed().Forget();
        }
    }
}
