using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Battle;
using FusionContinent.Model;
using FusionContinent.Target;
using Game.Battle;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    public partial class GameLand
    {
        // 战斗中的技能卡数据（仅 CardType.Skill）
        private List<BattleCardGroup> _skillCards = new List<BattleCardGroup>();
        // 技能ID -> 技能卡UI对象
        private Dictionary<int, GameObject> _skillCardObjs = new Dictionary<int, GameObject>();
        // 技能ID -> 技能卡交互与UI状态处理器
        private Dictionary<int, SkillCardInfoHandler> _skillCardHandlers = new Dictionary<int, SkillCardInfoHandler>();
        private float _defaultSkillCardLayoutSpacing;
        private bool _hasCachedDefaultSkillCardLayoutSpacing;
        private Coroutine _skillCardAddTransitionCoroutine;
        private bool _isSkillCardAddTransitionPlaying;
        private const int PrimarySkillCardSlotMaxCount = 8;
        private const float SkillCardExitDuration = 0.2f;
        private const float SkillCardEntryDuration = 0.3f;
        private const float SkillCardExitToEntryDelay = 0f;
        private const float SkillCardOffscreenExtraWidthMultiplier = 6f;
        /// <summary>
        /// 点击技能卡时的飞入动画（临时关闭，恢复时改为 true）。
        /// </summary>
        private const bool SkillCardClickFlyAnimationEnabled = false;
        private const float SkillCardClickScaleDownDuration = 0.06f;
        private const float SkillCardClickScaleUpDuration = 0.12f;
        private const float SkillCardClickScaleFactor = 0.88f;
        private const string SkillCardClickScaleTweenId = "SkillCardClickScale";
        // 技能点击预锁计数：避免同一技能在关键帧前被无限连点；
        // 对有充能的技能允许有限并发（按当前充能数上限）。
        private readonly Dictionary<int, int> _pendingReleaseSkillCounts = new Dictionary<int, int>();

        void Start()
        {
            // 战场 pve/pvp 显隐由 SceneInitStartSpawning → InitializeBattleArena 统一处理，避免此处抢先用错误 Mode 初始化

            // 自动释放技能
            StartCoroutine(AutomaticallyReleaseSkills());

            // 初始化技能卡牌
            InitSkillCards();
            RefreshSkillCardHeroVisibility();
            SetAutoToggle();
            // 监听流派星级变化
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_SCHOOL_STAR, ChangeSchoolStar);
        }

        /// <summary>
        /// 初始化技能卡UI、点击/拖拽事件，以及冷却初始状态。
        /// 技能卡展示已迁至 BattleSkillsPanelNew；若 cardInfoObj/CardSlot 未绑定则只初始化数据。
        /// </summary>
        private void InitSkillCards()
        {
            // 遍历战斗卡组
            _skillCards = Global.SoldierSkillTeams.Where(t => t.CardType == CardType.Skill).ToList();
            _skillCardObjs.Clear();

            var useLegacySkillCardUi = cardInfoObj != null && CardSlot != null;
            if (!useLegacySkillCardUi)
            {
                LogTool.LogWarning(
                    "[GameLand] cardInfoObj/CardSlot 未绑定，跳过旧技能卡 Instantiate（由 BattleSkillsPanelNew 负责展示）");
                for (int i = 0; i < _skillCards.Count; i++)
                {
                    var card = _skillCards[i];
                    if (card.CardId == 0) continue;
                    card.ResetData();
                }

                ScheduleSkillCardsReady();
                return;
            }

            for (int i = 0; i < _skillCards.Count; i++)
            {
                var card = _skillCards[i];
                {
                    if (card.CardId == 0) continue;
                    card.ResetData(); // 重置数据

                    var cardCfg = card.CardBaseCfg;
                    var specialCardCfg = _specialEffectsConfig.GetOrDefault(cardCfg.Id);
                    var targetSlot = GetTargetSkillCardSlot();
                    var cardInfo = Instantiate(cardInfoObj.gameObject, targetSlot, true);
                    _skillCardObjs.Add(card.CardId, cardInfo);

                    cardInfo.gameObject.SetActive(true);
                    cardInfo.name = $"card_{cardCfg.Id}";
                    cardInfo.transform.position = transform.position;
                    cardInfo.LocalScale(Vector3.one * GlobalConst.SKILL_CARD_DEFAULT_SCALE);
                    cardInfo.transform.Find("Canvas/Mask/Painting").GetComponent<Image>().sprite =
                        Global.CardIconSpriteAtlas.GetSpriteExt(cardCfg.Icon);
                    SkillCardUiHelper.ApplyQualityFrame(card, cardInfo.transform);

                    cardInfo.transform.Find("Canvas/Level/LevelText").GetComponent<Text>().text =
                        $"Lv {card.CardLevel}";
                    cardInfo.transform.Find("Canvas/CardName").GetComponent<TMP_Text>().text = $"{cardCfg.Name}";
                    UpdateCardInfoQuality(card.GetQualityType(), cardInfo);

                    // 创建并初始化 Handler（初始化时自动绑定解锁状态监听）
                    var handler = new SkillCardInfoHandler();
                    handler.Initialize(card, cardInfo, i);
                    _skillCardHandlers.Add(card.CardId, handler);

                    var cardLongClick = cardInfo.GetComponent<CardSlotLongClick>();
                    // 监听点击事件（改为 pointer tap 触发，不依赖 Button.onClick）
                    cardLongClick.onTap.AddListener(() => HandleSkillCardClick(cardInfo, cardCfg.Id, cardCfg.Name));
                    // 拖拽
                    // 供 CardSlotLongClick 内部日志与状态判定使用（如 ShouldBlockDrag）。
                    cardLongClick.cardId = cardCfg.Id;
                    // 拖拽开始：显示指示器并设置拖拽判定参数（范围、类型等）
                    cardLongClick.onDragPressDown.AddListener(() =>
                    {
                        if (!card.IsUnlock.Value ||
                            cardInfo.GetComponent<CardSlotLongClick>().dragState != DragState.GamePlay)
                            return;

                        cardInfo.transform.localScale = Vector3.one * GlobalConst.SKILL_CARD_DEFAULT_SCALE;
                        cardInfo.transform.Find("Effect/LeaderActionEffect02").gameObject.SetActive(true);

                        SoldierCard soldierCard = GamePlayManager.Default.GetVillageGameCard(specialCardCfg.ActsOnCard);
                        if (soldierCard == null)
                        {
                            LogTool.LogWarning($"[GameLand] 技能拖拽失败：未找到所属英雄，skillId={cardCfg.Id}, heroId={specialCardCfg.ActsOnCard}");
                            return;
                        }
                        soldierCard.transform.Find("indicator").localScale = Vector3.one;
                        cardLongClick.GameDrag = true;
                        cardLongClick.SetSolidercard(soldierCard);
                        cardLongClick.indicatorType = specialCardCfg.Indicator[0].Type;
                        cardLongClick.attackSize = new Vector2(
                                                       specialCardCfg.Indicator[0].ShapeScale.X,
                                                       specialCardCfg.Indicator[0].ShapeScale.Y) *
                                                   (soldierCard.BaseAttributes.GetFinal(
                                                       AttributeType.SoldierAttackRange) / 8f);

                        // 引导触发技能
                        Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS, GuideType.SkillGuide);
                    });
                    // 拖拽结束：在有效区域时触发技能释放
                    cardLongClick.onDragPressup.AddListener(() =>
                    {
                        if (!card.IsUnlock.Value)
                            return;

                        cardLongClick.GameDrag = false;
                        cardInfo.transform.localScale = Vector3.one * GlobalConst.SKILL_CARD_DEFAULT_SCALE;

                        var lastPos = cardLongClick.lastpos;
                        var lastWordPos = cardLongClick.lastwordpos;
                        GameObject cardUpEffect = GamePlayManager.Default._resLoader
                            .Get<GameObject>("SkillCardUse01").Instantiate();
                        cardUpEffect.transform.SetParent(this.transform);
                        cardUpEffect.transform.localScale = Vector3.one * 150;
                        cardUpEffect.transform.position = lastWordPos;
                        if (lastPos.y < 250f) return;

                        // 释放技能所属英灵
                        GamePlayManager.Default.TraverseVillageSoldier((soldierCard) =>
                        {
                            if (specialCardCfg.ActsOnCard == soldierCard.cardId)
                            {
                                soldierCard.transform.Find("indicator").localScale = Vector3.zero;
                                TryReleaseSkillOnClick(cardCfg.Id, soldierCard, lastWordPos);
                            }
                        });
                        

                        // 引导触发技能
                        Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS, GuideType.SkillGuide);
                    });

                    // 监听长按事件
                    cardLongClick.onLongPress.AddListener(() =>
                    {
                        Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS,
                            GuideType.LongPressSkillShowDetails);
                        Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS_SPE, cardCfg.Id);
                    });
                    cardLongClick.onLongPressUp.AddListener(() =>
                    {
                        Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS, GuideType.SkillGuide);
                    });
                }

                DistributeCardRotations(_skillCardObjs.Values.ToList());


                UpdateSkillCardLayoutSpacing();
            }

            ScheduleSkillCardsReady();
        }

        private void ScheduleSkillCardsReady()
        {
            // 所有卡牌创建完成后，统一初始化为可释放（不读初始CD）
            DOVirtual.DelayedCall(0.1f, () =>
            {
                foreach (var skillCard in _skillCards)
                {
                    if (skillCard.CardId == 0) continue;
                    SetSkillCardReady(skillCard.CardBaseCfg.Id);
                }

                // 所有技能状态初始化完成后发布事件
                LogTool.Log("[GameLand] 所有技能初始化为可释放状态完成");
            });
        }

        /// <summary>
        /// 按英雄上场状态刷新技能卡可见性：英雄未上场时不显示对应技能卡。
        /// </summary>
        private void RefreshSkillCardHeroVisibility()
        {
            var visibilityChanged = false;
            var visibleBeforeCardIds = new List<int>();
            var pendingShowCardIds = new List<int>();
            var hasNewVisibleCard = false;
            foreach (var pair in _skillCardObjs)
            {
                var skillId = pair.Key;
                var cardObj = pair.Value;
                if (cardObj == null) continue;

                var wasVisible = cardObj.activeSelf;
                if (wasVisible)
                {
                    visibleBeforeCardIds.Add(skillId);
                }
                var shouldShow = IsSkillHeroAvailable(skillId);
                if (wasVisible && !shouldShow)
                {
                    cardObj.SetActive(false);
                    visibilityChanged = true;
                }

                if (!wasVisible && shouldShow)
                {
                    hasNewVisibleCard = true;
                    pendingShowCardIds.Add(skillId);
                    visibilityChanged = true;
                }

                if (!shouldShow)
                {
                    ExitPendingRelease(skillId);
                }
            }

            if (visibilityChanged && !hasNewVisibleCard)
            {
                UpdateSkillCardLayoutSpacing();
            }

            // 新增可见技能卡时触发入场流程：
            // 1) 原本无卡：直接全量飞入；
            // 2) 原本有卡：先让当前卡牌飞出，再让“当前可见全量卡牌”飞入。
            if (hasNewVisibleCard)
            {
                PlaySkillCardAddTransition(visibleBeforeCardIds, pendingShowCardIds);
            }
        }

        private void PlaySkillCardAddTransition(List<int> visibleBeforeCardIds, List<int> pendingShowCardIds)
        {
            // 过场执行中不允许重入，避免“上一轮还在飞出，下一轮已切飞入”。
            if (_isSkillCardAddTransitionPlaying)
            {
                return;
            }

            if (_skillCardAddTransitionCoroutine != null)
            {
                StopCoroutine(_skillCardAddTransitionCoroutine);
                _skillCardAddTransitionCoroutine = null;
            }

            _skillCardAddTransitionCoroutine = StartCoroutine(
                PlaySkillCardAddTransitionCoroutine(visibleBeforeCardIds, pendingShowCardIds));
        }

        private IEnumerator PlaySkillCardAddTransitionCoroutine(List<int> visibleBeforeCardIds, List<int> pendingShowCardIds)
        {
            if (_isSkillCardAddTransitionPlaying)
            {
                yield break;
            }

            _isSkillCardAddTransitionPlaying = true;

            if (CardSlot == null && CardSlot2 == null)
            {
                _isSkillCardAddTransitionPlaying = false;
                yield break;
            }

            // LayoutRebuilder.ForceRebuildLayoutImmediate(cardSlotRect);
            // UIHelper.SafeRefreshLayout(cardSlotRect);

            var exitCards = new List<GameObject>();
            if (visibleBeforeCardIds != null && visibleBeforeCardIds.Count > 0)
            {
                foreach (var cardId in visibleBeforeCardIds)
                {
                    if (!_skillCardObjs.TryGetValue(cardId, out var cardObj) || cardObj == null) continue;
                    if (!cardObj.activeSelf) continue;
                    exitCards.Add(cardObj);
                }
            }
            var exitSequence = DOTween.Sequence().SetUpdate(true);
            if (exitCards.Count > 0)
            {
                foreach (var cardObj in exitCards)
                {
                    SetSkillCardCanvasExit(cardObj, true);
                }

                
                foreach (var cardObj in exitCards)
                {
                    var rect = cardObj.GetComponent<RectTransform>();
                    var canvasRect = GetSkillCardCanvasRect(cardObj);
                    if (rect == null || canvasRect == null) continue;
                    var exitOffsetX = GetSkillCardOffscreenRightOffsetX(rect);
                    exitSequence.Join(canvasRect.DOAnchorPosX(exitOffsetX, SkillCardExitDuration).SetEase(Ease.InCubic)
                        .SetUpdate(true));
                }
            }

            exitSequence.Play();
            yield return exitSequence.WaitForCompletion();
            // 飞出完成后至少等一帧再切 entry，防止同帧状态切换导致视觉抢跑。
            yield return null;
            
            // 旧卡退出后再激活新卡，避免新卡先出现在界面再瞬移到右侧。
            if (pendingShowCardIds != null)
            {
                foreach (var cardId in pendingShowCardIds)
                {
                    if (!_skillCardObjs.TryGetValue(cardId, out var cardObj) || cardObj == null) continue;
                    if (!cardObj.activeSelf)
                    {
                        cardObj.SetActive(true);
                    }

                    // 新卡真正显示前后都要同步一次冷却状态，避免显示旧CD。
                    RefreshSkillCardCooldownState(cardId);
                }
            }

            UpdateSkillCardLayoutSpacing();
            // LayoutRebuilder.ForceRebuildLayoutImmediate(cardSlotRect);
            // UIHelper.SafeRefreshLayout(cardSlotRect);

            var visibleCards = GetVisibleSkillCardsInSequence();
            if (visibleCards.Count == 0)
            {
                _isSkillCardAddTransitionPlaying = false;
                _skillCardAddTransitionCoroutine = null;
                yield break;
            }

            foreach (var cardObj in visibleCards)
            {
                var rect = cardObj.GetComponent<RectTransform>();
                var canvasRect = GetSkillCardCanvasRect(cardObj);
                if (rect == null || canvasRect == null) continue;
                canvasRect.anchoredPosition = new Vector2(GetSkillCardOffscreenRightOffsetX(rect), canvasRect.anchoredPosition.y);
                PlaySkillCardCanvasEntry(cardObj);
            }

            var entrySequence = DOTween.Sequence().SetUpdate(true);
            foreach (var cardObj in visibleCards)
            {
                var canvasRect = GetSkillCardCanvasRect(cardObj);
                if (canvasRect == null) continue;
                entrySequence.Join(canvasRect.DOAnchorPosX(0f, SkillCardEntryDuration).SetEase(Ease.OutCubic).SetUpdate(true));
            }
            entrySequence.Play();
            yield return entrySequence.WaitForCompletion();
            _isSkillCardAddTransitionPlaying = false;
            _skillCardAddTransitionCoroutine = null;
        }

        private List<GameObject> GetVisibleSkillCardsInSequence()
        {
            var result = new List<GameObject>();
            foreach (var card in _skillCards)
            {
                if (card == null || card.CardId == 0) continue;
                if (!_skillCardObjs.TryGetValue(card.CardId, out var cardObj) || cardObj == null) continue;
                if (!cardObj.activeSelf) continue;
                result.Add(cardObj);
            }

            return result;
        }

        private RectTransform GetSkillCardCanvasRect(GameObject cardObj)
        {
            var canvasNode = cardObj.transform.Find("Canvas");
            return canvasNode != null ? canvasNode.GetComponent<RectTransform>() : null;
        }

        private float GetSkillCardOffscreenRightOffsetX(RectTransform cardRect)
        {
            var parentRect = cardRect.parent as RectTransform;
            if (parentRect == null) return cardRect.rect.width * SkillCardOffscreenExtraWidthMultiplier;
            return parentRect.rect.width * 0.5f + cardRect.rect.width * SkillCardOffscreenExtraWidthMultiplier;
        }

        private void SetSkillCardCanvasExit(GameObject cardObj, bool isExit)
        {
            var canvasNode = cardObj.transform.Find("Canvas");
            if (canvasNode == null) return;
            var animator = canvasNode.GetComponent<Animator>();
            if (animator == null) return;

            animator.SetBool("exit", isExit);
        }

        private void PlaySkillCardCanvasEntry(GameObject cardObj)
        {
            var canvasNode = cardObj.transform.Find("Canvas");
            if (canvasNode == null) return;
            var animator = canvasNode.GetComponent<Animator>();
            if (animator == null) return;

            // 先置为 entry 分支，再强制从 entry 起点播放，确保“旧卡+新卡”同时播入场动画。
            animator.SetBool("exit", false);
            animator.Play("ani_cardentry_01", 0, 0f);
        }

        /// <summary>
        /// 刷新技能卡冷却状态（用于隐藏后重新显示时的CD同步）。
        /// </summary>
        private void RefreshSkillCardCooldownState(int skillId)
        {
            if (!_skillCardHandlers.TryGetValue(skillId, out var handler))
            {
                return;
            }

            if (!skill.TryGetValue(skillId, out var coolSkill))
            {
                // 首次显示且尚未建立冷却状态时，优先立即初始化，避免自动模式下先被直接释放。
                if (!TryStartSkillCoolingImmediately(skillId))
                {
                    SetCoolingSkillCard(skillId);
                }
                return;
            }

            if (coolSkill.RemainTime > 0f)
            {
                var hasCharging = coolSkill.ChargingNum > 0;
                var totalTime = coolSkill.Lasttime0 > 0f ? coolSkill.Lasttime0 : Mathf.Max(coolSkill.RemainTime, 0.01f);
                handler.StartCooldown(hasCharging);
                handler.UpdateCooldownUI(coolSkill.RemainTime, totalTime);
                if (coolSkill.ChargingNum > 0)
                {
                    handler.UpdateChargingUI(coolSkill.ChargingNum);
                }
                return;
            }

            handler.OnCoolingComplete_External(Global.AutoReleaseSkillState.Value);
            if (coolSkill.ChargingNum > 0)
            {
                handler.UpdateChargingUI(coolSkill.ChargingNum);
            }
        }

        /// <summary>
        /// 立即按技能当前CD初始化冷却（成功返回 true）。
        /// </summary>
        private bool TryStartSkillCoolingImmediately(int skillId)
        {
            var specialCardCfg = _specialEffectsConfig.GetOrDefault(skillId);
            if (specialCardCfg == null || specialCardCfg.ActsOnCard == 0) return false;

            var villageCard = GamePlayManager.Default.GetVillageGameCard(specialCardCfg.ActsOnCard);
            if (villageCard == null) return false;

            var skillCard = villageCard.GetSkillById(specialCardCfg.Id);
            if (skillCard == null) return false;

            var cdTime = skillCard.Attributes.GetFinal(AttributeType.SkillCdTime) + 0.85f;
            if (cdTime <= 0f) return false;

            CoolingSkillCard(skillId, cdTime);
            return true;
        }

        /// <summary>
        /// 新增英灵后，主动初始化该英灵对应技能卡的CD状态。
        /// </summary>
        public void InitializeSkillCardCoolingForSoldier(int soldierCardId)
        {
            // 新增英灵后先刷新技能卡可见性，确保对应技能卡及时显示并参与后续CD初始化。
            RefreshSkillCardHeroVisibility();

            foreach (var skillCard in _skillCards)
            {
                if (skillCard.CardId == 0 || skillCard.CardType != CardType.Skill) continue;

                var specialCardCfg = _specialEffectsConfig.GetOrDefault(skillCard.CardId);
                if (specialCardCfg == null) continue;
                if (specialCardCfg.ActsOnCard != soldierCardId) continue;

                if (!skill.TryGetValue(skillCard.CardId, out var coolSkill) || !coolSkill.Cool)
                {
                    if (!TryStartSkillCoolingImmediately(skillCard.CardId))
                    {
                        SetCoolingSkillCard(skillCard.CardId);
                    }
                }
            }
        }

        /// <summary>
        /// 新增英灵后，将该英灵对应技能卡直接置为可释放状态（不读初始CD）。
        /// </summary>
        public void InitializeSkillCardReadyForSoldier(int soldierCardId)
        {
            RefreshSkillCardHeroVisibility();

            foreach (var skillCard in _skillCards)
            {
                if (skillCard.CardId == 0 || skillCard.CardType != CardType.Skill) continue;

                var specialCardCfg = _specialEffectsConfig.GetOrDefault(skillCard.CardId);
                if (specialCardCfg == null) continue;
                if (specialCardCfg.ActsOnCard != soldierCardId) continue;

                SetSkillCardReady(skillCard.CardId);
            }

            // cardScroll 技能列表依赖上场英雄，英灵入场后立即刷新
            if (GameLandUI.Default != null)
            {
                GameLandUI.Default.RefreshSkillCardScrollAfterSoldierEnter();
            }
        }

        /// <summary>
        /// 强制将技能置为可释放状态，并同步更新对应UI。
        /// </summary>
        private void SetSkillCardReady(int skillId)
        {
            if (skill.TryGetValue(skillId, out var coolSkill))
            {
                if (coolSkill.coolTween != null && coolSkill.coolTween.IsActive())
                {
                    coolSkill.coolTween.Kill();
                }

                coolSkill.Cool = false;
                coolSkill.Lasttime = 0;
                coolSkill.RemainTime = 0;
            }
            else
            {
                skill[skillId] = new CoolSkill();
            }

            if (_skillCardHandlers.TryGetValue(skillId, out var handler))
            {
                handler.OnCoolingComplete_External(Global.AutoReleaseSkillState.Value);
            }
        }

        /// <summary>
        /// 技能所属英雄是否已上场（ActsOnCard=0 视为全体可用）。
        /// 已死亡的英雄视为未上场。
        /// </summary>
        private bool IsSkillHeroAvailable(int skillId)
        {
            var specialCardCfg = _specialEffectsConfig.GetOrDefault(skillId);
            if (specialCardCfg == null) return false;
            if (specialCardCfg.ActsOnCard == 0) return true;

            return GamePlayManager.Default != null &&
                   GamePlayManager.Default.villageGameCards.Any(v =>
                       v != null &&
                       v.RoleType == SoldierRoleType.Self &&
                       !v.isDie &&
                       v.cardId == specialCardCfg.ActsOnCard);
        }

        // private void Re
        public void UpdateCardInfoQuality(QualityType qualityType,GameObject cardObj)
        {
            SkillCardUiHelper.ApplyQualityFrame(cardObj.transform, qualityType);

            var bgIcon = Global.CardStyleIconSpriteAtlas.GetSprite(qualityType.ToString());
            if (bgIcon == null)
            {
                bgIcon = Global.CardStyleIconSpriteAtlas.GetSprite(QualityType.Ordinary.ToString());
            }
            cardObj.transform.Find("Canvas/Mask/Bg").GetComponent<Image>().sprite =
                bgIcon;
            if (qualityType != QualityType.Default)
            {
                var effectParent = cardObj.transform.Find("Canvas/Effect");
                var effectName = $"SkillCard{qualityType.ToString()}01";
                if (effectParent.Find(effectName) == null)
                {
                    for (int i = 0; i < effectParent.childCount; i++)
                        effectParent.GetChild(i).gameObject.SetActive(false);
                            
                    var pool = CommonItemPoolManager.Instance.GetGameObjectPool(effectName);
                    if (pool != null)
                    {
                        GameObject skillEffect = pool.Get();
                        skillEffect.name = effectName;
                        skillEffect.gameObject.SetActive(true);
                        skillEffect.transform.SetParent(effectParent);
                        skillEffect.transform.localScale = Vector3.one * 100;
                        skillEffect.transform.position = cardObj.transform.position;
                    }
                }
                else
                {
                    effectParent.Find(effectName).gameObject.SetActive(true);
                }
            }
        }
        // 更改流派星级
        private void ChangeSchoolStar(object obj)
        {
            if (transform == null) return;

            var info = (ChangeSchoolStar)obj;
            var schoolCfg = DataConfigManager.Tables.TbSchoolConfig.GetOrDefault(info.SchoolId);
            if (schoolCfg is not { SchoolType: SchoolType.Skill }) return;
            if (schoolCfg.Id == 400011)
            {
                Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_SELECT_CARD,schoolCfg.Id);
                // GamePlayManager.Default.TriggerGuide(GuideType.LongPressSkillShowDetails); // 引导技能详情
            }
            if (!_skillCardHandlers.ContainsKey(schoolCfg.Id)) return;
            Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_SELECT_CARD,schoolCfg.Id);
            // GamePlayManager.Default.TriggerGuide(GuideType.LongPressSkillShowDetails); // 引导 - 长按技能提示
            foreach (var cfg in schoolCfg.CardCountList)
            {
                if (cfg.CardCount == info.StarLv)
                {
                    

                    break;
                }
            }
        }


        public float maxRotationAngle = 30f; // 最大旋转角度
        public float cardSpacing = 1f; // 卡牌间距

        private void UpdateSkillCardLayoutSpacing()
        {
            var hasFanLayout = (CardSlot != null && CardSlot.GetComponent<CardFanLayout>() != null) ||
                               (CardSlot2 != null && CardSlot2.GetComponent<CardFanLayout>() != null);
            RefreshSkillCardLayoutForSlot(CardSlot);
            RefreshSkillCardLayoutForSlot(CardSlot2);

            // 双卡槽都没有 CardFanLayout 时，沿用旧逻辑兜底。
            if ((CardSlot == null || CardSlot.GetComponent<CardFanLayout>() == null) &&
                (CardSlot2 == null || CardSlot2.GetComponent<CardFanLayout>() == null))
            {
                RefreshSkillCardHorizontalLayoutFallback(CardSlot);
            }

            // 使用 CardFanLayout 时不再重排 sibling，避免卡牌顺序被反转。
            if (!hasFanLayout)
            {
                RefreshSkillCardSiblingOrderBySkillSequence();
            }
        }

        private void RefreshSkillCardSiblingOrderBySkillSequence()
        {
            RefreshSkillCardSiblingOrderForSlot(CardSlot);
            RefreshSkillCardSiblingOrderForSlot(CardSlot2);
        }

        private float GetSkillCardLayoutSpacing(int cardCount)
        {
            if (cardCount <= 5)
            {
                return _defaultSkillCardLayoutSpacing;
            }

            switch (cardCount)
            {
                case 6:
                    return -20f;
                case 7:
                    return -45f;
                case 8:
                    return -60f;
                default:
                    // 9 张及以上继续压缩间距，略微加大压缩，避免贴屏边（最多按 12 张压缩）
                    var extraCount = Mathf.Min(cardCount, 12) - 8;
                    return -60f - extraCount * 12f;
            }
        }

        private void DistributeCardRotations(List<GameObject> cards)
        {
            int cardCount = cards.Count;

            // 如果没有卡牌，直接返回
            if (cardCount == 0) return;

            // 计算总宽度
            float totalWidth = (cardCount - 1) * cardSpacing;

            // 计算起始位置（使卡牌组居中）
            Vector3 startPos = transform.position - new Vector3(totalWidth / 2, 0, 0);

            for (int i = 0; i < cardCount; i++)
            {
                if (cards[i] == null) continue;

                // 计算位置
                Vector3 cardPos = startPos + new Vector3(i * cardSpacing, 0, 0);
                cards[i].transform.position = cardPos;

                // 计算旋转角度
                float rotationAngle = 0f;

                if (cardCount == 1)
                {
                    // 一张卡牌不旋转
                    rotationAngle = 0f;
                }
                else
                {
                    // 计算旋转角度（从负到正均匀分布）
                    float t = i / (float)(cardCount - 1);
                    rotationAngle = Mathf.Lerp(-maxRotationAngle, maxRotationAngle, t);
                }

                // 应用旋转（绕Y轴旋转）
                cards[i].transform.rotation = Quaternion.Euler(0, 0, -rotationAngle / 10.0f);
            }
        }

        /// <summary>
        /// 技能卡点击缩放反馈：仅缩放子节点 Canvas（按下略缩、回弹）。
        /// </summary>
        private void PlaySkillCardClickScaleTween(GameObject cardRoot)
        {
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.TweenPlay))
            {
            if (cardRoot == null) return;

            var canvas = cardRoot.transform.Find("Canvas");
            if (canvas == null)
            {
                LogTool.LogWarning($"[SkillClick] 点击缩放失败: {cardRoot.name} 未找到 Canvas");
                return;
            }

            DOTween.Kill(canvas, SkillCardClickScaleTweenId);
            var baseScale = canvas.localScale;

            DOTween.Sequence()
                .Append(canvas.DOScale(baseScale * SkillCardClickScaleFactor, SkillCardClickScaleDownDuration)
                    .SetEase(Ease.OutQuad))
                .Append(canvas.DOScale(baseScale, SkillCardClickScaleUpDuration)
                    .SetEase(Ease.OutBack))
                .SetId(SkillCardClickScaleTweenId)
                .SetTarget(canvas);
            }
        }

        private int GetCityLevelUpperLimit()
        {
            var difficultyCfg = _cityLevelConfig.GetOrDefault(Global.GameDifficultyLevel.Value);
            return difficultyCfg?.LevelList?.Max(cfg => cfg.Level) ?? 0;
        }

        

       

        /**
         * 检查卡牌最大等级
         */
        private bool CheckCardMaxLevel(BattleCardGroup card)
        {
            // 战士没有等级上限
            if (card.CardType == CardType.Soldier)
            {
                // return false;
            }

            var cityLevelUpperLimit = GetCityLevelUpperLimit();
            if (cityLevelUpperLimit > 0 && card.CardLevel >= cityLevelUpperLimit)
            {
                return true;
            }

            return false;
        }

        /**
         * 获取卡牌的法力值
         */
        private int GetCardMana(int cardId, int cardLevel)
        {
            var cardCfg = _cardBaseConfig.GetOrDefault(cardId);
            switch (cardCfg.Type)
            {
                case CardType.Skill:
                    var specialCfg = _specialEffectsConfig.GetOrDefault(cardId);
                    if (specialCfg.PassiveSkill == 1)
                    {
                        var levelList = specialCfg.LevelPropertyList[cardLevel - 1];
                        // return levelList.MaxManaCorrect;
                    }
                    return specialCfg.NeedGameMana;
            }

            return 0;
        }

        public void RefreshCardNeedGameMana(int cardId, int needGameMana)
        {
            for (int i = 0; i < Global.SoldierSkillTeams.Count; i++)
            {
                var card = Global.SoldierSkillTeams[i];
                if (card.CardId == cardId)
                {
                    if (!_skillCardObjs.TryGetValue(cardId, out var cardInfo) || cardInfo == null)
                    {
                        return;
                    }
                    cardInfo.transform.Find("Canvas/ManaBox").transform.Find("ManaText").GetComponent<Text>().text =
                        $"{needGameMana}";
                    break;
                }
            }
        }

        /// <summary>
        /// 根据技能所属英雄当前属性，初始化技能冷却（含延迟重试，避免英雄尚未入场）。
        /// </summary>
        private void SetCoolingSkillCard(int cardId)
        {
            var specialCardCfg = _specialEffectsConfig.GetOrDefault(cardId);
            if (specialCardCfg == null || specialCardCfg.ActsOnCard == 0) return;

            LogTool.Log($"[GameLand] SetCoolingSkillCard 开始: cardId={cardId}, ActsOnCard={specialCardCfg.ActsOnCard}");

            // 使用重试机制确保 villageGameCards 已初始化
            int maxRetries = 20;
            int retryCount = 0;
            void TryInit()
            {
                var villageCard = GamePlayManager.Default.GetVillageGameCard(specialCardCfg.ActsOnCard);
                if (villageCard == null)
                {
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        DOVirtual.DelayedCall(0.1f, TryInit);
                    }
                    else
                    {
                        LogTool.LogWarning($"[GameLand] SetCoolingSkillCard: 重试{maxRetries}次后仍找不到英灵 ID={specialCardCfg.ActsOnCard}, 技能ID={cardId}");
                    }
                    return;
                }

                LogTool.Log($"[GameLand] SetCoolingSkillCard 找到英灵: {villageCard.cardId}");

                var skillCard = villageCard.GetSkillById(specialCardCfg.Id);
                if (skillCard == null)
                {
                    LogTool.LogWarning($"[GameLand] SetCoolingSkillCard: 英灵 {villageCard.cardId} 没有技能 ID={specialCardCfg.Id}");
                    return;
                }

                LogTool.Log($"[GameLand] SetCoolingSkillCard 找到技能: skillId={specialCardCfg.Id}, CdTime={skillCard.Attributes.GetFinal(AttributeType.SkillCdTime)}");

                var cdTime = skillCard.Attributes.GetFinal(AttributeType.SkillCdTime) + 0.85f;
                CoolingSkillCard(specialCardCfg.Id, cdTime);
            }

            TryInit();
        }


        public void CoolingSubSkillCard(int skillId, int cdTime)
        {
            if (skill.ContainsKey(skillId))
            {
                if (skill[skillId].Cool)
                {
                    CoolingSkillCard(skillId, skill[skillId].Lasttime - cdTime);
                }
            }
        }

        /// <summary>
        /// 减少指定技能当前冷却时间
        /// </summary>
        /// <param name="skillId">技能ID</param>
        /// <param name="reduceValue">减少的冷却值</param>
        public void ReduceSkillCooling(int skillId, float reduceValue)
        {
            if (!skill.ContainsKey(skillId) || !skill[skillId].Cool || reduceValue <= 0)
                return;

            float newRemainTime = Mathf.Max(0, skill[skillId].RemainTime - reduceValue);

            if (newRemainTime <= 0)
            {
                // 冷却直接完成
                if (skill[skillId].coolTween != null && skill[skillId].coolTween.IsActive())
                {
                    skill[skillId].coolTween.Kill();
                }
                skill[skillId].Cool = false;
                skill[skillId].Lasttime = 0;
                skill[skillId].RemainTime = 0;
                Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_CARD_COOLING, skillId);

                if (_skillCardHandlers.TryGetValue(skillId, out var handler))
                {
                    handler.OnCoolingComplete_External(Global.AutoReleaseSkillState.Value);
                }
            }
            else
            {
                skill[skillId].RemainTime = newRemainTime;
                float progress = 1f - (newRemainTime / skill[skillId].Lasttime0);
                skill[skillId].coolTween?.Goto(progress * skill[skillId].Lasttime0, andPlay: true);
            }
        }

        /// <summary>
        /// 根据角色cardId减少该角色所有技能的冷却时间
        /// </summary>
        /// <param name="soldierCardId">角色cardId</param>
        /// <param name="reduceValue">减少的冷却值</param>
        public void ReduceSoldierAllSkillsCooling(int soldierCardId, float reduceValue)
        {
            foreach (var card in Global.SoldierSkillTeams)
            {
                if (card.CardId == 0 || card.CardType != CardType.Skill)
                    continue;

                var specialCardCfg = _specialEffectsConfig.GetOrDefault(card.CardId);
                if (specialCardCfg == null)
                    continue;

                // ActsOnCard 为0表示作用于所有角色，否则只作用于指定角色
                if (specialCardCfg.ActsOnCard == 0 || specialCardCfg.ActsOnCard == soldierCardId)
                {
                    ReduceSkillCooling(card.CardId, reduceValue);
                }
            }
        }

        /**
         * 开始释放技能
         */
        private void StartReleaseSkill(int cardId, Vector3 targetPosition = default)
        {
            var specialCardCfg = _specialEffectsConfig.GetOrDefault(cardId);
            // 遍历场上相同的角色
            foreach (var v in GamePlayManager.Default.villageGameCards)
            {
                if (specialCardCfg.ActsOnCard == 0 || v.cardId == specialCardCfg.ActsOnCard)
                {
                    v.transform.Find("indicator").localScale = Vector3.zero;
                    RequestSoldierCastSkill(v, cardId, false, targetPosition, null);
                }
            }
        }


        /// <summary>
        /// 获取技能卡 CD 进度（供 UI 同步 cooling 倒计时）。
        /// </summary>
        public bool TryGetSkillCooldownProgress(int skillId, out float remainTime, out float totalTime)
        {
            remainTime = 0f;
            totalTime = 0f;
            if (!skill.TryGetValue(skillId, out var coolSkill))
            {
                return false;
            }

            if (!coolSkill.Cool || coolSkill.RemainTime <= 0f)
            {
                return false;
            }

            remainTime = coolSkill.RemainTime;
            totalTime = coolSkill.Lasttime0 > 0f ? coolSkill.Lasttime0 : coolSkill.RemainTime;
            return totalTime > 0f;
        }

        /// <summary>
        /// 输出技能当前 CD 快照，供卡琳 CD 修正等问题排查。
        /// </summary>
        public void LogSkillCooldownSnapshot(int skillId, string reason)
        {
            if (!skill.TryGetValue(skillId, out var coolSkill))
            {
                LogTool.Log($"[GameLand][CD快照][{reason}] skillId={skillId} 不在 skill 字典");
                return;
            }

            LogTool.Log(
                $"[GameLand][CD快照][{reason}] skillId={skillId}, Cool={coolSkill.Cool}, " +
                $"RemainTime={coolSkill.RemainTime:F3}, Lasttime={coolSkill.Lasttime:F3}, " +
                $"Lasttime0={coolSkill.Lasttime0:F3}, tweenActive={coolSkill.coolTween != null && coolSkill.coolTween.IsActive()}");
        }

        /**
         * 释放技能（回调）
         */
        public void ReleaseSkillCallBack(int skillId, float cdTime)
        {
            ExitPendingRelease(skillId);
            CheckChargingNumCoolingSkill(skillId, cdTime);

            CheckSkillOperateDuration(skillId, cdTime);
            
            // 通知新的 BattleSkillsPanelNew 开始冷却
            Global.Publisher.Publish(MessagePublisher.SKILL_COOLING_STARTED, new SkillCoolingData(skillId, cdTime));
        }

        public void ReleaseSkillCoolingShow(int skillId)
        {
            if (!_skillCardHandlers.TryGetValue(skillId, out var handler)) return;
            handler.StartCooldown(hasCharging: false);
        }

        /// <summary>
        /// 点击确认释放时立即进入技能卡冷却（早于动画命中帧）。
        /// </summary>
        public bool TryBeginSkillCooldownOnClick(int skillId, SoldierCard soldierCard)
        {
            if (soldierCard == null) return false;

            var skill = soldierCard.GetSkillById(skillId);
            if (skill is not FusionContinent.Skill.SkillBase skillBase)
            {
                LogTool.LogWarning($"[SkillRelease] 点击置CD失败: skillId={skillId}, 未找到 SkillBase");
                return false;
            }

            if (skillBase.IsNormalAttack()) return true;

            skillBase.BeginCooldownFromClick();
            return true;
        }

        /// <summary>
        /// 释放失败时回滚刚由点击触发的技能卡冷却。
        /// </summary>
        public void RollbackSkillCooldownOnReleaseFailed(int skillId)
        {
            if (!skill.TryGetValue(skillId, out var coolSkill)) return;

            if (coolSkill.coolTween != null && coolSkill.coolTween.IsActive())
            {
                coolSkill.coolTween.Kill();
            }

            coolSkill.Cool = false;
            coolSkill.RemainTime = 0;
            coolSkill.Lasttime = 0;

            if (_skillCardHandlers.TryGetValue(skillId, out var handler))
            {
                handler.UpdateCooldownUI(0f, 0f);
                handler.OnCoolingComplete_External(Global.AutoReleaseSkillState.Value);
            }

            Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_CARD_COOLING, skillId);
            LogTool.Log($"[SkillRelease] 释放失败，已回滚技能卡CD: skillId={skillId}");
        }

        /// <summary>
        /// 点击/拖拽确认释放：立即进入技能卡 CD，并触发英灵施法。
        /// </summary>
        public bool TryReleaseSkillOnClick(int skillId, SoldierCard soldierCard, Vector3 targetPosition = default,
            bool useNearestEnemy = false)
        {
            if (soldierCard == null) return false;

            if (!IsSkillReadyForRelease(skillId, out var readyReason))
            {
                LogTool.LogWarning($"[SkillRelease] 点击释放被拦截: skillId={skillId}, reason={readyReason}");
                return false;
            }

            if (!TryEnterPendingRelease(skillId)) return false;

            // 点击索敌（useNearestEnemy）时显示攻击范围，提示技能检测半径
            if (useNearestEnemy)
            {
                soldierCard.FlashAttackRangeHint();
            }

            if (!TryBeginSkillCooldownOnClick(skillId, soldierCard))
            {
                ExitPendingRelease(skillId);
                return false;
            }

            var request = BattleCastRequestFactory.FromActiveSkillCard(
                soldierCard, skillId, useNearestEnemy, targetPosition);
            request.SetOnComplete(success =>
            {
                if (!success)
                {
                    RollbackSkillCooldownOnReleaseFailed(skillId);
                }
            });

            if (!BattleScheduler.Instance.RequestCastSkill(request))
            {
                bool success = targetPosition != default
                    ? soldierCard.StartReleaseSkill(skillId, targetPosition)
                    : soldierCard.StartReleaseSkill(skillId, useNearestEnemy: useNearestEnemy);
                if (!success)
                {
                    ExitPendingRelease(skillId);
                    RollbackSkillCooldownOnReleaseFailed(skillId);
                }

                return success;
            }

            return true;
        }

        /// <summary>
        /// 调度出队后执行：进入技能卡 CD 并触发英灵施法（自动模式批量释放）。
        /// </summary>
        public bool TryExecuteDeferredSkillCardRelease(
            int skillId,
            SoldierCard soldierCard,
            bool useNearestEnemy,
            Vector3 targetPosition = default)
        {
            if (!TryEnterPendingRelease(skillId))
            {
                return false;
            }

            if (!TryBeginSkillCooldownOnClick(skillId, soldierCard))
            {
                ExitPendingRelease(skillId);
                return false;
            }

            var indicator = soldierCard.transform.Find("indicator");
            if (indicator != null)
            {
                indicator.localScale = Vector3.zero;
            }

            if (_skillCardObjs.TryGetValue(skillId, out var cardObj))
            {
                PlaySkillCardClickScaleTween(cardObj);
            }

            bool success = targetPosition != default
                ? soldierCard.StartReleaseSkill(skillId, targetPosition)
                : soldierCard.StartReleaseSkill(skillId, useNearestEnemy: useNearestEnemy);

            if (!success)
            {
                RollbackSkillCooldownOnReleaseFailed(skillId);
            }

            return success;
        }

        private bool TryEnqueueAutoSkillRelease(int cardId, SoldierCard soldierCard)
        {
            var request = BattleCastRequestFactory.FromActiveSkillCard(
                soldierCard,
                cardId,
                useNearestEnemy: false,
                deferSkillCardCooldown: true);

            if (BattleScheduler.Instance.RequestCastSkill(request))
            {
                return true;
            }

            return TryExecuteDeferredSkillCardRelease(cardId, soldierCard, false);
        }

        private void RequestSoldierCastSkill(
            SoldierCard soldierCard,
            int cardId,
            bool useNearestEnemy,
            Vector3 targetPosition,
            System.Action<bool> onComplete)
        {
            var request = BattleCastRequestFactory.FromActiveSkillCard(
                soldierCard, cardId, useNearestEnemy, targetPosition);
            if (onComplete != null)
            {
                request.SetOnComplete(onComplete);
            }

            if (!BattleScheduler.Instance.RequestCastSkill(request) && onComplete != null)
            {
                bool success = targetPosition != default
                    ? soldierCard.StartReleaseSkill(cardId, targetPosition)
                    : soldierCard.StartReleaseSkill(cardId, useNearestEnemy: useNearestEnemy);
                onComplete(success);
            }
        }

        /**
         * 设置技能拖拽状态
         */
        public void SetSkillDragState(int skillId, DragState dragState)
        {
            if (!_skillCardHandlers.TryGetValue(skillId, out var handler))
            {
                return;
            }

            handler.SetDragState(dragState);
        }


        /// <summary>
        /// 供新 SkillItem / cardScroll UI 转发点击，走原有技能卡释放逻辑。
        /// 自动战斗中点击未在 CD 的技能卡会取消自动战斗并手动释放。
        /// </summary>
        public void ClickSkillCard(int skillId, GameObject sourceCardObj = null)
        {
            if (!TryHandleManualSkillClickDuringAuto(skillId))
            {
                return;
            }

            if (TryReleaseSkillCardByPointer(skillId, out var blockedReason, skipDragStateCheck: true,
                    sourceCardObj: sourceCardObj))
            {
                LogTool.Log($"[SkillClick] 新UI点击释放成功: skillId={skillId}");
                Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS, GuideType.SkillGuide);
                return;
            }

            LogTool.LogWarning($"[SkillClick] 新UI点击被拦截: skillId={skillId}, reason={blockedReason}");
        }

        /// <summary>
        /// 处理“充能次数”与冷却的衔接：
        /// 先消费充能，再决定是否进入正常冷却。
        /// </summary>
        private void CheckChargingNumCoolingSkill(int skillId, float cdTime)
        {
            if (skill.ContainsKey(skillId) && skill[skillId].ChargingNum > 0)
            {
                skill[skillId].ChargingNum--;

                for (int i = 0; i < Global.SoldierSkillTeams.Count; i++)
                {
                    var card = Global.SoldierSkillTeams[i];
                    if (card.CardId != skillId) continue;

                    if (_skillCardHandlers.TryGetValue(skillId, out var handler))
                    {
                        handler.UpdateChargingUI(skill[skillId].ChargingNum);
                        handler.SetDragState(
                            skill[skillId].ChargingNum > 0 ? DragState.GamePlay : DragState.DragFalse);
                    }

                    switch (skill[skillId].ChargingType)
                    {
                        case ChargingStatusType.EffectiveInstantly:
                            if (skill[skillId].ChargingNum > 0) return;
                            break;
                    }
                }
            }

            CoolingSkillCard(skillId, cdTime);
        }

        /// <summary>
        /// 处理技能“可操作持续时间”倒计时；结束后若仍未释放完成则进入冷却。
        /// </summary>
        private void CheckSkillOperateDuration(int skillId, float cdTime)
        {
            if (skill.ContainsKey(skillId) && skill[skillId].OperateDuration > 0 &&
                skill[skillId].OperateDurationTween == null)
            {
                for (int i = 0; i < Global.SoldierSkillTeams.Count; i++)
                {
                    var card = Global.SoldierSkillTeams[i];
                    if (card.CardId != skillId) continue;

                    if (!_skillCardHandlers.TryGetValue(skillId, out var handler)) continue;

                    skill[skillId].OperateDurationTween = DOVirtual.Float(skill[skillId].OperateDuration, 0,
                        skill[skillId].OperateDuration, (a) =>
                        {
                            if (a <= 0)
                            {
                                skill[skillId].OperateDurationTween.Kill();
                                skill[skillId].OperateDurationTween = null;
                                skill[skillId].ChargingNum = 0;
                                CoolingSkillCard(skillId, cdTime);
                            }

                            handler.UpdateChargingUI_WithDuration(skill[skillId].ChargingNum, (int)a);
                        }).SetEase(Ease.Linear);
                }
            }
        }

        private int ChargingEnergy = 0;
        public Dictionary<int, CoolSkill> skill = new Dictionary<int, CoolSkill>();

        /**
         * 重置技能操作时长 并 增加充能次数
         */
        public void ResetSkillOperateDuration(int skillId, int addChargingNum = 0)
        {
            var specialCardCfg = _specialEffectsConfig.GetOrDefault(skillId);
            if (specialCardCfg == null || !skill.ContainsKey(skillId)) return;

            skill[skillId].OperateDuration = specialCardCfg.OperateDuration;
            if (skill[skillId].OperateDuration > 0)
            {
                if (skill[skillId].OperateDurationTween != null)
                {
                    skill[skillId].OperateDurationTween.Kill();
                    skill[skillId].OperateDurationTween = null;
                }

                CheckSkillOperateDuration(skillId, skill[skillId].Lasttime0);
            }

            if (addChargingNum > 0)
            {
                skill[skillId].ChargingNum += addChargingNum;
            }
        }

// 在 GameLand 类中添加辅助方法
        public bool IsSkillCooling(int skillId)
        {
            return skill.ContainsKey(skillId) && skill[skillId].Cool;
        }
        
        /**
         * 动态增加当前冷却剩余时间
         *
         * @return
         * @author Zpp
         * @create 2025/11/25
         **/
        public void AddSkillCurrentCooldownTime(int skillId, float additionalTime)
        {
            if (skill.ContainsKey(skillId) && skill[skillId].Cool)
            {
                float remain = skill[skillId].RemainTime;
                float newRemain = Mathf.Max(0, remain + additionalTime);
                skill[skillId].RemainTime = newRemain;

                float progress = 1f - (newRemain / skill[skillId].Lasttime0);
                skill[skillId].coolTween.Goto(progress * skill[skillId].Lasttime0, andPlay: true);
            }
        }

        /// <summary>
        /// 缩短已进入冷却状态的技能冷却时间
        /// </summary>
        /// <param name="skillId">技能ID</param>
        /// <param name="reduceSeconds">要缩短的秒数</param>
        /// <returns>是否成功缩短</returns>
        public bool ReduceSkillCooldown(int skillId, float reduceSeconds)
        {
            if (!skill.TryGetValue(skillId, out var coolSkill))
            {
                LogTool.LogWarning($"[GameLand] ReduceSkillCooldown 失败: skillId={skillId} 不在冷却字典中");
                return false;
            }

            if (!coolSkill.Cool)
            {
                LogTool.LogWarning(
                    $"[GameLand] ReduceSkillCooldown 失败: skillId={skillId} Cool=false, remain={coolSkill.RemainTime:F2}");
                return false;
            }

            var beforeRemainTime = coolSkill.RemainTime;
            var newRemainTime = Mathf.Max(0, coolSkill.RemainTime + reduceSeconds);

            if (newRemainTime <= 0)
            {
                // 直接完成冷却
                if (coolSkill.coolTween != null && coolSkill.coolTween.IsActive())
                {
                    coolSkill.coolTween.Kill();
                }
                coolSkill.RemainTime = 0;
                coolSkill.Cool = false;
                coolSkill.Lasttime = 0;
                if (_skillCardHandlers.TryGetValue(skillId, out var handler))
                {
                    handler.UpdateCooldownUI(0, coolSkill.Lasttime0);
                    // 调用冷却完成回调，恢复拖拽状态
                    handler.OnCoolingComplete_External(Global.AutoReleaseSkillState.Value);
                }
                Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_CARD_COOLING, skillId);
                LogTool.Log(
                    $"[GameLand] ReduceSkillCooldown 完成冷却: skillId={skillId}, delta={reduceSeconds:F2}, " +
                    $"before={beforeRemainTime:F2}");
                return true;
            }

            // 清除原有 tween，重新启动动画
            if (coolSkill.coolTween != null && coolSkill.coolTween.IsActive())
            {
                coolSkill.coolTween.Kill();
            }

            coolSkill.RemainTime = newRemainTime;
            coolSkill.Lasttime = newRemainTime;
            var originalCdTime = coolSkill.Lasttime0;
            var reducedCdTime = newRemainTime;

            _skillCardHandlers.TryGetValue(skillId, out var h);
            coolSkill.coolTween = DOTween.To(
                () => coolSkill.RemainTime,
                v =>
                {
                    coolSkill.RemainTime = v;
                    coolSkill.Cool = v > 0;
                    coolSkill.Lasttime = v;
                    if (h != null)
                    {
                        h.UpdateCooldownUI(v, originalCdTime);
                    }
                },
                0,
                reducedCdTime
            ).OnComplete(() =>
            {
                coolSkill.Cool = false;
                coolSkill.Lasttime = 0;
                coolSkill.RemainTime = 0f;
                if (h != null)
                {
                    // 调用冷却完成回调，恢复拖拽状态
                    h.OnCoolingComplete_External(Global.AutoReleaseSkillState.Value);
                }
                Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_CARD_COOLING, skillId);
            });

            NotifySkillCooldownAdjusted(skillId, newRemainTime);
            LogSkillCooldownSnapshot(skillId, "ReduceSkillCooldown-After");
            LogTool.Log(
                $"[GameLand] ReduceSkillCooldown 成功: skillId={skillId}, delta={reduceSeconds:F2}, " +
                $"before={beforeRemainTime:F2}, after={newRemainTime:F2}, total={originalCdTime:F2}");
            return true;
        }

        /// <summary>
        /// 冷却剩余时间被动态修正后，同步技能栏 UI（BattleSkillsPanelNew）。
        /// </summary>
        private void NotifySkillCooldownAdjusted(int skillId, float remainTime)
        {
            Global.Publisher.Publish(
                MessagePublisher.SKILL_COOLING_STARTED,
                new SkillCoolingData(skillId, remainTime));
        }

        /// <summary>
        /// 启动技能冷却主流程：更新冷却UI、限制拖拽、冷却完成后处理充能与状态恢复。
        /// </summary>
        public void CoolingSkillCard(int skillId, float cdTime)
        {
            var specialCardCfg = _specialEffectsConfig.GetOrDefault(skillId);
            if (specialCardCfg?.PassiveSkill == 1) return;

            if (!skill.ContainsKey(skillId))
            {
                skill.Add(skillId, new CoolSkill());
            }

            if (skill[skillId].coolTween != null && skill[skillId].coolTween.IsActive())
            {
                skill[skillId].coolTween.Kill();
            }

            if (!skill[skillId].Cool)
            {
                skill[skillId].Lasttime0 = cdTime;
            }

            skill[skillId].RemainTime = cdTime;
            skill[skillId].Lasttime = cdTime;
            skill[skillId].Cool = cdTime > 0f;

            _skillCardHandlers.TryGetValue(skillId, out var handler);
            var card = handler?.Card ?? _skillCards.FirstOrDefault(c => c.CardId == skillId);
            if (card == null)
            {
                LogTool.LogWarning(
                    $"[GameLand] CoolingSkillCard: 未找到技能卡 UI skillId={skillId}, cd={cdTime:F2}, 已写入 skill 字典");
                LogSkillCooldownSnapshot(skillId, "CoolingSkillCard-NoUiCard");
                return;
            }

            // 有充能次数时可以拖拽，否则禁止
            var hasCharging = skill[skillId].ChargingNum > 0;
            handler?.StartCooldown(hasCharging);

            var cooldownTarget = handler != null ? handler.CardInfoObj : null;
            if (cooldownTarget == null)
            {
                cooldownTarget = gameObject;
            }

            // 开始冷却动画
            skill[skillId].coolTween = DOTween.To(
                () => skill[skillId].RemainTime,
                v =>
                {
                    skill[skillId].RemainTime = v;
                    skill[skillId].Cool = v > 0;
                    skill[skillId].Lasttime = v;

                    if (handler != null && handler.CardInfoObj != null)
                    {
                        handler.UpdateCooldownUI(v, skill[skillId].Lasttime0);

                        // 如果没有充能次数，强制禁止拖拽并重置位置
                        if (skill[skillId].ChargingNum <= 0)
                        {
                            handler.SetDragState(DragState.DragFalse);
                            handler.ForceResetPosition();
                        }
                    }

                    if (skill[skillId].OperateDurationTween != null)
                    {
                        skill[skillId].OperateDurationTween.Kill();
                        skill[skillId].OperateDurationTween = null;
                    }
                },
                0,
                cdTime
            ).OnComplete(() =>
            {
                skill[skillId].Cool = false;
                skill[skillId].Lasttime = 0;
                skill[skillId].RemainTime = 0f;
                Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_CARD_COOLING, skillId);

                if (handler != null && handler.CardInfoObj != null)
                {
                    // 冷却完成特效
                    GameObject cardedownffect = GamePlayManager.Default._resLoader
                        .Get<GameObject>("SkillCardUse02").Instantiate();
                    cardedownffect.transform.SetParent(this.transform);
                    cardedownffect.transform.localScale = Vector3.one * 150;
                    cardedownffect.transform.position = handler.CardInfoObj.transform.position;
                    DOTween.To(v => { }, 0, 0, 1.5f).onComplete += () => { Destroy(cardedownffect); };

                    skill[skillId].OperateDuration = specialCardCfg.OperateDuration;
                    handler.OnCoolingComplete_External(Global.AutoReleaseSkillState.Value);
                }

                // 充能逻辑
                var specialCardLvCfg = specialCardCfg.LevelPropertyList[card.CardLevel - 1];
                if (specialCardLvCfg.ChargingStatus.Count > 0)
                {
                    var chargingStatus = specialCardLvCfg.ChargingStatus.First();
                    skill[skillId].ChargingType = chargingStatus.Type;

                    switch (chargingStatus.Type)
                    {
                        case ChargingStatusType.CdTime:
                            if (skill[skillId].ChargingNum < chargingStatus.MaxNum)
                            {
                                skill[skillId].ChargingNum++;
                                if (skill[skillId].ChargingNum < chargingStatus.MaxNum)
                                {
                                    CoolingSkillCard(skillId, cdTime);
                                }
                                handler?.UpdateChargingUI(skill[skillId].ChargingNum);
                            }
                            break;

                        case ChargingStatusType.EffectiveInstantly:
                            skill[skillId].ChargingNum = chargingStatus.MaxNum;
                            handler?.UpdateChargingUI(skill[skillId].ChargingNum);
                            break;
                    }
                }
            }).SetEase(Ease.Linear).SetTarget(cooldownTarget).SetId(GlobalConst.BattleTween);

            // 通知 cardScroll / BattleSkillsPanel 等 UI 同步倒计时
            Global.Publisher.Publish(MessagePublisher.SKILL_COOLING_STARTED, new SkillCoolingData(skillId, cdTime));
        }

        /// <summary>
        /// 停止所有技能冷却 DOTween（局内重开 / 场景销毁时调用）
        /// </summary>
        public void StopAllSkillCooldownTweens()
        {
            foreach (var coolSkill in skill.Values)
            {
                if (coolSkill.coolTween != null && coolSkill.coolTween.IsActive())
                {
                    coolSkill.coolTween.Kill();
                    coolSkill.coolTween = null;
                }

                if (coolSkill.OperateDurationTween != null && coolSkill.OperateDurationTween.IsActive())
                {
                    coolSkill.OperateDurationTween.Kill();
                    coolSkill.OperateDurationTween = null;
                }

                coolSkill.Cool = false;
                coolSkill.Lasttime = 0;
                coolSkill.RemainTime = 0f;
            }
        }

        /**
         * 自动释放技能按钮
         */
        private void SetAutoToggle()
        {
            // RegisterWithInitValue：若开局时 Global 已是 true，也能立刻套用自动模式
            Global.AutoReleaseSkillState.RegisterWithInitValue((isOn) =>
            {
                LogTool.Log($"[SkillAuto] 自动释放切换: isOn={isOn}");
                foreach (var card in _skillCards)
                {
                    if (card.CardId == 0 || card.CardType != CardType.Skill) continue;
                    if (!_skillCardHandlers.TryGetValue(card.CardId, out var handler)) continue;
                    handler.SetAutoMode(isOn);
                    if (!isOn && IsSkillReadyForRelease(card.CardId))
                    {
                        handler.SetDragState(DragState.GamePlay);
                    }
                }

                if (isOn)
                {
                    TryAutoReleaseSkillsOnce();
                }
            });
        }

        /// <summary>
        /// 切换到自动模式后立即尝试释放一次，避免等待协程 sleep。
        /// </summary>
        private void TryAutoReleaseSkillsOnce()
        {
            if (!Global.AutoReleaseSkillState.Value || GamePlayManager.Default == null ||
                GamePlayManager.Default.IsPaused())
            {
                return;
            }

            RefreshSkillCardHeroVisibility();

            foreach (var card in _skillCards)
            {
                if (card.CardId == 0 || !card.IsUnlock.Value || card.CardType != CardType.Skill)
                {
                    continue;
                }

                var cardId = card.CardId;
                if (!ShouldAutoReleaseSkill(cardId))
                {
                    continue;
                }

                var specialCardCfg = _specialEffectsConfig.GetOrDefault(cardId);
                if (specialCardCfg == null || specialCardCfg.PassiveSkill != 0 || !IsSkillReadyForRelease(cardId))
                {
                    continue;
                }

                foreach (var v in GamePlayManager.Default.villageGameCards)
                {
                    if (v.IsDie() || !EnemyTargetFinder.IsValidEnemy(v.currentTarget))
                    {
                        continue;
                    }

                    if (specialCardCfg.ActsOnCard != 0 && v.cardId != specialCardCfg.ActsOnCard)
                    {
                        continue;
                    }

                    if (TryEnqueueAutoSkillRelease(cardId, v))
                    {
                        break;
                    }
                }
            }
        }

        
        /**
         * 锁定技能释放
         *
         * @return
         * @author Zpp
         * @create 2025/10/31
         **/
        public void LockSkillRelease() {
            foreach (var card in _skillCards)
            {
                card.IsUnlock.Value = false;
            }

            Global.AutoReleaseSkillState.Value = false;
            // Autotoggle.isOn = Global.AutoReleaseSkillState;
        }
        
        /**
         * 锁定技能释放（指定英雄）
         *
         * @return
         * @author Zpp
         * @create 2025/10/31
         **/
        public void LockSkillRelease(int soldierId) {
            foreach (var card in _skillCards)
            {
                var specialCardCfg = _specialEffectsConfig.GetOrDefault(card.CardId);
                if (specialCardCfg.ActsOnCard == soldierId) 
                {
                    card.IsUnlock.Value = false;
                }
            }
        }

        // 自动释放技能
        IEnumerator AutomaticallyReleaseSkills()
        {
            while (true)
            {
                RefreshSkillCardHeroVisibility();
                if (!GamePlayManager.Default.IsPaused())
                {
                    foreach (var card in _skillCards)
                    {
                        if (card.CardId == 0 || !card.IsUnlock.Value || card.CardType != CardType.Skill) continue;

                        var cardId = card.CardId;
                        if (!ShouldAutoReleaseSkill(cardId)) continue;

                        var specialCardCfg = _specialEffectsConfig.GetOrDefault(cardId);
                        // 自动释放技能
                        if (specialCardCfg.PassiveSkill == 0 && IsSkillReadyForRelease(cardId))
                        {
                            // 遍历场上相同的角色
                            foreach (var v in GamePlayManager.Default.villageGameCards)
                            {
                                // 判断是否有存活目标
                                if (v.IsDie() || !EnemyTargetFinder.IsValidEnemy(v.currentTarget)) continue;
                                
                                if (specialCardCfg.ActsOnCard == 0 || v.cardId == specialCardCfg.ActsOnCard)
                                {
                                    if (TryEnqueueAutoSkillRelease(cardId, v))
                                    {
                                        break;
                                    }
                                }
                            }

                            yield return new WaitForSeconds(automaticallyReleaseSkillSleepTime); // 等待1秒
                        }
                    }
                }

                yield return new WaitForSeconds(automaticallyReleaseSkillSleepTime); // 等待1秒
            }
            // ReSharper disable once IteratorNeverReturns
        }

        /// <summary>
        /// 全局自动开启时所有角色技能自动释放；未开启时全部手动。
        /// </summary>
        private bool ShouldAutoReleaseSkill(int skillId)
        {
            return Global.AutoReleaseSkillState.Value;
        }

        /// <summary>
        /// 自动战斗中手动点击技能卡：
        /// - 技能在 CD：忽略，保持自动战斗
        /// - 技能未在 CD：取消自动战斗，继续走手动释放
        /// </summary>
        private bool TryHandleManualSkillClickDuringAuto(int skillId)
        {
            if (!Global.AutoReleaseSkillState.Value)
            {
                return true;
            }

            if (!IsSkillReadyForRelease(skillId, out var reason))
            {
                LogTool.Log($"[SkillClick] 自动模式中点击冷却中技能，保持自动: skillId={skillId}, reason={reason}");
                return false;
            }

            CancelAutoReleaseSkillDueToManualClick();
            return true;
        }

        private void CancelAutoReleaseSkillDueToManualClick()
        {
            if (!Global.AutoReleaseSkillState.Value)
            {
                return;
            }

            var skillService = BattleServiceManager.Instance != null
                ? BattleServiceManager.Instance.GetService<BattleSkillService>()
                : null;
            if (skillService != null)
            {
                skillService.CancelAutoReleaseFromManualSkill();
                LogTool.Log("[SkillClick] 手动点击可用技能，已取消自动战斗");
                return;
            }

            Global.AutoReleaseSkillState.Value = false;
            PlayerPrefs.SetString("AutoReleaseSkillState", "false");
            LogTool.Log("[SkillClick] 手动点击可用技能，已取消自动战斗（无 BattleSkillService）");
        }

        /**
         * 开始释放角色技能
         *
         * @return
         * @author Zpp
         * @create 2026/1/14
         **/
        private void StartReleaseSkill(SoldierCard soldierCard, GameObject cardObj, int cardId, bool useNearestEnemy = false)
        {
            if (!TryEnterPendingRelease(cardId)) return;

            if (useNearestEnemy)
            {
                soldierCard.FlashAttackRangeHint();
            }

            if (!TryBeginSkillCooldownOnClick(cardId, soldierCard))
            {
                ExitPendingRelease(cardId);
                return;
            }

            void HandleReleaseResult(bool releaseSuccess)
            {
                if (!releaseSuccess)
                {
                    RollbackSkillCooldownOnReleaseFailed(cardId);
                }
            }

            var indicator = soldierCard.transform.Find("indicator");
            if (indicator != null)
            {
                indicator.localScale = Vector3.zero;
            }

            PlaySkillCardClickScaleTween(cardObj);

            // 只有本体卡牌释放特效；飞入动画可临时关闭
            if (soldierCard.RoleType == SoldierRoleType.Self && SkillCardClickFlyAnimationEnabled)
            {
                cardObj.GetComponent<CardSlotLongClick>().ClickEffect(soldierCard.transform.position, 0.2f,
                    null,
                    () => { RequestSoldierCastSkill(soldierCard, cardId, useNearestEnemy, default, HandleReleaseResult); });
            }
            else
            {
                RequestSoldierCastSkill(soldierCard, cardId, useNearestEnemy, default, HandleReleaseResult);
            }
        }

        /// <summary>
        /// 进入“预锁释放”状态：
        /// 在真正释放回调返回前，限制同一技能重复触发。
        /// </summary>
        private bool TryEnterPendingRelease(int skillId)
        {
            if (!IsSkillReadyForRelease(skillId, out var readyReason))
            {
                LogTool.LogWarning($"[SkillClick] 可释放判定失败: skillId={skillId}, reason={readyReason}");
                return false;
            }

            _pendingReleaseSkillCounts.TryGetValue(skillId, out var pendingCount);
            var maxPendingCount = GetMaxPendingReleaseCount(skillId);
            if (pendingCount >= maxPendingCount)
            {
                LogTool.LogWarning($"[SkillClick] 预锁拦截: skillId={skillId}, pending={pendingCount}, max={maxPendingCount}");
                return false;
            }

            _pendingReleaseSkillCounts[skillId] = pendingCount + 1;
            LogTool.Log($"[SkillClick] 进入预锁: skillId={skillId}, pending={pendingCount + 1}, max={maxPendingCount}");

            return true;
        }

        private int GetMaxPendingReleaseCount(int skillId)
        {
            // 点击预锁只用于防重复触发，不参与充能策略；
            // 技能是否可继续释放由冷却/充能本身决定。
            return 1;
        }

        /// <summary>
        /// 统一技能可释放判定：
        /// 1) 不在冷却可释放；
        /// 2) 即便在冷却，只要有充能次数也可释放。
        /// </summary>
        private bool IsSkillReadyForRelease(int skillId)
        {
            return IsSkillReadyForRelease(skillId, out _);
        }

        private bool IsSkillReadyForRelease(int skillId, out string reason)
        {
            reason = string.Empty;
            if (IsSkillCardSilenced(skillId))
            {
                reason = "技能处于沉默状态";
                return false;
            }

            if (!skill.TryGetValue(skillId, out var coolSkill))
            {
                // 技能卡尚未初始化冷却时，先初始化并禁止本次释放（避免新英雄刚出现就直接放技能）。
                if (!TryStartSkillCoolingImmediately(skillId))
                {
                    SetCoolingSkillCard(skillId);
                }
                reason = "冷却状态未初始化，已触发初始化并拦截本次";
                return false;
            }

            if (coolSkill.ChargingNum > 0)
            {
                return true;
            }

            if (coolSkill.Cool)
            {
                reason = $"技能冷却中: remain={coolSkill.RemainTime:F2}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 退出“预锁释放”状态（通常在释放回调或失败分支中调用）。
        /// </summary>
        private void ExitPendingRelease(int skillId)
        {
            if (!_pendingReleaseSkillCounts.TryGetValue(skillId, out var pendingCount))
            {
                return;
            }

            pendingCount--;
            if (pendingCount <= 0)
            {
                _pendingReleaseSkillCounts.Remove(skillId);
                LogTool.Log($"[SkillClick] 退出预锁: skillId={skillId}, pending=0");
            }
            else
            {
                _pendingReleaseSkillCounts[skillId] = pendingCount;
                LogTool.Log($"[SkillClick] 退出预锁: skillId={skillId}, pending={pendingCount}");
            }
        }

        /// <summary>
        /// 点击释放入口：基于点击到的具体技能卡做完整可释放校验。
        /// </summary>
        private bool TryReleaseSkillCardByPointer(int clickedSkillId, out string blockedReason,
            bool skipDragStateCheck = false, GameObject sourceCardObj = null)
        {
            blockedReason = string.Empty;

            // 只处理当前被点击的技能卡，不再二次选卡，避免与 UGUI 射线结果不一致。
            var skillCard = _skillCards.FirstOrDefault(c => c.CardId == clickedSkillId);
            if (skillCard == null)
            {
                blockedReason = "未找到技能卡数据";
                return false;
            }
            if (!skillCard.IsUnlock.Value)
            {
                blockedReason = "技能未解锁";
                return false;
            }

            var hasLegacyCardObj = _skillCardObjs.TryGetValue(clickedSkillId, out var targetCardObj) &&
                                   targetCardObj != null;
            if (!hasLegacyCardObj && !skipDragStateCheck)
            {
                blockedReason = "未找到技能卡UI对象";
                return false;
            }

            // 统一可释放判定（解锁/冷却/充能/拖拽状态等），不通过则直接拦截。
            if (!IsSkillCardReleasable(clickedSkillId, out blockedReason, skipDragStateCheck))
            {
                return false;
            }

            var specialCardCfg = _specialEffectsConfig.GetOrDefault(clickedSkillId);
            if (specialCardCfg == null)
            {
                blockedReason = "未找到技能特效配置";
                return false;
            }
            if (!IsSkillHeroAvailable(clickedSkillId))
            {
                blockedReason = "技能所属英雄不在场";
                return false;
            }

            var soldierCard = GamePlayManager.Default.GetVillageGameCard(specialCardCfg.ActsOnCard);
            if (soldierCard == null)
            {
                blockedReason = $"未找到英灵 ActsOnCard={specialCardCfg.ActsOnCard}";
                LogTool.LogWarning($"点击释放技能失败：未找到英灵 ActsOnCard={specialCardCfg.ActsOnCard}, skillId={clickedSkillId}");
                return false;
            }

            if (skipDragStateCheck)
            {
                StartReleaseSkillFromNewUi(soldierCard, clickedSkillId, sourceCardObj);
                return true;
            }

            if (!hasLegacyCardObj)
            {
                blockedReason = "未找到技能卡UI对象";
                return false;
            }

            var cardLongClick = targetCardObj.GetComponent<CardSlotLongClick>();
            if (cardLongClick == null)
            {
                blockedReason = "缺少CardSlotLongClick组件";
                return false;
            }
            if (cardLongClick.dragState != DragState.GamePlay)
            {
                blockedReason = $"拖拽状态非GamePlay: {cardLongClick.dragState}";
                return false;
            }

            StartReleaseSkill(soldierCard, targetCardObj, clickedSkillId, useNearestEnemy: true);
            return true;
        }

        /// <summary>
        /// cardScroll / SkillItem 等新 UI 点击释放，不依赖 CardSlotLongClick。
        /// </summary>
        private void StartReleaseSkillFromNewUi(SoldierCard soldierCard, int cardId, GameObject sourceCardObj)
        {
            if (!TryReleaseSkillOnClick(cardId, soldierCard, useNearestEnemy: true))
            {
                return;
            }

            if (sourceCardObj != null)
            {
                PlaySkillCardClickScaleTween(sourceCardObj);
            }

            if (soldierCard.RoleType == SoldierRoleType.Self)
            {
                var indicator = soldierCard.transform.Find("indicator");
                if (indicator != null)
                {
                    indicator.localScale = Vector3.zero;
                }
            }
        }

        /// <summary>
        /// 技能卡点击处理：
        /// 先过滤拖拽/长按后的补发点击，再执行释放流程。
        /// </summary>
        private void HandleSkillCardClick(GameObject cardInfo, int skillId, string skillName)
        {
            LogTool.Log($"[SkillClick] 点击技能卡: skillId={skillId}, cardName={skillName}");

            if (!TryHandleManualSkillClickDuringAuto(skillId))
            {
                return;
            }

            // 拖拽/长按结束后 Unity 仍可能补发一次 Click，这里优先消费抑制标记。
            var cardLongClick = cardInfo.GetComponent<CardSlotLongClick>();
            if (cardLongClick != null && cardLongClick.ConsumeClickSuppression())
            {
                LogTool.LogWarning($"[SkillClick] 点击被抑制: skillId={skillId}, reason=拖拽/长按后补发Click");
                return;
            }

            if (TryReleaseSkillCardByPointer(skillId, out var blockedReason, skipDragStateCheck: true))
            {
                LogTool.Log($"[SkillClick] 点击释放成功: skillId={skillId}");
                Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS, GuideType.SkillGuide);
                return;
            }

            LogTool.LogWarning($"[SkillClick] 点击被拦截: skillId={skillId}, reason={blockedReason}");
        }

        private bool IsSkillCardReleasable(int skillId)
        {
            return IsSkillCardReleasable(skillId, out _);
        }

        /// <summary>
        /// 技能卡点击可释放判定（解锁、拖拽状态、英雄在场、冷却/充能）。
        /// </summary>
        private bool IsSkillCardReleasable(int skillId, out string blockedReason, bool skipDragStateCheck = false)
        {
            blockedReason = string.Empty;
            var skillCard = _skillCards.FirstOrDefault(c => c.CardId == skillId);
            if (skillCard == null)
            {
                blockedReason = "技能卡数据不存在";
                return false;
            }
            if (!skillCard.IsUnlock.Value)
            {
                blockedReason = "技能未解锁";
                return false;
            }

            if (!skipDragStateCheck)
            {
                if (!_skillCardObjs.TryGetValue(skillId, out var cardObj) || cardObj == null)
                {
                    blockedReason = "技能卡对象为空";
                    return false;
                }

                var cardLongClick = cardObj.GetComponent<CardSlotLongClick>();
                if (cardLongClick == null)
                {
                    blockedReason = "缺少CardSlotLongClick组件";
                    return false;
                }

                if (cardLongClick.dragState != DragState.GamePlay)
                {
                    blockedReason = $"dragState={cardLongClick.dragState}";
                    return false;
                }
            }

            if (!IsSkillHeroAvailable(skillId))
            {
                blockedReason = "技能所属英雄不可用";
                return false;
            }

            // 若还未建立冷却状态，先初始化并拦截本次点击释放。
            if (!skill.ContainsKey(skillId))
            {
                if (!TryStartSkillCoolingImmediately(skillId))
                {
                    SetCoolingSkillCard(skillId);
                }
                blockedReason = "冷却状态未初始化，已触发初始化后拦截本次点击";
                return false;
            }

            // 冷却且无充能时不可点
            if (skill.TryGetValue(skillId, out var coolSkill) && coolSkill.Cool && coolSkill.ChargingNum <= 0)
            {
                blockedReason = $"技能冷却中: remain={coolSkill.RemainTime:F2}, charging={coolSkill.ChargingNum}";
                return false;
            }

            return true;
        }

        private Transform GetTargetSkillCardSlot()
        {
            var secondarySlot = GetSecondarySkillCardSlot();
            if (CardSlot == null && secondarySlot == null)
            {
                return transform;
            }

            if (CardSlot == null)
            {
                return secondarySlot;
            }

            var primaryCount = CardSlot.childCount;
            if (primaryCount < PrimarySkillCardSlotMaxCount || secondarySlot == null)
            {
                return CardSlot;
            }

            return secondarySlot;
        }

        private Transform GetSecondarySkillCardSlot()
        {
            if (CardSlot2 != null)
            {
                return CardSlot2;
            }

            if (CardSlot != null && CardSlot.parent != null)
            {
                var foundSlot = CardSlot.parent.Find("CardSlot2");
                if (foundSlot != null)
                {
                    CardSlot2 = foundSlot;
                    return CardSlot2;
                }
            }

            return null;
        }

        private void RefreshSkillCardLayoutForSlot(Transform slot)
        {
            if (slot == null) return;

            var fanLayout = slot.GetComponent<CardFanLayout>();
            if (fanLayout == null) return;

            var cards = new List<RectTransform>();
            foreach (var skillCard in _skillCards)
            {
                if (skillCard == null || skillCard.CardId == 0) continue;
                if (!_skillCardObjs.TryGetValue(skillCard.CardId, out var cardObj) || cardObj == null) continue;
                if (!cardObj.activeSelf || cardObj.transform.parent != slot) continue;

                var rect = cardObj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    cards.Add(rect);
                }
            }

            fanLayout.Refresh(cards);
        }

        private void RefreshSkillCardHorizontalLayoutFallback(Transform slot)
        {
            if (slot == null) return;
            var layoutGroup = slot.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null) return;

            if (!_hasCachedDefaultSkillCardLayoutSpacing)
            {
                _defaultSkillCardLayoutSpacing = layoutGroup.spacing;
                _hasCachedDefaultSkillCardLayoutSpacing = true;
            }

            var visibleCardCount = _skillCardObjs.Values.Count(cardObj =>
                cardObj != null && cardObj.activeSelf && cardObj.transform.parent == slot);
            layoutGroup.spacing = GetSkillCardLayoutSpacing(visibleCardCount);
            layoutGroup.reverseArrangement = true;

            var cardSlotRect = slot as RectTransform;
            if (cardSlotRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(cardSlotRect);
            }
        }

        private void RefreshSkillCardSiblingOrderForSlot(Transform slot)
        {
            if (slot == null) return;

            // 固定层级：技能1在最上，技能2在技能1下，依次类推。
            // Unity UI 中 sibling 越靠后越在上层，因此需要把前面的技能放到更靠后的 sibling。
            // 这里倒序 SetAsLastSibling，最终顺序会变为：... 技能3(下) -> 技能2 -> 技能1(上)。
            for (int i = _skillCards.Count - 1; i >= 0; i--)
            {
                var card = _skillCards[i];
                if (card == null || card.CardId == 0) continue;
                if (!_skillCardObjs.TryGetValue(card.CardId, out var cardObj) || cardObj == null) continue;
                if (!cardObj.activeInHierarchy || cardObj.transform.parent != slot) continue;

                cardObj.transform.SetAsLastSibling();
            }
        }

    }

    public class CoolSkill
    {
        public bool Cool = false;
        public float Lasttime = 0;
        public float Lasttime0 = 0;
        public Tween coolTween = null; // 放在类的成员变量中
        public ChargingStatusType ChargingType; // 充能类型
        public int ChargingNum = 0; // 充能次数
        public int OperateDuration = 0; // 操作持续时间
        public Tween OperateDurationTween = null; // 操作持续时间动画
        public float RemainTime = 0f; // 剩余时间
    }
}
