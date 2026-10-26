using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace CardDrop
{
    public class DragBootstrap : MonoBehaviour
    {
        [Serializable]
        public struct SlotState
        {
            public int Index;
            public int Value;
            public bool IsActive;
        }

        [SerializeField] private DragSlot[] slots;
        [SerializeField] private int[] slotValues;
        [SerializeField] public DraggableItem[] items;
        [SerializeField] private int[] itemValues;
        private bool _eventsBound;

        public event Action<DragCheckContext> DragEnterActiveSlot;
        public event Action<DragCheckContext> DragExitActiveSlot;
        public event Action<DragCheckContext> DragSwapSuccess;
        public event Action<DragSlot> SlotClicked;

        private void Start()
        {
            Refresh(true);
        }

        private void InitSlots()
        {
            int count = Mathf.Min(slots.Length, slotValues.Length);
            for (int i = 0; i < count; i++)
            {
                if (slots[i] == null)
                {
                    continue;
                }

                slots[i].SetSlotValue(slotValues[i]);
            }
        }

        private void InitItems()
        {
            int count = Mathf.Min(items.Length, itemValues.Length);
            for (int i = 0; i < count; i++)
            {
                if (items[i] == null)
                {
                    continue;
                }

                items[i].SetItemValue(itemValues[i]);
            }
        }

        private void InitItemPositionsBySlotValue()
        {
            if (slots == null || items == null)
            {
                return;
            }

            bool[] slotUsed = new bool[slots.Length];
            for (int i = 0; i < items.Length; i++)
            {
                DraggableItem item = items[i];
                if (item == null)
                {
                    continue;
                }

                int slotIndex = FindFirstAvailableSlotIndex(item.ItemValue, slotUsed);
                if (slotIndex < 0)
                {
                    continue;
                }

                slotUsed[slotIndex] = true;
                item.PlaceOnSlot(slots[slotIndex]);
            }
        }

        private int FindFirstAvailableSlotIndex(int value, bool[] slotUsed)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slotUsed[i] || slots[i] == null)
                {
                    continue;
                }

                if (!slots[i].IsActive)
                {
                    continue;
                }

                if (slots[i].SlotValue == value)
                {
                    return i;
                }
            }

            return -1;
        }

        private void BindEvents()
        {
            if (_eventsBound)
            {
                return;
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    continue;
                }

                items[i].DragCheck += OnDragCheck;
                items[i].DragCheckFailed += OnDragCheckFailed;
                items[i].DragSuccess += OnDragSuccess;
                items[i].DragSwapSuccess += OnDragSwapSuccess;
                items[i].DragEnterActiveSlot += OnDragEnterActiveSlot;
                items[i].DragExitActiveSlot += OnDragExitActiveSlot;
            }
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    continue;
                }

                slots[i].SlotClicked += OnSlotClicked;
            }

            _eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!_eventsBound)
            {
                return;
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    continue;
                }

                items[i].DragCheck -= OnDragCheck;
                items[i].DragCheckFailed -= OnDragCheckFailed;
                items[i].DragSuccess -= OnDragSuccess;
                items[i].DragSwapSuccess -= OnDragSwapSuccess;
                items[i].DragEnterActiveSlot -= OnDragEnterActiveSlot;
                items[i].DragExitActiveSlot -= OnDragExitActiveSlot;
            }
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    continue;
                }

                slots[i].SlotClicked -= OnSlotClicked;
            }

            _eventsBound = false;
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private bool OnDragCheck(DragCheckContext context)
        {
            return true;
        }

        private void OnDragCheckFailed(DragCheckContext context)
        {
            Debug.Log($"DragCheckFailed: item={context.ItemValue}, slot={context.SlotValue}");
        }

        private void OnDragSuccess(DragCheckContext context)
        {
            Debug.Log($"DragSuccess: item={context.ItemValue}, slot={context.SlotValue}");
            DragSwapSuccess?.Invoke(context);
        }

        private void OnDragSwapSuccess(DragCheckContext context)
        {
            if (slots == null)
            {
                return;
            }

            // 拖拽交换成功后，同步记录每个槽位当前值，确保当前槽位值与目标槽位值已完成互换。
            if (slotValues == null || slotValues.Length != slots.Length)
            {
                slotValues = new int[slots.Length];
            }

            for (int i = 0; i < slots.Length; i++)
            {
                slotValues[i] = slots[i] == null ? default : slots[i].SlotValue;
            }
        }


        Color _color = Color.red;
        private void OnDragEnterActiveSlot(DragCheckContext context)
        {
            _color.a = 0.5f;
            context.Slot.GetComponent<Graphic>().color = _color;
            Debug.Log($"进入到了槽位 {context.SlotValue}");
            DragEnterActiveSlot?.Invoke(context);
        }

        private void OnDragExitActiveSlot(DragCheckContext context)
        {
            _color.a = 1;
            context.Slot.GetComponent<Graphic>().color = _color;

            Debug.Log($"离开了槽位 {context.SlotValue}");
            DragExitActiveSlot?.Invoke(context);
        }

        private void OnSlotClicked(DragSlot slot)
        {
            SlotClicked?.Invoke(slot);
        }

        public int[] GetAllSlotValues()
        {
            if (slots == null)
            {
                return Array.Empty<int>();
            }

            int[] values = new int[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                values[i] = slots[i] == null ? default : slots[i].SlotValue;
            }

            return values;
        }

        public List<SlotState> GetAllSlotStates()
        {
            var result = new List<SlotState>();
            if (slots == null)
            {
                return result;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    continue;
                }

                result.Add(new SlotState
                {
                    Index = i,
                    Value = slots[i].SlotValue,
                    IsActive = slots[i].IsActive
                });
            }

            return result;
        }

        public void Refresh(bool reapplyConfiguredValues = false)
        {
            UnbindEvents();
            ClearSlotOccupants();

            if (reapplyConfiguredValues)
            {
                InitSlots();
                InitItems();
            }

            InitItemPositionsBySlotValue();
            BindEvents();
        }

        public void SetValuesAndRefresh(int[] newSlotValues, int[] newItemValues)
        {
            slotValues = newSlotValues ?? Array.Empty<int>();
            itemValues = newItemValues ?? Array.Empty<int>();
            Refresh(true);
        }

        private void ClearSlotOccupants()
        {
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    continue;
                }

                slots[i].SetOccupant(null);
            }
        }
    }
}
