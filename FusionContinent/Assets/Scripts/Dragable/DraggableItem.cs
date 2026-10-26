using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardDrop
{
    [RequireComponent(typeof(RectTransform))]
    public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private int itemValue;
        [SerializeField] private bool snapToSlotOnSuccess = true;
        [SerializeField] private bool fallbackToValueEqualCheck = true;
        [SerializeField] private RectTransform dragContainer;
        public int Index;
        private RectTransform _rectTransform;
        private float _dragScaleFactor = 1f;
        private CanvasGroup _canvasGroup;
        private Transform _originParent;
        private Vector2 _originAnchoredPosition;
        private DragSlot _currentHoverSlot;
        private DragSlot _currentSlot;
        private DragSlot _dragStartSlot;

        public int ItemValue => itemValue;

        public event Func<DragCheckContext, bool> DragCheck;
        public event Action<DragCheckContext> DragCheckFailed;
        public event Action<DragCheckContext> DragSuccess;
        public event Action<DragCheckContext> DragSwapSuccess;
        public event Action<DragCheckContext> DragEnterActiveSlot;
        public event Action<DragCheckContext> DragExitActiveSlot;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                _dragScaleFactor = parentCanvas.scaleFactor;
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnDisable()
        {
            _currentSlot?.ClearOccupant(this);
            _currentSlot = null;
        }

        public void SetItemValue(int value)
        {
            itemValue = value;
        }

        public void PlaceOnSlot(DragSlot slot)
        {
            if (slot == null)
            {
                return;
            }

            _currentSlot?.ClearOccupant(this);
            _rectTransform.SetParent(slot.SnapPoint, true);
            _rectTransform.position = slot.SnapPoint.position;
            _currentSlot = slot;
            _currentSlot.SetOccupant(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _originParent = _rectTransform.parent;
            _originAnchoredPosition = _rectTransform.anchoredPosition;
            _currentHoverSlot = null;
            if (_currentSlot == null)
            {
                _currentSlot = _originParent == null ? null : _originParent.GetComponentInParent<DragSlot>();
            }
            _dragStartSlot = _currentSlot;
            _dragStartSlot?.ClearOccupant(this);
            if (dragContainer != null)
            {
                _rectTransform.SetParent(dragContainer, true);
            }
            _canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            _rectTransform.anchoredPosition += eventData.delta / _dragScaleFactor;
            UpdateHoverSlot(FindSlotUnderPointer(eventData));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            DragSlot slot = FindSlotUnderPointer(eventData);
            UpdateHoverSlot(slot);
            UpdateHoverSlot(null);
            _canvasGroup.blocksRaycasts = true;

            if (slot == null)
            {
                RecoverToOrigin();
                return;
            }

            var context = new DragCheckContext(this, slot);
            bool canTrigger = EvaluateDragCheck(context);

            if (!canTrigger)
            {
                DragCheckFailed?.Invoke(context);
                RecoverToOrigin();
                return;
            }

            DragSuccess?.Invoke(context);
            if (snapToSlotOnSuccess)
            {
                DraggableItem slotOccupant = slot.Occupant;
                bool swapped = false;
                if (slotOccupant != null && slotOccupant != this && _dragStartSlot != null)
                {
                    slotOccupant.PlaceOnSlot(_dragStartSlot);
                    swapped = true;
                }

                PlaceOnSlot(slot);
                if (swapped)
                {
                    DragSwapSuccess?.Invoke(context);
                }
            }
        }

        private bool EvaluateDragCheck(DragCheckContext context)
        {
            Delegate[] listeners = DragCheck?.GetInvocationList();
            if (listeners == null || listeners.Length == 0)
            {
                return !fallbackToValueEqualCheck || context.ItemValue == context.SlotValue;
            }

            for (int i = 0; i < listeners.Length; i++)
            {
                var listener = (Func<DragCheckContext, bool>)listeners[i];
                if (!listener(context))
                {
                    return false;
                }
            }

            return true;
        }

        private DragSlot FindSlotUnderPointer(PointerEventData eventData)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            for (int i = 0; i < results.Count; i++)
            {
                DragSlot slot = results[i].gameObject.GetComponentInParent<DragSlot>();
                if (slot != null && slot.IsActive)
                {
                    return slot;
                }
            }

            return null;
        }

        private void UpdateHoverSlot(DragSlot newSlot)
        {
            if (_currentHoverSlot == newSlot)
            {
                return;
            }

            if (_currentHoverSlot != null)
            {
                DragExitActiveSlot?.Invoke(new DragCheckContext(this, _currentHoverSlot));
            }

            _currentHoverSlot = newSlot;

            if (_currentHoverSlot != null)
            {
                DragEnterActiveSlot?.Invoke(new DragCheckContext(this, _currentHoverSlot));
            }
        }

        private void RecoverToOrigin()
        {
            if (_dragStartSlot != null)
            {
                PlaceOnSlot(_dragStartSlot);
                return;
            }

            _rectTransform.SetParent(_originParent, true);
            _rectTransform.anchoredPosition = _originAnchoredPosition;
        }
    }
}
