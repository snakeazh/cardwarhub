using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalentLineSpace : MonoBehaviour
{
    RectTransform selfRectTransform;
    public RectTransform LineSpaceRect
    {
        get
        {
            if (selfRectTransform == null)
            {
                selfRectTransform = GetComponent<RectTransform>();
            }
            return selfRectTransform;
        }
    }
    public RectTransform DefaultLine1Rect;
    public RectTransform DefaultLine2Rect;
}
