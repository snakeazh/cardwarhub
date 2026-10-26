using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackPaneal : MonoBehaviour
{
    private InputField InputField;
    private Button submitbtn;
    private Button quitbtn;
    private string url = "https://qm.qq.com/q/9lvJgyBOo0";
    // Start is called before the first frame update
    void Awake()
    {
        InputField = transform.Find("InputField").GetComponent<InputField>();
        submitbtn = transform.Find("submitbtn").GetComponent<Button>();
        quitbtn = transform.Find("quitPanel").GetComponent<Button>();
    }
    private void Start()
    {
        submitbtn.onClick.AddListener(() =>
        {
            Application.OpenURL(url);
            this.gameObject.SetActive(false);
        });
        quitbtn.onClick.AddListener(() =>
        {
            this.gameObject.SetActive(false);
        });
    }
    private void OnEnable()
    {
        InputField.text = "";
    }
    // Update is called once per frame
    void Update()
    {

    }
}
