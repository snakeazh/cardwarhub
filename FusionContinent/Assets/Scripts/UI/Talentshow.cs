using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Helper;
using FusionContinent.Model;
using ScrollView;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class Talentshow : MonoBehaviour
{
    private TbTalentConfig _talentConfig;
    public ScrollItem ItemPrefab;
    public TalentDetails talentDetails;
    public ScrollRect talentScrollRect; // 在Inspector中拖拽赋值
    public RectTransform background; // 背景对象（在Inspector中拖拽赋值）
    public Vector2 ItemSpacing;


    public TalentLineSpace talentLineSpace;

    private int talentExtraLevel;
    private int talentLevel;

    private void Awake()
    {
        _talentConfig = DataConfigManager.Tables.TbTalentConfig;
    }


    public float topLimitOffset = 0; // 超过 viewport 顶部 100像素就禁止继续向上拖

    void Start()
    {
        talentDetails.TalentUpOver = ReSetTalentShow;
        _cellHeight = ItemSize.y;
        OnUpdateItem = OnUpdateItemFunc;
    }

    private GameObject guideTarget;
    private GameObject guideProTarget;
    
    private void OnUpdateItemFunc(int dataIndex, ScrollItem scrollItem)
    {
        var cfg = displayTalentListStruct[dataIndex];
        scrollItem.name = cfg.Level.ToString();
        var talentobj = scrollItem.gameObject;
        talentobj.GetComponent<TalentShowObj>().level = cfg.Level;
        talentobj.GetComponent<TalentShowObj>().unlockLv = displayTalentListStruct[dataIndex].UnlockLv;
        talentobj.GetComponent<TalentShowObj>().talentListConfig = displayTalentListStruct[dataIndex];
        if (displayTalentListStruct[dataIndex].ExtraAttributes.Count > 0 || displayTalentListStruct[dataIndex].ExtraMainAttributes.Count > 0)
        {
            talentobj.GetComponent<TalentShowObj>().IsExtra = false;
        }
        else
        {
            talentobj.GetComponent<TalentShowObj>().IsExtra = true;
        }

        var talentShowobj = talentobj.GetComponent<TalentShowObj>();
        talentShowobj.levelNum.text = talentShowobj.level.ToString();

        // 处理解锁状态
        bool isUnlocked = talentShowobj.unlockLv <= Global.User.Level;

        talentShowobj.levelframe[0].gameObject.SetActive(isUnlocked);
        talentShowobj.levelframe[1].gameObject.SetActive(!isUnlocked);
        bool isTalentUnlocked = talentShowobj.level > talentLevel;

        talentShowobj.talentButton.onClick.RemoveAllListeners();
        talentShowobj.talentButton.onClick.AddListener(() =>
        {
            UnlockGuideManager.Send(ModuleFunctionType.Talent,3);
            if (isTalentUnlocked)
            {
                if (talentShowobj.unlockLv > Global.User.Level)
                {
                    talentDetails.SetTalentDetails(talentid, talentShowobj.talentListConfig, false, 1);
                }
                else
                {
                    if (talentShowobj.level == talentLevel + 1)
                    {
                        bool money = true;
                        for (int j = 0; j < talentShowobj.talentListConfig.ConsumeItems.Count; j++)
                        {
                            long itemnum = Global.GetItemNumById(talentShowobj.talentListConfig.ConsumeItems[j].Id);
                            if (itemnum < talentShowobj.talentListConfig.ConsumeItems[j].Num)
                            {
                                money = false;
                            }
                        }

                        if (money)
                        {
                            talentDetails.SetTalentDetails(talentid, talentShowobj.talentListConfig, false, 0);
                        }
                        else
                        {
                            talentDetails.SetTalentDetails(talentid, talentShowobj.talentListConfig, false, 2);
                        }
                    }
                    else
                    {
                        talentDetails.SetTalentDetails(talentid, talentShowobj.talentListConfig, false, 1);
                    }
                }
            }
            else
            {
                talentDetails.SetTalentDetails(talentid, talentShowobj.talentListConfig, false, 3);
            }
        });
        SpriteHelper.TryLoadTextureAsync(talentShowobj.talentListConfig.Icon,talentShowobj.talentIcon, false, "UITalentIcon");
        talentShowobj.talentIcon.gameObject.SetActive(true);
        talentShowobj.upTip.SetActive(false);
        talentShowobj.talentframe.transform.GetChild(0).gameObject.SetActive(false);
        bool redNodeActive = false;
        if (!isTalentUnlocked)
        {
            talentShowobj.talentframe.sprite = Global.BigWorldIconAtlas.GetSprite("RewardBox");
            SpriteHelper.TryLoadTextureAsync("RewardBox2", talentShowobj.talentOuterFrame);
            talentShowobj.LevelFrame.gameObject.SetActive(true);
            talentShowobj.LevelFrameNo.gameObject.SetActive(false);
            talentShowobj.talentIcon.color = Color.white;
        }
        else
        {
            talentShowobj.talentframe.sprite = Global.BigWorldIconAtlas.GetSprite("LockedSmallRewardFrame");
            SpriteHelper.TryLoadTextureAsync("LockedSmallRewardFrame2", talentShowobj.talentOuterFrame);
            talentShowobj.LevelFrame.gameObject.SetActive(false);
            talentShowobj.LevelFrameNo.gameObject.SetActive(true);
            talentShowobj.talentIcon.color = new Color32(155, 155, 155, 255);
            if (talentShowobj.unlockLv <= Global.User.Level && talentShowobj.level == talentLevel + 1)
            {
                bool money = true;
                for (int j = 0; j < talentShowobj.talentListConfig.ConsumeItems.Count; j++)
                {
                    long itemnum = Global.GetItemNumById(talentShowobj.talentListConfig.ConsumeItems[j].Id);
                    if (itemnum < talentShowobj.talentListConfig.ConsumeItems[j].Num)
                    {
                        money = false;
                        break;
                    }
                }

                if (money)
                {
                    talentShowobj.talentframe.transform.GetChild(0).gameObject.SetActive(true);
                    SpriteHelper.TryLoadTextureAsync("RewardBox2", talentShowobj.talentOuterFrame);
                    redNodeActive = true;
                }
            }
        }

        if (redNodeActive)
        {
            guideTarget = talentShowobj.talentButton.transform.parent.gameObject;
        }
        talentShowobj.talentframe.rectTransform.SetLocalRedType(0);

        
        // // 更新当前点的位置和连线引用
        int talentExtraNextLevel = -1;
        if ((cfg.ExtraAttributes != null && cfg.ExtraAttributes.Count > 0) || (cfg.ExtraMainAttributes != null && cfg.ExtraMainAttributes.Count > 0))
        {
            talentExtraNextLevel = cfg.Level;
        }

        if (talentExtraNextLevel == -1)
        {
            talentShowobj.TalentExtraObj.SetActive(false);
        }
        else
        {
            bool redExtraCount = false;
            talentShowobj.TalentExtraObj.SetActive(true);
            redExtraCount = HandleExtraTalent(talentShowobj, cfg);
            talentShowobj.talentExtraframe.rectTransform.SetLocalRedType(0);
            if (redExtraCount)
            {
                guideProTarget = talentShowobj.talentExtraButton.transform.parent.gameObject;
            }
        }
        
        //
        // // 处理额外天赋
        // HandleExtraTalent(talentShowobj, talentExtraNextLevel);
        // if (i == _talentShowObjs.Count - 1)
        // {
        //     talentShowobj.line.gameObject.SetActive(false);
        //     talentShowobj.extraline.gameObject.SetActive(false);
        // }
    }

    public bool HandleExtraTalent(TalentShowObj talentShowobj, TalentListStruct cfg)
    {
        bool redExtraCount = false;
        // 不再看等级，只看是否已经点过
        bool isExtraUnlocked = talentShowobj.level <= talentExtraLevel;

        SpriteHelper.TryLoadTextureAsync(talentShowobj.talentListConfig.ExtraIcon,talentShowobj.talentExtraIcon, false, "UITalentIcon");
        talentShowobj.talentExtraIcon.gameObject.SetActive(true);
        talentShowobj.upExtraTip.SetActive(false);
        talentShowobj.talentExtraframe.transform.GetChild(0).gameObject.SetActive(false);
        if (isExtraUnlocked)
        {
            talentShowobj.talentExtraframe.sprite = Global.BigWorldIconAtlas.GetSprite("BigRewardFrame");
            SpriteHelper.TryLoadTextureAsync("BigRewardFrame2", talentShowobj.talentExtraOuterFrame);
            talentShowobj.talentExtraIcon.color = Color.white;
        }
        else
        {
            talentShowobj.talentExtraframe.sprite = Global.BigWorldIconAtlas.GetSprite("LockedBigRewardFrame");
            SpriteHelper.TryLoadTextureAsync("LockedBigRewardFrame2", talentShowobj.talentExtraOuterFrame);
            talentShowobj.talentExtraIcon.color = new Color32(155, 155, 155, 255);

            // 只判断上一个天赋学了，并且货币足够，就给升级提示
            if (talentShowobj.level == Guide_talentExtraNextLevel)
            {
                bool money = true;
                for (int j = 0; j < talentShowobj.talentListConfig.ExtraConsumeItems.Count; j++)
                {
                    long itemnum = Global.GetItemNumById(talentShowobj.talentListConfig.ExtraConsumeItems[j].Id);
                    if (itemnum < talentShowobj.talentListConfig.ExtraConsumeItems[j].Num)
                    {
                        money = false;
                        break;
                    }
                }

                if (money)
                {
                    talentShowobj.talentExtraframe.transform.GetChild(0).gameObject.SetActive(true);
                    SpriteHelper.TryLoadTextureAsync("BigRewardFrame2", talentShowobj.talentExtraOuterFrame);
                    redExtraCount = true;
                }
            }
        }
        talentShowobj.talentExtraButton.onClick.RemoveAllListeners();
        talentShowobj.talentExtraButton.onClick.AddListener(() =>
        {
            UnlockGuideManager.Send(ModuleFunctionType.Talent,5);
            if (!isExtraUnlocked)
            {
                // 不再卡等级，只看货币+前置
                // 上一个基础等级天赋没学
                if (talentLevel >= talentShowobj.level  )
                {
                    if (talentShowobj.level > unLockExtLevelId )
                    {
                        talentDetails.SetTalentDetails(talentid, talentShowobj.talentListConfig, true, 4); // 上一个没学
                    }
                    else
                    {
                        bool money = true;
                        for (int j = 0; j < talentShowobj.talentListConfig.ExtraConsumeItems.Count; j++)
                        {
                            long itemnum = Global.GetItemNumById(talentShowobj.talentListConfig.ExtraConsumeItems[j].Id);
                            if (itemnum < talentShowobj.talentListConfig.ExtraConsumeItems[j].Num)
                            {
                                money = false;
                            }
                        }

                        if (money)
                        {
                            talentDetails.SetTalentDetails(talentid, talentShowobj.talentListConfig, true, 0); // 可升级
                        }
                        else
                        {
                            talentDetails.SetTalentDetails(talentid, talentShowobj.talentListConfig, true, 2); // 货币不足
                        }
                    }
                   
                }
                else
                {
                    talentDetails.SetTalentDetails(talentid, talentShowobj.talentListConfig, true, 1); // 上一个没学
                    //基础的没学，但是特殊天赋学了
                    
                }
            }
            else
            {
                talentDetails.SetTalentDetails(talentid, talentShowobj.talentListConfig, true, 3); // 已解锁
            }
        });
        return redExtraCount;
    }

    private int Guide_talentExtraNextLevel;
    private List<TalentListStruct> displayTalentListStruct;
    private int talentid;
    private int lastExtLevel = 0;
    private int lastExtLevelIndex = 0;

    private int displayDefaultLineIndex;
    private int displayDefaultExtLineIndex;


    public void CalLastExt()
    {
        lastExtLevelIndex = CheckLastExtLevelIndex();
        lastExtLevel = displayTalentListStruct[lastExtLevelIndex].Level;

        int index = 0;
        //遍历所有数据 拿到小的要显示的
        for (int i = displayTalentListStruct.Count - 1; i >= 0; i--)
        {
            var data = displayTalentListStruct[i];
            ///已解锁
            if (data.Level > talentLevel)
            {
                index = i;
                break;
            }
        }

        displayDefaultLineIndex = index;
        int extIndex = displayTalentListStruct.Count-1;
        for (int i = displayTalentListStruct.Count - 1; i >= displayDefaultLineIndex; i--)
        {
            var data = displayTalentListStruct[i];
            ///未解锁的等级 并且是高级天赋
            if (data.ExtraAttributes.Count > 0 || data.ExtraMainAttributes.Count > 0)
            {
                //如果已解锁
                if (talentExtraLevel >= data.Level)
                {
                    extIndex = i;
                }
            }
        }
        for (int i = displayTalentListStruct.Count - 1; i >= displayDefaultLineIndex; i--)
        {
            var data = displayTalentListStruct[i];
            ///未解锁的等级 并且是高级天赋
            if (data.ExtraAttributes.Count > 0 || data.ExtraMainAttributes.Count > 0)
            {
                //如果未解锁并且小功能已解锁
                if (talentExtraLevel < data.Level && talentLevel >= data.Level)
                {
                    Guide_talentExtraNextLevel = data.Level;
                    break;
                }
            }
        }

        displayDefaultExtLineIndex = extIndex;
        CalLastExtLevelIndex();
        // LogTool.E(unLockExtLevelId);
        // displayTalentListStruct.Count - 1 - lastExtLevelIndex;
    }

    /// <summary>
    /// 显示天赋
    /// </summary>
    /// <param name="talentID"></param>
    public void SetTalentShow(int talentID)
    {
        guideTarget = null;
        guideProTarget = null;
        // CommonItemPoolManager.Instance.GetGameObjectPool("TalentShowObj").ReturnAllEffect();
        talentid = talentID;
        talentLevel = Global.PlayerTalents[talentID].Lv;
        talentExtraLevel = Global.PlayerTalents[talentID].ExtraLv;
        displayTalentListStruct = _talentConfig.GetOrDefault(talentID).TalentList.ToArray().ToList();
        displayTalentListStruct.Reverse();

        CalLastExt();

        ///-185   -220
        BuildTalentShowItems(displayTalentListStruct);
        background.sizeDelta = new Vector2(background.sizeDelta.x, maxBackgroundHeight);
        SetLineHeight(this.talentLineSpace.DefaultLine1Rect, tweenLine1Height);
        // SetLineHeight(this.talentLineSpace.DefaultLine2Rect, tweenLine2Height);

        ScrollToIndex(displayDefaultLineIndex + 1, ScrollAlignment.End, false);


        StartCoroutine(WaitGuide());
    }


    IEnumerator WaitProGuide()
    {
        yield return new WaitForSeconds(0.5f);

        if (guideProTarget == null)
        {
            UnlockGuideManager.Instance.SetErrorTypeStep(ModuleFunctionType.Talent,5);
        }
        else
        {
            UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Talent,5,guideProTarget.GetComponent<RectTransform>());
        }
    }

    IEnumerator WaitGuide()
    {
        yield return null;
        yield return null;
        if (guideTarget == null)
        {
            UnlockGuideManager.Instance.SetErrorTypeStep(ModuleFunctionType.Talent,3);
        }
        else
        {
            UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Talent,3,guideTarget.GetComponent<RectTransform>());
        }
        
    }

    public int CheckLastExtLevelIndex()
    {
        for (int i = 0; i < displayTalentListStruct.Count; i++)
        {
            if (displayTalentListStruct[i].ExtraAttributes.Count > 0 || displayTalentListStruct[i].ExtraMainAttributes.Count > 0)
            {
                return i;
            }
        }

        return 0;
    }

    private int unLockExtLevelId;
    
    public void CalLastExtLevelIndex()
    {
        unLockExtLevelId = -1;
        for (int i = displayTalentListStruct.Count - 1; i >= 0; i--)
        {
            if (displayTalentListStruct[i].ExtraAttributes.Count > 0 || displayTalentListStruct[i].ExtraMainAttributes.Count > 0 )
            {
                unLockExtLevelId =  displayTalentListStruct[i].Level;
                if (talentExtraLevel >= displayTalentListStruct[i].Level)
                {
                    continue;
                }
                
                
                break;
            }
        }
    }

    // Scroll to a specific data index with optional alignment and tween
    public void ScrollToIndex(int index, ScrollAlignment alignment = ScrollAlignment.Start, bool animated = true,
        float tweenDuration = 0.25f)
    {
        if (ContentRect == null || Viewport == null) return;
        if (DataCount <= 0) return;

        // Clamp index
        index = Mathf.Clamp(index, 0, Mathf.Max(0, DataCount - 1));

        // Grid support: items per line (use at least 1)
        int perLine = Mathf.Max(1, ItemsPerLine);
        int lineIndex = index / perLine; // which visual line contains this index

        // Sizes along scrolling axis
        float itemSize = ItemSize.y;
        float spacing = ItemSpacing.y;
        int lineCount = Mathf.CeilToInt((float)DataCount / perLine);
        float contentSize = lineCount * itemSize + Mathf.Max(0, lineCount - 1) * spacing;
        float viewportSize = Viewport.rect.height;
        float scrollable = Mathf.Max(0, contentSize - viewportSize);

        // Start position of the requested item
        float itemStart = lineIndex * (itemSize + spacing);
        LogTool.Log(itemStart);
        // Alignment offset
        float alignedStart = itemStart;
        switch (alignment)
        {
            case ScrollAlignment.Center:
                alignedStart = itemStart - (viewportSize - itemSize) * 0.5f;
                break;
            case ScrollAlignment.End:
                alignedStart = itemStart - (viewportSize - itemSize);
                break;
            case ScrollAlignment.Start:
            default:
                break;
        }

        // Clamp to scrollable range
        alignedStart = Mathf.Clamp(alignedStart, 0f, scrollable);


        // Unity verticalNormalizedPosition: 1 = top (offset 0), 0 = bottom (offset scrollable)
        float target = scrollable <= 0.0001f ? 1f : 1f - (alignedStart / scrollable);
        if (animated)
        {
            DOTween.Kill(talentScrollRect, false);
            DOTween.To(() => talentScrollRect.verticalNormalizedPosition,
                    v => talentScrollRect.verticalNormalizedPosition = v,
                    target, tweenDuration)
                .SetEase(Ease.Linear)
                .SetTarget(talentScrollRect);
        }
        else
        {
            talentScrollRect.verticalNormalizedPosition = target;
        }
    }


    /// <summary>
    /// 背景动哈跟随
    /// </summary>
    public void ReSetTalentShow(bool isGrade)
    {
        talentLevel = Global.PlayerTalents[talentid].Lv;
        talentExtraLevel = Global.PlayerTalents[talentid].ExtraLv;
        CalLastExt();
        float duration = 0.3f;
        BuildTalentShowItems(displayTalentListStruct);
        if (isGrade)
        {
            // this.talentLineSpace.DefaultLine2Rect.Find("line").GetComponent<RectTransform>().DOSizeDelta(new Vector2(this.talentLineSpace.DefaultLine2Rect.sizeDelta.x,
            //     tweenLine2Height),duration).SetEase(Ease.Linear);
        }
        else
        {
            var target = new Vector2(background.sizeDelta.x, maxBackgroundHeight);
            background.DOSizeDelta(target, duration, true).SetEase(Ease.Linear);
            this.talentLineSpace.DefaultLine1Rect.Find("line").GetComponent<RectTransform>().DOSizeDelta(new Vector2(this.talentLineSpace.DefaultLine1Rect.sizeDelta.x,
                tweenLine1Height),duration).SetEase(Ease.Linear);
        }

        if (UnlockGuideManager.Instance.CheckUnlockGuideRunning(ModuleFunctionType.Talent))
        {
            // ScrollToIndex(displayDefaultExtLineIndex + 1, ScrollAlignment.Start, false);
            StartCoroutine(WaitProGuide());
        }
    
        // ScrollToIndex(displayDefaultLineIndex+1,ScrollAlignment.End,true,duration);
    }

    public Vector2 ItemSize;

    public RectTransform ContentRect;

    public void ShowList(int count)
    {
        DataCount = Mathf.Max(0, count);
        EnsureContentSize(count);
        ForceRefreshVisible();
    }

    public int DataCount { get; set; }

    private int ItemsPerLine = 1;
    private int _totalRows;
    public RectTransform Viewport;

    private int _lastStartRow = -1;
    private int _lastEndRow = -1;
    private float _cellHeight;

    public class Item
    {
        public int Index;
        public ScrollItem ScrollItem;
        public GameObject Node;
    }

    private void EnsureContentSize(int count)
    {
        _totalRows = Mathf.CeilToInt((float)count / Mathf.Max(1, ItemsPerLine));

        var width = ItemsPerLine > 0
            ? ItemsPerLine * ItemSize.x + (ItemsPerLine - 1)
            : 0f;
        var height = _totalRows > 0
            ? _totalRows * ItemSize.y + (_totalRows - 1) * ItemSpacing.y
            : 0f;

        var size = ContentRect.sizeDelta;
        var viewportW = Viewport != null ? Viewport.rect.width : size.x;
        size.x = viewportW;
        size.y = height;
        ContentRect.sizeDelta = size;
        // ContentRect.anchoredPosition = new Vector2(-viewportW/2, ContentRect.anchoredPosition.y);
    }

    /// <summary>
    /// 刷新显示信息
    /// </summary>
    public delegate void UpdateItem(int dataIndex, ScrollItem scrollItem);

    public UpdateItem OnUpdateItem;

    /// <summary>
    /// display items
    /// </summary>
    protected LinkedList<Item> displayedItems = new LinkedList<Item>();

    protected readonly HashSet<int> _displayedIndexSet = new HashSet<int>();

    /// <summary>
    /// hide items
    /// </summary>
    protected LinkedList<Item> cachedItems = new LinkedList<Item>();

    private void ForceRefreshVisible()
    {
        _lastStartRow = -1;
        _lastEndRow = -1;
        UpdateVisibleItems(true);
    }

    private void UpdateVisibleItems(bool force)
    {
        // content moved up => anchoredPosition.y increases; clamp to >=0
        var scrollOffsetY = Mathf.Max(0f, ContentRect.anchoredPosition.y);
        var viewportHeight = Viewport.rect.height;

        var startRow = Mathf.FloorToInt(scrollOffsetY / _cellHeight);
        var endRow = Mathf.FloorToInt((scrollOffsetY + viewportHeight - 1f) / _cellHeight);

        // buffer 1 row to reduce flicker
        startRow = Mathf.Max(0, startRow - 1);
        endRow = Mathf.Min(_totalRows - 1, endRow + 1);

        if (!force && startRow == _lastStartRow && endRow == _lastEndRow) return;

        _lastStartRow = startRow;
        _lastEndRow = endRow;

        var needed = new HashSet<int>();
        for (int row = startRow; row <= endRow; row++)
        {
            for (int col = 0; col < ItemsPerLine; col++)
            {
                int index = row * ItemsPerLine + col;
                if (index >= DataCount) break;
                needed.Add(index);
            }
        }


        // recycle items not needed (do not call ReleaseRuntimeAction during normal virtualization)
        var node = displayedItems.First;
        while (node != null)
        {
            var next = node.Next;
            if (!needed.Contains(node.Value.Index))
            {
                node.Value.Node.SetActive(false);
                cachedItems.AddLast(node.Value);
                _displayedIndexSet.Remove(node.Value.Index);
                displayedItems.Remove(node);
            }
            else
            {
                if (force)
                {
                    if (OnUpdateItem != null && node.Value.ScrollItem != null)
                    {
                        OnUpdateItem.Invoke(node.Value.Index, node.Value.ScrollItem);
                    }
                }
            }

            node = next;
        }

        // add missing items
        foreach (var idx in needed)
        {
            if (_displayedIndexSet.Contains(idx)) continue;
            var item = AcquireItem();
            item.Index = idx;
            PositionItem(item, idx);
            displayedItems.AddLast(item);
            _displayedIndexSet.Add(idx);

            if (OnUpdateItem != null && item.ScrollItem != null)
            {
                OnUpdateItem.Invoke(idx, item.ScrollItem);
            }
        }
    }

    private void PositionItem(Item item, int dataIndex)
    {
        var rect = item.Node.GetComponent<RectTransform>();
        var row = Mathf.FloorToInt(dataIndex / Mathf.Max(1, ItemsPerLine));
        var col = dataIndex % Mathf.Max(1, ItemsPerLine);

        var x = col * (ItemSize.x);
        var y = -row * (ItemSize.y);
        // rect.sizeDelta = ItemSize;
        rect.anchoredPosition = new Vector2(x, y);
    }

    private Item AcquireItem()
    {
        // Always reuse from internal cache first, regardless of PrefabFactory/ReleaseRuntimeAction.
        Item item;
        if (cachedItems.Count > 0)
        {
            item = cachedItems.First.Value;
            cachedItems.RemoveFirst();
            item.Node.SetActive(true);
        }
        else
        {
            var go = Object.Instantiate(ItemPrefab.gameObject, ContentRect);

            var rect = go.GetComponent<RectTransform>();
            if (rect != null)
            {
                // rect.anchorMin = new Vector2(0, 1);
                // rect.anchorMax = new Vector2(0, 1);
                // rect.pivot = new Vector2(0, 1);
                // rect.sizeDelta = ItemSize;
            }

            // Activate in case prefab is inactive
            go.SetActive(true);

            item = new Item
            {
                ScrollItem = go.GetComponent<ScrollItem>(),
                Node = go
            };
        }

        return item;
    }

    float maxBackgroundHeight = 0;

    private float tweenLine1Height = 0;
    // private float tweenLine2Height = 0;

    private void BuildTalentShowItems(List<TalentListStruct> talentListStruct)
    {
        ShowList(talentListStruct.Count);
        var lineCount = lastExtLevelIndex;
        var height = lineCount * ItemSize.y + Mathf.Max(0, lineCount - 1) * ItemSpacing.y;
        // this.talentLineSpace.LineSpaceRect.
        this.talentLineSpace.DefaultLine2Rect.offsetMax =
            new Vector2(this.talentLineSpace.DefaultLine2Rect.offsetMax.x, -height);

        var displayCount = talentListStruct.Count - 1 - displayDefaultLineIndex;
        var startOffestY = -135f;
        float height1 = displayCount * ItemSize.y + Mathf.Max(0, displayCount - 1) * ItemSpacing.y + startOffestY;
        tweenLine1Height = Math.Max(0,height1 );
        // SetLineHeight(this.talentLineSpace.DefaultLine1Rect, height1 - ItemSize.y );
        maxBackgroundHeight = Math.Max(0,height1 );

        // var displayCount2 = talentListStruct.Count - 1 - displayDefaultExtLineIndex;
        // float height2 = displayCount2 * ItemSize.y + Mathf.Max(0, displayCount2 - 1) * ItemSpacing.y;
        // tweenLine2Height =  height2;
        // SetLineHeight(this.talentLineSpace.DefaultLine2Rect, height2);
        // displayDefaultExtLineIndex
        // background.sizeDelta = new Vector2(background.sizeDelta.x, maxBackgroundHeight);
    }

    private void Update()
    {
        // UpdateBackground();
    }

    void UpdateBackground()
    {
        if (background == null || Viewport == null || ContentRect == null) return;
        float posy = Math.Abs(ContentRect.anchoredPosition.y);
        float contentHeight = Math.Abs(ContentRect.sizeDelta.y);
        float bgHeight = background.sizeDelta.y;
        float topClamp = -Mathf.Max(0f, topLimitOffset);
        float bottomClamp = -bgHeight;
        float desired = contentHeight - posy - bgHeight - 185f;
        float newY = Mathf.Clamp(desired, bottomClamp, topClamp);
        background.anchoredPosition = new Vector2(background.anchoredPosition.x, newY);
    }

    void SetLineHeight(RectTransform rect, float height)
    {
        var lineRect = rect.Find("line").GetComponent<RectTransform>();
        lineRect.sizeDelta = new Vector2(lineRect.sizeDelta.x, height);
    }

    private void LateUpdate()
    {
        if (DataCount <= 0 || Viewport == null) return;
        UpdateVisibleItems(false);
        // UpdateBackground();
    }

    private void OnDisable()
    {
        // CommonItemPoolManager.Instance.GetGameObjectPool("TalentShowObj").ReturnAllEffect();
    }
}

// 返回数据
public class UpGradeTalentResult
{
    public PlayerTalent Talent { get; set; }
    public ItemGroup Items { get; set; } // 同步道具
}