using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using QFramework;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class GemInlaySlot : MonoBehaviour
{
    public GameObject notGemObj;
    public Text slotText;
    public Button replaceBtn;

    private int curSlotIndex;
    // Start is called before the first frame update
    void Start()
    {
        replaceBtn.onClick.AddListener(ReplaceGem);
    }

    public void SetData(GemData gemData,int index)
    {
        if (gemData == null)
        {
            notGemObj.SetActive(true);
            slotText.gameObject.SetActive(false);
            replaceBtn.gameObject.SetActive(false);
        }
        else
        {
            notGemObj.SetActive(false);
            slotText.gameObject.SetActive(true);
            replaceBtn.gameObject.SetActive(true);
            
        }

        curSlotIndex = index;
    }
    
    private void ReplaceGem()
    {
        // 检查槽位是否有效
        if (curSlotIndex < 0 || curSlotIndex >= Global.GemSlots.Count)
        {
            LogTool.LogError("替换失败：槽位索引无效！");
            return;
        }

        // 将当前槽位的宝石移除到列表
        var currentGem = Global.GemSlots[curSlotIndex];
        if (currentGem != null)
        {
            LogTool.Log($"替换宝石：移除当前宝石 ID: {currentGem.GemId}");
        }
        
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
