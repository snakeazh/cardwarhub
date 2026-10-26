using System.Collections;
using System.Collections.Generic;
using Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TalentShowObj : MonoBehaviour
{
    public Image[] levelframe;

    public TMP_Text levelNum;
    
    public Image talentframe;
    public Image talentIcon;
    public Image talentOuterFrame;
    
    public Image talentExtraframe;
    public Image talentExtraIcon;
    public Image talentExtraOuterFrame;
    
    public Button talentButton;
    public Button talentExtraButton;

    public int level;
    public int unlockLv;
    
    public GameObject upTip;
    public GameObject upExtraTip;

    public Transform LevelFrame;
    public Transform LevelFrameNo;

    public bool IsExtra;
    
    public TalentListStruct talentListConfig;
    public GameObject TalentExtraObj;
    // Start is called before the first frame update
}
