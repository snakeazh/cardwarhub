using Config;
using DG.Tweening;
using FusionContinent.Model;
using FusionContinent.UI.Components;
using Game.SchoolChooseCard;
using GameService = Game.Services.SchoolChooseCardService;
using System.Collections.Generic;
using System.Linq;
using FusionContinent.Guide;
using UIFramework;
using UIFramework.Services;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    /// <summary>
    /// 流派三选一界面（MVVM）。
    /// 复用旧 Panel 的 UI 引用，负责卡牌渲染、品质特效、Animator 选中/未选中动画。
    /// 详细说明见同目录 SchoolChooseCardPanelNew_Context.md
    /// </summary>
    [PrefabPath("SchoolChooseCardPanel")]
    public class SchoolChooseCardPanelNew : UIPanel<SchoolChooseCardViewModel>
    {
        public override PanelStackMode StackMode => PanelStackMode.Managed;

        private const string EffectName2 = "ChooseCardColor02";
        private const string EffectLegend = "ChooseCardColor01";
        private const string EffectEpic = "ChooseCardGold01";
        private const string EffectRarity = "ChooseCardSilver01";

        // ChoseCard Animator（ani_choosecard01）：bool 参数 + 对应 State 名
        private const string AnimNormal = "normal";
        private const string AnimNoselect = "noselect";
        private const string AnimYinDanji = "yin_danji";
        private const string AnimCaiDanji = "cai_danji";
        private const string AnimJinDanji = "jin_danji";
        private const string AnimStateNormal = "ani_xk_yin_moren";
        private const string AnimStateNoselect = "ani_xk_weixuanze";
        private const string AnimStateYinDanji = "ani_xk_yin_dianji";
        private const string AnimStateJinDanji = "ani_xk_jin_dianji";
        private const string AnimStateCaiDanji = "ani_xk_cai_dianji";

        /// <summary>Legend 翻转后延迟显示卡牌。</summary>
        private const float LegendEntranceDelay = 0.4f;

        /// <summary>选中/关闭 Animator 播放后，延迟再提交选择并关面板。</summary>
        private const float ChooseCloseDelay = 0.5f;

        private SchoolChooseCardPanel mOldPanel;
        private readonly Dictionary<int, UnityAction> _refreshBtnHandlers = new Dictionary<int, UnityAction>();
        private readonly Dictionary<int, UnityAction> _chooseBtnHandlers = new Dictionary<int, UnityAction>();
        private readonly List<Tween> _starTweens = new List<Tween>();
        private readonly List<Tween> _cardEffectTweens = new List<Tween>();
        private readonly Dictionary<Transform, Transform> _detachedFlipEffects = new Dictionary<Transform, Transform>();
        private readonly Dictionary<Transform, Tween> _legendEntranceTweens = new Dictionary<Transform, Tween>();

        private UnityAction _getAllBtnHandler;
        private System.Action<string, string> _titleChangedHandler;
        private System.Action<string, string> _countdownChangedHandler;
        private System.Action<int, int> _goldChangedHandler;
        private System.Action<List<SchoolChooseCardViewModel.CardDisplayData>, List<SchoolChooseCardViewModel.CardDisplayData>> _cardListChangedHandler;
        private System.Action<List<SchoolChooseCardViewModel.QualityWeightData>, List<SchoolChooseCardViewModel.QualityWeightData>> _qualityChangedHandler;
        private System.Action<int> _countdownAutoChooseHandler;
        private System.Action _chooseOverHandler;
        /// <summary>刷新单卡后需要播放入场动画的槽位，CardList 变更回调消费后重置为 -1。</summary>
        private int _pendingAnimateSlotIndex = -1;

        protected override void OnInit(IUIData uiData)
        {
            mOldPanel = GetComponent<SchoolChooseCardPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[SchoolChooseCardPanelNew] 缺少 SchoolChooseCardPanel 组件");
            }

            base.OnInit(uiData);
        }

        protected override void OnBindViewModel(SchoolChooseCardViewModel vm)
        {
            if (mOldPanel == null) return;

            BindPropertyEvents(vm);
            BindServiceEvents();
            BindGlobalButtons();
            RefreshHeaderUI(vm);
            RenderQualityWeights(vm);
        }

        protected override void OnUnbindViewModel(SchoolChooseCardViewModel vm)
        {
            UnbindPropertyEvents(vm);
            UnbindServiceEvents();
            UnbindGlobalButtons();
            ClearCardButtonHandlers();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (mOldPanel == null || VM == null) return;

            mOldPanel.enabled = false;
            GamePlayManager.Default?.PauseGame();
            RefreshAllUI(VM, true);
            GamePlayManager.Default.TriggerGuide(GuideType.FirstChooseCard);
        }

        protected override void OnShow()
        {
            GamePlayManager.Default?.TriggerGuide(GuideType.FirstChooseCard);
        }

        protected override void OnHide()
        {
            StopAllCardEffects();
        }

        protected override void OnClose()
        {
            StopAllCardEffects();
            if (mOldPanel != null)
            {
                mOldPanel.enabled = true;
            }
        }

        private void Update()
        {
            VM?.OnUpdate(Time.unscaledDeltaTime);
        }

        public void SetPause(bool pause)
        {
            if (VM != null)
            {
                VM.IsPaused.Value = pause;
            }
        }

        private void BindPropertyEvents(SchoolChooseCardViewModel vm)
        {
            _titleChangedHandler = (_, newVal) =>
            {
                if (mOldPanel.TitleText != null)
                {
                    mOldPanel.TitleText.text = newVal;
                }
            };
            vm.TitleText.OnValueChanged += _titleChangedHandler;

            _countdownChangedHandler = (_, newVal) =>
            {
                if (mOldPanel.CountdownText != null)
                {
                    mOldPanel.CountdownText.text = newVal;
                }
            };
            vm.CountdownText.OnValueChanged += _countdownChangedHandler;

            _goldChangedHandler = (_, newVal) =>
            {
                if (mOldPanel.numberText != null)
                {
                    mOldPanel.numberText.text = newVal.ToString();
                }
                RefreshGoldCostUI(vm);
            };
            vm.GoldCount.OnValueChanged += _goldChangedHandler;

            _cardListChangedHandler = (_, _) =>
            {
                RenderCardList(vm, false, _pendingAnimateSlotIndex);
                _pendingAnimateSlotIndex = -1;
                
            };
            vm.CardList.OnValueChanged += _cardListChangedHandler;

            _qualityChangedHandler = (_, _) => RenderQualityWeights(vm);
            vm.QualityWeights.OnValueChanged += _qualityChangedHandler;

            _countdownAutoChooseHandler = OnCountdownAutoChooseRequested;
            vm.OnCountdownAutoChooseRequested += _countdownAutoChooseHandler;
        }

        private void UnbindPropertyEvents(SchoolChooseCardViewModel vm)
        {
            if (_titleChangedHandler != null)
            {
                vm.TitleText.OnValueChanged -= _titleChangedHandler;
                _titleChangedHandler = null;
            }

            if (_countdownChangedHandler != null)
            {
                vm.CountdownText.OnValueChanged -= _countdownChangedHandler;
                _countdownChangedHandler = null;
            }

            if (_goldChangedHandler != null)
            {
                vm.GoldCount.OnValueChanged -= _goldChangedHandler;
                _goldChangedHandler = null;
            }

            if (_cardListChangedHandler != null)
            {
                vm.CardList.OnValueChanged -= _cardListChangedHandler;
                _cardListChangedHandler = null;
            }

            if (_qualityChangedHandler != null)
            {
                vm.QualityWeights.OnValueChanged -= _qualityChangedHandler;
                _qualityChangedHandler = null;
            }

            if (_countdownAutoChooseHandler != null)
            {
                vm.OnCountdownAutoChooseRequested -= _countdownAutoChooseHandler;
                _countdownAutoChooseHandler = null;
            }
        }

        private void BindServiceEvents()
        {
            var service = GameServiceManager.Get<GameService>();
            _chooseOverHandler = OnChooseOver;
            service.OnChooseOver += _chooseOverHandler;
        }

        private void UnbindServiceEvents()
        {
            var service = GameServiceManager.Get<GameService>();
            if (_chooseOverHandler != null)
            {
                service.OnChooseOver -= _chooseOverHandler;
                _chooseOverHandler = null;
            }
        }

        private void BindGlobalButtons()
        {
            if (mOldPanel.GetAllBtn == null) return;

            _getAllBtnHandler = OnClickGetAll;
            mOldPanel.GetAllBtn.onClick.AddListener(_getAllBtnHandler);
        }

        private void UnbindGlobalButtons()
        {
            if (_getAllBtnHandler != null && mOldPanel != null && mOldPanel.GetAllBtn != null)
            {
                mOldPanel.GetAllBtn.onClick.RemoveListener(_getAllBtnHandler);
                _getAllBtnHandler = null;
            }
        }

        private void OnClickGetAll()
        {
            if (VM == null) return;

            var service = GameServiceManager.Get<GameService>();
            var cards = VM.CardList.Value;
            if (cards == null || cards.Count == 0) return;

            mOldPanel.GetAllBtn.interactable = false;
            SetAllCardButtonsInteractable(false);
            SetAllRefreshAreaVisible(false);

            PlayGetAllCardAnimations(cards);

            SubmitChooseAndCloseAfterAnimator(() =>
            {
                foreach (var card in cards)
                {
                    service.ChooseCard(card.CardId, card.Schools, card.IsCategorySkill);
                }
            });
        }

        private void RefreshAllUI(SchoolChooseCardViewModel vm, bool animateAllCards)
        {
            RefreshHeaderUI(vm);
            RenderQualityWeights(vm);
            RenderCardList(vm, animateAllCards, -1);
        }

        private void RefreshHeaderUI(SchoolChooseCardViewModel vm)
        {
            if (mOldPanel.numberText != null)
            {
                mOldPanel.numberText.text = vm.GoldCount.Value.ToString();
            }

            if (mOldPanel.GetAllBtn != null)
            {
                mOldPanel.GetAllBtn.interactable = true;
            }
        }

        private void RenderQualityWeights(SchoolChooseCardViewModel vm)
        {
            if (mOldPanel.QualityList == null) return;

            var weights = vm.QualityWeights.Value;
            for (int i = 0; i < mOldPanel.QualityList.childCount; i++)
            {
                var qualityObj = mOldPanel.QualityList.GetChild(i);
                bool active = i < weights.Count;
                qualityObj.gameObject.SetActive(active);
                if (!active) continue;

                var weight = weights[i];
                var text = qualityObj.Find("Text")?.GetComponent<TMP_Text>();
                if (text != null)
                {
                    text.text = $"{weight.Weight}%";
                    text.color = weight.Color;
                }

                var circle = qualityObj.Find("Circle")?.GetComponent<Image>();
                if (circle != null)
                {
                    circle.color = weight.Color;
                }
            }
        }

        /// <param name="animateAllCards">打开面板时为 true，全部卡牌播放入场</param>
        /// <param name="animateSlotIndex">刷新单卡时传入槽位，仅该槽播放入场；-1 表示不按槽位</param>
        private void RenderCardList(SchoolChooseCardViewModel vm, bool animateAllCards, int animateSlotIndex)
        {
            if (mOldPanel.ChooseList == null) return;

            StopAllCardEffects();
            ResetAllCardEffectTransforms();
            ClearCardButtonHandlers();
            HideAllCardSlots();
            int cardGuide = 1;
            GameObject cardGuideObj = null;
            var cards = vm.CardList.Value;

            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var cardObj = mOldPanel.ChooseList.Find($"ChoseCard1{i + 1}");
                if (cardObj == null) continue;

                bool playAnimation = animateAllCards || animateSlotIndex == i;
                bool delayedShow = playAnimation && card.Quality == QualityType.Legend;
                if (delayedShow)
                {
                    PrepareCardForEntrance(cardObj);
                }

                cardObj.gameObject.SetActive(true);
            }

            if (animateAllCards)
            {
                EnsureChooseListLayoutReady();
            }

            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var cardObj = mOldPanel.ChooseList.Find($"ChoseCard1{i + 1}");
                if (cardObj == null) continue;
                if (cardGuide == i)
                {
                    cardGuideObj = cardObj.transform.Find("RefreshBtn").gameObject;
                }

                bool playAnimation = animateAllCards || animateSlotIndex == i;
                RenderSingleCard(cardObj, card, playAnimation);
                BindCardButtons(cardObj, card, i);
            }
            if (cardGuideObj != null)
            {
                // GuideManager.Instance.SetGuideNode(GuideType.RefreshTip,cardGuideObj);
            }
            RefreshGoldCostUI(vm);
           GamePlayManager.Default.TriggerGuide(GuideType.RefreshTip);
            SetAllRefreshAreaVisible(true);
        }

        private void HideAllCardSlots()
        {
            for (int i = 0; i < mOldPanel.ChooseList.childCount; i++)
            {
                var child = mOldPanel.ChooseList.GetChild(i);
                if (child.name.StartsWith("ChoseCard"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>打开面板时 GridLayout 尚未排布，先激活卡牌再强制布局，避免 Legend 翻转光效落在错误位置。</summary>
        private void EnsureChooseListLayoutReady()
        {
            var chooseListRect = mOldPanel.ChooseList as RectTransform;
            if (chooseListRect == null) return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(chooseListRect);
        }

        private static void PlayEffectParticlesUnscaled(Transform effectRoot)
        {
            if (effectRoot == null) return;

            var particleSystems = effectRoot.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                var main = particleSystems[i].main;
                main.useUnscaledTime = true;
            }

            var rootParticle = effectRoot.GetComponent<ParticleSystem>();
            if (rootParticle != null)
            {
                rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                rootParticle.Clear(true);
                rootParticle.Play(true);
                return;
            }

            for (int i = 0; i < particleSystems.Length; i++)
            {
                var ps = particleSystems[i];
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear(false);
                ps.Play(false);
            }
        }

        /// <summary>
        /// 渲染单张卡牌。Legend 播放翻牌和品质特效，非 Legend 只播放 scaleX 入场。
        /// </summary>
        private void RenderSingleCard(Transform root, SchoolChooseCardViewModel.CardDisplayData data, bool playAnimation)
        {
            bool delayedShow = playAnimation && data.Quality == QualityType.Legend;

            if (!delayedShow)
            {
                SetLegendAnimatorEnabled(root, true);
            }

            PopulateCardVisuals(root, data);

            if (delayedShow)
            {
                EffectCard(data.Quality, root, playAnimation, delayedShow, data);
                HideCardVisualsForLegendEntrance(root);
            }
            else
            {
                ResetCardAnimator(root);
                PopulateCardVisuals(root, data);
                RestoreCardCanvasGroup(root);
                EffectCard(data.Quality, root, playAnimation, delayedShow, data);
            }
        }

        private void PopulateCardVisuals(Transform root, SchoolChooseCardViewModel.CardDisplayData data)
        {
            SetNodeActive(root, "Title", true);
            SetNodeActive(root, "Category", false);
            SetNodeActive(root, "Card", true);
            SetNodeActive(root, "Bg", true);
            SetNodeActive(root, "Description", true);
            SetNodeActive(root, "StarList", true);
            SetNodeActive(root, "Mask", true);
            SetNodeActive(root, "SuperTip", false);

            var title = root.Find("Title")?.GetComponent<TMP_Text>();
            if (title != null) title.text = data.Name;

            var category = root.Find("Category")?.GetComponent<TMP_Text>();
            if (category != null)
            {
                category.text = data.CategoryName;
                category.gameObject.SetActive(false);
            }

            var icon = root.Find("Card/Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = Global.SchoolChooseCardIconSpriteAtlas.GetSpriteExt(data.Icon);
                icon.SetNativeSize();
                icon.transform.localScale = Vector3.one;
            }

            var bg = root.Find("Bg")?.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = Global.SchoolChooseCardIconSpriteAtlas.GetSpriteExt($"Check{data.Quality}");
            }

            var mask = root.Find("Mask")?.GetComponent<Image>();
            if (mask != null)
            {
                mask.DOFade(0f, 0.2f).SetUpdate(true);
            }

            var desc = root.Find("Description")?.GetComponent<TMP_Text>();
            if (desc != null) desc.text = data.Description;

            var stars = root.Find("StarList");
            if (stars != null)
            {
                SetCardStars(stars, data.Star, data.Quality, !data.IsCategorySkill);
            }

            RenderBelongSchools(root, data);
            RenderSkillStar(root, data);
            RenderRecommendIcon(root, data);
        }

        private void RenderRecommendIcon(Transform root, SchoolChooseCardViewModel.CardDisplayData data)
        {
            var recommendIcon = root.Find("recommendIcon");
            if (recommendIcon == null) return;

            recommendIcon.gameObject.SetActive(data.IsRecommended);
        }

        private void RenderBelongSchools(Transform root, SchoolChooseCardViewModel.CardDisplayData data)
        {
            var belongRoot = root.Find("BelongSchool");
            if (belongRoot == null) return;

            bool active = !data.IsCategorySkill && data.SchoolIcons.Count > 0;
            belongRoot.gameObject.SetActive(active);
            if (!active) return;

            for (int i = 0; i < belongRoot.childCount; i++)
            {
                var cell = belongRoot.GetChild(i);
                bool show = i < data.SchoolIcons.Count;
                cell.gameObject.SetActive(show);
                if (!show) continue;

                var image = cell.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = Global.SchoolChooseCardIconSpriteAtlas.GetSpriteExt(data.SchoolIcons[i]);
                }
            }
        }

        private void RenderSkillStar(Transform root, SchoolChooseCardViewModel.CardDisplayData data)
        {
            var skillStar = root.Find("SkillStar");
            if (skillStar == null) return;

            skillStar.gameObject.SetActive(data.IsCategorySkill);
            if (!data.IsCategorySkill || data.Schools.Count == 0) return;

            var schoolCfg = DataConfigManager.Tables.TbSchoolConfig.GetOrDefault(data.Schools[0]);
            if (schoolCfg == null || schoolCfg.SchoolType != SchoolType.Skill) return;

            foreach (var lvCfg in schoolCfg.CardCountList)
            {
                if (lvCfg.Quality != QualityType.Legend) continue;

                var desc = skillStar.Find("SuperStar/Desc")?.GetComponent<TMP_Text>();
                var title = skillStar.Find("SuperStar/Title")?.GetComponent<TMP_Text>();
                var num = skillStar.Find("SuperStar/Star/Num")?.GetComponent<TMP_Text>();
                if (desc != null) desc.text = lvCfg.Desc;
                if (title != null) title.text = lvCfg.Title;
                if (num != null) num.text = lvCfg.CardCount.ToString();
                break;
            }
        }

        private void BindCardButtons(Transform root, SchoolChooseCardViewModel.CardDisplayData data, int slotIndex)
        {
            var refreshBtn = root.Find("RefreshBtn")?.GetComponent<UIPointerClickAndLongPress>();
            if (refreshBtn != null)
            {
                UnityAction refreshAction = () => OnClickRefresh(slotIndex);
                _refreshBtnHandlers[slotIndex] = refreshAction;
                refreshBtn.onClick.AddListener(refreshAction);
            }

            var chooseBtn = root.GetComponent<UIPointerClickAndLongPress>();
            if (chooseBtn != null)
            {
                UnityAction chooseAction = () => OnClickChoose(root, data);
                _chooseBtnHandlers[slotIndex] = chooseAction;
                chooseBtn.onClick.AddListener(chooseAction);
            }
        }

        private void ClearCardButtonHandlers()
        {
            if (mOldPanel == null || mOldPanel.ChooseList == null) return;

            foreach (var kv in _refreshBtnHandlers)
            {
                var cardObj = mOldPanel.ChooseList.Find($"ChoseCard1{kv.Key + 1}");
                var refreshBtn = cardObj?.Find("RefreshBtn")?.GetComponent<UIPointerClickAndLongPress>();
                if (refreshBtn != null)
                {
                    refreshBtn.onClick.RemoveListener(kv.Value);
                }
            }
            _refreshBtnHandlers.Clear();

            foreach (var kv in _chooseBtnHandlers)
            {
                var cardObj = mOldPanel.ChooseList.Find($"ChoseCard1{kv.Key + 1}");
                var chooseBtn = cardObj?.GetComponent<UIPointerClickAndLongPress>();
                if (chooseBtn != null)
                {
                    chooseBtn.onClick.RemoveListener(kv.Value);
                }
            }
            _chooseBtnHandlers.Clear();
        }

        private void OnClickRefresh(int slotIndex)
        {
            if (VM == null) return;

            var service = GameServiceManager.Get<GameService>();
            var (canAfford, _) = service.CheckRefreshCost();
            if (!canAfford) return;

            var currentCardIds = VM.CardList.Value.Select(v => v.CardId).ToList();
            int newCardId = service.RefreshSingleCard(slotIndex, currentCardIds);
            if (newCardId <= 0) return;

            var cfg = service.GetSchoolCardConfig(newCardId);
            if (cfg == null) return;

            var updatedCards = new List<SchoolChooseCardViewModel.CardDisplayData>(VM.CardList.Value);
            if (slotIndex < 0 || slotIndex >= updatedCards.Count) return;

            updatedCards[slotIndex] = VM.CreateCardDisplayData(newCardId, cfg, service, slotIndex + 1);
            updatedCards[slotIndex].IsRecommended = false;
            _pendingAnimateSlotIndex = slotIndex;
            VM.CardList.Value = updatedCards;
        }

        /// <summary>选中单卡：播放选中/未选中 Animator，0.5s 后提交选择并关闭。</summary>
        private void OnClickChoose(Transform cardObj, SchoolChooseCardViewModel.CardDisplayData data)
        {
            var service = GameServiceManager.Get<GameService>();
            SetAllCardButtonsInteractable(false);
            SetAllRefreshAreaVisible(false);
            if (mOldPanel.GetAllBtn != null)
            {
                mOldPanel.GetAllBtn.interactable = false;
            }

            CancelLegendEntranceForCard(cardObj);
            PrepareCardForChooseAnimation(cardObj, data);
            PlayChooseCardAnimations(cardObj, data.Quality);

            SubmitChooseAndCloseAfterAnimator(() =>
            {
                service.ChooseCard(data.CardId, data.Schools, data.IsCategorySkill);
            });
        }

        private void SubmitChooseAndCloseAfterAnimator(System.Action submitChoose)
        {
            _cardEffectTweens.Add(DOVirtual.DelayedCall(ChooseCloseDelay, () =>
            {
                submitChoose();
                GameServiceManager.Get<GameService>().ChooseOver();
            }, true));
        }

        private void SetAllCardButtonsInteractable(bool interactable)
        {
            if (mOldPanel == null || mOldPanel.ChooseList == null) return;

            for (int i = 0; i < mOldPanel.ChooseList.childCount; i++)
            {
                var child = mOldPanel.ChooseList.GetChild(i);
                if (!child.name.StartsWith("ChoseCard") || !child.gameObject.activeSelf) continue;

                var chooseBtn = child.GetComponent<UIPointerClickAndLongPress>();
                if (chooseBtn != null) chooseBtn.interactable = interactable;

                var refreshBtn = child.Find("RefreshBtn")?.GetComponent<UIPointerClickAndLongPress>();
                if (refreshBtn != null) refreshBtn.interactable = interactable;
            }
        }

        /// <summary>关闭动画时隐藏 RefreshBtn 与 hor（金币 icon + 刷新费用）；重新打开/重绘时显示。</summary>
        private void SetAllRefreshAreaVisible(bool visible)
        {
            if (mOldPanel == null || mOldPanel.ChooseList == null) return;

            for (int i = 0; i < mOldPanel.ChooseList.childCount; i++)
            {
                var child = mOldPanel.ChooseList.GetChild(i);
                if (!child.name.StartsWith("ChoseCard") || !child.gameObject.activeSelf) continue;

                var refreshBtn = child.Find("RefreshBtn");
                if (refreshBtn != null)
                {
                    refreshBtn.gameObject.SetActive(visible);
                }

                var refreshCostHor = child.Find("hor");
                if (refreshCostHor != null)
                {
                    refreshCostHor.gameObject.SetActive(visible);
                }
            }
        }

        private void RefreshGoldCostUI(SchoolChooseCardViewModel vm)
        {
            if (mOldPanel == null || mOldPanel.ChooseList == null) return;

            var service = GameServiceManager.Get<GameService>();
            int cost = service.GetRefreshCost();
            bool enough = Global.GameGold.Value > cost;
            string costText = enough ? cost.ToString() : $"<color=red>{cost}</color>";
            string btnSprite = enough ? "YellowRefreshButton" : "GrayRefreshButton";

            for (int i = 0; i < vm.CardList.Value.Count; i++)
            {
                var cardObj = mOldPanel.ChooseList.Find($"ChoseCard1{i + 1}");
                if (cardObj == null || !cardObj.gameObject.activeSelf) continue;

                var goldText = cardObj.Find("hor/goldText")?.GetComponent<TMP_Text>();
                if (goldText != null)
                {
                    goldText.text = costText;
                }

                var refreshBtn = cardObj.Find("RefreshBtn")?.GetComponent<UIPointerClickAndLongPress>();
                if (refreshBtn != null)
                {
                    refreshBtn.interactable = enough;
                }

                var refreshBtnImage = cardObj.Find("RefreshBtn")?.GetComponent<Image>();
                if (refreshBtnImage != null)
                {
                    refreshBtnImage.sprite = Global.SchoolChooseCardIconSpriteAtlas.GetSpriteExt(btnSprite);
                }
            }
        }

        private void SetCardStars(Transform starList, int starNum, QualityType qualityType, bool shineStar)
        {
            for (int i = 0; i < starList.childCount; i++)
            {
                var starObj = starList.GetChild(i);
                starObj.gameObject.SetActive(starNum >= i + 1);

                var starImage = starObj.GetComponent<Image>();
                if (starImage != null)
                {
                    starImage.sprite = Global.SchoolChooseCardIconSpriteAtlas.GetSpriteExt($"{qualityType}Star2");
                }

                var shine = starObj.Find("Shine")?.GetComponent<Image>();
                if (shine != null)
                {
                    shine.color = Color.white;
                    shine.sprite = Global.SchoolChooseCardIconSpriteAtlas.GetSpriteExt($"{qualityType}Star1");
                }
            }

            if (!shineStar) return;
            if (starNum <= 0 || starNum >= starList.childCount) return;

            var shineStarObj = starList.GetChild(starNum);
            shineStarObj.gameObject.SetActive(true);
            var shineImage = shineStarObj.Find("Shine")?.GetComponent<Image>();
            if (shineImage != null)
            {
                _starTweens.Add(
                    shineImage.DOFade(0f, 0.8f)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine).SetUpdate(true));
            }
        }

        private void StopAllCardEffects()
        {
            foreach (var tween in _starTweens)
            {
                tween.Kill();
            }
            _starTweens.Clear();

            foreach (var tween in _cardEffectTweens)
            {
                tween.Kill();
            }
            _cardEffectTweens.Clear();
            _legendEntranceTweens.Clear();

            RestoreAllDetachedFlipEffects();
            RestoreAllActiveCardVisuals();
        }

        /// <summary>重绘前回收可能被挂到场景根的转牌特效，避免刷新时闪一下。</summary>
        private void ResetAllCardEffectTransforms()
        {
            if (mOldPanel == null || mOldPanel.ChooseList == null) return;

            for (int i = 0; i < mOldPanel.ChooseList.childCount; i++)
            {
                var child = mOldPanel.ChooseList.GetChild(i);
                if (!child.name.StartsWith("ChoseCard")) continue;

                CleanupCardEffectTransformsWithFlip(child);
            }
        }

        private static void CleanupCardEffectTransforms(Transform parent)
        {
            parent.DOKill();
            ResetEffectTransform(parent, EffectName2);
            ResetEffectTransform(parent, EffectLegend);
            ResetEffectTransform(parent, EffectEpic);
            ResetEffectTransform(parent, EffectRarity);
        }

        private void CleanupCardEffectTransformsWithFlip(Transform parent)
        {
            RestoreDetachedFlipEffect(parent);
            CleanupCardEffectTransforms(parent);
        }

        private static void ResetEffectTransform(Transform parent, string effectName)
        {
            var effect = parent.Find($"{effectName}(Clone)");
            if (effect == null) return;

            effect.SetParent(parent, false);
            effect.localPosition = Vector3.zero;
            effect.localScale = Vector3.one;
            effect.gameObject.SetActive(false);
        }

        private void RestoreDetachedFlipEffect(Transform parent)
        {
            if (!_detachedFlipEffects.TryGetValue(parent, out var effect) || effect == null)
            {
                return;
            }

            effect.SetParent(parent, false);
            effect.localPosition = Vector3.zero;
            effect.localScale = Vector3.one;
            effect.gameObject.SetActive(false);
            _detachedFlipEffects.Remove(parent);
        }

        private void RestoreAllDetachedFlipEffects()
        {
            foreach (var kv in _detachedFlipEffects)
            {
                if (kv.Key == null || kv.Value == null) continue;

                kv.Value.SetParent(kv.Key, false);
                kv.Value.localPosition = Vector3.zero;
                kv.Value.localScale = Vector3.one;
                kv.Value.gameObject.SetActive(false);
            }

            _detachedFlipEffects.Clear();
        }

        /// <summary>选中/关闭 Animator 前隐藏品质粒子，避免与 danji 动画叠层露馅。</summary>
        private void HideAllCardQualityEffects()
        {
            if (mOldPanel == null || mOldPanel.ChooseList == null) return;

            for (int i = 0; i < mOldPanel.ChooseList.childCount; i++)
            {
                var child = mOldPanel.ChooseList.GetChild(i);
                if (!child.name.StartsWith("ChoseCard") || !child.gameObject.activeSelf) continue;

                CleanupCardEffectTransformsWithFlip(child);
            }
        }

        /// <summary>Legend / Epic / Rarity 有 DOTween 入场；其余品质无延迟特效。</summary>
        private static bool HasQualityEntranceEffect(QualityType quality)
        {
            return quality == QualityType.Legend
                || quality == QualityType.Epic
                || quality == QualityType.Rarity;
        }

        /// <summary>Legend/Epic/Rarity 入场前统一重置：回收特效、恢复 scale，Legend 暂停 Animator。</summary>
        private void PrepareCardForEntrance(Transform root)
        {
            root.DOKill();
            RestoreDetachedFlipEffect(root);
            CleanupCardEffectTransforms(root);

            var scale = root.localScale;
            root.localScale = new Vector3(1f, scale.y, scale.z);

            SetLegendAnimatorEnabled(root, false);
        }

        private static void SetNodeActive(Transform root, string nodeName, bool active)
        {
            var node = root.Find(nodeName);
            if (node != null)
            {
                node.gameObject.SetActive(active);
            }
        }

        private static readonly string[] LegendEntranceHiddenNodes =
        {
            "Title", "Category", "Card", "Bg", "Description", "StarList", "Mask",
            "BelongSchool", "SkillStar", "recommendIcon", "SuperTip", "RefreshBtn", "hor"
        };

        private static void HideCardVisualsForLegendEntrance(Transform root)
        {
            for (int i = 0; i < LegendEntranceHiddenNodes.Length; i++)
            {
                SetNodeActive(root, LegendEntranceHiddenNodes[i], false);
            }
        }

        private static void SetLegendAnimatorEnabled(Transform root, bool enabled)
        {
            var animator = root.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = enabled;
            }
        }

        private static void RestoreCardCanvasGroup(Transform root)
        {
            root.DOKill();
            var canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            var scale = root.localScale;
            root.localScale = new Vector3(1f, scale.y, scale.z);
        }

        private void RestoreAllActiveCardVisuals()
        {
            if (mOldPanel == null || mOldPanel.ChooseList == null) return;

            var cards = VM != null ? VM.CardList.Value : null;
            for (int i = 0; i < mOldPanel.ChooseList.childCount; i++)
            {
                var child = mOldPanel.ChooseList.GetChild(i);
                if (!child.name.StartsWith("ChoseCard") || !child.gameObject.activeSelf) continue;

                RestoreCardCanvasGroup(child);
                SetLegendAnimatorEnabled(child, true);

                int slotIndex = GetCardSlotIndex(child.name);
                if (cards != null && slotIndex >= 0 && slotIndex < cards.Count)
                {
                    PopulateCardVisuals(child, cards[slotIndex]);
                }
            }
        }

        private static int GetCardSlotIndex(string choseCardName)
        {
            const string prefix = "ChoseCard1";
            if (!choseCardName.StartsWith(prefix))
            {
                return -1;
            }

            if (!int.TryParse(choseCardName.Substring(prefix.Length), out int slotNum))
            {
                return -1;
            }

            return slotNum - 1;
        }

        private void TrackCardEffectTween(Tween tween)
        {
            if (tween == null) return;

            _cardEffectTweens.Add(tween);
        }

        private void PlayCardScaleIn(Transform root, float duration, System.Action onComplete)
        {
            root.DOKill();
            var scale = root.localScale;
            root.localScale = new Vector3(0f, scale.y, scale.z);
            TrackCardEffectTween(
                root.DOScaleX(1f, duration)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        root.localScale = new Vector3(1f, scale.y, scale.z);
                        onComplete();
                    }));
        }

        private static Transform FindCardEffect(Transform parent, string effectName, Dictionary<Transform, Transform> detachedFlipEffects)
        {
            var cloneName = $"{effectName}(Clone)";
            var effect = parent.Find(cloneName);
            if (effect != null)
            {
                return effect;
            }

            if (effectName == EffectName2
                && detachedFlipEffects.TryGetValue(parent, out var detached)
                && detached != null)
            {
                return detached;
            }

            return null;
        }

        private void CancelLegendEntranceForCard(Transform parent)
        {
            if (!_legendEntranceTweens.TryGetValue(parent, out var tween))
            {
                return;
            }

            tween.Kill();
            _legendEntranceTweens.Remove(parent);
        }

        private void PrepareCardForChooseAnimation(Transform cardRoot, SchoolChooseCardViewModel.CardDisplayData data)
        {
            SetLegendAnimatorEnabled(cardRoot, true);
            RestoreCardCanvasGroup(cardRoot);
            PopulateCardVisuals(cardRoot, data);
        }

        private void ScheduleLegendEntranceShow(
            Transform parent,
            Transform entranceEffect,
            SchoolChooseCardViewModel.CardDisplayData data)
        {
            CancelLegendEntranceForCard(parent);

            var tween = DOVirtual.DelayedCall(LegendEntranceDelay, () =>
            {
                _legendEntranceTweens.Remove(parent);
                FinishLegendEntrance(parent, entranceEffect, data);
            }, true);
            _legendEntranceTweens[parent] = tween;
            TrackCardEffectTween(tween);
        }

        private void FinishLegendEntrance(
            Transform parent,
            Transform entranceEffect,
            SchoolChooseCardViewModel.CardDisplayData data)
        {
            if (entranceEffect != null)
            {
                entranceEffect.gameObject.SetActive(true);
                PlayEffectParticlesUnscaled(entranceEffect);
            }
            SetLegendAnimatorEnabled(parent, true);
            ResetCardAnimator(parent);
            PopulateCardVisuals(parent, data);
            RestoreCardCanvasGroup(parent);
        }

        private void ResetCardAnimator(Transform cardRoot)
        {
            var animator = cardRoot.GetComponent<Animator>();
            if (animator == null) return;

            PlayCardAnimatorClip(animator, AnimStateNormal, AnimNormal);
        }

        /// <summary>点击选中卡：选中卡按品质播 danji，其余卡 noselect。</summary>
        private void PlayChooseCardAnimations(Transform selectedCard, QualityType selectedQuality)
        {
            if (mOldPanel == null || mOldPanel.ChooseList == null) return;

            HideAllCardQualityEffects();

            for (int i = 0; i < mOldPanel.ChooseList.childCount; i++)
            {
                var child = mOldPanel.ChooseList.GetChild(i);
                if (!child.name.StartsWith("ChoseCard") || !child.gameObject.activeSelf) continue;

                SetLegendAnimatorEnabled(child, true);
                RestoreCardCanvasGroup(child);

                var animator = child.GetComponent<Animator>();
                if (animator == null) continue;

                if (child == selectedCard)
                {
                    PlayCardAnimatorClip(
                        animator,
                        GetSelectAnimatorState(selectedQuality),
                        GetSelectAnimatorBoolParam(selectedQuality));
                }
                else
                {
                    PlayCardAnimatorClip(animator, AnimStateNoselect, AnimNoselect);
                }
            }
        }

        private void PlayGetAllCardAnimations(List<SchoolChooseCardViewModel.CardDisplayData> cards)
        {
            if (mOldPanel == null || mOldPanel.ChooseList == null) return;

            HideAllCardQualityEffects();

            for (int i = 0; i < cards.Count; i++)
            {
                var cardObj = mOldPanel.ChooseList.Find($"ChoseCard1{i + 1}");
                if (cardObj == null || !cardObj.gameObject.activeSelf) continue;

                CancelLegendEntranceForCard(cardObj);
                PrepareCardForChooseAnimation(cardObj, cards[i]);

                var animator = cardObj.GetComponent<Animator>();
                if (animator == null) continue;

                PlayCardAnimatorClip(
                    animator,
                    GetSelectAnimatorState(cards[i].Quality),
                    GetSelectAnimatorBoolParam(cards[i].Quality));
            }
        }

        private static string GetSelectAnimatorBoolParam(QualityType quality)
        {
            switch (quality)
            {
                case QualityType.Rarity:
                    return AnimYinDanji;
                case QualityType.Epic:
                    return AnimJinDanji;
                case QualityType.Legend:
                    return AnimCaiDanji;
                default:
                    return AnimYinDanji;
            }
        }

        private static string GetSelectAnimatorState(QualityType quality)
        {
            switch (quality)
            {
                case QualityType.Rarity:
                    return AnimStateYinDanji;
                case QualityType.Epic:
                    return AnimStateJinDanji;
                case QualityType.Legend:
                    return AnimStateCaiDanji;
                default:
                    return AnimStateYinDanji;
            }
        }

        /// <summary>直接 Play State 并同步 bool，避免仅 SetBool 时 AnyState 不触发。</summary>
        private static void PlayCardAnimatorClip(Animator animator, string stateName, string activeBoolParam)
        {
            animator.enabled = true;
            animator.SetBool(AnimNormal, activeBoolParam == AnimNormal);
            animator.SetBool(AnimNoselect, activeBoolParam == AnimNoselect);
            animator.SetBool(AnimYinDanji, activeBoolParam == AnimYinDanji);
            animator.SetBool(AnimCaiDanji, activeBoolParam == AnimCaiDanji);
            animator.SetBool(AnimJinDanji, activeBoolParam == AnimJinDanji);
            animator.Play(stateName, 0, 0f);
            animator.Update(0f);
        }

        private void OnChooseOver()
        {
            StopAllCardEffects();
            UIManager.ClosePanel(this);
        }

        private void OnCountdownAutoChooseRequested(int slotIndex)
        {
            if (VM == null || mOldPanel == null || mOldPanel.ChooseList == null) return;
            var cards = VM.CardList.Value;
            if (cards == null || slotIndex < 0 || slotIndex >= cards.Count) return;

            var cardObj = mOldPanel.ChooseList.Find($"ChoseCard1{slotIndex + 1}");
            if (cardObj == null || !cardObj.gameObject.activeSelf) return;

            OnClickChoose(cardObj, cards[slotIndex]);
        }

        /// <summary>
        /// 按品质处理入场：Legend 播放翻牌和品质特效，非 Legend 只播放 scaleX。
        /// </summary>
        private void EffectCard(
            QualityType quality,
            Transform parent,
            bool playEntranceAnimation,
            bool delayedShow,
            SchoolChooseCardViewModel.CardDisplayData data)
        {
            var color02 = FindCardEffect(parent, EffectName2, _detachedFlipEffects);
            var legend = FindCardEffect(parent, EffectLegend, _detachedFlipEffects);
            var epic = FindCardEffect(parent, EffectEpic, _detachedFlipEffects);
            var rarity = FindCardEffect(parent, EffectRarity, _detachedFlipEffects);

            color02?.gameObject.SetActive(false);
            legend?.gameObject.SetActive(false);
            epic?.gameObject.SetActive(false);
            rarity?.gameObject.SetActive(false);

            Vector3 localScale = Vector3.one;
            Vector3 localPosition = Vector3.zero;

            if (quality == QualityType.Legend)
            {
                if (color02 == null)
                {
                    color02 = Instantiate(Global.ResLoader.LoadSync<GameObject>(EffectName2), parent, true).transform;
                    color02.localPosition = localPosition;
                    color02.localScale = localScale;
                }

                if (legend == null)
                {
                    legend = Instantiate(Global.ResLoader.LoadSync<GameObject>(EffectLegend), parent, true).transform;
                    legend.localPosition = localPosition;
                    legend.localScale = localScale;
                }

                if (playEntranceAnimation)
                {
                    parent.DOKill();
                    legend.gameObject.SetActive(false);
                    ShowLegendFlipEffect(parent, color02);
                    ScheduleLegendEntranceShow(parent, legend, data);
                    return;
                }

                legend.gameObject.SetActive(true);
                PlayEffectParticlesUnscaled(legend);
                return;
            }

            if (quality == QualityType.Epic)
            {
                if (epic == null)
                {
                    epic = Instantiate(Global.ResLoader.LoadSync<GameObject>(EffectEpic), parent, true).transform;
                    epic.localPosition = localPosition;
                    epic.localScale = localScale;
                }

                epic.gameObject.SetActive(true);
                PlayEffectParticlesUnscaled(epic);
                if (playEntranceAnimation)
                {
                    PlayCardScaleIn(parent, 0.3f, () => RestoreCardCanvasGroup(parent));
                }

                return;
            }

            if (quality == QualityType.Rarity)
            {
                if (rarity == null)
                {
                    rarity = Instantiate(Global.ResLoader.LoadSync<GameObject>(EffectRarity), parent, true).transform;
                    rarity.localPosition = localPosition;
                    rarity.localScale = localScale;
                }

                rarity.gameObject.SetActive(true);
                PlayEffectParticlesUnscaled(rarity);
                if (playEntranceAnimation)
                {
                    PlayCardScaleIn(parent, 0.3f, () => RestoreCardCanvasGroup(parent));
                }

                return;
            }

            if (playEntranceAnimation)
            {
                PlayCardScaleIn(parent, 0.3f, () => RestoreCardCanvasGroup(parent));
            }
        }

        private void ShowLegendFlipEffect(Transform cardRoot, Transform color02)
        {
            color02.gameObject.SetActive(true);
            color02.SetParent(transform, true);
            color02.SetAsLastSibling();
            _detachedFlipEffects[cardRoot] = color02;
            PlayEffectParticlesUnscaled(color02);
            ScheduleFlipEffectCleanup(cardRoot, color02);
        }

        private void ScheduleFlipEffectCleanup(Transform cardRoot, Transform color02)
        {
            float duration = GetParticleEffectDuration(color02);
            var tween = DOVirtual.DelayedCall(duration, () =>
            {
                RestoreDetachedFlipEffect(cardRoot);
            }, true);
            TrackCardEffectTween(tween);
        }

        private float GetParticleEffectDuration(Transform effectRoot)
        {
            float duration = 0f;
            var particleSystems = effectRoot.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                var main = particleSystems[i].main;
                float particleDuration = GetCurveMax(main.startDelay) + main.duration + GetCurveMax(main.startLifetime);
                duration = Mathf.Max(duration, particleDuration);
            }

            if (duration <= 0f)
            {
                duration = LegendEntranceDelay;
            }

            return duration;
        }

        private float GetCurveMax(ParticleSystem.MinMaxCurve curve)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return curve.constant;
                case ParticleSystemCurveMode.TwoConstants:
                    return curve.constantMax;
                case ParticleSystemCurveMode.Curve:
                    return GetAnimationCurveMax(curve.curve) * curve.curveMultiplier;
                case ParticleSystemCurveMode.TwoCurves:
                    return Mathf.Max(
                        GetAnimationCurveMax(curve.curveMin),
                        GetAnimationCurveMax(curve.curveMax)) * curve.curveMultiplier;
                default:
                    return curve.constantMax;
            }
        }

        private float GetAnimationCurveMax(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
            {
                return 0f;
            }

            float value = curve.keys[0].value;
            for (int i = 1; i < curve.length; i++)
            {
                value = Mathf.Max(value, curve.keys[i].value);
            }

            return value;
        }
    }
}
