using System;
using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using FusionContinent.Model;
using UnityEngine;

public enum EGuideType
{
    Force,
    Weak
}

/// <summary>
/// 弱引导和强引导的组件
/// </summary>
public class GuideUIEventCompoent : MonoBehaviour
{
    public EGuideType guideType;
    protected virtual void Start()
    {
        
    }

    protected void SendGuideEvent()
    {
        // Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_INTERACTION+guideType.ToString(),gameObject);
    }
}
