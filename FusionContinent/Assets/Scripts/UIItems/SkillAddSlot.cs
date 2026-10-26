using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.UI.Components;
using Game.Services;
using Game.Soldier;
using Game.Team;
using UIFramework.Services;
using UnityEngine;
using UnityEngine.UI;

public class SkillAddSlot : MonoBehaviour
{
    // public button
    public SkillItemView Skill;
    private UIPointerClickAndLongPress mButton;
    public void Awake()
    {
        mButton = GetComponent<UIPointerClickAndLongPress>();
        mButton.onClick.RemoveAllListeners();
        mButton.onClick.AddListener(OnClickSlot);
    }

    private async void OnClickSlot()
    {
        if (displayId == 0 || displaySoliderId == 0)
        {
            return;
        }
        mButton.interactable = false;
        bool result = await GameServiceManager.GetOrCreate<TeamService>().SetSkillWearStateAsync(displaySoliderId, displayId, false);
        if (result)
        {
            mButton.interactable = true;
        }
    }

    private int displayId = 0;
    private int displaySoliderId = 0;
    public void SetData(int soliderId,int displaySkillId)
    {
        displaySoliderId = soliderId;
        displayId = displaySkillId;

        if (displayId <= 0)
        {
            Skill.gameObject.SetActive(false);
            return;
        }

        Skill.gameObject.SetActive(true);
        if (mButton != null) mButton.enabled = false;
        // 通过技能ID查配置
        var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(displayId);
        Skill.SetData(0, skillCfg, int.MaxValue);
    }
}
