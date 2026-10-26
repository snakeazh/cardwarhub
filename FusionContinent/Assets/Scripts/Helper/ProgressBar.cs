using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

namespace Helper
{
    public class ProgressBar : MonoBehaviour
    {
        public Image progress;
        public TMP_Text progressText;
        public float duration = 0.8f;

        // 调用此方法实现进度条缓动
        public void SetProgress(float targetValue, string text = "")
        {
            if (progress == null)
            {
                progress = GetComponent<Image>();
            }

            if (progressText != null)
            {
                progressText.text = text;
            }

            targetValue = Mathf.Clamp01(targetValue);

            // 先杀掉旧动画，避免叠加
            DOTween.Kill(progress);

            // 缓动 fillAmount
            progress.DOFillAmount(targetValue, duration)
                .SetEase(Ease.OutQuad)
                .SetId(progress).SetUpdate(true); // 用 Image 作为 ID
        }
    }
}