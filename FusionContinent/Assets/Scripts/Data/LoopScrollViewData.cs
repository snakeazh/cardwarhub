using UnityEngine;

public class LoopScrollViewData
{
    public int SiblingIndex { get; private set; }

    public Vector3 AnchorPosition3D { get; private set; }

    public LoopScrollViewData(int siblingIndex, Vector3 anchorPosition3D)
    {
        SiblingIndex = siblingIndex;
        AnchorPosition3D = anchorPosition3D;
    }
}
