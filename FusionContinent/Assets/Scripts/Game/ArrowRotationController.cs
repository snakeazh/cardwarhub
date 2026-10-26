using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ArrowRotationController : MonoBehaviour
{
    [Header("旋转设置")]
    public float startRotationX = 20f;
    public float endRotationX = -40f;
    
    [Header("时间设置")]
    public float minDuration = 0.5f;
    public float maxDuration = 2f;
    
    [Header("高度参考")]
    public float maxHeightDifference = 10f;
    
    private bool isAnimating = false;
    private Tween currentTween;

    public void StartRotationAnimation(Vector3 targetPosition)
    {
        // 停止当前动画（确保每次调用都重新开始）
        StopAnimation();
        
        isAnimating = true;
        // 计算基于高度差的持续时间
        float heightDifference = Mathf.Abs(transform.position.y - targetPosition.y);
        float duration = CalculateDuration(heightDifference);
        
        // 设置起始旋转（X轴为20度）
        Vector3 currentEuler = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(
            startRotationX, 
            currentEuler.y, 
            currentEuler.z
        );
        
        // 使用DOTween进行旋转动画 - 沿着自身坐标系旋转
        currentTween = transform.DOLocalRotate(
            new Vector3(endRotationX, currentEuler.y, currentEuler.z),
            duration,
            RotateMode.Fast
        ).SetEase(Ease.OutQuad)
         .OnComplete(() => {
            isAnimating = false;
            LogTool.Log("旋转动画完成");
        })
         .OnKill(() => {
            isAnimating = false;
        });
    }
    
    // 根据高度差计算持续时间
    private float CalculateDuration(float heightDifference)
    {
        // 确保高度差不为零
        heightDifference = Mathf.Max(0.1f, heightDifference);
        
        // 根据高度差计算持续时间
        float normalizedHeight = Mathf.Clamp01(heightDifference / maxHeightDifference);
        float duration = Mathf.Lerp(minDuration, maxDuration, normalizedHeight);
        
        return duration;
    }
    
    // 直接设置持续时间的方法
    public void StartRotationAnimationWithDuration(float duration)
    {
        // 停止当前动画（确保每次调用都重新开始）
        StopAnimation();
        
        isAnimating = true;
        
        // 设置起始旋转（X轴为20度）
        Vector3 currentEuler = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(
            startRotationX, 
            currentEuler.y, 
            currentEuler.z
        );
        
        // 使用DOTween进行旋转动画 - 沿着自身坐标系旋转
        currentTween = transform.DOLocalRotate(
            new Vector3(endRotationX, currentEuler.y, currentEuler.z),
            duration,
            RotateMode.Fast
        ).SetEase(Ease.OutQuad)
         .OnComplete(() => {
            isAnimating = false;
            LogTool.Log("旋转动画完成");
        })
         .OnKill(() => {
            isAnimating = false;
        });
    }
    
    // 立即开始旋转到结束状态（无动画）
    public void SnapToEndRotation()
    {
        StopAnimation();
        
        Vector3 currentEuler = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(
            endRotationX, 
            currentEuler.y, 
            currentEuler.z
        );
        
        isAnimating = false;
    }
    
    // 立即开始旋转到起始状态（无动画）
    public void SnapToStartRotation()
    {
        StopAnimation();
        
        Vector3 currentEuler = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(
            startRotationX, 
            currentEuler.y, 
            currentEuler.z
        );
        
        isAnimating = false;
    }
    
    // 停止当前动画
    public void StopAnimation()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
        isAnimating = false;
    }
    
    // 重置到初始旋转状态
    public void ResetRotation()
    {
        StopAnimation();
        SnapToStartRotation();
    }
    
    // 检查是否正在动画中
    public bool IsAnimating()
    {
        return isAnimating;
    }
    
    // 重新开始当前动画（如果正在运行）
    public void RestartAnimation()
    {
        if (isAnimating)
        {
            Vector3 currentEuler = transform.localEulerAngles;
            float progress = Mathf.InverseLerp(startRotationX, endRotationX, currentEuler.x);
            
            // 停止当前动画
            StopAnimation();
            
            // 重新设置起始状态
            transform.localRotation = Quaternion.Euler(
                startRotationX, 
                currentEuler.y, 
                currentEuler.z
            );
            
            // 重新开始动画
            StartRotationAnimationWithDuration(maxDuration * (1 - progress));
        }
    }
}