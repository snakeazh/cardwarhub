using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickEventPenetration : MonoBehaviour, IPointerClickHandler,IDragHandler,IBeginDragHandler,IEndDragHandler,IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Raycast(eventData,ExecuteEvents.pointerClickHandler);
    }

    private void Raycast<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> callbackFunction) where T : IEventSystemHandler
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject != this.gameObject)
            {
                ExecuteEvents.ExecuteHierarchy(result.gameObject, eventData, callbackFunction);
                break;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Raycast(eventData,ExecuteEvents.dragHandler);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Raycast(eventData,ExecuteEvents.beginDragHandler);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Raycast(eventData,ExecuteEvents.endDragHandler);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Raycast(eventData,ExecuteEvents.pointerDownHandler);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
     Raycast(eventData,ExecuteEvents.pointerUpHandler);
    }
}
