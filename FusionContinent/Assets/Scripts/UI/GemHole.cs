using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using QFramework;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class GemHole : MonoBehaviour
{
    public Image gemBg;
    public Image gemIcon;
    public Button gemBtn;
    // Start is called before the first frame update
    void Start()
    {
        gemBtn.onClick.AddListener(() =>
        {
            UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
            UIKit.OpenPanel<GemUnmountPanel>();
        });
    }

    public void SetData(GemData gemData)
    {
        if (gemData != null)
        {
            gemIcon.gameObject.SetActive(true);
        }
        else
        {
            gemIcon.gameObject.SetActive(false);
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
