using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using FusionContinent.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FusionContinent.Model;

public class TalentObj : MonoBehaviour
{
    public Image talentIcon;
    public TMP_Text  talentName;
    public TMP_Text talentStudyNum;
    public TMP_Text combatNum;
    public Button talentUpgradeBtn;
    public Button introduceBtn;
    private TbTalentConfig _tbTalentConfig;

    private void Awake()
    {
        _tbTalentConfig=DataConfigManager.Tables.TbTalentConfig;
    }

    public void SetTalent(int talentId)
    {
        // talentIcon.sprite=Global.TalentIconAtlas.GetSprite(_tbTalentConfig.GetOrDefault(talentId).Icon);
        talentName.text = _tbTalentConfig.GetOrDefault(talentId).Name;
        
        // 显示文本
        var talentlv = Global.PlayerTalents[talentId];
        int baseLevel = talentlv.Lv;
        int baseexlevel = talentlv.ExtraLv;
        int extraLevel = 0;

        // 找到当前等级在TalentList中的位置
        int index = baseexlevel;
        var talentList = _tbTalentConfig.GetOrDefault(talentId).TalentList;
        // 向上检查能加多少ExtraLv（每一级需要ExtraConsumeItems.Count != 0）
        for (int j = 0; j < baseexlevel && j < talentList.Count; j++)
        {
            if (talentList[j].ExtraConsumeItems.Count != 0)
            {
                extraLevel++;
            }
        }
        talentStudyNum.text =
            $"(<size=99><color=yellow>{baseLevel + extraLevel}</color></size>/" +
            $"{talentList.Count + talentList.Count(t => t.ExtraConsumeItems.Count != 0)})";
        
        var fightNum = FusionContinent.Model.Talent.GetFightingNumById(talentId);
        combatNum.GetComponent<RollingNumberManager>().Play(fightNum);
        talentUpgradeBtn.onClick.RemoveAllListeners();
        talentUpgradeBtn.onClick.AddListener(() =>
        {
            TalentPanel.Default.Talentshow.gameObject.SetActive(true);
            TalentPanel.Default.Talentshow.SetTalentShow(talentId);
        });
        introduceBtn.onClick.RemoveAllListeners();
        introduceBtn.onClick.AddListener(() =>
        {
            TalentPanel.Default.TalentIntro.gameObject.SetActive(true);
            TalentPanel.Default.TalentIntro.SetTalentIntor(talentId);
        });
    }
}
