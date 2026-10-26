using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;
using FusionContinent.Utils;

namespace FusionContinent.Manager
{
    public class RollingNumberManager : MonoBehaviour
    {
        [Header("文字滚动速度")]
        public float duration = 0.5f;
        
        [Header("物体大小")]
        public float localScale = 0;

        private float _lastNumber;
        private TMP_Text _numberText;
        private Coroutine _rollingCoroutine;
        private bool _hasRolled;

        public void Init(float initNumber)
        {
            _lastNumber = initNumber;
            if (_numberText == null)
                _numberText = transform.GetComponent<TMP_Text>();
            _numberText.text = NumberTool.FormatNumber(_lastNumber);

            // 停止正在进行的动画
            if (_rollingCoroutine != null)
            {
                StopCoroutine(_rollingCoroutine);
                _rollingCoroutine = null;
            }
            transform.DOKill();
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 同步基准值（不触发动画）。外部直接修改文本后调用，保持 _lastNumber 一致，
        /// 避免后续 Play 动画从错误起点插值导致数字回跳。
        /// </summary>
        public void SyncBaseline(float num)
        {
            _lastNumber = num;
            if (_rollingCoroutine != null)
            {
                StopCoroutine(_rollingCoroutine);
                _rollingCoroutine = null;
            }
        }

        /// <summary>
        /// 播放数字变化动画。首次调用直接设置文本不播放动画，后续调用仅在数值变化时播放动画。
        /// </summary>
        /// <param name="targetNum">目标数值</param>
        /// <param name="forceAnimate">为 true 时，即使是首次调用也强制播放动画</param>
        /// <param name="overrideDuration">大于 0 时覆盖默认滚动时长（秒），用于货币等需要及时刷新的场景</param>
        public void Play(float targetNum, bool forceAnimate = false, float overrideDuration = -1f)
        {
            if (_numberText == null)
                _numberText = transform.GetComponent<TMP_Text>();

            // 判断GameObject是否被关闭：直接设值并同步基准值，避免恢复显示后动画从错误起点插值
            if (!_numberText.gameObject.activeInHierarchy)
            {
                _numberText.text = NumberTool.FormatNumber(targetNum);
                _lastNumber = targetNum;
                return;
            }

            // 首次调用：仅设置文本，不播放动画（避免每次打开界面都滚动）
            if (!forceAnimate && !_hasRolled)
            {
                _numberText.text = NumberTool.FormatNumber(targetNum);
                _lastNumber = targetNum;
                _hasRolled = true;
                return;
            }

            // 数值未变化时跳过动画
            if (Mathf.Approximately(_lastNumber, targetNum))
            {
                _numberText.text = NumberTool.FormatNumber(targetNum);
                return;
            }

            if (_rollingCoroutine != null)
                StopCoroutine(_rollingCoroutine);

            // 停止之前的缩放动画，防止 Tween 堆积
            transform.DOKill();
            transform.localScale = Vector3.one;
            var rollDuration = overrideDuration > 0f ? overrideDuration : duration;
            if (localScale > 0f)
                transform.DOScale(localScale, rollDuration * 0.3f)
                    .From(Vector3.one)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(Ease.OutQuad);

            _rollingCoroutine = StartCoroutine(Rolling(targetNum, rollDuration));
        }

        private IEnumerator Rolling(float targetNum, float rollDuration)
        {
            float elapsed = 0f;
            var startNum = _lastNumber;
            while (elapsed < rollDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rollDuration);
                int current = Mathf.RoundToInt(Mathf.Lerp(startNum, targetNum, t));
                _lastNumber = current;
                _numberText.text = NumberTool.FormatNumber(current);
                yield return null;
            }

            _numberText.text = NumberTool.FormatNumber(targetNum);
            _rollingCoroutine = null;
            _lastNumber = targetNum;
            _hasRolled = true;
        }
    }
}