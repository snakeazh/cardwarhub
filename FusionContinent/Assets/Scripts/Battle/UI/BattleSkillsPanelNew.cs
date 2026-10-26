using Game.Battle;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Model;
using TMPro;

namespace FusionContinent
{
    [PrefabPath("BattleSkillsPanel")]
    /// <summary>
    /// 战斗技能子界面 - 新框架版本
    /// 管理技能卡牌槽位
    /// 作为 BattlePanelNew 的子界面，不参与 UIManager 堆栈管理
    /// </summary>
    public class BattleSkillsPanelNew : UIPanel<BattleSkillsViewModel>
    {
        /// <summary>
        /// 作为子界面，不参与UIManager堆栈管理
        /// </summary>
        public override PanelStackMode StackMode => PanelStackMode.Unmanaged;

        // 引用 BattleSkillsPanel 的 UI 组件
        private BattleSkillsPanel mOldPanel;

        // 缓存技能卡牌对象
        private Dictionary<int, GameObject> _skillCards = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, Tween> _skillCoolingTweens = new Dictionary<int, Tween>();
        
        // 事件处理器
        private System.Action<int> _skillSelectedHandler;
        private System.Action<int> _skillReleasedHandler;
        private System.Action<int> _skillDragStartedHandler;
        private System.Action _skillDragCancelledHandler;
        private System.Action<int, bool> _skillUnlockChangedHandler;
        private System.Action<int, float> _skillCoolingUpdatedHandler;
        private System.Action<object> _skillCoolingStartedHandler;
        private System.Action<object> _skillCoolingInitializedHandler;

        void Awake()
        {
            // 获取 BattleSkillsPanel 引用
            mOldPanel = GetComponent<BattleSkillsPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[BattleSkillsPanelNew] BattleSkillsPanel 组件未找到");
            }
        }

        protected override void OnBindViewModel(BattleSkillsViewModel vm)
        {
            if (mOldPanel == null) return;

            // 初始化技能卡牌
            InitializeSkillCards(vm);

            // 绑定事件
            BindEvents(vm);

            // 订阅技能冷却初始化完成事件
            _skillCoolingInitializedHandler = (obj) =>
            {
                LogTool.Log("[BattleSkillsPanelNew] 收到 SKILL_COOLING_INITIALIZED 事件");
                RefreshAllSkillCoolingStates(vm);
            };
            Global.Publisher.Subscribe(MessagePublisher.SKILL_COOLING_INITIALIZED, _skillCoolingInitializedHandler);

            LogTool.Log("[BattleSkillsPanelNew] 绑定完成");
        }

        protected override void OnUnbindViewModel(BattleSkillsViewModel vm)
        {
            UnbindEvents(vm);
        }

        /// <summary>
        /// 初始化技能卡牌
        /// </summary>
        private void InitializeSkillCards(BattleSkillsViewModel vm)
        {
            foreach (var skillData in vm.SkillCards.Value)
            {
                CreateOrUpdateSkillCard(skillData);
            }
        }

        /// <summary>
        /// 创建或更新技能卡牌 - 使用原界面的 BattleCardGroup
        /// </summary>
        private void CreateOrUpdateSkillCard(BattleCardGroup skillData)
        {
            if (mOldPanel?.CardSlot == null) return;

            GameObject cardObj;
            
            if (!_skillCards.TryGetValue(skillData.CardId, out cardObj))
            {

                var template = mOldPanel.CardInfo;
                cardObj = Instantiate(template.gameObject, mOldPanel.CardSlot);
                _skillCards.Add(skillData.CardId, cardObj);
            }

            // 更新显示
            UpdateSkillCardUI(cardObj, skillData);
        }

        /// <summary>
        /// 更新技能卡牌UI - 使用原界面的 BattleCardGroup
        /// </summary>
        private void UpdateSkillCardUI(GameObject cardObj, BattleCardGroup skillData)
        {
            cardObj.SetActive(true);
            cardObj.name = $"card_{skillData.CardId}";
            
            // 设置缩放
            cardObj.transform.localScale = Vector3.one * GlobalConst.SKILL_CARD_DEFAULT_SCALE;
            
            // 获取配置
            var cardCfg = skillData.CardBaseCfg;
            if (cardCfg == null) return;
            
            // 更新图标
            var painting = cardObj.transform.Find("Canvas/Painting")?.GetComponent<Image>();
            if (painting != null && !string.IsNullOrEmpty(cardCfg.Icon))
            {
                painting.sprite = Global.CardIconSpriteAtlas.GetSpriteExt(cardCfg.Icon);
            }
            
            var paintingBg = cardObj.transform.Find("Canvas/PaintingBg")?.GetComponent<Image>();
            if (paintingBg != null && !string.IsNullOrEmpty(cardCfg.Icon))
            {
                paintingBg.sprite = Global.CardIconSpriteAtlas.GetSpriteExt(cardCfg.Icon);
            }
            
            SkillCardUiHelper.ApplyQualityFrame(skillData, cardObj.transform);

            // 更新等级
            var levelText = cardObj.transform.Find("Canvas/Level/LevelText")?.GetComponent<Text>();
            if (levelText != null)
            {
                if (skillData.IsUnlock.Value)
                {
                    levelText.text = $"Lv {skillData.CardLevel}";
                }
                else
                {
                    levelText.text = $"{cardCfg.UnLockLevel}级解锁";
                }
            }
            
            // 更新名称
            var nameText = cardObj.transform.Find("Canvas/CardName")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = cardCfg.Name;
            }
            
            // 更新锁定状态
            UpdateSkillCardLockState(cardObj, skillData.IsUnlock.Value, skillData.CardBaseCfg?.UnLockLevel ?? 0);
            
            // 初始化冷却UI状态（默认隐藏）
            var coolingImage = cardObj.transform.Find("Canvas/cooling")?.GetComponent<Image>();
            if (coolingImage != null)
            {
                coolingImage.gameObject.SetActive(false);
                coolingImage.fillAmount = 0;
            }
            
            var timeSecond = cardObj.transform.Find("Canvas/Time/Second")?.GetComponent<TMP_Text>();
            var timeMillisecond = cardObj.transform.Find("Canvas/Time/Millisecond")?.GetComponent<TMP_Text>();
            if (timeSecond != null) timeSecond.transform.localScale = Vector3.zero;
            if (timeMillisecond != null) timeMillisecond.transform.localScale = Vector3.zero;
            
            // 检查 CardSlotLongClick 组件并初始化 dragState
            var cardLongClick = cardObj.GetComponent<CardSlotLongClick>();
            if (cardLongClick == null)
            {
                LogTool.LogError($"[BattleSkillsPanelNew] CardSlotLongClick 组件未找到: {skillData.CardId}");
            }
            else
            {
                LogTool.Log($"[BattleSkillsPanelNew] CardSlotLongClick 组件已找到: {skillData.CardId}");
                cardLongClick.cardId = skillData.CardId;
                // 初始化 dragState
                var specialCardCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(skillData.CardBaseCfg?.Id ?? 0);
                if (skillData.IsUnlock.Value && specialCardCfg?.PassiveSkill != 1)
                {
                    cardLongClick.dragState = DragState.GamePlay;
                    LogTool.Log($"[BattleSkillsPanelNew] 初始化 dragState=GamePlay: {skillData.CardId}");
                }
                else
                {
                    cardLongClick.dragState = DragState.DragFalse;
                    LogTool.Log($"[BattleSkillsPanelNew] 初始化 dragState=DragFalse: {skillData.CardId}, IsUnlock={skillData.IsUnlock.Value}, Passive={specialCardCfg?.PassiveSkill}");
                }
            }
            
            // 绑定点击和拖拽事件
            BindSkillCardEvents(cardObj, skillData);
        }
        
        /// <summary>
        /// 绑定技能卡牌事件（点击、拖拽、释放）
        /// </summary>
        private void BindSkillCardEvents(GameObject cardObj, BattleCardGroup skillData)
        {
            if (skillData?.CardBaseCfg == null) return;
            
            var cardCfg = skillData.CardBaseCfg;
            var specialCardCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(cardCfg.Id);
            if (specialCardCfg == null) return;
            
            // 点击事件
            var button = cardObj.GetComponent<Button>();
            if (button != null)
            {
                LogTool.Log($"[BattleSkillsPanelNew] 绑定点击事件: {skillData.CardId}");
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    LogTool.Log($"[BattleSkillsPanelNew] 点击技能: CardId={skillData.CardId}, IsUnlock={skillData.IsUnlock.Value}");
                    if (!skillData.IsUnlock.Value)
                    {
                        LogTool.Log($"[BattleSkillsPanelNew] 点击取消: 技能未解锁");
                        return;
                    }
                    
                    var longClick = cardObj.GetComponent<CardSlotLongClick>();
                    if (longClick != null && longClick.dragState != DragState.GamePlay &&
                        !Global.AutoReleaseSkillState.Value)
                    {
                        LogTool.Log($"[BattleSkillsPanelNew] 点击取消: dragState={longClick.dragState}");
                        return;
                    }
                    
                    // 找到技能所属的英雄并释放技能
                    bool foundSoldier = false;
                    try
                    {
                        GameLand.Default?.ClickSkillCard(cardCfg.Id, cardObj);
                        GamePlayManager.Default.TraverseVillageSoldier((soldierCard) =>
                        {
                            if (specialCardCfg.ActsOnCard == soldierCard.cardId)
                            {
                                foundSoldier = true;
                            }
                        });
                        if (!foundSoldier)
                        {
                            LogTool.LogWarning($"[SkillRelease-UI] 点击释放技能: 未找到对应英雄 ActsOnCard={specialCardCfg.ActsOnCard}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        LogTool.LogError($"[BattleSkillsPanelNew] 点击释放技能报错：{e}");
                    }
                    
                    // 引导触发技能
                    //Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS, GuideType.KillTheFirstMonster);
                });
            }
            
            // 拖拽事件
            var cardLongClick = cardObj.GetComponent<CardSlotLongClick>();
            if (cardLongClick != null)
            {
                LogTool.Log($"[BattleSkillsPanelNew] 绑定拖拽事件: {skillData.CardId}");
                
                // 清除旧的事件监听
                cardLongClick.onDragPressDown.RemoveAllListeners();
                cardLongClick.onDragPressup.RemoveAllListeners();
                
                // 拖拽开始
                cardLongClick.onDragPressDown.AddListener(() =>
                {
                    LogTool.Log($"[BattleSkillsPanelNew] 拖拽开始: CardId={skillData.CardId}, IsUnlock={skillData.IsUnlock.Value}, dragState={cardLongClick.dragState}");
                    
                    if (!skillData.IsUnlock.Value)
                    {
                        LogTool.Log($"[BattleSkillsPanelNew] 拖拽取消: 技能未解锁");
                        return;
                    }
                    if (cardLongClick.dragState != DragState.GamePlay)
                    {
                        LogTool.Log($"[BattleSkillsPanelNew] 拖拽取消: dragState={cardLongClick.dragState} 不是 GamePlay");
                        return;
                    }
                    
                    // 拖拽中保持卡牌默认缩放（统一从全局常量控制）
                    cardObj.transform.localScale = Vector3.one * GlobalConst.SKILL_CARD_DEFAULT_SCALE;
                    
                    // 显示特效
                    var effect = cardObj.transform.Find("Effect/LeaderActionEffect02")?.gameObject;
                    if (effect != null) effect.SetActive(true);
                    
                    
                    // 获取目标士兵
                    SoldierCard soldierCard = GamePlayManager.Default.GetVillageGameCard(specialCardCfg.ActsOnCard);
                    if (soldierCard != null)
                    {
                        var indicator = soldierCard.transform.Find("indicator");
                        if (indicator != null) indicator.localScale = Vector3.one;
                        
                        cardLongClick.GameDrag = true;
                        cardLongClick.SetSolidercard(soldierCard);
                        
                        if (specialCardCfg.Indicator.Count > 0)
                        {
                            cardLongClick.indicatorType = specialCardCfg.Indicator[0].Type;
                            cardLongClick.attackSize = new Vector2(
                                specialCardCfg.Indicator[0].ShapeScale.X,
                                specialCardCfg.Indicator[0].ShapeScale.Y) *
                                (soldierCard.BaseAttributes.GetFinal(AttributeType.SoldierAttackRange) / 8f);
                        }
                    }
                    
                    // 引导触发技能
                    //Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS, GuideType.KillTheFirstMonster);
                });
                
                // 拖拽释放
                cardLongClick.onDragPressup.AddListener(() =>
                {
                    LogTool.Log($"[BattleSkillsPanelNew] 拖拽释放: CardId={skillData.CardId}");
                    if (!skillData.IsUnlock.Value)
                    {
                        LogTool.Log($"[BattleSkillsPanelNew] 拖拽释放取消: 技能未解锁");
                        return;
                    }
                    
                    cardLongClick.GameDrag = false;
                    cardObj.transform.localScale = Vector3.one * GlobalConst.SKILL_CARD_DEFAULT_SCALE;
                    
                    
                    var lastPos = cardLongClick.lastpos;
                    var lastWordPos = cardLongClick.lastwordpos;
                    
                    // 播放释放特效
                    var cardUpEffect = GameObject.Instantiate(GamePlayManager.Default._resLoader.Get<GameObject>("SkillCardUse01"));
                    if (cardUpEffect != null)
                    {
                        cardUpEffect.transform.SetParent(transform);
                        cardUpEffect.transform.localScale = Vector3.one * 150;
                        cardUpEffect.transform.position = lastWordPos;
                    }
                    
                    // 检查释放位置是否有效（Y坐标大于250）
                    if (lastPos.y < 250f)
                    {
                        LogTool.LogWarning($"[SkillRelease-UI] 拖拽释放取消: y坐标 {lastPos.y} < 250, 技能 {cardCfg.Id}");
                        return;
                    }
                    
                    // 释放技能
                    bool foundSoldier = false;
                    try
                    {
                        GamePlayManager.Default.TraverseVillageSoldier((soldierCard) =>
                        {
                            if (specialCardCfg.ActsOnCard == soldierCard.cardId)
                            {
                                var indicator = soldierCard.transform.Find("indicator");
                                if (indicator != null) indicator.localScale = Vector3.zero;

                                var result = GameLand.Default != null &&
                                             GameLand.Default.TryReleaseSkillOnClick(cardCfg.Id, soldierCard,
                                                 lastWordPos);
                                foundSoldier = true;
                                if (!result)
                                {
                                    LogTool.LogWarning(
                                        $"[SkillRelease-UI] 拖拽释放失败: heroId={soldierCard.cardId}, skillId={cardCfg.Id}");
                                }
                            }
                        });
                        if (!foundSoldier)
                        {
                            LogTool.LogWarning($"[SkillRelease-UI] 拖拽释放技能: 未找到对应英雄 ActsOnCard={specialCardCfg.ActsOnCard}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        LogTool.LogError($"[SkillRelease-UI] 拖拽释放技能报错：{e}");
                    }
                    
                    // 引导触发技能
                    //Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS, GuideType.KillTheFirstMonster);
                });
            }
        }
        
        /// <summary>
        /// 更新技能锁定状态
        /// </summary>
        private void UpdateSkillCardLockState(GameObject cardObj, bool isUnlock, int unLockLevel)
        {
            var lockObj = cardObj.transform.Find("Canvas/Lock")?.gameObject;
            if (lockObj != null) lockObj.SetActive(!isUnlock);
            
            var maskNum = isUnlock ? (byte)255 : (byte)196;
            var color = new Color32(maskNum, maskNum, maskNum, 255);
            
            var cardImage = cardObj.GetComponent<Image>();
            if (cardImage != null) cardImage.color = color;
            
            var painting = cardObj.transform.Find("Canvas/Painting")?.GetComponent<Image>();
            if (painting != null) painting.color = color;
            
            // 更新等级显示
            var levelText = cardObj.transform.Find("Canvas/Level/LevelText")?.GetComponent<Text>();
            if (levelText != null)
            {
                if (isUnlock)
                {
                    // 如果是解锁状态，需要重新获取等级（这里简化处理）
                    var cardName = cardObj.name.Replace("card_", "");
                    int cardId;
                    if (int.TryParse(cardName, out cardId))
                    {
                        var card = Global.SoldierSkillTeams.FirstOrDefault(c => c.CardId == cardId);
                        if (card != null)
                        {
                            levelText.text = $"Lv {card.CardLevel}";
                        }
                    }
                }
                else
                {
                    levelText.text = $"{unLockLevel}级解锁";
                }
            }
            
            var levelObj = cardObj.transform.Find("Canvas/Level")?.gameObject;
            if (levelObj != null) levelObj.GetComponent<Image>().enabled = isUnlock;
        }

        /// <summary>
        /// 绑定事件
        /// </summary>
        private void BindEvents(BattleSkillsViewModel vm)
        {
            _skillSelectedHandler = (cardId) =>
            {
                LogTool.Log($"[BattleSkillsPanelNew] 技能选中: {cardId}");
            };
            vm.OnSkillSelected += _skillSelectedHandler;

            _skillReleasedHandler = (cardId) =>
            {
                LogTool.Log($"[BattleSkillsPanelNew] 技能释放: {cardId}");
            };
            vm.OnSkillReleased += _skillReleasedHandler;

            _skillUnlockChangedHandler = (cardId, isUnlock) =>
            {
                if (_skillCards.TryGetValue(cardId, out var cardObj))
                {
                    var card = vm.GetSkillCard(cardId);
                    if (card != null)
                    {
                        UpdateSkillCardLockState(cardObj, isUnlock, card.CardBaseCfg?.UnLockLevel ?? 0);
                    }
                }
            };
            vm.OnSkillUnlockChanged += _skillUnlockChangedHandler;

            _skillCoolingUpdatedHandler = (cardId, coolingTime) =>
            {
                UpdateSkillCoolingUI(cardId, coolingTime);
            };
            vm.OnSkillCoolingUpdated += _skillCoolingUpdatedHandler;

            // 订阅技能冷却开始事件（从 GameLand 发布）
            _skillCoolingStartedHandler = (obj) =>
            {
                if (obj is SkillCoolingData coolingData)
                {
                    SyncSkillCoolingUIFromGameLand(coolingData.SkillId);
                }
            };
            Global.Publisher.Subscribe(MessagePublisher.SKILL_COOLING_STARTED, _skillCoolingStartedHandler);
        }

        /// <summary>
        /// 从 GameLand 读取当前剩余 CD 并刷新 UI（支持卡琳 CD 修正等动态变更）。
        /// </summary>
        private void SyncSkillCoolingUIFromGameLand(int cardId)
        {
            if (GameLand.Default != null &&
                GameLand.Default.TryGetSkillCooldownProgress(cardId, out var remainTime, out var totalTime))
            {
                UpdateSkillCoolingUI(cardId, remainTime, totalTime);
                return;
            }

            ClearSkillCoolingUI(cardId);
        }

        /// <summary>
        /// 更新技能冷却 UI。totalTime 为初始 CD，remainTime 为当前剩余。
        /// </summary>
        private void UpdateSkillCoolingUI(int cardId, float remainTime, float totalTime)
        {
            if (!_skillCards.TryGetValue(cardId, out var cardObj) || cardObj == null)
            {
                return;
            }

            if (totalTime <= 0f || remainTime <= 0f)
            {
                ClearSkillCoolingUI(cardId);
                return;
            }

            var coolingImage = cardObj.transform.Find("Canvas/cooling")?.GetComponent<Image>();
            var timeSecond = cardObj.transform.Find("Canvas/Time/Second")?.GetComponent<TMP_Text>();
            var timeMillisecond = cardObj.transform.Find("Canvas/Time/Millisecond")?.GetComponent<TMP_Text>();
            var longClick = cardObj.GetComponent<CardSlotLongClick>();

            if (coolingImage == null)
            {
                return;
            }

            KillSkillCoolingTween(cardId);

            coolingImage.gameObject.SetActive(true);
            coolingImage.fillAmount = Mathf.Clamp01(remainTime / totalTime);

            if (timeSecond != null)
            {
                timeSecond.transform.localScale = Vector3.one;
                timeSecond.text = Mathf.CeilToInt(remainTime).ToString();
            }

            if (timeMillisecond != null)
            {
                timeMillisecond.transform.localScale = Vector3.one;
                timeMillisecond.text = $".{Mathf.FloorToInt(remainTime * 10) % 10}";
            }

            if (longClick != null)
            {
                longClick.dragState = DragState.DragFalse;
                longClick.GameWaitOver = false;
            }

            float currentRemain = remainTime;
            _skillCoolingTweens[cardId] = DOTween.To(
                () => currentRemain,
                v =>
                {
                    currentRemain = v;
                    coolingImage.fillAmount = Mathf.Clamp01(v / totalTime);

                    if (timeSecond != null)
                    {
                        timeSecond.text = Mathf.CeilToInt(v).ToString();
                    }

                    if (timeMillisecond != null)
                    {
                        timeMillisecond.text = $".{Mathf.FloorToInt(v * 10) % 10}";
                    }

                    if (longClick != null && longClick.IsDragging)
                    {
                        longClick.ForceResetPosition();
                    }
                },
                0f,
                remainTime)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    ClearSkillCoolingUI(cardId);
                    Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_CARD_COOLING, cardId);
                });
        }

        private void UpdateSkillCoolingUI(int cardId, float coolingTime)
        {
            UpdateSkillCoolingUI(cardId, coolingTime, coolingTime);
        }

        private void KillSkillCoolingTween(int cardId)
        {
            if (_skillCoolingTweens.TryGetValue(cardId, out var tween))
            {
                if (tween != null && tween.IsActive())
                {
                    tween.Kill();
                }

                _skillCoolingTweens.Remove(cardId);
            }
        }

        private void ClearSkillCoolingUI(int cardId)
        {
            KillSkillCoolingTween(cardId);

            if (!_skillCards.TryGetValue(cardId, out var cardObj) || cardObj == null)
            {
                return;
            }

            var coolingImage = cardObj.transform.Find("Canvas/cooling")?.GetComponent<Image>();
            var timeSecond = cardObj.transform.Find("Canvas/Time/Second")?.GetComponent<TMP_Text>();
            var timeMillisecond = cardObj.transform.Find("Canvas/Time/Millisecond")?.GetComponent<TMP_Text>();
            var longClick = cardObj.GetComponent<CardSlotLongClick>();

            if (coolingImage != null)
            {
                coolingImage.fillAmount = 0f;
                coolingImage.gameObject.SetActive(false);
            }

            if (timeSecond != null)
            {
                timeSecond.transform.localScale = Vector3.zero;
            }

            if (timeMillisecond != null)
            {
                timeMillisecond.transform.localScale = Vector3.zero;
            }

            if (longClick != null)
            {
                longClick.dragState = DragState.GamePlay;
                longClick.GameWaitOver = true;
            }
        }

        /// <summary>
        /// 刷新所有技能的冷却状态显示（初始化时调用）
        /// </summary>
        private void RefreshAllSkillCoolingStates(BattleSkillsViewModel vm)
        {
            LogTool.Log($"[BattleSkillsPanelNew] 刷新所有技能冷却状态, skill字典条目数: {GameLand.Default?.skill?.Count ?? 0}");

            foreach (var skillData in vm.SkillCards.Value)
            {
                int cardId = skillData.CardId;
                if (!_skillCards.TryGetValue(cardId, out var cardObj) || cardObj == null) continue;

                // 检查技能是否在冷却中
                var specialCardCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(cardId);
                if (specialCardCfg == null || specialCardCfg.PassiveSkill == 1) continue;

                if (GameLand.Default != null && GameLand.Default.skill.TryGetValue(cardId, out var coolSkill))
                {
                    LogTool.Log($"[BattleSkillsPanelNew] 技能ID={cardId}, Cool={coolSkill.Cool}, RemainTime={coolSkill.RemainTime:F2}, Lasttime0={coolSkill.Lasttime0:F2}");

                    if (coolSkill.Cool && coolSkill.RemainTime > 0)
                    {
                        // 技能正在冷却中，更新冷却UI
                        var coolingImage = cardObj.transform.Find("Canvas/cooling")?.GetComponent<Image>();
                        var timeSecond = cardObj.transform.Find("Canvas/Time/Second")?.GetComponent<TMP_Text>();
                        var timeMillisecond = cardObj.transform.Find("Canvas/Time/Millisecond")?.GetComponent<TMP_Text>();
                        var longClick = cardObj.GetComponent<CardSlotLongClick>();

                        if (coolingImage != null)
                        {
                            coolingImage.gameObject.SetActive(true);
                            float fillPercent = coolSkill.RemainTime / coolSkill.Lasttime0;
                            coolingImage.fillAmount = fillPercent;

                            LogTool.Log($"[BattleSkillsPanelNew] 设置冷却UI fillAmount={fillPercent:F2}");

                            // 更新时间文本
                            if (timeSecond != null)
                            {
                                int seconds = Mathf.CeilToInt(coolSkill.RemainTime);
                                timeSecond.text = seconds.ToString();
                                timeSecond.transform.localScale = Vector3.one;
                            }
                            if (timeMillisecond != null)
                            {
                                float ms = coolSkill.RemainTime % 1f;
                                timeMillisecond.text = $".{Mathf.FloorToInt(ms * 10)}";
                                timeMillisecond.transform.localScale = Vector3.one;
                            }

                            // 禁止拖拽
                            if (longClick != null)
                            {
                                longClick.dragState = DragState.DragFalse;
                                longClick.GameWaitOver = false;
                            }
                        }
                    }
                    else
                    {
                        // 技能未冷却，确保UI是隐藏的
                        LogTool.Log($"[BattleSkillsPanelNew] 技能ID={cardId} 未冷却");

                        var coolingImage = cardObj.transform.Find("Canvas/cooling")?.GetComponent<Image>();
                        var timeSecond = cardObj.transform.Find("Canvas/Time/Second")?.GetComponent<TMP_Text>();
                        var timeMillisecond = cardObj.transform.Find("Canvas/Time/Millisecond")?.GetComponent<TMP_Text>();

                        if (coolingImage != null)
                        {
                            coolingImage.fillAmount = 0;
                            coolingImage.gameObject.SetActive(false);
                        }
                        if (timeSecond != null) timeSecond.transform.localScale = Vector3.zero;
                        if (timeMillisecond != null) timeMillisecond.transform.localScale = Vector3.zero;

                        var longClick = cardObj.GetComponent<CardSlotLongClick>();
                        if (longClick != null)
                        {
                            longClick.dragState = DragState.GamePlay;
                            longClick.GameWaitOver = true;
                        }
                    }
                }
                else
                {
                    LogTool.Log($"[BattleSkillsPanelNew] 技能ID={cardId} 在 skill 字典中不存在");
                }
            }
        }

        /// <summary>
        /// 解绑事件
        /// </summary>
        private void UnbindEvents(BattleSkillsViewModel vm)
        {
            if (_skillSelectedHandler != null)
            {
                vm.OnSkillSelected -= _skillSelectedHandler;
                _skillSelectedHandler = null;
            }

            if (_skillReleasedHandler != null)
            {
                vm.OnSkillReleased -= _skillReleasedHandler;
                _skillReleasedHandler = null;
            }

            if (_skillUnlockChangedHandler != null)
            {
                vm.OnSkillUnlockChanged -= _skillUnlockChangedHandler;
                _skillUnlockChangedHandler = null;
            }

            if (_skillCoolingUpdatedHandler != null)
            {
                vm.OnSkillCoolingUpdated -= _skillCoolingUpdatedHandler;
                _skillCoolingUpdatedHandler = null;
            }

            if (_skillCoolingStartedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.SKILL_COOLING_STARTED, _skillCoolingStartedHandler);
                _skillCoolingStartedHandler = null;
            }

            if (_skillCoolingInitializedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.SKILL_COOLING_INITIALIZED, _skillCoolingInitializedHandler);
                _skillCoolingInitializedHandler = null;
            }

            foreach (var pair in _skillCoolingTweens)
            {
                if (pair.Value != null && pair.Value.IsActive())
                {
                    pair.Value.Kill();
                }
            }

            _skillCoolingTweens.Clear();
        }

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
            _skillCards.Clear();
        }
    }
}
