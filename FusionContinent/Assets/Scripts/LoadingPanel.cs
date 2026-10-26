using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingPanel : MonoBehaviour
{
    public Slider progressBar; // 进度条
    public TMP_Text loadingText; // 加载文本

    // 更新加载进度
    public void UpdateProgress(float progress)
    {
        progressBar.value = progress;
        loadingText.text = $"{Mathf.RoundToInt((progress * 100)>=100?100:progress * 100)}%";
    }

    // 显示加载页面
    public void Show()
    {
        // LoadingEffect.Instance.PlayAnim(true);
        //gameObject.SetActive(false);
        //gameObject.transform.localScale = Vector3.zero;
    }

    // 隐藏加载页面
    public void Hide()
    {
        //gameObject.SetActive(false);
        // LoadingEffect.Instance.PlayAnim(true);
    }

    // TODO 启动假的加载进度
    public void StartFakeLoading(float duration = 1f)
    {
        Show();
        // StartCoroutine(FakeLoadingCoroutine(duration));
    }

    // 协程实现假的加载进度
    private IEnumerator FakeLoadingCoroutine(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            UpdateProgress(progress);
            yield return null;
        }

        UpdateProgress(5f);
        yield return new WaitForSeconds(0.1f); // 可选：显示满进度一瞬间
        Hide();
    }
}