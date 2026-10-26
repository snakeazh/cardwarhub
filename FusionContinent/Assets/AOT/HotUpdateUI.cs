using System;
using System.Collections;
using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotUpdateUI : MonoBehaviour
{
    public GameObject NoticeGameObject; 
    public Slider ProgressSlider;
    public TextMeshProUGUI  ProgressText;
    public TextMeshProUGUI  DownloadText;
    public TextMeshProUGUI AnnouncementText;
    public TextMeshProUGUI  TipsText;
    public HotUpdateConfirmDialog hotUpdateConfirmDialog;
    [Range(1,10)]
    public float Speed = 1f;

    private void Awake()
    {
        NoticeGameObject.SetActive(false);
    }

    public void ShowNotice()
    {
        NoticeGameObject.SetActive(true);
    }

    /// <summary>
    /// 设置提示内容
    /// </summary>
    /// <param name="msg"></param>
    public void SetMessage(string msg)
    {
        TipsText.text = msg;
    }

    /// <summary>
    /// 设置公告
    /// </summary>
    /// <param name="msg"></param>
    public void SetAnnouncement(string msg)
    {
        AnnouncementText.text = msg;
    }

    public void SetDownloadProgress(long current, long max)
    {
        DownloadText.text = ResourceInitStep.FormatFileSize(current) + "/" + ResourceInitStep.FormatFileSize(max);
    }
    
    /// <summary>
    /// 显示提示框
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="cfmAction"></param>
    /// <param name="cancelAction"></param>
    public void ShowDialog(string msg,Action cfmAction,Action cancelAction)
    {
        hotUpdateConfirmDialog.gameObject.SetActive(true);
        hotUpdateConfirmDialog.ShowDialog(msg, cfmAction, cancelAction);
    }

    private float selfProgress = 0f;
    private float targetProgress = 0f;
    public void SetProgress(float  progress)
    {
        targetProgress = progress;
    }

    public void ResetUI()
    {
        selfProgress = 0f;
        targetProgress = 0f;
    }


    public float GetProgress()
    {
        return targetProgress;
    }
    
    private void Update()
    {
        selfProgress += Time.deltaTime * Speed;
        selfProgress = Mathf.Clamp(selfProgress,0,targetProgress);
        // float progress = selfProgress;
        ProgressSlider.value = selfProgress;
        ProgressText.text = (selfProgress*100f).ToString("F1");
    }
}
