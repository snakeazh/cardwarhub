using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using UnityEngine;
using UnityEngine.UI;

public class HaveItemNode : MonoBehaviour
{
    public Image Icon;
    public TMPro.TextMeshProUGUI CountText;
    
    public void SetData(int itemId)
    {
        if (itemId <= 0 || CountText == null)
        {
            return;
        }

        long priorityOwn = Global.GetItemNumById(itemId);

        CountText.text = $"{priorityOwn}";
        CountText.rectTransform.sizeDelta = new Vector2(CountText.preferredWidth, CountText.preferredHeight);

        // 设置图标
        var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(itemId);
        if (itemCfg != null && Icon != null && Global.ItemIconSpriteAtlas != null)
        {
            Icon.sprite = Global.ItemIconSpriteAtlas.GetSpriteExt(itemCfg.Icon);
        }

        // 刷新布局
        // if (layoutGroup != null)
        // {
        //     layoutGroup.SetLayoutHorizontal();
        // }
        // ShopConsumeRenderer.RenderConsume(Icon,CountText,null,new RewardItem(itemId,needCount,ItemType.ItemGroup),true,1);
        // Icon.sprite = Global.ItemIconSpriteAtlas.GetSpriteExt()
    }
}
