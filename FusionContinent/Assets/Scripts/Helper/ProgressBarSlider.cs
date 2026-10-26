using DG.Tweening;
using FusionContinent.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.Helper
{
    /// <summary>
    /// 经验进度条（基于标准 Slider 驱动）
    /// 保留跨等级升级补间动画：从「战斗前等级经验」动画到「战斗后等级经验」。
    /// 对外接口 SetProgress / Play 与旧 LevelUpProgressBar 一致。
    /// </summary>
    public class ProgressBarSlider : MonoBehaviour
    {
        [SerializeField] private Slider mSlider;
        /// <summary>涨满（升级瞬间）播放的特效，默认取名为 SettlementLevelUp01 的子物体。</summary>
        [SerializeField] private GameObject mLevelUpEffect;
        public float DurationPerLevel = 0.45f;
        public float MinDuration = 0.6f;
        public Ease EaseType = Ease.OutQuad;

        private UserLevelInfo mStart;
        private UserLevelInfo mEnd;
        private Tween mTween;
        /// <summary>补间过程中已越过的等级，用于检测升级瞬间。</summary>
        private int mLastTweenLevel = -1;

        // 升级特效的播放组件缓存（避免每次播放时查找）
        private ParticleSystem[] mLevelUpParticles;
        private Animator mLevelUpAnimator;

        private void Awake()
        {
            if (mSlider == null)
            {
                mSlider = GetComponent<Slider>();
            }

            if (mLevelUpEffect == null)
            {
                var effect = transform.Find("SettlementLevelUp01");
                mLevelUpEffect = effect != null ? effect.gameObject : null;
            }
            if (mLevelUpEffect != null)
            {
                // 默认隐藏，只在进度条涨满（升级瞬间）时播放
                mLevelUpEffect.SetActive(false);
                // 预缓存播放组件，避免每次播放时查找
                mLevelUpParticles = mLevelUpEffect.GetComponentsInChildren<ParticleSystem>(true);
                mLevelUpAnimator = mLevelUpEffect.GetComponentInChildren<Animator>(true);
            }
        }

        /// <summary>
        /// 记录起点/终点（不播动画，先停在起点）
        /// </summary>
        public void SetProgress(UserLevelInfo start, UserLevelInfo end)
        {
            if (mTween != null)
            {
                mTween.Kill();
            }

            mStart = start;
            mEnd = end;
            mLastTweenLevel = mStart != null ? mStart.Level : -1;
            Refresh(GetLevelValue(start));
        }

        /// <summary>
        /// 播放跨等级补间动画（供 UIBattleEndAnim 动画结束后触发）
        /// </summary>
        public void Play()
        {
            if (mTween != null)
            {
                mTween.Kill();
            }

            float startProgress = GetLevelProgress(mStart);
            float endProgress = GetLevelProgress(mEnd);
            float startValue = GetLevelValue(mStart);
            float endValue = GetLevelValue(mEnd);
            int levelDistance = Mathf.Max(0, mEnd.Level - mStart.Level);
            float duration = Mathf.Max(MinDuration,
                (levelDistance + Mathf.Abs(endProgress - startProgress)) * DurationPerLevel);

            float curValue = startValue;
            Refresh(curValue);
            mTween = DOTween.To(
                    () => curValue,
                    value =>
                    {
                        curValue = value;
                        Refresh(value);
                        CheckLevelUp(value);
                    },
                    endValue,
                    duration)
                .SetEase(EaseType)
                .SetUpdate(true)
                .OnComplete(() => Refresh(endValue));
        }

        /// <summary>
        /// 等级值越过整数边界（进度条涨满回绕）即为升级瞬间，触发升级特效。
        /// </summary>
        private void CheckLevelUp(float levelValue)
        {
            int level = Mathf.FloorToInt(levelValue);
            if (level > mLastTweenLevel)
            {
                mLastTweenLevel = level;
                PlayLevelUpEffect();
            }
        }

        private void PlayLevelUpEffect()
        {
            if (mLevelUpEffect == null)
            {
                LogTool.LogError("[ProgressBarSlider][PlayLevelUpEffect] mLevelUpEffect == null，无法播放升级特效");
                return;
            }

            // 注意：不要在同一帧内 SetActive(false)→SetActive(true)。
            // Unity 对同一帧的状态翻转有合并优化，net 状态不变时不触发 OnEnable，
            // 会导致 ParticleSystem 的 playOnAwake 和 Animator 都不会真正启动。
            // 因此这里只激活（首次从 inactive→active 是真实转换，会触发 OnEnable），
            // 并显式 Play 粒子 / Rebind Animator，保证跨多级时也能可靠重播。
            mLevelUpEffect.SetActive(true);

            if (mLevelUpAnimator != null)
            {
                mLevelUpAnimator.Rebind();
            }
        }

        private float GetLevelProgress(UserLevelInfo levelInfo)
        {
            var lvCfg = DataConfigManager.Tables.TbPlayerLevelConfig.Get(levelInfo.Level);
            return levelInfo.Exp > 0 && lvCfg != null ? Mathf.Clamp01((float)levelInfo.Exp / lvCfg.Exp) : 0f;
        }

        private float GetLevelValue(UserLevelInfo levelInfo)
        {
            return levelInfo.Level + GetLevelProgress(levelInfo);
        }

        private void Refresh(float levelValue)
        {
            int level = Mathf.FloorToInt(levelValue);
            float progress = Mathf.Clamp01(levelValue - level);

            if (mSlider != null) mSlider.value = progress;
        }

        private void OnDestroy()
        {
            if (mTween != null)
            {
                mTween.Kill();
            }
        }
    }
}
