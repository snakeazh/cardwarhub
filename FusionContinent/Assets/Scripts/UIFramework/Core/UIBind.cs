using System;
using UnityEngine;
using UnityEngine.UI;
#if UIFramework_TMPRO
using TMPro;
#endif

namespace UIFramework
{
    /// <summary>
    /// 绑定工具类 - 简化View中的绑定代码
    /// </summary>
    public static class UIBind
    {
        #region Text绑定
        
        /// <summary>
        /// 绑定Text组件到字符串属性
        /// </summary>
        public static void Text(Text text, BindableProperty<string> property)
        {
            if (text == null || property == null) return;
            
            // 初始值
            text.text = property.Value;
            
            // 监听变化
            property.OnValueChanged += (oldVal, newVal) =>
            {
                if (text != null) text.text = newVal;
            };
        }
        
        /// <summary>
        /// 绑定Text组件到int属性（自动转换）
        /// </summary>
        public static void Text(Text text, BindableProperty<int> property, string format = "{0}")
        {
            if (text == null || property == null) return;
            
            text.text = string.Format(format, property.Value);
            
            property.OnValueChanged += (oldVal, newVal) =>
            {
                if (text != null) text.text = string.Format(format, newVal);
            };
        }
        
        #endregion
        
#if UIFramework_TMPRO
        #region TMP_Text绑定
        
        /// <summary>
        /// 绑定TMP_Text组件
        /// </summary>
        public static void Text(TMP_Text text, BindableProperty<string> property)
        {
            if (text == null || property == null) return;
            
            text.text = property.Value;
            
            property.OnValueChanged += (oldVal, newVal) =>
            {
                if (text != null) text.text = newVal;
            };
        }
        
        /// <summary>
        /// 绑定TMP_Text组件到int
        /// </summary>
        public static void Text(TMP_Text text, BindableProperty<int> property, string format = "{0}")
        {
            if (text == null || property == null) return;
            
            text.text = string.Format(format, property.Value);
            
            property.OnValueChanged += (oldVal, newVal) =>
            {
                if (text != null) text.text = string.Format(format, newVal);
            };
        }
        
        #endregion
#endif
        
        #region Image绑定
        
        /// <summary>
        /// 绑定Image的sprite
        /// </summary>
        public static void Sprite(Image image, BindableProperty<Sprite> property)
        {
            if (image == null || property == null) return;
            
            image.sprite = property.Value;
            
            property.OnValueChanged += (oldVal, newVal) =>
            {
                if (image != null) image.sprite = newVal;
            };
        }
        
        /// <summary>
        /// 绑定Image的fillAmount
        /// </summary>
        public static void FillAmount(Image image, BindableProperty<float> property)
        {
            if (image == null || property == null) return;
            
            image.fillAmount = property.Value;
            
            property.OnValueChanged += (oldVal, newVal) =>
            {
                if (image != null) image.fillAmount = newVal;
            };
        }
        
        #endregion
        
        #region Button绑定
        
        /// <summary>
        /// 绑定Button点击到Command
        /// </summary>
        public static void Click(Button button, Command command)
        {
            if (button == null || command == null) return;
            
            // 点击执行命令
            button.onClick.AddListener(() => command.Execute());
            
            // 命令可用状态影响按钮交互
            Action updateInteractable = () => button.interactable = command.CanExecute;
            command.OnCanExecuteChanged += updateInteractable;
            updateInteractable();
        }
        
        /// <summary>
        /// 绑定Button点击到Command<T>
        /// </summary>
        public static void Click<T>(Button button, Command<T> command, Func<T> paramProvider)
        {
            if (button == null || command == null) return;
            
            button.onClick.AddListener(() => command.Execute(paramProvider()));
            
            // 这里简化处理，实际需要更复杂的CanExecute判断
            button.interactable = true;
        }
        
        /// <summary>
        /// 绑定Button点击到Action
        /// </summary>
        public static void Click(Button button, Action action)
        {
            if (button == null || action == null) return;
            button.onClick.AddListener(() => action());
        }
        
        #endregion
        
        #region GameObject绑定
        
        /// <summary>
        /// 绑定GameObject的active状态
        /// </summary>
        public static void Active(GameObject obj, BindableProperty<bool> property)
        {
            if (obj == null || property == null) return;
            
            obj.SetActive(property.Value);
            
            property.OnValueChanged += (oldVal, newVal) =>
            {
                if (obj != null) obj.SetActive(newVal);
            };
        }
        
        #endregion
        
        #region Slider绑定
        
        /// <summary>
        /// 绑定Slider值（双向绑定）
        /// </summary>
        public static void Slider(Slider slider, BindableProperty<float> property, bool twoWay = false)
        {
            if (slider == null || property == null) return;
            
            // ViewModel -> View
            slider.value = property.Value;
            property.OnValueChanged += (oldVal, newVal) =>
            {
                if (slider != null) slider.value = newVal;
            };
            
            // View -> ViewModel（双向）
            if (twoWay)
            {
                slider.onValueChanged.AddListener(val => property.Value = val);
            }
        }
        
        #endregion
    }
}
