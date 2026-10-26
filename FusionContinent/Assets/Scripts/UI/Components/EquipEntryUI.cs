using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using UnityEngine;

public class EquipEntryUI : MonoBehaviour
{
    public TMPro.TextMeshProUGUI EntryName;
    public TMPro.TextMeshProUGUI EntryValue;

    /// <summary>
    /// 当前条目对应的词条配置 id，供外部读取。
    /// </summary>
    public int EntryId { get; private set; }

    /// <summary>
    /// 填充一条装备词条（基础属性）展示。
    /// EntryName 显示配置中的 Desc；EntryValue 暂不显示（配置表中暂无数据）。
    /// </summary>
    /// <param name="entryId">TbEquipEntryConfig 中的配置 id</param>
    public void SetData(int entryId)
    {
        EntryId = entryId;

        var cfg = DataConfigManager.Tables.TbEquipEntryConfig.GetOrDefault(entryId);

        // 名称：直接展示配置中的 Desc；取不到配置时给空
        if (EntryName != null)
            EntryName.text = cfg != null ? cfg.Desc : string.Empty;

        // 数值：配置表中暂无数据，暂不显示
        if (EntryValue != null)
            EntryValue.text = string.Empty;
    }
}
