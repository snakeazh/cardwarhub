using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum CellType
{
    Header,
    Cell,
}

public class HeadCellScrollView : MonoBehaviour
{
    public class CellData
    {
        public CellType CellType;
        public int DataIndex;
        public GameObject target;
        public float Height;
        public float PosY;
        public float PosX;
    }
    
    public class HeaderCellData : CellData
    {
        public int ChildCount;
    }
    
    // Config - defaults, can be overridden by data
    public float HeaderHeight = 60f;
    public float RowHeight = 250f;
    public int ItemsPerRow = 1;
    private List<CellData> _headCellDatas = new List<CellData>();
    public RectTransform Content;
    public RectTransform Viewport;
    public ScrollRect ScrollRect;
    
    public GameObject HeaderPrefab;
    public GameObject CellPrefab;
    
    public Action<CellData, GameObject> OnUpdateItem;
    
    private LinkedList<GameObject> _headerPool = new LinkedList<GameObject>();
    private LinkedList<GameObject> _cellPool = new LinkedList<GameObject>();

    private void Awake()
    {
        if (ScrollRect == null) ScrollRect = GetComponent<ScrollRect>();
        if (ScrollRect != null)
        {
            if (Content == null) Content = ScrollRect.content;
            if (Viewport == null) Viewport = ScrollRect.viewport;
        }
        
        if (Content != null)
        {
            Content.anchorMin = new Vector2(0.5f, 1);
            Content.anchorMax = new Vector2(0.5f, 1);
            Content.pivot = new Vector2(0.5f, 1);
            Content.anchoredPosition = Vector2.zero;
        }
    }

    public void BuildElement(int header, int rowCount)
    {
        _headCellDatas.Add(new HeaderCellData()
        {
            CellType = CellType.Header,
            DataIndex = _headCellDatas.Count,
            Height = HeaderHeight,
            ChildCount = rowCount
        });

        for (int i = 0; i < rowCount; i++)
        {
            _headCellDatas.Add(new CellData()
            {
                CellType = CellType.Cell,
                DataIndex = _headCellDatas.Count,
                Height = RowHeight
            });
        }
    }

    public void ShowList()
    {
        RecalculateLayout();
        UpdateVisibleItems(true);
    }
    
    public void Clear()
    {
        RecycleAll();
        _headCellDatas.Clear();
        if (Content != null) Content.sizeDelta = Vector2.zero;
    }
    
    private void RecalculateLayout()
    {
        float y = 0;
        int col = 0;
        float contentWidth = Content != null ? Content.rect.width : (Viewport != null ? Viewport.rect.width : 0);
        float cellWidth = (ItemsPerRow > 0) ? contentWidth / ItemsPerRow : contentWidth;

        for (int i = 0; i < _headCellDatas.Count; i++)
        {
            var item = _headCellDatas[i];
            
            // Use config height if not set
            if (item.Height <= 0)
            {
                item.Height = (item.CellType == CellType.Header) ? HeaderHeight : RowHeight;
            }

            if (item.CellType == CellType.Header)
            {
                // New header breaks the grid row
                if (col > 0)
                {
                    y += RowHeight; // Assuming previous items were cells
                    col = 0;
                }
                
                item.PosY = y;
                item.PosX = contentWidth * 0.5f; // Center
                y += item.Height;
            }
            else
            {
                item.PosY = y;
                item.PosX = col * cellWidth;
                
                col++;
                if (col >= ItemsPerRow)
                {
                    col = 0;
                    y += item.Height;
                }
            }
        }
        
        // Add height for partial last row
        if (col > 0)
        {
            y += RowHeight;
        }
        
        if (Content != null)
        {
            Content.sizeDelta = new Vector2(Content.sizeDelta.x, y);
        }
    }
    
    private void LateUpdate()
    {
        if (_headCellDatas.Count > 0 && Content != null && Viewport != null)
        {
            UpdateVisibleItems(false);
        }
    }

    private void UpdateVisibleItems(bool force)
    {
        float contentY = Content.anchoredPosition.y;
        float viewportH = Viewport.rect.height;
        
        float startY = Mathf.Max(0, contentY);
        float endY = startY + viewportH;
        
        // Expand buffer
        startY -= RowHeight; 
        endY += RowHeight;

        // 1. Recycle items out of view
        for (int i = 0; i < _headCellDatas.Count; i++)
        {
            var item = _headCellDatas[i];
            if (item.target != null)
            {
                float itemTop = item.PosY;
                float itemBottom = item.PosY + item.Height;
                
                if (itemBottom < startY || itemTop > endY)
                {
                    RecycleItem(item);
                }
            }
        }

        // 2. Spawn items in view
        int startIndex = FindStartIndex(startY);
        if (startIndex < 0) startIndex = 0;

        for (int i = startIndex; i < _headCellDatas.Count; i++)
        {
            var item = _headCellDatas[i];
            if (item.PosY > endY) break;

            if (item.PosY + item.Height > startY)
            {
                if (item.target == null)
                {
                    SpawnItem(item);
                }
                else if (force)
                {
                    OnUpdateItem?.Invoke(item, item.target);
                }
            }
        }
    }

    private int FindStartIndex(float y)
    {
        int low = 0;
        int high = _headCellDatas.Count - 1;
        int result = -1;
        
        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            var item = _headCellDatas[mid];
            
            if (item.PosY + item.Height < y)
            {
                low = mid + 1;
            }
            else if (item.PosY > y)
            {
                high = mid - 1;
            }
            else
            {
                result = mid;
                high = mid - 1; // Try to find earlier one
            }
        }
        return result != -1 ? result : low;
    }

    private void RecycleAll()
    {
        foreach (var item in _headCellDatas)
        {
            RecycleItem(item);
        }
    }

    private void RecycleItem(CellData item)
    {
        if (item.target == null) return;
        
        item.target.SetActive(false);
        if (item.CellType == CellType.Header)
            _headerPool.AddLast(item.target);
        else
            _cellPool.AddLast(item.target);
            
        item.target = null;
    }

    private void SpawnItem(CellData item)
    {
        GameObject go = null;
        if (item.CellType == CellType.Header)
        {
            if (_headerPool.Count > 0)
            {
                go = _headerPool.First.Value;
                _headerPool.RemoveFirst();
            }
            else if (HeaderPrefab != null)
            {
                go = Instantiate(HeaderPrefab, Content);
            }
        }
        else
        {
            if (_cellPool.Count > 0)
            {
                go = _cellPool.First.Value;
                _cellPool.RemoveFirst();
            }
            else if (CellPrefab != null)
            {
                go = Instantiate(CellPrefab, Content);
            }
        }

        if (go != null)
        {
            go.SetActive(true);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                if (item.CellType == CellType.Header)
                {
                    rt.pivot = new Vector2(0.5f, 1);
                }
                else
                {
                    rt.pivot = new Vector2(0, 1);
                }
                
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(item.PosX, -item.PosY);
                
                // float width = Content.rect.width;
                // if (ItemsPerRow > 0 && item.CellType == CellType.Cell)
                // {
                //     width /= ItemsPerRow;
                // }
                // rt.sizeDelta = new Vector2(width, item.Height);
            }
            item.target = go;
            OnUpdateItem?.Invoke(item, go);
        }
    }
}
