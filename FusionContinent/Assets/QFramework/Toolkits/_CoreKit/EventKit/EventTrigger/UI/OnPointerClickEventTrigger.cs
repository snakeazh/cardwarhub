/****************************************************************************
 * Copyright (c) 2016 - 2023 liangxiegame UNDER MIT License
 * 
 * https://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace QFramework
{
    public class OnPointerClickEventTrigger : MonoBehaviour, IPointerClickHandler
    {
        public UnityAction<PointerEventData> OnPointerClickEvent;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (OnPointerClickEvent != null) OnPointerClickEvent(eventData);
        }
        
        public static OnPointerClickEventTrigger CheckAndAddListener(GameObject obj)
        {
            OnPointerClickEventTrigger listener = obj.GetComponent<OnPointerClickEventTrigger>();
            if (listener == null)
            {
                listener = obj.AddComponent<OnPointerClickEventTrigger>();
            }

            return listener;
        }
        public static  OnPointerClickEventTrigger Get(GameObject obj)
        {
            return CheckAndAddListener (obj);
        }
        
        void OnDestroy()
        {
            OnPointerClickEvent = null;
        }
    }
}