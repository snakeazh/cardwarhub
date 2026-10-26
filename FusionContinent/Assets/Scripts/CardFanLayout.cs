using System;
using System.Collections.Generic;
using UnityEngine;

public class CardFanLayout : MonoBehaviour
{
    public float spacing = 80;
    public float maxAngle = 20;
    public float height = 30;
    public int compactCountThreshold = 3;
    [Range(0.1f, 1f)] public float compactSpacingMultiplier = 0.7f;
    [Range(0.1f, 1f)] public float compactAngleMultiplier = 0.65f;

    public void Refresh(List<RectTransform> cards)
    {
        int count = cards.Count;
        float currentSpacing = spacing;
        float currentMaxAngle = maxAngle;

        // if (count <= compactCountThreshold)
        // {
        //     currentSpacing *= compactSpacingMultiplier;
        //     currentMaxAngle *= compactAngleMultiplier;
        // }

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (count - 1f);

            float normalized = t * 2 - 1;

            float angle = -normalized * currentMaxAngle;

            float x = normalized * currentSpacing * count * 0.5f;

            float y = -Mathf.Abs(normalized) * height;

            cards[i].anchoredPosition = new Vector2(x, y);

            cards[i].localRotation =
                Quaternion.Euler(0, 0, angle);
        }
    }
}