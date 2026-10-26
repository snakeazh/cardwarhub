using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalkSdk : MonoBehaviour
{
    public static TalkSdk Instance;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start");
        // TalkingDataSDK.SetVerboseLogDisable();
        TalkingDataSDK.BackgroundSessionEnabled();
        TalkingDataSDK.InitSDK("DBB925F721474B41A70B3DE4AEBB4751", "unity", "");
        
    }

    public void StartSdk()
    {

        TalkingDataSDK.StartA();
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
