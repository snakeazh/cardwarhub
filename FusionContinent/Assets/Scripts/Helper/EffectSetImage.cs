using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectSetImage : MonoBehaviour
{
    public ParticleSystemRenderer Renderer;
    public void SetImage(Texture2D texture)
    {
        if (Renderer)
        {
            Renderer.material.SetTexture("_maintex", texture);   
        }
    }
}
