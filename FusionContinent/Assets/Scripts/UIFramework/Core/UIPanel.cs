using System;
using UnityEngine;

namespace UIFramework
{
    /// <summary>
    /// Panel状态
    /// </summary>
    public enum PanelState
    {
        None,
        Init,
        Opening,
        Open,
        Closing,
        Closed
    }

    /// <summary>
    /// UI层级 - 枚举值即为Sorting Order
    /// </summary>
    public enum UILevel
    {
        Bg = -100,
        Common = 0,
        Const = 100,
        Panel = 200,
        PopUI = 300,
        Guide = 400,
        Tips = 500,
        Loading = 1000,
    }

    public enum PanelStackMode
    {
        Managed,
        Unmanaged
    }

    public interface IUIData { }

    public abstract class UIPanel : MonoBehaviour
    {
        private UIPoolScope mPoolScope;

        public Transform Transform => transform;
        public GameObject GameObject => gameObject;
        public UILevel Level { get; set; } = UILevel.Common;
        public PanelState State { get; protected set; } = PanelState.None;
        public virtual PanelStackMode StackMode => PanelStackMode.Managed;
        public bool IsManaged => StackMode == PanelStackMode.Managed;
        protected UIPoolScope PoolScope => mPoolScope ??= EnsurePoolScope();

        protected IUIData mUIData;
        private Action mOnClosedCallback;

        public virtual void Init(IUIData uiData = null)
        {
            EnsurePoolScope();
            mUIData = uiData;
            State = PanelState.Init;
            OnInit(uiData);
        }

        public virtual void Open(IUIData uiData = null)
        {
            EnsurePoolScope();
            State = PanelState.Opening;
            mUIData = uiData ?? mUIData;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            OnOpen(uiData);
            State = PanelState.Open;
            OnBecameVisible();
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            OnShow();
            OnBecameVisible();
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
            OnHide();
        }

        public virtual void Close()
        {
            if (State == PanelState.Closed) return;

            State = PanelState.Closing;
            OnClose();
            ReleasePanelResources();
            mPoolScope?.ReleaseAll();

            // 平衡打开路径的图集引用(打开/预加载各 +1,此处 -1;未配置的界面静默跳过)
            if (AtlasManager.EnableAutoPreload)
            {
                AtlasManager.Instance.ReleaseForPanel(GetType());
            }

            mOnClosedCallback?.Invoke();
            mOnClosedCallback = null;

            State = PanelState.Closed;

            if (IsManaged)
            {
                UIManager.RemovePanel(this);
            }

            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        public void OnClosed(Action callback)
        {
            mOnClosedCallback += callback;
        }

        protected virtual void OnInit(IUIData uiData) { }
        protected virtual void OnOpen(IUIData uiData) { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnClose() { }

        /// <summary>
        /// 刷新界面显示（如：该界面上层的 PopUI 弹窗全部关闭后，由 UIManager 回调触发）
        /// </summary>
        public virtual void Refresh() { }

        /// <summary>
        /// 面板关闭后的资源释放（在 OnClose 之后、对象池回收之前）。
        /// </summary>
        protected virtual void ReleasePanelResources() { }

        protected virtual void OnBecameVisible()
        {
            if (!ShouldUpdateUserToolOnBecameVisible())
            {
                return;
            }

            UIManager.UpdateUserToolForPanel(this);
        }

        protected virtual bool ShouldUpdateUserToolOnBecameVisible()
        {
            return true;
        }

        protected void CloseSelf()
        {
            if (IsManaged)
            {
                UIManager.ClosePanel(this);
            }
            else
            {
                Close();
            }
        }

        private UIPoolScope EnsurePoolScope()
        {
            if (mPoolScope == null)
            {
                mPoolScope = UIPoolScope.Get(gameObject);
                mPoolScope.ReleaseOnDisable = false;
                mPoolScope.ReleaseOnDestroy = true;
            }

            return mPoolScope;
        }
    }

    public abstract class UIPanel<TViewModel> : UIPanel, IView where TViewModel : ViewModel
    {
        public TViewModel VM { get; private set; }
        public bool IsValid => this != null && gameObject != null && !Equals(null);
        ViewModel IView.ViewModel => VM;

        protected override void OnInit(IUIData uiData)
        {
            var vmData = uiData as ViewModelData;
            if (vmData?.ViewModel is TViewModel vm)
            {
                VM = vm;
                OnBindViewModel(vm);
            }
            else
            {
                LogTool.LogError($"[{GetType().Name}] 需要传入{typeof(TViewModel).Name}类型的ViewModel");
            }
        }
        
        protected override void ReleasePanelResources()
        {
            if (VM != null)
            {
                LogTool.Log($"{VM.GetType().Name} : 取消绑定");
                OnUnbindViewModel(VM);
                VM.OnDestroy();
                VM = null;
            }
        }

        protected abstract void OnBindViewModel(TViewModel vm);
        protected abstract void OnUnbindViewModel(TViewModel vm);

        void IView.Bind(ViewModel viewModel)
        {
            if (viewModel is TViewModel vm)
            {
                VM = vm;
            }
        }
    }
}
