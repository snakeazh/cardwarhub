using System;
using System.Collections;
using System.Collections.Generic;
using FusionContinent.UI.Components;
using ScrollView;
using UnityEngine;

public class CustomBtnScrollItem : ScrollItem
{
    public UIPointerClickAndLongPress clickBtn;


    public Action OnClick;
    public override void OnSpawn()
    {
        base.OnSpawn();
        if (clickBtn!= null)
        {
            clickBtn.onClick.RemoveAllListeners();
            clickBtn.onClick.AddListener(() =>
            {
                OnClick?.Invoke();
            });
        }
    }

    public override void OnRelease()
    {
        if (clickBtn!= null)
        {
            clickBtn.onClick.RemoveAllListeners();
        }
        base.OnRelease();
            
    }
}
