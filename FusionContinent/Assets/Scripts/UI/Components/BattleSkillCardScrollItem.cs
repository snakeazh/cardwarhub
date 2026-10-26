using Config;
using DG.Tweening;
using FusionContinent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.UI.Components
{
    /// <summary>
    /// cardScroll 中单张技能卡的展示与点击。
    /// 父节点 CardInfo 负责布局与点击；子节点 Canvas 仅负责表现（缩放、层级）。
    /// </summary>
    public class BattleSkillCardScrollItem : MonoBehaviour
    {
        private int mSkillId;
        private Button mClickButton;
        private Canvas mCardCanvas;
        private RectTransform mScaleTarget;
        private bool mManualOperateAllowed;
        private bool mScrollFocusAllowed = true;
        private float mBaseLayoutWidth;
        private float mBaseLayoutHeight;
        private bool mHasBaseLayoutSize;
        private bool? mLastIsNormalSize;
        private float mDisplayBaseScale = 1f;
        private bool mIsAutoReleaseScaleAnimating;
        private bool mAutoReleaseAnimatorOverridden;
        private bool mPressAnimatorEnabled;
        private bool mWasCooling;
        private bool mSilenced;
        private Tweener mScaleTween;
        private Tweener mWidthTween;

        private const string AutoReleaseScaleTweenId = "AutoReleaseScale";
        private const float AutoReleaseScalePhase1Duration = 0.2f;
        private const float AutoReleaseScalePhase2Duration = 0.3f;
        private const float AutoReleaseScalePhase3Duration = 0.3f;
        private const float AutoReleaseScaleMin = 0.83f;
        private const float AutoReleaseScalePeak = 1.2f;

        public int SkillId => mSkillId;

        public RectTransform ScaleTarget
        {
            get
            {
                if (mScaleTarget == null)
                {
                    mScaleTarget = transform.Find("Canvas") as RectTransform;
                    if (mScaleTarget == null)
                    {
                        LogTool.LogError("[BattleSkillCardScrollItem] 未找到子节点 Canvas");
                    }
                }

                return mScaleTarget;
            }
        }

        private void EnsureLayoutElement()
        {
            var rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            CacheBaseLayoutSize(layoutElement, rectTransform);

            if (layoutElement.preferredWidth <= 0f)
            {
                layoutElement.preferredWidth = mBaseLayoutWidth;
                layoutElement.minWidth = mBaseLayoutWidth;
            }

            if (layoutElement.preferredHeight <= 0f)
            {
                layoutElement.preferredHeight = mBaseLayoutHeight;
            }
        }

        private void CacheBaseLayoutSize(LayoutElement layoutElement, RectTransform rectTransform)
        {
            if (mHasBaseLayoutSize)
            {
                return;
            }

            if (layoutElement.preferredWidth > 0f)
            {
                mBaseLayoutWidth = layoutElement.preferredWidth;
            }
            else if (rectTransform.rect.width > 0f)
            {
                mBaseLayoutWidth = rectTransform.rect.width;
            }
            else
            {
                mBaseLayoutWidth = 249f;
            }

            if (layoutElement.preferredHeight > 0f)
            {
                mBaseLayoutHeight = layoutElement.preferredHeight;
            }
            else if (rectTransform.rect.height > 0f)
            {
                mBaseLayoutHeight = rectTransform.rect.height;
            }
            else
            {
                mBaseLayoutHeight = 230f;
            }

            mHasBaseLayoutSize = true;
        }

        public bool SetLayoutSlotSize(bool isNormalSize, float normalWidth, float edgeWidth)
        {
            EnsureLayoutElement();

            var layoutElement = GetComponent<LayoutElement>();
            var rectTransform = transform as RectTransform;
            if (layoutElement == null || rectTransform == null)
            {
                return false;
            }

            if (!mHasBaseLayoutSize)
            {
                if (normalWidth > 0f)
                {
                    mBaseLayoutWidth = normalWidth;
                    mBaseLayoutHeight = rectTransform.rect.height > 0f ? rectTransform.rect.height : 230f;
                    mHasBaseLayoutSize = true;
                }
                else
                {
                    CacheBaseLayoutSize(layoutElement, rectTransform);
                }
            }

            var targetWidth = isNormalSize ? mBaseLayoutWidth : edgeWidth;
            return ApplyLayoutWidth(targetWidth, layoutElement, rectTransform);
        }

        public bool IsAutoReleaseScaleAnimating()
        {
            return mIsAutoReleaseScaleAnimating;
        }

        public bool TweenToDisplayState(bool isNormalSize, float targetScale, float targetWidth, float duration,
            System.Action onLayoutDirty)
        {
            if (mIsAutoReleaseScaleAnimating)
            {
                return false;
            }

            if (!mLastIsNormalSize.HasValue && IsDisplayStateMatched(targetScale, targetWidth))
            {
                mLastIsNormalSize = isNormalSize;
                return false;
            }

            if (mLastIsNormalSize.HasValue && mLastIsNormalSize.Value == isNormalSize &&
                (IsDisplayTweenActive() || IsDisplayStateMatched(targetScale, targetWidth)))
            {
                return false;
            }

            mLastIsNormalSize = isNormalSize;
            mDisplayBaseScale = targetScale;
            KillDisplayTweens();
            KillAutoReleaseScaleTween();

            var scaleTarget = ScaleTarget;
            if (scaleTarget != null)
            {
                mScaleTween = scaleTarget
                    .DOScale(Vector3.one * targetScale, duration)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true);
            }

            EnsureLayoutElement();
            var layoutElement = GetComponent<LayoutElement>();
            var rectTransform = transform as RectTransform;
            if (layoutElement == null || rectTransform == null)
            {
                return false;
            }

            var currentWidth = rectTransform.sizeDelta.x;
            mWidthTween = DOTween.To(
                    () => currentWidth,
                    value =>
                    {
                        currentWidth = value;
                        ApplyLayoutWidth(value, layoutElement, rectTransform);
                        onLayoutDirty?.Invoke();
                    },
                    targetWidth,
                    duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(() => onLayoutDirty?.Invoke());

            return true;
        }

        public void SnapDisplayState(bool isNormalSize, float targetScale, float targetWidth)
        {
            if (mIsAutoReleaseScaleAnimating)
            {
                return;
            }

            KillDisplayTweens();
            KillAutoReleaseScaleTween();
            mLastIsNormalSize = isNormalSize;
            mDisplayBaseScale = targetScale;

            var scaleTarget = ScaleTarget;
            if (scaleTarget != null)
            {
                scaleTarget.localScale = Vector3.one * targetScale;
            }

            EnsureLayoutElement();
            var layoutElement = GetComponent<LayoutElement>();
            var rectTransform = transform as RectTransform;
            if (layoutElement != null && rectTransform != null)
            {
                ApplyLayoutWidth(targetWidth, layoutElement, rectTransform);
            }
        }

        public void ResetDisplayStateCache()
        {
            mLastIsNormalSize = null;
        }

        private bool IsDisplayTweenActive()
        {
            return (mScaleTween != null && mScaleTween.IsActive()) ||
                   (mWidthTween != null && mWidthTween.IsActive());
        }

        private bool IsDisplayStateMatched(float targetScale, float targetWidth)
        {
            var scaleTarget = ScaleTarget;
            if (scaleTarget != null &&
                !Mathf.Approximately(scaleTarget.localScale.x, targetScale))
            {
                return false;
            }

            var rectTransform = transform as RectTransform;
            return rectTransform != null &&
                   Mathf.Approximately(rectTransform.sizeDelta.x, targetWidth);
        }

        private bool ApplyLayoutWidth(float width, LayoutElement layoutElement, RectTransform rectTransform)
        {
            var widthChanged = false;

            if (!Mathf.Approximately(layoutElement.preferredWidth, width))
            {
                layoutElement.preferredWidth = width;
                layoutElement.minWidth = width;
                widthChanged = true;
            }

            var sizeDelta = rectTransform.sizeDelta;
            if (!Mathf.Approximately(sizeDelta.x, width))
            {
                sizeDelta.x = width;
                rectTransform.sizeDelta = sizeDelta;
                widthChanged = true;
            }

            return widthChanged;
        }

        private void KillDisplayTweens()
        {
            if (mScaleTween != null && mScaleTween.IsActive())
            {
                mScaleTween.Kill();
            }

            mScaleTween = null;

            if (mWidthTween != null && mWidthTween.IsActive())
            {
                mWidthTween.Kill();
            }

            mWidthTween = null;
        }

        private void KillAutoReleaseScaleTween()
        {
            var scaleTarget = ScaleTarget;
            if (scaleTarget != null)
            {
                DOTween.Kill(scaleTarget, AutoReleaseScaleTweenId);
            }

            mIsAutoReleaseScaleAnimating = false;
            mAutoReleaseAnimatorOverridden = false;
        }

        /// <summary>
        /// 自动模式切换时清理自动释放缩放状态，避免 Canvas 缩放与 Animator 状态残留。
        /// </summary>
        public void ClearAutoReleasePresentation()
        {
            KillAutoReleaseScaleTween();

            var scaleTarget = ScaleTarget;
            if (scaleTarget != null)
            {
                scaleTarget.localScale = Vector3.one * mDisplayBaseScale;
            }
        }

        /// <summary>
        /// 自动释放技能时，仅未缩小的卡牌播放 Canvas 缩放反馈：0.2s→0.83，0.3s→1.2，0.3s→1。
        /// </summary>
        public void PlayAutoReleaseScaleTween()
        {
            if (!Global.AutoReleaseSkillState.Value || !mScrollFocusAllowed)
            {
                return;
            }

            var scaleTarget = ScaleTarget;
            if (scaleTarget == null)
            {
                return;
            }

            KillAutoReleaseScaleTween();
            if (mScaleTween != null && mScaleTween.IsActive())
            {
                mScaleTween.Kill();
                mScaleTween = null;
            }

            SetPressAnimatorEnabled(false);
            mAutoReleaseAnimatorOverridden = true;

            var baseScale = mDisplayBaseScale;
            mIsAutoReleaseScaleAnimating = true;
            DOTween.Sequence()
                .Append(scaleTarget.DOScale(Vector3.one * (baseScale * AutoReleaseScaleMin),
                    AutoReleaseScalePhase1Duration).SetEase(Ease.OutQuad))
                .Append(scaleTarget.DOScale(Vector3.one * (baseScale * AutoReleaseScalePeak),
                    AutoReleaseScalePhase2Duration).SetEase(Ease.OutQuad))
                .Append(scaleTarget.DOScale(Vector3.one * baseScale, AutoReleaseScalePhase3Duration)
                    .SetEase(Ease.OutQuad))
                .SetId(AutoReleaseScaleTweenId)
                .SetTarget(scaleTarget)
                .SetUpdate(true)
                .OnComplete(OnAutoReleaseScaleTweenFinished)
                .OnKill(OnAutoReleaseScaleTweenFinished);
        }

        private void OnAutoReleaseScaleTweenFinished()
        {
            mIsAutoReleaseScaleAnimating = false;
            mAutoReleaseAnimatorOverridden = false;
            ApplyPressAnimatorForScrollScale(mScrollFocusAllowed);
        }

        private void SetPressAnimatorEnabled(bool enabled)
        {
            if (mPressAnimatorEnabled == enabled)
            {
                return;
            }

            mPressAnimatorEnabled = enabled;

            var rootAnimator = GetComponent<Animator>();
            var buttonAnim = GetComponentInChildren<UIButtonAnim>(true);
            if (buttonAnim != null)
            {
                buttonAnim.SetAnimatorEnabled(enabled);
            }
            else if (rootAnimator != null)
            {
                rootAnimator.enabled = enabled;
            }

            if (!enabled || rootAnimator == null)
            {
                return;
            }

            rootAnimator.Play("Normal", 0, 0f);
        }

        public void ResetLayoutSlotSize()
        {
            EnsureLayoutElement();

            var layoutElement = GetComponent<LayoutElement>();
            var rectTransform = transform as RectTransform;
            if (layoutElement == null || rectTransform == null || !mHasBaseLayoutSize)
            {
                return;
            }

            ApplyLayoutWidth(mBaseLayoutWidth, layoutElement, rectTransform);
        }

        private void Awake()
        {
            EnsureLayoutElement();
            DisableChildRaycastTargets();

            mClickButton = GetComponent<Button>();
            if (mClickButton != null)
            {
                mClickButton.onClick.AddListener(OnCardClicked);
            }
        }

        private void OnDestroy()
        {
            KillDisplayTweens();
            KillAutoReleaseScaleTween();

            if (mClickButton != null)
            {
                mClickButton.onClick.RemoveListener(OnCardClicked);
            }
        }

        public void Bind(BattleCardGroup battleSkill, bool canManualOperate)
        {
            if (battleSkill == null)
            {
                gameObject.SetActive(false);
                return;
            }

            mSkillId = battleSkill.CardId;

            var cardCfg = battleSkill.CardBaseCfg;
            var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(mSkillId);
            if (cardCfg == null || specialCfg == null)
            {
                gameObject.SetActive(false);
                return;
            }

            var painting = transform.Find("Canvas/Mask/Painting")?.GetComponent<Image>();
            if (painting != null)
            {
                painting.sprite = Global.CardIconSpriteAtlas.GetSpriteExt(cardCfg.Icon);
            }

            SkillCardUiHelper.ApplyQualityFrame(battleSkill, transform);

            var cardName = transform.Find("Canvas/CardName")?.GetComponent<TMP_Text>();
            if (cardName != null)
            {
                cardName.text = cardCfg.Name;
            }

            var levelText = transform.Find("Canvas/Level/LevelText")?.GetComponent<Text>();
            if (levelText != null)
            {
                levelText.text = $"Lv {battleSkill.CardLevel}";
            }

            SetManualOperate(canManualOperate);
            mWasCooling = false;
            gameObject.SetActive(true);
        }

        public void SetManualOperate(bool canManualOperate)
        {
            if (mManualOperateAllowed == canManualOperate)
            {
                return;
            }

            mManualOperateAllowed = canManualOperate;
            ApplyInteractable();
            ApplyPressAnimatorForScrollScale(mScrollFocusAllowed);
        }

        public void SetSilenced(bool silenced)
        {
            if (mSilenced == silenced)
            {
                return;
            }

            mSilenced = silenced;
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = silenced ? 0.55f : 1f;
            ApplyInteractable();
        }

        /// <summary>
        /// 设置子 Canvas 渲染层级：中心最高，向左右递减（数值越大越在上层）。
        /// </summary>
        public void SetVisualLayerRank(int layerRank)
        {
            EnsureCardCanvas();
            if (mCardCanvas == null)
            {
                return;
            }

            mCardCanvas.sortingOrder = layerRank;
        }

        /// <summary>
        /// 仅视口内正常大小的卡牌允许点击；边缘缩小的卡牌不可点。
        /// </summary>
        public void SetScrollFocusAllowed(bool scrollFocusAllowed)
        {
            var focusChanged = mScrollFocusAllowed != scrollFocusAllowed;
            mScrollFocusAllowed = scrollFocusAllowed;
            ApplyInteractable();
            if (focusChanged)
            {
                ApplyPressAnimatorForScrollScale(scrollFocusAllowed);
            }
        }

        private void ApplyInteractable()
        {
            var canClick = mManualOperateAllowed && mScrollFocusAllowed && !mSilenced;

            if (mClickButton != null)
            {
                mClickButton.interactable = canClick;
            }
        }

        /// <summary>
        /// 边缘缩小卡由 ScrollView 控制 Canvas 缩放，关闭父节点 Animator，避免与展示缩放冲突。
        /// </summary>
        private void ApplyPressAnimatorForScrollScale(bool isNormalDisplaySize)
        {
            if (mAutoReleaseAnimatorOverridden)
            {
                return;
            }

            if (Global.AutoReleaseSkillState.Value)
            {
                SetPressAnimatorEnabled(false);
                return;
            }

            SetPressAnimatorEnabled(isNormalDisplaySize);
        }

        private void OnCardClicked()
        {
            if (mSkillId <= 0 || !mManualOperateAllowed || !mScrollFocusAllowed)
            {
                return;
            }

            GameLand.Default?.ClickSkillCard(mSkillId, gameObject);
        }

        public void UpdateCooldownUI(float remainTime, float totalTime)
        {
            var isCooling = totalTime > 0f && remainTime > 0f;
            if (isCooling && !mWasCooling && Global.AutoReleaseSkillState.Value)
            {
                PlayAutoReleaseScaleTween();
            }

            mWasCooling = isCooling;
            var cooling = transform.Find("Canvas/cooling")?.GetComponent<Image>();
            if (cooling != null)
            {
                cooling.gameObject.SetActive(isCooling);
                cooling.fillAmount = isCooling ? Mathf.Clamp01(remainTime / totalTime) : 0f;
            }

            SetCooldownTimeVisible(isCooling);
            if (!isCooling)
            {
                return;
            }

            var second = transform.Find("Canvas/Time/Second")?.GetComponent<TMP_Text>();
            var millisecond = transform.Find("Canvas/Time/Millisecond")?.GetComponent<TMP_Text>();
            if (second != null)
            {
                second.text = Mathf.FloorToInt(remainTime).ToString();
            }

            if (millisecond != null)
            {
                millisecond.text = $".{(Mathf.FloorToInt(remainTime * 10) % 10)}";
            }
        }

        public void SetCooldownReady()
        {
            UpdateCooldownUI(0f, 0f);
        }

        private void SetCooldownTimeVisible(bool visible)
        {
            var second = transform.Find("Canvas/Time/Second");
            var millisecond = transform.Find("Canvas/Time/Millisecond");
            if (second != null)
            {
                second.gameObject.SetActive(visible);
            }

            if (millisecond != null)
            {
                millisecond.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 子 Canvas 仅作表现，关闭其下所有 Graphic 的射线检测。
        /// </summary>
        private void DisableChildRaycastTargets()
        {
            var childCanvas = transform.Find("Canvas");
            if (childCanvas == null)
            {
                return;
            }

            var graphics = childCanvas.GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics)
            {
                graphic.raycastTarget = false;
            }
        }

        private void EnsureCardCanvas()
        {
            if (mCardCanvas != null)
            {
                return;
            }

            var scaleTarget = ScaleTarget;
            if (scaleTarget != null)
            {
                mCardCanvas = scaleTarget.GetComponent<Canvas>();
            }

            if (mCardCanvas == null)
            {
                LogTool.LogError("[BattleSkillCardScrollItem] 子节点 Canvas 缺少 Canvas 组件，请在预制体上挂载");
            }
        }
    }
}
