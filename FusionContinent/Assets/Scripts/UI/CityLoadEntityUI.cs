using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

public class CityLoadEntityUI : MonoBehaviour
{
    public CityLoadData cityLoadData;
    public Image cityLoadBg;
    public Image cityLoadIcon;
    public GameObject warObj;
    public Button cityLoadBtn;

    // Start is called before the first frame update
    void Start()
    {
        cityLoadBtn.onClick.AddListener(() => { ChangeCityLoad(); UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play(); });
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void SetData(CityLoadData cityLoad)
    {
        cityLoadData = cityLoad;
        cityLoadIcon.sprite = Global.CardIconSpriteAtlas.
            GetSprite(DataConfigManager.Tables.TbMainCityBattleConfig.GetOrDefault(cityLoadData.Id).Icon);
        if (cityLoadData.Id == Global.CitySkill)
        {
            warObj.SetActive(true);
        }
        else
        {
            warObj.SetActive(false);
        }
    }

    public void ChangeCityLoad()
    {
       
    }
}