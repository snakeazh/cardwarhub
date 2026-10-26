using System.Collections.Generic;
using UnityEngine;

namespace FusionContinent.UI.Components
{
    /// <summary>
    /// <see cref="UISelectableToggle"/> 的互斥分组。
    /// 挂到任意节点上，将同组 Toggle 的 group 字段指向它即可。
    /// </summary>
    [DisallowMultipleComponent]
    public class UISelectableToggleGroup : MonoBehaviour
    {
        [Header("分组行为")]
        [SerializeField]
        [Tooltip("true = 允许全部不选（再次点击已选中的会取消）。false = 必须有一个选中。")]
        private bool m_AllowSwitchOff;

        private readonly List<UISelectableToggle> m_Toggles = new List<UISelectableToggle>();

        public bool allowSwitchOff
        {
            get => m_AllowSwitchOff;
            set => m_AllowSwitchOff = value;
        }

        internal void RegisterToggle(UISelectableToggle toggle)
        {
            if (!m_Toggles.Contains(toggle))
                m_Toggles.Add(toggle);
        }

        internal void UnregisterToggle(UISelectableToggle toggle)
        {
            m_Toggles.Remove(toggle);
        }

        internal bool CanSwitchOn(UISelectableToggle toggle, bool toOn)
        {
            if (!toOn)
                return true;

            if (toggle.isOn && !m_AllowSwitchOff)
                return false;

            return true;
        }

        internal void NotifyToggleOn(UISelectableToggle toggle)
        {
            foreach (var t in m_Toggles)
            {
                if (t != null && t != toggle)
                    t.SetIsOnWithoutNotify(false);
            }
        }

        /// <summary>
        /// 确保至少有一个选中。如果全部都没选，强制选第一个。
        /// </summary>
        public void EnsureOneSelected()
        {
            if (m_AllowSwitchOff)
                return;

            foreach (var t in m_Toggles)
            {
                if (t != null && t.isOn)
                    return;
            }

            if (m_Toggles.Count > 0 && m_Toggles[0] != null)
                m_Toggles[0].isOn = true;
        }
    }
}
