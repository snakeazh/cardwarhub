using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[AttributeUsage(AttributeTargets.Field)]
public class SpriteAtlasTargetAttribute : Attribute
{
    public string Name;

    public SpriteAtlasTargetAttribute(string name)
    {
        Name = name;
    }
}
