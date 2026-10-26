using DG.Tweening;
using FusionContinent.Sounds;
using Manager;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FusionContinent.UI.Components
{
    /// <summary>
    /// 作为 <see cref="Button"/> 的替代：继承 <see cref="Selectable"/>，具备颜色/精灵/动画等过渡与导航能力，
    /// 同时提供短按（点击）与长按事件。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UIPointerClickAndLongPress : Selectable, IBeginDragHandler, IDragHandler, IEndDragHandler, ISubmitHandler
    {
        [Header("阈值")]
        [SerializeField, Min(0.05f)]
        private float m_LongPressDuration = 0.5f;

        [SerializeField, Min(0f)]
        private float m_MoveCancelThresholdPixels = 24f;

        [SerializeField]
        private bool m_UseUnscaledTime = true;

        [Header("行为")]
        [SerializeField]
        private bool m_CancelOnPointerExit;

        [Header("按下缩放")]
        [SerializeField]
        private bool m_EnablePressScale = true;

        [SerializeField, Range(0.5f, 1f)]
        private float m_PressedScale = 0.9f;

        [SerializeField, Min(0.01f)]
        private float m_ScaleDuration = 0.1f;

        [Tooltip("缩放目标，留空则使用本物体 Transform。")]
        [SerializeField]
        private Transform m_ScaleTarget;

        [Header("音效")]
        [SerializeField]
        [Tooltip("短按触发 onClick 时播放统一按钮点击音效（SoundType.CommonBtn）")]
        private bool m_EnableClickSound = true;

        [Header("事件")]
        [SerializeField]
        private UnityEvent m_OnClick;

        [SerializeField]
        private UnityEvent m_OnLongPress;

        [Tooltip("仅在曾经触发过长按后，指针抬起时调用一次。")]
        [SerializeField]
        private UnityEvent m_OnLongPressEnd;

        public UnityEvent onClick => m_OnClick;
        public UnityEvent onLongPress => m_OnLongPress;
        public UnityEvent onLongPressEnd => m_OnLongPressEnd;

        public float LongPressDuration
        {
            get => m_LongPressDuration;
            set => m_LongPressDuration = Mathf.Max(0.05f, value);
        }

        public float MoveCancelThresholdPixels
        {
            get => m_MoveCancelThresholdPixels;
            set => m_MoveCancelThresholdPixels = Mathf.Max(0f, value);
        }

        private bool m_Pressing;
        private int m_ActivePointerId = int.MinValue;
        private Vector2 m_PressScreenPosition;
        private float m_PressTime;
        private bool m_LongPressFired;
        private bool m_Cancelled;
        private bool m_ForwardingDrag;
        private ScrollRect m_ParentScrollRect;
        private Vector3 m_OriginalScale;
        private static readonly int s_ScaleTweenId = "UIPointerClickAndLongPress.Scale".GetHashCode();

        private Transform ScaleTransform => m_ScaleTarget != null ? m_ScaleTarget : transform;

        protected override void Awake()
        {
            base.Awake();
            m_OriginalScale = ScaleTransform.localScale;
        }

        protected override void OnDisable()
        {
            ResetPressTracking();
            RestoreScaleImmediate();
            base.OnDisable();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            base.OnPointerDown(eventData);
            if (!IsActive() || !IsInteractable())
            {
                return;
            }

            m_Pressing = true;
            m_ActivePointerId = eventData.pointerId;
            m_PressScreenPosition = eventData.position;
            m_PressTime = m_UseUnscaledTime ? Time.unscaledTime : Time.time;
            m_LongPressFired = false;
            m_Cancelled = false;
            PlayPressScale();
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                base.OnPointerUp(eventData);
                return;
            }

            var handledOurPointer = m_Pressing && eventData.pointerId == m_ActivePointerId;
            var eligibleClick = false;
            var longEnd = false;
            if (handledOurPointer)
            {
                var now = m_UseUnscaledTime ? Time.unscaledTime : Time.time;
                eligibleClick = !m_Cancelled && !m_LongPressFired && !m_ForwardingDrag
                    && !ScreenDeltaExceedsCancel(eventData.position)
                    && now - m_PressTime < m_LongPressDuration;
                longEnd = m_LongPressFired;
                if (m_ForwardingDrag)
                {
                    StopForwardingScrollDrag(eventData);
                }

                ResetPressTracking();
                PlayReleaseScale();
            }

            base.OnPointerUp(eventData);

            if (!handledOurPointer)
            {
                return;
            }

            if (eligibleClick)
            {
                PlayClickSound();
                m_OnClick?.Invoke();
            }

            if (longEnd)
            {
                m_OnLongPressEnd?.Invoke();
            }
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            if (m_CancelOnPointerExit && m_Pressing && eventData.pointerId == m_ActivePointerId)
            {
                m_Cancelled = true;
            }

            base.OnPointerExit(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!m_Pressing || eventData.pointerId != m_ActivePointerId)
            {
                return;
            }

            TryForwardScrollDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!m_Pressing || eventData.pointerId != m_ActivePointerId)
            {
                return;
            }

            if (m_ForwardingDrag)
            {
                m_ParentScrollRect.OnDrag(eventData);
                return;
            }

            if (ScreenDeltaExceedsCancel(eventData.position))
            {
                m_Cancelled = true;
                if (TryForwardScrollDrag(eventData))
                {
                    m_ParentScrollRect.OnDrag(eventData);
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            StopForwardingScrollDrag(eventData);
        }

        /// <summary>键盘/手柄提交时触发短按（与 <see cref="Button"/> 一致，不触发长按）。</summary>
        public void OnSubmit(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                return;
            }

            PlayClickSound();
            m_OnClick?.Invoke();
        }

        // 统一按钮点击音效，替代原 UIButtonClickSoundListener 的行为
        private void PlayClickSound()
        {
            if (!m_EnableClickSound)
            {
                return;
            }

            SoundManager.Instance.PlayVoice(SoundType.CommonBtn);
        }

        private void Update()
        {
            if (!m_Pressing || m_Cancelled || m_LongPressFired)
            {
                return;
            }

            if (!IsActive() || !IsInteractable())
            {
                return;
            }

            if (!TryGetPointerScreenPosition(m_ActivePointerId, out var screenPos))
            {
                return;
            }

            if (ScreenDeltaExceedsCancel(screenPos))
            {
                m_Cancelled = true;
                return;
            }

            var now = m_UseUnscaledTime ? Time.unscaledTime : Time.time;
            if (now - m_PressTime < m_LongPressDuration)
            {
                return;
            }

            m_LongPressFired = true;
            m_OnLongPress?.Invoke();
        }

        private bool ScreenDeltaExceedsCancel(Vector2 currentScreen)
        {
            var threshold = m_MoveCancelThresholdPixels;
            return (currentScreen - m_PressScreenPosition).sqrMagnitude >= threshold * threshold;
        }

        private bool TryForwardScrollDrag(PointerEventData eventData)
        {
            if (m_ForwardingDrag)
            {
                return true;
            }

            if (!ScreenDeltaExceedsCancel(eventData.position))
            {
                return false;
            }

            m_Cancelled = true;
            m_ParentScrollRect = GetComponentInParent<ScrollRect>();
            if (m_ParentScrollRect == null)
            {
                return false;
            }

            m_ForwardingDrag = true;
            RestoreScaleImmediate();
            m_ParentScrollRect.OnBeginDrag(eventData);
            return true;
        }

        private void StopForwardingScrollDrag(PointerEventData eventData)
        {
            if (!m_ForwardingDrag || m_ParentScrollRect == null)
            {
                return;
            }

            m_ParentScrollRect.OnEndDrag(eventData);
            m_ForwardingDrag = false;
            m_ParentScrollRect = null;
        }

        private void ResetPressTracking()
        {
            m_Pressing = false;
            m_ActivePointerId = int.MinValue;
            m_Cancelled = false;
            m_LongPressFired = false;
            m_ForwardingDrag = false;
            m_ParentScrollRect = null;
        }

        private void PlayPressScale()
        {
            if (!m_EnablePressScale)
            {
                return;
            }

            AnimateScaleTo(m_OriginalScale * m_PressedScale);
        }

        private void PlayReleaseScale()
        {
            if (!m_EnablePressScale)
            {
                return;
            }

            AnimateScaleTo(m_OriginalScale);
        }

        private void RestoreScaleImmediate()
        {
            if (!m_EnablePressScale)
            {
                return;
            }

            KillScaleTween();
            ScaleTransform.localScale = m_OriginalScale;
        }

        private void AnimateScaleTo(Vector3 targetScale)
        {
            KillScaleTween();
            var scaleTransform = ScaleTransform;
            scaleTransform
                .DOScale(targetScale, m_ScaleDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(m_UseUnscaledTime)
                .SetId(s_ScaleTweenId)
                .SetTarget(scaleTransform);
        }

        private void KillScaleTween()
        {
            DOTween.Kill(ScaleTransform, s_ScaleTweenId);
        }

        private static bool TryGetPointerScreenPosition(int pointerId, out Vector2 screenPos)
        {
            if (pointerId == PointerInputModule.kMouseLeftId || pointerId == -1)
            {
                screenPos = Input.mousePosition;
                return Input.GetMouseButton(0);
            }

            for (var i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.fingerId == pointerId)
                {
                    screenPos = t.position;
                    return t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
                }
            }

            screenPos = default;
            return false;
        }
    }
}
