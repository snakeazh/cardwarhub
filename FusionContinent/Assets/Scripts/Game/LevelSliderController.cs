using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Events;

public class LevelSliderController : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("Scroll View Setup")] public ScrollRect scrollRect;
    public RectTransform content;
    public RectTransform viewport;
    public RectTransform centerAnchor; // 场景中的中心点锚

    [Header("Card Setup")] public GameObject cardPrefab; // 卡片预制体
    public List<Sprite> levelSprites; // 关卡图片数组
    public List<int> cardid;

    [Header("Scaling")] public float scaleMax = 1.0f;
    public float scaleMin = 0.8f;
    public float scaleRange = 300f;

    [Header("Snapping")] public float snapSpeed = 10f;

    private List<RectTransform> cards = new List<RectTransform>();
    private bool dragging = false;
    private float targetPos;
    private bool needSnap = false;

    public bool IsPlayerUpGrade;
    public bool IsTalent;

    public bool IsChooseClick = false;

    private float swipeVelocityThreshold = 5000; // 快速滑动临界值
    private float swipeDistanceThreshold = 100f; // 轻微滑动距离
    private Vector2 dragStartPos;
    
    private LevelCard previousCenteredCard;
    private Coroutine recordPreviousCardCoroutine;
    
    private int dragStartCardIndex = -1;


    // 定义事件类型
    [System.Serializable]
    public class CardCenterChangedEvent : UnityEvent<LevelCard>
    {
    }

    [System.Serializable]
    public class CardClickedEvent : UnityEvent<LevelCard>
    {
    }

// 事件字段
    public CardCenterChangedEvent onCenterChanged;
    public CardClickedEvent onCardClicked;

    private bool IsInit = false;

    public void SetCardId(List<int> cardId, List<Sprite> levelsprites = null)
    {
        cardid = cardId;
        if (levelsprites == null) return;
        levelSprites = levelsprites;
    }

    private void Awake()
    {
        levelSprites.Clear();
        // 注意要从后向前删除，因为删除会改变子物体列表
        for (int i = content.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(content.transform.GetChild(i).gameObject);
        }
    }


    void Start()
    {
        //GenerateCards();
        scrollRect.inertia = false;
        scrollRect.decelerationRate = 0;
    }

    public void RefreshCards()
    {
        if (cards.Count == 0) return;

        for (int i = 0; i < cards.Count; i++)
        {
            LevelCard lc = cards[i].GetComponent<LevelCard>();
            Sprite sp = (i < levelSprites.Count) ? levelSprites[i] : null;
            lc.SetLevel(i, sp, this, cardid[i]); // 传入ScrollSnap引用
        }
    }

    public void GenerateCards()
    {
        float cardWidth = ((RectTransform)cardPrefab.transform).sizeDelta.x;
        cards.Clear();
        // ➕ 添加前置占位（左边）
        GameObject leftPadding = new GameObject("LeftPadding", typeof(RectTransform));
        RectTransform leftRT = leftPadding.GetComponent<RectTransform>();
        leftRT.SetParent(content, false);
        leftRT.sizeDelta = new Vector2(viewport.rect.width / 2f - cardWidth / 2f, 0); // 让第一张居中
        if (IsTalent)
        {
            GameObject leftPadding2 = new GameObject("LeftPadding2", typeof(RectTransform));
            RectTransform leftRT2 = leftPadding2.GetComponent<RectTransform>();
            leftRT2.SetParent(content, false);
            leftRT2.sizeDelta = new Vector2(viewport.rect.width / 2f - cardWidth / 2f, 0);
        }

        // ➕ 添加卡片
        for (int i = 0; i < levelSprites.Count; i++)
        {
            GameObject obj = Instantiate(cardPrefab, content);
            RectTransform cardRT = obj.GetComponent<RectTransform>();
            cards.Add(cardRT);

            LevelCard lc = obj.GetComponent<LevelCard>();
            Sprite sp = (i < levelSprites.Count) ? levelSprites[i] : null;
            if (lc != null)
            {
                if (IsPlayerUpGrade||IsTalent)
                {
                    lc.SetLevel(i, sp, this, cardid[i]); // 传入ScrollSnap引用
                }
                else
                {
                    lc.SetLevel(i, sp, this); // 传入ScrollSnap引用
                }
            }
        }

        // ➕ 添加后置占位（右边）
        GameObject rightPadding = new GameObject("RightPadding", typeof(RectTransform));
        RectTransform rightRT = rightPadding.GetComponent<RectTransform>();
        rightRT.SetParent(content, false);
        rightRT.sizeDelta = new Vector2(viewport.rect.width / 2f - cardWidth / 2f, 0);
        if (IsTalent||IsPlayerUpGrade)
        {
            GameObject rightPadding2 = new GameObject("RightPadding2", typeof(RectTransform));
            RectTransform rightRT2 = rightPadding2.GetComponent<RectTransform>();
            rightRT2.SetParent(content, false);
            rightRT2.sizeDelta = new Vector2(viewport.rect.width / 2f - cardWidth / 2f, 0);
        }

        IsInit = true;
    }

    private LevelCard currentCenteredCard;

    void Update()
    {
        if (!IsInit) return;

        UpdateCardScales(); // 缩放效果可以保留，增加视觉反馈

        if (!dragging)
        {
            UpdateCenteredCardTween(); // 🟡 仅在非拖动时才判断中心卡片变化
        }

        if (needSnap && !dragging)
        {
            float current = scrollRect.horizontalNormalizedPosition;
            float next = Mathf.Lerp(current, targetPos, Time.deltaTime * snapSpeed);
            scrollRect.horizontalNormalizedPosition = next;

            if (Mathf.Abs(next - targetPos) < 0.001f)
                needSnap = false;
        }
    }

    /// <summary>
    /// 根据关卡ID滑动并居中对应卡片
    /// </summary>
    public void SnapToCardById(int id)
    {
        if (id < 0 || id >= cards.Count)
        {
            LogTool.LogWarning($"SnapToCardById: id {id} 超出范围");
            return;
        }

        LevelCard card = cards[id].GetComponent<LevelCard>();
        if (card != null)
        {
            IsChooseClick = false;
            SnapToCard(card);
        }
    }

    public void SnapToCardAfterLayout(int id)
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        StartCoroutine(DelaySnapToCardStable(id));
    }

    private IEnumerator DelaySnapToCardStable(int id)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content); // 强制布局完成
        UIHelper.SafeRefreshLayout(content);
        yield return null; // 第3帧：确保所有UI更新同步完成

        SnapToCardById(id); // 此时设置不会被重置
    }

    public void SnapToCardById2(int id)
    {
        if (id < 0 || id >= cards.Count)
        {
            LogTool.LogWarning($"SnapToCardById: id {id} 超出范围");
            return;
        }

        LevelCard card = cards[id].GetComponent<LevelCard>();
        if (card != null)
        {
            IsChooseClick = true;
            SnapToCard(card);
        }
    }

    /// <summary>
    /// 修正目标 content 的位置，使最左和最右的卡片也可以被居中
    /// </summary>
    private float ClampContentX(float targetX)
    {
        if (content.childCount < 2) return targetX;

        // 获取左右 padding 的宽度
        float leftPadding = content.GetChild(0).GetComponent<RectTransform>().rect.width;
        float rightPadding = content.GetChild(content.childCount - 1).GetComponent<RectTransform>().rect.width;

        float scrollableWidth = content.rect.width - viewport.rect.width;

        float minX = -scrollableWidth + leftPadding;
        float maxX = rightPadding;

        return Mathf.Clamp(targetX, minX, maxX);
    }

    void UpdateCenteredCardTween()
    {
        LevelCard centeredCard = GetCurrentCenteredCard();

        if (centeredCard != currentCenteredCard)
        {
            // 上一个卡片缩小
            if (currentCenteredCard != null)
            {
                currentCenteredCard.ScaleDown();
            }

            // 新的卡片放大
            if (centeredCard != null)
            {
                centeredCard.ScaleUp();
                onCenterChanged?.Invoke(centeredCard);
            }

            currentCenteredCard = centeredCard;

            // 启动延迟记录
            if (recordPreviousCardCoroutine != null)
            {
                StopCoroutine(recordPreviousCardCoroutine);
            }

            recordPreviousCardCoroutine = StartCoroutine(DelayRecordPreviousCard(centeredCard));
        }
    }
    private IEnumerator DelayRecordPreviousCard(LevelCard newCenteredCard)
    {
        yield return new WaitForSeconds(0.2f);

        previousCenteredCard = newCenteredCard;
        recordPreviousCardCoroutine = null;
    }

    
    public LevelCard GetPreviousCenteredCard()
    {
        return previousCenteredCard;
    }

    public void SnapToCard(LevelCard card)
    {
        RectTransform cardRT = card.GetComponent<RectTransform>();
        if (cardRT == null) return;

        float cardLocalX = content.InverseTransformPoint(cardRT.position).x;
        float centerLocalX = content.InverseTransformPoint(centerAnchor.position).x;

        float offset = centerLocalX - cardLocalX;
        Vector3 targetContentPos = content.localPosition + new Vector3(offset, 0, 0);

        targetContentPos.x = ClampContentX(targetContentPos.x);

        // ✅ 直接赋值，不抖动
        content.localPosition = targetContentPos;

        IsChooseClick = false;
        snapCoroutine = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        dragStartPos = scrollRect.content.anchoredPosition;

        var startCard = GetCurrentCenteredCard();
        if (startCard != null)
        {
            dragStartCardIndex = cards.IndexOf(startCard.GetComponent<RectTransform>());
        }
    }

    private bool isSnapping = false;
    

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;

        float dragDelta = scrollRect.content.anchoredPosition.x - dragStartPos.x;

        scrollRect.velocity = Vector2.zero;
        scrollRect.inertia = false;

        if (Mathf.Abs(dragDelta) > 0.01f)
        {
            // 👇 只根据方向移动一个卡片（不管速度或距离）
            int dir = dragDelta > 0 ? -1 : 1;
            SnapToNeighborCard(dir);
        }
        else
        {
            SnapToNearestCard(); // 几乎没有滑动时，吸附回去
        }

        StartCoroutine(RestoreInertiaAfterDelay(0.3f));
    }



    public void SnapToNeighborCard(int direction)
    {
        int targetIndex = dragStartCardIndex + direction;
        if (targetIndex >= 0 && targetIndex < cards.Count)
        {
            var target = cards[targetIndex];
            if (target != null && !target.Equals(null))
            {
                IsChooseClick = true;
                SnapToCard(target.GetComponent<LevelCard>());
            }
        }
        else
        {
            SnapToNearestCard(); // 滑到底时吸附回最近卡
        }
    }



    private IEnumerator DelaySnap()
    {
        yield return new WaitForEndOfFrame(); // 允许一帧惯性流动
        scrollRect.velocity = Vector2.zero;
        scrollRect.inertia = false;

        SnapToNearestCard();
        yield return new WaitForSeconds(0.3f);
        scrollRect.inertia = true;
    }

    private IEnumerator RestoreInertiaAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        scrollRect.inertia = true;
    }

    /// <summary>
    /// 获取当前距离中心点最近的卡片组件
    /// </summary>
    /// <returns>居中卡片的 LevelCard 脚本，找不到返回 null</returns>
    public LevelCard GetCurrentCenteredCard()
    {
        float closest = float.MaxValue;
        RectTransform closestCard = null;
        Vector3 centerPos = centerAnchor.position;

        foreach (RectTransform card in cards)
        {
            if (card == null || card.Equals(null)) continue; // 防止访问被销毁的对象

            float dist = Mathf.Abs(card.position.x - centerPos.x);
            if (dist < closest)
            {
                closest = dist;
                closestCard = card;
            }
        }

        if (closestCard != null)
        {
            return closestCard.GetComponent<LevelCard>();
        }

        return null;
    }

    public LevelCard GetNeighborCard(int offset)
    {
        var currentCard = GetCurrentCenteredCard();
        if (currentCard == null) return null;

        int index = cards.IndexOf(currentCard.GetComponent<RectTransform>());
        int newIndex = index + offset;
        if (newIndex >= 0 && newIndex < cards.Count)
        {
            var target = cards[newIndex];
            if (target == null || target.Equals(null)) return null; // 👈 防止访问已销毁对象

            return target.GetComponent<LevelCard>();
        }

        return null;
    }

    private Coroutine snapCoroutine;

    void SnapToNearestCard()
    {
        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
        }

        float contentWidth = content.rect.width - viewport.rect.width;

        float closest = float.MaxValue;
        RectTransform closestCard = null;

        Vector3 centerPos = centerAnchor.position;

        foreach (RectTransform card in cards)
        {
            if (card == null || card.Equals(null)) continue; // 防止访问已销毁对象

            float dist = Mathf.Abs(card.position.x - centerPos.x);
            if (dist < closest)
            {
                closest = dist;
                closestCard = card;
            }
        }

        if (closestCard != null)
        {
            float cardLocalX = content.InverseTransformPoint(closestCard.position).x;
            float centerLocalX = content.InverseTransformPoint(centerAnchor.position).x;

            float offset = centerLocalX - cardLocalX;

            Vector3 targetContentPos = content.localPosition + new Vector3(offset, 0, 0);

            // 限制滚动范围
            targetContentPos.x = ClampContentX(targetContentPos.x);

            //snapCoroutine = StartCoroutine(SmoothMoveContent(targetContentPos));
            content.localPosition = targetContentPos;
            snapCoroutine = null;
            var cardComponent = closestCard.GetComponent<LevelCard>();
            if (cardComponent != null)
            {
                LogTool.Log($"当前居中卡牌 ID：{cardComponent.levelId}");
            }
        }
    }

    private IEnumerator SmoothMoveContent(Vector3 targetPos)
    {
        // 👇 删除整个协程内容，避免造成跳跃感
        content.localPosition = targetPos;
        yield return null;
        snapCoroutine = null;
    }

    private IEnumerator SmoothMoveContent2(Vector3 targetPos)
    {
        float moveSpeed = 10000f;

        while (Vector3.Distance(content.localPosition, targetPos) > 0.01f)
        {
            content.localPosition = Vector3.MoveTowards(content.localPosition, targetPos, moveSpeed * Time.deltaTime);
            content.localPosition = targetPos;
            yield return null;
        }

        snapCoroutine = null;
        IsChooseClick = false;
    }


    void UpdateCardScales()
    {
        Vector3 centerWorld = viewport.TransformPoint(viewport.rect.center);

        foreach (RectTransform card in cards)
        {
            if (card == null || card.Equals(null)) continue; // 👈 防止访问已销毁对象

            float dist = Mathf.Abs(card.position.x - centerWorld.x);
            float t = Mathf.Clamp01(dist / scaleRange);
            float scale = Mathf.Lerp(scaleMax, scaleMin, t);
            card.localScale = Vector3.Lerp(card.localScale, Vector3.one * scale, Time.deltaTime * 10f);
        }
    }
}