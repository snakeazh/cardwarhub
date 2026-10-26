using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIEntryController : MonoBehaviour
{
    [Header("如在 Inspector 拖拽 UiObjects，则优先使用")]
    public List<RectTransform> UiObjects;

    public bool EnableLayoutAfterAnim = false;
    public float ExitMoveDistance = 2000f;     // 退出移动距离

    private List<Vector2> targetPositions = new List<Vector2>();
    private Sequence animSequence;
    private bool isFirst = true;
    private bool uiObjectsManuallySet = false;
    public Action OnAnimationComplete;
    private bool direction = true;

    private bool isPreparing = false;
    private bool hasRecordedPositions = false;

    private void OnEnable()
    {
        if (isPreparing) return;

        if (!uiObjectsManuallySet && !HasAssignedUiObjects())
        {
            return;
        }

        for (int i = 0; i < UiObjects.Count; i++)
        {
            var obj = UiObjects[i];
            if (obj == null) continue;

            obj.gameObject.SetActive(false);
        }

        StartCoroutine(PrepareAnimation());
    }

    private bool HasAssignedUiObjects()
    {
        try
        {
            if (UiObjects == null || UiObjects.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < UiObjects.Count; i++)
            {
                if (UiObjects[i] != null)
                {
                    return true;
                }
            }

            return false;
        }
        catch (UnassignedReferenceException)
        {
            return false;
        }
    }

    public void Play(bool dic = true)
    {
        direction = dic;
        isPreparing = false;
        OnEnable();
    }

    private void OnDisable()
    {
        if (animSequence != null && animSequence.IsActive())
        {
            animSequence.Kill();
        }
    }

    private IEnumerator PrepareAnimation()
    {
        isPreparing = true;

        // 开启 layout group 强制刷新布局
        foreach (var obj in UiObjects)
        {
            EnableLayoutGroups(obj);
            obj.gameObject.SetActive(true);
        }

        // 强制布局刷新
        yield return null;
        foreach (var obj in UiObjects)
        {
            var parent = obj.transform.parent as RectTransform;
            if (parent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }

        // 再等待一帧保证布局最终完成
        yield return null;

        // 记录最终位置
        if (!hasRecordedPositions)
        {
            targetPositions.Clear();
            foreach (var obj in UiObjects)
            {
                targetPositions.Add(obj.anchoredPosition);
            }
            hasRecordedPositions = true;
        }

        // 关闭 LayoutGroup 以便播放动画不冲突
        foreach (var obj in UiObjects)
        {
            DisableLayoutGroups(obj);
        }

        // 设置初始动画位置（飞入前偏移）
        for (int i = 0; i < UiObjects.Count; i++)
        {
            UiObjects[i].anchoredPosition = targetPositions[i] + (direction ? new Vector2(-2000f, 0) : new Vector2(0f, 2000f));
        }

        PlayAnimation();
        isFirst = false;
        isPreparing = false;
    }

    private void PlayAnimation()
    {
        if (animSequence != null && animSequence.IsActive())
        {
            animSequence.Kill();
        }

        animSequence = DOTween.Sequence().SetUpdate(true);

        for (int i = 0; i < UiObjects.Count; i++)
        {
            int index = i;
            RectTransform rt = UiObjects[index];

            animSequence.Insert(index * 0.05f,
                rt.DOAnchorPos(targetPositions[index], 0f).SetEase(Ease.OutBack)
            );
        }

        animSequence.OnComplete(() =>
        {
            if (EnableLayoutAfterAnim)
            {
                foreach (var obj in UiObjects)
                {
                    EnableLayoutGroups(obj);
                }
            }

            OnAnimationComplete?.Invoke();
        });
    }

    /// <summary>
    /// 设置退出UI效果 - 当前选中物体向上移动，其余向下移动
    /// </summary>
    /// <param name="selectedObject">当前选中的物体</param>
    /// <param name="duration">动画过度时间</param>
    /// <param name="targetUiObjects">重新传入目标物体</param>
    public void SetQuitUIEffect(RectTransform selectedObject, float duration = 0.4f)
    {
        if (animSequence != null && animSequence.IsActive())
        {
            animSequence.Kill();
        }

        animSequence = DOTween.Sequence().SetUpdate(true);
        foreach (var obj in UiObjects)
        {
            DisableLayoutGroups(obj);
        }

        foreach (var obj in UiObjects)
        {
            if (obj == selectedObject)
            {
                // 选中物体向上移动
                animSequence.Join(
                    obj.DOAnchorPosY(obj.anchoredPosition.y + ExitMoveDistance, duration)
                        .SetEase(Ease.OutQuad)
                );
            }
            else
            {
                // 其他物体向下移动
                animSequence.Join(
                    obj.DOAnchorPosY(obj.anchoredPosition.y - ExitMoveDistance, duration)
                        .SetEase(Ease.OutQuad)
                );
            }
        }

        animSequence.OnComplete(() =>
        {
            // 动画完成后可以在这里添加回调
            for (int i = 0; i < UiObjects.Count; i++)
            {
                var obj = UiObjects[i];
                obj.gameObject.SetActive(false);
                obj.anchoredPosition = targetPositions[i];
            }
        });
    }
    
    /// <summary>
    /// 设置退出UI效果 - 当前选中物体不动，其余向下移动
    /// </summary>
    /// <param name="selectedObject">当前选中的物体</param>
    /// <param name="duration">动画过度时间</param>
    /// <param name="targetUiObjects">重新传入目标物体</param>
    public void SetQuitUIEffect2(RectTransform selectedObject, float duration = 0.4f)
    {
        if (animSequence != null && animSequence.IsActive())
        {
            animSequence.Kill();
        }

        animSequence = DOTween.Sequence().SetUpdate(true);
        foreach (var obj in UiObjects)
        {
            DisableLayoutGroups(obj);
        }

        foreach (var obj in UiObjects)
        {
            if (obj != selectedObject)
            {
                // 其他物体向下移动
                animSequence.Join(
                    obj.DOAnchorPosY(obj.anchoredPosition.y - ExitMoveDistance, duration)
                        .SetEase(Ease.OutQuad)
                );
            }
        }

        animSequence.OnComplete(() =>
        {
            // 动画完成后可以在这里添加回调
            for (int i = 0; i < UiObjects.Count; i++)
            {
                var obj = UiObjects[i];
                obj.gameObject.SetActive(obj == selectedObject);
                obj.anchoredPosition = targetPositions[i];
            }
        });
    }

    private void DisableLayoutGroups(Transform t)
    {
        var v = t.GetComponent<VerticalLayoutGroup>();
        var h = t.GetComponent<HorizontalLayoutGroup>();
        var g = t.GetComponent<GridLayoutGroup>();

        if (v != null) v.enabled = false;
        if (h != null) h.enabled = false;
        if (g != null) g.enabled = false;
    }

    private void EnableLayoutGroups(Transform t)
    {
        var v = t.GetComponent<VerticalLayoutGroup>();
        var h = t.GetComponent<HorizontalLayoutGroup>();
        var g = t.GetComponent<GridLayoutGroup>();

        if (v != null) v.enabled = true;
        if (h != null) h.enabled = true;
        if (g != null) g.enabled = true;
    }

    /// <summary>
    /// 外部动态设置 UI 列表（会覆盖 Inspector 拖拽的）
    /// </summary>
    public void SetUIObjects(List<RectTransform> uiObjects, bool dic = true)
    {
        this.UiObjects = uiObjects;
        uiObjectsManuallySet = true;
        hasRecordedPositions = false; // 重新采集坐标
        direction = dic;
        
        StartCoroutine(PrepareAnimation());
    }
}