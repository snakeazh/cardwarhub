#define DISABLE_SKILL_CONTENT

using Game.Soldier;
using Game.Soldier.RedDot;
using Game.RedDot;
using Game.RedDot.Rules;
using Game.RedDot.View;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;
using UIFramework;
using Config;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Cysharp.Threading.Tasks;
using FusionContinent.Model;
using FusionContinent.Helper;
using FusionContinent.Utils;
using FusionContinent.Animation;
using FusionContinent.Attribute;
using FusionContinent.Manager;
using FusionContinent.Sounds;
using Game.Equip;
using Game.Player;
using Game.Team;
using Manager;
using ScrollView;
using UIFramework.Services;
using UnityEngine.UI;
using UILevel = QFramework.UILevel;
using UIManager = UIFramework.UIManager;

namespace FusionContinent
{
    /// <summary>
    /// 士兵/英灵界面 - 新框架版本 (MVVM)
    /// 复用原 SoldierPanel 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("SoldierPanel")]
    public class SoldierPanelNew : UIFramework.SubPanelManager<SoldierPanelViewModel>
    {
        private const int OpenPanelNone = -1;
        private const int OpenPanelSkill = 1;
        private const int OpenPanelEquip = 2;
        private const int OpenPanelFusionGem = 3;

        // 引用原 Panel，复用其 UI 组件
        private SoldierPanel mOldPanel;

        // 升级红点 scope：驱动 UpGradeBtn（Current/Upgrade → 当前选中士兵）。
        // 与 SoldierMainPanelNew 共享同名 scope，引用计数管理生命周期。
        private readonly SoldierPanelRedDotScope _redDotScope = new SoldierPanelRedDotScope();
        
        // 动画
        private Tween mAnimImage;
        private Tween mAnimLayout;
        private System.Action<int, int> mOpenPanelValueChangedHandler;
        AttributeSet SoliderAttributeSet;

        // 技能槽位缓存
        private List<SkillItemView> mSkillItems = new List<SkillItemView>();
        private bool _isClosing;
        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<SoldierPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[SoldierPanelNew] 找不到 SoldierPanel 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            
            // SoldierPanel 作为子界面管理器，默认挂在自身节点下复用子界面
            mSubPanelContainer = transform;

            LogTool.Log("[SoldierPanelNew] 已关联 SoldierPanel");
        }

        protected override void OnBindViewModel(SoldierPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 激活升级红点 scope，UpGradeBtn 绑当前选中士兵的升级红点（Current/Upgrade）
            _redDotScope.OpenShared(SoldierRedDotHelper.GetSoldierCardIds());
            if (vm.CurrentCard.Value != null)
            {
                _redDotScope.SetSelectedCard(vm.CurrentCard.Value.CardId);
            }
            if (mOldPanel.UpGradeBtn != null)
            {
                mOldPanel.UpGradeBtn.BindLocalRedDot(_redDotScope, RedNodeType.SoliderUpgradeCard);
            }

            if (mOldPanel.AwakeBtn != null)
            {
                mOldPanel.AwakeBtn.BindLocalRedDot(_redDotScope, RedNodeType.SoliderAwakeningCard);
            }

            // 绑定属性
            BindProperties(vm);
            
            // 绑定 ViewModel 事件（所有全局事件订阅统一在 ViewModel 中处理）
            vm.OnSoldierChanged += OnSoldierChanged;
            vm.OnSkinChanged += OnSkinChanged;
            vm.OnUpgradeSuccess += OnUpgradeSuccess;
            vm.OnUpgradeFailed += OnUpgradeFailed;
            vm.OnAwakeningRequested += OnAwakeningRequested;
            vm.OnDevelopRequested += OnDevelopRequested;
            vm.OnEquipRequested += OnEquipRequested;
            vm.OnSkinRequested += OnSkinRequested;
            vm.OnFusionGemRequested += OnFusionGemRequested;
            vm.OnMainAttributesRequested += OnMainAttributesRequested;
            vm.OnBaseAttributesRequested += OnBaseAttributesRequested;
            vm.OnSkillDataChanged += OnSkillDataChanged; // 技能数据变化时刷新显示
            vm.OnCardSkinChanged += OnCardSkinChanged; // 皮肤变更时刷新显示
            Global.Publisher.Subscribe(MessagePublisher.TEAM_HERO_SKILL_WEAR_CHANGED, OnTeamHeroSkillWearChanged);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_ITEM_CHANGE, OnItemChanged);

            // 初始化按钮
            InitButtons(vm);

            // 同步 BindableProperty 初始值（OnValueChanged 不会在订阅时重放当前值）
            if (mOldPanel.UpGradeBtn != null)
                mOldPanel.UpGradeBtn.interactable = vm.IsUpgradeBtnInteractable.Value;

            // 初始化按钮选中状态
            UpdateBtnSelectedState(vm.CurrentOpenPanelValue.Value);
        }

        protected override void OnUnbindViewModel(SoldierPanelViewModel vm)
        {
            // 取消 ViewModel 事件订阅
            vm.OnSoldierChanged -= OnSoldierChanged;
            vm.OnSkinChanged -= OnSkinChanged;
            vm.OnUpgradeSuccess -= OnUpgradeSuccess;
            vm.OnUpgradeFailed -= OnUpgradeFailed;
            vm.OnAwakeningRequested -= OnAwakeningRequested;
            vm.OnDevelopRequested -= OnDevelopRequested;
            vm.OnEquipRequested -= OnEquipRequested;
            vm.OnSkinRequested -= OnSkinRequested;
            vm.OnFusionGemRequested -= OnFusionGemRequested;
            vm.OnMainAttributesRequested -= OnMainAttributesRequested;
            vm.OnBaseAttributesRequested -= OnBaseAttributesRequested;
            vm.OnSkillDataChanged -= OnSkillDataChanged;
            vm.OnCardSkinChanged -= OnCardSkinChanged;
            Global.Publisher.Unsubscribe(MessagePublisher.TEAM_HERO_SKILL_WEAR_CHANGED, OnTeamHeroSkillWearChanged);
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_ITEM_CHANGE, OnItemChanged);

            if (mOpenPanelValueChangedHandler != null)
            {
                vm.CurrentOpenPanelValue.OnValueChanged -= mOpenPanelValueChangedHandler;
                mOpenPanelValueChangedHandler = null;
            }
            
            // 清理动画
            mAnimImage?.Kill();
            mAnimLayout?.Kill();

            // 关闭升级红点 scope（引用计数归零才真正销毁）
            _redDotScope.CloseShared();
        }

        #region 属性绑定

        private void BindProperties(SoldierPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 当前士兵变化
            vm.CurrentCard.OnValueChanged += (oldVal, newVal) =>
            {
                if (newVal != null)
                {
                    SetSoldierData(newVal);
                    // 切换士兵时刷新升级红点（UpGradeBtn 绑定的是当前选中士兵）
                    _redDotScope.SetSelectedCard(newVal.CardId);
                }
            };



            // 升级按钮状态
            vm.IsUpgradeBtnInteractable.OnValueChanged += (oldVal, newVal) =>
            {
                if (mOldPanel.UpGradeBtn != null)
                    mOldPanel.UpGradeBtn.interactable = newVal;
            };

            // 皮肤变化
            vm.CurrentSkinId.OnValueChanged += (oldVal, newVal) =>
            {
                SetSkin(newVal);
            };

            // [已注释] 消耗品多选功能：不再监听勾选状态变化
            // vm.ConsumeItems.OnValueChanged += (oldVal, newVal) =>
            // {
            //     RefreshConsumes(newVal);
            // };
            



            // 技能筛选变化（切换显示技能卡图鉴或奥术卡图鉴）
            vm.CurrentSkillFilter.OnValueChanged += (oldVal, newVal) =>
            {
                // UpdateSkillFilterUI(newVal);
            };

            mOpenPanelValueChangedHandler = (oldVal, newVal) =>
            {
                if (newVal == OpenPanelNone) return;
                SwitchPanelByValue(newVal);
            };
            vm.CurrentOpenPanelValue.OnValueChanged += mOpenPanelValueChangedHandler;

            // 奥术卡品质筛选变化
            // vm.GeneralSkillQualityFilters.OnValueChanged += (oldVal, newVal) =>
            // {
            //     UpdateGeneralSkillQualityFilterUI(newVal);
            // };
        }

        #endregion

        #region 面板切换值绑定

        private void SwitchPanelByValue(int panelValue)
        {
            var currentCard = VM?.CurrentCard.Value;
            if (currentCard == null) return;

            UpdateBtnSelectedState(panelValue);

            switch (panelValue)
            {
                case OpenPanelSkill:
                    CloseSubPanel<EquipPanelNew>();
                    if (mOldPanel.SkillModule != null)
                        mOldPanel.SkillModule.gameObject.SetActive(true);
                    VM?.OnChangeSkillFilterCommand.Execute(CardType.Skill);
                    break;
                case OpenPanelEquip:
                    // if (mOldPanel.SkillModule != null)
                    //     mOldPanel.SkillModule.gameObject.SetActive(false);
                    // var vm = new Game.Equip.EquipPanelViewModel();
                    // vm.OnCreate();
                    //
                    // // 先初始化数据，再打开界面
                    // vm.Initialize(currentCard.CardId);
                    // OpenOrReuseSubPanel<EquipPanelNew,Game.Equip.EquipPanelViewModel>(vm);
                    // OnEquipRequested(currentCard.CardId);
                    break;
                case OpenPanelFusionGem:
                    OnFusionGemRequested(currentCard.CardId);
                    break;
                default:
                    LogTool.LogWarning($"[SoldierPanelNew] 未识别的 CurrentOpenPanelValue: {panelValue}");
                    break;
            }
        }

        private void UpdateBtnSelectedState(int panelValue)
        {
            bool isEquip = panelValue == OpenPanelEquip;
            SetBtnSelected(mOldPanel?.SoldierBtn, !isEquip);
            SetBtnSelected(mOldPanel?.EquipBtn, isEquip);
        }

        private static void SetBtnSelected(Button btn, bool isSelected)
        {
            if (btn == null) return;
            var selected = btn.transform.Find("Selected")?.gameObject;
            var unSelected = btn.transform.Find("UnSelected")?.gameObject;
            if (selected != null) selected.SetActive(isSelected);
            if (unSelected != null) unSelected.SetActive(!isSelected);
        }

        #endregion

        #region 初始化按钮

        private void InitButtons(SoldierPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 装备按钮
            if (mOldPanel.EquipBtn != null)
            {
                mOldPanel.EquipBtn.onClick.RemoveAllListeners();
                mOldPanel.EquipBtn.onClick.AddListener(() =>
                {
                    vm.OnEquipCommand.Execute();
            
                });
            }

            // 士兵按钮（关闭装备界面，恢复SkillModule）
            if (mOldPanel.SoldierBtn != null)
            {
                mOldPanel.SoldierBtn.onClick.RemoveAllListeners();
                mOldPanel.SoldierBtn.onClick.AddListener(() =>
                {
                    this.VM.CurrentOpenPanelValue.Value = SoldierPanelNew.OpenPanelSkill;

                    // 关闭装备子面板
                    // UIFramework.UIManager.ClosePanel<EquipPanelNew>();
                    // if (mOldPanel.SkillModule != null)
                        // mOldPanel.SkillModule.gameObject.SetActive(true);
                });
            }

            // 皮肤按钮
            if (mOldPanel.SkinBtn != null)
            {
                // mOldPanel.SkinBtn.onClick.RemoveAllListeners();
                // mOldPanel.SkinBtn.onClick.AddListener(() => vm.OnSkinCommand.Execute());
                mOldPanel.SkinBtn.onClick.AddListener(() => Toast.Show("未开放"));
            }

            // 觉醒按钮
            if (mOldPanel.AwakeBtn != null)
            {
                mOldPanel.AwakeBtn.onClick.RemoveAllListeners();
                mOldPanel.AwakeBtn.onClick.AddListener(() => vm.OnAwakeningCommand.Execute());
            }

            // 融合石按钮
            // if (mOldPanel.FusionGemBtn != null)
            // {
            //     mOldPanel.FusionGemBtn.onClick.RemoveAllListeners();
            //     mOldPanel.FusionGemBtn.onClick.AddListener(() => vm.OnFusionGemCommand.Execute());
            // }

            // 主属性按钮
            if (mOldPanel.MainAttributes != null)
            {
                mOldPanel.MainAttributes.onClick.RemoveAllListeners();
                mOldPanel.MainAttributes.onClick.AddListener(() => vm.OnMainAttributesCommand.Execute());
            }

            // 基础属性按钮
            if (mOldPanel.PlayerCoreAttr != null)
            {
                var baseAttrBtn = mOldPanel.PlayerCoreAttr;
                if (baseAttrBtn != null)
                {
                    baseAttrBtn.onClick.RemoveAllListeners();
                    baseAttrBtn.onClick.AddListener(() => vm.OnBaseAttributesCommand.Execute());
                }
            }

            // 升级按钮
            if (mOldPanel.UpGradeBtn != null)
            {
                mOldPanel.UpGradeBtn.onClick.RemoveAllListeners();
                mOldPanel.UpGradeBtn.onClick.AddListener(OnUpgradeBtnClicked);
            }

#if !DISABLE_SKILL_CONTENT
            // 技能按钮
            if (mOldPanel.SkillBtn != null)
            {
                mOldPanel.SkillBtn.onClick.RemoveAllListeners();
                mOldPanel.SkillBtn.onClick.AddListener(() => vm.OnChangeSkillFilterCommand.Execute(CardType.Skill));
            }
#endif



            // 关闭按钮
            if (mOldPanel.CloseBtn != null)
            {
                mOldPanel.CloseBtn.onClick.RemoveAllListeners();
                mOldPanel.CloseBtn.onClick.AddListener(CloseWithAnimation);
            }

            // Mask 按钮（点击遮罩关闭）
            if (mOldPanel.Mask != null)
            {
                mOldPanel.Mask.onClick.RemoveAllListeners();
                mOldPanel.Mask.onClick.AddListener(CloseWithAnimation);
            }

        }
        #endregion

        #region 数据设置

        private void SetSoldierData(PlayerCard cardInfo)
        {
            if (mOldPanel == null || cardInfo == null) return;

            // 刷新最新数据
            var playerCard = Global.GetCardById(cardInfo.CardId);
            if (playerCard != null)
            {
                cardInfo = playerCard;
            }
            
            if (SoliderAttributeSet == null)
            {
                SoliderAttributeSet = new AttributeSet();
            }
            SoliderAttributeSet.ClearMetaData();
      
            SoliderAttributeSet.InitFromConfig(cardInfo.CardId,cardInfo.Lv);
            SoliderAttributeSet.RemoveMainAttribute("talentMain",AttributeSourceType.Talent);
            SoliderAttributeSet.RemoveAttributeSource(AttributeSourceType.Talent,"talentAttr");

            var talentAttr = Talent.GetAttributes();
            if (talentAttr!= null)
            {
                SoliderAttributeSet.ApplyMainAttribute("talentMain",talentAttr.MainAttributes,AttributeSourceType.Talent);
                SoliderAttributeSet.ApplyAttributeSource(AttributeSourceType.Talent,"talentAttr",talentAttr.SubAttributes);
            }

            SoliderAttributeSet.RemoveMainAttribute("EquipMain",AttributeSourceType.Equip);
            SoliderAttributeSet.RemoveAttributeSource(AttributeSourceType.Equip,"EquipAttr");
            var equipAttr = Equip.GetAttributes(DataConfigManager.Tables.TbPlayerConst.GlobalUserWearId);
            if (equipAttr!= null)
            {
                SoliderAttributeSet.ApplyMainAttribute("EquipMain",equipAttr.MainAttributes,AttributeSourceType.Equip);
                SoliderAttributeSet.ApplyAttributeSource(AttributeSourceType.Equip,"EquipAttr",equipAttr.SubAttributes);
            }
            // SoliderAttributeSet.ApplyAttributeSource(AttributeSourceType.Skin,"soldier_skin",skinCfg.Attributes);
            // 设置皮肤
            SetSkin(cardInfo.ObjData.Skin);
            // 设置属性
            SetMainAttributes(cardInfo);
            // 设置技能
            VM.UpdateSkillList();

            // 设置升级
            SetUpgrade(cardInfo);


            // 检查觉醒
            CheckAwakingLevel(cardInfo);

            // 设置核心属性数值（攻击力/攻速/生命值/攻击距离）
            SetPlayerCoreAttributes(cardInfo);

            // 刷新技能槽位显示
            RefreshSkillSlot(cardInfo);

            // 刷新已穿戴战斗技能槽位
            RefreshBattleSkillSlots(cardInfo.CardId);
        }

        private void SetSkin(int skinId)
        {
            if (mOldPanel == null || VM.CurrentCard.Value == null) return;

            var cardInfo = VM.CurrentCard.Value;
            var soldierCfg = VM.GetSoldierConfig(cardInfo.CardId);
            if (soldierCfg == null) return;

            var bigIcon = cardInfo.Config.Icon;
            var spineName = soldierCfg.Spine;
            var soldierName = cardInfo.Config.Name;
            var soldierName2 = soldierCfg.Name;
       
            SoliderAttributeSet.RemoveAttributeSource(AttributeSourceType.Skin,"soldier_skin");
            if (skinId != 0)
            {
                var skinCfg = VM.GetSkinConfig(skinId);
                if (skinCfg != null)
                {
                    bigIcon = skinCfg.Icon;
                    spineName = skinCfg.Spine;

                    SoliderAttributeSet.ApplyAttributeSource(AttributeSourceType.Skin,"soldier_skin",skinCfg.Attributes);
                }
            }

            // 设置基础信息
            SpriteHelper.TryLoadPaintAsync(bigIcon, mOldPanel.BigIcon, EPaintType.Large, false);
            mOldPanel.SoldierName.text = soldierName;
            mOldPanel.SoldierName2.text = soldierName2;
            mOldPanel.MainAttrTypeIcon.sprite = Global.AttributeIconAtlas.GetSpriteExt(soldierCfg.MainAttrType.ToString());
            mOldPanel.TypeIcon.sprite = Global.CareerTypeIconAtlas.GetSpriteExt(soldierCfg.CarType.ToString());
            // Spine
            for (int i = 0; i < mOldPanel.Spine.childCount; i++)
                Destroy(mOldPanel.Spine.GetChild(i).gameObject);

            if (!string.IsNullOrEmpty(spineName))
            {
                var soliderSpine = Global.ResLoader.LoadSync<GameObject>(spineName).Instantiate().transform;
                soliderSpine.SetParent(mOldPanel.Spine);
                soliderSpine.localScale = Vector3.one;
                soliderSpine.localPosition = Vector3.zero;
                mOldPanel.Spine.localScale = soldierCfg.SpineScale > 0 ? Vector3.one * soldierCfg.SpineScale : Vector3.one;
            }

            // mOldPanel.SpineShade.gameObject.SetActive(!string.IsNullOrEmpty(spineName));
        }

        private void SetUpgrade(PlayerCard cardInfo)
        {
            if (mOldPanel == null) return;

            // 默认隐藏满级提示，仅在满级时显示
            mOldPanel.IsFullLevel.gameObject.SetActive(false);

            var developCfg = VM.GetDevelopConfig(cardInfo.CardId);
            if (developCfg == null) return;

            // 设置等级
            mOldPanel.ExpProgressBar.Find("Level/Text")?.GetComponent<TMP_Text>()?.SetText($"{cardInfo.Lv}");

            // 判断是否解锁
            mOldPanel.UpGrade.gameObject.SetActive(cardInfo.IsHave);

            // 设置经验
            if (cardInfo.Lv < developCfg.LevelPropertyList.Count)
            {
                var lvCfg = developCfg.LevelPropertyList[cardInfo.Lv - 1];
                var slider = mOldPanel.ExpProgressBar.Find("Slider")?.GetComponent<Slider>();
                if (slider != null)
                    slider.value = Mathf.Clamp01((float)cardInfo.Exp / lvCfg.Exp);
                mOldPanel.ExpProgressBar.Find("Progress/Text")?.GetComponent<TMP_Text>()?.SetText($"{cardInfo.Exp}/{lvCfg.Exp}");
                mOldPanel.ExpProgressBar.gameObject.SetActive(true);
                mOldPanel.UpGradeBtn.gameObject.SetActive(true);
                mOldPanel.IsFullLevel.gameObject.SetActive(false);
            }
            else
            {
                mOldPanel.ExpProgressBar.Find("Progress/Text")?.GetComponent<TMP_Text>()?.SetText("MAX");
                mOldPanel.ExpProgressBar.gameObject.SetActive(false);
                mOldPanel.UpGradeBtn.gameObject.SetActive(false);
                mOldPanel.IsFullLevel.gameObject.SetActive(true);
            }

            // mOldPanel.LevelText.text = $"{developCfg.LevelPropertyList[cardInfo.Lv - 1].Name}";
            mOldPanel.LevelText.text = $"Lv.{cardInfo.Lv}";
            
            // 刷新消耗品显示
            RefreshConsumes();
        }

        Dictionary<MainAttributeType, float> GetAttributeValues(SoldierLevelPropertyList soldierLevelPropertyList,int cardId)
        {
            return SoliderAttributeSet.GetAllMainAttributeValues();
            // Dictionary<MainAttributeType, float> ValueKay = new Dictionary<MainAttributeType, float>();
            // foreach (var mainAttribute in soldierLevelPropertyList.MainAttributes)
            // {
            //     ValueKay.Add(mainAttribute.Type, mainAttribute.Value);
            // }

         

            // return ValueKay;
        }
        // Talent.GetAttributes(), // 获取天赋属性
        // Equip.GetAttributes(),  // 获取装备属性
        private Dictionary<MainAttributeType, float> CurrentMainAttributeValues =
            new Dictionary<MainAttributeType, float>();
        private void SetMainAttributes(PlayerCard cardInfo)
        {
            if (mOldPanel == null) return;

            var soldierCfg = VM.GetSoldierConfig(cardInfo.CardId);
            if (soldierCfg == null) return;

            var soldierLvCfg = soldierCfg.LevelPropertyList[cardInfo.Lv - 1];

            // 设置属性值
            for (int i = 0; i < mOldPanel.MainAttributes.transform.childCount; i++)
                mOldPanel.MainAttributes.transform.GetChild(i).gameObject.SetActive(false);
            CurrentMainAttributeValues = GetAttributeValues(soldierLvCfg,cardInfo.CardId);
            for (int i = 0; i < soldierLvCfg.MainAttributes.Count; i++)
            {
                var attrTran = mOldPanel.MainAttributes.transform.childCount > i
                    ? mOldPanel.MainAttributes.transform.GetChild(i)
                    : Instantiate(mOldPanel.MainAttributes.transform.GetChild(0), mOldPanel.MainAttributes.transform);

                attrTran.gameObject.SetActive(true);
                var mainAttr = soldierLvCfg.MainAttributes[i];
                var mainAttrCfg = DataConfigManager.Tables.TbMainAttributeConfig.GetOrDefault(mainAttr.Type);
                attrTran.Find("AttributeIcon/Icon").GetComponent<Image>().sprite = (Global.AttributeIconAtlas.GetSprite(mainAttrCfg.Icon));
                attrTran.Find("NumText").GetComponent<TMP_Text>().color =(soldierCfg.MainAttrType == mainAttr.Type ? Color.yellow : Color.white);
                attrTran.Find("NumText")?.GetComponent<RollingNumberManager>()?.Play(CurrentMainAttributeValues[mainAttr.Type]);
            }
        }

        private void CheckAwakingLevel(PlayerCard cardInfo)
        {
            if (mOldPanel == null) return;

            bool canAwakening = VM.CanAwakening(cardInfo, out int awakeningLevel);
            bool isMaxLevel = VM.GetDevelopConfig(cardInfo.CardId) != null &&
                              cardInfo.Lv >= VM.GetDevelopConfig(cardInfo.CardId).LevelPropertyList.Count;

            // 可觉醒时：显示觉醒提示，隐藏升级相关和满级提示
            // 满级时：隐藏升级相关，显示满级提示
            // 其他：正常显示升级相关
            mOldPanel.UpGradeBtn.gameObject.SetActive(!canAwakening && !isMaxLevel);
            mOldPanel.ExpProgressBar.gameObject.SetActive(!canAwakening && !isMaxLevel);
            mOldPanel.UpgradeTips.gameObject.SetActive(canAwakening);
            mOldPanel.IsFullLevel.gameObject.SetActive(!canAwakening && isMaxLevel);
        }

        /// <summary>
        /// 设置 PlayerCoreAttr 下 6 个 AttrData 的 Num 文本：
        /// 固定顺序：生命值、攻击力、攻击速度、攻击间隔、攻击距离、暴击值
        /// </summary>
        private void SetPlayerCoreAttributes(PlayerCard cardInfo)
        {
            if (mOldPanel == null || mOldPanel.PlayerCoreAttr == null) return;
            var coreAttrTrans = mOldPanel.PlayerCoreAttr.transform;
            var soldierCfg = VM.GetSoldierConfig(cardInfo.CardId);
            if (soldierCfg == null) return;

            float attackTime = 1;
            for (int i = 0; i < DataConfigManager.Tables.TbGameBattleConst.AttackSpeedGapOfCarType.Count; i++)
            {
                if (DataConfigManager.Tables.TbGameBattleConst.AttackSpeedGapOfCarType[i].Type == soldierCfg.CarType)
                {
                    attackTime = DataConfigManager.Tables.TbGameBattleConst.AttackSpeedGapOfCarType[i].Count;
                }
            }
            var nextAttackTime = Formula.AttackInterval(SoliderAttributeSet.GetFinal(Config.AttributeType.SoldierNumOfAttackSpeed),  DataConfigManager.Tables.TbGameBattleConst.AttackSpeedBase,
                DataConfigManager.Tables.TbGameBattleConst.AfkTimeLimitMax);
            nextAttackTime = nextAttackTime / attackTime;
            // 生命值、攻击力、攻击速度
            var defaultValues = new[] {
                SoliderAttributeSet.GetFinal(Config.AttributeType.SoldierMaxHealth),
                SoliderAttributeSet.GetFinal(Config.AttributeType.SoldierAttackPower),
                nextAttackTime };
            for (int i = 0; i < coreAttrTrans.childCount && i < 6; i++)
            {
                var numText = coreAttrTrans.GetChild(i).Find("Num")?.GetComponent<TMP_Text>();
                if (numText == null) continue;

                switch (i)
                {
                    case 0: // 生命值
                    case 1: // 攻击力
                    case 2: // 攻击速度
                        numText.text = defaultValues[i].ToString("F0");
                        break;
                    case 3: // 攻击间隔
                        numText.text = SoliderAttributeSet.GetFinal(Config.AttributeType.SoldierAttackInterval).ToString("F1") + "秒";
                        break;
                    case 4: // 攻击距离
                        numText.text = SoliderAttributeSet.GetFinal(Config.AttributeType.SoldierAttackRange).ToString("F0");
                        break;
                    case 5: // 暴击值(暴击伤害) + 暴击率
                        var critDmg = SoliderAttributeSet.GetFinal(Config.AttributeType.SoldierCriticalDamage);
                        var critRate = SoliderAttributeSet.GetFinal(Config.AttributeType.SoldierCriticalRate);
                        numText.text = $"{critDmg:F0}({critRate:F0}%)";
                        break;
                }
            }
        }

        /// <summary>
        /// 刷新角色技能槽位显示（使用 SkillItem 预制体）
        /// </summary>
        private void RefreshSkillSlot(PlayerCard cardInfo)
        {
            if (mOldPanel == null || mOldPanel.Skill == null) return;
            if (cardInfo == null) return;

            var soldierService = GameServiceManager.Get<Game.Soldier.SoldierService>();
            if (soldierService == null) return;

            var skillConfigs = soldierService.GetSkillConfigsBySoldierId(cardInfo.CardId);

            // 确保有足够的 SkillItem
            EnsureSkillItemCount(5);

            for (int i = 0; i < mSkillItems.Count; i++)
            {
                var item = mSkillItems[i];
                var sortType = GetSkillSortType(i);
                SpecialEffectsConfig skillCfg = null;

                if (sortType != null)
                {
                    skillCfg = soldierService.GetSoldierSkillConfig(cardInfo.CardId, sortType);
                }

                var slotIndex = i;
                item.SetData(cardInfo.CardId, skillCfg, cardInfo.Lv, (skillId) => OnSkillSlotClick(cardInfo.CardId, slotIndex));

                // 英灵技能引导第3步：第二个技能槽
                if (i == 1 && UnlockGuideManager.Instance != null)
                {
                    UnlockGuideManager.Instance.SetUnlockTypeStep(
                        ModuleFunctionType.SoldierSkillAtlas,
                        3,
                        item.GetComponent<RectTransform>());
                }
            }
        }

        private void RefreshBattleSkillSlots(int soldierId)
        {
            if (mOldPanel?.BringBattleSkillSlot == null) return;

            var teamService = GameServiceManager.Get<TeamService>();
            if (teamService == null) return;

            var battleSkillSlots = mOldPanel.BringBattleSkillSlot.GetComponentsInChildren<SkillAddSlot>(true);
            if (battleSkillSlots == null || battleSkillSlots.Length == 0) return;

            for (int i = 0; i < battleSkillSlots.Length && i < 2; i++)
            {
                var skillId = teamService.GetSoliderSkillSlotIndex(soldierId, i);
                battleSkillSlots[i].SetData(soldierId, skillId);
            }
        }

        private void EnsureSkillItemCount(int count)
        {
            if (mOldPanel?.Skill == null) return;

            var skillParent = mOldPanel.Skill;

            // 复用已有子节点上的 SkillItemView 组件
            for (int i = mSkillItems.Count; i < Mathf.Min(count, skillParent.childCount); i++)
            {
                var item = skillParent.GetChild(i).GetComponent<SkillItemView>();
                if (item == null)
                {
                    LogTool.LogError($"[SoldierPanelNew] Skill 子节点 {i} 未挂载 SkillItemView");
                    return;
                }
                mSkillItems.Add(item);
            }

            // 隐藏多余的
            for (int i = 0; i < mSkillItems.Count; i++)
            {
                mSkillItems[i].gameObject.SetActive(i < count);
            }
        }

        /// <summary>
        /// 根据槽位索引获取对应的技能排序类型
        /// </summary>
        private SoldierSkillSortType GetSkillSortType(int slotIndex)
        {
            return slotIndex switch
            {
                0 => SoldierSkillSortType.PassiveSkills,     // 被动技能
                1 => SoldierSkillSortType.AbilitySkills1,   // 主动技能1
                2 => SoldierSkillSortType.AbilitySkills2,   // 主动技能2
                3 => SoldierSkillSortType.AbilitySkills3,   // 主动技能3
                4 => SoldierSkillSortType.AwakingSkills,    // 觉醒技能
                _ => (SoldierSkillSortType)(-1)
            };
        }

        /// <summary>
        /// 技能槽位点击回调
        /// </summary>
        private async void OnSkillSlotClick(int cardId, int slotIndex)
        {
            var soldierService = GameServiceManager.Get<Game.Soldier.SoldierService>();
            if (soldierService == null) return;

            var sortType = GetSkillSortType(slotIndex);
            if (sortType == (SoldierSkillSortType)(-1)) return;

            var skillCfg = soldierService.GetSoldierSkillConfig(cardId, sortType);
            if (skillCfg == null) return;

            var cardInfo = VM?.CurrentCard.Value;
            if (cardInfo == null) return;

            // 觉醒技能未激活：统一提示，不展示角色等级解锁文案
            if (skillCfg.AwakingSkill == 1)
            {
                var awakingSkillCard = Global.GetCardById(skillCfg.Id);
                if (awakingSkillCard == null || !awakingSkillCard.IsHave)
                {
                    FusionContinent.Toast.Show("英灵觉醒后激活");
                    return;
                }
            }
            else if (cardInfo.Lv < skillCfg.UnlockLv)
            {
                var unlockDesc = !string.IsNullOrEmpty(skillCfg.UnlockSoldierLvDesc)
                    ? skillCfg.UnlockSoldierLvDesc
                    : $"角色等级达到 {skillCfg.UnlockLv} 级解锁";
                FusionContinent.Toast.Show(unlockDesc);
                return;
            }

            // 等级已满足但技能未解锁：先触发解锁，成功后刷新槽位显示，再次点击进入详情
            var skillCard = Global.GetCardById(skillCfg.Id);
            if (skillCard == null || !skillCard.IsHave)
            {
                bool success = await soldierService.UnlockSkillCardAsync(skillCfg.Id);
                if (!success) return;

                RefreshSkillSlot(cardInfo);
                return;
            }

            if (slotIndex == 1)
            {
                UnlockGuideManager.Send(ModuleFunctionType.SoldierSkillAtlas, 3);
            }

            var skillUpgradeVm = new Game.Skill.SkillUpGradePanelViewModel();
            skillUpgradeVm.OnCreate();
            skillUpgradeVm.SetData(skillCfg.Id);
            UIFramework.UIManager.OpenPanelAsync<SkillUpGradePanelNew, Game.Skill.SkillUpGradePanelViewModel>(skillUpgradeVm, UIFramework.UILevel.PopUI).Forget();

            LogTool.Log($"[SoldierPanelNew] 打开技能详情: CardId={cardId}, SlotIndex={slotIndex}, SkillId={skillCfg.Id}");
        }

        /// <summary>
        /// 刷新消耗品显示（从 DevelopConfig.LevelPropertyList 读取当前等级升级所需的消耗品）
        /// 每个 Consumes 子节点挂载 RewardEntity，用 Consume 样式展示「拥有/所需」，不足时标红。
        /// </summary>
        private void RefreshConsumes()
        {
            if (mOldPanel?.Consumes == null) return;

            var cardInfo = VM?.CurrentCard.Value;
            if (cardInfo == null) return;

            var developCfg = VM.GetDevelopConfig(cardInfo.CardId);
            if (developCfg == null) return;

            // 满级时没有消耗品
            if (cardInfo.Lv > developCfg.LevelPropertyList.Count)
            {
                for (int i = 0; i < mOldPanel.Consumes.childCount; i++)
                    mOldPanel.Consumes.GetChild(i).gameObject.SetActive(false);
                return;
            }

            var lvCfg = developCfg.LevelPropertyList[cardInfo.Lv - 1];
            var itemList = lvCfg.ConsumeItems;

            for (int i = 0; i < mOldPanel.Consumes.childCount && i < itemList.Count; i++)
            {
                var item = itemList[i];
                var rewObj = mOldPanel.Consumes.GetChild(i).gameObject;
                rewObj.SetActive(true);

                var rewardEntity = rewObj.GetComponent<RewardEntity>();
                if (rewardEntity != null)
                {
                    // Consume 样式：自动显示「拥有/所需」，数量不足时 consumeNum 标红
                    rewardEntity.SetData(item, RewardStyleType.Consume);
                }
            }

            // 隐藏多余的子节点
            for (int i = itemList.Count; i < mOldPanel.Consumes.childCount; i++)
                mOldPanel.Consumes.GetChild(i).gameObject.SetActive(false);
        }

        // [已注释] 消耗品多选功能：不再需要点击回调
        // private void OnConsumeItemClick(int itemId)
        // {
        //     VM?.OnToggleConsumeItemCommand.Execute(itemId);
        // }

        #endregion

        #region 事件处理

        private void OnSoldierChanged()
        {
            // 播放切换动画
            PlayChangeAnimation();

            var currentCard = VM?.CurrentCard.Value;
            if (currentCard != null)
            {
                RefreshBattleSkillSlots(currentCard.CardId);
            }
        }

        private void OnTeamHeroSkillWearChanged(object data)
        {
            var currentCard = VM?.CurrentCard.Value;
            if (currentCard != null)
            {
                RefreshBattleSkillSlots(currentCard.CardId);
            }
        }

        /// <summary>
        /// 物品数量变化时刷新觉醒相关 UI
        /// </summary>
        private void OnItemChanged(object data)
        {
            var currentCard = VM?.CurrentCard.Value;
            if (currentCard != null)
            {
                CheckAwakingLevel(currentCard);
            }
        }

        private void OnSkinChanged(int skinId)
        {
            SetSkin(skinId);
        }

#if !DISABLE_SKILL_CONTENT
        /// <summary>
        /// 更新技能筛选UI（切换技能卡图鉴和奥术卡图鉴的显示）
        /// </summary>
        private void UpdateSkillFilterUI(CardType filter)
        {
            if (mOldPanel == null) return;

            bool isGeneralSkill = filter == CardType.GeneralSkill;

            // 切换内容容器显示（SkillContent 和 GeneralSkillContent）
            if (mOldPanel.SkillContent != null)
                mOldPanel.SkillContent.gameObject.SetActive(!isGeneralSkill);

            if (mOldPanel.GeneralSkillContent != null)
                mOldPanel.GeneralSkillContent.gameObject.SetActive(isGeneralSkill);

            // 更新按钮选中状态
            var skillBtnSelected = mOldPanel.SkillBtn?.transform.Find("Selected")?.gameObject;
            var generalBtnSelected = mOldPanel.GeneralBtn?.transform.Find("Selected")?.gameObject;

            if (skillBtnSelected != null)
                skillBtnSelected.SetActive(!isGeneralSkill);

            if (generalBtnSelected != null)
                generalBtnSelected.SetActive(isGeneralSkill);

            // 刷新当前显示的列表
            if (isGeneralSkill)
            {
                // 显示奥术卡图鉴
                RefreshGeneralSkillScrollView(VM?.GeneralSkillList.Value, mOldPanel.GeneralSkillScroll);
                // 更新奥术卡数量显示
                UpdateGeneralSkillCardNum();
            }
            else
            {
                // 显示技能卡图鉴
                RefreshSkillScrollView(VM?.SkillList.Value, mOldPanel.SkillCardScroll);
            }
        }
#endif
        

        private void OnUpgradeSuccess()
        {
            // 播放升级特效
            PlayUpgradeEffect();

            // 刷新数据
            if (VM.CurrentCard.Value != null)
            {
                SetSoldierData(VM.CurrentCard.Value);
            }

            // 升级后经验/等级变化，刷新本地红点 + 全局 SoliderCanUpgrade
            _redDotScope.NotifyUpgradeDataChanged();
        }

        private void OnUpgradeBtnClicked()
        {
            UnlockGuideManager.Send(ModuleFunctionType.SoldierAtlas, 3);
            VM?.OnUpgradeCommand.Execute();
        }

        private void OnUpgradeFailed(string errorMessage)
        {
            FusionContinent.Toast.Show(errorMessage);
        }

        private void OnAwakeningRequested(int cardId, int level)
        {
            if (level < 40)
            {
                FusionContinent.Toast.Show("英灵升至40级开放");
                return;
            }

            if (!GameMainPanel.Default.CheckModuleFunction(ModuleFunctionType.SoldierAwakening)) return;
            
            // 使用新的 UIFramework 打开觉醒界面
            var vm = new Game.Soldier.AwakeningPanelViewModel();
            vm.OnCreate();
            vm.Initialize(cardId, level);
            
            // 订阅皮肤变更事件
            vm.OnAwakeningSuccess += (skinId) =>
            {
                // 刷新当前士兵显示
                if (VM.CurrentCard.Value != null)
                {
                    SetSoldierData(VM.CurrentCard.Value);
                }

                _redDotScope.NotifyAwakeningDataChanged();
            };
                        
            UIFramework.UIManager.OpenPanelAsync<AwakeningPanelNew, Game.Soldier.AwakeningPanelViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
            
        }

        private void OnDevelopRequested(int cardId)
        {
            if (!GameMainPanel.Default.CheckModuleFunction(ModuleFunctionType.SoldierDevelop)) return;
            
            // 使用新的 UIFramework 打开养成奖励界面
            var vm = new Game.Soldier.DevelopRewardsViewModel();
            vm.OnCreate();
            vm.Initialize(cardId);
            UIFramework.UIManager.OpenPanelAsync<DevelopRewardsNew, Game.Soldier.DevelopRewardsViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
            
        }

        private void OnEquipRequested(int cardId)
        {
            // UnlockGuideManager.Send(ModuleFunctionType.Equip, 2);
            UnlockGuideManager.Send(ModuleFunctionType.EquipPhase, 2);
            UnlockGuideManager.Send(ModuleFunctionType.EquipEntry, 2);

            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Equip)) return;
            
            this.VM.CurrentOpenPanelValue.Value = SoldierPanelNew.OpenPanelEquip;
            // 使用新的 UIFramework 打开装备界面
            // var vm = new Game.Equip.EquipPanelViewModel();
            // vm.OnCreate();
            //
            // // 先初始化数据，再打开界面
            // vm.Initialize(cardId);
            // // OpenOrReuseSubPanel<EquipPanelNew,Game.Equip.EquipPanelViewModel>(vm);
            // UIFramework.UIManager.OpenPanelAsync<EquipPanelNew, Game.Equip.EquipPanelViewModel>(vm, UIFramework.UILevel.PopUI);
        }

        private void OnSkinRequested(int cardId)
        {
            if (!GameMainPanel.Default.CheckModuleFunction(ModuleFunctionType.SoldierSkin)) return;
            
            // 使用新的 UIFramework 打开皮肤选择界面
            var vm = new Game.Soldier.SkinSoliderPanelViewModel();
            vm.OnCreate();
            vm.Initialize(cardId);
            
            // 订阅皮肤变更事件
            vm.OnSkinChanged += (skinId) =>
            {
                // 刷新当前士兵显示
                if (VM.CurrentCard.Value != null)
                {
                    SetSoldierData(VM.CurrentCard.Value);
                }
            };

            UIFramework.UIManager.OpenPanelAsync<SkinSoliderPanelNew, Game.Soldier.SkinSoliderPanelViewModel>(vm,
                UIFramework.UILevel.PopUI);
        }

        /// <summary>
        /// 打开融合石面板
        /// </summary>
        private void OnFusionGemRequested(int soldierId)
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.FusionGemSlot, true)) return;

            // 使用新的 MVVM 架构打开融合石面板
            var vm = new Game.Equip.FusionGemPanelViewModel();
            vm.OnCreate();
            vm.Initialize(soldierId);

            UIFramework.UIManager.OpenPanelAsync<FusionGemPanelNew, Game.Equip.FusionGemPanelViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
        }

        /// <summary>
        /// 打开主属性详情弹窗
        /// </summary>
        private void OnMainAttributesRequested(int cardId)
        {
            var currentCard = VM?.CurrentCard.Value;
            if (currentCard == null) return;

            var soldierCfg = VM.GetSoldierConfig(cardId);
            if (soldierCfg == null) return;

            var soldierLvCfg = soldierCfg.LevelPropertyList[currentCard.Lv - 1];
            if (soldierLvCfg.MainAttributes == null || soldierLvCfg.MainAttributes.Count == 0) return;

            var rows = new List<Game.Soldier.MainAttrRowData>();

            foreach (var mainAttr in soldierLvCfg.MainAttributes)
            {
                var mainAttrCfg = DataConfigManager.Tables.TbMainAttributeConfig.GetOrDefault(mainAttr.Type);
                if (mainAttrCfg == null) continue;

                var mainAttrValue = CurrentMainAttributeValues != null && CurrentMainAttributeValues.ContainsKey(mainAttr.Type)
                    ? CurrentMainAttributeValues[mainAttr.Type]
                    : mainAttr.Value;

                var attrNameText = $"{mainAttrCfg.Name}:{mainAttrValue:F0}";

                var valueParts = new List<string>();
                if (mainAttrCfg.ConvertToBaseAttributes != null)
                {
                    foreach (var convertAttr in mainAttrCfg.ConvertToBaseAttributes)
                    {
                        var targetCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(convertAttr.Type);
                        if (targetCfg == null) continue;

                        var convertedValue = mainAttrValue * convertAttr.Value;
                        var attrName = LocalizationManager.Instance.GetText(LocalizationModule.UITexts, targetCfg.Name,"+" + convertedValue);
                        // string result = attrName[..^3];
                        valueParts.Add($"{attrName}");
                    }
                }

                rows.Add(new Game.Soldier.MainAttrRowData
                {
                    AttrNameText = attrNameText,
                    AttrValueText = string.Join("\n", valueParts)
                });
            }

            var vm = new MainAttrDetailViewModel();
            vm.OnCreate();
            vm.Rows.AddRange(rows);

            UIFramework.UIManager.OpenPanelAsync<MainAttrDetailNew, MainAttrDetailViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
        }

        /// <summary>
        /// 打开基础属性详情弹窗
        /// </summary>
        private static readonly Config.AttributeType[] sCoreAttrTypes = new Config.AttributeType[]
        {
            Config.AttributeType.SoldierMaxHealth,
            Config.AttributeType.SoldierAttackPower,
            Config.AttributeType.SoldierSpellPower,
            Config.AttributeType.SoldierAttackSpeed,
            Config.AttributeType.SoldierPhysicalResistance,
            Config.AttributeType.SoldierMagicResistance,
            Config.AttributeType.SoldierCriticalRate,
            Config.AttributeType.SoldierCriticalDamage,
            Config.AttributeType.SoldierArmorPenetration,
            Config.AttributeType.SoldierMagicPenetration,
            Config.AttributeType.SoldierMissNum,
            Config.AttributeType.SoldierAttackRange,
            Config.AttributeType.SoldierAttackInterval,
        };

        private void OnBaseAttributesRequested(int cardId)
        {
            if (SoliderAttributeSet == null) return;

            var rows = new List<Game.Soldier.BaseAttrRowData>();

            foreach (var attrType in sCoreAttrTypes)
            {
                FormatAttrNameValue(attrType, out var name, out var value);
                rows.Add(new Game.Soldier.BaseAttrRowData
                {
                    AttrName = name,
                    AttrValue = value
                });
            }

            var vm = new BaseAttrDetailViewModel();
            vm.OnCreate();
            vm.Rows.AddRange(rows);

            UIFramework.UIManager.OpenPanelAsync<BaseAttrDetailNew, BaseAttrDetailViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
        }

        private void FormatAttrNameValue(Config.AttributeType attrType, out string attrName, out string attrValue)
        {
            var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attrType);
            if (attrCfg == null)
            {
                attrName = string.Empty;
                attrValue = string.Empty;
                return;
            }
            attrName = LocalizationManager.Instance.GetText(LocalizationModule.UITexts, attrCfg.Name);
            attrName = attrName[..^3];
            var value = SoliderAttributeSet.GetFinal(attrType);

            if (attrType == AttributeType.SoldierAttackSpeed)
            {
                var soldierCfg = VM.GetSoldierConfig(this.VM.CurrentCard.Value.CardId);
                float attackTime = 1;
                for (int i = 0; i < DataConfigManager.Tables.TbGameBattleConst.AttackSpeedGapOfCarType.Count; i++)
                {
                    if (DataConfigManager.Tables.TbGameBattleConst.AttackSpeedGapOfCarType[i].Type == soldierCfg.CarType)
                    {
                        attackTime = DataConfigManager.Tables.TbGameBattleConst.AttackSpeedGapOfCarType[i].Count;
                    }
                }
                var nextAttackTime = Formula.AttackInterval(SoliderAttributeSet.GetFinal(Config.AttributeType.SoldierNumOfAttackSpeed),  DataConfigManager.Tables.TbGameBattleConst.AttackSpeedBase,
                    DataConfigManager.Tables.TbGameBattleConst.AfkTimeLimitMax);
                nextAttackTime = nextAttackTime / attackTime;
                attrValue = nextAttackTime.ToString("F0");
            }
            else
            {
                attrValue = value.ToString("F0");
            }
            
        }

        /// <summary>
        /// 技能数据变化时刷新显示
        /// </summary>
        private void OnSkillDataChanged()
        {
            // 刷新技能列表显示
            // RefreshSkillScrollView(VM.SkillList.Value, mOldPanel.SkillCardScroll);
            //
            // // 刷新奥术卡列表显示
            // RefreshGeneralSkillScrollView(VM.GeneralSkillList.Value, mOldPanel.GeneralSkillScroll);
            //
            // // 更新奥术卡数量显示
            // UpdateGeneralSkillCardNum();
            
            // 刷新当前士兵数据（等级等可能变化）
            if (VM.CurrentCard.Value != null)
            {
                SetSoldierData(VM.CurrentCard.Value);
            }
            
            LogTool.Log("[SoldierPanelNew] 技能数据变化，已刷新显示");
        }
        
        /// <summary>
        /// 皮肤变更时刷新显示
        /// </summary>
        private void OnCardSkinChanged(int skinId)
        {
            // 刷新当前士兵显示
            if (VM?.CurrentCard.Value != null)
            {
                SetSoldierData(VM.CurrentCard.Value);
                LogTool.Log($"[SoldierPanelNew] 皮肤变更，已刷新显示: SkinId={skinId}");
            }
        }

        #endregion

        #region 动画

        private void CloseWithAnimation()
        {
            if (_isClosing) return;

            if (mOldPanel.CloseAnimation != null)
            {
                _isClosing = true;
                UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, () =>
                {
                    UIFramework.UIManager.ClosePanel(this);
                });
            }
            else
            {
                UIFramework.UIManager.ClosePanel(this);
            }
        }

        private void PlayChangeAnimation()
        {
            if (mOldPanel == null) return;
        }

        private void PlayUpgradeEffect()
        {
            if (mOldPanel == null) return;

            DOTween.To(v => { }, 0, 0, 0.5f).onComplete += () =>
            {
                mOldPanel.CharacterUpgradeEffect01.gameObject.SetActive(false);
                mOldPanel.CharacterUpgradeEffect01.gameObject.SetActive(true);
                SoundManager.Instance.PlayVoice(SoundType.SoundCultivateUpgrade);
            };

            mOldPanel.CharacterUpgradeEffect02.gameObject.SetActive(false);
            mOldPanel.CharacterUpgradeEffect02.gameObject.SetActive(true);
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            LogTool.Log($"[SoldierPanelNew] OnOpen: VM={VM != null}, CurrentCard={VM?.CurrentCard.Value != null}");

            _isClosing = false;

            // 初始化显示数据
            if (VM != null && VM.CurrentCard.Value != null)
            {
                // 刷新所有UI状态
                RefreshAllUIState();

                LogTool.Log($"[SoldierPanelNew] OnOpen: 初始化完成，士兵ID={VM.CurrentCard.Value.CardId}");
            }
            else
            {
                LogTool.LogWarning("[SoldierPanelNew] OnOpen: ViewModel或CurrentCard为空，无法初始化");
            }

            // 打开动画后再注册升级引导目标，避免高亮时按钮尚未就绪
            if (mOldPanel.OpenAnimation != null)
            {
                UIAnimationUtils.PlayAnimations(mOldPanel.OpenAnimation.gameObject, RegisterSoldierAtlasGuideStep3);
            }
            else
            {
                RegisterSoldierAtlasGuideStep3Delayed().Forget();
            }
        }

        void RegisterSoldierAtlasGuideStep3()
        {
            RegisterSoldierAtlasGuideStep3Delayed().Forget();
        }

        async UniTaskVoid RegisterSoldierAtlasGuideStep3Delayed()
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(0.35f));
            if (!this || mOldPanel == null || UnlockGuideManager.Instance == null)
            {
                return;
            }

            if (mOldPanel.UpGradeBtn != null)
            {
                UnlockGuideManager.Instance.SetUnlockTypeStep(
                    ModuleFunctionType.SoldierAtlas,
                    3,
                    mOldPanel.UpGradeBtn.GetComponent<RectTransform>());
            }
        }
        
        /// <summary>
        /// 刷新所有UI状态（用于初始化）
        /// </summary>
        private void RefreshAllUIState()
        {
            if (VM == null || mOldPanel == null) return;
            
            var cardInfo = VM.CurrentCard.Value;
            if (cardInfo == null) return;
 
            // 设置士兵数据
            SetSoldierData(cardInfo);
            
            // 初始化消耗品显示
            RefreshConsumes();
            
            // 根据当前筛选类型初始化对应的技能列表显示
            // UpdateSkillFilterUI(VM.CurrentSkillFilter.Value);
            
            // 刷新技能列表
            VM.UpdateSkillList();
        }

        protected override void OnClose()
        {
            base.OnClose();
        }

        protected override void OnShow()
        {
            
        }

        protected override void OnHide()
        {
            mSkillItems.Clear();
        }

        #endregion

        #region 公共方法

        public void SetData(bool openPanel = true)
        {
            if (VM != null && VM.CurrentCard.Value != null)
            {
                SetSoldierData(VM.CurrentCard.Value);
            }
        }

        public EquipPanel OpenEquipPanel()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Equip)) return null;
            var panel = UIKit.OpenPanel<EquipPanel>();
            if (VM?.CurrentCard.Value != null)
            {
                panel.SetData(VM.CurrentCard.Value.CardId);
            }
            return panel;
        }

        #endregion
    }
}