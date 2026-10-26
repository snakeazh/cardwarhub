using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRewardItem : MonoBehaviour
{
    public Image ItemImage;
    public TextMeshProUGUI NumberText;

    public void SetData(Config.RewardAndNum rewardAndNum)
    {
        var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(rewardAndNum.Id);
        ItemImage.sprite = Global.ItemIconSpriteAtlas.GetSpriteExt(itemCfg.Icon);

        NumberText.text = $"×{rewardAndNum.Num}";
        NumberText.ForceMeshUpdate();
        NumberText.rectTransform.sizeDelta = new Vector2(NumberText.preferredWidth, NumberText.rectTransform.sizeDelta.y);

        float imageWidth = ((RectTransform)ItemImage.transform).rect.width;
        float textWidth = NumberText.preferredWidth;
        float totalWidth = imageWidth + textWidth;

        var selfRect = (RectTransform)transform;
        selfRect.sizeDelta = new Vector2(totalWidth, selfRect.sizeDelta.y);

        float startX = -totalWidth * 0.5f;
        var imageRect = (RectTransform)ItemImage.transform;
        imageRect.anchoredPosition = new Vector2(startX + imageWidth * 0.5f, imageRect.anchoredPosition.y);
        NumberText.rectTransform.anchoredPosition = new Vector2(startX + imageWidth + textWidth * 0.5f, NumberText.rectTransform.anchoredPosition.y);
    }
}
