using UnityEngine;
using DG.Tweening;

namespace FusionContinent.Animation
{
    /// <summary>
    /// 挂载到 UI 图片上，自动上下平移滑动
    /// </summary>
    public class UITweenFloat : MonoBehaviour
    {
        [Header("移动距离")]
        public float Distance = 10f;

        [Header("单程时长(秒)")]
        public float Duration = 1.5f;

        [Header("缓动曲线")]
        public Ease EaseType = Ease.InOutSine;

        [Header("首次移动方向")]
        public bool MoveUpFirst = true;

        [Header("到达端点后停顿时间(秒)")]
        public float PauseDuration = 0f;

        private RectTransform mRect;
        private Vector2 mOriginPos;
        private Sequence mSequence;

        void Start()
        {
            mRect = GetComponent<RectTransform>();
            mOriginPos = mRect.anchoredPosition;
            Play();
        }

        void OnEnable()
        {
            if (mRect != null) Play();
        }

        void OnDisable()
        {
            Kill();
        }

        void OnDestroy()
        {
            Kill();
        }

        private void Play()
        {
            Kill();
            mRect.anchoredPosition = mOriginPos;

            float targetY = MoveUpFirst ? mOriginPos.y + Distance : mOriginPos.y - Distance;

            mSequence = DOTween.Sequence();
            mSequence.SetUpdate(true);
            mSequence.SetLoops(-1, LoopType.Yoyo);

            // 移动
            mSequence.Append(
                mRect.DOAnchorPosY(targetY, Duration).SetEase(EaseType).SetUpdate(true)
            );
            // 到达端点后停顿
            if (PauseDuration > 0f)
            {
                mSequence.AppendInterval(PauseDuration);
            }
        }

        private void Kill()
        {
            if (mSequence != null && mSequence.IsActive())
            {
                mSequence.Kill();
                mSequence = null;
            }
        }
    }
}
