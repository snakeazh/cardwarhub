using System;
using Cysharp.Threading.Tasks;
using FusionContinent;
using UIFramework;
using UnityEngine;

namespace Manager
{
    public class DialogManager
    {
        /// <summary>
        /// 显示通用确认框
        /// </summary>
        public static void ShowConfirm(string title, string message, Action onConfirm, Action onCancel = null, bool noCancel = false)
        {
            // 如果用户之前勾选了"不再提示"，直接执行确认并关闭
            if (Global.ConfirmDialogPanelStatus == 2)
            {
                onConfirm?.Invoke();
                return;
            }
            var vm = new ConfirmDialogPanelViewModel();
            vm.OnCreate();
            vm.Title = title;
            vm.Message = message ?? string.Empty;
            vm.NoCancel = noCancel;

            vm.OnConfirmRequested += onConfirm;
            if (onCancel != null)
            {
                vm.OnCancelRequested += onCancel;
            }

            UIManager.OpenPanelAsync<ConfirmDialogPanelNew, ConfirmDialogPanelViewModel>(vm, UILevel.PopUI).Forget();
        }
    }
}
