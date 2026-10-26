using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Localization;
using UnityEngine;

public class LocalizationaAutoHeightText : LocalizationText
{
    public float addHeight;
    protected override void OnRefresh()
    {
        base.OnRefresh();
        var selfRectTrans = this.GetComponent<RectTransform>();
        selfRectTrans.sizeDelta = new Vector2(selfRectTrans.sizeDelta.x, TextUGUI.preferredHeight+addHeight);
    }
}
