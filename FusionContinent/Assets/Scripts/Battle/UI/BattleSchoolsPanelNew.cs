using Game.Battle;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Guide;
using FusionContinent.Model;
using FusionContinent.UI.Components;
using Helper;
using TMPro;

namespace FusionContinent
{
    [PrefabPath("BattleSchoolsPanel")]
    /// <summary>
    /// 战斗流派子界面 - 新框架版本
    /// 管理流派卡牌的显示
    /// 作为 BattlePanelNew 的子界面，不参与 UIManager 堆栈管理
    /// 使用 SchoolCardConfig.Id 作为唯一标识
    /// </summary>
    public class BattleSchoolsPanelNew : UIPanel<BattleSchoolsViewModel>
    {
        private const int SpecialSchoolTypeId = 102;

        /// <summary>
        /// 作为子界面，不参与UIManager堆栈管理
        /// </summary>
        public override PanelStackMode StackMode => PanelStackMode.Unmanaged;

        // 引用 BattleSchoolsPanel 的 UI 组件
        private BattleSchoolsPanel mOldPanel;

        // 缓存流派卡牌对象 - 按 schoolCardId
        private Dictionary<int, GameObject> _schoolCards = new Dictionary<int, GameObject>();

        // 缓存流派品质（按 schoolCardId），用于检测品质变化
        private Dictionary<int, QualityType> _schoolQualities = new Dictionary<int, QualityType>();

        // Camera 引用，用于坐标转换
        private Camera _worldCamera;
        private Camera _uiCamera;
        private RectTransform _uiRectTransform;
        
        // 事件处理器
        private System.Action<int> _schoolAddedHandler;
        private System.Action<int> _schoolUpdatedHandler;
        private System.Action<int, QualityType> _schoolQualityChangedHandler;
        private System.Action<object> _unlockSchoolHandler;
        private System.Action<object> _schoolCardFlyEffectHandler;

        void Awake()
        {
            // 获取 BattleSchoolsPanel 引用
            mOldPanel = GetComponent<BattleSchoolsPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[BattleSchoolsPanelNew] BattleSchoolsPanel 组件未找到");
            }

            _uiRectTransform = GetComponent<RectTransform>();
        }

        protected override void OnBindViewModel(BattleSchoolsViewModel vm)
        {
            if (mOldPanel == null) return;

            // 初始化现有流派
            InitializeSchools(vm);

            // 绑定事件
            BindEvents(vm);

            LogTool.Log("[BattleSchoolsPanelNew] 绑定完成");
        }

        protected override void OnUnbindViewModel(BattleSchoolsViewModel vm)
        {
            UnbindEvents(vm);
        }

        /// <summary>
        /// 初始化流派显示 - 与 GameLandUI 一致
        /// </summary>
        private void InitializeSchools(BattleSchoolsViewModel vm)
        {
            foreach (var schoolId in vm.SchoolIds.Value)
            {
                CreateOrUpdateSchoolCard(vm, schoolId);
            }
        }

        /// <summary>
        /// 绑定事件
        /// </summary>
        private void BindEvents(BattleSchoolsViewModel vm)
        {
            _schoolAddedHandler = (schoolId) =>
            {
                LogTool.Log($"[BattleSchoolsPanelNew] OnSchoolAdded 收到: schoolId={schoolId}");
                CreateOrUpdateSchoolCard(vm, schoolId);
            };
            vm.OnSchoolAdded += _schoolAddedHandler;

            _schoolUpdatedHandler = (schoolId) =>
            {
                UpdateSchoolCardAndCheckQuality(vm, schoolId);
            };
            vm.OnSchoolUpdated += _schoolUpdatedHandler;

            _schoolQualityChangedHandler = (schoolId, quality) =>
            {
                PlayQualityEffect(schoolId, quality);
            };
            vm.OnSchoolQualityChanged += _schoolQualityChangedHandler;

            // 监听流派解锁事件
            _unlockSchoolHandler = (obj) =>
            {
                if (obj is int schoolId)
                {
                    LogTool.Log($"[BattleSchoolsPanelNew] 收到 UNLOCK_SCHOOL: schoolId={schoolId}");
                    vm.AddSchool(schoolId);
                }
            };
            Global.Publisher.Subscribe(MessagePublisher.UNLOCK_SCHOOL, _unlockSchoolHandler);

            // 监听流派卡牌飞入特效事件
            _schoolCardFlyEffectHandler = (obj) =>
            {
                if (obj is SchoolCardFlyEffectData flyEffectData)
                {
                    PlaySchoolCardFlyEffect(flyEffectData);
                }
            };
            Global.Publisher.Subscribe(MessagePublisher.SCHOOL_CARD_FLY_EFFECT, _schoolCardFlyEffectHandler);
        }

        /// <summary>
        /// 解绑事件
        /// </summary>
        private void UnbindEvents(BattleSchoolsViewModel vm)
        {
            if (_schoolAddedHandler != null)
            {
                vm.OnSchoolAdded -= _schoolAddedHandler;
                _schoolAddedHandler = null;
            }

            if (_schoolUpdatedHandler != null)
            {
                vm.OnSchoolUpdated -= _schoolUpdatedHandler;
                _schoolUpdatedHandler = null;
            }

            if (_schoolQualityChangedHandler != null)
            {
                vm.OnSchoolQualityChanged -= _schoolQualityChangedHandler;
                _schoolQualityChangedHandler = null;
            }

            if (_unlockSchoolHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.UNLOCK_SCHOOL, _unlockSchoolHandler);
                _unlockSchoolHandler = null;
            }

            if (_schoolCardFlyEffectHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.SCHOOL_CARD_FLY_EFFECT, _schoolCardFlyEffectHandler);
                _schoolCardFlyEffectHandler = null;
            }
        }

        /// <summary>
        /// 创建或更新流派卡牌（按 SchoolCardConfig.Id）
        /// </summary>
        private void CreateOrUpdateSchoolCard(BattleSchoolsViewModel vm, int schoolId)
        {
            if (mOldPanel?.Schools == null) return;
            if (IsHiddenSchoolCard(schoolId))
            {
                RemoveSchoolCardIfExists(schoolId);
                return;
            }

            var schoolCardCfg = vm.GetSchoolCardConfig(schoolId);
            if (schoolCardCfg == null) return;
            var schoolCardCount = vm.GetSchoolCardCount(schoolId);
            
            var schoolSlotList = mOldPanel.Schools.transform.Find("Viewport/Content");
            if (schoolSlotList == null) return;

            GameObject cardSlot;
            bool hasExistingCard = _schoolCards.TryGetValue(schoolId, out cardSlot) && cardSlot != null;
            bool isNewCard = !hasExistingCard;
            LogTool.Log($"[BattleSchoolsPanelNew] CreateOrUpdate 开始: schoolCardId={schoolId}, haveCardCount={schoolCardCount}, isNewCard={isNewCard}, cacheCount={_schoolCards.Count}");
            if (isNewCard)
            {
                if (_schoolCards.ContainsKey(schoolId))
                {
                    _schoolCards.Remove(schoolId);
                }

                // 创建新卡牌 - 与 GameLandUI 第521-528行一致
                var childPos = schoolSlotList.GetChild(0);

                // 计算当前数量（在添加之前）
                int currentCount = _schoolCards.Count;

                cardSlot = Instantiate(childPos.gameObject, schoolSlotList.transform);
                _schoolCards.Add(schoolId, cardSlot);
                cardSlot.transform.localScale = Vector3.one;

                // 设置位置 - 与 GameLandUI 第528行一致
                childPos.SetSiblingIndex(0);
                cardSlot.transform.localPosition = new Vector3(
                    childPos.localPosition.x,
                    -1 * (currentCount + 1) * (childPos.GetComponent<RectTransform>().rect.height - 44.87f),
                    0
                );

                // 强制刷新布局
                LayoutRebuilder.ForceRebuildLayoutImmediate(schoolSlotList.GetComponent<RectTransform>());

                // 初始化品质记录
                var quality = vm.GetSchoolQuality(schoolId);
                _schoolQualities[schoolId] = quality;
                LogTool.Log($"[BattleSchoolsPanelNew] 新建流派卡: schoolCardId={schoolId}, quality={quality}, cacheCount={_schoolCards.Count}");
            }
            else
            {
                // 相同 schoolId 时按真实计数回填，避免重复事件导致数量漂移
                UpdateSchoolCardCountText(cardSlot, schoolCardCount);
                UpdateSchoolCardIcon(cardSlot, schoolCardCfg);
                LogTool.Log($"[BattleSchoolsPanelNew] 复用流派卡并同步数量: schoolId={schoolId}, count={schoolCardCount}");
            }

            // 更新显示 - 与 GameLandUI 第535-542行一致
            if (isNewCard)
            {
                UpdateSchoolCardUI(cardSlot, schoolId, schoolCardCfg, schoolCardCount);
            }

            // 绑定点击事件显示详情
            BindSchoolCardClickEvent(vm, cardSlot, schoolId, schoolCardCfg);
        }

        /// <summary>
        /// 按真实计数更新卡牌数量文本与排序值
        /// </summary>
        private void UpdateSchoolCardCountText(GameObject cardSlot, int schoolCardCount)
        {
            if (cardSlot == null) return;

            var cardCountText = cardSlot.transform.Find("CardCount/CardCountText")?.GetComponent<Text>();
            if (cardCountText != null)
            {
                cardCountText.text = $"{schoolCardCount}";
            }

            var sorterItem = cardSlot.GetComponent<SorterItemData>();
            if (sorterItem != null)
            {
                sorterItem.level = schoolCardCount;
            }

            LogTool.Log($"[BattleSchoolsPanelNew] CardCountText 同步数量: {schoolCardCount}");
        }

        /// <summary>
        /// 更新流派卡牌并检查品质变化
        /// </summary>
        private void UpdateSchoolCardAndCheckQuality(BattleSchoolsViewModel vm, int schoolId)
        {
            if (mOldPanel?.Schools == null) return;
            if (IsHiddenSchoolCard(schoolId))
            {
                RemoveSchoolCardIfExists(schoolId);
                return;
            }

            var schoolCardCfg = vm.GetSchoolCardConfig(schoolId);
            if (schoolCardCfg == null) return;
            var schoolCardCount = vm.GetSchoolCardCount(schoolId);
            // 确保卡牌存在
            if (!_schoolCards.TryGetValue(schoolId, out var cardSlot) || cardSlot == null)
            {
                CreateOrUpdateSchoolCard(vm, schoolId);
                return;
            }

            // 获取当前品质
            var currentQuality = vm.GetSchoolQuality(schoolId);

            // 检查品质是否变化
            if (_schoolQualities.TryGetValue(schoolId, out var oldQuality))
            {
                if (currentQuality != oldQuality)
                {
                    LogTool.Log($"[BattleSchoolsPanelNew] 流派品质变化: schoolId={schoolId}, oldQuality={oldQuality}, newQuality={currentQuality}");

                    // 更新品质记录
                    _schoolQualities[schoolId] = currentQuality;

                    // 品质提升时，不立即更新UI，等缩放动画开始时再显示新品质
                    // 只更新数量文本，不更新品质背景
                    UpdateSchoolCardCountOnly(cardSlot, schoolCardCount);

                    // 播放品质变化特效（包含缩放动画和UI更新）
                    PlayQualityEffectWithUIUpdate(schoolId, currentQuality, cardSlot, schoolCardCfg, schoolCardCount);
                    return;
                }
            }

            // 品质未变化，正常更新UI
            UpdateSchoolCardUI(cardSlot, schoolId, schoolCardCfg, schoolCardCount);
            GuideManager.Instance.SetGuideNode(GuideType.PressSchoolShowDetails,cardSlot);
            GamePlayManager.Default.TriggerGuide(GuideType.PressSchoolShowDetails);
        }

        /// <summary>
        /// 仅更新流派卡牌数量，不更新品质显示
        /// </summary>
        private void UpdateSchoolCardCountOnly(GameObject cardSlot, int schoolCardCount)
        {
            // 只更新卡牌数量
            var cardCountText = cardSlot.transform.Find("CardCount/CardCountText")?.GetComponent<Text>();
            if (cardCountText != null)
            {
                cardCountText.text = $"{schoolCardCount}";
            }

            // 更新排序数据
            var sorterItem = cardSlot.GetComponent<SorterItemData>();
            if (sorterItem != null)
            {
                sorterItem.level = schoolCardCount;
            }
        }

        /// <summary>
        /// 播放品质变化特效，并在缩放动画开始时更新UI
        /// </summary>
        private void PlayQualityEffectWithUIUpdate(int schoolId, QualityType quality, GameObject cardSlot, SchoolCardConfig schoolCardCfg, int schoolCardCount)
        {
            if (cardSlot == null) return;

            // 延迟播放缩放动画和特效（与老界面一致）
            DOTween.To(v => { }, 0, 0, 0.7f).onComplete = () =>
            {
                // 在缩放动画开始时，才更新UI显示新品质
                UpdateSchoolCardUI(cardSlot, schoolId, schoolCardCfg, schoolCardCount);

                // 播放缩放动画
                CardDoScale(cardSlot.transform);
            };

            DOTween.To(v => { }, 0, 0, 1.9f).onComplete = () =>
            {
                // 根据品质播放不同特效
                switch (quality)
                {
                    case QualityType.Legend:
                        PlayLegendEffect(cardSlot);
                        break;
                    case QualityType.Epic:
                        PlayEpicEffect(cardSlot);
                        break;
                }
            };
        }

        /// <summary>
        /// 更新流派卡牌UI - 与 GameLandUI 第535-542行一致
        /// </summary>
        private void UpdateSchoolCardUI(GameObject cardSlot, int schoolCardId, SchoolCardConfig schoolCardCfg, int schoolCardCount)
        {
            cardSlot.SetActive(true);
            cardSlot.transform.localScale = Vector3.one;

            LogTool.Log($"[BattleSchoolsPanelNew] UpdateSchoolCardUI: schoolCardId={schoolCardId}, HaveCardCount={schoolCardCount}, scale={cardSlot.transform.localScale}, activeSelf={cardSlot.activeSelf}");

            // 更新卡牌数量 - 与 GameLandUI 第538行一致
            var cardCountText = cardSlot.transform.Find("CardCount/CardCountText")?.GetComponent<Text>();
            if (cardCountText != null)
            {
                cardCountText.text = $"{schoolCardCount}";
            }

            // 更新图标 - 与 GameLandUI 第540行一致
            UpdateSchoolCardIcon(cardSlot, schoolCardCfg);

            // 更新排序数据 - 与 GameLandUI 第542行一致
            var sorterItem = cardSlot.GetComponent<SorterItemData>();
            if (sorterItem != null)
            {
                sorterItem.level = schoolCardCount;
            }

            // 更新品质背景
            UpdateSchoolQuality(cardSlot, schoolCardCfg);
            GuideManager.Instance.SetGuideNode(GuideType.PressSchoolShowDetails,cardSlot.gameObject);
            GamePlayManager.Default.TriggerGuide(GuideType.PressSchoolShowDetails);
        }

        /// <summary>
        /// 更新流派品质显示 - 与 GameLandUI 逻辑一致
        /// </summary>
        private void UpdateSchoolQuality(GameObject cardSlot, SchoolCardConfig schoolCardCfg)
        {
            var iconBg = cardSlot.transform.Find("Iconbg")?.GetComponent<Image>();
            if (iconBg == null) return;

            iconBg.sprite = Global.SchoolIconAtlas.GetSprite(QualityType.Default.ToString());
        }

        /// <summary>
        /// 更新流派图标（读取 ConnectionIcon 图集）
        /// </summary>
        private void UpdateSchoolCardIcon(GameObject cardSlot, SchoolCardConfig schoolCardCfg)
        {
            var iconImage = cardSlot.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage == null) return;
            // var iconRect = iconImage.GetComponent<RectTransform>();
            // if (iconRect != null)
            // {
            //     iconRect.sizeDelta = new Vector2(80f, 80f);
            // }

            if (string.IsNullOrEmpty(schoolCardCfg.Icon))
            {
                return;
            }

            var sprite = Global.ConnectionIconAtlas.GetSpriteExt(schoolCardCfg.Icon);
            if (sprite == null)
            {
                LogTool.LogWarning($"[BattleSchoolsPanelNew] ConnectionIcon 图集未找到图标: {schoolCardCfg.Icon}");
                return;
            }

            iconImage.sprite = sprite;
        }

        /// <summary>
        /// 绑定流派卡牌点击事件 - 显示 SchoolTipDetail
        /// </summary>
        private void BindSchoolCardClickEvent(BattleSchoolsViewModel vm, GameObject cardSlot, int schoolCardId, SchoolCardConfig schoolCardCfg)
        {
            var button = cardSlot.GetComponent<UIPointerClickAndLongPress>();
            if (button == null) return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                ShowSchoolTipDetail(cardSlot, schoolCardId, schoolCardCfg, vm.GetSchoolCardCount(schoolCardId));
            });
        }

        /// <summary>
        /// 显示流派详情面板 - 与 GameLandUI 第682-796行一致
        /// </summary>
        private void ShowSchoolTipDetail(GameObject cardSlot, int schoolCardId, SchoolCardConfig schoolCardCfg, int schoolCardCount)
        {
            if (mOldPanel?.SchoolTipDetail == null) return;

            var tipDetail = mOldPanel.SchoolTipDetail;
            tipDetail.gameObject.SetActive(true);
            Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS,GuideType.PressSchoolShowDetails);
            // 显示 Option 标记
            // var option = cardSlot.transform.Find("Option")?.gameObject;
            // if (option != null) option.SetActive(true);

            // 设置位置 - 与 GameLandUI 第688-689行一致
            tipDetail.transform.position = new Vector3(
                tipDetail.transform.position.x,
                cardSlot.transform.position.y - 3.5f,
                tipDetail.transform.position.z
            );

            var cardDataTip = tipDetail.Find("Canvas");
            if (cardDataTip == null) return;

            // 绑定关闭按钮 - 与 GameLandUI 第693-697行一致
            var quitBtn = cardDataTip.transform.Find("Quit")?.GetComponent<Button>();
            if (quitBtn != null)
            {
                quitBtn.onClick.RemoveAllListeners();
                quitBtn.onClick.AddListener(() =>
                {
                    tipDetail.gameObject.SetActive(false);
                });
            }

            // 设置名称 - 与 GameLandUI 第699行一致
            var nameText = cardDataTip.Find("Mask/Bg/hor/NameTxt")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null) nameText.text = schoolCardCfg.Name;

            // 设置描述（显示 StarList 的 Desc1 + Desc2）
            var descText = cardDataTip.Find("Mask/Bg/CardCountBox/Content/DescText")?.GetComponent<TextMeshProUGUI>();
            if (descText != null) descText.text = BuildSchoolTipDescription(schoolCardCfg, schoolCardCount);
        }

        private string BuildSchoolTipDescription(SchoolCardConfig schoolCardCfg, int schoolCardCount)
        {
            if (schoolCardCfg?.StarList == null || schoolCardCfg.StarList.Count == 0)
            {
                return schoolCardCfg?.Desc ?? string.Empty;
            }

            var starCfg = schoolCardCfg.StarList[0];
            var desc2 = BuildSchoolStarDesc(starCfg.Desc2, starCfg, schoolCardCount, true);
            return desc2;
        }

        private string BuildSchoolStarDesc(string template, SchoolCardStarListStruct starCfg, int schoolCardCount, bool multiplyByCount)
        {
            if (string.IsNullOrEmpty(template) || starCfg == null) return template ?? string.Empty;

            var result = template;
            for (int i = 1; i <= starCfg.Attributes.Count; i++)
            {
                var attr = starCfg.Attributes[i - 1];
                var val = attr.ValueType == ValueType.Percentage ? attr.Value * 100 : attr.Value;
                if (attr.Type is AttributeType.SoldierCriticalRate or AttributeType.SoldierCriticalDamage)
                {
                    val = attr.Value * 100;
                }

                if (multiplyByCount)
                {
                    val *= schoolCardCount;
                }

                result = result.Replace($"{{Attribute{i}}}", FormatDisplayValue(val));
            }

            var param1 = starCfg.Param1;
            var param2 = starCfg.Param2;
            if (multiplyByCount)
            {
                param1 *= schoolCardCount;
                param2 *= schoolCardCount;
            }

            result = result.Replace("{Param1}", FormatDisplayValue(param1));
            result = result.Replace("{Param2}", FormatDisplayValue(param2));
            return result;
        }

        /// <summary>
        /// 设置流派卡牌列表 - 与 GameLandUI 第705-796行一致
        /// </summary>
        private void SetupSchoolCardList(Transform cardDataTip, int schoolCardId, int schoolCardCount)
        {
            var cardCountBox = cardDataTip.Find("CardCountBox/Viewport/Content");
            if (cardCountBox == null) return;

            var cardDetail = cardCountBox.Find("CardCountDetails")?.gameObject;
            if (cardDetail == null) return;

            // 清除旧的卡牌详情 - 与 GameLandUI 第707-716行一致
            for (int i = cardCountBox.transform.childCount - 1; i >= 0; i--)
            {
                var child = cardCountBox.transform.GetChild(i).gameObject;
                if (i == 0)
                {
                    child.SetActive(false); // 保留第一个作为模板
                }
                else
                {
                    Destroy(child);
                }
            }

            var schoolCardCfg = SchoolManager.Instance.SchoolCardConfig.GetOrDefault(schoolCardId);
            if (schoolCardCfg == null) return;
            if (schoolCardCfg.StarList == null || schoolCardCfg.StarList.Count == 0) return;

            GameObject cardObj = Instantiate(cardDetail, cardCountBox);
            cardObj.SetActive(true);
            cardObj.transform.localScale = Vector3.one;

            var numText = cardObj.transform.Find("CardCountText/CardCountText")?.GetComponent<TMP_Text>();
            if (numText != null) numText.text = "(1)";

                // 固定使用1级配置（CardStar 不再参与）
                var starCfg = schoolCardCfg.StarList[0];

                // 设置描述 - 与 GameLandUI 第732-743行一致
                var desc = starCfg.Desc2;
                for (int i = 1; i <= starCfg.Attributes.Count; i++)
                {
                    var attr = starCfg.Attributes[i - 1];
                    var val = attr.ValueType == ValueType.Percentage ? attr.Value * 100 : attr.Value;
                    if (attr.Type is AttributeType.SoldierCriticalRate or AttributeType.SoldierCriticalDamage)
                    {
                        val = attr.Value * 100;
                    }
                    val *= schoolCardCount;
                    desc = desc.Replace($"{{Attribute{i}}}", FormatDisplayValue(val));
                }

                if (starCfg.Param1 > 0) desc = desc.Replace("{Param1}", FormatDisplayValue(starCfg.Param1 * schoolCardCount));
                if (starCfg.Param2 > 0) desc = desc.Replace("{Param2}", FormatDisplayValue(starCfg.Param2 * schoolCardCount));

                var descText = cardObj.transform.Find("Property/DescText")?.GetComponent<LocalizationaAutoHeightText>();
                if (descText != null)
                {
                    descText.SetData(LocalizationModule.UITexts, desc);
                }

                // 设置颜色（根据是否解锁）- 与 GameLandUI 第752-763行一致
                // bool isUnlocked = _schoolCard.CardStar > 0;
                // var matchColor = DataConfigManager.Tables.TbGameBattleConst.SchoolCardLockedDescColor.First().Color;
                // var matchColorAlpha = DataConfigManager.Tables.TbGameBattleConst.SchoolCardLockedDescTransparency;
                // Color textColor = new Color(matchColor.X / 255f, matchColor.Y / 255f, matchColor.Z / 255f, 1);
                //
                // if (!isUnlocked)
                // {
                //     textColor = new Color(matchColor.X / 255f, matchColor.Y / 255f, matchColor.Z / 255f, matchColorAlpha / 255f);
                // }
                //
                // if (numText != null) numText.color = textColor;
                // if (descText != null)
                // {
                //     var tmpText = descText.GetComponent<TMP_Text>();
                //     if (tmpText != null) tmpText.color = textColor;
                // }

                // 更新布局 - 与 GameLandUI 第761-763行一致
                UIHelper.SafeRefreshLayout(cardObj.GetComponent<RectTransform>());
                UIHelper.SafeRefreshLayout(cardObj.transform.Find("Property")?.GetComponent<RectTransform>());
                UIHelper.SafeRefreshLayout(cardObj.transform.Find("Property/DescText")?.GetComponent<RectTransform>());

            // 刷新布局 - 与 GameLandUI 第807-810行一致
            UIHelper.SafeRefreshLayout(cardCountBox.GetComponent<RectTransform>());
        }

        private string FormatDisplayValue(float value)
        {
            if (Mathf.Abs(value - Mathf.Round(value)) < 0.001f)
            {
                return ((int)Mathf.Round(value)).ToString();
            }

            return value.ToString("0.##");
        }

        /// <summary>
        /// 播放品质变化特效
        /// </summary>
        private void PlayQualityEffect(int schoolId, QualityType quality)
        {
            if (!_schoolCards.TryGetValue(schoolId, out var cardSlot)) return;

            // Iconbg 固定使用 Default，不随品质变化
            var iconBg = cardSlot.transform.Find("Iconbg")?.GetComponent<Image>();
            if (iconBg != null)
            {
                iconBg.sprite = Global.SchoolIconAtlas.GetSprite(QualityType.Default.ToString());
            }

            // 延迟播放缩放动画和特效（与老界面一致）
            DOTween.To(v => { }, 0, 0, 0.7f).onComplete = () => 
            { 
                CardDoScale(cardSlot.transform); 
            };
            
            DOTween.To(v => { }, 0, 0, 1.9f).onComplete = () =>
            {
                // 根据品质播放不同特效
                switch (quality)
                {
                    case QualityType.Legend:
                        PlayLegendEffect(cardSlot);
                        break;
                    case QualityType.Epic:
                        PlayEpicEffect(cardSlot);
                        break;
                }
            };
        }

        /// <summary>
        /// 播放传说品质特效
        /// </summary>
        private void PlayLegendEffect(GameObject cardSlot)
        {
            // 特效1
            var pool1 = CommonItemPoolManager.Instance?.GetGameObjectPoolOrCreate("BondEffectGetColor01");
            if (pool1 != null)
            {
                var effect = pool1.Get();
                if (effect != null)
                {
                    effect.transform.SetParent(cardSlot.transform);
                    effect.transform.localScale = Vector3.one * 100;
                    effect.transform.position = cardSlot.transform.position;
                    
                    DOTween.To(v => { }, 0, 0, 2f).onComplete = () => pool1.Return(effect);
                }
            }

            // 特效2
            var pool2 = CommonItemPoolManager.Instance?.GetGameObjectPoolOrCreate("BondEffectGetColor03");
            if (pool2 != null)
            {
                var effect = pool2.Get();
                if (effect != null)
                {
                    effect.transform.SetParent(cardSlot.transform);
                    effect.transform.localScale = Vector3.one * 100;
                    effect.transform.position = cardSlot.transform.position;
                }
            }

            // 特效3
            var pool3 = CommonItemPoolManager.Instance?.GetGameObjectPoolOrCreate("BondEffectGetColor02");
            if (pool3 != null)
            {
                var effect = pool3.Get();
                if (effect != null)
                {
                    effect.transform.localScale = Vector3.one * 0.5f;
                    effect.transform.position = cardSlot.transform.position;
                    DOTween.To(v => { }, 0, 0, 2f).onComplete = () => pool3.Return(effect);
                }
            }
        }

        /// <summary>
        /// 播放史诗品质特效
        /// </summary>
        private void PlayEpicEffect(GameObject cardSlot)
        {
            var pool = CommonItemPoolManager.Instance?.GetGameObjectPoolOrCreate("BondEffectGetGolden01");
            if (pool != null)
            {
                var effect = pool.Get();
                if (effect != null)
                {
                    effect.transform.SetParent(cardSlot.transform);
                    effect.transform.localScale = Vector3.one * 100;
                    effect.transform.position = cardSlot.transform.position;
                }
            }
        }

        /// <summary>
        /// 卡牌放大动画（与老界面CardDoScale一致）
        /// </summary>
        private void CardDoScale(Transform cardSlot)
        {
            if (cardSlot == null) return;

            // 确保初始缩放和位置
            cardSlot.localScale = Vector3.one;
            Vector3 originalPos = cardSlot.localPosition;

            // 缩放到 0.3 并添加左右抖动
            Sequence seq = DOTween.Sequence();

            // 同时播放缩放和位置抖动
            seq.Append(cardSlot.DOScale(0.3f, 0.6f).SetEase(Ease.InQuad));
            seq.Join(cardSlot.DOShakePosition(0.6f, new Vector3(10f, 0f, 0f), 20, 90, false, false)
                .SetRelative(true));

            // 缩放回弹
            seq.Append(cardSlot.DOScale(Vector3.one * 1.2f, 0.5f).SetEase(Ease.OutBack));

            // 恢复正常大小
            seq.Append(cardSlot.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutQuad));
            
            // 确保位置恢复
            seq.OnComplete(() =>
            {
                cardSlot.localPosition = originalPos;
                GuideManager.Instance.SetGuideNode(GuideType.PressSchoolShowDetails,cardSlot.gameObject);
                GamePlayManager.Default.TriggerGuide(GuideType.PressSchoolShowDetails);
            });
        }

        #region 流派卡牌飞入特效

        /// <summary>
        /// 播放流派卡牌飞入特效
        /// </summary>
        private void PlaySchoolCardFlyEffect(SchoolCardFlyEffectData flyEffectData)
        {
            if (flyEffectData == null) return;

            int schoolId = ResolveSchoolIdFromFlyEffectData(flyEffectData);
            if (schoolId <= 0) return;

            // 获取目标流派卡牌
            if (!_schoolCards.TryGetValue(schoolId, out var targetCard) || targetCard == null)
            {
                LogTool.LogWarning($"[BattleSchoolsPanelNew] 找不到目标流派卡牌: {schoolId}");
                return;
            }

            // 检查是否是新流派卡（第一次获得）
            var schoolCardCount = SchoolManager.Instance?.GetSchoolCardCount(schoolId) ?? 0;
            bool isNewSchool = schoolCardCount <= 1;

            LogTool.Log($"[BattleSchoolsPanelNew] 飞入特效前: schoolCardId={schoolId}, HaveCardCount={schoolCardCount}, isNewSchool={isNewSchool}, currentScale={targetCard.transform.localScale}");

            // 如果是新流派，先隐藏卡牌，等待飞入完成后再显示
            if (isNewSchool)
            {
                targetCard.transform.localScale = Vector3.zero;
                LogTool.Log($"[BattleSchoolsPanelNew] 新流派设置为scale=0: schoolId={schoolId}");
            }

            // 从对象池获取特效
            var bondEffectGet01Pool = BattleItemPoolManager.Instance?.GetGameObjectPoolOrCreate("BondEffectGet01");
            if (bondEffectGet01Pool == null) return;

            GameObject bondEffectGet01 = bondEffectGet01Pool.Get();
            if (bondEffectGet01 == null) return;

            // 设置特效父级为当前transform（与老界面一致，不使用false参数）
            bondEffectGet01.transform.SetParent(transform);


            bondEffectGet01.transform.position = flyEffectData.StartPosition;
            bondEffectGet01.transform.localScale = Vector3.one * 128f;
            LogTool.Log($"[BattleSchoolsPanelNew] 飞入特效: schoolId={schoolId}, isNew={isNewSchool},worldPos={flyEffectData.StartPosition} ,{targetCard.transform.position}");

            // 播放贝塞尔曲线飞入动画（使用屏幕坐标转换，避免Camera Z轴差异）
            MoveAlongBezier(bondEffectGet01.transform, targetCard.transform, schoolId,
                flyEffectData.Duration, () =>
                {
                    bondEffectGet01Pool.Return(bondEffectGet01);
                    // 播放到达特效和放大动画
                    PlayArrivalEffect(schoolId, isNewSchool);
                });
        }

        /// <summary>
        /// 从飞入数据解析目标流派卡ID
        /// 优先使用 SchoolCardId（SchoolCardConfig.Id），再回退旧的 SchoolIds 首项
        /// </summary>
        private int ResolveSchoolIdFromFlyEffectData(SchoolCardFlyEffectData flyEffectData)
        {
            if (flyEffectData.SchoolCardId > 0)
            {
                if (DataConfigManager.Tables.TbSchoolCardConfig.GetOrDefault(flyEffectData.SchoolCardId) == null)
                {
                    LogTool.LogWarning($"[BattleSchoolsPanelNew] 无效 SchoolCardId: {flyEffectData.SchoolCardId}");
                    return 0;
                }
                if (IsHiddenSchoolCard(flyEffectData.SchoolCardId))
                {
                    return 0;
                }
                return flyEffectData.SchoolCardId;
            }

            if (flyEffectData.SchoolIds != null && flyEffectData.SchoolIds.Count > 0)
            {
                return flyEffectData.SchoolIds[0];
            }

            LogTool.LogWarning("[BattleSchoolsPanelNew] 飞入数据缺少 SchoolCardId/SchoolIds");
            return 0;
        }

        private bool IsHiddenSchoolCard(int schoolCardId)
        {
            var cfg = DataConfigManager.Tables.TbSchoolCardConfig.GetOrDefault(schoolCardId);
            return cfg != null && cfg.Schools != null && cfg.Schools.Contains(SpecialSchoolTypeId);
        }

        private void RemoveSchoolCardIfExists(int schoolCardId)
        {
            if (_schoolCards.TryGetValue(schoolCardId, out var cardObj))
            {
                if (cardObj != null)
                {
                    Destroy(cardObj);
                }
                _schoolCards.Remove(schoolCardId);
            }
            _schoolQualities.Remove(schoolCardId);
        }
        
        /// <summary>
        /// 贝塞尔曲线移动（使用屏幕坐标转换，解决Camera Z轴差异问题）
        /// </summary>
        private void MoveAlongBezier(Transform obj, Transform target, int schoolId, float duration, System.Action onComplete = null)
        {
            // 延迟确保目标位置正确（与老界面一致使用0.3f）
            DOTween.To(v => { }, 0, 0, 0.3f).onComplete = () =>
            {
                if (target == null || obj == null) return;

                // 获取起点和终点的局部坐标
                // Vector3 start = obj.localPosition;
                Vector3 start = UIFramework.UIRoot.Instance.GetComponent<RectTransform>().InverseTransformPoint(obj.position);
                // ① 获取目标 UI 世界坐标
                Vector3 uiWorldPos = UIFramework.UIRoot.Instance.GetComponent<RectTransform>().InverseTransformPoint(target.position);

                Vector3 end = uiWorldPos;

                // 控制点（形成弧线，与老界面一致使用Vector3.up * 2f）
                Vector3 control = (start + end) / 2f + Vector3.up * 2f;

                float t = 0f;
                DOTween.To(() => t, x =>
                    {
                        t = x;
                        if (obj != null)
                            obj.localPosition = GetQuadraticBezierPoint(t, start, control, end);
                    }, 1f, duration / 1.2f)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        if (obj != null)
                            obj.localPosition = end;
                        onComplete?.Invoke();
                    });
            };
        }

        /// <summary>
        /// 获取二次贝塞尔曲线点
        /// </summary>
        private Vector3 GetQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            return u * u * p0 + 2 * u * t * p1 + t * t * p2;
        }

        /// <summary>
        /// 播放到达特效和放大动画
        /// </summary>
        private void PlayArrivalEffect(int schoolId, bool isNewSchool)
        {
            if (!_schoolCards.TryGetValue(schoolId, out var cardSlot) || cardSlot == null) return;

            var schoolCardCfg = DataConfigManager.Tables.TbSchoolCardConfig.GetOrDefault(schoolId);
            if (schoolCardCfg == null) return;
            int haveCount = SchoolManager.Instance.GetSchoolCardCount(schoolId);
            bool hasExactMatch = haveCount > 0;

            // 首次获得该流派时，仅在本次获得触发品质提升（精确匹配）才播放缩放动画
            if (isNewSchool && hasExactMatch)
            {
                cardSlot.transform.localScale = Vector3.one;
                CardDoScale(cardSlot.transform);
            }

            if (hasExactMatch)
            {
                // 播放到达特效
                var BondEffectGet03pool = CommonItemPoolManager.Instance?.GetGameObjectPoolOrCreate("BondEffectGet03");
                if (BondEffectGet03pool != null)
                {
                    GameObject BondEffectGet03 = BondEffectGet03pool.Get();
                    if (BondEffectGet03 != null)
                    {
                        BondEffectGet03.transform.position = cardSlot.transform.position;
                        BondEffectGet03.transform.localScale = Vector3.one * 0.4f;
                        BondEffectGet03.transform.SetParent(cardSlot.transform);

                        DOTween.To(v => { }, 0, 0, 1f).onComplete = () =>
                        {
                            BondEffectGet03pool.Return(BondEffectGet03);
                            // 特效播放完成后，更新流派卡显示
                            UpdateSchoolCardUI(cardSlot, schoolId, schoolCardCfg, haveCount);
                        };
                    }
                    else
                    {
                        // 如果特效创建失败，也更新显示
                        UpdateSchoolCardUI(cardSlot, schoolId, schoolCardCfg, haveCount);
                    }
                }
                else
                {
                    // 如果对象池不存在，也更新显示
                    UpdateSchoolCardUI(cardSlot, schoolId, schoolCardCfg, haveCount);
                }
            }
            else
            {
                // 没有匹配配置时，也更新显示
                UpdateSchoolCardUI(cardSlot, schoolId, schoolCardCfg, haveCount);
            }
        }

        #endregion

        protected override void OnOpen(IUIData uiData = null)
        {
            gameObject.SetActive(true);
        }

        protected override void OnShow()
        {
            gameObject.SetActive(true);
        }

        protected override void OnHide()
        {
            gameObject.SetActive(false);
        }

        protected override void OnClose()
        {
            base.OnClose();
        }
    }
}
