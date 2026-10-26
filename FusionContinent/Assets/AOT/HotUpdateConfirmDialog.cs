using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotUpdateConfirmDialog : MonoBehaviour
{
    public Button ConfirmButton;
    public Button CancelButton;
    public Action OnConfirmAction;
    public Action OnCancelAction;

    public TextMeshProUGUI contextTxt;
    
    public void ShowDialog(string msg,Action cfmAction,Action cancelAction)
    {
        contextTxt.text = msg;
        OnConfirmAction = cfmAction;
        OnCancelAction = cancelAction;
    }
    
    private void Awake()
    {
        ConfirmButton.onClick.AddListener(ConfirmClick);
        CancelButton.onClick.AddListener(CancelClick);

    }

    private void CancelClick()
    {
        OnCancelAction?.Invoke();
        this.gameObject.SetActive(false);
        Application.Quit();
    }

    private void ConfirmClick()
    {
        OnConfirmAction?.Invoke();
        this.gameObject.SetActive(false);
    }
}
