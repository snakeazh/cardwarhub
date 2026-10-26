using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FusionContinent.UI.Components
{
    /// <summary>
    /// Toggle 优化版 — 继承 <see cref="Selectable"/>，点击切换开关状态，
    /// 直接控制目标 GameObject 的显示/隐藏（而非仅单个 Graphic 的 Alpha）。
    /// 支持互斥分组 <see cref="UISelectableToggleGroup"/>。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UISelectableToggle : Selectable, ISubmitHandler
    {
        [Header("状态")]
        [SerializeField]
        private bool m_IsOn;

        [Header("分组")]
        [SerializeField]
        private UISelectableToggleGroup m_Group;

        [Header("目标对象")]
        [Tooltip("isOn = true  时显示")]
        [SerializeField]
        private GameObject[] m_ActiveTargets;

        [Tooltip("isOn = false 时显示")]
        [SerializeField]
        private GameObject[] m_InactiveTargets;

        [Header("事件")]
        [SerializeField]
        private BoolEvent m_OnValueChanged = new BoolEvent();

        public bool isOn
        {
            get => m_IsOn;
            set
            {
                if (m_IsOn == value)
                    return;

                if (value && m_Group != null && !m_Group.CanSwitchOn(this, value))
                    return;

                Set(value, true);
            }
        }

        public UISelectableToggleGroup group
        {
            get => m_Group;
            set => m_Group = value;
        }

        public BoolEvent onValueChanged => m_OnValueChanged;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_Group != null)
                m_Group.RegisterToggle(this);
            ApplyTargets();
        }

        protected override void OnDisable()
        {
            if (m_Group != null)
                m_Group.UnregisterToggle(this);
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (Application.isPlaying)
                return;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                    ApplyTargets();
            };
        }
#endif

        public void SetIsOnWithoutNotify(bool value)
        {
            Set(value, false);
        }

        public void Toggle()
        {
            isOn = !isOn;
        }

        private void Set(bool value, bool sendCallback)
        {
            if (m_IsOn == value)
                return;

            m_IsOn = value;

            if (m_IsOn && m_Group != null)
                m_Group.NotifyToggleOn(this);

            ApplyTargets();
            DoStateTransition(currentSelectionState, false);

            if (sendCallback)
                m_OnValueChanged.Invoke(m_IsOn);
        }

        private void ApplyTargets()
        {
            ApplyGameObjects(m_ActiveTargets, m_IsOn);
            ApplyGameObjects(m_InactiveTargets, !m_IsOn);
        }

        private static void ApplyGameObjects(GameObject[] targets, bool active)
        {
            if (targets == null)
                return;

            foreach (var obj in targets)
            {
                if (obj != null)
                    obj.SetActive(active);
            }
        }

        private bool m_Pressing;
        private int m_ActivePointerId = int.MinValue;

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            base.OnPointerDown(eventData);

            if (!IsActive() || !IsInteractable())
                return;

            m_Pressing = true;
            m_ActivePointerId = eventData.pointerId;
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                base.OnPointerUp(eventData);
                return;
            }

            var handledOurPointer = m_Pressing && eventData.pointerId == m_ActivePointerId;

            if (handledOurPointer && IsActive() && IsInteractable())
            {
                // 单个开关（无 group）或 group 允许全部不选时，点击可在 on/off 间来回切换；
                // 否则保持原有的「点击只能置 on」语义，避免把互斥页签里已选中的项点掉。
                if (!m_IsOn || m_Group == null || m_Group.allowSwitchOff)
                    Toggle();
                else
                    isOn = true;
            }

            m_Pressing = false;
            m_ActivePointerId = int.MinValue;
            base.OnPointerUp(eventData);
        }

        void ISubmitHandler.OnSubmit(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
                return;

            Toggle();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (m_IsOn)
            {
                base.DoStateTransition(SelectionState.Pressed, instant);
            }
            else
            {
                base.DoStateTransition(state, instant);
            }
        }
    }

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }
}
