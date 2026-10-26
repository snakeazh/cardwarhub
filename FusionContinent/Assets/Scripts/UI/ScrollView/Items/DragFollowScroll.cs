using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FollowScroll : MonoBehaviour,IBeginDragHandler,IEndDragHandler,IDragHandler
{
    public ScrollRect  scrollRect;
    

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (scrollRect  == null)
        {
            return;
        }
        scrollRect.OnBeginDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (scrollRect  == null)
        {
            return;
        }
        scrollRect.OnEndDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (scrollRect  == null)
        {
            return;
        }
        scrollRect.OnDrag(eventData);
    }
}
