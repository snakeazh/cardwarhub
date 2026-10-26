using Config;
using FusionContinent;
using FusionContinent.Utils;
using UnityEngine;
using UnityEngine.UI;

public class UIConsumeItem : MonoBehaviour
{
    public enum ThemeType
    {
        /// <summary>浅色主题：所需数用 COLOR_REQUIRED</summary>
        Light = 0,
        /// <summary>深色主题：所需数用白色</summary>
        Dark = 1,
    }

    public Image Icon;
    public TMPro.TextMeshProUGUI CountText;

    /// <summary>深浅主题，可在预制体 Inspector 配置</summary>
    public ThemeType Theme2 = ThemeType.Dark;

    /// <summary>拥有数量充足时的颜色（深绿）</summary>
    public const string COLOR_OWN_ENOUGH = "#488719";

    /// <summary>拥有数量不足时的颜色（红）</summary>
    public const string COLOR_OWN_NOT_ENOUGH = "#FF7272";

    /// <summary>所需数量颜色（浅色主题）</summary>
    public const string COLOR_REQUIRED = "#705148";

    /// <summary>所需数量颜色（深色主题）</summary>
    public const string COLOR_REQUIRED_DARK = "#FFFFFF";

    public void SetData(int itemId, int needCount)
    {
        if (itemId <= 0 || CountText == null)
        {
            return;
        }

        long ownNum = Global.GetItemNumById(itemId);
        bool hasEnough = ownNum >= needCount;

        // 拥有数单独上色；所需数随深浅主题：浅色 #705148，深色白色
        var requiredHex = Theme2 == ThemeType.Dark ? COLOR_REQUIRED_DARK : COLOR_REQUIRED;
        ColorUtility.TryParseHtmlString(requiredHex, out var requiredColor);
        CountText.color = requiredColor;
        var ownColor = hasEnough ? COLOR_OWN_ENOUGH : COLOR_OWN_NOT_ENOUGH;
        CountText.text =
            $"<color={ownColor}>{NumberTool.FormatNumber(ownNum)}</color>/{NumberTool.FormatNumber(needCount)}";
        CountText.rectTransform.sizeDelta = new Vector2(CountText.preferredWidth, CountText.preferredHeight);

        var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(itemId);
        if (itemCfg != null && Icon != null)
        {
            Icon.sprite = Global.ItemIconSpriteAtlas.GetSprite(itemCfg.Icon);
        }
    }
}
