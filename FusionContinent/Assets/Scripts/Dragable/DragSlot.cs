using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardDrop
{
    public class DragSlot : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private int slotValue;
        [SerializeField] private Transform snapPoint;
        [SerializeField] private bool isActive = true;
        private DraggableItem occupant;

        public int SlotValue => slotValue;
        public Transform SnapPoint => snapPoint == null ? transform : snapPoint;
        public bool IsActive => isActive;
        public DraggableItem Occupant => occupant;
        public event Action<DragSlot> SlotClicked;

        public void SetSlotValue(int value)
        {
            slotValue = value;
        }

        public void SetActive(bool active)
        {
            isActive = active;
        }

        public void SetOccupant(DraggableItem item)
        {
            occupant = item;
            slotValue = occupant == null ? 0 : occupant.ItemValue;
        }

        public void ClearOccupant(DraggableItem item)
        {
            if (occupant == item)
            {
                occupant = null;
                slotValue = 0;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SlotClicked?.Invoke(this);
        }
    }
}
