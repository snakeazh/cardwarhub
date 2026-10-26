using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
public class CameraShake : MonoBehaviour
{
    public Transform[] targets; // 需要抖动的物体数组
    public float shakeDuration = 0.5f;
    public float shakeStrength = 0.1f;
    public int shakeFrequency = 10;

    private Vector3[] originalPositions; // 存储每个物体的初始位置

    void Start()
    {
        // 初始化每个物体的初始位置
        originalPositions = new Vector3[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                originalPositions[i] = targets[i].position;
            }
        }
    }

    public void ShakeCamera()
    {
        // 为每个物体创建一个抖动序列
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                ShakeSingleObject(targets[i], originalPositions[i]);
            }
        }
    }

    private void ShakeSingleObject(Transform target, Vector3 originalPosition)
    {
        Sequence shakeSequence = DOTween.Sequence();

        for (int j = 0; j < shakeFrequency; j++)
        {
            // 在每个抖动周期内，随机改变物体的位置
            float randomX = Random.Range(-shakeStrength, shakeStrength);
            float randomY = Random.Range(-shakeStrength, shakeStrength);
            Vector3 shakePosition = new Vector3(originalPosition.x + randomX, originalPosition.y + randomY, originalPosition.z);

            // 添加一个移动到随机位置然后返回原位置的动画到序列中
            shakeSequence.Append(target.DOMove(shakePosition, shakeDuration / (shakeFrequency * 2)))
                         .Append(target.DOMove(originalPosition, shakeDuration / (shakeFrequency * 2)));
        }

        // 设置序列的循环类型和次数
        shakeSequence.SetLoops(1, LoopType.Yoyo);
    }
}