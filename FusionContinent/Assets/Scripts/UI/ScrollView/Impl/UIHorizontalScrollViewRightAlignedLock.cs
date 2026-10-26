using System.Collections;
using System.Collections.Generic;
using ScrollView;
using UnityEngine;

public class UIHorizontalScrollViewRightAlignedLock : UIHorizontalScrollViewRightAligned
{
    protected override int ComputeRowsPerColumn()
    {
        return this.ItemsPerLine;
    }
}
