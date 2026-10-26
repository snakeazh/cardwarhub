using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;


namespace FusionContinent.Helper
{
    public class LongPressDragItem : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler,
        IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [Header("配置")]
        public float longPressTime = 0.3f; // 长按触发时间
        public Canvas canvas;              // UI主Canvas
        public bool lockScrollWhileDragging = true; // 拖拽时锁定ScrollRect

        [Header("事件回调")]
        public Action<GameObject> OnLongPressStart; // 长按开始
        public Action<GameObject> OnDropTarget;     // 拖拽结束
        public Action<Vector2> OnDragUpdate;        // 拖拽中更新位置

        private bool isPressing;
        private bool isDragging;
        private float pressTimer;

        private GameObject dragCopy;
        private RectTransform dragCopyRect;
        private PointerEventData currentEventData;

        private ScrollRect parentScroll; // 缓存父级ScrollRect

        void Awake()
        {
            // 自动寻找 Canvas & ScrollRect
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
            parentScroll = GetComponentInParent<ScrollRect>();
        }

        void Update()
        {
            if (isPressing && !isDragging)
            {
                pressTimer += Time.deltaTime;
                if (pressTimer >= longPressTime)
                {
                    StartDrag();
                }
            }

            if (isDragging && dragCopyRect != null && currentEventData != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    currentEventData.position,
                    canvas.worldCamera,
                    out Vector2 pos);
                dragCopyRect.anchoredPosition = pos;
                OnDragUpdate?.Invoke(pos);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressing = true;
            pressTimer = 0f;
            currentEventData = eventData;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressing = false;
            pressTimer = 0f;

            if (isDragging)
            {
                EndDrag(eventData);
            }
        }

        private void StartDrag()
        {
            isDragging = true;
            isPressing = false;

            OnLongPressStart?.Invoke(gameObject);

            // ✅ 锁定 ScrollRect 滚动
            if (lockScrollWhileDragging && parentScroll != null)
                parentScroll.enabled = false;

            // ✅ 创建副本
            dragCopy = Instantiate(gameObject, canvas.transform);
            dragCopy.name = gameObject.name + "_DragCopy";
            dragCopyRect = dragCopy.GetComponent<RectTransform>();

            // ✅ 初始化副本
            dragCopyRect.localScale = Vector3.one;
            dragCopyRect.anchorMin = new Vector2(0.5f, 0.5f);
            dragCopyRect.anchorMax = new Vector2(0.5f, 0.5f);
            dragCopyRect.pivot = new Vector2(0.5f, 0.5f);
            dragCopy.transform.SetAsLastSibling();

            foreach (var g in dragCopy.GetComponentsInChildren<Graphic>())
                g.raycastTarget = false;

            foreach (var img in dragCopy.GetComponentsInChildren<Image>())
            {
                var c = img.color;
                c.a = 0.6f;
                img.color = c;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                currentEventData.position,
                canvas.worldCamera,
                out Vector2 pos);
            dragCopyRect.anchoredPosition = pos;
        }

        private void EndDrag(PointerEventData eventData)
        {
            isDragging = false;

            // ✅ 释放 ScrollRect
            if (lockScrollWhileDragging && parentScroll != null)
                parentScroll.enabled = true;

            GameObject target = GetUIUnderPointer(eventData);
            OnDropTarget?.Invoke(target);

            if (dragCopy != null)
                Destroy(dragCopy);
        }

        private GameObject GetUIUnderPointer(PointerEventData eventData)
        {
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (var r in results)
            {
                if (r.gameObject != gameObject && r.gameObject != dragCopy)
                    return r.gameObject;
            }
            return null;
        }

        public void OnBeginDrag(PointerEventData eventData) { }
        public void OnDrag(PointerEventData eventData) { currentEventData = eventData; }
        public void OnEndDrag(PointerEventData eventData) { }
    }


}