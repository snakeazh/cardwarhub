using Game.HeroSeat;
using TMPro;
using UIFramework.Services;
using UnityEngine;

public class SeatSlotItem : MonoBehaviour
{
    public GameObject Slot;
    public GameObject AddNode;
    public TextMeshProUGUI LockText;
    public int Index;
    public SeatEntity Entity;
    public FusionContinent.UI.Components.UIPointerClickAndLongPress SlotClickItem;

    public void SetData(int slotIndex)
    {
        Index = slotIndex;
        var seatService = GameServiceManager.Get<SeatService>();
        var skillId = seatService.GetSlotSkillId(slotIndex);

        if (skillId > 0)
        {
            Slot.SetActive(false);
            Entity.gameObject.SetActive(true);
            Entity.SetData(new SeatEntityData
            {
                SeatSkillId = skillId,
                IsSelect = false,
                ShowFrame = false,
                CheckUnlock = false,
            });
            return;
        }

        Slot.SetActive(true);
        Entity.gameObject.SetActive(false);

        if (seatService.IsSeatSlotUnlocked(slotIndex))
        {
            LockText.gameObject.SetActive(false);
            AddNode.SetActive(true);
            return;
        }

        LockText.gameObject.SetActive(true);
        AddNode.SetActive(false);
        LockText.text = $"备战席 Lv.{seatService.GetSeatSlotUnlockLevel(slotIndex)}";
    }
}
