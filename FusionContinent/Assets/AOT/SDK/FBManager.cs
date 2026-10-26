using System;
using System.Collections;
using System.Collections.Generic;
using Facebook.Unity;
using UnityEngine;

public class FBManager : MonoBehaviour
{
    private void Awake()
    {
        // 微信小游戏/WebGL 环境没有 DOM，Facebook SDK 初始化会崩溃，直接跳过
#if !UNITY_WEBGL
        FB.Init(OnInitComplete);
#endif
    }

    private void OnInitComplete()
    {
        Debug.Log($"face bool init complete done {FB.IsInitialized}");
    }
}
