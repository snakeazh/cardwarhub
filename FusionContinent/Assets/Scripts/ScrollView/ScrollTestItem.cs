using System;
using System.Collections;
using System.Collections.Generic;
using ScrollView;
using UnityEngine;
using UnityEngine.UI;

public class ScrollTestData
{
    public int index;
    
    public bool isSelected;
}
public class ScrollTestItem : ScrollItem
{
    public Text text; 
    public Button button;
    
    public Action OnClick;

    private void Awake()
    {
        button?.onClick.AddListener(()=>OnClick?.Invoke());
    }

    public override void SetData<T>(T data)
    {
        text.text = (data as ScrollTestData)?.index.ToString();
        text.color = Color.black;
    }

    protected override void OnRefreshSelect<T>(T data)
    {
        base.OnRefreshSelect(data);
        text.color = (data as ScrollTestData).isSelected ? Color.red : Color.black;
    }
}
