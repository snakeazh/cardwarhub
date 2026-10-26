using System;
using FusionContinent;
using FusionContinent.UI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ButtonStyleType
{
    ButtonBlue,
    ButtonRed,
    ButtonYellow,
    ButtonGreen,
    ButtonPink,
    ButtonGray,
}

/// <summary>
/// 按钮内容展示模式（对应 Prefab 上三套互斥节点）。
/// </summary>
public enum ButtonContentMode
{
    /// <summary>纯文字</summary>
    TextOnly = 0,
    /// <summary>文字 + 消耗品图标与数量</summary>
    TextAndConsume = 1,
    /// <summary>仅消耗品图标与数量</summary>
    ConsumeOnly = 2,
}

/// <summary>
/// 对应 Assets/Res/UIConfig/TextStyleSheet.asset 中的样式名。
/// 正文基准 Font Size=36；TitleDialog60 基准 Font Size=60。
/// </summary>
public enum ButtonTextStyle
{
    /// <summary>不改样式，沿用 Prefab 上的 Text Style</summary>
    Default = 0,
    Normal,
    ButtonSmall,
    ButtonMiddle,
    ButtonLarge,
    Content24,
    Content30,
    Content36,
    Content48,
    TitleDialog60,
}

public class ButtonData
{
    public ButtonStyleType buttonStyle;
    public ButtonContentMode ContentMode = ButtonContentMode.TextOnly;
    public ButtonTextStyle TextStyle = ButtonTextStyle.Default;
    public string ButtonDesc;
    /// <summary>消耗道具 Id（TextAndConsume / ConsumeOnly 时有效）</summary>
    public int ConsumeItemId;
    /// <summary>消耗所需数量</summary>
    public int ConsumeNeedCount;
    public Action OnClick;
}

/// <summary>
/// 通用样式按钮。外部只通过 SetData / SetInteractable 使用。
/// Prefab 结构：Bg、Btn + 三套内容根节点（同时只亮一套）。
/// </summary>
public class UICommonBtn : MonoBehaviour
{
    public Image Bg;

    [SerializeField]
    private UIPointerClickAndLongPress Btn;

    [Header("纯文字")]
    [SerializeField]
    private GameObject TextOnlyRoot;
    [SerializeField]
    private TextMeshProUGUI TextOnlyDesc;

    [Header("文字 + 消耗")]
    [SerializeField]
    private GameObject TextAndConsumeRoot;
    [SerializeField]
    private TextMeshProUGUI TextAndConsumeDesc;
    [SerializeField]
    private UIConsumeItem TextAndConsumeItem;

    [Header("仅消耗")]
    [SerializeField]
    private GameObject ConsumeOnlyRoot;
    [SerializeField]
    private UIConsumeItem ConsumeOnlyItem;

    private ButtonData mData;

    private void Awake()
    {
        Btn.onClick.AddListener(OnBtnClicked);
    }

    private void OnDestroy()
    {
        Btn.onClick.RemoveListener(OnBtnClicked);
    }

    public void SetData(ButtonData buttonData)
    {
        mData = buttonData;
        Bg.sprite = Global.ButtonAtlas.GetSprite(buttonData.buttonStyle.ToString());
        ApplyContent(buttonData);
    }

    public void SetInteractable(bool interactable)
    {
        Btn.interactable = interactable;
    }

    private void ApplyContent(ButtonData buttonData)
    {
        SetRootActive(TextOnlyRoot, buttonData.ContentMode == ButtonContentMode.TextOnly);
        SetRootActive(TextAndConsumeRoot, buttonData.ContentMode == ButtonContentMode.TextAndConsume);
        SetRootActive(ConsumeOnlyRoot, buttonData.ContentMode == ButtonContentMode.ConsumeOnly);

        switch (buttonData.ContentMode)
        {
            case ButtonContentMode.TextOnly:
                ApplyText(TextOnlyDesc, buttonData.ButtonDesc, buttonData.TextStyle);
                break;

            case ButtonContentMode.TextAndConsume:
                ApplyText(TextAndConsumeDesc, buttonData.ButtonDesc, buttonData.TextStyle);
                ApplyConsume(TextAndConsumeItem, buttonData);
                break;

            case ButtonContentMode.ConsumeOnly:
                ApplyConsume(ConsumeOnlyItem, buttonData);
                break;
        }
    }

    private static void SetRootActive(GameObject root, bool active)
    {
        if (root != null)
        {
            root.SetActive(active);
        }
    }

    private static void ApplyText(TextMeshProUGUI tmp, string text, ButtonTextStyle textStyle)
    {
        if (tmp == null)
        {
            LogTool.LogError("[UICommonBtn] 当前模式缺少文字节点绑定");
            return;
        }

        ApplyTextStyle(tmp, textStyle);
        tmp.text = text ?? string.Empty;
    }

    private static void ApplyConsume(UIConsumeItem consumeItem, ButtonData buttonData)
    {
        if (consumeItem == null)
        {
            LogTool.LogError("[UICommonBtn] 当前模式缺少 ConsumeItem 绑定");
            return;
        }

        if (consumeItem.CountText != null)
        {
            ApplyTextStyle(consumeItem.CountText, buttonData.TextStyle);
        }

        consumeItem.SetData(buttonData.ConsumeItemId, buttonData.ConsumeNeedCount);
    }

    private static void ApplyTextStyle(TMP_Text tmp, ButtonTextStyle textStyle)
    {
        if (textStyle == ButtonTextStyle.Default)
        {
            return;
        }

        if (textStyle == ButtonTextStyle.TitleDialog60)
        {
            tmp.fontSize = Utils.TextMeshProTools.TitleBaseFontSize;
        }
        else if (textStyle == ButtonTextStyle.Content24
            || textStyle == ButtonTextStyle.Content30
            || textStyle == ButtonTextStyle.Content36
            || textStyle == ButtonTextStyle.Content48
            || textStyle == ButtonTextStyle.ButtonSmall
            || textStyle == ButtonTextStyle.ButtonMiddle
            || textStyle == ButtonTextStyle.ButtonLarge)
        {
            tmp.fontSize = Utils.TextMeshProTools.BodyBaseFontSize;
        }

        Utils.TextMeshProTools.ApplyTextStyle(tmp, textStyle.ToString());
    }

    private void OnBtnClicked()
    {
        if (mData != null && mData.OnClick != null)
        {
            mData.OnClick.Invoke();
        }
    }
}
