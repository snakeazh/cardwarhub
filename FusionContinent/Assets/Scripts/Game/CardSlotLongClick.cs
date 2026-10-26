using System;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using QFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum DragState
{
    GamePlay,
    GameMenu,
    DragFalse,
    DragTrue
}

public class CardSlotLongClick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public UnityEvent onTap;
    public UnityEvent onLongPress;
    public UnityEvent onLongPressUp;
    public UnityEvent onDragPressDown;
    public UnityEvent onDragPressup;
    public bool onPressUpButtonEnabled = true;
    
    private bool isPointerDown = false;
    private float pointerDownTimer = 0f;

    private Vector3 Targetmovepos;
    public Vector3 Targetpos;
    public float rangeThreshold = 100.0f;
    private bool Islong;
    public Vector2 lastpos;
    public Vector3 lastwordpos;
    public float longPressDuration = 0.5f;
    public SoldierCard solider;
    public bool GameWaitOver;
    public bool GameDrag;
    public int cardId;

    private bool isdrag;
    public DragState dragState = DragState.DragFalse;
    public bool IsDragging { get; private set; }
    private Vector3 originalPosition;

    [SerializeField] private Transform cardParent;
    [SerializeField] private Transform dragLayer;
    [SerializeField] private float selectedRaiseYOffset = 80f;

    private int siblingIndex;
    private RectTransform rect;
    private Button button;
    private Canvas dragRenderCanvas;
    private bool hasOriginalRenderCanvas;
    private bool originalOverrideSorting;
    private int originalSortingOrder;
    private Transform cardCanvas;
    private Vector3 cardCanvasOriginalLocalPos;
    private bool hasCardCanvas;
    private Transform cardEffectRoot;
    private Vector3 cardEffectRootOriginalLocalPos;
    private bool hasCardEffectRoot;
    private Transform cardSharkRoot;
    private Vector3 cardSharkRootOriginalLocalPos;
    private Vector3 cardSharkRootOriginalLocalScale;
    private bool hasCardSharkRoot;
    private Transform selectedImageRoot;
    private bool isSelectedRaised;
    private const int DragTopSortingOrder = 5000;
    private Vector2 originalAnchoredPos;
    private Vector3 attackPointpos;
    public Vector2 attackSize;
    public IndicatorType indicatorType;
    public Sprite attackPointIcon;
    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        dragRenderCanvas = GetComponent<Canvas>();
        if (dragRenderCanvas != null)
        {
            hasOriginalRenderCanvas = true;
            originalOverrideSorting = dragRenderCanvas.overrideSorting;
            originalSortingOrder = dragRenderCanvas.sortingOrder;
        }
        Targetpos = attackPointpos;
        originalAnchoredPos = rect.anchoredPosition;
        originalPosition = transform.position;

        cardParent = this.transform.parent;
        dragLayer = this.transform.parent;
        cardCanvas = transform.Find("Canvas");
        if (cardCanvas != null)
        {
            hasCardCanvas = true;
            cardCanvasOriginalLocalPos = cardCanvas.localPosition;
        }
        cardEffectRoot = transform.Find("Effect");
        if (cardEffectRoot != null)
        {
            hasCardEffectRoot = true;
            cardEffectRootOriginalLocalPos = cardEffectRoot.localPosition;
        }
        cardSharkRoot = transform.Find("shark");
        if (cardSharkRoot != null)
        {
            hasCardSharkRoot = true;
            cardSharkRootOriginalLocalPos = cardSharkRoot.localPosition;
            cardSharkRootOriginalLocalScale = cardSharkRoot.localScale;
        }
        selectedImageRoot = transform.Find("Image");
    }

    private void Update()
    {
        if (isPointerDown)
        {
            pointerDownTimer += Time.deltaTime;
            Vector2 mousePosition = Input.mousePosition;
            float distance = Vector2.Distance(Targetmovepos, mousePosition);

            if (distance > rangeThreshold)
            {
                Reset();
            }
            else
            {
                if (pointerDownTimer >= longPressDuration && !ShouldBlockDrag() && (button == null || button.interactable))
                {
                    Islong = true;
                    suppressNextClick = true;
                    suppressReason = "longPress";
                    LogTool.Log($"[SkillClick] 设置点击抑制: cardId={cardId}, reason={suppressReason}");
                    onLongPress.Invoke();
                    Reset();
                }
            }
        }
    }

    private bool ShouldBlockDrag()
    {
        if (cardId == 0) return false;

        if (GameLand.Default != null && GameLand.Default.skill.TryGetValue(cardId, out var skillInfo))
        {
            // 有充能次数时可以拖拽
            if (skillInfo.ChargingNum > 0) return false;

            // 正在冷却且无充能时禁止拖拽
            return skillInfo.Cool;
        }

        // skill字典中不存在该技能,视为未冷却(允许拖拽)
        return false;
    }

    private bool IsCoolingUiBlocked()
    {
        var cooling = transform.Find("Canvas/cooling");
        if (cooling == null || !cooling.gameObject.activeInHierarchy) return false;
        var image = cooling.GetComponent<Image>();
        return image != null && image.fillAmount > 0f;
    }

    public void SetSolidercard(SoldierCard soldier)
    {
        this.solider = soldier;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (ShouldBlockDrag() || IsCoolingUiBlocked()) return;
        LogTool.Log($"[SkillClick] PointerDown: cardId={cardId}, dragState={dragState}");

        siblingIndex = transform.GetSiblingIndex();
        Targetmovepos = Input.mousePosition;
        originalAnchoredPos = rect.anchoredPosition;
        originalPosition = transform.position;
        isPointerDown = true;
        isdrag = true;
        Islong = false;
        pointerDownTimer = 0f;
        if (dragState == DragState.GamePlay)
        {
            SetSelectedRaised(true);
            SetSelectedEffectVisible(true);
            transform.Find("Effect").localScale = Vector3.one;
            transform.Find("shark").localScale = Vector3.one;
            transform.Find("Canvas/AttackPoint").GetComponent<RectTransform>().sizeDelta = attackSize;
            if (indicatorType != IndicatorType.CircularRange)
            {
                transform.Find("Canvas/AttackPoint").localScale = Vector3.zero;
            }

            transform.Find("Canvas/AttackPoint").localScale = Vector3.zero;
            RaiseCardRenderOrder();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetSelectedRaised(false);
        SetSelectedEffectVisible(false);
        LogTool.Log($"[SkillClick] PointerUp: cardId={cardId}, didDrag={didDrag}, dragState={dragState}");
        var isBlocked = ShouldBlockDrag() || IsCoolingUiBlocked();
        // 自动战斗时 dragState 为 DragFalse，仍允许点按未 CD 技能以取消自动
        var isTap = !didDrag && !Islong && !isBlocked &&
                    (dragState == DragState.GamePlay || Global.AutoReleaseSkillState.Value);
        onLongPressUp.Invoke();
        if (isTap)
        {
            LogTool.Log($"[SkillClick] Tap触发: cardId={cardId}");
            onTap?.Invoke();
        }
        if (didDrag && dragState != DragState.DragFalse && !isBlocked)
        {
            suppressNextClick = true;
            suppressReason = "dragRelease";
            LogTool.Log($"[SkillClick] 设置点击抑制: cardId={cardId}, reason={suppressReason}");
            //transform.localScale = Vector3.one;
            if (dragState == DragState.GamePlay)
            {
                //rect = transform.Find("Canvas/AttackPoint").GetComponent<RectTransform>();
        
                transform.Find("Effect").localScale = Vector3.zero;
                transform.Find("shark").localScale = Vector3.zero;
                transform.Find("Canvas/AttackPoint").localScale = Vector3.zero;
            }

            lastpos = rect.anchoredPosition;
            lastwordpos = this.transform.position;
            Targetpos = this.transform.position;
            //onDragPressup.Invoke();
            DOTween.To(v => { }, 0, 0, 0.1f).SetId(GlobalConst.MonsterTween).onComplete += () =>
            {
                onDragPressup.Invoke();
                RestoreCardSiblingOrder();
                RefreshCardParentLayout();
                //ClickAnim(attackPointpos);
            };
        }
        else if (isBlocked && (IsDragging || didDrag))
        {

            ForceResetPosition();
        }

        RestoreCardRenderOrder();
        Reset();
    }

    private void RaiseCardRenderOrder()
    {
        if (dragRenderCanvas == null)
        {
            // 根节点无 Canvas 时，拖拽开始时临时添加一个，仅用于渲染提权。
            dragRenderCanvas = gameObject.AddComponent<Canvas>();
            hasOriginalRenderCanvas = false;
        }

        dragRenderCanvas.overrideSorting = true;
        // 拖拽中显示到最上层，但不改 sibling 和布局顺序。
        dragRenderCanvas.sortingOrder = DragTopSortingOrder;
    }

    private void RestoreCardRenderOrder()
    {
        if (dragRenderCanvas == null) return;

        if (hasOriginalRenderCanvas)
        {
            dragRenderCanvas.overrideSorting = originalOverrideSorting;
            dragRenderCanvas.sortingOrder = originalSortingOrder;
            return;
        }

        Destroy(dragRenderCanvas);
        dragRenderCanvas = null;
    }

    private void Reset()
    {
        isPointerDown = false;
        pointerDownTimer = 0f;
        didDrag = false;
        IsDragging = false;
    }

    private bool didDrag = false;

    private Tween clickTween;
    private bool Isclick;
    // 标记“下一次 Button.onClick 需要被吞掉”，用于防止拖拽/长按后重复触发点击释放。
    private bool suppressNextClick;
    private string suppressReason = string.Empty;

    public bool ConsumeClickSuppression()
    {
        // 只消费一次，避免把后续真实点击也拦掉。
        if (!suppressNextClick) return false;
        LogTool.LogWarning($"[SkillClick] 消费点击抑制: cardId={cardId}, reason={suppressReason}");
        suppressNextClick = false;
        suppressReason = string.Empty;
        return true;
    }

    public Tween ClickEffect(Vector3 pos, float duration = 0.5f, TweenCallback onFinished = null,TweenCallback OnStartCall = null)
    {
        SetSelectedRaised(false);
        if (clickTween != null)
            clickTween.Kill();
        // 外部回调
        OnStartCall?.Invoke();
        Isclick = true;
        var selectedFx = transform.Find("Effect/LeaderActionEffect02");
        if (selectedFx != null)
        {
            // 点击释放时隐藏蓝色选中态，避免跟随飞牌造成视觉干扰
            selectedFx.gameObject.SetActive(false);
        }
        transform.Find("shark").localScale = Vector3.one;
        var safeDuration = Mathf.Max(0.48f, duration * 1.5f);
        var flyOutDuration = safeDuration * 0.5f;
        var flyOutScaleDuration = safeDuration * 0.2f;
        var flyBackDuration = flyOutDuration * 0.5f;
        var flyBackScaleDuration = flyOutDuration * 0.2f;

        // 需求调整：直接移动整张卡牌（cardInfo 根节点），不再只移动内部视觉层。
        var startWorldPos = transform.position;
        var startScale = transform.localScale;
        var targetWorldPos = pos;
        targetWorldPos.z = startWorldPos.z;
        var effectTransform = transform.Find("Effect");
        var sharkTransform = transform.Find("shark");

        clickTween = DOTween.Sequence()
            .Append(transform.DOMove(targetWorldPos, flyOutDuration))
            .Append(transform.DOScale(startScale * 0.1f, flyOutScaleDuration))
            .AppendCallback(() => { onFinished?.Invoke(); })
            .Append(transform.DOMove(startWorldPos, flyBackDuration).SetEase(Ease.InCubic))
            .Append(transform.DOScale(startScale, flyBackScaleDuration))
            .OnComplete(() =>
            {
                Isclick = false;
                transform.position = startWorldPos;
                transform.localScale = startScale;
                if (effectTransform != null) effectTransform.localScale = Vector3.zero;
                if (sharkTransform != null) sharkTransform.localScale = Vector3.zero;
                RestoreCardSiblingOrder();
                RefreshCardParentLayout();
            })
            .OnKill(() =>
            {
                Isclick = false;
                transform.position = startWorldPos;
                transform.localScale = startScale;
                if (effectTransform != null) effectTransform.localScale = Vector3.zero;
                if (sharkTransform != null) sharkTransform.localScale = Vector3.zero;
                RestoreCardSiblingOrder();
                RefreshCardParentLayout();
            }).SetId(GlobalConst.MonsterTween);

        return clickTween;
    }



    public void OnDrag(PointerEventData eventData)
    {
        if (ShouldBlockDrag())
        {
            ForceResetPosition();
            return;
        }

        if (dragState != DragState.GamePlay) return;

        if (dragState != DragState.DragFalse && !isPointerDown)
        {
            if (isdrag)
            {
                onDragPressDown.Invoke();
                isdrag = false;
                transform.SetParent(dragLayer, true);
                IsDragging = true;
            }

            bool shouldReturn = false;
            Transform chargingNum = this.transform.Find("Canvas/ChargingNum");
            Transform cooling = this.transform.Find("Canvas/cooling");

            if (chargingNum.gameObject.activeSelf)
            {
                string[] chargeParts = chargingNum.GetComponent<Text>().text.Split('/');
                if (chargeParts.Length > 0 && int.Parse(chargeParts[0]) == 0)
                {
                    shouldReturn = true;
                }
            }
            else if (cooling != null && cooling.GetComponent<Image>().fillAmount != 0)
            {
                shouldReturn = true;
            }

            if (shouldReturn) return;

            didDrag = true;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint);

            rect.anchoredPosition = localPoint;

            if (dragState == DragState.GamePlay)
            {
                Vector3 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                newPosition.z = 0;

                transform.position = newPosition;

                Transform attackPoint = transform.Find("Canvas/AttackPoint");
                attackPoint.position = newPosition;
                attackPoint.localScale = Vector3.one;
                attackPointpos = newPosition;
                attackPoint.GetComponent<Image>().color =
                    attackPoint.GetComponent<RectTransform>().anchoredPosition.y < 300 ? Color.red : Color.white;
                if (GameWaitOver && solider != null)
                {
                    Transform indicator = solider.transform.Find("indicator");
                    if (indicator != null)
                    {
                        Vector2 direction = attackPointpos - indicator.transform.position;
                        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                        indicator.rotation = Quaternion.Euler(0, 0, angle - 90);
                        
                        float distance = (direction.magnitude * 2.5f + 4.5f) / (solider.transform.localScale.x * 1.5f);
                        indicator.localScale = new Vector3(indicator.localScale.x, distance, indicator.localScale.z);

                        indicator.GetChild(0).GetComponent<Image>().color =
                            attackPoint.GetComponent<RectTransform>().anchoredPosition.y < 300 ? Color.red : Color.white;
                    }
                }
            }
        }
    }

    public void ForceResetPosition()
    {
        SetSelectedRaised(false);
        SetSelectedEffectVisible(false);
        transform.SetParent(cardParent, false);
        transform.SetSiblingIndex(siblingIndex);
        RestoreCardRenderOrder();
        // 技能卡重置时回到默认展示缩放，避免点击后越点越小。
        transform.localScale = Vector3.one * GlobalConst.SKILL_CARD_DEFAULT_SCALE;
        // 不强行回写旧坐标，交给 Layout 重新排位，避免新卡插入后的旧坐标污染。
        RefreshCardParentLayout();

        if (solider != null)
        {
            Transform indicator = solider.transform.Find("indicator");
            if (indicator != null)
            {
                indicator.localScale = Vector3.zero;
            }
        }

        IsDragging = false;
        didDrag = false;
    }

    private void RefreshCardParentLayout()
    {
        if (cardParent == null) return;

        var fanLayout = cardParent.GetComponent<CardFanLayout>();
        if (fanLayout != null)
        {
            var cards = new List<RectTransform>();
            for (int i = 0; i < cardParent.childCount; i++)
            {
                var child = cardParent.GetChild(i);
                if (!child.gameObject.activeSelf) continue;

                var rect = child as RectTransform;
                if (rect != null)
                {
                    cards.Add(rect);
                }
            }

            fanLayout.Refresh(cards);
            return;
        }

        var parentRect = cardParent as RectTransform;
        if (parentRect == null) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        UIHelper.SafeRefreshLayout(parentRect);
    }

    private void RestoreCardSiblingOrder()
    {
        if (cardParent == null) return;

        transform.SetParent(cardParent, false);
        transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, Math.Max(0, cardParent.childCount - 1)));
    }

    private void SetSelectedRaised(bool isRaised)
    {
        if (isRaised == isSelectedRaised) return;

        isSelectedRaised = isRaised;
        var yOffset = isRaised ? selectedRaiseYOffset : 0f;
        if (hasCardCanvas)
        {
            cardCanvas.localPosition = cardCanvasOriginalLocalPos + new Vector3(0f, yOffset, 0f);
        }

        if (hasCardEffectRoot)
        {
            cardEffectRoot.localPosition = cardEffectRootOriginalLocalPos + new Vector3(0f, yOffset, 0f);
        }

        if (hasCardSharkRoot)
        {
            cardSharkRoot.localPosition = cardSharkRootOriginalLocalPos + new Vector3(0f, yOffset, 0f);
        }
    }

    private void SetSelectedEffectVisible(bool isVisible)
    {
        var selectedFx = transform.Find("Effect/LeaderActionEffect02");
        if (selectedFx != null)
        {
            selectedFx.gameObject.SetActive(isVisible);
        }

        if (selectedImageRoot != null)
        {
            selectedImageRoot.gameObject.SetActive(isVisible);
        }

        if (hasCardSharkRoot)
        {
            cardSharkRoot.localScale = isVisible ? cardSharkRootOriginalLocalScale : Vector3.zero;
        }
    }

}