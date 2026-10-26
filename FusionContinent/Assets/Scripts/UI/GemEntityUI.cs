using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using QFramework;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GemEntityUI : MonoBehaviour
{
    public GemData gemData;
    public Image gemBg;
    public Image gemIcon;
    public Button gemBtn;
    
    
    // Start is called before the first frame update
    void Start()
    {
        gemBtn.onClick.AddListener(() =>
        {
            UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
            OnClickGemFun();
        });
        
    }

    public void SetData(GemData gem)
    {
        gemData = gem;
        switch (gem.Quality)
        {
            case 0:
                gemBg.color = Color.gray;
                break;
            case 1:
                gemBg.color = Color.cyan;
                break;
            case 2:
                gemBg.color = Color.blue;
                break;
            case 3:
                gemBg.color = Color.magenta;
                break;
        }
        gemIcon.sprite = Global.CardIconSpriteAtlas.GetSprite(DataConfigManager.Tables.TbGemConfig.GetOrDefault(gemData.GemId).Icon);
    } 
    
    // Update is called once per frame
    void Update()
    {
        
    }

    void OnClickGemFun()
    {
        UIKit.OpenPanel<GemInlayPanel>();
    }
}
