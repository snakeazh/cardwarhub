using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(Button))]
public class ButtonClickPassThrough : MonoBehaviour
{
    private Button selfButton;
    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;

    private void Awake()
    {
        selfButton = GetComponent<Button>();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            LogTool.LogError($"[{name}] 没有找到父物体上的 Canvas 组件，穿透点击无法工作！");
            enabled = false;
            return;
        }

        raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            LogTool.LogError($"[{name}] Canvas 上没有找到 GraphicRaycaster 组件，穿透点击无法工作！");
            enabled = false;
            return;
        }

        eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            LogTool.LogError($"[{name}] 场景中没有找到 EventSystem，穿透点击无法工作！");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        selfButton.onClick.AddListener(OnSelfClick);
    }

    private void OnDisable()
    {
        selfButton.onClick.RemoveListener(OnSelfClick);
    }

    private void OnSelfClick()
    {
        // 构造 PointerEventData，使用当前鼠标位置
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        // 射线检测
        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        foreach (var result in results)
        {
            GameObject go = result.gameObject;

            // 跳过自己
            if (go == gameObject)
                continue;

            // 找到 Button 并触发
            Button btn = go.GetComponentInParent<Button>();
            if (btn != null && btn.interactable)
            {
                LogTool.Log($"[{name}] 穿透触发按钮: {go.name}");
                btn.onClick.Invoke();

                // 如果你想只触发第一个按钮，执行完这里就 break
                break;
            }
        }
    }
}
