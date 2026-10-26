using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using FusionContinent.UI.Components;
using UnityEngine;
using UnityEngine.UI;
using Config;

public class EquipEffectEntry : MonoBehaviour
{
    public Image Flag;

    public TMPro.TextMeshProUGUI Name;

    public UISelectableToggle Toggle;

    /// <summary>
    /// 当前条目对应的特效配置 id，供外部读取选中态时使用。
    /// </summary>
    public int EffectId { get; private set; }

    /// <summary>
    /// 填充一条装备特效展示。
    /// </summary>
    /// <param name="effectEntryId">TbEquipEffectsConfig 中的配置 id</param>
    /// <param name="lockStatus">该特效是否处于锁定状态（true=已锁定，不可选中）</param>
    /// <param name="ShowLock">是否展示锁图标；false 时即便已锁定也不显示锁</param>
    public void SetData(int effectEntryId, bool lockStatus, bool ShowLock)
    {
        EffectId = effectEntryId;

        var cfg = DataConfigManager.Tables.TbEquipEffectsConfig.GetOrDefault(effectEntryId);

        // 名称：直接展示表格配置的 Name,未取到配置时给空
        if (Name != null)
            Name.text = cfg != null ? $"[{cfg.Name}]" : string.Empty;

        Toggle.gameObject.SetActive(ShowLock);
        if (ShowLock)
            Toggle.SetIsOnWithoutNotify(lockStatus);
    }
}
