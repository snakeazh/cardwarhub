using UnityEngine;

public class RectTransformState
{
    public Vector3 Pos;
    public Vector2 Size;
    public Vector3 Scale;
    public Quaternion Rot;

    public void Save(RectTransform rect)
    {
        Pos = rect.anchoredPosition3D;
        Size = rect.sizeDelta;
        Scale = rect.localScale;
        Rot = rect.localRotation;
    }

    public void Load(RectTransform rect)
    {
        rect.anchoredPosition3D = Pos;
        rect.sizeDelta = Size;
        rect.localScale = Scale;
        rect.localRotation = Rot;
    }
}