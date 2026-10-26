using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FusionContinent.UI.Components
{
    /// <summary>
    /// 游戏统一按钮入口。具体表现由 GameUIButtonDecorator 子类叠加负责。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class GameUIButton : MonoBehaviour
    {
        [Header("通用")]
        [SerializeField] private Button mButton;

        [Tooltip("逻辑点击事件（只在允许点击时触发）")]
        [SerializeField] private UnityEvent mOnClick = new UnityEvent();

        private UnityEngine.Events.UnityAction _clickHandler;
        private bool _baseInteractable = true;
        private bool _initialized;
        private readonly List<GameUIButtonDecorator> _decorators = new List<GameUIButtonDecorator>();

        public Button Button => mButton;

        private void Reset()
        {
            mButton = GetComponent<Button>();
        }

        private void Awake()
        {
            if (mButton == null)
            {
                mButton = GetComponent<Button>();
            }

            _baseInteractable = mButton.interactable;
            RebuildDecorators();

            _clickHandler = OnButtonClicked;
            mButton.onClick.AddListener(_clickHandler);
            Refresh();
            _initialized = true;
        }

        private void OnEnable()
        {
            if (_initialized)
            {
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (mButton != null && _clickHandler != null)
            {
                mButton.onClick.RemoveListener(_clickHandler);
            }

            for (int i = 0; i < _decorators.Count; i++)
            {
                _decorators[i]?.Deinitialize();
            }

            _decorators.Clear();
        }

        #region 公共API

        public void RebuildDecorators()
        {
            for (int i = 0; i < _decorators.Count; i++)
            {
                _decorators[i]?.Deinitialize();
            }

            _decorators.Clear();
            GetComponents(_decorators);
            _decorators.Sort((a, b) => a.Order.CompareTo(b.Order));

            for (int i = 0; i < _decorators.Count; i++)
            {
                _decorators[i]?.Initialize(this);
            }
        }

        public void Refresh()
        {
            for (int i = 0; i < _decorators.Count; i++)
            {
                var decorator = _decorators[i];
                if (decorator != null && decorator.isActiveAndEnabled)
                {
                    decorator.Refresh();
                }
            }

            RefreshInteractable();
        }

        public void SetInteractable(bool interactable)
        {
            _baseInteractable = interactable;
            RefreshInteractable();
        }

        public void AddClickListener(UnityAction listener)
        {
            if (listener != null)
            {
                mOnClick.AddListener(listener);
            }
        }

        public void RemoveClickListener(UnityAction listener)
        {
            if (listener != null)
            {
                mOnClick.RemoveListener(listener);
            }
        }
        public void RemoveAllClickListener()
        {
            mOnClick.RemoveAllListeners();

        }
        public void InvokeLogicClick()
        {
            mOnClick?.Invoke();
        }

        #endregion

        private void RefreshInteractable()
        {
            if (mButton == null)
            {
                return;
            }

            bool decoratorsAllowClick = true;
            for (int i = 0; i < _decorators.Count; i++)
            {
                var decorator = _decorators[i];
                if (decorator != null && decorator.isActiveAndEnabled && !decorator.AllowButtonInteractable)
                {
                    decoratorsAllowClick = false;
                    break;
                }
            }

            mButton.interactable = _baseInteractable && decoratorsAllowClick;
        }

        private void OnButtonClicked()
        {
            Refresh();

            for (int i = 0; i < _decorators.Count; i++)
            {
                var decorator = _decorators[i];
                if (decorator != null && decorator.isActiveAndEnabled && !decorator.BeforeLogicClick())
                {
                    Refresh();
                    return;
                }
            }

            InvokeLogicClick();

            for (int i = 0; i < _decorators.Count; i++)
            {
                var decorator = _decorators[i];
                if (decorator != null && decorator.isActiveAndEnabled)
                {
                    decorator.AfterLogicClick();
                }
            }

            Refresh();
        }
    }
}
