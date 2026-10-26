using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadCellScrollViewTest : MonoBehaviour
{
    public HeadCellScrollView ScrollView;

    private void Start()
    {
        ScrollView.BuildElement(1,10);
        ScrollView.BuildElement(2,10);
        ScrollView.BuildElement(3,10);
        ScrollView.ItemsPerRow = 3;
        ScrollView.ShowList();
    }
}
