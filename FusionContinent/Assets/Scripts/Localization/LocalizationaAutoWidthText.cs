using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Localization;
using UnityEngine;

public class LocalizationaAutoWidthText : LocalizationText
{
    // public float addHeight;
    public float maxWidth = 100f;
    public float parentWidthCorrection;// 父级宽度修正
    public RectTransform parentAutoWidth;// 父级自适应宽度
    
    protected override void OnRefresh()
    {
        base.OnRefresh();
        var selfRectTrans = this.GetComponent<RectTransform>();

        var displayWidth = Mathf.Min(TextUGUI.preferredWidth, maxWidth);
        selfRectTrans.sizeDelta = new Vector2(displayWidth, selfRectTrans.sizeDelta.y);

        if (parentAutoWidth != null)
        {
            parentAutoWidth.sizeDelta = new Vector2(displayWidth + parentWidthCorrection, parentAutoWidth.sizeDelta.y);
        }
    }
}
