using Config;
using FusionContinent;
using FusionContinent.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterEntryItem : MonoBehaviour
{
    public Image SkillIcon;

    public TMP_Text LevelText;

    public void SetData(HiddenRealmFloorEntryData entryData)
    {
        LevelText.text = entryData.Level.ToString();

        var entryConfig = FindEntryConfig(entryData.EntryType);
        SkillIcon.sprite = Global.MonsterEntryAtlas.GetSpriteExt(entryConfig.Icon);
    }

    private MonsterEntryConfig FindEntryConfig(int entryType)
    {
        var list = DataConfigManager.Tables.TbMonsterEntryConfig.DataList;
        for (int i = 0; i < list.Count; i++)
        {
            if ((int)list[i].MonsterEntryType == entryType)
            {
                return list[i];
            }
        }

        return null;
    }
}
