using UnityEngine;

namespace CardDrop
{
    public class DragCheckContext
    {
        public DraggableItem Item { get; }
        public DragSlot Slot { get; }
        public int ItemValue => Item.ItemValue;
        public int SlotValue => Slot.SlotValue;

        public DragCheckContext(DraggableItem item, DragSlot slot)
        {
            Item = item;
            Slot = slot;
        }
    }
}
