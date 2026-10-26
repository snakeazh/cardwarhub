using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using JetBrains.Annotations;
using QFramework;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class GemUnmountSlot : MonoBehaviour
{
    public GameObject notGemObj;
    public Text slotText;
    public Button unmountBtn;
    public Image gemIcon;

    private int gemNum;
    private int curSlotIndex;
    // Start is called before the first frame update
    void Start()
    {
        unmountBtn.onClick.AddListener(RemoveGem);
    }

    public void SetData(GemData gemData,int index)
    {
        if (gemData == null)
        {
            notGemObj.SetActive(true);
            slotText.gameObject.SetActive(false);
            unmountBtn.gameObject.SetActive(false);
            gemIcon.gameObject.SetActive(false);
        }
        else
        {
            notGemObj.SetActive(false);
            slotText.gameObject.SetActive(true);
            unmountBtn.gameObject.SetActive(true);
            gemIcon.gameObject.SetActive(true);

        }

        curSlotIndex = index;
    }
    private void RemoveGem()
    {
        // 更新槽位数据
        Global.GemSlots[curSlotIndex] = null;
        SetData(null,curSlotIndex);

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
