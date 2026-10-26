using UnityEngine;

namespace FusionContinent.UI.Components
{
    /// <summary>
    /// 按钮装饰器基类。多个装饰器可以挂在同一个按钮上，按 Order 顺序执行。
    /// </summary>
    public abstract class GameUIButtonDecorator : MonoBehaviour
    {
        protected GameUIButton Owner { get; private set; }

        public virtual int Order => 0;

        public virtual bool AllowButtonInteractable => true;

        public virtual void Initialize(GameUIButton owner)
        {
            Owner = owner;
        }

        public virtual void Deinitialize()
        {
            Owner = null;
        }

        public virtual void Refresh()
        {
        }

        /// <summary>
        /// 返回 false 时拦截 GameUIButton 的逻辑点击事件。
        /// </summary>
        public virtual bool BeforeLogicClick()
        {
            return true;
        }

        public virtual void AfterLogicClick()
        {
        }
    }
}
