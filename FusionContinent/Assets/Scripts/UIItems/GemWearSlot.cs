using System.Collections;
using System.Collections.Generic;
using System;
using Assets.Scripts.Localization;
using Config;
using FusionContinent;
using Game.Equip;
using TMPro;
using UIFramework.Services;
using UnityEngine;
using UnityEngine.UI;

public class GemWearSlot : MonoBehaviour
{
    public GameObject WearedBg;
    
    public GameObject UnWearedBg;

    public Image PartIcon;
    
    public Image EquipIcon;
    
    
    public TMP_Text WeaponName;
    
    public TMP_Text WeaponQuality;
    public GameObject Selected;
    public GameObject WearEffect;
    public EquipPartType PartType { get; private set; }
    
    public List<Image> GemList = new List<Image>();
    
    /// <param name="currentGemId">当前详情页融合石 Id；仅该石穿戴的栏位显示特效，默认隐藏</param>
    public void SetData(EquipPartType equipPartType, int currentGemId = 0)
    {
        // var partNameList = DataConfigManager.Tables.TbGameBattleConst.EquipPartTypeList;
        PartType =  equipPartType;
        PartIcon.sprite = Global.EquipPartIconAtlas.GetSprite(PartType.ToString());
        PartIcon.gameObject.SetActive(true);
        EquipIcon.gameObject.SetActive(false);
        // // 已佩戴融合石
        string wearDesc = "";
        EquipCardSlotData slotData = GameServiceManager.GetOrCreate<EquipService>().GetWearPartSlot(DataConfigManager.Tables.TbPlayerConst.GlobalUserWearId,PartType);
        WearedBg.gameObject.SetActive(false);
        UnWearedBg.gameObject.SetActive(false);
        GemList[0].sprite = Global.FusionGemStoreAtlas.GetSprite($"UnsetState");
        bool showWearEffect = false;
        //已佩戴装备
        if (slotData !=null && slotData.EquipOnlyId > 0)
        {
            var cfg = Global.PlayerEquips[slotData.EquipOnlyId];
            PartIcon.gameObject.SetActive(false);
            EquipIcon.gameObject.SetActive(true);
            wearDesc = Global.PlayerEquips[slotData.EquipOnlyId].Config.Name;
            EquipIcon.GetComponent<Image>().sprite = Global.EquipIconSpriteAtlas.GetSprite(cfg.Config.Icon);
            var quality = cfg.Quality;
            WearedBg.GetComponent<Image>().sprite = Global.FusionGemBgAtlas.GetSprite($"EquipmentInlayBackgroundFrame{quality.ToString()}");
            WearedBg.gameObject.SetActive(true);
            // WeaponQuality.text = DataConfig.
            if (slotData.FusionGem != null)
            {
                GemList[0].sprite = Global.FusionGemStoreAtlas.GetSprite($"InlayStone{slotData.FusionGem.Quality.ToString()}");
                showWearEffect = currentGemId > 0 && slotData.FusionGem.Id == currentGemId;
            }
        }
        else
        {
            UnWearedBg.gameObject.SetActive(true);
        }
        WeaponName.text = wearDesc;
        WearEffect.SetActive(showWearEffect);
    }

    public void SetSelected(bool selected)
    {
        if (Selected == null)
        {
            var selectedTrans = transform.Find("Selected") ?? transform.Find("Select") ?? transform.Find("Check");
            if (selectedTrans != null)
            {
                Selected = selectedTrans.gameObject;
            }
        }
        
        if (Selected != null)
        {
            Selected.SetActive(selected);
        }
    }

    private int GetWearSlotIndex(EquipPartType selectedPartType)
    {
        int index = 0;
        foreach (EquipPartType partType in Enum.GetValues(typeof(EquipPartType)))
        {
            if (partType == EquipPartType.Weapon)
            {
                continue;
            }

            if (partType == selectedPartType)
            {
                return index;
            }

            index++;
        }

        return -1;
    }
}