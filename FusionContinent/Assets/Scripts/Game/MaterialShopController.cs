using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.U2D;

public class MaterialShopController : MonoBehaviour
{
    
    public Button itembtn;
    public Button equipbtn;

    public GameObject ItemShop;
    public GameObject EqipShop;

    private void Awake()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        itembtn.onClick.AddListener(() =>
        {
            ItemShop.SetActive(true);
            EqipShop.SetActive(false);
            itembtn.transform.Find("Image").localScale = Vector3.one;
            equipbtn.transform.Find("Image").localScale = Vector3.zero;
        });
        equipbtn.onClick.AddListener(() =>
        {
            ItemShop.SetActive(false);
            EqipShop.SetActive(true);
            itembtn.transform.Find("Image").localScale = Vector3.zero;
            equipbtn.transform.Find("Image").localScale = Vector3.one;
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
