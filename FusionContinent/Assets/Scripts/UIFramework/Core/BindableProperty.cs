using System;
using System.Collections.Generic;
using Game.UserTool;

namespace UIFramework
{
    /// <summary>
    /// 可绑定属性 - 数据变更时通知视图
    /// </summary>
    public class BindableProperty<T>
    {
        private T mValue;
        
        public T Value
        {
            get => mValue;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(mValue, value))
                {
                    var oldValue = mValue;
                    mValue = value;
                    OnValueChanged?.Invoke(oldValue, value);
                }
            }
        }
        
        public event Action<T, T> OnValueChanged;
        
        public BindableProperty(T initialValue = default)
        {
            mValue = initialValue;
        }
        
        public void SetWithoutNotify(T value)
        {
            mValue = value;
        }
    }

    /// <summary>
    /// 命令 - 封装可绑定的操作
    /// </summary>
    public class Command
    {
        private Action mAction;
        private Func<bool> mCanExecute;
        
        public event Action OnCanExecuteChanged;
        
        public Command(Action action, Func<bool> canExecute = null)
        {
            mAction = action;
            mCanExecute = canExecute;
        }
        
        public bool CanExecute => mCanExecute?.Invoke() ?? true;
        
        public void Execute()
        {
            if (CanExecute)
            {
                mAction?.Invoke();
            }
        }
        
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged?.Invoke();
        }
    }

    /// <summary>
    /// 带参数的Command
    /// </summary>
    public class Command<T>
    {
        private Action<T> mAction;
        private Func<T, bool> mCanExecute;
        
        public event Action OnCanExecuteChanged;
        
        public Command(Action<T> action, Func<T, bool> canExecute = null)
        {
            mAction = action;
            mCanExecute = canExecute;
        }
        
        public bool CanExecute(T param) => mCanExecute?.Invoke(param) ?? true;
        
        public void Execute(T param)
        {
            if (CanExecute(param))
            {
                mAction?.Invoke(param);
            }
        }
        
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged?.Invoke();
        }
    }

    /// <summary>
    /// ViewModel基类（纯C#）
    /// </summary>
    public abstract class ViewModel : IUserToolConfigurable
    {
        public virtual void OnCreate() { }
        public virtual void OnDestroy() { }
        
        /// <summary>
        /// 是否是全屏界面
        /// 全屏界面切换时会自动进行入栈出栈操作
        /// 注意：如果通过 UIManager 打开时指定了层级为 Common 或 PopUI，会自动设置为 true
        /// </summary>
        public virtual bool IsFullScreen { get; set; } = false;

        public virtual UserToolData GetUserToolConfig()
        {
            return null;
        }
    }

    /// <summary>
    /// 绑定器接口
    /// </summary>
    public interface IBinder
    {
        void Bind(ViewModel viewModel);
        void Unbind();
    }
}
